============================================================================
LESSON 6 — שלב ב' — המשך שיעור 6 בתוך המודל הקיים
============================================================================
recorded 2026-08-13 11:38:14 -> 2026-08-13 12:03:24

### 1. WHAT CHANGED IN THE MODEL (before -> after)
  shapes        6 -> 12     (+6)
  plates       12 -> 20     (+8)
  bolts        80 -> 96     (+16)
  other         6 -> 12     (+6)
  holes       256 -> 308    (+52)
  joints        0 -> 4      (+4)

### 2. HOW HE WORKED (the method)
  events 742 | commands 188 | cancelled 21 | created 168 | erased 70
  UNDO share: 29%
  commands used:
     UNDO                     x52
     -VIEW                    x33
     PS_COPY                  x17
     PS_GLOBAL_VIEW           x15
     DIMLINEAR                x14
     3DORBITTRANSPARENT       x9
     GRIP_STRETCH             x9
     LINE                     x7
     ERASE                    x5
     PS_DRILL                 x4
     PS_HIDE_EXCLUDE          x3
     PS_HIDE                  x3

### 3. WHAT HE BUILT (per object, with real parameters)

  (no command)  ->  3 object(s)
     ?                                      x3

  PS_INS_PROF  ->  10 object(s)
     Ks_ShapeReference                         x8
     Ks_Shape       SHS100X100X4            x1
     Ks_Shape       U200                    x1

  PS_COPY  ->  18 object(s)
     AcDbLine                               x4
     Ks_Shape       SHS100X100X4            x3
     Ks_Plate       600x80x10              verts=5  x3
     AcDbDictionary                         x2
     Ks_GroupData                           x2
     AcDbGroup                              x2
     Ks_Shape       U200                    x1
     Ks_Plate       600x140x10             verts=5 HOLES=12  x1

  UNDO  ->  56 object(s)
     AcDbDictionary                         x20
     AcDbXrecord                            x15
     Ks_Bolt                                x8
     AcDbGroup                              x4
     Ks_GroupData                           x4
     Ks_Plate       400x400x10             verts=5 HOLES=4 CONN[Brace Plate(t10,p0,b0)]  x2
     Ks_Plate       250x200x10             verts=5 HOLES=4 CONN[Brace Plate(t10,p0,b0)]  x2
     Ks_ShapeReference                         x1

  -VIEW  ->  5 object(s)
     Ks_Bolt                                x4
     Ks_ShapeReference                         x1

  DIMLINEAR  ->  15 object(s)
     AcDbLine                               x3
     AcDbPoint                              x3
     Ks_ShapeReference                         x2
     AcDbBlockReference                         x2
     AcDbBlockBegin                         x1
     AcDbBlockEnd                           x1
     AcDbBlockTableRecord                         x1
     AcDbMText                              x1
     AcDbRotatedDimension                         x1

  JOIN  ->  2 object(s)
     AcDbPolyline                           x2

  PS_PLATE  ->  2 object(s)
     Ks_Plate       800x80x10              verts=5  x1
     Ks_Plate       600x180x10             verts=5  x1

  PS_BOLT  ->  12 object(s)
     Ks_Bolt                                x12

  UNISOLATEOBJECTS  ->  1 object(s)
     AcDbXrecord                            x1

  joints created in this lesson:
     Brace Plate(t10,p0,b0)  x4

  holes created in this lesson: 5 objects carried holes