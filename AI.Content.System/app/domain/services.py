from app.domain.entities import ContentItem
from app.core.logger import get_logger

logger = get_logger(__name__)

def calculate_rank_score(item: ContentItem) -> float:
    """
    计算内容排名分（核心领域逻辑）
    规则：基础分(score*0.6) + AI关键词加分 + 来源权重
    """
    # 基础分
    base_score = item.score * 0.6
    
    # AI 关键词加分
    ai_keywords = ["AI", "人工智能", "LLM", "大模型", "GPT", "OpenAI"]
    keyword_score = 20 if any(keyword in item.title for keyword in ai_keywords) else 0
    
    # 来源权重
    source_weights = {
        "reddit": 1.2,
        "hn": 1.1,
        "devto": 1.0
    }
    source_weight = source_weights.get(item.source, 1.0)
    
    # 最终得分
    total_score = (base_score + keyword_score) * source_weight
    
    logger.debug(f"内容[{item.title}]评分：基础分={base_score}, 关键词分={keyword_score}, 权重={source_weight}, 总分={total_score}")
    
    return round(total_score, 2)

def filter_high_quality_items(items: list[ContentItem], threshold: float = 30.0) -> list[ContentItem]:
    """过滤高分内容"""
    high_quality = [item for item in items if item.rank_score and item.rank_score >= threshold]
    logger.info(f"过滤后保留 {len(high_quality)}/{len(items)} 条高质量内容")
    return high_quality