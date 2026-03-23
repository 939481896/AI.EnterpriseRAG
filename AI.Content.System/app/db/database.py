from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker
from app.config import DATABASE_URL
from app.db.models import Base  # 导入模型基类

# 创建数据库引擎（适配 Windows 路径）
engine = create_engine(
    DATABASE_URL, 
    connect_args={"check_same_thread": False}  # SQLite 必须加
)
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

# 创建/更新数据库表（新增字段会自动添加，不影响已有数据）
def create_tables():
    """创建所有数据库表（如果不存在），并同步字段"""
    Base.metadata.create_all(bind=engine)

# 新增：获取数据库会话的依赖函数（供接口使用）
def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()