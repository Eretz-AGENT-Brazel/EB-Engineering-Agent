============================================================================
LESSON 5 — מידול עמוד פלדה שמעוגן לקיר ולרצפה
============================================================================
recorded 2026-07-31 17:02:23 -> 2026-07-31 17:39:10

### 1. WHAT CHANGED IN THE MODEL (before -> after)
  shapes        0 -> 2      (+2)
  plates        0 -> 4      (+4)
  bolts         0 -> 0      (+0)
  other         1 -> 23     (+22)
  holes         0 -> 20     (+20)
  joints        0 -> 2      (+2)

### 2. HOW HE WORKED (the method)
  events 1364 | commands 276 | cancelled 36 | created 389 | erased 255
  UNDO share: 24%
  commands used:
     UNDO                     x64
     -VIEW                    x37
     PS_GLOBAL_VIEW           x30
     PS_COPY                  x28
     ERASE                    x20
     LINE                     x15
     3DORBITTRANSPARENT       x15
     VSCURRENT                x14
     DIMLINEAR                x13
     GRIP_STRETCH             x11
     PS_GROUNDPL              x5
     COPY                     x4

### 3. WHAT HE BUILT (per object, with real parameters)

  UNDO  ->  74 object(s)
     AcDbDictionary                         x33
     AcDbXrecord                            x30
     Ks_VolBody                             x4
     AcDbGroup                              x2
     Ks_GroupData                           x2
     Ks_ShapeReference                         x1
     Ks_Shape       300X14                 HOLES=4 CONN[Brace Plate(t10,p0,b0)]  x1
     Ks_Shape       300X20                 HOLES=8 CONN[Brace Plate(t10,p0,b0)]  x1

  PS_INS_PROF  ->  13 object(s)
     Ks_ShapeReference                         x5
     AcDbDictionary                         x4
     Ks_Shape       HE600A                  x2
     AcDbXrecord                            x1
     Ks_DataRecord                          x1

  -VIEW  ->  12 object(s)
     Ks_VolBody                             x12

  DIMLINEAR  ->  20 object(s)
     Ks_VolBody                             x4
     AcDbLine                               x3
     AcDbPoint                              x3
     AcDbBlockReference                         x2
     Ks_ShapeReference                         x1
     AcDbXrecord                            x1
     AcDbRegAppTableRecord                         x1
     AcDbBlockBegin                         x1
     AcDbBlockEnd                           x1
     AcDbBlockTableRecord                         x1
     AcDbMText                              x1
     AcDbRotatedDimension                         x1

  PS_COPY  ->  24 object(s)
     Ks_VolBody                             x18
     Ks_Plate       500x500x20             verts=5 HOLES=4  x4
     Ks_Shape       300X20                 HOLES=8  x2

  PS_GROUNDPL  ->  4 object(s)
     Ks_VolBody                             x4

  3DORBITTRANSPARENT  ->  2 object(s)
     AcDbDictionary                         x1
     AcDbSolidBackground                         x1

  JOIN  ->  2 object(s)
     AcDbPolyline                           x2

  PS_PLATE  ->  1 object(s)
     Ks_Plate       300x300x10             verts=5  x1

  PS_DRILL  ->  2 object(s)
     AcDbLine                               x2

  UNISOLATEOBJECTS  ->  1 object(s)
     AcDbXrecord                            x1

  EXTRUDE  ->  10 object(s)
     AcDbShExtrusion                         x2
     AcDbEvalGraph                          x2
     AcDbShHistory                          x2
     AcDb3dSolid                            x2
     AcDbAssocPersSubentManager                         x1
     AcDbPersSubentManager                         x1

  RECTANG  ->  1 object(s)
     AcDbPolyline                           x1

  joints created in this lesson:
     Brace Plate(t10,p0,b0)  x2

  holes created in this lesson: 8 objects carried holes