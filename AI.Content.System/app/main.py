from fastapi import FastAPI
from app.api.routes import router
from app.infrastructure.persistence.db import init_db
from app.application.scheduler import start_scheduler
from app.core.logger import get_logger

logger = get_logger(__name__)

# 初始化 FastAPI 应用
app = FastAPI(
    title="AI Content Generation Platform",
    description="基于 DDD 架构的 AI 短视频内容生成平台",
    version="1.0.0"
)

# 事件：启动时初始化数据库和定时任务
@app.on_event("startup")
def startup_event():
    logger.info("应用启动中...")
    # 初始化数据库
    init_db()
    # 启动定时任务
    start_scheduler()
    logger.info("应用启动完成")

# 事件：关闭时停止定时任务
@app.on_event("shutdown")
def shutdown_event():
    logger.info("应用关闭中...")
    from app.application.scheduler import stop_scheduler
    stop_scheduler()
    logger.info("应用关闭完成")

# 注册路由
app.include_router(router, prefix="/api/v1")

# 根路由
@app.get("/")
def root():
    return {
        "message": "AI Content Generation Platform",
        "docs_url": "/docs",
        "redoc_url": "/redoc"
    }