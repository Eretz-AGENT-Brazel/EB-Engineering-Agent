# 🗺️ API-SURFACE — מפת ה-API המלאה של ProStructures

*נוצר 02/08/2026 ברפלקציה על `ProStructuresNet.dll` — לא מניחוש, לא מהאינטרנט.*
*החוב הפתוח מ-R1 נסגר.*

```
C:\Program Files\Bentley\ProStructures Ss6 R1\AutoCAD 2015\Prg\ProStructuresNet.dll
3,306 טיפוסים · 325 ציבוריים · 4,796 חתימות מתודות
מפה גולמית מלאה: knowledge/API-SURFACE-RAW.txt  (383 KB)
```

## 📊 המספר שמסביר הכול

| | |
|---|---|
| טיפוסים ציבוריים ב-API | **325** |
| טיפוסים שהסוכן משתמש בהם (v31) | **26** |
| **ניצולת** | **8%** |

---

## 🔴 הטבלה שכואבת: מה בניתי ביד מול מה שכבר קיים

| מה שעשיתי ידנית (ועלה סבבים) | מה שקיים ב-API | קובץ |
|---|---|---|
| ברגי עיגון — העתקת בורג + סיבוב + חישוב אורך, ~12 סבבי תיקון | **`PsCreateFastener.CreateFastenerStraightAnchorBolt(Dm, Extrusion, TopEmbedment, MiddleEmbedment, BottomEmbedment, ThreadLength, PlateThickness, GroutThickness, …)`** | `Concrete` |
| `replicate` = `Database.DeepCloneObjects` + `Matrix3d` (רמת AutoCAD) | **`PsMiscTools.ObjectsCopy(PsSelection, PsMatrix) → PsSelection`** | `Miscellaneous` |
| שיקוף — לא מומש בכלל | **`PsMiscTools.Mirror3d(Id, p1, p2, p3)`** | `Miscellaneous` |
| בדיקת כפילויות ידנית (הבאג של 754 עוגנים) | **`PsSelection.RemoveDuplicates()`** — שורה אחת | `Drawing` |
| בחירה לפי תיבה שגרמה ל"כדור שלג" | **`PsSelection.SelectAllObjectsInRange(…, MinPoint, MaxPoint)`** + `SetSelectionFilter` | `Drawing` |
| "אין לי עיניים" — בדיקת יחסים שלא נכתבה | **`PsGeometryFunctions`** — 47 מתודות (ראה למטה) | `Geometry.Utilities` |
| הפרט לא הסתובב כיחידה (פלטת בסיס נשארה מאחור) | **`PsObjectGroup`** / `PsCreateAssembly` / `PsBaseplateLinkDataMgd.CreateGroup` | `Modeling` |
| אוריינטציית אוגן — טעיתי **פעמיים** | **`PsBaseplateLinkDataMgd.PlateInShapeDirection`** | `Connection.LinkData` |
| פריסת חורים 2×3 במקום 3×2 | **`PsDrillObject.SetLinearHoleField(Diameter, XField, YField)`** — צירים מפורשים | `Modification.Edit` |
| קיטום ריפ — פוליגון מצויר ביד | **`PsEdgeChamfer`** (`EdgeLayout`, `TopVar1/2`, `DownVar1/2`) | `Modification.ObjectData` |
| חורים אובליים — חוב פתוח | **`PsDrillObject.SetRotateSlottedHoles`** + `LongHoleMode` + `HoleGeometrie` | `Modification.Edit` |
| העתקת קדיחה בין חלקים — ידנית | **`PsDrillObject.TakeoverDrills(DrillSource, DrillTarget)`** | `Modification.Edit` |
| בדיקת התנגשויות — לא מומש | **`PsCollisionCheck`** — 22 סוגי בדיקה, `Apply()`, `BodyCount` | `Modeling` |
| מספור חלקים (PS_POS) — לא מומש | **`PsCreatePositioning`** + `RecordIdenticalRecord` (זיהוי חלקים זהים!) | `Miscellaneous` |
| רשימת חלקים — לא מומש | **`PsCreatePartlist.CreateMDBFile(File, Template, Selection)`** | `Miscellaneous` |
| פריסת פלטה (development) | **`PsCreateUnfold`** — `GetOuterGeo`, `GetInnerGeo` | `Miscellaneous` |
| בטון כ-SOLID "להמחשה" | **`PsCreateConcreteSlab` / `PsCreateConcreteWall` / `PsCreateConcreteFooting`** + זיון מלא | `Concrete.*` |
| ייצוא לבדיקה — PNGOUT נכשל | **`PsMiscTools.CreateStlFile` / `CreateSatFile`** · `PsSelection.WriteXmlFile` | `Miscellaneous` |
| ריתוכים — לא מומש | **`PsCreateWeldFlag`** · `WeldSeamFlange/Web` במחבר | `Annotation` |

