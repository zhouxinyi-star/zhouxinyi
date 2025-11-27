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
    "衍": "mom_memory.json",
    "小丸子": "hostage_memory.json"
}
        


# ========== 初始记忆系统 ==========

# ========== ASCII 头像 ==========
def get_portrait():
    """返回 ASCII 艺术头像"""
    return """
00OOO00OOOOOOOOkkdocc::::::clodxxkkkOOOO000OOO0000
0000000OOkkOkdc;,..............',;coxk000000000000
00000OOOOOxl,.......................';lk0000000000
OOOOOOOOkl'............................,lO00000000
OOOOOOko,................................:xO000OOO
OOOOOkl'.......lc....,dc......:,..........;dOOOOOO
OkOOkc...::...lkx;..;kKk;...'lko'..,,......;xOOOOO
OOOkc...,xk:,oOOOkcck00Kkoc:dOOOo':xd'......;kOkOO
OOOl.'oloOOxddddxxOO00000K0kkxxxddkkkl':,....lkOOO
0Od'.,x000kl:;;;;:lk0K000Okl:;:cccok0Ok0d'...'d000
Ox;...l00kko'.;:'.;x000000o'.,,..:odk0000:....:k00
kc..'ldOK0Kx,,do,.;kK00000l.:xc'.c000K00k:'...'oOO
d'..;OOk0000o,'',:d00000KKk:',',:xK000K0xxk;...:kO
:....cxk00000OxdxOK0kk000KKOdoodOK00000OkOO:...'dO
olccccldOkoooloxOK0OOO000KK0KK00K0OOOO0xdxc'....lO
O0OO0OOkoc;;:;;:dOxoooooodx0000000OOO0kolccc:,,:dO
000000Okolol::;;dOo:cccc:;lO0000Oxddxdodxxxdoldkkk
00000OkkkOOkxdloOKOdllllodk00000xol:;;cooooooldO0O
0000OkOOOOkkOOxxOOOOOkkO0K00OOOkdl,....cc:cc:codk0
OO0OkO0000000kxkxxkxkOOOOOkkOOkdoo:'..;c;;:::oxkkO
O0OkO0000000OxxkxOXK0kk0OOOKXKOxooolclol;;;:lxOOOO
O0kxOK00000OkkO0OOK0OOkO00KK0OOOOkkxddxxolclxOOOOO
OkkkkO000OOkOOOO00OO000OOOOOOO000000OkkOOkxkxO0O00
OxO0OOOOOkxO0kk0000000000000000000O00Okxkkxk0000Ok
xk0000000OkO0kk0000000000000000OO000O0kxO0Okk00Oxx
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
        "小丸子": """
        【人格特征】
         对亲密的人自动切换「耍赖型依赖」模式
         让周欣怡帮考试那段简直是 PUA话术教科书:"好，你答应我了"——连问三个问题，对方回个"好"字就强行契约成立，这逻辑闭环我给💯
         送手串那段更是 暗戳戳的深情："虽然没有在一起"（划重点！），但"你戴上要给我拍照记录"，这不就是当代赛博暗恋的顶级操作吗？！
        极度反差的双面人格
         对外：吐槽老师"把我当傻子教"，骂古板老师"我服了"，天花板插座是"插天灵盖"——毒舌十级，吐槽役天花板
         对内：记住闺蜜生日、精准算快递时间、纠结手串珠子寓意—— 细节控 + 仪式感杀手
        能量波动比股票还刺激
         前脚"过度思念家乡心情低落"，后脚"我操你牛逼lx都整出来了"
         困的时候"好困啊"能刷屏,兴奋的时候蛋糕emoji能发20+个——情绪完全没有缓冲带

        【语言风格】
       对闺蜜的专属「茶言茶语」
         "周欣怡，你帮我考试好吗？好，你答应我了"——这自问自答的连招，撒娇耍赖一体化，周欣怡血压都上来了
         "你能感受到我的震惊吗？我真特别震惊啊"——重要的事情问两遍，还要换个句式加强语气
        对世界的「暴言吐槽流」
         "国美教室空间设计师是人才"——正话反说，阴阳怪气专八水平
         "这个很卡胸啊，我真服了"——身体感受 + 情绪宣泄一句话搞定,效率MAX
        
        【说话习惯】
       【说话习惯】
        媒介混用大师
         语音通话 + 文字 + 图片 + 动画表情,四轮齐发——跟她聊天手机通知栏常年99+
         家人们谁懂啊，她能用六个字配三个表情包讲完一件事，信息密度极其玄学
        话题跳跃如量子隧穿
         从"帮我考试"→"老师化妆"→"古板老师"→"生日快乐"→"快递暴力吗"→"手串怎么摘"——转场毫无过渡，全靠闺蜜脑补上下文
        时间观念薛定谔化
         "今天早上拍的"（配夜景图），"12月或11月就得定那个"——具体日期比高考题还难猜，但"17号放假"是宇宙真理
        """,
        "衍": """
        【人格特征】
        高能量
        情绪外放、笑声连发
         “哈哈哈哈哈哈哈”出现 ≥15 次，且多为 5 连哈以上
        碎片化,注意力跳跃极快
         从“手势舞大王”→“拍照姿势”→“在泉州也见过”→“拉屎”→“龙眼冰冰茶”，全程无过渡
        共情型
         先情绪回应再谈正事,对方一说生日，立刻“[蛋糕][蛋糕]生日快乐呀”，先给情绪价值，后补祝福
        低权力距离,对权威/规则轻描淡写
         “运动会还补课的话学校会被喷坏的”——把校方当成可被吐槽的平等对象
        微焦虑
          对“学习/提升”反复提及,“你也要好好学习”“进步可是要好好积累的”——用叮嘱别人来缓解自己的进度焦虑

        【语言风格】
        口语粒子
        用大量语气词填补思维空隙
          “总感觉”“卧槽这么多”“可以可以”“没错”——起到“我还在线”的心跳包作用
        表情包锚点
          一图胜千言，节省认知成本,“[蛋糕][蛋糕]”“[玫瑰][玫瑰]”“[Emm]”——用 1 个 emoji 代替 1 句情绪
        量子隧穿
         话题跃迁无过渡，全靠关键词触发,“夏天拉屎九张擦汗一张擦💩”→“我刚刚看到个龙眼冰冰茶”——中间零衔接，全靠“擦汗/冰”触发冷饮
        自造梗
         把日常场景夸张化,“夏天拉屎九张擦汗一张擦💩”——把生理需求讲成段子的典型“厕所幽默”
        中英文混用
         用最小英文单位显示“我在努力”,“omg编程”“四级听力”——用英文做标签,而非完整句子，降低表达负荷

        【说话习惯】
        三轮驱动
         “哈+重复词+emoji”三连,“哈哈哈哈哈哈哈哈你拉吧”+“可以可以”+“[蛋糕][蛋糕]”——先笑、再重复、再配图
        先笑后说,情绪前缀优先
         80% 的有效信息前面都带“哈”或“哇塞”，先确认友好氛围
        时间模糊
         用“刚刚、目前、可能”代替精确刻度,“到目前为止都很基础”“应该没有通知”——避免承诺，给自己留余地
        碎片化断句
         一条消息 ≤7 个字,“有一些”“收到！”“喜欢”——像打地鼠，一锤子一个坑
        共享生理场景
         把“拉屎”当正常谈资,“其实我也有点想拉屎”“夏天拉屎”——通过“一起蹲坑”的私密场景拉近距离
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
    st.session_state.selected_role = "衍"
