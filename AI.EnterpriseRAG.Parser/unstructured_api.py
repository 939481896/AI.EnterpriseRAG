# filename: unstructured_api.py
from fastapi import FastAPI, UploadFile, File, HTTPException, Depends
from fastapi.responses import JSONResponse
from fastapi.security import APIKeyHeader
from fastapi.middleware.cors import CORSMiddleware
from unstructured.partition.auto import partition
from unstructured.chunking.title import chunk_by_title
import os
import uuid
import tempfile
import magic  # 需安装：pip install python-magic
import asyncio
from typing import Optional
import logging

# ========== 日志配置（生产级调试） ==========
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
    handlers=[logging.StreamHandler()]
)
logger = logging.getLogger(__name__)

app = FastAPI(title="Unstructured 文档解析 API")

# ========== CORS跨域（适配.NET客户端） ==========
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # 生产环境替换为你的.NET服务域名
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# ========== API鉴权（避免接口滥用） ==========
API_KEY = "your_secure_api_key_here"  # 生产环境从环境变量读取
api_key_header = APIKeyHeader(name="X-API-Key", auto_error=False)

async def get_api_key(api_key_header: Optional[str] = Depends(api_key_header)):
    if api_key_header != API_KEY:
        raise HTTPException(status_code=401, detail="无效的API密钥")
    return api_key_header

# 原有配置
MAX_FILE_SIZE = 100 * 1024 * 1024  # 100MB
SUPPORTED_TYPES = ["pdf", "docx", "txt", "png", "jpg", "jpeg"]
CHUNK_SIZE = 800
CHUNK_OVERLAP = 100
PARSE_TIMEOUT = 600  # 解析超时时间（5分钟）

@app.post("/parse-document")
async def parse_document(
    file: UploadFile = File(...),
    api_key: str = Depends(get_api_key)  # 鉴权依赖
):
    temp_file = None
    try:
        # ========== 更可靠的文件大小校验 ==========
        file_content = await file.read()
        file_size = len(file_content)
        if file_size > MAX_FILE_SIZE:
            raise HTTPException(status_code=413, detail="文件大小不能超过100MB")
        
        # ========== 文件类型双重校验（避免后缀绕过） ==========
        if not file.filename:
            raise HTTPException(status_code=400, detail="文件名不能为空")
        
        # 后缀校验
        file_name_clean = os.path.basename(file.filename)  # 清洗路径，避免遍历
        file_ext = file_name_clean.split(".")[-1].lower() if "." in file_name_clean else ""
        if file_ext not in SUPPORTED_TYPES:
            raise HTTPException(status_code=415, detail=f"不支持的文件类型: {file_ext}")
        
        # 魔数校验（真实文件类型）
        file_type_magic = magic.from_buffer(file_content[:1024], mime=True)
        ext_to_mime = {
            "pdf": "application/pdf",
            "docx": "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "txt": "text/plain",
            "png": "image/png",
            "jpg": "image/jpeg",
            "jpeg": "image/jpeg"
        }
        if file_ext in ext_to_mime and file_type_magic != ext_to_mime[file_ext]:
            raise HTTPException(status_code=415, detail=f"文件类型不匹配：实际为{file_type_magic}")
        
        # ========== 创建临时文件（保留原有逻辑） ==========
        with tempfile.NamedTemporaryFile(delete=False, suffix=f".{file_ext}") as tmp:
            temp_file = tmp.name
            tmp.write(file_content)
        
        # ========== 解析超时控制 ==========
        try:
            # 异步执行解析（避免阻塞EventLoop）
            elements = await asyncio.wait_for(
                asyncio.to_thread(  # 用线程池执行同步解析逻辑
                    partition,
                    filename=temp_file,
                    extract_images_in_pdf=False,
                    infer_table_structure=True,
                    remove_background=False,
                    languages=["eng", "zh"]
                ),
                timeout=PARSE_TIMEOUT
            )
        except asyncio.TimeoutError:
            raise HTTPException(status_code=504, detail="文档解析超时（超过5分钟）")
        
        # ========== 原有分块逻辑（保留） ==========
        chunks = chunk_by_title(
            elements,
            max_characters=CHUNK_SIZE,
            overlap=CHUNK_OVERLAP
        )
        
        result = []
        for i, chunk in enumerate(chunks):
            result.append({
                "chunk_id": f"chunk-{i+1}",
                "title": getattr(chunk.metadata, 'title', "") or "",
                "content": chunk.text.strip(),
                "file_type": file_ext,
                "page_number": getattr(chunk.metadata, 'page_number', 0) or 0
            })
        
        logger.info(f"文档解析成功：{file_name_clean}，分块数：{len(result)}")
        return JSONResponse(content={
            "success": True,
            "chunks": result,
            "total_chunks": len(result)
        })

    # ========== 精细化异常捕获 ==========
    except HTTPException:
        # 主动抛出的业务异常，直接向上传递
        raise
    except Exception as e:
        logger.error(f"文档解析失败：{str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=f"解析失败: {str(e)}")
    
    finally:
        # ========== 更健壮的临时文件清理 ==========
        if temp_file and os.path.exists(temp_file):
            try:
                os.remove(temp_file)
                logger.info(f"临时文件已清理：{temp_file}")
            except Exception as e:
                logger.error(f"临时文件清理失败：{str(e)}")

# ========== 健康检查接口（便于监控） ==========
@app.get("/health")
async def health_check():
    return {"status": "healthy", "service": "unstructured-api"}