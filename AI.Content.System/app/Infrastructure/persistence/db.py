from sqlalchemy import Column, Integer, String, Text, Float, DateTime
from sqlalchemy.sql import func
from app.infrastructure.persistence.db import Base

class ContentModel(Base):
    """内容存储模型"""
    __tablename__ = "generated_contents"
    
    id = Column(Integer, primary_key=True, index=True)
    source = Column(String, index=True)  # 数据源（reddit/hn/devto）
    original_title = Column(String, index=True)  # 原始标题
    topic = Column(String, index=True)  # AI 生成的选题
    script = Column(Text)  # AI 生成的脚本
    score = Column(Float)  # 计算后的排名分
    created_at = Column(DateTime, default=func.now())  # 创建时间