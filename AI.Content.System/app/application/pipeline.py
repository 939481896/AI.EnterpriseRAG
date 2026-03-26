# -*- coding: utf-8 -*-
# app.application.pipeline.py
import time
from typing import List
from sqlalchemy.orm import Session

from app.domain.entities import ContentItem, GeneratedContent
from app.domain.services import calculate_rank_score, filter_high_quality_items
from app.infrastructure import web_extractor
from app.infrastructure.sources.reddit import RedditSource
from app.infrastructure.sources.hackernews import HackerNewsSource
from app.infrastructure.sources.devto import DevToSource
from app.infrastructure.sources.mit_techreview import MITTechReviewSource
from app.infrastructure.sources.venturebeat import VentureBeatSource
from app.infrastructure.web_extractor import WebExtractor
from app.infrastructure.ai.llm_client import LLMClient
from app.infrastructure.persistence.repository import ContentRepository
from app.core.config import settings
from app.core.logger import get_logger

logger = get_logger(__name__)

class ContentGenerationPipeline:
    def __init__(self, db: Session):
        self.db = db
        self.repository = ContentRepository(db)
        self.llm_client = LLMClient()
        self.web_extractor = WebExtractor()
        # 初始化数据源
        self.sources = [
            RedditSource(limit=settings.CRAWL_LIMIT),
            HackerNewsSource(limit=settings.CRAWL_LIMIT),
            DevToSource(limit=settings.CRAWL_LIMIT),
            MITTechReviewSource(limit=settings.CRAWL_LIMIT),
            VentureBeatSource(limit=settings.CRAWL_LIMIT)
        ]

    def run(self) -> int:
        """运行完整流水线"""
        logger.info("==============================")
        logger.info("🚀 启动自动化内容生成流水线")
        logger.info("==============================")

        # 1. 采集数据
        raw_items = self._fetch_all_sources()
        if not raw_items:
            logger.warning("未采集到任何原始数据，流程终止")
            return 0

        # 2. 评分与过滤
        # 注意：现在 calculate_rank_score 会自动处理 Entity 里的元数据
        qualified_items = filter_high_quality_items(raw_items, threshold=30.0)
        if not qualified_items:
            logger.info("⚖️ 过滤后无高质量内容，流程终止")
            return 0

        # 3. AI 生成与存储
        generated_list = []
        for i, item in enumerate(qualified_items):
            logger.info(f"进展: [{i+1}/{len(qualified_items)}] 正在处理: {item.source} -> {item.title[:30]}...")
            
            # 为免费版 Gemini 增加频率避让（防止 429 报错）
            if i > 0:
                time.sleep(3) 

            generated = self._process_single_item(item)
            if generated:
                generated_list.append(generated)

        # 4. 批量保存
        if generated_list:
            count = self.repository.bulk_save(generated_list)
            logger.info(f"🏁 流水线执行完毕！新增入库: {count} 条")
            return count
        
        return 0

    def _fetch_all_sources(self) -> List[ContentItem]:
        """从所有数据源采集并转换为领域对象"""
        all_items = []
        for source in self.sources:
            try:
                raw_data = source.fetch_raw()
                for data in raw_data:
                    # 这里的关键是：把爬虫抓到的 url 映射到实体中
                    all_items.append(ContentItem(
                        source=source.source_name,
                        title=data.get("title", ""),
                        score=data.get("score", 0),
                        url=data.get("url"), # 传递 URL
                        description=data.get("description") # 传递描述
                    ))
            except Exception as e:
                logger.error(f"源 {source.source_name} 采集异常: {e}")
        return all_items

    def _process_single_item(self, item: ContentItem) -> GeneratedContent:
        """调用 AI 处理单条内容"""
        try:

            # 2. 如果有 URL，尝试抓取深度内容
            full_content = self.web_extractor.get_main_content(item.url)

            # 3. AI 深度总结 (取代原本简短的 description)
            if full_content:
                deep_summary = self.llm_client.summarize_article(item.title, full_content)
                final_description = deep_summary if deep_summary else item.description
            else:
                final_description = item.description

            item.description=final_description
            # 第一步：生成选题
            topic = self.llm_client.generate_topic(item.title,item.description)
            if not topic:
                return None
            # 赛道分类
            category = self.llm_client.classify_topic(topic, item.description)

            # 第二步：生成脚本
            script = self.llm_client.generate_script(topic,item.description,category)
            if not script:
                return None
            final_score = getattr(item, "rank_score", 0.0) 
            if final_score == 0.0:
            # 如果万一没拿到 rank_score，尝试拿原始 score
                final_score = float(item.score) if item.score else 0.0
            # 构造最终实体，带上原有的 URL
            return GeneratedContent(
                source=item.source,
                original_title=item.title,
                url=item.url,  # 确保 URL 被传递到存储层
                topic=topic,
                category=category,
                description=item.description,
                script=script,
                score=final_score
            )
        except Exception as e:
            logger.error(f"AI 处理失败 [{item.title[:20]}]: {e}")
            return None