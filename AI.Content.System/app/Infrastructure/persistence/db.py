# app/infrastructure/persistence/db.py
from sqlalchemy import create_engine
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker
from app.core.config import settings
from app.core.logger import get_logger

logger = get_logger(__name__)

# ========== 关键修复：删除自导入，直接定义 Base ==========
# 定义 SQLAlchemy 基础模型类（只定义一次）
Base = declarative_base()

# 创建数据库引擎
engine = create_engine(
    settings.DB_URL,
    # SQLite 特有配置：避免多线程问题
    connect_args={"check_same_thread": False} if "sqlite" in settings.DB_URL else {}
)

# 创建会话工厂（每次请求创建新会话）
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

def get_db():
    """FastAPI 依赖注入：获取数据库会话"""
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()

def init_db():
    """初始化数据库（创建所有表）"""
    logger.info("初始化数据库...")
    # 延迟导入 models，避免循环依赖
    import app.infrastructure.persistence.models
    Base.metadata.create_all(bind=engine)
    logger.info("数据库初始化完成")