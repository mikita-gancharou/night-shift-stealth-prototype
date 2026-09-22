#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Night Shift - procedural placeholder asset generator.

Generates every audio and sprite placeholder the prototype needs, from scratch,
with no network access and no binary source assets.

Outputs
-------
    <project>/Assets/Audio/*.wav           mono 44100 Hz 16-bit PCM
    <project>/Assets/Art/Sprites/*.png     RGBA, white shapes on transparency

Backends
--------
numpy + Pillow are used when importable (faster array math, faster PNG encode).
When either is missing the script falls back to the standard library only
(`array`/`math` for DSP, `wave` for WAV, `zlib`/`struct` for PNG). Both paths
produce equivalent assets; nothing is pip-installed.

Everything is deterministic: every RNG is explicitly seeded, so re-running the
script overwrites the previous files with byte-identical content.

Run:
    python generate_assets.py            # write assets + print a report
    python generate_assets.py --audio    # only WAVs
    python generate_assets.py --sprites  # only PNGs
"""

from __future__ import annotations

import array
import math
import os
import random
import struct
import sys
import time
import wave
import zlib

# --------------------------------------------------------------------------- #
#  Backend detection
# --------------------------------------------------------------------------- #

try:
    import numpy as _np
except Exception:  # pragma: no cover - exercised only on bare installs
    _np = None

try:
    import PIL as _PIL
    from PIL import Image as _Image
except Exception:  # pragma: no cover
    _PIL = None
    _Image = None

HAVE_NUMPY = _np is not None
HAVE_PIL = _Image is not None


# --------------------------------------------------------------------------- #
#  Paths & global config
# --------------------------------------------------------------------------- #

TOOLS_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_DIR = os.path.dirname(TOOLS_DIR)
AUDIO_DIR = os.path.join(PROJECT_DIR, "Assets", "Audio")
SPRITE_DIR = os.path.join(PROJECT_DIR, "Assets", "Art", "Sprites")

SR = 44100                 # sample rate, Hz
PEAK_DBFS = -3.0           # peak-normalisation target for every file
FADE_IN = 0.003            # 3 ms click guard on one-shots
FADE_OUT = 0.004           # 4 ms click guard on one-shots

# Musical material - everything lives in A minor so layers cross-fade cleanly.
A1, A2, A3, A4, A5 = 55.00, 110.00, 220.00, 440.00, 880.00
C2, C3, C4, C5, C6 = 65.41, 130.81, 261.63, 523.25, 1046.50
E2, E3, E4, E5 = 82.41, 164.81, 329.63, 659.25
D3, F3, G3, G4, B3, B4 = 146.83, 174.61, 196.00, 392.00, 246.94, 493.88

# A few sounds are described as deliberately quiet / deliberately a background
# bed. They are peak-normalised to PEAK_DBFS first and then trimmed by this much
# so the set has sane relative loudness out of the box.
# Footsteps repeat twice a second, so they sit far below the one-shots you only hear once.
TRIM_DB = {
    "footstep_soft_1.wav": -13.0,
    "footstep_soft_2.wav": -13.0,
    "footstep_soft_3.wav": -13.0,
    "footstep_1.wav": -11.0,
    "footstep_2.wav": -11.0,
    "footstep_3.wav": -11.0,
    "footstep_gravel_1.wav": -9.5,
    "footstep_gravel_2.wav": -9.5,
    "footstep_gravel_3.wav": -9.5,
    "footstep_metal_1.wav": -10.0,
    "footstep_metal_2.wav": -10.0,
    "footstep_metal_3.wav": -10.0,
    "ui_click.wav": -5.0,
    "ui_hover.wav": -12.0,
    "ambient_loop.wav": -6.0,
    "tension_loop.wav": -5.0,
    "goal_hum.wav": -6.0,
}


# --------------------------------------------------------------------------- #
#  Tiny array layer - one code path, two backends
# --------------------------------------------------------------------------- #

def zeros(n):
    return _np.zeros(n, dtype=_np.float64) if HAVE_NUMPY else [0.0] * n


def ones(n):
    return _np.ones(n, dtype=_np.float64) if HAVE_NUMPY else [1.0] * n


def from_list(xs):
    return _np.asarray(xs, dtype=_np.float64) if HAVE_NUMPY else list(xs)


def to_list(a):
    return a.tolist() if HAVE_NUMPY else list(a)


def concat(a, b):
    return _np.concatenate([a, b]) if HAVE_NUMPY else list(a) + list(b)


def tile(a, k):
    return _np.tile(a, k) if HAVE_NUMPY else list(a) * k


def add(a, b):
    if HAVE_NUMPY:
        return a + b
    if isinstance(b, (int, float)):
        return [x + b for x in a]
    if isinstance(a, (int, float)):
        return [a + y for y in b]
    return [x + y for x, y in zip(a, b)]


def sub(a, b):
    if HAVE_NUMPY:
        return a - b
    if isinstance(b, (int, float)):
        return [x - b for x in a]
    if isinstance(a, (int, float)):
        return [a - y for y in b]
    return [x - y for x, y in zip(a, b)]


def mul(a, b):
    if HAVE_NUMPY:
        return a * b
    if isinstance(b, (int, float)):
        return [x * b for x in a]
    if isinstance(a, (int, float)):
        return [a * y for y in b]
    return [x * y for x, y in zip(a, b)]


def scale(a, k):
    return mul(a, float(k))


def neg(a):
    return -a if HAVE_NUMPY else [-x for x in a]


def v_abs(a):
    return _np.abs(a) if HAVE_NUMPY else [abs(x) for x in a]


def v_exp(a):
    return _np.exp(a) if HAVE_NUMPY else [math.exp(x) for x in a]


def v_tanh(a):
    return _np.tanh(a) if HAVE_NUMPY else [math.tanh(x) for x in a]


def v_hypot(x, y):
    if HAVE_NUMPY:
        return _np.hypot(x, y)
    return [math.hypot(p, q) for p, q in zip(x, y)]


def v_max2(a, b):
    if HAVE_NUMPY:
        return _np.maximum(a, b)
    if isinstance(b, (int, float)):
        return [x if x > b else b for x in a]
    return [x if x > y else y for x, y in zip(a, b)]


def v_min2(a, b):
    if HAVE_NUMPY:
        return _np.minimum(a, b)
    if isinstance(b, (int, float)):
        return [x if x < b else b for x in a]
    return [x if x < y else y for x, y in zip(a, b)]


def v_clip(a, lo, hi):
    if HAVE_NUMPY:
        return _np.clip(a, lo, hi)
    return [lo if x < lo else (hi if x > hi else x) for x in a]


def peak_of(a):
    if HAVE_NUMPY:
        return float(_np.max(_np.abs(a))) if len(a) else 0.0
    return max((abs(x) for x in a), default=0.0)


def _acc(dst, doff, src, soff, count, gain):
    """dst[doff:doff+count] += src[soff:soff+count] * gain"""
    if count <= 0:
        return
    if HAVE_NUMPY:
        dst[doff:doff + count] += src[soff:soff + count] * gain
    else:
        for i in range(count):
            dst[doff + i] += src[soff + i] * gain


def addmix(dst, src, offset=0, gain=1.0):
    """Mix `src` into `dst` at `offset`, clipping anything past the end."""
    n, m = len(dst), len(src)
    if offset >= n:
        return dst
    start = max(0, offset)
    _acc(dst, start, src, start - offset, min(m - (start - offset), n - start), gain)
    return dst


def addmix_wrap(dst, src, offset=0, gain=1.0):
    """Mix `src` into `dst` at `offset`, wrapping around the end.

    Used for every transient inside a loop so that a decay tail which runs past
    the loop point reappears at the head - that is what keeps the seam silent.
    """
    n, m = len(dst), len(src)
    if n <= 0 or m <= 0:
        return dst
    off = offset % n
    first = min(m, n - off)
    _acc(dst, off, src, 0, first, gain)
    pos, rest = first, m - first
    while rest > 0:
        take = min(rest, n)
        _acc(dst, 0, src, pos, take, gain)
        pos += take
        rest -= take
    return dst


# --------------------------------------------------------------------------- #
#  Oscillators, noise, envelopes
# --------------------------------------------------------------------------- #

def dur_n(seconds):
    return int(round(seconds * SR))


def _max_of(freq):
    if isinstance(freq, (int, float)):
        return float(freq)
    return peak_of(freq)


def phase_of(n, freq, phase0=0.0):
    """Phase ramp for a constant or per-sample frequency."""
    if isinstance(freq, (int, float)):
        if HAVE_NUMPY:
            return 2.0 * math.pi * freq * _np.arange(n) / SR + phase0
        k = 2.0 * math.pi * freq / SR
        return [phase0 + k * i for i in range(n)]
    if HAVE_NUMPY:
        return phase0 + 2.0 * math.pi * _np.cumsum(freq) / SR
    ph, k, out = phase0, 2.0 * math.pi / SR, []
    for f in freq:
        ph += k * f
        out.append(ph)
    return out


def osc_sine(n, freq, phase0=0.0):
    ph = phase_of(n, freq, phase0)
    return _np.sin(ph) if HAVE_NUMPY else [math.sin(p) for p in ph]


def osc_tri(n, freq, harmonics=7, phase0=0.0):
    """Additive triangle: odd harmonics, 1/k^2, alternating sign.

    Band-limited by construction, so it stays soft instead of buzzing.
    """
    ph = phase_of(n, freq, phase0)
    fmax = max(1.0, _max_of(freq))
    out = zeros(n)
    for i in range(harmonics):
        k = 2 * i + 1
        if k * fmax > SR * 0.45:
            break
        w = ((-1.0) ** i) / float(k * k)
        if HAVE_NUMPY:
            out = out + w * _np.sin(k * ph)
        else:
            out = [o + w * math.sin(k * p) for o, p in zip(out, ph)]
    return scale(out, 8.0 / (math.pi ** 2))


def loop_sine(n, cycles, phase0=0.0):
    """Sine that completes exactly `cycles` periods over `n` samples.

    Building every tonal layer this way is what makes the loops seamless
    without any crossfade.
    """
    c = int(round(cycles))
    if HAVE_NUMPY:
        return _np.sin(2.0 * math.pi * c * _np.arange(n) / n + phase0)
    k = 2.0 * math.pi * c / n
    return [math.sin(k * i + phase0) for i in range(n)]


def loop_tone(n, hz, duration, phase0=0.0):
    """`loop_sine` addressed by frequency; the pitch snaps to the loop grid."""
    return loop_sine(n, max(1, int(round(hz * duration))), phase0)


def lfo(n, cycles, phase=0.0, lo=0.0, hi=1.0):
    """Exactly-periodic unipolar LFO over the whole buffer."""
    c = int(round(cycles))
    if HAVE_NUMPY:
        s = _np.sin(2.0 * math.pi * c * _np.arange(n) / n + phase)
    else:
        k = 2.0 * math.pi * c / n
        s = [math.sin(k * i + phase) for i in range(n)]
    return add(scale(add(scale(s, 0.5), 0.5), (hi - lo)), lo)


def rng_noise(n, seed):
    """Deterministic uniform white noise."""
    if HAVE_NUMPY:
        return _np.random.default_rng(seed).uniform(-0.7, 0.7, n)
    r = random.Random(seed)
    return [r.uniform(-0.7, 0.7) for _ in range(n)]


def env_perc(n, attack_s, decay_tau, sr=SR):
    """Raised-cosine attack into an exponential decay, peaking at 1.0."""
    a = min(n, max(1, int(round(attack_s * sr))))
    tau = max(1e-6, decay_tau * sr)
    if HAVE_NUMPY:
        e = _np.empty(n, dtype=_np.float64)
        e[:a] = 0.5 - 0.5 * _np.cos(math.pi * (_np.arange(a) + 1.0) / a)
        if n > a:
            e[a:] = _np.exp(-_np.arange(n - a) / tau)
        return e
    out = [0.0] * n
    for i in range(a):
        out[i] = 0.5 - 0.5 * math.cos(math.pi * (i + 1.0) / a)
    for i in range(a, n):
        out[i] = math.exp(-(i - a) / tau)
    return out


def env_seg(n, points):
    """Piecewise-linear envelope from (time_fraction, value) breakpoints."""
    xs = [float(p[0]) for p in points]
    ys = [float(p[1]) for p in points]
    if HAVE_NUMPY:
        return _np.interp(_np.linspace(0.0, 1.0, n), xs, ys)
    out, seg = [0.0] * n, 0
    for i in range(n):
        t = i / (n - 1.0) if n > 1 else 0.0
        while seg < len(xs) - 2 and t > xs[seg + 1]:
            seg += 1
        span = xs[seg + 1] - xs[seg] if seg + 1 < len(xs) else 0.0
        if span <= 0.0:
            out[i] = ys[min(seg, len(ys) - 1)]
        else:
            u = min(1.0, max(0.0, (t - xs[seg]) / span))
            out[i] = ys[seg] + (ys[seg + 1] - ys[seg]) * u
    return out


def freq_seg(n, points):
    """Piecewise glide interpolated in log-frequency, so it sounds musical."""
    return v_exp(env_seg(n, [(t, math.log(max(1e-6, f))) for t, f in points]))


def lin_ramp(n, a, b):
    return env_seg(n, [(0.0, a), (1.0, b)])


def exp_ramp(n, a, b):
    return freq_seg(n, [(0.0, a), (1.0, b)])


# --------------------------------------------------------------------------- #
#  Filters (sample recursions - identical on both backends)
# --------------------------------------------------------------------------- #

def _lp_coef(fc):
    fc = max(8.0, min(float(fc), SR * 0.45))
    return 1.0 - math.exp(-2.0 * math.pi * fc / SR)


def one_pole_lp(x, cutoff):
    """One-pole low pass; `cutoff` may be a scalar or a per-sample array."""
    xs = to_list(x)
    n = len(xs)
    out = [0.0] * n
    y = 0.0
    if isinstance(cutoff, (int, float)):
        a = _lp_coef(cutoff)
        for i in range(n):
            y += a * (xs[i] - y)
            out[i] = y
    else:
        cs = to_list(cutoff)
        for i in range(n):
            y += _lp_coef(cs[i]) * (xs[i] - y)
            out[i] = y
    return from_list(out)


def low_pass(x, cutoff, poles=2):
    y = x
    for _ in range(poles):
        y = one_pole_lp(y, cutoff)
    return y


def high_pass(x, cutoff, poles=1):
    y = x
    for _ in range(poles):
        y = sub(y, one_pole_lp(y, cutoff))
    return y


def band_pass(x, lo, hi, poles=2):
    return low_pass(high_pass(x, lo, poles), hi, poles)


def resonator(x, freq, decay_s, amp=1.0):
    """Two-pole resonant peak - the metallic / clacky partials come from here."""
    xs = to_list(x)
    n = len(xs)
    r = math.exp(-1.0 / max(1e-4, decay_s * SR))
    w = 2.0 * math.pi * min(freq, SR * 0.45) / SR
    a1, a2 = 2.0 * r * math.cos(w), -r * r
    g = (1.0 - r) * amp
    out = [0.0] * n
    y1 = y2 = 0.0
    for i in range(n):
        y = g * xs[i] + a1 * y1 + a2 * y2
        y2, y1 = y1, y
        out[i] = y
    return from_list(out)


def cyclic_filter(x, fn):
    """Filter `x` as if it were an infinite loop of itself.

    Runs `fn` over two concatenated copies and keeps the second one, so the
    filter state at the head matches the state at the loop point. Without this
    every IIR pass leaves a start-up transient at the head that does not match
    the settled tail - which is audible as a tick at the loop point.
    """
    n = len(x)
    return fn(tile(x, 2))[n:2 * n]


def _tile2(v):
    """Tile a per-sample modulation array to match a doubled buffer."""
    return v if isinstance(v, (int, float)) else tile(v, 2)


def loop_low_pass(x, cutoff, poles=2):
    return cyclic_filter(x, lambda s: low_pass(s, _tile2(cutoff), poles))


def loop_band_pass(x, lo, hi, poles=2):
    return cyclic_filter(x, lambda s: band_pass(s, _tile2(lo), _tile2(hi), poles))


# --------------------------------------------------------------------------- #
#  Shaping & WAV output
# --------------------------------------------------------------------------- #

def normalize(x, dbfs=PEAK_DBFS):
    p = peak_of(x)
    if p <= 1e-12:
        return x
    return scale(x, (10.0 ** (dbfs / 20.0)) / p)


def apply_fades(x, fade_in=FADE_IN, fade_out=FADE_OUT):
    """Raised-cosine fades so no one-shot starts or ends on a non-zero sample.

    The fade-in is shortened so it always ends before the loudest sample: on a
    click whose peak lands 0.3 ms in, a full 3 ms fade would flatten the very
    transient the sound is made of. Run this *before* normalising so the file
    still ends up at the intended peak.
    """
    xs = to_list(x)
    n = len(xs)
    if n < 4:
        return from_list(xs)
    peak_i = max(range(n), key=lambda i: abs(xs[i]))
    fi = min(max(1, int(round(fade_in * SR))), max(1, peak_i), n // 2)
    fo = min(max(1, int(round(fade_out * SR))), n // 2)
    for i in range(fi):
        xs[i] *= 0.5 - 0.5 * math.cos(math.pi * i / fi)
    for i in range(fo):
        xs[n - 1 - i] *= 0.5 - 0.5 * math.cos(math.pi * i / fo)
    return from_list(xs)


def soft_sat(x, drive=1.0):
    """Gentle tanh saturation - adds bite without square-wave harshness."""
    return scale(v_tanh(scale(x, drive)), 1.0 / math.tanh(drive))


def write_wav(path, x, sr=SR):
    if HAVE_NUMPY:
        pcm = _np.clip(_np.asarray(x, dtype=_np.float64), -1.0, 1.0)
        data = (pcm * 32767.0).astype("<i2").tobytes()
    else:
        buf = array.array("h", (0,) * len(x))
        for i, v in enumerate(x):
            v = -1.0 if v < -1.0 else (1.0 if v > 1.0 else v)
            buf[i] = int(v * 32767.0)
        if sys.byteorder == "big":
            buf.byteswap()
        data = buf.tobytes()
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(data)


# =========================================================================== #
#  ONE-SHOTS
# =========================================================================== #

def make_footstep_soft(seed):
    """Crouched step: muffled low thump, almost no high end."""
    n = dur_n(0.13)
    body = low_pass(rng_noise(n, seed), 340.0, poles=3)
    body = mul(body, env_perc(n, 0.004, 0.034))
    thump = mul(osc_sine(n, exp_ramp(n, 94.0, 50.0)), env_perc(n, 0.004, 0.046))
    return add(scale(body, 0.7), scale(thump, 1.0))


def make_footstep_concrete(seed):
    """Normal step on concrete: a soft sole landing, weight instead of click.

    The first version stacked a 1.8-5.4 kHz tick on a 1 ms attack - exactly the band the
    ear is most sensitive to. Twice a second that became fatiguing, so the body now carries
    the sound, the attack is eased and the top end is only a hint.
    """
    n = dur_n(0.17)
    scuff = band_pass(rng_noise(n, seed), 240.0, 1400.0, poles=2)
    scuff = mul(scuff, env_perc(n, 0.007, 0.042))
    air = mul(band_pass(rng_noise(n, seed + 7), 1200.0, 2800.0, 2),
              env_perc(n, 0.005, 0.013))
    low = mul(osc_sine(n, exp_ramp(n, 116.0, 60.0)), env_perc(n, 0.005, 0.058))
    step = add(add(scale(scuff, 0.8), scale(air, 0.1)), scale(low, 1.0))
    return low_pass(step, 2600.0, poles=2)


def make_footstep_gravel(seed):
    """Gravel: a crunch made of small grains, kept out of the hissy top octaves."""
    n = dur_n(0.20)
    out = zeros(n)
    r = random.Random(seed)
    bed = mul(band_pass(rng_noise(n, seed), 500.0, 2600.0, 2), env_perc(n, 0.005, 0.046))
    out = add(out, scale(bed, 0.62))
    for g in range(7):
        gl = int(SR * r.uniform(0.007, 0.020))
        grain = band_pass(rng_noise(gl, seed * 31 + g),
                          r.uniform(700.0, 1400.0), r.uniform(1900.0, 3100.0), 2)
        grain = mul(grain, env_perc(gl, 0.0018, r.uniform(0.004, 0.011)))
        addmix(out, grain, int(r.uniform(0.0, 0.105) * SR), r.uniform(0.22, 0.55))
    low = mul(osc_sine(n, exp_ramp(n, 106.0, 62.0)), env_perc(n, 0.004, 0.040))
    return low_pass(add(out, scale(low, 0.75)), 3200.0, 2)


def make_footstep_metal(seed, detune=1.0):
    """Metal grating: a dull ring. Lower partials, shorter decays, softer excitation."""
    n = dur_n(0.26)
    exc = mul(low_pass(rng_noise(n, seed), 2600.0, 2), env_perc(n, 0.002, 0.009))
    # The resonators have a tiny impulse gain, so they need a large mix weight to ring at all.
    out = zeros(n)
    for f, dec, amp in ((790.0, 0.135, 1.00), (1190.0, 0.100, 0.58),
                        (1810.0, 0.070, 0.30), (2620.0, 0.042, 0.13)):
        out = add(out, resonator(exc, f * detune, dec, amp))
    clack = mul(band_pass(rng_noise(n, seed + 3), 500.0, 2400.0, 2),
                env_perc(n, 0.004, 0.014))
    low = mul(osc_sine(n, exp_ramp(n, 138.0, 78.0)), env_perc(n, 0.005, 0.044))
    return low_pass(add(add(scale(out, 30.0), scale(clack, 0.40)), scale(low, 0.25)), 3400.0, 2)


def make_stone_throw():
    """Airy whoosh: a noise band whose centre sweeps downward."""
    n = dur_n(0.30)
    centre = exp_ramp(n, 2700.0, 520.0)
    air = band_pass(rng_noise(n, 401), scale(centre, 0.55), scale(centre, 1.75), 2)
    air = mul(air, env_seg(n, [(0.0, 0.0), (0.06, 0.55), (0.30, 1.0),
                               (0.62, 0.55), (1.0, 0.0)]))
    body = mul(low_pass(rng_noise(n, 402), 300.0, 2),
               env_seg(n, [(0.0, 0.0), (0.35, 1.0), (1.0, 0.0)]))
    return add(scale(air, 1.0), scale(body, 0.22))


def make_stone_impact():
    """Sharp clack, then two quieter bounces."""
    n = dur_n(0.35)
    out = zeros(n)

    def clack(at, amp, decay, bright, seed):
        ln = n - at
        if ln <= 64:
            return
        exc = mul(rng_noise(ln, seed), env_perc(ln, 0.0003, 0.0025))
        body = zeros(ln)
        for f, d, a in ((900.0, 1.00, 1.00), (1480.0, 0.70, 0.55), (2350.0, 0.50, 0.32)):
            body = add(body, resonator(exc, f * bright, decay * d, a))
        hit = mul(band_pass(rng_noise(ln, seed + 1), 1200.0, 7000.0, 2),
                  env_perc(ln, 0.0004, 0.006))
        addmix(out, add(body, scale(hit, 0.8)), at, amp)

    clack(0, 1.00, 0.090, 1.00, 411)
    clack(dur_n(0.155), 0.34, 0.060, 1.13, 412)
    clack(dur_n(0.255), 0.13, 0.040, 1.26, 413)
    thud = mul(osc_sine(n, exp_ramp(n, 180.0, 82.0)), env_perc(n, 0.0015, 0.034))
    return add(out, scale(thud, 0.35))


def make_pickup_stone():
    """Soft two-note blip up: E5 -> A5."""
    n = dur_n(0.25)
    out = zeros(n)
    for hz, at, dec, amp in ((E5, 0.000, 0.085, 0.85), (A5, 0.075, 0.135, 1.00)):
        ln = n - dur_n(at)
        voice = add(scale(osc_sine(ln, hz), 0.75), scale(osc_tri(ln, hz), 0.25))
        addmix(out, mul(voice, env_perc(ln, 0.006, dec)), dur_n(at), amp)
    return low_pass(out, 5200.0, 1)


def make_pickup_intel():
    """Brighter three-note arpeggio (A4 C5 E5) with an octave shimmer."""
    n = dur_n(0.60)
    out = zeros(n)
    for hz, at, amp in ((A4, 0.000, 0.80), (C5, 0.095, 0.90), (E5, 0.190, 1.00)):
        ln = n - dur_n(at)
        voice = add(scale(osc_tri(ln, hz), 0.55), scale(osc_sine(ln, hz), 0.45))
        shimmer = scale(osc_sine(ln, hz * 2.0), 0.18)
        addmix(out, mul(add(voice, shimmer), env_perc(ln, 0.005, 0.20)), dur_n(at), amp)
    tail_at = dur_n(0.190)
    tail_n = n - tail_at
    tail = add(scale(osc_sine(tail_n, E5 * 2.0), 0.55), scale(osc_sine(tail_n, A5), 0.45))
    addmix(out, mul(tail, env_perc(tail_n, 0.030, 0.16)), tail_at, 0.16)
    return low_pass(out, 7000.0, 1)


def make_hide_enter():
    """Muffled cloth/door shut - a low-passed noise burst closing down."""
    n = dur_n(0.40)
    cloth = low_pass(rng_noise(n, 601), exp_ramp(n, 1150.0, 170.0), poles=3)
    cloth = mul(cloth, env_seg(n, [(0.0, 0.0), (0.03, 1.0), (0.22, 0.42), (1.0, 0.0)]))
    thud = mul(osc_sine(n, exp_ramp(n, 132.0, 60.0)), env_perc(n, 0.004, 0.085))
    return add(scale(cloth, 1.0), scale(thud, 0.65))


def make_hide_exit():
    """Same gesture as hide_enter, opening and a little brighter."""
    n = dur_n(0.40)
    cloth = low_pass(rng_noise(n, 611), exp_ramp(n, 1900.0, 430.0), poles=2)
    cloth = mul(cloth, env_seg(n, [(0.0, 0.0), (0.04, 1.0), (0.30, 0.48), (1.0, 0.0)]))
    rustle = mul(band_pass(rng_noise(n, 612), 1800.0, 4500.0, 2),
                 env_seg(n, [(0.0, 0.0), (0.05, 0.8), (0.45, 0.25), (1.0, 0.0)]))
    thud = mul(osc_sine(n, exp_ramp(n, 156.0, 74.0)), env_perc(n, 0.004, 0.070))
    return add(add(scale(cloth, 1.0), scale(rustle, 0.16)), scale(thud, 0.50))


def make_switch_toggle():
    """Crisp electrical clack: two tiny transients, the second quieter."""
    n = dur_n(0.20)
    out = zeros(n)

    def click(at, amp, seed, bright):
        ln = n - at
        exc = mul(rng_noise(ln, seed), env_perc(ln, 0.0002, 0.0008))
        body = zeros(ln)
        for f, d, a in ((2400.0, 0.012, 1.00), (3600.0, 0.008, 0.55), (5200.0, 0.005, 0.30)):
            body = add(body, resonator(exc, f * bright, d, a))
        snap = mul(band_pass(rng_noise(ln, seed + 1), 2500.0, 9000.0, 2),
                   env_perc(ln, 0.0003, 0.0022))
        addmix(out, add(body, scale(snap, 0.9)), at, amp)

    click(0, 1.00, 621, 1.00)
    click(dur_n(0.038), 0.42, 623, 0.88)
    tick = mul(low_pass(rng_noise(n, 625), 420.0, 2), env_perc(n, 0.001, 0.012))
    return add(out, scale(tick, 0.25))


def make_guard_suspicious():
    """Short rising two-tone 'hm?' - a question, not an alarm."""
    n = dur_n(0.50)
    out = zeros(n)
    n1 = dur_n(0.20)
    v1 = add(scale(osc_tri(n1, E4), 0.5), scale(osc_sine(n1, E4), 0.5))
    addmix(out, mul(v1, env_perc(n1, 0.012, 0.085)), 0, 0.75)
    at = dur_n(0.20)
    n2 = n - at
    glide = freq_seg(n2, [(0.0, G4), (0.55, G4 * 1.03), (1.0, C5)])
    v2 = add(scale(osc_tri(n2, glide), 0.55), scale(osc_sine(n2, glide), 0.45))
    addmix(out, mul(v2, env_perc(n2, 0.018, 0.13)), at, 1.0)
    return low_pass(out, 3800.0, 1)


def make_guard_alert():
    """Alarm sting: a hard fall then a steep rise, with a noise riser."""
    n = dur_n(0.80)
    glide = freq_seg(n, [(0.0, A5 * 1.12), (0.32, E4), (0.40, E4 * 1.06), (1.0, C6)])
    lead = add(scale(osc_tri(n, glide), 0.6), scale(osc_sine(n, glide), 0.4))
    detuned = osc_tri(n, mul(glide, 1.006))
    tone = soft_sat(add(scale(lead, 1.0), scale(detuned, 0.35)), 1.6)
    tone = mul(tone, env_seg(n, [(0.0, 0.0), (0.012, 1.0), (0.30, 0.82),
                                 (0.45, 0.95), (0.88, 0.9), (1.0, 0.0)]))
    riser = band_pass(rng_noise(n, 701),
                      exp_ramp(n, 700.0, 2600.0), exp_ramp(n, 2400.0, 9000.0), 2)
    riser = mul(riser, env_seg(n, [(0.0, 0.0), (0.40, 0.10), (1.0, 0.85)]))
    hit = mul(osc_sine(n, exp_ramp(n, 210.0, 70.0)), env_perc(n, 0.001, 0.075))
    return add(add(scale(tone, 1.0), scale(riser, 0.30)), scale(hit, 0.45))


def make_guard_lost():
    """Falling, deflating two-tone: the guard gives up."""
    n = dur_n(0.70)
    out = zeros(n)
    n1 = dur_n(0.28)
    v1 = add(scale(osc_tri(n1, A4), 0.5), scale(osc_sine(n1, A4), 0.5))
    addmix(out, mul(v1, env_perc(n1, 0.012, 0.11)), 0, 1.0)
    at = dur_n(0.27)
    n2 = n - at
    glide = freq_seg(n2, [(0.0, E4), (0.45, E4 * 0.97), (1.0, B3)])
    v2 = add(scale(osc_tri(n2, glide), 0.5), scale(osc_sine(n2, glide), 0.5))
    addmix(out, mul(v2, env_perc(n2, 0.020, 0.16)), at, 0.85)
    sigh = mul(low_pass(rng_noise(n, 711), exp_ramp(n, 900.0, 260.0), 2),
               env_seg(n, [(0.0, 0.0), (0.45, 0.5), (1.0, 0.0)]))
    out = add(out, scale(sigh, 0.14))
    return low_pass(out, exp_ramp(n, 2600.0, 700.0), 1)


def make_camera_beep():
    """Thin electronic beep around 1.7 kHz."""
    n = dur_n(0.15)
    tone = add(scale(osc_sine(n, 1700.0), 1.0), scale(osc_sine(n, 3400.0), 0.12))
    return mul(tone, env_perc(n, 0.003, 0.038))


def make_win_jingle():
    """Calm resolved C-major arpeggio over a soft pad (relative major of Am)."""
    n = dur_n(1.80)
    out = zeros(n)
    for hz, at, amp in ((C4, 0.00, 0.80), (E4, 0.16, 0.85), (G4, 0.32, 0.90), (C5, 0.48, 1.00)):
        ln = n - dur_n(at)
        voice = add(scale(osc_sine(ln, hz), 0.6), scale(osc_tri(ln, hz), 0.4))
        voice = add(voice, scale(osc_sine(ln, hz * 2.0), 0.14))
        addmix(out, mul(voice, env_perc(ln, 0.008, 0.42)), dur_n(at), amp)
    pad = zeros(n)
    for hz, amp in ((C3, 1.0), (G3, 0.55), (E4, 0.35), (C4, 0.45)):
        pad = add(pad, scale(osc_sine(n, hz), amp))
    pad = mul(low_pass(pad, 1100.0, 2),
              env_seg(n, [(0.0, 0.0), (0.18, 0.9), (0.62, 1.0), (1.0, 0.0)]))
    shimmer = mul(osc_sine(n, C6), env_seg(n, [(0.0, 0.0), (0.45, 0.0), (0.60, 0.7), (1.0, 0.0)]))
    return add(add(scale(out, 1.0), scale(pad, 0.42)), scale(shimmer, 0.07))


def make_lose_sting():
    """Dark minor fall over a low drone."""
    n = dur_n(1.50)
    drone = add(scale(osc_sine(n, A1), 1.0), scale(osc_sine(n, E2), 0.35))
    drone = add(drone, scale(osc_sine(n, A2), 0.22))
    drone = mul(low_pass(drone, 420.0, 2),
                env_seg(n, [(0.0, 0.0), (0.05, 1.0), (0.55, 0.8), (1.0, 0.0)]))
    glide = freq_seg(n, [(0.0, A3), (0.30, A3), (0.45, F3), (0.70, F3), (0.85, D3), (1.0, D3 * 0.96)])
    lead = add(scale(osc_tri(n, glide), 0.6), scale(osc_sine(n, glide), 0.4))
    lead = mul(lead, env_seg(n, [(0.0, 0.0), (0.03, 1.0), (0.45, 0.72), (0.80, 0.45), (1.0, 0.0)]))
    lead = low_pass(lead, exp_ramp(n, 2400.0, 520.0), 1)
    swell = mul(low_pass(rng_noise(n, 801), 380.0, 3),
                env_seg(n, [(0.0, 0.0), (0.25, 0.7), (1.0, 0.0)]))
    return add(add(scale(drone, 0.85), scale(lead, 1.0)), scale(swell, 0.20))


def make_ui_click():
    """Tight UI click."""
    n = dur_n(0.08)
    exc = mul(rng_noise(n, 901), env_perc(n, 0.0002, 0.0006))
    body = add(resonator(exc, 1150.0, 0.010, 1.0), resonator(exc, 1900.0, 0.006, 0.5))
    tick = mul(band_pass(rng_noise(n, 902), 2000.0, 8000.0, 2), env_perc(n, 0.0003, 0.0030))
    return add(scale(body, 1.0), scale(tick, 0.55))


def make_ui_hover():
    """Very quiet high tick."""
    n = dur_n(0.06)
    tone = add(scale(osc_sine(n, 2600.0), 1.0), scale(osc_sine(n, 3900.0), 0.2))
    tone = mul(tone, env_perc(n, 0.0008, 0.0095))
    air = mul(band_pass(rng_noise(n, 911), 3000.0, 9000.0, 2), env_perc(n, 0.0003, 0.0035))
    return add(scale(tone, 1.0), scale(air, 0.22))


# =========================================================================== #
#  SEAMLESS LOOPS
# =========================================================================== #
#  Every tonal layer uses `loop_tone`/`lfo` (an exact whole number of cycles per
#  loop), every noise bed is filtered cyclically, and every transient is mixed
#  with `addmix_wrap`. Nothing therefore needs a crossfade at the seam.

def make_ambient_loop(duration=12.0):
    """Dark night-yard drone: low sustained tone under slow filtered wind."""
    n = dur_n(duration)

    drone = zeros(n)
    for hz, amp in ((A1, 1.00), (A1 * 1.003, 0.28), (A2, 0.42), (E2, 0.20), (A3, 0.08)):
        drone = add(drone, scale(loop_tone(n, hz, duration), amp))
    drone = mul(drone, lfo(n, 1, phase=0.0, lo=0.78, hi=1.0))
    drone = loop_low_pass(drone, lfo(n, 1, phase=1.1, lo=190.0, hi=330.0), poles=2)

    wind = loop_band_pass(rng_noise(n, 1001),
                          lfo(n, 2, phase=0.4, lo=170.0, hi=280.0),
                          lfo(n, 3, phase=2.0, lo=700.0, hi=1700.0), 2)
    wind = mul(wind, lfo(n, 2, phase=0.9, lo=0.25, hi=1.0))

    air = loop_band_pass(rng_noise(n, 1002), 2600.0, 6000.0, 2)
    air = mul(air, lfo(n, 1, phase=3.0, lo=0.15, hi=1.0))

    return add(add(scale(drone, 1.0), scale(wind, 0.55)), scale(air, 0.045))


def make_tension_loop(duration=8.0):
    """Same key, darker, with a slow heartbeat-ish low pulse underneath."""
    n = dur_n(duration)

    drone = zeros(n)
    for hz, amp in ((A1, 1.00), (A1 * 1.004, 0.30), (C2, 0.30), (A2, 0.26), (C3, 0.10)):
        drone = add(drone, scale(loop_tone(n, hz, duration), amp))
    drone = loop_low_pass(drone, lfo(n, 1, phase=0.3, lo=170.0, hi=290.0), poles=2)
    drone = mul(drone, lfo(n, 2, phase=1.7, lo=0.72, hi=1.0))

    # 8 heartbeats over 8 s = 60 BPM, each a lub-dub pair.
    beats = int(round(duration))
    pulse = zeros(n)
    bl = dur_n(0.42)
    for b in range(beats):
        t0 = b * (n / float(beats))
        for off, amp, f0, f1 in ((0.00, 1.00, 62.0, 36.0), (0.255, 0.55, 54.0, 33.0)):
            thump = mul(osc_sine(bl, exp_ramp(bl, f0, f1)), env_perc(bl, 0.006, 0.085))
            thump = low_pass(thump, 180.0, 2)
            addmix_wrap(pulse, thump, int(t0 + off * SR), amp)

    breath = loop_band_pass(rng_noise(n, 1101),
                            lfo(n, 2, phase=0.0, lo=200.0, hi=360.0),
                            lfo(n, 3, phase=1.2, lo=900.0, hi=2100.0), 2)
    breath = mul(breath, lfo(n, 2, phase=2.4, lo=0.20, hi=1.0))

    unease = mul(scale(loop_tone(n, E4, duration), 1.0), lfo(n, 3, phase=0.7, lo=0.0, hi=1.0))
    unease = loop_low_pass(unease, 2000.0, 1)

    return add(add(add(scale(drone, 1.0), scale(pulse, 0.95)),
                   scale(breath, 0.40)), scale(unease, 0.05))


def make_chase_loop(duration=6.0, bpm=130.0):
    """Driving pulse at 130 BPM - 13 beats fit 6.000 s exactly."""
    n = dur_n(duration)
    beats = int(round(duration * bpm / 60.0))   # 13
    spb = n / float(beats)

    pulse = zeros(n)
    kl = dur_n(0.20)
    tl = dur_n(0.09)
    for b in range(beats):
        t0 = b * spb
        kick = mul(osc_sine(kl, exp_ramp(kl, 118.0, A1)), env_perc(kl, 0.002, 0.075))
        kick = add(kick, scale(mul(band_pass(rng_noise(kl, 1200 + b), 1500.0, 5000.0, 2),
                                   env_perc(kl, 0.0004, 0.0045)), 0.22))
        addmix_wrap(pulse, kick, int(t0), 1.0)
        tick = mul(band_pass(rng_noise(tl, 1300 + b), 2200.0, 7000.0, 2),
                   env_perc(tl, 0.0006, 0.010))
        addmix_wrap(pulse, tick, int(t0 + spb * 0.5), 0.26)

    bass = add(scale(loop_tone(n, A1, duration), 1.0), scale(loop_tone(n, A2, duration), 0.35))
    bass = mul(bass, lfo(n, beats, phase=-math.pi / 2.0, lo=0.25, hi=1.0))
    bass = loop_low_pass(bass, 300.0, 2)

    tense = add(scale(loop_tone(n, A2, duration), 0.6), scale(loop_tone(n, E3, duration), 0.4))
    tense = mul(tense, lfo(n, 1, phase=-math.pi / 2.0, lo=0.10, hi=1.0))
    tense = loop_low_pass(tense, lfo(n, 1, phase=-math.pi / 2.0, lo=420.0, hi=1800.0), 2)

    return add(add(scale(pulse, 1.0), scale(bass, 0.55)), scale(tense, 0.22))


def make_goal_hum(duration=4.0):
    """Warm electrical hum for the extraction beacon."""
    n = dur_n(duration)
    hum = zeros(n)
    for mult, amp in ((1.0, 1.00), (2.0, 0.34), (3.0, 0.17), (4.0, 0.08), (5.0, 0.04)):
        hum = add(hum, scale(loop_tone(n, A2 * mult, duration), amp))
    hum = add(hum, scale(loop_tone(n, A1, duration), 0.28))
    hum = mul(hum, lfo(n, 2, phase=0.0, lo=0.88, hi=1.0))
    hum = mul(hum, lfo(n, 3, phase=1.4, lo=0.94, hi=1.0))
    hum = loop_low_pass(hum, lfo(n, 1, phase=0.5, lo=1200.0, hi=2000.0), poles=2)

    sizzle = loop_band_pass(rng_noise(n, 1401), 2200.0, 5200.0, 2)
    sizzle = mul(sizzle, lfo(n, 2, phase=2.2, lo=0.3, hi=1.0))

    return add(scale(hum, 1.0), scale(sizzle, 0.045))


# =========================================================================== #
#  SPRITES - signed distance fields with analytic coverage
# =========================================================================== #
#  Every shape is expressed as a signed distance in pixels; the alpha of a pixel
#  is clamp(0.5 - d, 0, 1), i.e. the exact coverage of a straight edge crossing
#  that pixel. This anti-aliases better than 4x supersampling and costs less.

def grid(w, h):
    """Flat pixel-centre coordinate arrays of length w*h."""
    if HAVE_NUMPY:
        idx = _np.arange(w * h)
        return (idx % w) + 0.5, (idx // w) + 0.5
    xs = [0.0] * (w * h)
    ys = [0.0] * (w * h)
    i = 0
    for y in range(h):
        for x in range(w):
            xs[i] = x + 0.5
            ys[i] = y + 0.5
            i += 1
    return xs, ys


def coverage(d):
    """Signed distance (px, negative inside) -> anti-aliased alpha."""
    return v_clip(sub(0.5, d), 0.0, 1.0)


# ---- primitives ------------------------------------------------------------

def sd_circle(xs, ys, cx, cy, r):
    return sub(v_hypot(sub(xs, cx), sub(ys, cy)), r)


def sd_ellipse(xs, ys, cx, cy, rx, ry):
    """Approximate ellipse SDF - accurate enough for sub-pixel coverage."""
    k = v_hypot(scale(sub(xs, cx), 1.0 / rx), scale(sub(ys, cy), 1.0 / ry))
    return scale(sub(k, 1.0), min(rx, ry))


def sd_box(xs, ys, cx, cy, hx, hy):
    qx = sub(v_abs(sub(xs, cx)), hx)
    qy = sub(v_abs(sub(ys, cy)), hy)
    outside = v_hypot(v_max2(qx, 0.0), v_max2(qy, 0.0))
    inside = v_min2(v_max2(qx, qy), 0.0)
    return add(outside, inside)


def sd_round_box(xs, ys, cx, cy, hx, hy, r):
    return sub(sd_box(xs, ys, cx, cy, hx - r, hy - r), r)


def sd_segment(xs, ys, x0, y0, x1, y1, r):
    """Capsule: the swept disc of radius r from (x0,y0) to (x1,y1)."""
    dx, dy = x1 - x0, y1 - y0
    dd = dx * dx + dy * dy
    px, py = sub(xs, x0), sub(ys, y0)
    if dd < 1e-9:
        return sub(v_hypot(px, py), r)
    t = v_clip(scale(add(scale(px, dx), scale(py, dy)), 1.0 / dd), 0.0, 1.0)
    return sub(v_hypot(sub(px, scale(t, dx)), sub(py, scale(t, dy))), r)


def sd_convex(xs, ys, pts):
    """Convex polygon as the max of its outward half-planes (exact inside)."""
    cx = sum(p[0] for p in pts) / len(pts)
    cy = sum(p[1] for p in pts) / len(pts)
    d = None
    m = len(pts)
    for i in range(m):
        ax, ay = pts[i]
        bx, by = pts[(i + 1) % m]
        ex, ey = bx - ax, by - ay
        ln = math.hypot(ex, ey)
        if ln < 1e-9:
            continue
        nx, ny = ey / ln, -ex / ln
        if nx * (cx - ax) + ny * (cy - ay) > 0.0:   # force outward normals
            nx, ny = -nx, -ny
        hp = add(scale(sub(xs, ax), nx), scale(sub(ys, ay), ny))
        d = hp if d is None else v_max2(d, hp)
    return d


def op_union(*ds):
    out = ds[0]
    for d in ds[1:]:
        out = v_min2(out, d)
    return out


def op_inter(a, b):
    return v_max2(a, b)


def op_sub(a, b):
    return v_max2(a, neg(b))


def op_outline(d, half):
    """Turn a filled shape into a stroke of total width 2*half."""
    return sub(v_abs(d), half)


def op_smooth_union(a, b, k):
    """Polynomial smooth minimum - gives organic, pebble-like blends."""
    hh = v_clip(add(scale(sub(b, a), 0.5 / k), 0.5), 0.0, 1.0)
    lin = add(mul(b, sub(1.0, hh)), mul(a, hh))
    return sub(lin, scale(mul(hh, sub(1.0, hh)), k))


# ---- PNG output ------------------------------------------------------------

def _rgba_bytes(alpha, n):
    """White RGB everywhere (no dark halo when Unity filters), shape in alpha."""
    if HAVE_NUMPY:
        a = (_np.clip(_np.asarray(alpha, dtype=_np.float64), 0.0, 1.0) * 255.0 + 0.5)
        buf = _np.empty((n, 4), dtype=_np.uint8)
        buf[:, 0:3] = 255
        buf[:, 3] = a.astype(_np.uint8)
        return buf.tobytes()
    out = bytearray(n * 4)
    for i, v in enumerate(alpha):
        v = 0.0 if v < 0.0 else (1.0 if v > 1.0 else v)
        j = i * 4
        out[j] = out[j + 1] = out[j + 2] = 255
        out[j + 3] = int(v * 255.0 + 0.5)
    return bytes(out)


def _png_chunk(tag, data):
    return (struct.pack(">I", len(data)) + tag + data +
            struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))


def write_png(path, w, h, alpha):
    rgba = _rgba_bytes(alpha, w * h)
    if HAVE_PIL:
        _Image.frombytes("RGBA", (w, h), rgba).save(path, "PNG", optimize=True)
        return
    stride = w * 4
    raw = bytearray()
    for y in range(h):
        raw.append(0)                       # filter type 0 (None) on every row
        raw += rgba[y * stride:(y + 1) * stride]
    blob = b"\x89PNG\r\n\x1a\n"
    blob += _png_chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
    blob += _png_chunk(b"IDAT", zlib.compress(bytes(raw), 9))
    blob += _png_chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(blob)


# ---- sprite families -------------------------------------------------------

def sprite_circle_fill():
    w = h = 256
    xs, ys = grid(w, h)
    return w, h, coverage(sd_circle(xs, ys, 128.0, 128.0, 0.96 * 128.0))


def sprite_ring(size, stroke_frac):
    w = h = size
    c = size / 2.0
    xs, ys = grid(w, h)
    outer = 0.96 * c
    rc = outer / (1.0 + stroke_frac * 0.5)          # centre-line radius
    half = rc * stroke_frac * 0.5
    return w, h, coverage(op_outline(sd_circle(xs, ys, c, c, rc), half))


def sprite_arrow():
    w = h = 128
    xs, ys = grid(w, h)
    body = sd_convex(xs, ys, [(64.0, 8.0), (118.0, 116.0), (10.0, 116.0)])
    notch = sd_convex(xs, ys, [(64.0, 96.0), (86.0, 126.0), (42.0, 126.0)])
    return w, h, coverage(op_sub(body, notch))


def sprite_soft_glow():
    w = h = 256
    xs, ys = grid(w, h)
    r = v_hypot(sub(xs, 128.0), sub(ys, 128.0))
    core, edge = 0.10 * 128.0, 128.0
    t = v_clip(scale(sub(r, core), 1.0 / (edge - core)), 0.0, 1.0)
    falloff = sub(1.0, mul(t, t))
    return w, h, mul(falloff, falloff)             # (1 - t^2)^2, zero at the rim


def sprite_vignette():
    w = h = 512
    xs, ys = grid(w, h)
    r = v_hypot(sub(xs, 256.0), sub(ys, 256.0))
    r0, r1 = 0.62 * 256.0, 1.05 * 256.0
    u = v_clip(scale(sub(r, r0), 1.0 / (r1 - r0)), 0.0, 1.0)
    s = mul(mul(u, u), sub(3.0, scale(u, 2.0)))            # smoothstep
    return w, h, mul(s, s)                                 # squared: clear centre, tight edge


def sprite_panel():
    w = h = 64
    xs, ys = grid(w, h)
    return w, h, coverage(sd_round_box(xs, ys, 32.0, 32.0, 32.0, 32.0, 16.0))


def sprite_panel_outline():
    w = h = 64
    xs, ys = grid(w, h)
    path = sd_round_box(xs, ys, 32.0, 32.0, 30.5, 30.5, 14.5)
    return w, h, coverage(op_outline(path, 1.5))   # 3 px stroke, hollow inside


def sprite_bar_fill():
    w = h = 32
    return w, h, ones(w * h)


def sprite_icon_eye():
    w = h = 128
    xs, ys = grid(w, h)
    rr, k = 65.0, 39.0                              # lens: 104 x 52 px
    lens = op_inter(sd_circle(xs, ys, 64.0, 64.0 + k, rr),
                    sd_circle(xs, ys, 64.0, 64.0 - k, rr))
    return w, h, coverage(op_union(op_outline(lens, 5.0),
                                   sd_circle(xs, ys, 64.0, 64.0, 15.0)))


def sprite_icon_footstep():
    w = h = 128
    xs, ys = grid(w, h)
    sole = sd_ellipse(xs, ys, 58.0, 86.0, 27.0, 32.0)
    toes = op_union(sd_circle(xs, ys, 40.0, 40.0, 11.0),
                    sd_circle(xs, ys, 62.0, 33.0, 8.5),
                    sd_circle(xs, ys, 81.0, 37.0, 8.0),
                    sd_circle(xs, ys, 96.0, 46.0, 7.5))
    return w, h, coverage(op_union(sole, toes))


def sprite_icon_stone():
    w = h = 128
    xs, ys = grid(w, h)
    blobs = [(60.0, 70.0, 30.0), (86.0, 64.0, 22.0), (72.0, 92.0, 24.0),
             (40.0, 84.0, 20.0), (72.0, 44.0, 20.0)]
    rock = sd_circle(xs, ys, *blobs[0])
    for b in blobs[1:]:
        rock = op_smooth_union(rock, sd_circle(xs, ys, *b), 10.0)
    highlight = sd_segment(xs, ys, 50.0, 54.0, 70.0, 46.0, 5.0)
    return w, h, coverage(op_sub(rock, highlight))


def sprite_icon_intel():
    w = h = 128
    xs, ys = grid(w, h)
    page = sd_round_box(xs, ys, 64.0, 66.0, 36.0, 46.0, 6.0)
    fold = sd_convex(xs, ys, [(78.0, 14.0), (106.0, 14.0), (106.0, 42.0)])
    doc = op_sub(page, fold)
    doc = op_sub(doc, sd_segment(xs, ys, 42.0, 74.0, 86.0, 74.0, 5.0))
    doc = op_sub(doc, sd_segment(xs, ys, 42.0, 94.0, 76.0, 94.0, 5.0))
    return w, h, coverage(doc)


def sprite_icon_crouch():
    w = h = 128
    xs, ys = grid(w, h)
    # Side view. The knee sits *above* the hip and the torso leans forward -
    # that is what separates a crouch from a figure sitting on a chair.
    figure = op_union(
        sd_circle(xs, ys, 44.0, 34.0, 14.0),                       # head
        sd_segment(xs, ys, 50.0, 48.0, 62.0, 78.0, 11.0),          # leaning torso
        sd_segment(xs, ys, 60.0, 82.0, 94.0, 62.0, 11.0),          # thigh, knee up
        sd_segment(xs, ys, 94.0, 62.0, 90.0, 98.0, 9.5),           # shin
        sd_segment(xs, ys, 82.0, 104.0, 106.0, 104.0, 6.5),        # foot
        sd_segment(xs, ys, 58.0, 84.0, 46.0, 98.0, 8.5),           # trailing heel
    )
    return w, h, coverage(figure)


def sprite_icon_lock():
    w = h = 128
    xs, ys = grid(w, h)
    body = sd_round_box(xs, ys, 64.0, 86.0, 36.0, 28.0, 8.0)
    keyhole = op_union(sd_circle(xs, ys, 64.0, 80.0, 8.0),
                       sd_segment(xs, ys, 64.0, 80.0, 64.0, 98.0, 4.5))
    body = op_sub(body, keyhole)
    shackle = op_inter(op_outline(sd_circle(xs, ys, 64.0, 58.0, 21.0), 5.0),
                       sub(ys, 58.0))                              # upper half only
    stubs = op_union(sd_segment(xs, ys, 43.0, 58.0, 43.0, 64.0, 5.0),
                     sd_segment(xs, ys, 85.0, 58.0, 85.0, 64.0, 5.0))
    return w, h, coverage(op_union(body, shackle, stubs))


def sprite_crosshair():
    w = h = 64
    xs, ys = grid(w, h)
    dot = op_outline(sd_circle(xs, ys, 32.0, 32.0, 7.0), 2.0)
    ticks = op_union(
        sd_segment(xs, ys, 32.0, 11.0, 32.0, 19.0, 2.0),
        sd_segment(xs, ys, 32.0, 45.0, 32.0, 53.0, 2.0),
        sd_segment(xs, ys, 11.0, 32.0, 19.0, 32.0, 2.0),
        sd_segment(xs, ys, 45.0, 32.0, 53.0, 32.0, 2.0),
    )
    return w, h, coverage(op_union(dot, ticks))


# =========================================================================== #
#  Registries
# =========================================================================== #

ONE_SHOTS = [
    ("footstep_soft_1.wav", lambda: make_footstep_soft(101)),
    ("footstep_soft_2.wav", lambda: make_footstep_soft(102)),
    ("footstep_soft_3.wav", lambda: make_footstep_soft(103)),
    ("footstep_1.wav", lambda: make_footstep_concrete(111)),
    ("footstep_2.wav", lambda: make_footstep_concrete(112)),
    ("footstep_3.wav", lambda: make_footstep_concrete(113)),
    ("footstep_gravel_1.wav", lambda: make_footstep_gravel(121)),
    ("footstep_gravel_2.wav", lambda: make_footstep_gravel(122)),
    ("footstep_gravel_3.wav", lambda: make_footstep_gravel(123)),
    ("footstep_metal_1.wav", lambda: make_footstep_metal(131, 1.000)),
    ("footstep_metal_2.wav", lambda: make_footstep_metal(132, 1.062)),
    ("footstep_metal_3.wav", lambda: make_footstep_metal(133, 0.947)),
    ("stone_throw.wav", make_stone_throw),
    ("stone_impact.wav", make_stone_impact),
    ("pickup_stone.wav", make_pickup_stone),
    ("pickup_intel.wav", make_pickup_intel),
    ("hide_enter.wav", make_hide_enter),
    ("hide_exit.wav", make_hide_exit),
    ("switch_toggle.wav", make_switch_toggle),
    ("guard_suspicious.wav", make_guard_suspicious),
    ("guard_alert.wav", make_guard_alert),
    ("guard_lost.wav", make_guard_lost),
    ("camera_beep.wav", make_camera_beep),
    ("win_jingle.wav", make_win_jingle),
    ("lose_sting.wav", make_lose_sting),
    ("ui_click.wav", make_ui_click),
    ("ui_hover.wav", make_ui_hover),
]

LOOPS = [
    ("ambient_loop.wav", make_ambient_loop),
    ("tension_loop.wav", make_tension_loop),
    ("chase_loop.wav", make_chase_loop),
    ("goal_hum.wav", make_goal_hum),
]

SPRITES = [
    ("circle_fill.png", sprite_circle_fill),
    ("ring.png", lambda: sprite_ring(256, 0.10)),
    ("ring_thin.png", lambda: sprite_ring(256, 0.04)),
    ("arrow.png", sprite_arrow),
    ("soft_glow.png", sprite_soft_glow),
    ("vignette.png", sprite_vignette),
    ("panel.png", sprite_panel),
    ("panel_outline.png", sprite_panel_outline),
    ("bar_fill.png", sprite_bar_fill),
    ("icon_eye.png", sprite_icon_eye),
    ("icon_footstep.png", sprite_icon_footstep),
    ("icon_stone.png", sprite_icon_stone),
    ("icon_intel.png", sprite_icon_intel),
    ("icon_crouch.png", sprite_icon_crouch),
    ("icon_lock.png", sprite_icon_lock),
    ("crosshair.png", sprite_crosshair),
]


# =========================================================================== #
#  Verification helpers
# =========================================================================== #

def verify_wav(path):
    """Read the written file back and measure what the spec cares about.

    `seam_ratio` is the jump across the loop point divided by the largest
    ordinary sample-to-sample step in the file. A seamless loop scores <= ~1:
    the wrap is just another normal step. A discontinuity scores far higher.
    """
    with wave.open(path, "rb") as r:
        ch, sw, sr, nf = r.getnchannels(), r.getsampwidth(), r.getframerate(), r.getnframes()
        raw = r.readframes(nf)
    buf = array.array("h")
    buf.frombytes(raw)
    if sys.byteorder == "big":
        buf.byteswap()

    peak, max_step, prev = 0, 1, buf[0] if nf else 0
    for v in buf:
        a = -v if v < 0 else v
        if a > peak:
            peak = a
        d = v - prev
        if d < 0:
            d = -d
        if d > max_step:
            max_step = d
        prev = v
    peak_db = 20.0 * math.log10(peak / 32767.0) if peak else -999.0
    seam = abs(buf[0] - buf[-1]) if nf > 1 else 0
    return {
        "channels": ch, "bits": sw * 8, "rate": sr, "frames": nf,
        "seconds": nf / float(sr), "peak_dbfs": peak_db,
        "seam": seam / 32767.0, "seam_ratio": seam / float(max_step),
        "first": (buf[0] / 32767.0) if nf else 0.0,
        "last": (buf[-1] / 32767.0) if nf else 0.0,
        "bytes": os.path.getsize(path),
    }


def _unfilter_rgba(raw, w, h):
    """Undo the five PNG scanline filters for 8-bit RGBA (bpp = 4)."""
    stride = w * 4
    out = bytearray(stride * h)
    prior = bytearray(stride)
    pos = 0
    for y in range(h):
        ft = raw[pos]
        pos += 1
        line = bytearray(raw[pos:pos + stride])
        pos += stride
        if ft == 1:
            for i in range(4, stride):
                line[i] = (line[i] + line[i - 4]) & 0xFF
        elif ft == 2:
            for i in range(stride):
                line[i] = (line[i] + prior[i]) & 0xFF
        elif ft == 3:
            for i in range(stride):
                a = line[i - 4] if i >= 4 else 0
                line[i] = (line[i] + ((a + prior[i]) >> 1)) & 0xFF
        elif ft == 4:
            for i in range(stride):
                a = line[i - 4] if i >= 4 else 0
                b = prior[i]
                c = prior[i - 4] if i >= 4 else 0
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                pr = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                line[i] = (line[i] + pr) & 0xFF
        elif ft != 0:
            raise ValueError("unknown PNG filter %d" % ft)
        out[y * stride:(y + 1) * stride] = line
        prior = line
    return out


def verify_png(path):
    """Parse the file back off disk: header, colour type, alpha extremes.

    Fully decodes the IDAT stream (including Pillow's adaptive row filters) so
    the check is honest regardless of which backend wrote the file.
    """
    with open(path, "rb") as f:
        blob = f.read()
    if blob[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("not a PNG: %s" % path)
    pos, idat, info = 8, bytearray(), {}
    while pos + 8 <= len(blob):
        ln = struct.unpack(">I", blob[pos:pos + 4])[0]
        tag = blob[pos + 4:pos + 8]
        data = blob[pos + 8:pos + 8 + ln]
        if tag == b"IHDR":
            w, h, depth, ctype, comp, filt, interlace = struct.unpack(">IIBBBBB", data[:13])
            info.update(width=w, height=h, depth=depth, ctype=ctype, interlace=interlace)
        elif tag == b"IDAT":
            idat += data
        elif tag == b"IEND":
            break
        pos += 12 + ln
    if info.get("ctype") != 6 or info.get("depth") != 8 or info.get("interlace") != 0:
        info.update(alpha_min=-1, alpha_max=-1, bytes=os.path.getsize(path),
                    has_alpha=(info.get("ctype") == 6))
        return info
    pixels = _unfilter_rgba(zlib.decompress(bytes(idat)), info["width"], info["height"])
    alphas = pixels[3::4]
    info.update(alpha_min=min(alphas), alpha_max=max(alphas),
                bytes=os.path.getsize(path), has_alpha=True)
    return info


# =========================================================================== #
#  main
# =========================================================================== #

def _fmt_bytes(b):
    return "%.1f KB" % (b / 1024.0) if b < 1024 * 1024 else "%.2f MB" % (b / 1048576.0)


def build_audio():
    os.makedirs(AUDIO_DIR, exist_ok=True)
    rows = []
    for name, builder in ONE_SHOTS:
        sig = normalize(apply_fades(builder()), PEAK_DBFS)
        trim = TRIM_DB.get(name, 0.0)
        if trim:
            sig = scale(sig, 10.0 ** (trim / 20.0))
        path = os.path.join(AUDIO_DIR, name)
        write_wav(path, sig)
        rows.append((name, "one-shot", verify_wav(path)))
        print("  audio  %-24s ok" % name)
    for name, builder in LOOPS:
        sig = normalize(builder(), PEAK_DBFS)       # no fades - it must loop
        trim = TRIM_DB.get(name, 0.0)
        if trim:
            sig = scale(sig, 10.0 ** (trim / 20.0))
        path = os.path.join(AUDIO_DIR, name)
        write_wav(path, sig)
        rows.append((name, "loop", verify_wav(path)))
        print("  audio  %-24s ok (loop)" % name)
    return rows


def build_sprites():
    os.makedirs(SPRITE_DIR, exist_ok=True)
    rows = []
    for name, builder in SPRITES:
        w, h, alpha = builder()
        path = os.path.join(SPRITE_DIR, name)
        write_png(path, w, h, alpha)
        rows.append((name, verify_png(path)))
        print("  sprite %-24s ok" % name)
    return rows


def report(audio_rows, sprite_rows):
    if audio_rows:
        print("\n%-24s %-9s %8s %8s %6s %5s %10s %9s" % (
            "FILE", "KIND", "SECONDS", "FRAMES", "RATE", "BITS", "PEAK dBFS", "SIZE"))
        print("-" * 88)
        for name, kind, v in audio_rows:
            print("%-24s %-9s %8.3f %8d %6d %5d %10.2f %9s" % (
                name, kind, v["seconds"], v["frames"], v["rate"], v["bits"],
                v["peak_dbfs"], _fmt_bytes(v["bytes"])))
        loops = [(n, v) for n, k, v in audio_rows if k == "loop"]
        if loops:
            print("\nLoop seam check - wrap step vs the largest ordinary step "
                  "(<= 1.0 means the seam is indistinguishable):")
            for n, v in loops:
                print("  %-24s jump %.6f full-scale   ratio %.3f"
                      % (n, v["seam"], v["seam_ratio"]))
    if sprite_rows:
        print("\n%-24s %11s %6s %7s %11s %9s" % (
            "FILE", "SIZE PX", "DEPTH", "RGBA", "ALPHA RANGE", "SIZE"))
        print("-" * 76)
        for name, v in sprite_rows:
            print("%-24s %5dx%-5d %6d %7s %5d..%-5d %9s" % (
                name, v["width"], v["height"], v["depth"],
                "yes" if v["has_alpha"] else "NO",
                v["alpha_min"], v["alpha_max"], _fmt_bytes(v["bytes"])))


def check(audio_rows, sprite_rows):
    """Hard assertions - the script fails loudly rather than shipping junk."""
    problems = []
    for name, kind, v in audio_rows:
        if v["bytes"] < 1024:
            problems.append("%s: file is suspiciously small" % name)
        if (v["channels"], v["bits"], v["rate"]) != (1, 16, SR):
            problems.append("%s: expected mono/16-bit/%d Hz" % (name, SR))
        if not (0.02 <= v["seconds"] <= 30.0):
            problems.append("%s: implausible duration %.3fs" % (name, v["seconds"]))
        if v["peak_dbfs"] < -30.0:
            problems.append("%s: nearly silent (%.1f dBFS)" % (name, v["peak_dbfs"]))
        if kind == "loop" and v["seam_ratio"] > 1.2:
            problems.append("%s: loop seam stands out (ratio %.3f)" % (name, v["seam_ratio"]))
        if kind == "one-shot" and (abs(v["first"]) > 0.002 or abs(v["last"]) > 0.002):
            problems.append("%s: one-shot does not start/end near zero" % name)
    for name, v in sprite_rows:
        if v["bytes"] < 64:
            problems.append("%s: file is suspiciously small" % name)
        if not v["has_alpha"] or v["depth"] != 8:
            problems.append("%s: not 8-bit RGBA" % name)
        if v["alpha_max"] < 250:
            problems.append("%s: shape never becomes opaque" % name)
    return problems


def main(argv):
    do_audio = "--sprites" not in argv
    do_sprites = "--audio" not in argv
    print("Night Shift asset generator")
    print("  python  : %s" % sys.version.split()[0])
    print("  numpy   : %s" % (_np.__version__ if HAVE_NUMPY else "not installed (stdlib fallback)"))
    print("  Pillow  : %s" % (_PIL.__version__ if HAVE_PIL else "not installed (stdlib fallback)"))
    print("  audio   -> %s" % AUDIO_DIR)
    print("  sprites -> %s\n" % SPRITE_DIR)

    t0 = time.time()
    audio_rows = build_audio() if do_audio else []
    sprite_rows = build_sprites() if do_sprites else []
    report(audio_rows, sprite_rows)

    problems = check(audio_rows, sprite_rows)
    print("\nGenerated %d audio + %d sprite files in %.1fs."
          % (len(audio_rows), len(sprite_rows), time.time() - t0))
    if problems:
        print("\nFAILED CHECKS:")
        for p in problems:
            print("  - %s" % p)
        return 1
    print("All checks passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
