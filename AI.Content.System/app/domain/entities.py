from dataclasses import dataclass
from typing import Optional, List

@dataclass
class ContentItem:
    """原始内容实体（爬虫结果）"""
    source: str
    title: str
    score: int
    rank_score: Optional[float] = None  # 计算后的排名分

@dataclass
class Topic:
    """AI 生成的选题"""
    content_item_id: int  # 关联的原始内容ID
    title: str
    script: Optional[str] = None  # 生成的脚本

@dataclass
class GeneratedContent:
    """最终生成的内容（存储用）"""
    source: str
    original_title: str
    topic: str
    script: str
    score: float