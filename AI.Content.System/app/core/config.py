# -*- coding: utf-8 -*-
from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict
from typing import Optional

class Settings(BaseSettings):
    # 使用最新的 model_config 替代旧的 class Config
    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        extra="ignore",          # 允许 .env 中存在类中未定义的变量
        case_sensitive=False     # 环境变量不区分大小写
    )

    # --- LLM 配置 ---
    LLM_PROVIDER: str = "openai"

    # --- OpenAI 配置 ---
    OPENAI_API_KEY: Optional[str] = None
    OPENAI_MODEL: str = "gpt-4o-mini"
    
    # --- DeepSeek 配置 ---
    DEEPSEEK_API_KEY: Optional[str] = None
    DEEPSEEK_MODEL: str = "deepseek-chat"
    
    # --- Gemini 配置 ---
    GEMINI_API_KEY: Optional[str] = None
    GEMINI_MODEL: str = "gemini-2.0-flash"

    # --- 数据库配置 ---
    DB_URL: str = "sqlite:///./content.db"
    
    # --- 爬虫配置 ---
    PROXY_ENABLE: bool = False
    PROXY_URL: Optional[str] = None
    CRAWL_LIMIT: int = 10
    REDDIT_URL: str = "https://www.reddit.com/r/artificial/hot.json"
    
    # --- 定时任务配置 ---
    SCHEDULER_INTERVAL_HOURS: int = 2
    
    # --- 日志配置 ---
    LOG_LEVEL: str = "INFO"

# 实例化
settings = Settings()