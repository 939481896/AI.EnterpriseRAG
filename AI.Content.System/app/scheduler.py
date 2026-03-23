from apscheduler.schedulers.background import BackgroundScheduler
from app.pipeline.pipeline import run_pipeline

scheduler = BackgroundScheduler()

# 每2小时执行一次
scheduler.add_job(run_pipeline, "interval", hours=2)

def start_scheduler():
    try:
        scheduler.start()
        print("定时任务已启动，每2小时执行一次")
    except Exception as e:
        print("定时任务启动失败:", e)