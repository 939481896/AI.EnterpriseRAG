import httpx
from app.core.config import settings

def test_proxy():
    # 1. 准备代理配置
    proxies = None
    if settings.PROXY_ENABLE and settings.PROXY_URL:
        proxies = settings.PROXY_URL
        print(f"检测到配置代理: {proxies}")
    else:
        print("未检测到代理配置，将尝试直连...")

    try:
        # 2. 使用 Client 模式发起请求
        with httpx.Client(proxy=proxies, timeout=10.0) as client:
            # 访问一个返回当前公网 IP 的 API
            response = client.get("https://httpbin.org/ip")
            ip = response.json().get("origin")
            print(f"✅ 访问成功！当前 Python 环境识别的出口 IP 为: {ip}")
            
            # 3. 额外测试下能不能连上 Google (Gemini 必要条件)
            google_check = client.get("https://www.google.com", timeout=5.0)
            if google_check.status_code == 200:
                print("✅ 代理已打通，可以正常访问 Google/Gemini API")
                
    except Exception as e:
        print(f"❌ 访问失败: {e}")
        print("\n💡 调试建议:")
        print(f"1. 检查 Clash 是否开启？(当前配置: {settings.PROXY_URL})")
        print("2. 检查 Clash 的端口是否真的是 7890 (有些版本是 7897 或 1080)")
        print("3. 确保 .env 文件中的 PROXY_ENABLE=True")

if __name__ == "__main__":
    test_proxy()