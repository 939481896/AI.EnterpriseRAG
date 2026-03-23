from sqlalchemy import Column, Integer, String, Text, DateTime
from sqlalchemy.ext.declarative import declarative_base
from datetime import datetime

Base = declarative_base()

class Content(Base):
    __tablename__ = "contents"

    id = Column(Integer, primary_key=True, index=True)
    source = Column(String, index=True)  # 来源（reddit/hn/twitter）
    title = Column(String)  # 原始标题
    topic = Column(String)  # AI生成的选题
    script = Column(Text)   # AI生成的脚本
    # 新增：插入时间（自动设置为当前时间）
    created_at = Column(DateTime, default=datetime.now, index=True)