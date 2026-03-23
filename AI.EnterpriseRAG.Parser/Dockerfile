# 使用 Unstructured 官方完整版镜像作为基础
FROM unstructured-io/unstructured:latest

# 设置环境变量
ENV PYTHONUNBUFFERED=1
ENV PYTHONIOENCODING=utf-8
ENV API_KEY="your_secure_api_key_here"

# 设置工作目录
WORKDIR /app

# 安装 API 框架依赖（仅补充 FastAPI/uvicorn 等）
COPY requirements.txt .
RUN pip install --no-cache-dir \
    -i https://pypi.tuna.tsinghua.edu.cn/simple \
    -r requirements.txt

# 复制你的 API 代码
COPY unstructured_api.py .

# 暴露端口
EXPOSE 8000

# 健康检查
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD curl -f http://localhost:8000/health || exit 1

# 启动命令
CMD ["uvicorn", "unstructured_api:app", "--host", "0.0.0.0", "--port", "8000", "--workers", "2"]