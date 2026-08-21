# THE SECTION FRAME IS NOT INFERABLE — so the op measures itself

*21/08/2026, the last API item of the bridge round. A wrong section frame is invisible in every
count: the part exists, its section is right, its length is right to the micron, and its steel
points the wrong way.*

---

## What went wrong

`bridge_build.shapes` reads the frame out of a part's property bag and passes **props X → `ax`,
props Y → `ay`** with `rot=0`, because the axes carry the rotation. That is correct for **6,064
of the bridge's 6,240 shapes** and wrong for the rest.

The cost is geometric and it hides well. An 80×80 tube turned to the wrong angle carries a
bounding box up to `80·√2 − 80 = 33.1 mm` wider than it should — and that is the whole of the
34.05 mm error that sat in 176 shapes of the first rebuild, on parts whose cut planes I had
already spent an afternoon suspecting. (All eight subsets of a three-plane part gave the
identical body: the cuts were never the problem.)

1,229 shapes read back with the rotation sign flipped. Only those whose section is not symmetric
about that flip show it as geometry, which is why the body gate saw 176 and not 1,229.

## The measurement

One `L100X10`, source body span `[236.508, 1502.669, 3488.590]`, built eight times from the
**same two axes** — every order and every sign:

| frame | span error |
|---|---|
| `X, Y` | 9.092 mm |
| `Y, X` | 9.080 |
| `-X, Y` | 9.092 |
| **`X, -Y`** | **0.021** |
| **`-X, -Y`** | **0.021** |
| `-Y, X` | 9.080 |
| `Y, -X` | 9.080 |
| `-Y, -X` | 9.080 |

⇒ the sign matters as much as the order, and nothing in what the API exposes says which is
right. There is nothing to infer.

## So the op reports its own error

**v209:** `beam` and `plate9` accept **`wantspan=x,y,z`** and answer **`spanErr=<mm>`** — the
worst-axis difference between the bounding box the caller asked for and the one that got built.
Same philosophy as `drill`'s `made=`: report the delta, never the intention. Verified:

```
beam HE200B 1000 long, wantspan=1000,200,200 -> spanErr=0
                       wantspan=1000,200,300 -> spanErr=100
plate9 600x300x12,     wantspan=600,300,12   -> spanErr=0
                       wantspan=600,300,20   -> spanErr=8
```

Callers that pass no `wantspan` are untouched.

## And the client picks the frame by measuring

**`eb_api.beam_framed(name, catalog, p1, p2, ax, ay, wantspan, ...)`** builds candidates in
order — the two plain readings first, so a part whose frame was already right costs one call —
keeps the one with the smallest `spanErr`, erases every loser, and returns
`(handle, spanErr, which)`. On the three shapes that defeated the first rebuild:

| part | section | frame chosen | spanErr |
|---|---|---|---|
| `53E381` | L100X10 | `X,-Y` | **0.021 mm** |
| `17D879` | SHS80X80X3.6 | `Y,X` | **0.016 mm** |
| `562E48` | L70X7 | `X,-Y` | **0.028 mm** |

⭐ **The general rule this belongs to, and the one worth carrying forward:** where the API cannot
read a state back — a cut plane's offset, a section frame's handedness, a bolt's chosen length —
do not reason about it. Build every candidate, measure against the source, keep what fits. Three
separate defects in this model fell to that one move, and every hypothesis I reasoned my way to
instead (the flange selector, the reversed ray, the cut-plane flag, `DoForAll` consuming a
selection) was refuted by measurement afterwards.

## What this did NOT fix, stated plainly

Applied as a repair pass over the existing rebuild (`app/bridge_reframe.py`), the swapped frame
alone improved **62 of 170** off-shapes — 34.05 mm → 0.017 mm on the worst — and left 108 where
neither of the two orders helped. The pass tried two candidates; `beam_framed` tries eight, and
the L100X10 above is a case the two-candidate pass could only take from 9.09 to 9.08. Re-running
the repair through `beam_framed` would close more of them; it has not been run, and 299 plates
have never had the equivalent frame test at all.
