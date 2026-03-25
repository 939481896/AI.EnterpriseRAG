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
    """手动触发流水线 (异步非阻塞)"""
    if not scheduler.running:
        logger.warning("定时任务未启动，但已通过 API 手动触发后台流水线")
    
    background_tasks.add_task(run_pipeline_task)
    
    return {
        "status": "accepted",
        "message": "内容生成任务已在后台启动。",
        "estimated_time": "每条内容约 30-60 秒"
    }

# ... (中间的 scheduler 相关路由保持不变) ...

@router.get("/content/list/{source}")
def get_content_by_source(source: str, limit: int = 20, db: Session = Depends(get_db)):
    """按数据源查询生成的内容列表 (仅展示摘要)"""
    try:
        repo = ContentRepository(db)
        contents = repo.get_by_source(source, limit)
        
        return {
            "source": source,
            "count": len(contents),
            "items": [
                {
                    "id": c.id,
                    "title": c.original_title,
                    "topic": c.topic,
                    "script_summary": c.script[:100] + "..." if c.script else None,
                    "score": round(c.score, 2) if c.score else 0,
                    "created_at": c.created_at
                } for c in contents
            ]
        }
    except Exception as e:
        logger.error(f"查询列表失败: {e}")
        raise HTTPException(status_code=500, detail="获取内容列表失败")

@router.get("/content/detail/{content_id}")
def get_content_detail(content_id: int, db: Session = Depends(get_db)):
    """获取单条内容的完整详细信息 (包括完整脚本)"""
    try:
        repo = ContentRepository(db)
        # 你的 ContentRepository 实现了 get_by_id 方法
        content = repo.get_by_id(content_id) 
    
        if not content:
            raise HTTPException(status_code=404, detail="内容未找到")
        
        return {
            "id": content.id,
            "source": content.source,
            "title": content.original_title,
            "topic": content.topic,
            "full_script": content.script, # 这里输出完整内容
            "score": content.score,
            "created_at": content.created_at
        }
    except Exception as e:
        logger.error(f"详情查询失败: {e}")
        raise HTTPException(status_code=500, detail="获取详情失败")

from fastapi.responses import Response

@router.get(
    "/content/export/{content_id}", 
    response_class=Response  # 💡 关键修正：显式声明返回原始响应类
)
def export_content_to_file(content_id: int, db: Session = Depends(get_db)):
    """
    导出脚本为纯文本文件 (.md格式，完美排版)
    """
    try:
        repo = ContentRepository(db)
        content = repo.get_by_id(content_id)
        
        if not content:
            raise HTTPException(status_code=404, detail="内容未找到")

        # 1. 组合文件名（处理中文文件名兼容性）
        safe_title = content.topic.replace("/", "_").replace("\\", "_")[:20]
        filename = f"{safe_title}.md"

        # 2. 准备文件内容
        file_content = (
            f"# 选题：{content.topic}\n"
            f"原文标题：{content.original_title}\n"
            f"评分：{content.score}\n"
            f"生成时间：{content.created_at}\n"
            f"{'='*30}\n\n"
            f"{content.script}"
        )

        # 3. 返回 Response
        return Response(
            content=file_content,
            media_type="text/markdown", # 💡 告诉浏览器这是 Markdown
            headers={
                # 💡 使用 utf-8 编码确保中文文件名不乱码
                "Content-Disposition": f'attachment; filename="{filename.encode("utf-8").decode("latin-1")}"'
            }
        )
    except Exception as e:
        logger.error(f"导出失败: {e}")
        raise HTTPException(status_code=500, detail="文件导出失败")