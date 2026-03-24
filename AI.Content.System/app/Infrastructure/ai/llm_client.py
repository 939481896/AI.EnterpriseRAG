# -*- coding: utf-8 -*-
import os
import time
import httpx
import google.generativeai as genai
from openai import OpenAI
from app.core.config import settings
from app.core.logger import get_logger

logger = get_logger(__name__)

class LLMClient:
    """统一 LLM 客户端：支持元数据感知与自动重试逻辑"""
    def __init__(self):
        self.provider = settings.LLM_PROVIDER.lower()
        self.httpx_client = self._setup_httpx_client()
        
        if self.provider == "gemini":
            self._init_gemini()
        elif self.provider == "deepseek":
            self._init_deepseek()
        else:
            self._init_openai()

    def _setup_httpx_client(self) -> httpx.Client:
        """配置支持代理的 httpx 客户端"""
        proxies = settings.PROXY_URL if settings.PROXY_ENABLE else None
        return httpx.Client(proxy=proxies, timeout=60.0)

    def _init_openai(self):
        self.client = OpenAI(api_key=settings.OPENAI_API_KEY, http_client=self.httpx_client)
        self.model = settings.OPENAI_MODEL or "gpt-4o-mini"
        logger.info(f"OpenAI 客户端就绪: {self.model}")

    def _init_deepseek(self):
        self.client = OpenAI(
            api_key=settings.DEEPSEEK_API_KEY,
            base_url="https://api.deepseek.com",
            http_client=self.httpx_client
        )
        self.model = settings.DEEPSEEK_MODEL or "deepseek-chat"
        logger.info(f"DeepSeek 客户端就绪: {self.model}")

    def _init_gemini(self):
        # 💡 强化：确保环境变量覆盖所有可能的代理 Key
        if settings.PROXY_ENABLE and settings.PROXY_URL:
            for env_key in ["HTTP_PROXY", "HTTPS_PROXY", "http_proxy", "https_proxy"]:
                os.environ[env_key] = settings.PROXY_URL
            
        genai.configure(api_key=settings.GEMINI_API_KEY)
        self.model_name = settings.GEMINI_MODEL or "gemini-2.0-flash"
        self.client = genai.GenerativeModel(self.model_name)
        logger.info(f"Gemini 客户端就绪: {self.model_name}")

    def _ask_llm_with_retry(self, prompt: str, temperature: float, max_retries: int = 2) -> str:
        """核心执行逻辑：增加自动重试机制应对 429 错误"""
        for attempt in range(max_retries + 1):
            try:
                if self.provider == "gemini":
                    response = self.client.generate_content(
                        prompt,
                        generation_config={"temperature": temperature},
                        request_options={"timeout": 40}
                    )
                    return response.text
                else:
                    response = self.client.chat.completions.create(
                        model=self.model,
                        messages=[{"role": "user", "content": prompt}],
                        temperature=temperature
                    )
                    return response.choices[0].message.content
            except Exception as e:
                # 💡 针对 429 频率限制进行自动指数级退避
                if "429" in str(e) and attempt < max_retries:
                    wait_time = (attempt + 1) * 10 
                    logger.warning(f"检测到频率限制 (429)，将在 {wait_time} 秒后重试... ({attempt + 1}/{max_retries})")
                    time.sleep(wait_time)
                    continue
                
                logger.error(f"LLM [{self.provider}] 调用终极异常: {e}")
                raise

    def generate_topic(self, title: str, description: str = "") -> str:
        """
        基于标题和描述生成最精准的选题
        💡 改进：接收 description 参数，增强 AI 理解力
        """
        logger.info(f"正在请求 LLM 生成选题: {title[:30]}...")
        
        # 构建更丰富的上下文
        context = f"标题：{title}\n内容摘要：{description}" if description else f"标题：{title}"
        
        prompt = (
            f"你是一个爆款内容专家。请根据以下内容，提炼出一个最具传播力的短视频选题标题：\n\n"
            f"{context}\n\n"
            f"要求：只返回一个标题，不要解释，不要带引号，字数在20字以内，极具吸引力。"
        )
        
        try:
            # 💡 增加退避，给 API 留出余地
            time.sleep(1)
            content = self._ask_llm_with_retry(prompt, temperature=0.7)
            return content.strip().replace('"', '').replace('选题：', '')
        except Exception:
            return ""

    def generate_script(self, topic: str) -> str:
        """为特定选题生成脚本"""
        logger.info(f"正在生成详细脚本: {topic[:20]}...")
        prompt = (
            f"请为短视频选题『{topic}』编写一个完整的拍摄脚本。\n"
            f"结构要求：\n"
            f"1. 黄金3秒（开头钩子）\n"
            f"2. 核心内容（干货或冲突点）\n"
            f"3. 结尾引导（点赞或评论互动）\n"
            f"语言风格：口语化、快节奏、直接。"
        )
        try:
            return self._ask_llm_with_retry(prompt, temperature=0.8)
        except Exception as e:
            return f"脚本生成失败: {e}"