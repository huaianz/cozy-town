# -*- coding: utf-8 -*-
"""生成《田舍小镇》剧情与系统设计文档 .docx"""
from docx import Document
from docx.shared import Pt, Cm, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_LINE_SPACING
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_ALIGN_VERTICAL
from docx.oxml.ns import qn
from docx.oxml import OxmlElement

HEI = "黑体"
SONG = "宋体"
KAI = "楷体"
WEST = "Arial"
BLACK = RGBColor(0, 0, 0)
HEAD_FILL = "D9D9D9"
ZEBRA_FILL = "F2F2F2"

doc = Document()

# ---------- 页面 ----------
sec = doc.sections[0]
sec.page_width, sec.page_height = Cm(21.0), Cm(29.7)
sec.top_margin = sec.bottom_margin = sec.left_margin = sec.right_margin = Cm(2.5)

# ---------- 通用字体工具 ----------
def set_run_font(run, cn=SONG, size=12, bold=False, color=BLACK, west=WEST):
    run.font.name = west
    run.font.size = Pt(size)
    run.font.bold = bold
    run.font.color.rgb = color
    rpr = run._element.get_or_add_rPr()
    rfonts = rpr.find(qn('w:rFonts'))
    if rfonts is None:
        rfonts = OxmlElement('w:rFonts'); rpr.append(rfonts)
    rfonts.set(qn('w:ascii'), west)
    rfonts.set(qn('w:hAnsi'), west)
    rfonts.set(qn('w:eastAsia'), cn)

def _xml_element(obj):
    return obj._p if hasattr(obj, '_p') else obj.element

def set_first_line_chars(par, chars=200):
    ppr = _xml_element(par).get_or_add_pPr()
    ind = ppr.find(qn('w:ind'))
    if ind is None:
        ind = OxmlElement('w:ind'); ppr.append(ind)
    ind.set(qn('w:firstLineChars'), str(chars))
    ind.set(qn('w:firstLine'), '480' if chars == 200 else '0')

def clear_indent(par):
    ppr = _xml_element(par).get_or_add_pPr()
    ind = ppr.find(qn('w:ind'))
    if ind is not None:
        ppr.remove(ind)

# ---------- 配置内置样式 ----------
def config_style(name, cn, size, bold=False, align=None, before=0, after=0,
                 line=1.5, firstline=False, color=BLACK):
    st = doc.styles[name]
    st.font.name = WEST; st.font.size = Pt(size); st.font.bold = bold
    st.font.color.rgb = color
    rpr = st.element.get_or_add_rPr()
    rfonts = rpr.find(qn('w:rFonts'))
    if rfonts is None:
        rfonts = OxmlElement('w:rFonts'); rpr.append(rfonts)
    rfonts.set(qn('w:ascii'), WEST); rfonts.set(qn('w:hAnsi'), WEST)
    rfonts.set(qn('w:eastAsia'), cn)
    pf = st.paragraph_format
    pf.space_before = Pt(before); pf.space_after = Pt(after)
    if line:
        pf.line_spacing = line
    if align is not None:
        pf.alignment = align
    if firstline:
        set_first_line_chars(st, 200)

config_style('Normal', SONG, 12, line=1.5, firstline=True,
             align=WD_ALIGN_PARAGRAPH.JUSTIFY)
config_style('Title', HEI, 18, bold=True, align=WD_ALIGN_PARAGRAPH.CENTER,
             before=0, after=18, line=1.0)

# 去除 Title 样式自带的蓝色下边框
def remove_title_border():
    ppr = doc.styles['Title'].element.get_or_add_pPr()
    old = ppr.find(qn('w:pBdr'))
    if old is not None:
        ppr.remove(old)
    pbdr = OxmlElement('w:pBdr')
    bottom = OxmlElement('w:bottom'); bottom.set(qn('w:val'), 'nil')
    pbdr.append(bottom); ppr.append(pbdr)
