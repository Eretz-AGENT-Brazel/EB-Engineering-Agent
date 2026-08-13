============================================================================
LESSON 6 — שלב ה - המשך מידול
============================================================================
recorded 2026-08-13 12:24:51 -> 2026-08-13 12:42:35

### 1. WHAT CHANGED IN THE MODEL (before -> after)
  shapes       12 -> 34     (+22)
  plates       20 -> 20     (+0)
  bolts        96 -> 96     (+0)
  other        12 -> 16     (+4)
  holes       308 -> 308    (+0)
  joints        4 -> 28     (+24)

### 2. HOW HE WORKED (the method)
  events 487 | commands 134 | cancelled 16 | created 99 | erased 47
  UNDO share: 19%
  commands used:
     UNDO                     x24
     PS_COPY                  x17
     ERASE                    x17
     GRIP_STRETCH             x17
     LINE                     x13
     -VIEW                    x8
     PS_GLOBAL_VIEW           x7
     PS_MODIFY                x6
     3DORBITTRANSPARENT       x4
     VSCURRENT                x3
     PS_INS_PROF              x2
     PS_HIDE_EXCLUDE          x2

### 3. WHAT HE BUILT (per object, with real parameters)

  PS_INS_PROF  ->  6 object(s)
     Ks_ShapeReference                         x4
     Ks_Shape       SHS200X200X5            x2

  PS_COPY  ->  36 object(s)
     Ks_Shape       SHS200X200X5            x21
     Ks_Shape       SHS200X200X5           CONN[Anglecut(t3,p0,b0); Anglecut(t3,p0,b0)]  x8
     AcDbLine                               x7

  ROTATE  ->  7 object(s)
     AcDbRegAppTableRecord                         x1
     AcDbDimStyleTableRecord                         x1
     AcDbLinetypeTableRecord                         x1
     AcDbPolyline                           x1
     AcDbBlockBegin                         x1
     AcDbBlockEnd                           x1
     AcDbBlockTableRecord                         x1

  UNDO  ->  14 object(s)
     AcDbDictionary                         x7
     AcDbXrecord                            x7

  MIRROR  ->  2 object(s)
     Ks_Shape       SHS200X200X5            x2

  JOIN  ->  4 object(s)
     AcDbSequenceEnd                         x1
     AcDb3dPolyline                         x1
     AcDb3dPolylineVertex                         x1
     AcDbPolyline                           x1

  PS_PLATE  ->  2 object(s)
     Ks_Plate       684.08x103.55x10       verts=4  x1
     Ks_Plate       655.26x196.47x10       verts=5  x1

  joints created in this lesson:
     Anglecut(t3,p0,b0); Anglecut(t3,p0,b0)  x8

  holes created in this lesson: 0 objects carried holes