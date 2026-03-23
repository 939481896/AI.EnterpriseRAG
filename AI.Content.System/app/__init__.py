# 全局设置编码，解决 Windows ASCII 问题
import os
import sys

# 设置 Python 全局编码为 UTF-8
os.environ["PYTHONIOENCODING"] = "utf-8"
os.environ["LC_ALL"] = "zh_CN.UTF-8"
os.environ["LANG"] = "zh_CN.UTF-8"

# 强制 stdout/stderr 使用 UTF-8
sys.stdout.reconfigure(encoding='utf-8')
sys.stderr.reconfigure(encoding='utf-8')