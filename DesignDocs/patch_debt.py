# -*- coding: utf-8 -*-
p = r'D:\yx\Cozy Town\Assets\Scripts\DebtManager.cs'
src = open(p, encoding='gbk').read()

# 1) 缓存字段
fanchor = '    private int currentDay;'
assert fanchor in src, 'field anchor missing'
src = src.replace(fanchor,
                  '    private static TimeManager cachedTimeManager;\n' + fanchor, 1)

# 2) RemainingDays 内 FindObjectOfType -> 缓存
old = '        TimeManager timeManager = FindObjectOfType<TimeManager>();'
assert old in src, 'findobject anchor missing'
new = (
    '        TimeManager timeManager = cachedTimeManager;\n'
    '        if (timeManager == null)\n'
    '        {\n'
    '            cachedTimeManager = FindObjectOfType<TimeManager>();\n'
    '            timeManager = cachedTimeManager;\n'
    '        }'
)
src = src.replace(old, new, 1)

# 3) 结局触发过场
eanchor = '            EventHandler.CallFarmEventEvent("归乡", "老镇长把地契交到你手里：这块田，从今天起真正回家了。", false);'
assert eanchor in src, 'ending anchor missing'
src = src.replace(eanchor, eanchor + '\n\n            EventHandler.CallStoryEndingEvent();', 1)

open(p, 'w', encoding='gbk').write(src)
print('DebtManager patched; ending =', 'CallStoryEndingEvent' in src,
      '; cache =', 'cachedTimeManager' in src)
