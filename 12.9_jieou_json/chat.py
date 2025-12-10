<<<<<<< HEAD
from api import call_zhipu_api
from roles import get_role_prompt

def chat_once(history, user_input, role_name="小丸子"):
    """进行一次对话交互，返回AI的回复内容"""
    # 将用户输入添加到对话历史
    history.append({"role": "user", "content": user_input})
    
    # 构造API调用消息
    system_message = get_role_prompt(role_name)
    api_messages = [{"role": "system", "content": system_message}] + history[1:]
    
    # 调用API获取AI回复
    result = call_zhipu_api(api_messages)
    assistant_reply = result['choices'][0]['message']['content']
    
    # 将AI回复添加到对话历史
    history.append({"role": "assistant", "content": assistant_reply})
    
=======
from api import call_zhipu_api
from roles import get_role_prompt

def chat_once(history, user_input, role_name="小丸子"):
    """进行一次对话交互，返回AI的回复内容"""
    # 将用户输入添加到对话历史
    history.append({"role": "user", "content": user_input})
    
    # 构造API调用消息
    system_message = get_role_prompt(role_name)
    api_messages = [{"role": "system", "content": system_message}] + history[1:]
    
    # 调用API获取AI回复
    result = call_zhipu_api(api_messages)
    assistant_reply = result['choices'][0]['message']['content']
    
    # 将AI回复添加到对话历史
    history.append({"role": "assistant", "content": assistant_reply})
    
>>>>>>> 69b975d53f968ed81969eadd0d07264386cea57b
    return assistant_reply