from typing import List
from app.domain.entities import ContentItem, GeneratedContent
from app.domain.services import filter_high_quality_items
from app.infrastructure.sources.reddit import RedditSource
from app.infrastructure.sources.hackernews import HackerNewsSource
from app.infrastructure.sources.devto import DevToSource
from app.infrastructure.ai.llm_client import LLMClient
from app.infrastructure.persistence.repository import ContentRepository
from app.core.logger import get_logger
from sqlalchemy.orm import Session

logger = get_logger(__name__)

class ContentGenerationPipeline:
    """内容生成流水线（应用层核心）"""
    def __init__(self, db: Session):
        # 初始化数据源
        self.sources = [
            RedditSource(),
            HackerNewsSource(),
            DevToSource()
        ]
        # 初始化 AI 客户端
        self.llm_client = LLMClient()
        # 初始化数据仓库
        self.repository = ContentRepository(db)

    def fetch_all_sources(self) -> List[ContentItem]:
        """抓取所有数据源的内容"""
        all_items = []
        for source in self.sources:
            items = source.fetch()
            all_items.extend(items)
        logger.info(f"所有数据源共抓取 {len(all_items)} 条内容")
        return all_items

    def generate_content(self, item: ContentItem) -> List[GeneratedContent]:
        """为单条内容生成选题和脚本"""
        generated_contents = []
        
        # 生成选题
        topics = self.llm_client.generate_topic(item.title)
        if not topics:
            logger.warning(f"为[{item.title}]生成选题为空，跳过")
            return []
        
        # 为每个选题生成脚本
        for topic in topics:
            script = self.llm_client.generate_script(topic)
            generated_content = GeneratedContent(
                source=item.source,
                original_title=item.title,
                topic=topic,
                script=script,
                score=item.rank_score or 0.0
            )
            generated_contents.append(generated_content)
        
        return generated_contents

    def run(self) -> int:
        """运行完整流水线"""
        logger.info("========== 启动内容生成流水线 ==========")
        
        # 1. 抓取所有数据源内容
        raw_items = self.fetch_all_sources()
        if not raw_items:
            logger.warning("未抓取到任何内容，流水线终止")
            return 0
        
        # 2. 过滤高质量内容
        high_quality_items = filter_high_quality_items(raw_items)
        if not high_quality_items:
            logger.warning("无高质量内容，流水线终止")
            return 0
        
        # 3. 生成选题和脚本
        all_generated = []
        for item in high_quality_items:
            generated = self.generate_content(item)
            all_generated.extend(generated)
        
        if not all_generated:
            logger.warning("未生成任何内容，流水线终止")
            return 0
        
        # 4. 保存到数据库
        saved_count = self.repository.bulk_save(all_generated)
        
        logger.info(f"========== 流水线完成，共生成并保存 {saved_count} 条内容 ==========")
        return saved_count