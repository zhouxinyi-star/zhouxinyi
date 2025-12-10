import json
import os
from datetime import datetime

# 角色名到记忆文件名的映射（使用当前文件夹内的两个 JSON 文件）
MEMORY_FILES = {
    "小丸子": " XIAOWANZI_memory.json",
    "衍": "YAN_memory.json",
}

def load_memory():
    """从JSON文件加载对话历史，返回对话历史列表"""
    if os.path.exists():
        try:
            with open(MEMORY_FILES, 'r', encoding='utf-8') as f:
                data = json.load(f)
                history = data.get('history', [])
                print(f"✓ 已加载 {len(history)} 条历史对话")
                return history
        except Exception as e:
            print(f"⚠ 加载记忆失败: {e}，将使用新的对话历史")
            return []
    else:
        print("✓ 未找到记忆文件，开始新对话")
        return []

def save_memory(conversation_history, role_system):
    """保存对话历史到JSON文件"""
    try:
        data = {
            "role_system": role_system,
            "history": conversation_history,
            "last_update": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        }
        
        with open(MEMORY_FILES, 'w', encoding='utf-8') as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        
        print(f"✓ 已保存 {len(conversation_history)} 条对话到记忆文件")
    except Exception as e:
        print(f"⚠ 保存记忆失败: {e}")