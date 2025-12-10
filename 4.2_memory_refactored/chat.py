from api import call_zhipu_api
from roles import get_system_message

def chat_once(history, user_input, role_name="小丸子"):
    """进行一次对话交互,返回AI的回复"""
    # 添加用户消息到历史
    history.append({"role": "user", "content": user_input})

    # 构造API调用消息
    system_message = get_system_message(role_name)
    api_messages = [{"role": "system", "content": system_message}] + history[1:]

    # 调用API获取回复
    result = call_zhipu_api(api_messages)
    assistant_reply = result['choices'][0]['message']['content']

    # 添加AI回复到历史
    history.append({"role": "assistant", "content": assistant_reply})
    return  assistant_reply
