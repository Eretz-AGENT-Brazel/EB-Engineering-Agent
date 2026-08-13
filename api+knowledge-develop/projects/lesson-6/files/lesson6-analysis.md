============================================================================
LESSON 6 — מחברי קורות ומידול קורות — beam connections
============================================================================
recorded 2026-08-13 09:29:10 -> 2026-08-13 09:59:44

### 1. WHAT CHANGED IN THE MODEL (before -> after)
  shapes        0 -> 4      (+4)
  plates        0 -> 8      (+8)
  bolts         0 -> 32     (+32)
  other         1 -> 5      (+4)
  holes         0 -> 144    (+144)
  joints        0 -> 0      (+0)

### 2. HOW HE WORKED (the method)
  events 1068 | commands 265 | cancelled 6 | created 228 | erased 71
  UNDO share: 29%
  commands used:
     UNDO                     x74
     -VIEW                    x35
     3DORBITTRANSPARENT       x32
     PS_GLOBAL_VIEW           x29
     PS_COPY                  x28
     ERASE                    x15
     PS_DRILL                 x14
     VSCURRENT                x6
     LINE                     x4
     GRIP_STRETCH             x4
     MOVE                     x3
     DIMLINEAR                x3

### 3. WHAT HE BUILT (per object, with real parameters)

  (no command)  ->  66 object(s)
     AcDbDictionary                         x33
     AcDbXrecord                            x32
     AcDbRegAppTableRecord                         x1

  PS_COPY  ->  26 object(s)
     AcDbLine                               x6
     Ks_Plate       800x200x10             verts=5  x6
     Ks_Shape       HE200A                  x5
     Ks_Plate       800x150x10             verts=5  x4
     Ks_Plate       800x200x10             verts=5 HOLES=16  x2
     AcDbDictionary                         x1
     AcDbXrecord                            x1
     Ks_Plate       800x150x10             verts=5 HOLES=16  x1

  PS_INS_PROF  ->  10 object(s)
     Ks_ShapeReference                         x4
     AcDbDictionary                         x3
     AcDbXrecord                            x1
     Ks_DataRecord                          x1
     Ks_Shape       HE200A                  x1

  UNDO  ->  58 object(s)
     AcDbDictionary                         x29
     AcDbXrecord                            x29

  JOIN  ->  1 object(s)
     AcDbPolyline                           x1

  PS_PLATE  ->  1 object(s)
     Ks_Plate       800x200x10             verts=5  x1

  3DROTATE  ->  3 object(s)
     AcDbDictionary                         x1
     AcDbSolidBackground                         x1
     AcDbXrecord                            x1

  DIMLINEAR  ->  39 object(s)
     AcDbLine                               x9
     AcDbPoint                              x9
     AcDbBlockReference                         x6
     AcDbBlockBegin                         x3
     AcDbBlockEnd                           x3
     AcDbBlockTableRecord                         x3
     AcDbMText                              x3
     AcDbRotatedDimension                         x3

  UNISOLATEOBJECTS  ->  1 object(s)
     AcDbXrecord                            x1

  PS_BOLT  ->  32 object(s)
     Ks_Bolt                                x32

  joints created in this lesson:
     (none)

  holes created in this lesson: 3 objects carried holes