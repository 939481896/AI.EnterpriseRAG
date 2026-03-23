from apscheduler.schedulers.background import BackgroundScheduler
from apscheduler.jobstores.sqlalchemy import SQLAlchemyJobStore
from app.core.config import settings
from app.core.logger import get_logger
from app.infrastructure.persistence.db import engine

logger = get_logger(__name__)

# 初始化定时任务调度器
scheduler = BackgroundScheduler(
    jobstores={
        "default": SQLAlchemyJobStore(engine=engine)  # 任务存储到数据库
    },
    timezone="Asia/Shanghai"
)

def run_pipeline_scheduled():
    """定时任务执行函数"""
    from app.infrastructure.persistence.db import SessionLocal
    from app.application.pipeline import ContentGenerationPipeline
    
    logger.info("定时任务触发：启动内容生成流水线")
    db = SessionLocal()
    try:
        pipeline = ContentGenerationPipeline(db)
        pipeline.run()
    finally:
        db.close()

def start_scheduler():
    """启动定时任务"""
    if not scheduler.running:
        # 添加定时任务（每 X 小时执行）
        scheduler.add_job(
            run_pipeline_scheduled,
            "interval",
            hours=settings.SCHEDULER_INTERVAL_HOURS,
            id="content_generation_pipeline",
            replace_existing=True
        )
        scheduler.start()
        logger.info(f"定时任务已启动，每 {settings.SCHEDULER_INTERVAL_HOURS} 小时执行一次")
    else:
        logger.info("定时任务已在运行中")

def stop_scheduler():
    """停止定时任务"""
    if scheduler.running:
        scheduler.shutdown()
        logger.info("定时任务已停止")