# 基础镜像：Python 3.10 slim（兼容Unstructured，轻量）
FROM python:3.10.19-slim

# 设置工作目录
WORKDIR /app

# 安装系统级依赖（仅保留必需项，避免OCR依赖增加镜像体积）
RUN apt update && apt install -y --no-install-recommends \
    libmagic1 \
    poppler-utils \
    && rm -rf /var/lib/apt/lists/*

# 安装Python依赖
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# 复制代码到容器
COPY unstructured_api.py .

# 暴露API端口
EXPOSE 9000

# 环境变量（仅保留必需项，注释单独换行）
# 实时输出日志，便于排查问题
ENV PYTHONUNBUFFERED=1

# 启动命令（4个worker提升并发）
CMD ["uvicorn", "unstructured_api:app", "--host", "0.0.0.0", "--port", "9000", "--workers", "4"]