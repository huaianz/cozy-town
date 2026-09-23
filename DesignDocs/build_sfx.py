# -*- coding: utf-8 -*-
"""程序化合成《田舍小镇》短音效 -> Resources/Audio/SFX (16bit PCM wav, 单声道)
短音效用 PCM + DecompressOnLoad，运行期零解码 CPU；单声道节省内存。"""
import numpy as np, wave, os

SR = 44100
OUT = r"D:\yx\Cozy Town\Assets\Resources\Audio\SFX"
os.makedirs(OUT, exist_ok=True)

rng = np.random.default_rng(7)

def t(dur):
    return np.linspace(0, dur, int(SR*dur), endpoint=False)

def sine(freq, dur, phase=0.0):
    return np.sin(2*np.pi*freq*t(dur) + phase)

def noise(dur):
    return rng.standard_normal(int(SR*dur))

def expdec(dur, k):
    return np.exp(-k*t(dur))

def adsr(dur, a=0.005, d=0.08, s=0.6, rfrac=0.25):
    n=int(SR*dur); aN=int(SR*a); rN=int(SR*dur*rfrac); dN=int(SR*d)
    env=np.ones(n)*s
    env[:aN]=np.linspace(0,1,aN)
    if dN>0: env[aN:aN+dN]=np.linspace(1,s,dN)
    env[-rN:]=np.linspace(s,0,rN)
    return env

def lpf(x, win):
    k=np.ones(win)/win
    return np.convolve(x,k,mode='same')

def tone(freq, dur, kind='sine', vib=0.0):
    tt=t(dur)
    if kind=='sine': w=np.sin(2*np.pi*freq*tt)
    elif kind=='tri': w=2/np.pi*np.arcsin(np.sin(2*np.pi*freq*tt))
    else: w=np.sign(np.sin(2*np.pi*freq*tt))
    if vib: w*= (1+0.02*np.sin(2*np.pi*vib*tt))
    return w

def save(name, x, peak=0.9):
    x=np.asarray(x,dtype=np.float64)
    m=np.max(np.abs(x))
    if m>0: x=x/m*peak
    # 轻微淡出避免爆音
    fade=int(SR*0.01); x[:fade]*=np.linspace(0,1,fade); x[-fade:]*=np.linspace(1,0,fade)
    pcm=(x*32767).astype('<i2')
    with wave.open(os.path.join(OUT,name+'.wav'),'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    print('saved', name, round(len(pcm)/SR,3),'s')

# 1 Dig 锄地：低频闷响 + 短土噪
d=0.18
x = sine(95,d)*expdec(d,28)*0.9
x += lpf(noise(d),6)*expdec(d,40)*0.7
save('Dig', x)

# 2 Water 浇水：低通噪声水流
d=0.45
x = lpf(noise(d),14)*0.8
env = np.minimum(1, t(d)/0.05)*np.exp(-1.2*np.maximum(0,t(d)-0.25))
save('Water', x*env)

# 3 Plant 种植：轻拨 + 轻土
d=0.22
x = tone(520,d,'sine')*adsr(d,a=0.004,d=0.05,s=0.3,rfrac=0.5)*0.5
_n = lpf(noise(0.12),8)*expdec(0.12,30)*0.4
_pad = np.zeros_like(x); _pad[:len(_n)] += _n
x += _pad
save('Plant', x)

# 4 Harvest 收获：明亮上行琶音 C5 E5 G6
notes=[523.25,659.25,783.99]; each=0.11; gap=0.02
x=np.zeros(int(SR*(each*3+gap*2)))
for i,f in enumerate(notes):
    seg=tone(f,each,'tri')*adsr(each,a=0.005,d=0.05,s=0.5,rfrac=0.3)
    s=int(SR*i*(each+gap)); x[s:s+len(seg)]+=seg
save('Harvest', x)

# 5 Chop 砍树：木质冲击
d=0.2
x = lpf(noise(d),3)*expdec(d,30)*0.9
x += sine(180,d)*expdec(d,26)*0.6
save('Chop', x)

# 6 Mine 挖矿：金属叮当
d=0.28
x = (sine(1320,d)*0.6 + sine(1980,d)*0.4 + sine(2640,d)*0.2)*expdec(d,16)
_n = lpf(noise(0.1),2)*expdec(0.1,40)*0.5
_pad=np.zeros_like(x); _pad[:len(_n)]+=_n
x += _pad
save('Mine', x)

# 7 Pickup 拾取：上行双音 E5 A5
x=np.zeros(int(SR*0.22))
for i,f in enumerate([659.25,880.0]):
    seg=tone(f,0.12,'sine')*adsr(0.12,a=0.004,d=0.04,s=0.4,rfrac=0.4)
    s=int(SR*i*0.1); x[s:s+len(seg)]+=seg
save('Pickup', x, peak=0.8)

# 8 Buy 购买：收银双铃
x=np.zeros(int(SR*0.45))
for i,f in enumerate([1568,2093]):
    seg=tone(f,0.3,'sine')*adsr(0.3,a=0.003,d=0.1,s=0.4,rfrac=0.4)
    s=int(SR*(0.02+i*0.12)); x[s:s+len(seg)]+=seg
save('Buy', x, peak=0.8)

# 9 Sell 卖出：硬币双碰
x=np.zeros(int(SR*0.3))
for i,f in enumerate([1318,1760]):
    seg=(sine(f,0.16)*0.7+sine(f*1.5,0.16)*0.3)*adsr(0.16,a=0.002,d=0.05,s=0.3,rfrac=0.4)
    s=int(SR*i*0.08); x[s:s+len(seg)]+=seg
save('Sell', x, peak=0.8)

# 10 Deliver 交付：柔和上行确认
x=np.zeros(int(SR*0.32))
for i,f in enumerate([587.33,880.0]):
    seg=tone(f,0.2,'sine')*adsr(0.2,a=0.01,d=0.08,s=0.5,rfrac=0.4)
    s=int(SR*i*0.12); x[s:s+len(seg)]+=seg
save('Deliver', x, peak=0.8)

# 11 Sleep 睡觉：柔和缓慢下行
x=np.zeros(int(SR*0.9))
for i,f in enumerate([440,349.23,261.63]):
    seg=tone(f,0.45,'sine')*adsr(0.45,a=0.08,d=0.1,s=0.5,rfrac=0.5)*0.7
    s=int(SR*i*0.22); x[s:s+len(seg)]+=seg
x += lpf(noise(0.9),40)*0.015
save('Sleep', x, peak=0.7)

print('ALL DONE ->', OUT)
