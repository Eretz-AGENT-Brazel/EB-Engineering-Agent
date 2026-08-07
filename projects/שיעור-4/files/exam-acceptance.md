==========================================================================
EXAM ACCEPTANCE — lesson 4: column + base plate to the floor + 8 ribs
==========================================================================
EB_OK whoami doc=C:\Users\User\Desktop\EB PROSTEEL AGENT\projects\שיעור-4\מבחן-שיעור-4.dwg entities=15 reqid=cea7f5bc
  EB_OK dumpfull2 shapes=2 plates=8 bolts=0 other=5 err=0 -> eb_full_acc.txt reqid=2fac51c3
  EB_OK dumpholes objs=10 withholes=1 holes=4 slotted=0 err=0 -> eb_holes_acc.txt reqid=50be3ea4
  EB_OK dumppoly plates=8 nonrect=0 verts>4=8 err=0 -> eb_poly_acc.txt reqid=9a7db82b
  EB_OK connscan scanned=10 withlinks=2 links=2 err=0 | t13/BASEPLATE/RIB/SPLICE/SHEARPLATE/WEBANGLE/COPE=1 t10/BASEPLATE/RIB/SPLICE/SHEARPLATE/WEBANGLE/COPE=1 -> eb_conn_acc.txt reqid=11bf080a

--- column ---
  RQ200X8    RQ 200x8         z 20..3500  L=3480
  400X20     BRFL 400x20      z 10..10  L=400
   PASS column is a 200 square hollow          RQ200X8
   PASS design level at 3500                   top z=3500
   PASS column shortened by plate thickness    starts z=20, L=3480

--- base plate ---
   PASS plate sits ON the floor (0..20)        centre z=10
   PASS plate is 400 long                      L=400
   PASS plate thickness 20 (from its profile name) 400X20

--- holes ---
   PASS 4 holes                                4
   PASS all holes dia 23                       {23: 4}
   PASS hole spacing 300 x 300                 300 x 300
   PASS edge distance 50                       50

--- ribs ---
   PASS 8 rib plates                           8
   PASS every rib is shaped (5 verts), not a rectangle 8/8
   PASS all ribs 100 x 100                     {(100, 100)}
   PASS all ribs identical contour             1 distinct
   PASS rib contour matches the drawing        -50,-50,0;50,-50,0;50,-25,0;-25,50,0;-50,50,0

--- rib placement ---
  ribs per column face: {'+Y': 2, '-Y': 2, '+X': 2, '-X': 2}
   PASS 2 ribs on each of the 4 faces          {'+Y': 2, '-Y': 2, '+X': 2, '-X': 2}
   PASS ribs stand on the plate (centre z=70)  {70}

--- connection ---
   PASS base plate exists as a CONNECTION      2 joints total
  joint parameters: L=400 W=400 t=20 holeDia=23 hx=300 hy=300 anchors=1 anchorDia=0 poly=0 weldFl=0 weldWeb=0

==========================================================================
RESULT: ALL 18 CHECKS PASSED
==========================================================================

saving...
  saved: מבחן-שיעור-4.dwg (105366 bytes)