remove_title_border()
config_style('Heading 1', HEI, 16, bold=True, before=14, after=6, line=1.0)
config_style('Heading 2', HEI, 14, bold=True, before=11, after=5, line=1.0)
config_style('Heading 3', HEI, 12, bold=True, before=9, after=4, line=1.0)

# ---------- 段落辅助 ----------
def para(text="", style=None, size=12, cn=SONG, bold=False, align=None,
         firstline=True, after=None, before=None, color=BLACK, line=1.5):
    p = doc.add_paragraph(style=style)
    if text:
        r = p.add_run(text)
        set_run_font(r, cn=cn, size=size, bold=bold, color=color)
    pf = p.paragraph_format
    pf.line_spacing = line
    if align is not None:
        pf.alignment = align
    if after is not None:
        pf.space_after = Pt(after)
    if before is not None:
        pf.space_before = Pt(before)
    if style and style.startswith('Heading'):
        pf.alignment = WD_ALIGN_PARAGRAPH.LEFT
        clear_indent(p)
    elif style == 'Title':
        clear_indent(p)
    elif firstline:
        set_first_line_chars(p, 200)
    else:
        clear_indent(p)
    return p

def h1(t): return para(t, style='Heading 1')
def h2(t): return para(t, style='Heading 2')
def h3(t): return para(t, style='Heading 3')

def body(t, **kw):
    kw.setdefault('firstline', True)
    return para(t, **kw)

def bullet(t, size=12):
    p = doc.add_paragraph(style='List Bullet')
    r = p.add_run(t); set_run_font(r, cn=SONG, size=size)
    p.paragraph_format.line_spacing = 1.5
    clear_indent(p)
    return p

# ---------- 表格辅助 ----------
def shade_cell(cell, fill):
    tcpr = cell._tc.get_or_add_tcPr()
    shd = OxmlElement('w:shd')
    shd.set(qn('w:val'), 'clear'); shd.set(qn('w:color'), 'auto')
    shd.set(qn('w:fill'), fill)
    tcpr.append(shd)

def set_cell_text(cell, text, size=10.5, bold=False, cn=SONG,
                  align=WD_ALIGN_PARAGRAPH.LEFT):
    cell.text = ""
    p = cell.paragraphs[0]
    p.alignment = align
    p.paragraph_format.line_spacing = 1.0
    p.paragraph_format.space_before = Pt(2); p.paragraph_format.space_after = Pt(2)
    clear_indent(p)
    r = p.add_run(str(text)); set_run_font(r, cn=cn, size=size, bold=bold)
    cell.vertical_alignment = WD_ALIGN_VERTICAL.CENTER

def make_table(headers, rows, widths=None, zebra=False):
    t = doc.add_table(rows=1, cols=len(headers))
    t.style = 'Table Grid'
    t.alignment = WD_TABLE_ALIGNMENT.CENTER
    t.autofit = True
    hdr = t.rows[0].cells
    for i, htext in enumerate(headers):
        set_cell_text(hdr[i], htext, size=10.5, bold=True,
                      align=WD_ALIGN_PARAGRAPH.CENTER)
        shade_cell(hdr[i], HEAD_FILL)
    # 表头重复
    trpr = t.rows[0]._tr.get_or_add_trPr()
    th = OxmlElement('w:tblHeader'); th.set(qn('w:val'), 'true'); trpr.append(th)
    for ri, row in enumerate(rows):
        cells = t.add_row().cells
        for ci, val in enumerate(row):
            al = WD_ALIGN_PARAGRAPH.CENTER if len(str(val)) <= 12 \
                else WD_ALIGN_PARAGRAPH.LEFT
            set_cell_text(cells[ci], val, size=10.5, align=al)
            if zebra and ri % 2 == 1:
                shade_cell(cells[ci], ZEBRA_FILL)
    if widths:
        for ci, w in enumerate(widths):
            for row in t.rows:
                row.cells[ci].width = Cm(w)
    return t

