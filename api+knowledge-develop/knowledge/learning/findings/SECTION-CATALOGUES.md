# The section catalogues — what is actually installed

*Surveyed 07/08/2026 after Amir's instruction: "תגוון קצת עם הפרופילים — אל תתקע רק על HEB או
IPE. תנסה לגוון קצת ולצאת מהקופסא להכיר וללמוד עוד הרבה סוגים של חתכי פרופילים."*

He was right that this was a gap. Up to that point every model I built used **five** section
types. The machine has **357 catalogues**.

## The rule that matters most

> **A section key is an opaque string. Look it up; never construct it.**

The library is not internally consistent, and a wrong key fails quietly:

| trap | evidence |
|---|---|
| **German decimal comma** | `Peiner_HD :: HD260x68,15` · `DIN_C :: C150x75x6,5` · `DIN KALT-U :: 30/30x1,5` |
| …but **decimal point elsewhere** | `DIN RUNDROHR :: RO 219.1x6.3` — same library, same release |
| **embedded space** | `RO 219.1x8` has a space after `RO`; `RQ200x8` has none |
| **catalogue name ≠ what you type** | `DIN HALBE IPE` finds nothing; the real name is `DIN_HALBE IPE` (underscore, then space) |
| **the stored name is different again** | pass `DIN_QUADRATROHR` → the model stores `DIN.DIN_QUADROHR`; `DIN WINKEL VD` → `DIN.DIN_WINK_GL_VD`; `DIN FLACHEISEN` → `DIN.DIN_FLACH` |

### 🆕 Keys measured on 13/08/2026 (lesson 6 + the connection research)

| passed as | stored in the model | note |
|---|---|---|
| `SHS100X100X4` | **`BRITAIN.BS_CELSIUS_SHS`** | Amir's own choice for the small tube |
| `SHS200X200X5` | **`BRITAIN.BS_HYBOX_SHS`** | ⭐⭐ **a DIFFERENT British SHS catalogue for a different size, in the same session** — the strongest possible illustration of *look the key up, never construct it* |
| `U200` | **`FRANCE.FR_UPN`** | the channel he used; `rot` read back as `37.5` while the envelope was axis-aligned |
| `HE200A` / `HE300B` | `DIN.DIN_HEA` / `DIN.DIN_HEB` | catalogues `DIN_HEA` / `DIN_HEB` |
| `IPE300` / `IPE500` | `DIN.DIN_IPE` | |
| `FL 150x12` | `DIN.DIN_FLACH` | note the **space** in the passed name |
| `EA70X70X8` · `110X10` · `200X10` | — | ⚠️ produced **by the macros** (web-angle cleats, fin plate, end plate): the fin plate and end plate come out as **catalogue FLAT BARS, not `Ks_Plate`** ⇒ an audit that queries plates will miss them |
| **keys are upper-cased on write** | asked `RQ200x8`, model holds `RQ200X8`; asked `HD260x68,15`, model holds `HD260X68,15` |

⇒ A model dumped with `dumpfull` **cannot be fed straight back** into `shape` — the catalogue
and key have both been transformed. Verify with `op=dumpcat catalog=<exact name>` first.
`verifykeys`-style pre-flight (check every planned key against the catalogue listing, print the
nearest matches on a miss) caught nothing on 07/08 only because the keys had already been read
out of the real listing.

⚠️ **`op=dumpcat` overwrites `eb_cat.txt`.** Delete the file before each call — on the first
survey attempt I read a stale leftover **37 times in a row** and "found" that every catalogue in
the library contained the same 232 round tubes. *No artifact = it didn't happen* applies to
artifacts that are merely **old**, not only to ones that are missing.

## The European families EB would actually buy

Verified present, with a real key from each:

| family | catalogue | example key |
|---|---|---|
| I-beams | `DIN_IPE` `DIN_I` (IPN) `DIN_HEA` `DIN_HEB` `DIN_HEM` `DIN_HEAA` | `IPE300` `I300` `HE200M` |
| heavy columns | `Peiner_HD` `Peiner_HL` `Peiner_PHP` `EN_HL` | `HD260x68,15` `HL920x342` |
| channels | `DIN_U` (UPN) `DIN_UPE` `DIN_UAP` | `U200` `UPE200` `UAP150` |
| equal angles | `DIN WINKEL GLEICH` | `L100x10` |
| unequal angles | `DIN_WINKEL_UNGLEICH` | `L100x75x8` |
| **double / quadruple angles** | `DIN WINKEL HD` (horizontal pair) · `DIN WINKEL VD` (vertical pair) · `DIN WINKEL 4T` (four) · `DIN WINKEL DIA` (diagonal pair) | `L80x8` |
| **split tees** (a beam cut in half) | `DIN_HALBE IPE` `DIN HALBE HEA/HEB/HEM` `DIN HALBE I` | `HIPE300` `HHE300B` |
| rolled tees | `DIN_T` `DIN_TB` | `T80` `TB40` |
| square hollow | `DIN_QUADRATROHR` (hot) · `DIN_QUADRATROHR_KALT` (cold) · `MSH QR` | `RQ200x8` |
| rectangular hollow | `DIN_RECHTECKROHR` (+`_KALT`) · `MSH RR` | `RR200x100x8` |
| round hollow | `DIN RUNDROHR` · `MSH RO` · `ROHR DIN 2448` · `ROHR_MANNESMANN` | `RO 219.1x8` |
| cold-formed purlins | `DIN_C` `DIN KALT-U` `DIN_Z` · makers: `METSEC_C` `KING_C` `SADEF_C` `WARD_C` `FISCHER_C` `ALB_C` `STABA_C` `SAB_C` `VOEST_C_KALT` | `Z160` `C150x75x6,5` |
| eaves beams | `EAVES` `METSEC EAVES` `ZED` | |
| flats & bars | `DIN FLACHEISEN` (419 sizes) · `DIN_BREITFLACHEISEN` (wide flats, 338) · `DIN RUNDSTAHL` · `DIN VIERKANT` | `100x10` `RD30` `KT40` |
| threaded rod | `DIN GEWINDESTANGEN` | `M20` |
| special | `DIN_BULBFLAT` (bulb flat — shipbuilding) · `DIN MULTIBEAM` | `120x17x17x6` `A230x180` |
| **not steel** | `VOLLHOLZ` `KANTHOLZ` (timber) | `VH100x220` |

Other national systems are installed in full and are there when a job needs them:
AISC (imperial `AISC_I_*` **and** metric `AISC_M_*`), BS, CISC, AS/NZ, JIS, IS (`IN_*`),
PN, SA, RUS, CN, Swiss, SW (Swedish), IMCA.

⚠️ AISC ships in two unit systems — `AISC_I_W` vs `AISC_M_W`. **METRIC ALWAYS** (Amir, 02/08)
means `AISC_M_*` if an American section is ever required.

## What a varied model looks like

The B.6 band (07/08) is the reference: **52 members, 14 distinct sections, 343 m of steel** —
heavy `Peiner_HD` corner columns, `HE200M` perimeter, `RQ200x8` hollow interior columns,
`RO 219.1x8` tube columns above, `I300` IPN girders, `UPE200` cross beams, `HIPE300` split-tee
roof members, `RR200x100x8` eaves, `Z160` purlins, `L100x10` and double-angle `L80x8` bracing,
`RD30` tie rods, `100x10` flat, `T80` posts.

The 37 members from the earlier lessons, sitting beside it, use **5**.
