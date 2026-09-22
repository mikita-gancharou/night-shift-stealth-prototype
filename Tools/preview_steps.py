"""Render a short preview of how the footsteps sound in game.

It lays the generated step clips out at the intervals PlayerNoise uses, applies the same
volume and pitch spread AudioManager applies, and mixes them over the ambient bed at its
in-game level. Handy for judging the mix without launching the build.

    python Tools/preview_steps.py            -> Docs/footstep_preview.wav
"""
import array
import math
import os
import random
import wave

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
AUDIO = os.path.join(ROOT, "Assets", "Audio")
OUT = os.path.join(ROOT, "Docs", "footstep_preview.wav")

SR = 44100
MASTER = 0.9  # AudioManager._masterVolume

# (label, clip prefix, how many clips, seconds between steps, AudioManager volume)
SEQUENCE = [
    ("crouch on concrete", "footstep_soft", 3, 0.62, 0.50),
    ("walk on concrete", "footstep", 3, 0.46, 0.70),
    ("sprint on concrete", "footstep", 3, 0.31, 0.90),
    ("walk on gravel", "footstep_gravel", 3, 0.46, 0.70),
    ("sprint on gravel", "footstep_gravel", 3, 0.31, 0.90),
    ("walk on metal", "footstep_metal", 3, 0.46, 0.70),
]

GAP = 0.55  # pause between sections


def read_wav(path):
    with wave.open(path, "rb") as reader:
        frames = reader.readframes(reader.getnframes())
    samples = array.array("h")
    samples.frombytes(frames)
    return [s / 32768.0 for s in samples]


def resample(samples, pitch):
    """Pitch shift the lazy way, exactly like an AudioSource pitch change: resample."""
    length = int(len(samples) / pitch)
    out = []
    for i in range(length):
        position = i * pitch
        left = int(position)
        right = min(left + 1, len(samples) - 1)
        frac = position - left
        out.append(samples[left] * (1.0 - frac) + samples[right] * frac)
    return out


def mix(destination, source, offset, gain):
    for i, value in enumerate(source):
        index = offset + i
        if 0 <= index < len(destination):
            destination[index] += value * gain


def main():
    random.seed(7)

    variants = {}
    for _, prefix, _, _, _ in SEQUENCE:
        if prefix in variants:
            continue
        clips = []
        for index in (1, 2, 3):
            path = os.path.join(AUDIO, "%s_%d.wav" % (prefix, index))
            if os.path.exists(path):
                clips.append(read_wav(path))
        variants[prefix] = clips

    total_seconds = sum(count * interval + GAP for _, _, count, interval, _ in SEQUENCE) + 1.0
    track = [0.0] * int(total_seconds * SR)

    cursor = 0.5
    for label, prefix, count, interval, volume in SEQUENCE:
        print("%6.2f s  %s" % (cursor, label))
        for _ in range(count):
            clip = random.choice(variants[prefix])
            shifted = resample(clip, random.uniform(0.92, 1.08))
            mix(track, shifted, int(cursor * SR), volume * random.uniform(0.86, 1.14) * MASTER)
            cursor += interval
        cursor += GAP

    # Ambient bed at the level MusicDirector uses, so the steps are judged in context.
    ambient_path = os.path.join(AUDIO, "ambient_loop.wav")
    if os.path.exists(ambient_path):
        ambient = read_wav(ambient_path)
        position = 0
        while position < len(track):
            mix(track, ambient, position, 0.35)
            position += len(ambient)

    peak = max(abs(v) for v in track) or 1.0
    if peak > 0.99:
        track = [v / peak * 0.99 for v in track]

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    data = array.array("h", [int(max(-1.0, min(1.0, v)) * 32767) for v in track])
    with wave.open(OUT, "wb") as writer:
        writer.setnchannels(1)
        writer.setsampwidth(2)
        writer.setframerate(SR)
        writer.writeframes(data.tobytes())

    print("wrote %s (%.1f s, peak %.2f)" % (OUT, len(track) / SR, peak))


if __name__ == "__main__":
    main()