def caption(t):
    p = para(t, size=10.5, cn=SONG, align=WD_ALIGN_PARAGRAPH.CENTER,
             firstline=False, before=4, after=4, line=1.0)
    return p

def page_break():
    doc.add_page_break()

# ---------- 目录域 ----------
def add_toc():
    p = doc.add_paragraph()
    clear_indent(p)
    run = p.add_run()
    fb = OxmlElement('w:fldChar'); fb.set(qn('w:fldCharType'), 'begin')
    it = OxmlElement('w:instrText'); it.set(qn('xml:space'), 'preserve')
    it.text = 'TOC \\o "1-2" \\h \\z \\u'
    fs = OxmlElement('w:fldChar'); fs.set(qn('w:fldCharType'), 'separate')
    ft = OxmlElement('w:t'); ft.text = '右键此处选择“更新域”以生成目录。'
    fe = OxmlElement('w:fldChar'); fe.set(qn('w:fldCharType'), 'end')
    run._r.append(fb); run._r.append(it); run._r.append(fs)
    run._r.append(ft); run._r.append(fe)

def enable_update_fields():
    settings = doc.settings.element
    uf = OxmlElement('w:updateFields'); uf.set(qn('w:val'), 'true')
    settings.append(uf)

# ============================================================
# 封面
# ============================================================
for _ in range(4):
    para("", firstline=False, line=1.0)
para("田舍小镇", style='Title', size=26)
para("剧情与系统设计文档", style='Title', size=20)
para("——主线故事「归乡」与赎地系统设计", cn=KAI, size=14,
     align=WD_ALIGN_PARAGRAPH.CENTER, firstline=False, line=1.0, after=36)
for _ in range(6):
    para("", firstline=False, line=1.0)
info = [
    ("项目名称", "田舍小镇（Cozy Town）"),
    ("文档类型", "剧情与系统设计文档（GDD）"),
    ("文档版本", "V1.0（草案）"),
    ("编写日期", "2026-09-23"),
    ("文档状态", "待评审 · 数值待校准"),
]
ti = make_table(["项目", "内容"], info, widths=[4.5, 9.0])
page_break()

# 目录
para("目  录", style='Heading 1', align=WD_ALIGN_PARAGRAPH.CENTER)
add_toc()
enable_update_fields()
page_break()

# ============================================================
# 1 文档概述
# ============================================================
h1("1 文档概述")
h2("1.1 项目背景")
body("《田舍小镇》是一款 2D 俯视角田园生活模拟游戏，采用 Unity 引擎与 URP 渲染管线开发，"
     "面向 PC 与移动端（WebGL / 小游戏）平台。游戏目前已具备玩家移动与体力、五种工具、"
     "作物种植、钓鱼、挖矿、果树、池塘、区域切换、商店买卖、烹饪、委托订单、赶集日、"
     "昼夜与天气、工具耐久与升级、背包、图鉴以及存档等较为完整的经营玩法系统。")
body("现阶段游戏的主要不足在于：各玩法系统相对独立，缺少一条贯穿始终的主线目标与情感驱动，"
     "玩家在熟悉玩法后容易陷入“漫无目的刷钱”的状态。为此，本设计引入一条以“继承老宅、"
     "分期赎地”为核心的主线故事，将现有玩法系统串联为有目标、有节奏、有情感落点的完整体验。")
