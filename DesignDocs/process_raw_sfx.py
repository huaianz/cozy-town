# -*- coding: utf-8 -*-
"""处理 AI 真实拟音 raw -> SFX (单声道 44.1k 16bit)。
raw 头时长字段损坏，按字节解析；能量包络定位起点；按音效裁切；淡入淡出；归一化。"""
import numpy as np, wave, os, glob

RAW = r"D:\yx\Cozy Town\Assets\Resources\Audio\_raw"
OUT = r"D:\yx\Cozy Town\Assets\Resources\Audio\SFX"
SR_IN = 40000
SR_OUT = 44100

# 目标有效长度(秒)
DUR = {
    "Dig": 0.45, "Water": 1.0, "Plant": 0.6, "Harvest": 0.9, "Chop": 0.5,
    "Mine": 0.6, "Pickup": 0.5, "Buy": 0.8, "Sell": 0.7, "Deliver": 0.8,
    "Sleep": 2.2,
}

def load_raw(path):
    data = open(path, "rb").read()
    # 找 'data' chunk；找不到就跳过标准44字节
    i = data.find(b"data")
    if i >= 0:
        start = i + 8
    else:
        start = 44
    raw = data[start:]
    n = (len(raw) // 4) * 4
    x = np.frombuffer(raw[:n], dtype="<i2").reshape(-1, 2)
    mono = x.mean(axis=1)
    return mono

def resample(x, sr0, sr1):
    if sr0 == sr1:
        return x
    n = int(round(len(x) * sr1 / sr0))
    xp = np.linspace(0, len(x) - 1, n)
    idx = xp.astype(np.int64)
    frac = xp - idx
    nxt = np.minimum(idx + 1, len(x) - 1)
    return x[idx] * (1 - frac) + x[nxt] * frac

def find_onset(x, frame=400):
    n = len(x) // frame
    if n <= 0:
        return 0
    rms = np.sqrt(np.mean(x[: n * frame].reshape(n, frame) ** 2, axis=1))
    peak = rms.max()
    if peak <= 0:
        return 0
    thr = peak * 0.08
    hits = np.where(rms > thr)[0]
    if len(hits) == 0:
        return 0
    return max(0, int(hits[0]) * frame - int(SR_IN * 0.02))

def save(name, x, peak=0.88):
    m = np.max(np.abs(x))
    if m > 0:
        x = x / m * peak
    fi = int(SR_OUT * 0.005); fo = int(SR_OUT * 0.04)
    x[:fi] *= np.linspace(0, 1, fi)
    x[-fo:] *= np.linspace(1, 0, fo)
    pcm = (x * 32767).astype("<i2")
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR_OUT)
        w.writeframes(pcm.tobytes())
    print("saved", name, round(len(pcm)/SR_OUT, 2), "s")

for p in sorted(glob.glob(os.path.join(RAW, "*_raw.wav"))):
    name = os.path.basename(p).replace("_raw.wav", "")
    if name not in DUR:
        continue
    x = load_raw(p)
    onset = find_onset(x)
    need = int(SR_IN * DUR[name])
    seg = x[onset:onset + need]
    if len(seg) < need:  # 末尾不足则补零
        seg = np.pad(seg, (0, need - len(seg)))
    seg = resample(seg, SR_IN, SR_OUT)
    save(name, seg)

print("DONE ->", OUT)
