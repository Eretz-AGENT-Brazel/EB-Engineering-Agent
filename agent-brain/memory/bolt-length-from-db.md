---
name: bolt-length-from-db
description: אורך הבורג נבחר מהאחיזה דרך טבלת ה-mdb של ProSteel; grip = אמצע KLEMMMIN..KLEMMMAX פחות DELTA לפי קוטר
metadata: 
  node_type: memory
  type: reference
  originSessionId: e1e7e219-55ad-4b68-8b41-c82b511ba272
  modified: 2026-08-21T12:51:28.499Z
---

⚡⚡ ‏`bolt len=` **מתעלמים ממנו**. האורך נבחר מהאחיזה דרך מסד הברגים של ProSteel, ו-`op=styles` מצהיר איזה קובץ וטבלה מאחורי כל אחד מ-27 הסטיילים המותקנים:

```
Data\Bolts\Australia.mdb @ AS_Bolt_88s      ← הסטייל 8.8S (הטבלה האוסטרלית)
Data\Bolts\DinBolts.mdb  @ SCH912, SCH6914  ← DIN912, DIN6914
```

קוראים אותם ב-PowerShell 32 ביט + `Microsoft.Jet.OLEDB.4.0` (ראה [[partlist-and-selection]]). כל שורה מחזיקה `KLEMMMIN`/`KLEMMMAX` — טווח האחיזה שבוחר את האורך — ולכן:

```
grip = (KLEMMMIN + KLEMMMAX)/2 − DELTA(קוטר)
DELTA:  M12 15.75 · M14(DIN912) 15 · M16 20 · M20 23.5 · M24 28.5 · M27(DIN6914) 35
p1 = תחילת הציר ·  p2 = p1 + grip·כיוון      (הבורג פורש את אורכו המלא מ-p1)
```

‏**14/14** על כל ייעוד בגשר בניסיון הראשון, כולל `M20 x 120` (אחיזה 84) ו-`M24 x 330` (אחיזה 277.5). ‏5,405 ברגים נבנו ב-130 שניות עם ספקטרום אורכים זהה למקור ו-95.16% מהמרכזים בתוך 0.1 מ"מ.

⚠️ **לבדוק את הטבלה לפני שמבטיחים מידה.** ‏`AS_Bolt_88s` נגמרת ב-**M12 x 60** ואין בה M14 ו-M27 בכלל — ומכאן שכל אחיזה מעל ~38 ב-M12 נדחית. ההערה הקודמת "8.8S מקבל רק אחיזה 24" נכונה ל-M12 בלבד. עותק של שתי הטבלאות שמור ב-`knowledge/bolt_table_AS_88s.tsv` ו-`bolt_table_DIN.tsv`.

⚡ הכלל הכללי: **מסד הנתונים של התוכנה קריא, ולכן פרמטר שנראה עקשן הוא בדרך כלל חיפוש בטבלה שלא קראתי.**

קשור: [[bolts-follow-holes]] · [[bolt-rule-8-8s-plus2]] · [[partlist-and-selection]] · [[read-back-what-you-gave]]
