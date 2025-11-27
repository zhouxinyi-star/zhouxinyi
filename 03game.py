import requests
import json
import random

from requests.utils import stream_decode_response_unicode
from xunfei_tts import text_to_speech

def call_zhipu_api(messages, model="glm-4-flash"):
    url = "https://open.bigmodel.cn/api/paas/v4/chat/completions"

    headers = {
        "Authorization":"ef23336ea2d74c9ba17039447366b645.goL3aYZUzEGp9wMT",
        "Content-Type": "application/json"
    }

    data = {
        "model": model,
        "messages": messages,
        "temperature": 0.5   
    }

    response = requests.post(url, headers=headers, json=data)

    if response.status_code == 200:
        return response.json()
    else:
        raise Exception(f"API调用失败: {response.status_code}, {response.text}")

# 游戏设置
role_system = ["真凶","目击者"]
current_role = random.choice(role_system)

# 系统提示词
game_system = f"""游戏背景故事
在一个风雨交加的夜晚，富豪李先生在他的书房中被发现身亡。现场留有血迹、一个破碎的花瓶和一张神秘的字条。
你是本案的关键人物：{current_role}

游戏规则：
1. 用户会通过提问来猜测你的身份
2. 你通过描述案发时的行动、动机、与受害者的关系来暗示身份，但不能直接说出"{current_role}"这个词
3. 必须在前3个回答中给出关键线索
4. 回答要富有戏剧性，营造悬疑氛围,每个回答2-3句话
5. 不要提及其他可能的身份选项
6. 当用户准确说出"{current_role}"这个词时，你只回复"案件已破"来结束游戏
7. 保持神秘感，让游戏有趣

角色设定
- 如果是"真凶"：暗示有作案动机、了解案发现场细节、言辞闪烁
- 如果是"目击者"：强调看到了可疑情况、提供线索、表现出恐惧

回答示例
- 玩家问"案发时你在哪里？"
  真凶回答："我当时在书房附近...有些细节我不太方便说"
  目击者回答："我听到书房有争吵声，透过门缝看到一个人影"
- 玩家问"你和李先生关系如何？"
  真凶回答："我们之间有些...经济纠纷，他欠我不少钱"
  目击者回答："我只是个普通的佣人，偶尔会给他送茶"

现在开始这场悬疑调查!在前3轮给出足够线索,让侦探在5轮内破案。"""

# 维护对话历史
conversation_history = [
    {"role": "system", "content": game_system}
]

# 显示游戏开始信息
print("=" * 60)
print("游戏开始！")
print(f"你的身份是：{current_role}")
print("=" * 60)
print("\n故事背景:")
print("在一个风雨交加的夜晚，富豪李先生被发现死在自己的书房中。")
print("现场有血迹、破碎的花瓶和一张神秘字条。")
print("现在，你将审问本案的关键人物。")
print(f"\n你的身份:侦探|对方身份:{current_role}(需要你调查出来)")
print(game_system)
print("=" * 60)
print("\n开始游戏,请输入你的问题...\n")

# 多轮对话循环
while True:
    user_input = input("请输入你要说的话：")
    
    # 添加用户消息到历史
    conversation_history.append({"role": "user", "content": user_input})
    
    # 调用API
    result = call_zhipu_api(conversation_history)
    assistant_reply = result['choices'][0]['message']['content']
    
    
    # 添加助手回复到历史（使用原始文本，不是函数返回值）
    conversation_history.append({"role": "assistant", "content": assistant_reply})
    
    # 打印回复
    print(assistant_reply)
    text_to_speech(assistant_reply)
    
    # 检查是否猜对（模型回复"再见"）
    if "再见" in assistant_reply:
        print(f"\n游戏结束!正确答案是:{current_role}")
    # 根据身份给出不同结局
        if "真凶" in current_role:
            print(" 真凶已被锁定！正义得到伸张！")
        else:
            print(" 目击者的证词为破案提供了关键线索！")
        break