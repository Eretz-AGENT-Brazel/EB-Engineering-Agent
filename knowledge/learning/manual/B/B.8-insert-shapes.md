
---

## AUDIT 10/08/2026 — what was missing

### The five shape types are five DATABASES, and four were unreachable

The folder is the **catalog**, the `.psp` file is the **section**:

```
Data/UserShapes/<catalog>/<section>.psp      Special / Sopro   68 catalogs, 1528 sections
Data/RoofWall/<catalog>/<section>.psp        Roof-Wall         20 catalogs,  270 sections
Data/CombiShapes/<catalog>/<section>.psp     Combination       15 catalogs,   88 sections
Data/WeldShapes/<catalog>/<section>.psp      Weld               3 catalogs,    4 sections
```

Some catalogs store a **`.dbf` table** instead of `.psp` files (`SCHRAG_z-pfetten`,
`SCHRAG_c-riegel`, `Kantteile`, `Steel Deck`). There the section name is the row's **`KEY`**.

⚠️ **Address a section by its FILENAME.** `Dreiecksbinder/R273x28-H440.psp` reads back with
`name='R244.5x22.2-H420'` — the internal name field disagrees with the geometry; the filename
does not.

```
beam kind=standard   name="HE 300 B"                            (default, unchanged)
beam kind=special    catalog=SCHRAG_z-pfetten  name=Z140-15     Z purlin
beam kind=roofwall   catalog=Bardage           name=4-250-36bx100
beam kind=combi      catalog=Dreiecksbinder    name=R273x28-H440
```

**Families that matter here** — cold-formed purlins (`SCHRAG_z-pfetten`, `SCHRAG_c-riegel`,
`Sadef_zed/cee/sigma`, `sbe_c/z/zeta`, `ayrsh_zeta`, `ayrshire_eb`), crane rails
(`Kranschienen_Form_A`, `krupp_zr/ztg/zth`, `kbk_kran`), Halfen cast-in channels
(`halfen_hl/hm/np/p`), bent sheet (`Kantteile`), decking (`Steel Deck`), stairs (`stair`).

### B.8.2 Bent Shapes — op `bendshape`

`PsCreateBendShape` was never used. It is also **the only creator with `SelectWeldSections`**:
the straight creator has four selectors, the bent one has five. Welded plate girders
(`WeldShapes/I-Profile/I950x300x30`, `Kastentraeger/K900x400`) are reachable **only** this way.

```
bendshape name="HE 200 B" pts=0,0,0;0,2500,0;2000,4000,0        polyline path
bendshape name="RO 88.9x5" circle=1500                          ring
bendshape name=... helix=r,angle,rising,resolution[,left]       helix
bendshape kind=weld catalog=I-Profile name=I950x300x30 pts=...  welded girder
```

⚠️ **A bent shape needs ≥ 3 path points.** Two return nothing — same section name, 2 points
fails and 3 succeeds.

⚠️ **`handle=` (`ConvertFromPolyline`) does not follow arcs.** It creates a shape, and the shape
is not the path: a 90° bulge left a vertex **650 mm outside** the result's bounding box, and
`Update()` on the polyline changes nothing. The op therefore reports **`pathfit=ok`** or
**`pathfit=MISMATCH n/N_vertices_outside_by_Xmm`** on every call — check it. Straight `pts=`
paths always read `ok`.
