# -*- coding: utf-8 -*-
import os
import sys
import uvicorn

# 强制将项目根目录加入 Python 路径（核心修复）
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

# 解决 Windows 编码问题
os.environ["PYTHONIOENCODING"] = "utf-8"
os.environ["PYTHONUTF8"] = "1"

# 手动导入并启动（避免子进程导入）
if __name__ == "__main__":
    # 直接启动 FastAPI，不使用 reload（Windows 推荐）
    uvicorn.run(
        "app.main:app",
        host="0.0.0.0",
        port=8000,
        workers=1,  # 强制单进程
        reload=False  # 关闭 reload，避免子进程
    )