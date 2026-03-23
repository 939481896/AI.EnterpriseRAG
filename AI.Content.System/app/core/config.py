# -*- coding: utf-8 -*-
from pydantic import Field
from pydantic_settings import BaseSettings  # 关键：从 pydantic-settings 导入
from typing import Optional

class Settings(BaseSettings):
    # OpenAI 配置
    OPENAI_API_KEY: str
    OPENAI_MODEL: str = Field(default="gpt-4o-mini")
    
    # 数据库配置
    DB_URL: str = Field(default="sqlite:///./content.db")
    
    # 爬虫配置
    PROXY_ENABLE: bool = Field(default=False)
    PROXY_URL: Optional[str] = Field(default=None)
    CRAWL_LIMIT: int = Field(default=10)  # 每个源爬取数量
    REDDIT_URL: str = Field(default="https://www.reddit.com/r/artificial/hot.json")  # Reddit 抓取 URL
    
    # 定时任务配置
    SCHEDULER_INTERVAL_HOURS: int = Field(default=2)
    
    # 日志配置
    LOG_LEVEL: str = Field(default="INFO")

    class Config:
        env_file = ".env"
        env_file_encoding = "utf-8"

settings = Settings()