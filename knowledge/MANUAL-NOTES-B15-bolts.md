# B.15 Bolts — chapter notes

*Read end to end 09/08/2026, pages 248–261 (fulltext lines 5959–6298): B.15.1 Bolting parts ·
B.15.2 Bolt Style Management · B.15.3 Insertion of Threaded Rods · B.15.4 Sort.*

> *"Bolting of component parts is the **easiest form of automatic connections** provided by
> ProSteel. In previous versions, the components to be bolted had to be **drilled first. Which now
> is not necessary any more.**"*

⭐ **Drilling first is optional — but the drill-free case is a DIFFERENT PRODUCT, not a shortcut.**

Amir, 09/08, on being shown this test: *"אפשר — ואני רוצה שנגדיר את זה **רק באישור של מי שממדל
איתך**… זה נקרא **בורג קודח** — בורג שקודחים אותו לתוך הפרופיל מבלי לקדוח חור בטרם ההחדרה. זה
חיבור שמתבצע **בדרך כלל בשטח**, ואם אנחנו ממדלים את זה בתוכנה זה **על מנת להבין כמה ברגים אנחנו
צריכים להזמין** וכדי **שהפרט הזה לא יחמוק מאיתנו**."*

A **self-drilling screw** cuts its own hole as it goes in. Its missing hole is therefore
**correct**, not an omission — and it is normally a **site** connection. It is modelled so the
count reaches the parts list and the detail is not lost.

⚠️⚠️ **The rule has not changed.** Amir, immediately after the wording above went loose:
*"זה טעות קריטית לחלוטין **למעט מקרה אחד בודד**… **כל עוד לא נאמר — זה טעות קריטית**."*

**A bolt through steel with no modelled hole is a CRITICAL ERROR. Full stop.** The two cases look
**identical in the model**; only the intent differs, and the intent is not the agent's.
**A self-drilling screw is a declaration Amir makes, not a category the agent may apply.
Silence means critical error.** Amir's sequence — DRILL, then pick the two parts — remains the
default, and is the only route the API offers anyway.

## What the command actually does

> *"the program **checks the position of the parts** with regard to possible bolting (if need be,
> with regard to **drillings which are situated one above the other and having an allowable
> tolerance**; but it **does not check possible mounting**). Necessary **bolt lengths are
> calculated** and the bolts are inserted… Then, the bolts are adopted into a bolt list."*

⚠️ **It does not check mounting access.** A bolt can be created that no spanner can ever reach.
That check is the modeller's, not the software's.

⭐ **Back-to-back bolting** (v17 onwards): existing bolts and parts are **adopted into a new
bolting** — so two web angles either side of a girder bolt through correctly in one go.
*"The only prerequisite is that bolts and connection parts are connected with each other via
**logical links**."* It can be switched off in Global Settings/Bolts *"because it possibly requires
a considerable amount of calculation time."*

⭐ **Styles are componentised**, not summed: *"the necessary data from which the bolts are
generated are not summed up any more but they have been **divided into the corresponding
components**… length calculation is designed on the basis of the selected component parts."*
So a style is head + washer(s) + nut + safety nut, each with its own data file — and the grip
length follows from which of them are switched on.

## B.15.1 Bolting parts — the dialog

| field | meaning |
|---|---|
| ⭐ `Bolt style` | the style to use — **without it nothing is created** |
| ⭐ `Single hole Bolting` | *"Normally, **at least 2 component parts are required**… This option creates bolts for **each hole of the selected component parts without requiring a second part**."* |
| ⭐ `Create dynamic Connection` | the parts are joined by **logical links** and *"the bolting is **automatically updated** in case of modification"* |
| `Diameter` | the **virtual** hole diameter, for a manually created single bolt |
| `Work loose` | the virtual free play, manual insertion only |
| `Length Addition` | how much the bolts are extended — *"valid for **all kinds** of insertion"* |
| ⭐⭐ `Gap distance` | *"the **maximum distance between two holes** which are assumed to belong to the bolting. **If this value is exceeded the holes cannot be bolted.**"* |
| ⭐⭐ `Angle difference` | *"the difference of the **angles** of the drilled holes in degrees. If this value is exceeded, **the holes don't align** and cannot be bolted."* |
| `Colours` | bolt colour, and the monitor background it is judged against |

