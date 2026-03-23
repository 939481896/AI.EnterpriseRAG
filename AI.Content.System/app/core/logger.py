import logging
from logging.handlers import RotatingFileHandler
import os
from app.core.config import settings

def get_logger(name: str) -> logging.Logger:
    """创建带文件轮转的日志器"""
    # 创建 logs 目录
    if not os.path.exists("logs"):
        os.makedirs("logs")
    
    # 配置日志格式
    formatter = logging.Formatter(
        "%(asctime)s - %(name)s - %(levelname)s - %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S"
    )
    
    # 文件处理器（轮转，最大 10MB，保留 5 个备份）
    file_handler = RotatingFileHandler(
        "logs/app.log",
        maxBytes=10*1024*1024,
        backupCount=5,
        encoding="utf-8"
    )
    file_handler.setFormatter(formatter)
    
    # 控制台处理器
    console_handler = logging.StreamHandler()
    console_handler.setFormatter(formatter)
    
    # 配置 logger
    logger = logging.getLogger(name)
    logger.setLevel(getattr(logging, settings.LOG_LEVEL))
    logger.addHandler(file_handler)
    logger.addHandler(console_handler)
    
    # 避免重复输出
    logger.propagate = False
    
    return logger