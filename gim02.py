from site import USER_BASE
import requests
import json

def call_zhipu_api(messages, model="glm-4-flash"):
    url = "https://open.bigmodel.cn/api/paas/v4/chat/completions"

    headers = {
        "Authorization": "3c5630491548431d8eb87422cd9bbf81.Wm2BfMEkf6STJARm",
        "Content-Type": "application/json"
    }

    data = {
        "model": model,
        "messages": messages,
        "temperature": 1.0
    }

    response = requests.post(url, headers=headers, json=data)

    if response.status_code == 200:
        return response.json()
    else:
        raise Exception(f"API调用失败: {response.status_code}, {response.text}")
# 使用示例
role_system=["管家老陈","家庭医生李医生","侄女王小妹"]
import random
current_role = random.choice(role_system)
break_message="当我指认了正确的凶手,你只回复我“恭喜你找到了真凶”，不要有其他任何回答。"
# 游戏结绍
print("别墅侦探游戏")
print("富豪张先生在别墅中被杀，有三个嫌疑人:")
while True:  # 表示“当条件为真时一直循环”。由于 True 永远为真，这个循环会一直运行，直到遇到 break 才会停止。
    user_input = input("请输入你要说的问题或指认:")

    messages = [
        {"role": "user", "content": role_system + user_input}
    ]
    result = call_zhipu_api(messages)
    assistant_reply=result['choices'][0]['message']['content']
    print(assistant_reply)
    if"恭喜你找到了真凶" in assistant_reply:
        break