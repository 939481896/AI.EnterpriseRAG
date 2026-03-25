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
    """
    统一 LLM 客户端：支持火山方舟 (DeepSeek/Doubao) 接入
    特性：元数据感知、自动指数级退避、多模型接入点切换
    """
    def __init__(self):
        self.provider = settings.LLM_PROVIDER.lower()
        self.httpx_client = self._setup_httpx_client()
        
        # 对应火山方舟的不同接入点 ID (需在 .env 中配置)
        self.topic_model = getattr(settings, "VOLC_TOPIC_ENDPOINT", settings.VOLC_ENDPOINT_ID)
        self.script_model = getattr(settings, "VOLC_SCRIPT_ENDPOINT", settings.VOLC_ENDPOINT_ID)
        
        if self.provider == "gemini":
            self._init_gemini()
        elif self.provider == "volcengine":
            self._init_volcengine()
        elif self.provider == "deepseek":
            self._init_deepseek()
        else:
            self._init_openai()

    def _setup_httpx_client(self) -> httpx.Client:
        """配置支持代理的 httpx 客户端"""
        proxies = settings.PROXY_URL if settings.PROXY_ENABLE else None
        return httpx.Client(proxy=proxies, timeout=60.0)
    def _setup_no_proxy_client(self) -> httpx.Client:
        """创建一个纯净的、不带代理的 httpx 客户端（用于国内 API）"""
        return httpx.Client(timeout=60.0) # 不传入 proxy 参数

    def _init_volcengine(self):
        """初始化火山方舟 (兼容 OpenAI 格式)"""
        # 火山方舟国内访问通常不需要代理，但在 httpx_client 中已处理逻辑
        direct_client = self._setup_no_proxy_client()
        self.client = OpenAI(
            api_key=settings.VOLC_API_KEY,
            base_url="https://ark.cn-beijing.volces.com/api/v3",
            http_client=direct_client
        )
        logger.info(f"火山方舟客户端就绪，选题模型: {self.topic_model}，脚本模型: {self.script_model}")

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
        logger.info(f"DeepSeek 官方客户端就绪: {self.model}")

    def _init_gemini(self):
        if settings.PROXY_ENABLE and settings.PROXY_URL:
            for env_key in ["HTTP_PROXY", "HTTPS_PROXY", "http_proxy", "https_proxy"]:
                os.environ[env_key] = settings.PROXY_URL
        genai.configure(api_key=settings.GEMINI_API_KEY)
        self.model_name = settings.GEMINI_MODEL or "gemini-2.0-flash"
        self.client = genai.GenerativeModel(self.model_name)
        logger.info(f"Gemini 客户端就绪: {self.model_name}")

    def _ask_llm_with_retry(self, prompt: str, temperature: float, model_override: str = None, max_retries: int = 2) -> str:
        """核心执行逻辑：支持火山方舟多模型切换与 429 退避"""
        # 如果是火山方舟，使用传入的接入点 ID，否则使用默认 model
        current_model = model_override if model_override else getattr(self, "model", None)

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
                        model=current_model,
                        messages=[{"role": "user", "content": prompt}],
                        temperature=temperature
                    )
                    return response.choices[0].message.content
            except Exception as e:
                # 针对频率限制进行自动指数级退避 (火山方舟也会有频率限制)
                if "429" in str(e) and attempt < max_retries:
                    wait_time = (attempt + 1) * 15 
                    logger.warning(f"检测到频率限制 (429)，将在 {wait_time} 秒后重试... ({attempt + 1}/{max_retries})")
                    time.sleep(wait_time)
                    continue
                
                logger.error(f"LLM [{self.provider}] 调用异常: {e}")
                raise

    def generate_topic(self, title: str, description: str = "") -> str:
        """基于标题和摘要生成选题 - 加入爆款公式"""
        logger.info(f"正在生成选题: {title[:30]}...")
        
        context = f"标题：{title}\n内容摘要：{description}" if description else f"标题：{title}"
        prompt = (
            f"你是一个坐拥千万粉丝的抖音科技大V。请根据以下内容，提炼出一个最具冲击力的短视频标题：\n\n"
            f"{context}\n\n"
            f"要求：\n"
            f"1. 必须使用以下爆款公式之一：【认知反差】、【利益钩子】、【情绪共鸣】或【紧急警示】。\n"
            f"2. 严禁出现“第一”、“最”、“完美”等抖音违禁词。\n"
            f"3. 只返回一个标题，不解释，不带引号，15字以内。\n"
            f"4. 风格要直接、扎心、说人话。"
        )
        
        try:
            content = self._ask_llm_with_retry(
                prompt, 
                temperature=0.8, # 稍微提高随机性，增加网感
                model_override=self.topic_model
            )
            return content.strip().replace('"', '').replace('选题：', '')
        except Exception:
            return ""

    def generate_script(self, topic: str) -> str:
        """为选题生成脚本 - 优化时长、排版、避坑与标签"""
        logger.info(f"正在生成脚本: {topic[:20]}...")
        
        # 💡 核心优化：增加“分镜描述”和“违禁词过滤”
        prompt = (
            f"你现在是一个专业的短视频编剧。请为选题『{topic}』编写一个 1 分钟左右（约 300-400 字口播稿）的短视频脚本。\n\n"
            f"--- 强制要求 ---\n"
            f"1. **规避违禁词**：严禁出现“最、第一、赚钱、暴利、顶级、核心”等词汇。用“天花板、很香、核心、关键”等词替代。\n"
            f"2. **时长控制**：语速设定为 300 字/分钟，总内容确保在 60s-80s 之间。\n"
            f"3. **结构化输出**：请严格按照以下格式输出：\n\n"
            f"【视频信息】\n"
            f"- 建议BGM：(如：搞怪卡点/科技深邃)\n"
            f"- 核心标签：(提供3-5个带#的热门标签)\n\n"
            f"【脚本正文】\n"
            f"1. **黄金3秒 (开头钩子)**：必须直接抛出结论或制造巨大的反差冲突！\n"
            f"2. **反转/切入**：打破常规认知，带入核心干货。\n"
            f"3. **深度干货**：分 1、2、3 点说人话。每点一句话总结。\n"
            f"4. **结尾引导**：不要说“请点赞”，要用“如果是你，你会选xx还是yy？评论区告诉我”这类互动话术。\n\n"
            f"【拍摄建议】\n"
            f"- 请给出3个关键画面的视觉描述（例如：表情、特写、或PPT素材内容）。\n\n"
            f"语言风格：极度口语化（多用“我告诉你”、“听懂了吗”、“真的绝了”），像在和好哥们聊天。"
        )
        
        try:
            # 💡 建议使用 豆包 Pro 接入点，它的口语化程度非常高
            return self._ask_llm_with_retry(
                prompt, 
                temperature=0.85, 
                model_override=self.script_model
            )
        except Exception as e:
            return f"脚本生成失败: {e}"