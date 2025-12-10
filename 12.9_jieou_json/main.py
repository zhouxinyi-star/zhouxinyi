<<<<<<< HEAD
from memory import load_memory, save_memory
from roles import get_role_prompt
from logic import should_exit_by_user, should_exit_by_ai
from chat import chat_once

# 全局配置
ROLE_NAME = "小丸子"

def main():
    """主程序入口：初始化对话历史，运行主循环，保存记忆"""
    # 初始化角色设定
    role_system = get_role_prompt(ROLE_NAME)
    # system_message = get_system_message (ROLE_NAME)
    
    # 加载历史记忆
    conversation_history = load_memory()
    
    # 如果记忆为空，初始化对话历史
    if not conversation_history:
        conversation_history = [
            {"role": "system", "content": role_system}
        ]
        print("✓ 初始化新对话")
    
    try:
        while True:
            # 获取用户输入
            user_input = input("\n请输入你要说的话（输入\"再见\"退出）：")
            
            # 检查是否结束对话
            if should_exit_by_user(user_input):
                print("对话结束，记忆已保存")
                break
            
            # 进行一次对话交互
            assistant_reply = chat_once(conversation_history, user_input, ROLE_NAME)
            
            # 显示AI回复
            print(assistant_reply)
            
            # 保存记忆到文件
            save_memory(conversation_history, role_system)
            
            # 检查AI回复是否表示结束
            if should_exit_by_ai(assistant_reply):
                print("\n对话结束，记忆已保存")
                break
    
    except KeyboardInterrupt:
        # 用户按 Ctrl+C 中断程序
        print("\n\n程序被用户中断，正在保存记忆...")
        save_memory(conversation_history, role_system)
        print("✓ 记忆已保存")
    except Exception as e:
        # 其他异常（API调用失败、网络错误等）
        print(f"\n\n发生错误: {e}")
        print("正在保存记忆...")
        save_memory(conversation_history, role_system)
        print("✓ 记忆已保存")

if __name__ == "__main__":
=======
from memory import load_memory, save_memory
from roles import get_role_prompt
from logic import should_exit_by_user, should_exit_by_ai
from chat import chat_once

# 全局配置
ROLE_NAME = "小丸子"

def main():
    """主程序入口：初始化对话历史，运行主循环，保存记忆"""
    # 初始化角色设定
    role_system = get_role_prompt(ROLE_NAME)
    # system_message = get_system_message (ROLE_NAME)
    
    # 加载历史记忆
    conversation_history = load_memory()
    
    # 如果记忆为空，初始化对话历史
    if not conversation_history:
        conversation_history = [
            {"role": "system", "content": role_system}
        ]
        print("✓ 初始化新对话")
    
    try:
        while True:
            # 获取用户输入
            user_input = input("\n请输入你要说的话（输入\"再见\"退出）：")
            
            # 检查是否结束对话
            if should_exit_by_user(user_input):
                print("对话结束，记忆已保存")
                break
            
            # 进行一次对话交互
            assistant_reply = chat_once(conversation_history, user_input, ROLE_NAME)
            
            # 显示AI回复
            print(assistant_reply)
            
            # 保存记忆到文件
            save_memory(conversation_history, role_system)
            
            # 检查AI回复是否表示结束
            if should_exit_by_ai(assistant_reply):
                print("\n对话结束，记忆已保存")
                break
    
    except KeyboardInterrupt:
        # 用户按 Ctrl+C 中断程序
        print("\n\n程序被用户中断，正在保存记忆...")
        save_memory(conversation_history, role_system)
        print("✓ 记忆已保存")
    except Exception as e:
        # 其他异常（API调用失败、网络错误等）
        print(f"\n\n发生错误: {e}")
        print("正在保存记忆...")
        save_memory(conversation_history, role_system)
        print("✓ 记忆已保存")

if __name__ == "__main__":
>>>>>>> 69b975d53f968ed81969eadd0d07264386cea57b
    main()