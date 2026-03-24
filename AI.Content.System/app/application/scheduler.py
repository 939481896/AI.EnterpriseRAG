# -*- coding: utf-8 -*-
from datetime import datetime
from apscheduler.schedulers.background import BackgroundScheduler
from apscheduler.jobstores.sqlalchemy import SQLAlchemyJobStore
from app.core.config import settings
from app.core.logger import get_logger
from app.infrastructure.persistence.db import engine

logger = get_logger(__name__)

# 初始化定时任务调度器
# 提示：使用 SQLAlchemyJobStore 会在数据库中生成一张 apscheduler_jobs 表
scheduler = BackgroundScheduler(
    jobstores={
        "default": SQLAlchemyJobStore(engine=engine)
    },
    timezone="Asia/Shanghai"
)

def run_pipeline_scheduled():
    """定时任务执行函数（带独立 Session 管理）"""
    from app.infrastructure.persistence.db import SessionLocal
    from app.application.pipeline import ContentGenerationPipeline
    
    logger.info("⏰ [Scheduler] 定时任务触发：启动内容生成流水线")
    db = SessionLocal()
    try:
        pipeline = ContentGenerationPipeline(db)
        # 执行流水线逻辑
        count = pipeline.run()
        logger.info(f"✅ [Scheduler] 定时任务执行完毕，生成了 {count} 条内容")
    except Exception as e:
        logger.error(f"❌ [Scheduler] 定时任务执行失败: {str(e)}", exc_info=True)
    finally:
        # 务必手动关闭数据库连接，防止连接池耗尽
        db.close()

def start_scheduler():
    """启动定时任务"""
    if not scheduler.running:
        # 添加定时任务
        scheduler.add_job(
            run_pipeline_scheduled,
            trigger="interval",
            hours=settings.SCHEDULER_INTERVAL_HOURS,
            id="content_generation_pipeline",
            replace_existing=True,
            # 💡 优化点 1: 启动时立即执行一次，防止等待几小时才看到效果
            next_run_time=datetime.now(),
            # 💡 优化点 2: 允许任务在错过预定时间后的 3600 秒内仍能补跑
            misfire_grace_time=3600,
            # 💡 优化点 3: 限制同一时间只能运行一个该任务实例，防止抓取任务重叠
            max_instances=1
        )
        scheduler.start()
        logger.info(f"🚀 定时任务已启动，每 {settings.SCHEDULER_INTERVAL_HOURS} 小时执行一次")
    else:
        logger.info("⚠️ 定时任务已在运行中")

def stop_scheduler():
    """安全停止定时任务"""
    if scheduler.running:
        # wait=True 确保正在运行的 Pipeline 处理完当前条目后再退出
        #scheduler.shutdown(wait=True)
        #logger.info("🛑 定时任务已安全停止")
        scheduler.shutdown(wait=False)
        logger.info("定时任务已强制停止")