h2("1.2 设计目标")
bullet("目标驱动：以分期“赎地费”为中期目标，为玩家提供清晰的时间压力与成长方向。")
bullet("系统串联：让种地、钓鱼、挖矿、委托、赶集等所有赚钱手段都服务于主线，提升各系统的存在感与价值。")
bullet("情感共鸣：通过外婆的回忆信件，讲述“土地、邻里与回家”的主题，形成温暖治愈的情感体验。")
bullet("低挫败：坚持休闲治愈定位，到期未还清不判定游戏失败，而以宽限与鼓励代替惩罚。")
bullet("可扩展：还清债务后开放自由经营“后日谈”，为后续新区域、新 NPC 剧情预留接口。")
h2("1.3 文档范围与读者")
body("本文档涵盖主线故事设定、人物设定、三幕剧情结构、赎地（债务）系统规则、开场 Intro 流程、"
     "地契面板 UI、存档字段、与现有系统的集成方案、新建与修改脚本清单、数值平衡说明及开发里程碑。"
     "读者为游戏的策划、程序与美术成员，可作为后续开发与评审的依据。")
body("需特别说明：文档中出现的还款金额、截止天数等均为暂定设计值，需在结合游戏内作物真实售价、"
     "单日产能与各系统收益实测后进行校准；凡未定数值均已在文中明确标注，不作为最终数值。")

# ============================================================
# 2 世界观与故事设定
# ============================================================
h1("2 世界观与故事设定")
h2("2.1 世界观")
body("田舍小镇是一座依山傍海、节奏缓慢的乡间小镇。镇上居民彼此熟识、互相帮衬，保留着定期赶集、"
     "依四季节气耕作的传统。小镇外围分布着农田、池塘、河流、果林与矿洞，构成了玩家主要的生产与生活空间。"
     "随着青壮年外出，小镇一度冷清，而主角的到来将让这片土地重新热闹起来。")
h2("2.2 主角设定")
body("主角是在城市长大的年轻人，童年时每逢暑假都会到田舍小镇的外婆家生活，对这片田与小镇怀有模糊而温暖的记忆。"
     "长大后主角忙于城市生活，与小镇渐渐断了联系。故事开始时，主角因一个意外的寄件，重新踏上了归途。"
     "主角的姓名与外观可由玩家在开始游戏前自定义，剧情文本以第二人称或中性称谓呈现，避免与玩家设定冲突。")
h2("2.3 故事缘起")
body("某天，主角收到一个从田舍小镇寄来的旧木盒，里面装着一把生锈的铜钥匙、一张泛黄的地契照片，"
     "以及外婆生前留下的一封信。信中写道，老宅和那片田一直为你留着。主角于是辞去城市的工作，"
     "提着一只旧皮箱，回到了阔别多年的田舍小镇。")
h2("2.4 核心矛盾：赎地约定")
body("外婆晚年为修缮老宅、帮衬邻里，曾向镇公所欠下一笔“土地承包修缮费”，地契也因此一直抵押在镇上。"
     "和善的老镇长是外婆的旧友，不忍将土地收归公有。主角到来后，老镇长立下约定：只要在宽限期内"
     "分三期把费用补齐，地契便正式归主角所有；这块田，也才算真正“回家”。这笔分期债务，构成了游戏"
     "第一年的核心目标与主线张力。")

# ============================================================
# 3 人物设定
# ============================================================
h1("3 人物设定")
h2("3.1 人物总览")
caption("表 3-1  田舍小镇主要人物一览")
make_table(
    ["角色", "建议对应素材", "身份 / 职能", "在主线中的作用"],
    [
        ["老镇长", "Old Man", "镇公所负责人", "外婆旧友，主线引路人，主持分期赎地与归还地契"],
        ["杂货铺老板娘", "Girl01", "种子杂货铺", "出售种子，讲述外婆生前旧事"],
        ["铁匠大叔", "Boy / M", "铁匠铺", "工具升级与挖矿线引导者"],
        ["渔夫", "Girl02", "河畔渔夫", "钓鱼线引导者"],
        ["牧场女孩", "Girl03", "牧场主", "动物与养殖线伙伴"],
        ["其他居民", "Girl04–Girl07", "镇民 / 摊主", "委托来源与赶集日互动对象"],
        ["外婆", "Intro / 信件（不出场）", "老宅与田的原主人", "以回忆信件贯穿全篇，是情感核心"],
    ],
    widths=[2.6, 3.0, 3.2, 5.2], zebra=True)
