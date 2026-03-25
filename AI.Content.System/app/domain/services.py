# -*- coding: utf-8 -*-
from typing import List
from app.domain.entities import ContentItem
from app.core.logger import get_logger

logger = get_logger(__name__)

# 将配置提取到外部，方便未来迁移到 settings 或数据库
RANK_CONFIG = {
    "AI_KEYWORDS": {"ai", "人工智能", "llm", "大模型", "gpt", "openai", "deepseek", "gemini", "rag"},
    "SOURCE_WEIGHTS": {
        "reddit": 1.2,
        "hn": 1.1,
        "devto": 1.0
    },
    "KEYWORD_BONUS": 20.0,
    "BASE_RATIO": 0.6
}

def calculate_rank_score(item: ContentItem) -> float:
    """
    计算内容排名分
    """
    # 1. 基础分计算
    base_score = (item.score or 0) * RANK_CONFIG["BASE_RATIO"]
    
    # 2. 关键词加分（不区分大小写）
    title_lower = item.title.lower()
    has_ai_keyword = any(kw in title_lower for kw in RANK_CONFIG["AI_KEYWORDS"])
    keyword_score = RANK_CONFIG["KEYWORD_BONUS"] if has_ai_keyword else 0.0
    
    # 3. 获取来源权重
    source_weight = RANK_CONFIG["SOURCE_WEIGHTS"].get(item.source.lower(), 1.0)
    
    # 4. 最终得分计算
    total_score = (base_score + keyword_score) * source_weight
    
    # 直接更新实体的属性，确保后续 filter 能读到
    item.rank_score = round(total_score, 2)
    
    logger.debug(f"评分详情 - [{item.title[:20]}...]: 基础={base_score}, 关键词={keyword_score}, 权重={source_weight}, 总分={item.rank_score}")
    
    return item.rank_score

def filter_high_quality_items(items: List[ContentItem], threshold: float = 30.0) -> List[ContentItem]:
    """
    过滤高分内容（带自动评分逻辑）
    """
    high_quality = []
    
    for item in items:
        # 如果还没评分，先调用评分函数
        if getattr(item, 'rank_score', None) is None:
            calculate_rank_score(item)
            
        if item.rank_score >= threshold:
            high_quality.append(item)
    
    # 排序：按分数从高到低排列，让 Pipeline 优先处理最优质内容
    high_quality.sort(key=lambda x: x.rank_score, reverse=True)
    
    logger.info(f"⚖️ 质量过滤：输入 {len(items)} 条 -> 保留 {len(high_quality)} 条 (阈值: {threshold})")
    return high_quality