**The six buttons:** bolt selected parts · bolt selected parts **within an area** · **manual
insertion** (*"select start and endpoint of **grip length**"*) · ⭐ **attach a single nut and/or
washer with no bolt** (pick the insertion point, then the direction the head would face; for a
round bar, *"first press the **ESC**-key, then click the round bar at the desired end"*) ·
rotate bolts · ⭐ **recalculate bolt weight from volume**.

## B.15.2 Bolt Style Management

A style is assembled from components, each switchable and each with its own dialog:
**bolt** · `2nd Washer` · `Tapered washer` · `2nd Tapered Washer` · **washer** · **nut** ·
`Safety nut`.

**Bolt definition** — `Data file` (only bolt files are listed) · `Material` (only materials
declared as *bolt* materials) · `Tension` (%) · `Length Addition` ·
`Layout` = ⭐ **shop bolt or assembly bolt** — *"When an **assembly list** is created, **only
assembly bolts** will be taken into consideration"* · `Units` · `Coating` · `Colour` · `Bitmap`.

Flags: `Countersunk head` · `Parts list entry` · `Inner Hexagon` ·
⚠️ **`No DM Check`** — *"Normally, all entered diameters which are found will be **reduced to
standard values**. Thus e.g. a bolt with DM = 12.5 mm **cannot be created**."*
⭐ `Individual Mounting Space` — `Upper Area` / `Lower Area`, each a width and a length, entered
**absolutely, as `*xx` (a multiple of the diameter / length / nut height) or as `+xx` (an addition
to it)**.

⭐ **The parts-list entry is a template**: `$(D)` = bolt diameter, `$(L)` = bolt length, and for
bolts also `$(GLM)` = min grip length and `$(GLX)` = max grip length. `Export name` follows the
same rules. Nuts and washers have their own entries with `$(D)` and `$(L)`.

⚠️ *"The current definition is stored **in the drawing AND in a file**."* Two homes for one style —
and B.15.4's update button exists precisely because they drift apart.

## B.15.3 Threaded Rods

`Bolt Style` (defines the thread) · `Diameter` · `End Offset` · `Round to` (rounds the **overall
length**). Inserted **by two points**.

## B.15.4 Sort