h2("3.2 外婆：回忆核心")
body("外婆在故事开始时已故，并不直接登场，而是通过三封回忆信、邻居的讲述以及老宅中遗留的物件被逐渐拼塑出来。"
     "她一生守着这片田，勤恳、宽厚，与邻里相互帮衬，晚年甚至为帮小镇渡过难关而抵押了地契。"
     "外婆的形象承担着游戏的情感重量，也是三幕剧情在“土地—邻里—回家”主题上的落点。")

# ============================================================
# 4 主线剧情结构
# ============================================================
h1("4 主线剧情结构")
h2("4.1 结构总览")
caption("表 4-1  三幕主线结构一览（天数与金额为暂定设计值）")
make_table(
    ["幕次", "季节 / 时间", "主题", "核心玩法", "还款金额（暂定）"],
    [
        ["第一幕", "春季 · 约第 28 天", "荒地", "教学：耕种与卖货", "500 G"],
        ["第二幕", "夏季 · 约第 56 天", "生计", "钓鱼、挖矿、委托、果树", "2,000 G"],
        ["第三幕", "秋冬季 · 约第 84 天 / 第一年末", "归乡", "高级作物、烹饪、赶集冲刺", "5,000 G"],
    ],
    widths=[1.8, 4.0, 1.6, 4.2, 2.4])
h2("4.2 第一幕 · 春 ·「荒地」")
body("开场图文 Intro 之后，主角抵达老宅，老镇长已在门前等候。镇长说明欠款由来与三期约定，"
     "交给主角第一块荒地、基础工具与几粒种子。本幕为教学关，引导玩家完成锄地、播种、浇水、收获与卖货的基本循环。")
body("截止日，镇长前来收取第一期费用，感慨主角弯腰干活的样子与外婆一模一样。还清后解锁外婆的第一封信，"
     "并获得稻草人、基础田扩建等奖励。")
h2("4.3 第二幕 · 夏 ·「生计」")
body("单靠种地已难以凑齐第二期费用，镇长“不经意”地提示主角可以去河边钓鱼、进矿洞挖矿、帮邻居完成委托。"
     "本幕开放钓鱼、矿洞、委托订单与果树等系统，引导玩家建立多元化的收入结构。")
body("主角前往镇公所还款时，恰逢来赶集的村民，大家开始真正接纳这位“外婆家的孩子”。"
     "还清后解锁外婆的第二封信，并开放温室、装饰系统与工具升级。")
h2("4.4 第三幕 · 秋冬 ·「归乡」")
body("最后一笔费用数额最大，又值年终赶集日。全镇居民都在用各自的方式帮主角想办法，"
     "本幕是对高级作物、烹饪、赶集日高价出货等综合经营能力的检验。")
body("还清后，老镇长从抽屉中取出真正的地契郑重交给主角，并讲出外婆当年抵押地契其实是为了帮小镇渡过难关。"
     "随后引出外婆的最后一封信与全镇举办的归乡宴，第一幕主线至此收束。")
h2("4.5 三封回忆信")
body("三封信随三期还款依次解锁，主题层层递进，第三封形成情感反转：钱从来不是目的，“回家”才是。",
     firstline=True)
caption("表 4-2  三封回忆信内容")
make_table(
    ["次序", "主题", "信件内容（暂定文案）"],
    [
        ["第一封", "土地", "“种下的东西或许会被风雨打倒，但土地从不会辜负认真对待它的人。”"],
        ["第二封", "邻里", "“一个人守不住一座小镇，是大家互相帮衬，才有了田舍。”"],
        ["第三封", "回家", "“我从没指望你还清什么，我只是想给你留一个，随时能回来的地方。”"],
    ],
    widths=[1.8, 1.8, 10.4])