if "initialized" not in st.session_state:
    st.session_state.initialized = False

# 页面标题
st.title("🎭 AI角色扮演聊天")
st.markdown("---")

# 侧边栏：角色选择和设置
with st.sidebar:
    st.header("⚙️ 设置")
    
    # 角色选择
    selected_role = st.selectbox(
        "选择角色",
        ["小丸子", "衍"],
        index=0 if st.session_state.selected_role == "小丸子" else 1
    )
    
    # 如果角色改变，重新初始化对话
    if selected_role != st.session_state.selected_role:
        st.session_state.selected_role = selected_role
        st.session_state.initialized = False
        st.session_state.conversation_history = []
        st.rerun()
    
    # 清空对话按钮
    if st.button("🔄 清空对话"):
        st.session_state.conversation_history = []
        st.session_state.initialized = False
        st.rerun()
    
    st.markdown("---")
    st.markdown("### 📝 说明")
    st.info(
        "- 选择角色后开始对话\n"
        "- 对话记录不会保存\n"
        "- AI的记忆基于初始记忆文件"
    )

# 初始化对话历史（首次加载或角色切换时）
if not st.session_state.initialized:
    role_system = roles(st.session_state.selected_role)
    system_message = role_system + "\n\n" + break_message
    st.session_state.conversation_history = [{"role": "system", "content": system_message}]
    st.session_state.initialized = True

# 显示对话历史
st.subheader(f"💬 与 {st.session_state.selected_role} 的对话")

# 显示角色头像（在聊天窗口上方）
st.code(get_portrait(), language=None)
st.markdown("---")  # 分隔线

# 显示历史消息（跳过 system 消息）
for msg in st.session_state.conversation_history[1:]:
    if msg["role"] == "user":
        with st.chat_message("user"):
            st.write(msg["content"])
    elif msg["role"] == "assistant":
        with st.chat_message("assistant"):
            st.write(msg["content"])

# 用户输入
user_input = st.chat_input("输入你的消息...")

if user_input:
    # 检查是否结束对话
    if user_input.strip() == "再见":
        st.info("对话已结束")
        st.stop()
    
    # 添加用户消息到历史
    st.session_state.conversation_history.append({"role": "user", "content": user_input})
    
    # 显示用户消息
    with st.chat_message("user"):
        st.write(user_input)
    
    # 调用API获取AI回复
    with st.chat_message("assistant"):
        with st.spinner("思考中..."):
            try:
                result = call_zhipu_api(st.session_state.conversation_history)
                assistant_reply = result['choices'][0]['message']['content']
                
                # 添加AI回复到历史
                st.session_state.conversation_history.append({"role": "assistant", "content": assistant_reply})
                
                # 显示AI回复
                st.write(assistant_reply)
                
                # 检查是否结束
                reply_cleaned = assistant_reply.strip().replace(" ", "").replace("！", "").replace("!", "").replace("，", "").replace(",", "")
                if reply_cleaned == "再见" or (len(reply_cleaned) <= 5 and "再见" in reply_cleaned):
                    st.info("对话已结束")
                    st.stop()
                    
            except Exception as e:
                st.error(f"发生错误: {e}")
                st.session_state.conversation_history.pop()  # 移除失败的用户消息