> **המסקנה בשורה אחת:** אמיר צדק לחלוטין. *"אתה כבר שם, בתוך התוכנה — תחיה אותה."*
> כמעט כל שעה שבזבזתי בשיעור 5 הייתה קריאה לפונקציה שלא קראתי עליה.

---

## 🧭 מפת מרחבי השמות (325 טיפוסים)

| מרחב שמות | # | מה יש שם | מצב |
|---|---|---|---|
| `Bentley.ProStructures` | 127 | **אנומים** — כל הקבועים: `HoleType`, `DrillType`, `InsertFace`, `ShapePosition`, `WeldSign`, `SelectionFilter`, `CutType`, `LongHoleMode`… | ⚠️ כמעט לא נקראו |
| `.Geometry.Data` | 15 | `PsPoint` `PsVector` `PsMatrix` `PsPolygon` **`PsExtents`** `PsGeoLine/Arc/Circle` `PsRectangle` | ✅ חלקי |
| `.Geometry.Utilities` | 5 | **`PsGeometryFunctions`** `PsContourFinder` `PsBeamIntersectionFinder` `PsPointInsideFinder` `PsSurfaceFinder` | ❌ **0%** |
| `.Steel.Shape` | 6 | `PsCreateShape` `PsShapeLoader` **`PsCreateBendShape`** `PsCreateArcShape` `PsCreateUserShape` `PsUserShapeManager` | ✅ חלקי |
| `.Steel.Plate` | 4 | `PsCreatePlate` **`PsCreateBendPlate`** `PsBendPlateFlange` `PsCreateArcPlate` | ✅ חלקי |
| `.Steel.Bolt` | 2 | `PsCreateBolt` **`PsCreateBoltStyle`** | ✅ חלקי |
| `.Modification.Edit` | 6 | `PsDrillObject` `PsCutObjects` **`PsEditPlateModification`** **`PsEditShapeModification`** | ✅ חלקי |
| `.Modification.ObjectData` | 11 | `PsSingleHoleArray` **`PsHoleField`** **`PsEdgeChamfer`** **`PsVertexChamfer`** `PsCutPlane` `PsCutPolygon` | ⚠️ 1 מתוך 11 |
| `.Connection.Standard` | 9 | 3 בשימוש · **חסרים:** `PsCopeConnection` `PsHaunchConnection` `PsPurlinConnection` `PsShearPlateConnection` `PsStandardPlateConnection` `PsWebAngleConnection` | ⚠️ 3/9 |
| `.Connection.LinkData` | 18 | תבניות הפרמטרים של כל מחבר | ⚠️ 6/18 |
| `.Connection.General` | 3 | `PsEditLogicalLink` `PsLogicalLink` **`PsCreateConnection`** | ✅ חלקי |
| `.Modeling` | 6 | `PsCreateWorkframe` · **`PsCollisionCheck` `PsCreateAssembly` `PsObjectGroup` `PsCreateGrid`** | ⚠️ 1/6 |
| `.Drawing` | 13 | **`PsSelection` `PsTransaction` `PsLayer` `PsFilterObject` `PsBlock` `PsMarkObject` `PsDictionary`** | ❌ **0%** |
| `.Miscellaneous` | 25 | **`PsMiscTools` `PsCreatePartlist` `PsCreatePositioning` `PsCollision…` `PsNCData` `PsCreateUnfold` `PsCompareDrawing` `PsUnits` `PsAnalysisDisplay`** | ❌ **0%** |
| `.Property` | 4 | `PsObjectProperties` · **`PsShapeInfo` `PsGroupProperties` `PsUserProperties`** | ⚠️ 1/4 |
| `.Annotation` | 2 | **`PsCreateWeldFlag` `PsCreatePositionFlag`** | ❌ 0% |
| `.CadSystem` | 5 | **`PsApplication` `PsTemplate` `PsTemplateManager` `PsProgressMeter` `PsResource`** | ❌ 0% |
| `.Configuration` | 12 | **`PsGlobalSettings` `PsMaterialTable` `PsDescriptionTable` `PsCallback`** | ❌ 0% |
| `.Concrete.*` | 33 | לוחות, קירות, יסודות, פאנלים, **וכל הזיון** | ❌ 0% |
| `.Assignment` | 3 | `PsAreaClassManager` `PsDisplayClassManager` `PsPartFamilyManager` | ❌ 0% |