h2("4.6 结局与后日谈")
body("第三期还清后播放结局过场：老镇长归还地契、全镇归乡宴、片尾字幕。结局之后进入自由经营的“后日谈”模式，"
     "地契面板更新为“土地已归你所有”，并以此为基础预留新区域、新 NPC 剧情线、节日活动等后续内容的扩展接口。")

# ============================================================
# 5 赎地系统
# ============================================================
h1("5 核心系统：赎地（债务）系统")
h2("5.1 系统目标")
body("赎地系统是主线的数值载体，负责管理分期债务、还款进度、截止判定与奖惩发放。它以“一期一目标”的方式，"
     "把玩家在各经营系统中的行为转化为可量化的主线进度，并在每期结束时触发相应剧情。")
h2("5.2 分期机制")
caption("表 5-1  分期还款规则（金额与天数为暂定设计值，需实测校准）")
make_table(
    ["期数", "截止时间（暂定）", "应还金额（暂定）", "能力考核点", "还清后主要解锁"],
    [
        ["第一期", "第 28 天（春末）", "500 G", "耕种 + 卖货", "第一封信、稻草人、田块扩建"],
        ["第二期", "第 56 天（夏末）", "2,000 G", "钓鱼 / 挖矿 / 委托", "第二封信、温室、装饰、工具升级"],
        ["第三期", "第 84 天 / 年末", "5,000 G", "高级作物 / 烹饪 / 赶集", "第三封信、地契、结局过场"],
    ],
    widths=[1.6, 3.2, 2.4, 3.4, 4.0], zebra=True)
h2("5.3 还款来源")
body("还款所用货币复用商店系统的金币字段，主要来源包括：出售作物与采集物（商店）、完成委托订单、"
     "出售鱼获与矿产品、果林采收，以及赶集日的高价出货。所有来源均为游戏现有系统，无需新增独立的赚钱玩法。")
h2("5.4 奖惩机制")
h3("5.4.1 正向奖励")
bullet("按期还清：进入下一幕剧情，解锁对应信件、功能与区域。")
bullet("提前还清：可给予镇长好感、专属装饰 / 道具，或下一期金额少量减免。")
bullet("全部还清：归还地契、播放结局、获得纪念性奖励并开启后日谈。")
h3("5.4.2 还款交互")
body("玩家可在资金充足时，通过地契面板主动“缴纳当期费用”；也可在截止日由剧情自动结算。"
     "支持一次性缴清当期，不鼓励拆分小额缴纳，以减少 UI 与判定复杂度。")
h2("5.5 失败与宽限处理")
body("为契合休闲治愈定位，到期未凑齐不判定游戏失败、不强制回档。改为触发老镇长“再宽限几天”的剧情，"
     "可设置象征性的少量滞纳金或不设惩罚，以鼓励而非挫败玩家。宽限期内仍未还清时，可再次给予宽限，"
     "确保所有玩家都能推进并完成主线。是否收取滞纳金及具体比例，列入待确认事项。")
h2("5.6 状态流转")
body("单期债务的状态流转为：未开始 → 进行中（本期目标生效）→ 已缴清（发放奖励、进入过场）→ 解锁下一期；"
     "若到期未缴清，则进入宽限中，缴清后回到正常流程。三期全部缴清后，主线状态置为已完成，转入后日谈。")

# ============================================================
# 6 开场 Intro
# ============================================================
h1("6 开场流程（Intro）设计")
body("开场在开始场景点击“开始游戏”后、进入主游戏场景前播放，采用图文（静帧 + 旁白文字）形式，"
     "可复用 Resources/Art/Intro 下已有的背景、人物、海、招牌等素材。")
