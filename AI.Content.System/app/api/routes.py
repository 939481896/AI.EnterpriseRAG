from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session
from app.application.pipeline import ContentGenerationPipeline
from app.application.scheduler import scheduler, start_scheduler, stop_scheduler
from app.infrastructure.persistence.db import get_db
from app.core.logger import get_logger

logger = get_logger(__name__)
router = APIRouter()

@router.get("/")
def health_check():
    """健康检查"""
    return {
        "status": "healthy",
        "scheduler_running": scheduler.running
    }

@router.get("/run-pipeline")
def run_pipeline_manual(db: Session = Depends(get_db)):
    """手动触发流水线"""
    try:
        pipeline = ContentGenerationPipeline(db)
        count = pipeline.run()
        return {
            "status": "success",
            "generated_count": count,
            "message": f"流水线执行完成，生成 {count} 条内容"
        }
    except Exception as e:
        logger.error(f"手动执行流水线失败: {e}", exc_info=True)
        raise HTTPException(status_code=500, detail=f"流水线执行失败: {str(e)}")

@router.get("/scheduler/start")
def start_scheduler_api():
    """启动定时任务"""
    try:
        start_scheduler()
        return {"status": "success", "message": "定时任务已启动"}
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"启动定时任务失败: {str(e)}")

@router.get("/scheduler/stop")
def stop_scheduler_api():
    """停止定时任务"""
    try:
        stop_scheduler()
        return {"status": "success", "message": "定时任务已停止"}
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"停止定时任务失败: {str(e)}")

@router.get("/scheduler/status")
def get_scheduler_status():
    """获取定时任务状态"""
    jobs = []
    if scheduler.running:
        for job in scheduler.get_jobs():
            jobs.append({
                "id": job.id,
                "next_run_time": job.next_run_time,
                "interval_hours": settings.SCHEDULER_INTERVAL_HOURS
            })
    
    return {
        "running": scheduler.running,
        "jobs": jobs
    }

@router.get("/content/{source}")
def get_content_by_source(source: str, limit: int = 10, db: Session = Depends(get_db)):
    """按数据源查询生成的内容"""
    from app.infrastructure.persistence.repository import ContentRepository
    repo = ContentRepository(db)
    contents = repo.get_by_source(source, limit)
    
    # 转换为字典返回
    return {
        "source": source,
        "count": len(contents),
        "contents": [
            {
                "id": c.id,
                "original_title": c.original_title,
                "topic": c.topic,
                "score": c.score,
                "created_at": c.created_at
            } for c in contents
        ]
    }