---

## 👁️ `PsGeometryFunctions` — "העיניים" שכבר קיימות

זו התשובה לשורש #1 ו-#2 בדוח הלקחים (*"אני מאמת ספירות, לא גיאומטריה"* · *"אין לי עיניים"*).
**כל בדיקה שתכננתי לכתוב מאפס — כבר כתובה, עם `SetTolerance` מובנה.**

```csharp
Double  GetDistanceBetween        (PsPoint a, PsPoint b)
Double  GetDistanceToPlane        (PsPoint p, PsPoint planeOrigin, PsVector planeNormal)
Int32   IsPointOnLine             (PsPoint p, PsPoint start, PsPoint end, Int32 type)
Int32   IsVectorPerpendicularTo   (PsVector v1, PsVector v2)
Int32   IsVectorParallelTo        (PsVector v1, PsVector v2)
Int32   IsVectorAlignedTo         (PsVector v1, PsVector v2)
Double  GetAngleBetweenVectors    (PsVector v1, PsVector v2)
PsPoint OrthoProjectPointToPlane  (PsPoint p, PsPoint origin, PsVector normal)
PsPoint OrthoProjectPointToLine   (PsPoint p, PsPoint start, PsPoint end)
PsPoint GetPointInDirection       (PsPoint ref, PsVector dir, Double dist)
PsPoint PolarPoint                (PsPoint start, Double angle, Double dist)
Void    SetTolerance              (…)          ← סובלנות מובנית
+ עוד ~35: מכפלות וקטוריות/סקלריות, חיתוכי קווים ומישורים, קשתות, המרות מעלות/רדיאנים
```

**מיפוי ישיר לטעויות שקרו:**

| הטעות (מדוח הלקחים) | הבדיקה שהייתה תופסת אותה |
|---|---|
| #4 אוריינטציית אוגן הפוכה, ×2 | `IsVectorPerpendicularTo(flangeNormal, wallNormal)` |
| #9 עוגני קיר מרחפים באוויר | `GetDistanceToPlane(boltHead, plateFace, plateNormal) == 0` |
| #10 אום מרחף 10 מ"מ (ואז גם ברצפה) | `GetDistanceBetween(nutTop, rodEnd) ≤ tol` |
| #11 עמוד מרחף 180 מ"מ מעל הרצפה | `GetDistanceToPlane(columnBase, slabTop, +Z) == 0` |
| #6 פריסת חורים בציר הלא נכון | `IsVectorParallelTo(holeAxis, plateLongAxis)` |
| #12 שכפול כפול (754 במקום 480) | `PsSelection.RemoveDuplicates()` |
| #13 מחבר משוכפל מוסיף עוגנים | `PsCollisionCheck.Apply()` + `RecordIdenticalRecord` |

---

## 🔩 `PsCreateFastener` — ברגי העיגון, שכבר פתורים

```csharp
Int64 CreateFastenerStraightAnchorBolt(
        Double Dm,                //  קוטר                       ← M20
        Double Extrusion,         //  בליטה מעל הפלטה             ← בדיוק גובה האום
        Double TopEmbedment,      //  ┐
        Double MiddleEmbedment,   //  ├ העומק בבטון בשלושה מקטעים ← 120 מ"מ
        Double BottomEmbedment,   //  ┘
        Double ThreadLength,      //  אורך ההברגה
        Double PlateThickness,    //  עובי הפלטה                  ← 20
        Double GroutThickness,    //  שכבת ה-grout                ← קיים בתקן!
        … )
Int64 CreateFastenerHookAnchorBolt(…)   // עוגן מכופף J
Int64 CreateFastenerBendAnchorBolt(…)   // עוגן מכופף L
Int64 CreateFastenerHeadBolt(…)         // מוט עם ראש  ← תקן הייחוס
Int64 CreateFastenerHexStud / RoundStud(…)
Int64 CreateFastenerHookDowel / BendDowel(…)
Void  SetInsertMatrix(PsMatrix) · SetLayer · SetArticle · SetDescription
enum  PsFastenerAnchorBoltDimMode { twoSegmentMode, threeSegmentMode, fourSegmentMode }
```

