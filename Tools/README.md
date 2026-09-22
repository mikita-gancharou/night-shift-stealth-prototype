# Night Shift - procedural placeholder assets

`generate_assets.py` synthesises every placeholder sound and sprite the
prototype needs. Nothing is downloaded and no binary source assets are kept in
the repo: the script *is* the asset source.

This folder sits outside `Assets/` on purpose, so Unity never tries to compile
or import it.

## Re-running it

```
python Tools/generate_assets.py
```

from the project root (any working directory works - the script resolves its
output paths from its own location). It overwrites whatever is already there.

Flags:

| Flag         | Effect                       |
|--------------|------------------------------|
| *(none)*     | regenerate audio **and** sprites |
| `--audio`    | only the WAVs                |
| `--sprites`  | only the PNGs                |

It prints a table of everything it wrote and then re-reads each file off disk to
verify it (format, duration, peak level, loop seams, PNG colour type and alpha
range). A failed check makes the script exit with status 1, so it is safe to
call from a build step.

Takes about 7 seconds with numpy installed.

## Output

| Folder                    | Files                                            |
|---------------------------|--------------------------------------------------|
| `Assets/Audio/`           | 23 one-shots + 4 seamless loops, `.wav`          |
| `Assets/Art/Sprites/`     | 16 sprites, `.png`                               |

The script writes **only** to those two folders plus this one. It never creates
`.meta` files - Unity generates those on import.

## Dependencies

`numpy` and `Pillow` are used when they are importable, purely for speed. When
either is missing the script silently falls back to the standard library
(`wave` + `array` for audio, `zlib` + `struct` for PNG) and produces equivalent
files. **Do not `pip install` anything to make this work.** The banner at the
top of each run says which backend was used.

Everything is deterministic - every RNG is explicitly seeded - so a re-run
reproduces byte-identical assets rather than a new random set.

One caveat: determinism holds *within* a backend. numpy's generator and the
standard library's `random` produce different streams from the same seed, so
noise-based sounds come out as a different (but equivalent) realisation if you
switch backends. Purely tonal files - `camera_beep`, `guard_suspicious`,
`pickup_stone`, `pickup_intel`, `win_jingle` - are bit-identical either way,
and every sprite is pixel-identical either way.

## Audio conventions

- Mono, 44100 Hz, 16-bit PCM.
- Peak-normalised to **-3 dBFS**. A handful of files are then trimmed further
  because the design calls for them to be quiet or to sit under everything
  else; the trims live in `TRIM_DB` near the top of the script:
  `footstep_soft_*` -8 dB, `ui_click` -5 dB, `ui_hover` -12 dB,
  `ambient_loop` -6 dB, `tension_loop` -5 dB, `goal_hum` -6 dB.
  Remove an entry from `TRIM_DB` to get a flat -3 dBFS file.
- One-shots get a raised-cosine fade in/out (up to 3 / 4 ms) so they start and
  end on exact zero. The fade-in is automatically shortened on percussive
  sounds so it never flattens the transient, and it is applied *before*
  normalisation so the stated peak is the real peak.
- Loops get **no** fades - that would defeat the loop.

### How the loops stay seamless

Rather than crossfading the tail over the head, every layer is built to be
exactly periodic over the loop length:

- Tonal layers use `loop_tone()` / `lfo()`, which complete a whole number of
  cycles across the buffer (so the pitch snaps to the loop grid - at 12 s that
  is a 0.08 Hz grid, inaudible).
- Noise beds go through `cyclic_filter()`, which runs the filter over two
  concatenated copies and keeps the second. The filter state at the head then
  matches the state at the loop point. Every IIR pass inside a loop must use
  `loop_low_pass` / `loop_band_pass` for this reason - a plain `low_pass` leaves
  a start-up transient at the head that ticks audibly at the seam.
- Transients (heartbeats, chase pulses) are mixed with `addmix_wrap()`, so a
  decay tail that runs past the end reappears at the start.

The verification pass reports each loop's seam as a ratio against the largest
ordinary sample-to-sample step in the file. Anything at or below 1.0 means the
wrap is smaller than a normal step, i.e. inaudible.

### Key

Everything is in A minor (55 / 110 Hz roots, accents on A, C, E) so the four
loops can cross-fade into each other without clashing. `win_jingle` resolves to
C major, the relative major. `chase_loop` is 6.000 s = 13 beats at exactly
130 BPM, which is why its pulse tiles perfectly.

## Sprite conventions

- RGBA PNG, transparent background, **white RGB in every pixel** (including the
  transparent ones, so Unity's bilinear filtering never bleeds a dark halo).
  The shape lives entirely in the alpha channel - tint at runtime.
- All sizes are powers of two.
- Edges are anti-aliased **analytically**: each shape is a signed distance
  field, and a pixel's alpha is `clamp(0.5 - distance, 0, 1)`, the exact
  coverage of a straight edge crossing that pixel. This is sharper than
  supersampling and costs one pass instead of sixteen.
- Icon strokes are at least 8 px at 128x128 so they survive being drawn at
  32 px.

`panel.png` and `panel_outline.png` are meant to be 9-sliced: 16 px corner
radius, identical corners, flat middle. `panel_outline`'s 3 px stroke sits just
inside the canvas edge so the two line up when stacked.

## Adding a new asset

1. Write a `make_<name>()` (audio) or `sprite_<name>()` (sprite) function.
   Audio builders return a float array at any level; sprite builders return
   `(width, height, alpha)`.
2. Add it to the `ONE_SHOTS`, `LOOPS` or `SPRITES` list.
3. Re-run. Normalisation, fades, format and verification are handled for you.

For a new loop, build its layers with `loop_tone` / `lfo` / `loop_band_pass` /
`addmix_wrap` and the seam takes care of itself. The seam ratio in the report
will tell you immediately if you slipped.
