# -*- coding: utf-8 -*-
from typing import List
from app.domain.entities import ContentItem
from app.core.logger import get_logger

logger = get_logger(__name__)

# 将配置提取到外部，方便未来迁移到 settings 或数据库
RANK_CONFIG = {
    # 6 大赛道【英文 + 中文】关键词（匹配国外网站内容）
    "CATEGORY_KEYWORDS": {
        "ai_tech": {
            "ai", "artificial intelligence", "llm", "gpt", "openai", "deepseek", "gemini", "agi",
            "大模型", "人工智能", "科技", "编程", "算法", "模型", "数码"
        },
        "workplace": {
            "work", "job", "career", "office", "productivity", "manager", "leadership", "promotion",
            "职场", "工作", "效率", "汇报", "管理", "升职", "沟通", "办公"
        },
        "finance": {
            "finance", "investing", "money", "stock", "fund", "economy", "wealth", "budget",
            "财经", "理财", "投资", "经济", "基金", "股票", "财商", "资产"
        },
        "life": {
            "life", "home", "lifestyle", "gadget", "daily", "clean", "kitchen", "organization",
            "生活", "居家", "好物", "收纳", "日常", "家居", "幸福感"
        },
        "education": {
            "study", "learn", "education", "skill", "memory", "reading", "exam", "focus",
            "学习", "教育", "读书", "备考", "思维", "笔记", "专注力"
        },
        "business": {
            "business", "startup", "entrepreneur", "marketing", "strategy", "company", "industry",
            "创业", "商业", "生意", "模式", "行业", "老板", "认知"
        }
    },
    # 国外来源权重（Reddit 权重最高）
    "SOURCE_WEIGHTS": {
        "reddit": 1.2,
        "hn": 1.1,
        "devto": 1.0,
        "default": 1.0
    },
    "KEYWORD_BONUS": 16.0,    # 外网内容通用加分
    "BASE_RATIO": 0.6,        # 基础热度系数
    "DEFAULT_THRESHOLD": 24.0 # 外网内容更合理的阈值
}

def calculate_rank_score(item: ContentItem) -> float:
    """
    外网爬虫专用 | 中英文双语 | 6 赛道内容评分
    """
    # 1. 基础分（外网点赞/热度分）
    base_score = (item.score or 0) * RANK_CONFIG["BASE_RATIO"]

    # 2. 统一转小写（支持中英文匹配）
    title_low = item.title.strip().lower()
    keyword_score = 0.0
    matched_category = None

    # 3. 双语关键词匹配
    for category, keywords in RANK_CONFIG["CATEGORY_KEYWORDS"].items():
        if any(kw in title_low for kw in keywords):
            keyword_score = RANK_CONFIG["KEYWORD_BONUS"]
            matched_category = category
            break

    # 4. 来源权重
    source_key = item.source.lower() if item.source else "default"
    source_weight = RANK_CONFIG["SOURCE_WEIGHTS"].get(source_key, 1.0)

    # 5. 最终得分
    total_score = (base_score + keyword_score) * source_weight
    item.rank_score = round(total_score, 2)

    logger.debug(
        f"评分 [{matched_category or 'unknown'}] "
        f"| 标题={item.title[:25]}... "
        f"| 基础={base_score} +关键词={keyword_score} ×来源={source_weight} = {item.rank_score}"
    )
    return item.rank_score

def filter_high_quality_items(items: List[ContentItem], threshold: float = None) -> List[ContentItem]:
    """
    高质量内容过滤（外网双语专用）
    """
    if threshold is None:
        threshold = RANK_CONFIG["DEFAULT_THRESHOLD"]

    filtered = []
    for item in items:
        if not getattr(item, "rank_score", None):
            calculate_rank_score(item)

        if item.rank_score >= threshold:
            filtered.append(item)

    # 高分优先
    filtered.sort(key=lambda x: x.rank_score, reverse=True)

    logger.info(f"⚖️ 外网内容过滤：输入 {len(items)} → 保留 {len(filtered)} 条（阈值：{threshold}）")
    return filtered