**כל פרמטר שנלחמתי בו 12 סבבים הוא ארגומנט אחד כאן.** והמשפחה מתמפה אחד-לאחד
לטיפולוגיה של מחקר העיגון: ישר / מכופף L / מכופף J / עם ראש / מסמרות.
ראה `knowledge/research/ביסוס-ועיגון-עמודי-פלדה.pdf` פרק 3.

---

## 🏗️ `PsBaseplateLinkDataMgd` — 35 פרמטרים של פלטת בסיס

```
AnchorBoltDiameter · AnchorBoltDrillLength · AnchorBoltGripDiameter · AnchorBoltGripLength
AnchorBoltKeySize · AnchorBolts · AnchorBoltsOutside · CreateDetailedAnchorBolts
CreateGroup ← הפרט כיחידה אחת!          PlateInShapeDirection ← אוריינטציית האוגן!
CreateInnerHoles · CreateOuterHoles · BasePlateIsPolyPlate
HoleDiameter/Inner/Outer · HoleCountHorizontalOuter · HoleCountVerticalOuter
HoleDistanceHorizontal/Inner/Outer · HoleDistanceVertical/Inner/Outer
Length · Width · Thickness · LiningThickness ← שכבת ה-grout
DistanceToSupport · ShortenShape ← זה שקיצר לי את העמוד 9 פעמים
WeldSeamFlange · WeldSeamWeb · WeldToFlange · WeldToWeb · WeldStyleCRC
```

**שלושה פרמטרים היו חוסכים שעות:** `CreateDetailedAnchorBolts` · `CreateGroup` ·
`PlateInShapeDirection`.

---

## 🔧 `PsDrillObject` — קדיחה מלאה (31 מתודות)

```csharp
SetSingleHoleField(Diameter)                          // חור בודד
SetLinearHoleField(Diameter, XField, YField)          // ← פריסה בצירים מפורשים
SetRadialHoleField(Diameter, Radius, HoleCount)       // פריסה רדיאלית
SetRadialHoleRange(FromAngle, ToAngle)
SetHoleType(HoleType) · SetHoleBoltType · SetDrillType(DrillType)
SetRotateSlottedHoles(bool)     ← חורים אובליים (החוב הפתוח)
SetHoleDepth · SetDeepStart · SetHoleWorkloose(מרווח!) · SetHoleCounter · SetHoleStep
SetXYPlane(XAxis, YAxis) · SetNormal · SetInsertPoint · SetCoordinateSystem
SetXPosition / SetYPosition (PositionSelection) · SetXOffset / SetYOffset
SetIgnoreInnerContour · SetMidLineAlignement
TakeoverDrills(DrillSource, DrillTarget)   ← העתקת קדיחה בין חלקים!
Apply() · GetModifyIndex()
```

`SetHoleWorkloose` = **מרווח החור** — הפרמטר שמחקר העיגון קבע שהוא 8 מ"מ אצלנו
מול 2 (EN) עד 13 (AISC).

---

## 📦 ארבע יכולות שלמות שלא נגענו בהן

### 1. `PsObjectGroup` — הפרט כיחידה אחת
```csharp
AddSubParts(PsSelection) · Create() · CreateAssembly(Origin, XAxis, YAxis)
getAllPartsOf(Id, …) · getMainPartOf · setMainPart · computeWeight(Id, withoutBolts)
ComputeDimension(Id, out L, out W, out H) · WeightCenterOfGroup
```
זה **בדיוק** העיקרון שאמיר מלמד: *"מייצר פרט אחד שחוזר על עצמו ואז מעתיק"*.
פרט = קבוצה. שכפול = העתקת הקבוצה. הפלטה לא יכולה להישאר מאחור.