caption("表 6-1  开场 Intro 分镜流程（暂定）")
make_table(
    ["镜次", "画面", "旁白 / 文本", "目的"],
    [
        ["1", "桌上的旧木盒、铜钥匙与信", "外婆来信的旁白", "交代缘起"],
        ["2", "车窗外移动的乡野 / 海", "“我回到了阔别多年的小镇。”", "时间与空间过渡"],
        ["3", "田舍小镇站牌", "小镇名称出现", "确立场景"],
        ["4", "老宅门前，老镇长等候", "镇长说明欠款与三期约定", "抛出核心目标"],
        ["5", "荒地、工具与种子", "操作引导开始", "进入教学与主游戏"],
    ],
    widths=[1.4, 4.6, 5.0, 3.0], zebra=True)

# ============================================================
# 7 UI
# ============================================================
h1("7 UI/UX 设计：地契面板")
body("地契面板是赎地系统的主界面，定位与现有图鉴面板类似，玩家可随时查看主线进度。建议包含以下信息：")
bullet("当前期数与本幕主题（如“第一期 · 荒地”）。")
bullet("当期应还金额、已缴纳金额与进度条。")
bullet("截止日期与剩余天数（如“距春末还有 X 天”）。")
bullet("“立即缴纳”按钮（资金充足时可用），以及缴纳后的过场触发。")
bullet("已解锁信件的回看入口，与三封回忆信对应。")
body("三期全部还清后，面板主体更新为“土地已归你所有”，保留信件回看与后日谈相关入口。"
     "面板视觉风格与 UI2.0 / UITheme 保持一致，使用游戏现有字体与图标规范。")

# ============================================================
# 8 存档
# ============================================================
h1("8 存档设计")
body("主线与赎地进度需纳入现有存档系统，保证读档后状态一致。建议在存档数据中新增以下字段：")
caption("表 8-1  存档新增字段（建议）")
make_table(
    ["字段", "类型", "含义"],
    [
        ["storyPhase", "int", "当前主线幕次（0 未开始 / 1–3 对应三幕 / 4 已完成）"],
        ["currentInstallment", "int", "当前应还期数（1–3）"],
        ["paidAmount", "int", "当期已缴纳金额"],
        ["installmentCleared", "bool[]", "各期是否已缴清"],
        ["unlockedLetters", "int / bool[]", "已解锁的回忆信件编号"],
        ["isGracePeriod", "bool", "当前是否处于宽限期"],
        ["introPlayed", "bool", "开场 Intro 是否已经播放"],
    ],
    widths=[3.6, 2.6, 7.8], zebra=True)
body("新游戏初始化时将各字段置为默认值并播放开场；读档时据此恢复地契面板、可用系统与剧情状态。"
     "字段命名在实现时可与现有 SaveData 的命名风格统一。")

# ============================================================
# 9 集成
# ============================================================
h1("9 与现有系统集成")
h2("9.1 复用的现有系统")
caption("表 9-1  主线复用的现有系统")
make_table(
    ["现有系统", "对应脚本（示例）", "在主线中的用途"],
    [
        ["时间 / 季节", "TimeManager、GameDate", "提供截止日期与天数事件"],
        ["昼夜 / 天气", "DayNightManager、WeatherEffects", "营造氛围、影响生产节奏"],
        ["商店 / 货币", "ShopManager、ShopUI", "金币来源与还款资金"],
        ["委托订单", "CommissionPanel", "中期重要收入与考核点"],
        ["赶集日", "MarketDay", "第三期高价冲刺"],
        ["钓鱼 / 挖矿 / 果树", "FishingManager、MineManager、FruitTreeManager", "多元化还款来源"],
        ["事件 / 选择", "FarmEventManager、EventChoicePanel", "开场与还款剧情过场"],
        ["图鉴 / 面板", "AlmanacManager、AlmanacPanel", "地契面板的实现参照"],
        ["存档", "SaveLoadManager、SaveData", "持久化主线进度"],
    ],
    widths=[2.8, 5.4, 5.8], zebra=True)