Manages the style **selection list**: create a new style (*"equipped with the settings of the
current style"*) · load one from file · delete without confirmation ⚠️ · move up / down ·
⭐ **update all styles from disk** — *"The styles are stored as **objects in the drawing**. When the
style definition is modified on the hard disk, normally the modifications are **not transferred**
to the internal objects. They are carried out for all styles by this function."*

---

## The API

`Bentley.ProStructures.Steel.Bolt.PsCreateBolt` maps to B.15.1 field for field:

| dialog | property / method |
|---|---|
| `Bolt style` | `BoltStyle` · `BoltStyleName` · `BoltStyleIndex` · `BoltStyleCRC` · `BoltStyleCount` |
| the bolt type | `BoltType` · `BoltTypeName` · `BoltTypeIndex` · `BoltTypeCRC` · `BoltTypeCount` |
| `Diameter` | `Diameter` |
| `Length Addition` | `AdditionalLength` |
| ⭐ **`Gap distance`** | **`MaxObjectDistance`** |
| ⭐ **`Angle difference`** | **`MaxDeclination`** |
| the hole alignment tolerance | **`MaxCenterDistance`** |
| read-back | `BoltCount` · `ObjectId` |

**The four creation routes:**
```
AddObject(id) per part + Create()                                  the automatic path
CreateSingleBolt(start, end, dia, styleName, lengthAddition)       manual, by GRIP LENGTH
CreateSingleNut(start, end, dia, styleName)                        a nut/washer with no bolt
CreateThreadedRod(start, end, dia, offset, styleName)              B.15.3
```

⇒ **This chapter explains two failures from earlier today.**
- `boltparts` refusing with no `style=`: `BoltStyle` is a plain string, unset means no style, and
  the dialog's own rule is that a style is required. The refusal was correct.
- The end-plate bolt that would not form: its holes were **200 mm apart** in the far flange —
  far beyond `Gap distance`. **The plugin's own error hint had said exactly that and was
  right**; it was dismissed too readily when the real cause of a *different* failure turned out
  to be the missing style.

⚠️ `BoltStyleIndex` and `BoltTypeIndex` each appear **twice** in the type dump, and
`BoltStyleNameCRC` / `BoltTypeNameCRC` are typed **String** despite the name — almost certainly
"give me the name for this CRC". Resolve by signature before use.


---

# MEASURED 09/08/2026

*Band at x ≥ 210000: 9 objects — 4 plates and 5 bolts.*

## ⛔ "Drilling first is no longer necessary" — not from this API

The chapter's headline claim, tested directly: two overlapping 300×200×12 plates with
**zero holes**, then `boltparts`.

```
holesOnParts=0   created=0   create=False
holes after: 0 and 0
```

The control — the same pair **drilled first** — gave 2 holes each and **2 bolts**, cleanly.

⇒ **`PsCreateBolt.AddObject + Create()` still requires existing holes.** The
drill-free path the manual describes lives in the *dialog*, not at this API entry point.
⭐ **Amir's sequence — DRILL, then pick the two parts — is the only one that works from code.**
Not merely his habit: the only route available to the agent.

## Three creation routes that were missing, all working

| route | measured |
|---|---|
| `CreateSingleBolt(start, end, dia, style, addLen)` | a `Ks_Bolt` **65 long** for a 40 mm grip — head and nut are the difference |
| `CreateSingleNut(start, end, dia, style)` | a `Ks_Bolt` with **no shank extent** — the manual's "attach a nut and/or washer **without the corresponding bolt**" |
| `CreateThreadedRod(start, end, dia, offset, style)` | **660** long for a 600 span at `offset=30` ⇒ **the End Offset projects at BOTH ends** |

⇒ All three take the style **by name**, directly — no CRC needed.

## The style table

`BoltStyleCount` = **27**, `BoltTypeCount` = **27** — the same list, from `4.6S` and `8.8S`
(Australian) through `A307/A325/A490` (US) to `DIN558…DIN965`.

⚠️⚠️ **What the type dump hides, and the compiler reveals:**
- **`BoltStyle`, `BoltType` and `Diameter` are WRITE-ONLY.** The dump prints them as ordinary
  properties; they have **no get accessor**. You can set a style and never read back which one
  is set.
- **`BoltStyleName` is INDEXED** — `get_BoltStyleName(int)`. That indexer is the enumeration
  route and it is **invisible in the dump**, which shows a plain `String`.

⇒ Third instance of this pattern today, after `get_ParentFlangeIndex(int)` and
`get_WeldStyleName(int)`. **When a `String` or `Int32` property looks like it should be a list,
try the indexer.**

## What this chapter explains about earlier failures

- ⭐ **`boltparts` refusing without `style=`.** `BoltStyle` is a plain string with no default,
  and B.15.1 makes a style mandatory. The refusal was correct and the plugin's message was the
  vague part.
- ⭐ **The end-plate bolt that would not form** (frame 2): its holes were **200 mm apart**, in the
  far flange — far beyond `Gap distance` (`MaxObjectDistance`). **The plugin's own error hint
  said exactly that and was right.** It was dismissed too readily because a *different* failure
  earlier that day had turned out to be the missing style.
  ⚠️ Two different causes, one error message. Read the hint against the geometry, not against
  the last thing that went wrong.