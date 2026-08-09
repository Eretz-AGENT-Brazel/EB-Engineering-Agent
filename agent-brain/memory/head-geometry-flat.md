---
name: head-geometry-flat
description: EB tank heads (כיפות) are FLAT lids with a bent edge — never dished/domed
metadata: 
  node_type: memory
  type: feedback
  originSessionId: f91b529d-dc89-40b6-9887-8daf2489a303
---

EB steel-tank heads (כיפות) are modelled as a **FLAT circular plate whose rim is bent inward into a straight cylindrical lip** — NOT a dished/domed/torispherical/elliptical head. There is no crown curvature.

Geometry rule (verified with Amir's example): the lip's axial length = **BENDING LENGTH** (אורך הכיפוף). `INSERT` mm of that lip sits inside the shell end; the rest protrudes. So the **flat face protrudes (bendingLength − insert)** beyond the shell end. Example: bend 80 mm, insert 30 mm → 30 mm inside, **50 mm protruding**, and the outermost surface is the flat face (no bulge). Only a small radius at the bent corner. Default GAP shell↔head = −5 mm (head nested inside the shell). See [[vessels-stage1]].

**Why:** Amir reacted very strongly ("לאאאא… אין לזה שום קשר לקיעור של הכיפה") when I first built the head as a dished ellipse. Protrusion has nothing to do with any crown curvature.

**How to apply:** in TankForge `model.js` the head is a LatheGeometry of profile `[rim → straight lip → small corner bend → flat face at centre]`. If ever tempted to add crown depth/dish to a head, don't — confirm with Amir first.
