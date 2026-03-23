# 补全所有必要的导入（关键修复点）
from fastapi import FastAPI, HTTPException, Depends, Query
from sqlalchemy.orm import Session
from sqlalchemy import desc
from datetime import datetime

# 导入项目内部模块
from app.db.database import create_tables, get_db
from app.db.models import Content
from app.pipeline.pipeline import run_pipeline

# 初始化 FastAPI 应用
app = FastAPI(title="AI Content System")

# 启动时创建/更新数据库表
create_tables()

# 基础健康检查接口
@app.get("/")
def root():
    return {
        "msg": "AI Content System Running",
        "endpoints": {
            "/run": "执行内容生成流水线",
            "/history": "查询历史生成数据（倒序）"
        }
    }

# 执行流水线接口
@app.get("/run")
def run():
    try:
        run_pipeline()
        return {"status": "ok", "msg": "Pipeline executed successfully"}
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"流水线执行失败：{str(e)}")

# 新增：查询历史数据接口（倒序排列）
@app.get("/history")
def get_history(
    db: Session = Depends(get_db),  # 现在 Depends 已导入，不会报错
    # 分页参数（默认第1页，每页10条）
    page: int = Query(1, ge=1, description="页码，从1开始"),
    page_size: int = Query(10, ge=1, le=100, description="每页条数，最大100"),
    # 可选筛选条件
    source: str = Query(None, description="按来源筛选（reddit/hn/twitter）")
):
    try:
        # 构建查询条件
        query = db.query(Content)
        
        # 可选：按来源筛选
        if source:
            query = query.filter(Content.source == source)
        
        # 按插入时间倒序排列（核心需求）
        query = query.order_by(desc(Content.created_at))
        
        # 总条数（用于分页）
        total = query.count()
        
        # 分页计算
        offset = (page - 1) * page_size
        data = query.offset(offset).limit(page_size).all()
        
        # 格式化返回结果
        result = {
            "total": total,  # 总条数
            "page": page,
            "page_size": page_size,
            "total_pages": (total + page_size - 1) // page_size,  # 总页数
            "data": [
                {
                    "id": item.id,
                    "source": item.source,
                    "original_title": item.title,  # 原始标题
                    "ai_topic": item.topic,        # AI生成的选题
                    "ai_script": item.script,      # AI生成的脚本
                    "created_at": item.created_at.strftime("%Y-%m-%d %H:%M:%S") if item.created_at else None  # 兼容旧数据
                }
                for item in data
            ]
        }
        
        return result
    
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"查询失败：{str(e)}")

# 可选：启动定时任务（如需自动执行，取消注释）
# from app.scheduler import start_scheduler
# start_scheduler()