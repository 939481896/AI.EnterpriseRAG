# -*- coding: utf-8 -*-
from fastapi import APIRouter, Depends, HTTPException, BackgroundTasks
from sqlalchemy.orm import Session
from datetime import datetime

from app.application.pipeline import ContentGenerationPipeline
from app.application.scheduler import scheduler, start_scheduler, stop_scheduler
from app.infrastructure.persistence.db import get_db, SessionLocal
from app.infrastructure.persistence.repository import ContentRepository
from app.core.config import settings
from app.core.logger import get_logger

logger = get_logger(__name__)
router = APIRouter()

# --- 辅助函数：后台执行逻辑 ---
def run_pipeline_task():
    """后台运行流水线的包装函数"""
    db = SessionLocal()
    try:
        logger.info("后台任务：正在启动手动触发的流水线...")
        pipeline = ContentGenerationPipeline(db)
        count = pipeline.run()
        logger.info(f"后台任务：流水线执行完毕，生成了 {count} 条内容")
    except Exception as e:
        logger.error(f"后台任务执行失败: {str(e)}", exc_info=True)
    finally:
        db.close()

# --- 路由定义 ---

@router.get("/health")
def health_check():
    """健康检查及状态监控"""
    return {
        "status": "healthy",
        "timestamp": datetime.now().isoformat(),
        "scheduler_running": scheduler.running,
        "llm_provider": settings.LLM_PROVIDER
    }

@router.post("/run-pipeline")
def run_pipeline_manual(background_tasks: BackgroundTasks):
    """
    手动触发流水线 (异步非阻塞)
    注意：使用 POST 更加符合 RESTful 规范（触发动作）
    """
    if not scheduler.running:
        logger.warning("定时任务未启动，但已通过 API 手动触发后台流水线")
    
    # 将耗时的 LLM 任务丢进后台，立即给前端返回响应
    background_tasks.add_task(run_pipeline_task)
    
    return {
        "status": "accepted",
        "message": "内容生成任务已在后台启动，请通过日志或查询接口查看进度。",
        "estimated_time": "每条内容约 30-60 秒"
    }

@router.get("/scheduler/start")
def start_scheduler_api():
    """启动定时任务调度器"""
    try:
        if scheduler.running:
            return {"status": "info", "message": "调度器已经在运行中"}
        start_scheduler()
        return {"status": "success", "message": "定时任务调度器已成功启动"}
    except Exception as e:
        logger.error(f"API启动调度器失败: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@router.get("/scheduler/stop")
def stop_scheduler_api():
    """停止定时任务调度器"""
    try:
        stop_scheduler()
        return {"status": "success", "message": "定时任务调度器已停止"}
    except Exception as e:
        logger.error(f"API停止调度器失败: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@router.get("/scheduler/status")
def get_scheduler_status():
    """获取详细的作业调度状态"""
    job_list = []
    try:
        if scheduler.running:
            for job in scheduler.get_jobs():
                job_list.append({
                    "job_id": job.id,
                    "next_run": job.next_run_time.isoformat() if job.next_run_time else None,
                    "trigger": str(job.trigger)
                })
        
        return {
            "is_running": scheduler.running,
            "active_jobs": job_list,
            "interval_hours": settings.SCHEDULER_INTERVAL_HOURS
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"查询状态失败: {str(e)}")

@router.get("/content/{source}")
def get_content_by_source(source: str, limit: int = 20, db: Session = Depends(get_db)):
    """
    按数据源查询生成的内容
    :param source: reddit / hn / devto
    :param limit: 返回条数
    """
    try:
        repo = ContentRepository(db)
        contents = repo.get_by_source(source, limit)
        
        return {
            "source": source,
            "query_time": datetime.now().isoformat(),
            "count": len(contents),
            "items": [
                {
                    "id": c.id,
                    "title": c.original_title,
                    "topic": c.topic,
                    "script": c.script[:100] + "..." if c.script else None, # 仅展示摘要
                    "score": round(c.score, 2) if c.score else 0,
                    "created_at": c.created_at.isoformat() if hasattr(c.created_at, 'isoformat') else str(c.created_at)
                } for c in contents
            ]
        }
    except Exception as e:
        logger.error(f"查询内容失败: {e}")
        raise HTTPException(status_code=500, detail="获取内容列表失败")