h2("9.2 新建脚本清单")
caption("表 9-2  建议新建的脚本（名称可在实现时调整）")
make_table(
    ["脚本名", "职责"],
    [
        ["StoryManager", "主线状态机，统筹幕次推进、剧情触发与完成判定"],
        ["DebtManager", "赎地（债务）数据与缴费、截止、宽限逻辑"],
        ["DeedPanelUI", "地契面板界面显示与交互"],
        ["IntroPlayer", "开场图文 Intro 的播放与跳转"],
        ["LetterData / LetterPanel", "回忆信件数据定义与回看界面"],
    ],
    widths=[4.0, 10.0])
h2("9.3 需修改的现有脚本")
caption("表 9-3  建议修改的现有脚本")
make_table(
    ["脚本", "修改内容"],
    [
        ["SaveData / SaveLoadManager", "增加并读写主线与债务相关字段"],
        ["TimeManager / GameDate", "在截止日抛出事件，供债务系统结算"],
        ["ShopManager", "暴露金币余额变动接口，供缴费与还款判定使用"],
        ["MainMenuManager", "“开始游戏”后接入开场 Intro 流程"],
        ["EventHandler", "新增主线 / 债务 / 信件相关事件定义"],
    ],
    widths=[4.6, 9.4])

# ============================================================
# 10 数值平衡
# ============================================================
h1("10 数值平衡说明")
body("本文档中的还款金额（500 / 2,000 / 5,000 G）与截止天数（第 28 / 56 / 84 天）均为暂定设计值，"
     "用于搭建节奏框架，不代表最终数值。正式平衡需基于以下实测数据进行：")
bullet("各类作物的种子成本、成熟周期、单次产量与售价，折算为单日净利润。")
bullet("钓鱼、挖矿、委托、果树等系统在单位游戏时间内的期望收益。")
bullet("赶集日的价格加成幅度与出现频率。")
bullet("玩家在教学引导下，达成各幕目标所需的合理游戏时长与操作次数。")
body("校准原则：第一期应保证正常跟随教学即可轻松完成；第二期要求玩家使用至少一种多元化收入；"
     "第三期需要较完整的经营规划，但在宽限机制下所有玩家最终都可完成。具体数值待上述数据采集后另行补充，"
     "在补充前以占位 / 暂定形式保留，不做臆断。")

# ============================================================
# 11 里程碑
# ============================================================
h1("11 开发里程碑")
caption("表 11-1  建议开发顺序（最小闭环优先）")
make_table(
    ["阶段", "内容", "产出 / 验收"],
    [
        ["M1", "开场 Intro + 地契面板 + 第一期还款", "可跑通的最小主线闭环"],
        ["M2", "债务系统完善：分期、宽限、奖惩", "三幕规则可配置、可结算"],
        ["M3", "第二、三幕剧情与三封信件", "主线全程可通关"],
        ["M4", "结局过场与后日谈、存档联调", "完整流程 + 存档可靠"],
        ["M5", "数值实测与平衡、美术与 polish", "节奏与体验达标"],
    ],
    widths=[1.4, 6.6, 6.0], zebra=True)

# ============================================================
# 12 待确认
# ============================================================
h1("12 待确认事项")
bullet("主角与外婆的最终称谓、主角是否限定性别或完全由玩家自定义。")
bullet("还款金额与截止天数的最终数值（需结合作物售价与单日产能实测）。")
bullet("到期未还是否收取滞纳金，以及宽限期的具体天数。")
bullet("提前还清的奖励形式（好感 / 装饰 / 减免）及其力度。")
bullet("开场 Intro 的最终美术形式（静帧图文 / 动态分镜）与篇幅。")
bullet("后日谈开放的新区域、新 NPC 与节日活动的范围与排期。")

out = r"D:\yx\Cozy Town\DesignDocs\田舍小镇_剧情与系统设计文档_V1.0.docx"
doc.save(out)
print("saved:", out)