### 2. `PsCollisionCheck` — 22 סוגי בדיקה
```
CheckShapeToShape · CheckPlateToPlate · CheckBoltToBolt · CheckShapeToPlate
CheckConcreteToShape · CheckConcreteToPlate · CheckRebarTo* · UseBoltMountSpace
Apply() → int · BodyCount · MinVolume · ZoomToObject(i)
```

### 3. `PsCreatePositioning` — מספור חלקים + זיהוי זהות
```
SetColumnPrefix/BeamPrefix/OthersPrefix · SetLengthTol/HolesTol/WeightTol
SetEqualPartAssemblies/Connections/Groups/Singles
RecordHandle · RecordWeight · RecordVolume · RecordPosnum
RecordIdenticalRecord  ← התוכנה מזהה חלקים זהים בעצמה
```

### 4. `PsCreatePartlist` — רשימת חלקים למסד נתונים
```csharp
CreateMDBFile(FileName, TemplateName, PsSelection)   // → .mdb שאפשר לקרוא!
PerformPartlist2(file, format, asSingle, autoMode, outputFile)
GetPartlistTemplateNames() → ArrayList
SetTolerances(LengthTol, WidthTol, HeightTol, WeightTol)
```

---

## 📚 34 מאקרו חיבורים עם תיעוד מקומי

```
C:\Program Files\Bentley\ProStructures Ss6 R1\AutoCAD 2015\Prg\Plugins\*.chm
```
```
AECChute · AngleSplice · Array3D · BasePlate · BeamBeamClamp · BeamBeamShear
BeamBeamSplice · BeamBeamStiffener · BeamColumnEndPlate · BeamColumnFlange
BeamColumnMoment · BeamColumnSeated · BeamColumnWeb · BoxBeamShear · BoxBeamSplice
CatWalk · CircularPlatform · ColumnSplice · DualGusset · FlangeBraceConnection
Handrail · HideShowObjects · HollowBeamSplitter · HollowShapeBracing · PipeFlange
PipeStrap · PlateFunnel · PurlinBeamBraceFly · PurlinBeamStiffener · RodBrace
SquarePlatform · StairsConn · WebBraceConnection · WebMoment
```
> ⚠️ `hh.exe -decompile` לא עבד בהרצה לא-אינטראקטיבית ואין 7-Zip מותקן.
> החילוץ פתיר (קורא CHM ב-Python או פתיחה ידנית) — **פריט פתוח**.
> `Array3D` ו-`BasePlate` הם הראשונים לקרוא.

---

## ⚖️ שלוש דרכים חוקיות להפעיל את התוכנה (LISP אסור)

| דרך | מה זה | מותר? |
|---|---|---|
| **.NET API בתוך התהליך** | מה שהפלאגין עושה — `ProStructuresNet.dll` ישירות | ✅ **הדרך הראשית** |
| **`SendCommand` של COM עם שם פקודה** | `doc.SendCommand("EB_RUN31\n")` — מחרוזת פקודה, לא LISP | ✅ מותר |
| **`Editor.Command()` / `SendStringToExecute`** | הרצת פקודות התוכנה עצמן (`PS_*`) מתוך הפלאגין | ✅ "הפקודות של התוכנה" |
| ~~`(command "…")`~~ | טופס LISP | 🚫 **אסור** |

**החוב הפתוח היחיד:** `eb_api.py:200` עדיין שולח `(command "_NETLOAD" …)`.
**התיקון:** `FILEDIA=0` ואז `_NETLOAD` כמחרוזת פקודה רגילה — מסיר את ה-LISP האחרון.

---

## 🎯 סדר קריאה מומלץ ב-`API-SURFACE-RAW.txt`

1. `PsGeometryFunctions` — העיניים
2. `PsMiscTools` — העתקה/שיקוף/ייצוא
3. `PsSelection` — בחירה, סינון, הסרת כפילויות
4. `PsCreateFastener` — עוגנים
5. `PsObjectGroup` + `PsCreateAssembly` — הפרט כיחידה
6. `PsDrillObject` — קדיחה מלאה
7. `PsBaseplateLinkDataMgd` — 35 פרמטרי פלטת בסיס
8. `PsCollisionCheck` · `PsCreatePositioning` · `PsCreatePartlist` — שערי איכות
9. `Bentley.ProStructures` (127 אנומים) — הקבועים לכל הנ"ל
