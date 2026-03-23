# app/ai/topic_generator.py
import os
from openai import OpenAI
from app.config import OPENAI_API_KEY

# 解决 Windows 编码问题
os.environ["PYTHONIOENCODING"] = "utf-8"
os.environ["LC_ALL"] = "zh_CN.UTF-8"

client = OpenAI(api_key=OPENAI_API_KEY)

def generate_topics(text):
    prompt = f"""
基于以下内容，生成5个短视频爆款选题（中文）：
{text}
"""

    res = client.chat.completions.create(
        model="gpt-4o-mini",
        messages=[{"role": "user", "content": prompt}]
    )

    return res.choices[0].message.content.split("\n")