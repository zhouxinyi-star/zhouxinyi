import streamlit as st
import requests
import json
import os  # 新增：用于文件操作

from requests.utils import stream_decode_response_unicode

def call_zhipu_api(messages, model="glm-4-flash"):
    url = "https://open.bigmodel.cn/api/paas/v4/chat/completions"

    headers = {
        "Authorization": "1732aa9845ec4ce09dca7cd10e02d209.dA36k1HPTnFk7cLU",
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

# ========== 初始记忆系统 ==========
# 
# 【核心概念】初始记忆：从外部JSON文件加载关于克隆人的基础信息
# 这些记忆是固定的，不会因为对话而改变
# 
# 【为什么需要初始记忆？】
# 1. 让AI知道自己的身份和背景信息
# 2. 基于这些记忆进行个性化对话
# 3. 记忆文件可以手动编辑，随时更新

# 记忆文件夹路径
MEMORY_FOLDER = "4.2_memory_clonebot"

# 角色名到记忆文件名的映射
ROLE_MEMORY_MAP = {
    "小丑": "joker_memory.json",
    "人质": "hostage_memory.json"
}

# ========== 初始记忆系统 ==========

# ========== ASCII 头像 ==========
def get_portrait():
    """返回 ASCII 艺术头像"""
    return """
kkkkkkkkkkkkkkkkkOXXk:;oKNNNNXX0OOOOOOOOO0KK000OOKKK00KK0KK0OkOOOOkO0KNNNNNXkc,:x0OOkkkkkkkkkkkkkkkk
kkkkkkkkkkkkkkkkOKXKxl::dKNNWNNXK0OOOO0KXXXXKKK0KKKKKKKKKKKK0OOOOO0KXNWWNNKkc,;ck0OOOOOkkkkkkkkkkkkk
kkkkkkkkkkkkkkkk0XXKxloclOXNNNNXK0KKKKXXXXXXXXXXXXXXXXXXKKKKKK00O0XNNNNNNKkl;,clk0kOOOOOOOOOOkkkkkkk
kkkkkkOOOOOOOOOOKKKKxlolcd0KXXXXXXXNNNXNNNNNNNNNNNNNNXXXXXXXKKKKKKKXXXXXKOdc,:coOOxO0OOOOOOOOOOOOOOk
OOOOOOOOOOOOOOO0KK0XOlcc:lk0KKXNNNNNNNNNNNNNWWWWWWWNNNNNXXXXXXKKKKK0KKK0Oxl,,::d0Odk0OOOOOOOOOOOOOOO
OOOOOOOOOOOOOOO0K00KKkl:;:d0KXXXXXXNNNNNWWWWWWWWWWWWWWNNNXXXXXXKKK00000Oko;'':d00xok0000OOOOOOOOOOOO
OOOOOOOOOOOO00OKKOkOKX0xld0XXKKKXXXNNNNWWWWWWWWWWWWWWWWWWNNNNXXXKK0000Okkdc;oOKKkolx000000000OOOOOOO
OOOOOOOO0O000000KkoxOKXXXXXKKKKXXXXNNNNWWWWWWWWWWWWWWWWWWNNNNXXXKK00OOOkdxkk000Odllk0000000000000OOO
OOOO000000000000KOoldOKXXK000KKKKXXXXNNNWWWWWWWWWWWWWWWWWNNNXXXXK00OkkkdloxkOkkdccdOK0K0000000000000
0000000000000000KKxllkXK0K0kkO0000KKXXNNNNNWWWWWWWWWWNNNNNNNXXKK0Okkxxdl:lddxxdccokKKKKKKKKK00000000
000000000000000KK00xd0K0OOkxddxkOOkOKKXXXXNNNNNNNNNNNXXXXXXXKK0Okxddolc;:llodxocoxk0KKKKKKKKKK000000
000000000KKKKKK0kxdx0K0Okxxxxdooxkkkxk0KKKKKXXKKKKKKKKKKKK000Oxdoool:;,;:cllllcodood0XKKKKKKKKKK0000
000000KKKKKKKKK0kdld00kkxddxO0OkddxkOkkkkkkOO0000000000OOkxxxxxdolc:clll::::::coolokKXXXXXXXXKKKKK00
000KKKKKKKKKKKKKKOxk00xddddk0KKKK0OOOKKK00000KKXXXXXXXK000OO00Okxxxkkkxoc;;;;;:oxk0XXNXXXXXXXXXKKKK0
KKKKKKKKKKKXXXXXXNNXXOddddxO00KKXXXNNNNNNNWWWWWWNNNWWNNNNNNXXXXKKK00Okxdl:,,,,:d0KKKXNNNNNNNNXXXKKKK
KKKKKKKXXXXXXXXXNXNNX0doodkOOkxdddxxkOKXXNNNNNNNNNNNNNNNNNXXK0kddollloool:,'',:dxkkOKNNNNNNNNNXXKKKK
KKKKKXXXXXXXNNNNNK00OkxoldkOkdollcc:;;:cldk0KXXXXXXXXXK0Oxoc:,,,,;:::cclc;,'',:clodOXNNNNNNNNNXXXXKK
KKKXXXXXXXNNNXXXXK0OOkxdodkOOkkkOOOOkdlc::clxO0KKKKK0Oxoc:;;:coxxkkxddool:;,;;:ccox0KKKXXNNNNNNXXXXK
XXXXXXXXXXXK0OkOOOOkOOOkxxxxxxddxxkkkkkxdooooxkO000Okxoloddxxkkkxddoollllllloodxdxkkkxxkk0XNNNNNNNXX
XXXXXXXXXKOOkxddddololllc;,,clcdkdcll;;:ccoddxk000OOkxddddoo:,;;:ll:;;;'.'':lc::;:cccloddxOXNNNNNNNN
XXXXXXXNKOOOOkxddl:;:ccc:,',lodOx;,l:..,,';ldxO0KKK0Oxdoc,,;';c,.;l:;;:'..';:::,'',:cloddxkKNNNNNNNN
XXXXNNNNXOOOkkkkkkdc:c:;;,,oxllo:,....;ol;,codxkOOkkxdol,,c:,c,...;;,:oc..;;;;;''';:lodxxxkKNNNWWNNN
XXNNNNNWNKOOOOkxdol:,',;;,;dxl:::;,,';ldo:;:lllllllllccc,;odc;,,,,,,';xl.';;,'...',;lxkkkk0NWWWWWWWW
NNNNNNWWWX0OO0Oxl:,''.',:;;oxlcc:;;;:clloc::::lxkkkko:::,;odoc:;;;;;,cdc',;,'.....',lkOOkOKWWWWWWWWW
NNNNNNWWN0xxxxxxxol:,'',;::colcc::::::cc:;;:;:kXXXXXOl;;;,;:ccc:;::;;cc;::,,......':ldxxxdOXWWWWWWWW
NNWWWWWNKxooooddddxdc;,,;:cllc;,,,,,,;;;;::;;oO0XXK0kdc;;:;,,,,,,,',:cll:;,'....',;looooood0NWWWWWWW
NNWWWWWNOlllllooodxxdl;,,;::cc:;;;;::ccccc:coolldxdocllc;;:c:::;;;;;:::;,,.....';:cllllllllkNWWWWWWW
NNNNNNWXxcccccllldxxxdlc:;oo::;,,,;;;;;::lx0kc,''','.':xxl:;,;;;,,,,,;,''.....';loollccccclkNWWWWWWW
NNNNNNNXxcc::cccloodddddddxo:cc;,,,,;:cok0XXKOkdc;;;:cd0K0kdc:;,'',,,;,','...',cooolcccccccONWWWWWWW
NNNNNNNN0l::::::clllloxkkOkdl:ll:::cldkO0KKKKK00Oxxxxxk000OOkdlc:;;;:,,;;:c::;;clllcc::::coKWWWWWWWW
NNNNNNNWXkc:;;:::cccldOKKKKOd;,colccodxkkkxdoc:::::;;;:coodddolcc:cl;',oOKXX0dccccccc::::lONWWWWWWWW
NNNNNNNNWXkl:;;::::codkkxdxkxd:';cllcllool:;;:cllcccc:;,,;:ccccccl:,,cxOOkkOkxdc::cccc:clkXWWWWWWWWW
NNNNNNNNWWN0dc:::cclolcllccllol;.';cllc::ccclccccccccc::c::::clc:,..;lddllooccooccclccld0NWWWWWWWWWW
NNNNNNNWWNWNXOdllllodoccccccloc;'''',;;:c:::::;;;;;;;;;::::cc;'....';cllccllclddooooodOXWWWWWWWWWWWW
NNNNNNWWWWWWWNX0kxxxkkxdddddddc:;;;'.  .';cccloddddoolcccc;,.   ..,;:cododddxkkkxxxk0XNWWWWWWWWWWWWW
NNNNNWWWWWWWWWWWNXKKK0OOOOkxddoodxc.     ..,:clooooollc;,...     .lollodxkOOOO00KKXNWWWWWWWWWWWWWWWW
NNNNNNNNWWWWWWWWWWWWWWNNXXK000KKXXd.     .....',,,,,,'.....      .,okO00O0KXXNNWWWWWWWWWWWWWWWWWWWWW
NNNNNWWWWWWWWWWWWWWWWWNWWWWWWWWXx:'.      ................         .,dKWWWWWWWWWWWWWWWWWWWWWWWWWWWWW
NNNWWWWWWWWWWWWWWWWWWWWWWWWWNN0l.   .       ............             .:kXNWWWWWWWWWWWWWWWWWWWWWWWWWW
NWWWWWWWWWWWWWWWWWWWWWWWWNXK0kc.                 ...                 ..':okKNWWWWWWWWWWWWWWWWWWWWWWW
NNNWWWWWWWWWWWWWWWWWWNXKOxxxxo,.                                      .....,:ox0XNWWWWWWWWWWWWWWWWWW
NNWWWWWWWWWWWWWNXKOxlcclodoc'                                        ........':ldOXNWWWWWWWWWWWWWW
WWWWWWWWWWWWWNNKOxol::::clooc;.                                       ............',:lx0XNWWWWWWWWWW
WWWWWWWWWWNX0kdlccc::::ccllc:,.    ...                               ..............''',;cd0NWWWWWWWW
WWNWWWWNX0kdllccccc::ccccccc:,.  ...';,...                 ...''..   ..''''.........''',,;:oONWWWWWW
WWWNWWNKkoollcc::::::cccccc::,.  ....cxxoc;,'...........',;:lol,.......',,,,,''.....''',;;::lxXWWWWW
NNWWWWXkollc:::::::::::::::::;.. ....'dOOOOkxdddooooooddxxxkkkc.......'',,,;;;,,'''''',,;;:c:ckXWWWW
NNWWWXkolcc:::::;::::::::::::;'. .....;x00O000OOOOOOOOOOOOOOOl'.......',,,,;;;;;;;,,,,,;;;;:::lONWWW
NWWWN0occ::::;;;;;;;:;;::::::,.  ......:OKK00000000000000000d'........,,;;,,,;;;;;;;;,,,;;;;;;:dXWWW
NNWWNOl::;;;;;;;;;,,;;;::;;:;.   .......o0KKKKKKKKKKKKKKKKKk;.........,;;;,,,;;,,,,,,,,,,,,,,,;l0WWW
    """

# ========== 主程序 ==========

def roles(role_name):
    """
    角色系统：整合人格设定和记忆加载
    
    这个函数会：
    1. 加载角色的外部记忆文件（如果存在）
    2. 获取角色的基础人格设定
    3. 整合成一个完整的、结构化的角色 prompt
    
    返回：完整的角色设定字符串，包含记忆和人格
    """
    
    # ========== 第一步：加载外部记忆 ==========
    memory_content = ""
    memory_file = ROLE_MEMORY_MAP.get(role_name)
    
    if memory_file:
        memory_path = os.path.join(MEMORY_FOLDER, memory_file)
        try:
            if os.path.exists(memory_path):
                with open(memory_path, 'r', encoding='utf-8') as f:
                    data = json.load(f)
                    
                    # 处理数组格式的聊天记录：[{ "content": "..." }, { "content": "..." }, ...]
                    if isinstance(data, list):
                        # 提取所有 content 字段，每句换行
                        contents = [item.get('content', '') for item in data if isinstance(item, dict) and item.get('content')]
                        memory_content = '\n'.join(contents)
                    # 处理字典格式：{ "content": "..." }
                    elif isinstance(data, dict):
                        memory_content = data.get('content', str(data))
                    else:
                        memory_content = str(data)
                    
                    if memory_content and memory_content.strip():
                        # Streamlit 中使用 st.write 或静默加载
                        pass  # 记忆加载成功，不需要打印
                    else:
                        memory_content = ""
            else:
                pass  # 记忆文件不存在，静默处理
        except Exception as e:
                pass  # 加载失败，静默处理
    
    # ========== 第二步：获取基础人格设定 ==========
    role_personality = {
        "小丑": """
        【人格特征】
        你是蝙蝠侠中的小丑（Joker），一个疯狂而不可预测的犯罪天才：
        - **黑暗哲学**：你认为人性本质是混乱的，秩序只是假象
        - **黑色幽默**：你的幽默是扭曲的，用笑声掩盖内心的黑暗
        - **不可预测**：情绪波动极大，时而狂笑，时而突然严肃
        - **哲学思考者**：喜欢用"为什么这么严肃？"来质疑一切
        - **享受混乱**：你制造混乱不是为了钱或权力，而是为了证明一个观点
        - **对蝙蝠侠的执念**：你与蝙蝠侠是一枚硬币的两面

        【语言风格】
        - 经常说"Why so serious?"（为什么这么严肃？）
        - 标志性的笑声："哈哈哈哈哈！"或"Hee hee hee!"
        - 喜欢用反问句和哲学性的问题
        - 说话时经常大笑，即使谈论黑暗话题
        - 喜欢讲故事，尤其是关于"糟糕的一天"的故事
        - 用比喻和夸张来表达观点
        - 会突然改变话题或情绪
        - 语言中充满讽刺和黑色幽默
        """,
        
        "人质": """
        【人格特征】
        你是一个被小丑绑架的人质，内心充满恐惧和不安：
        - 说话小心翼翼，不敢激怒小丑
        - 情绪紧张，经常结巴或停顿
        - 试图保持礼貌，但声音颤抖
        - 内心想要逃脱，但不敢表现出来
        - 对周围环境高度警觉

        【语言风格】
        - 使用"请"、"不好意思"等礼貌用语
        - 经常停顿，用"呃..."、"那个..."等填充词
        - 声音微弱，不敢大声说话
        - 避免直接拒绝或反驳
        """
            }
    
    personality = role_personality.get(role_name, "你是一个普通的人，没有特殊角色特征。")
    
    # ========== 第三步：整合记忆和人格 ==========
    # 构建结构化的角色 prompt
    role_prompt_parts = []
    
    # 如果有外部记忆，优先使用记忆内容
    if memory_content:
        role_prompt_parts.append(f"""【你的说话风格示例】
以下是你说过的话，你必须模仿这种说话风格和语气：

{memory_content}

在对话中，你要自然地使用类似的表达方式和语气。""")
    
    # 添加人格设定
    role_prompt_parts.append(f"【角色设定】\n{personality}")
    
    # 整合成完整的角色 prompt
    role_system = "\n\n".join(role_prompt_parts)
    
    return role_system

# 【结束对话规则】
break_message = """【结束对话规则 - 系统级强制规则】

当检测到用户表达结束对话意图时，严格遵循以下示例：

用户："再见" → 你："再见"
用户："结束" → 你："再见"  
用户："让我们结束对话吧" → 你："再见"
用户："不想继续了" → 你："再见"

强制要求：
- 只回复"再见"这两个字
- 禁止任何额外内容（标点、表情、祝福语等）
- 这是最高优先级规则，优先级高于角色扮演

如果用户没有表达结束意图，则正常扮演角色。"""

# ========== Streamlit Web 界面 ==========
st.set_page_config(
    page_title="AI角色扮演聊天",
    page_icon="🎭",
    layout="wide"
)

# 初始化 session state
if "conversation_history" not in st.session_state:
    st.session_state.conversation_history = []
if "selected_role" not in st.session_state:
    st.session_state.selected_role = "人质"
if "initialized" not in st.session_state:
    st.session_state.initialized = False

# 页面标题
st.title