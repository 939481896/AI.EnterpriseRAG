# app/ai/script_generator.py
import os
from openai import OpenAI
from app.config import OPENAI_API_KEY

# 解决 Windows 编码问题
os.environ["PYTHONIOENCODING"] = "utf-8"
os.environ["LC_ALL"] = "zh_CN.UTF-8"

client = OpenAI(api_key=OPENAI_API_KEY)

def generate_script(topic):
    prompt = f"""
写一个短视频脚本：
主题：{topic}
要求：30-60秒，口语化，有节奏，有钩子
"""

    res = client.chat.completions.create(
        model="gpt-4o-mini",
        messages=[{"role": "user", "content": prompt}]
    )

    return res.choices[0].message.content