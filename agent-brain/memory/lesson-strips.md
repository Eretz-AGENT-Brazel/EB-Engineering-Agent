---
name: lesson-strips
description: "Amir wants each lesson's modelling in its own defined strip with visible separation between lessons"
metadata: 
  node_type: memory
  type: feedback
  originSessionId: a4c7fddc-7ed7-48b9-a359-e4b37dc8fdbe
  modified: 2026-08-10T07:23:27.960Z
---

אמיר, 2026-08-10: **"שכל המידולים של כל שיעור יהיו בסטריפ מוגדר ונראה הפרדה בין שיעור לשיעור."**

**Why:** מודל תרגול נקרא בעיניים. בלי הפרדה גלויה צריך לפתוח קובץ חיצוני כדי לדעת איפה שיעור אחד נגמר והשני מתחיל.

**How to apply:** מקצב 60,000 מ"מ לאורך +X — רצועה של 56,000 ומרווח של 4,000, **והמרווח עצמו הוא ההפרדה**. גבול כל רצועה הוא `Ks_Grid` **בשם השיעור**, וכל הגבולות בשכבה `_STRIPS` כדי לכבות בלחיצה. בנוסף לתייג כל מדגם דרך טאב Data (`propset Note1=` למה הוא, `Note2=` מה נמדד עליו) — ואז המודל מתעד את עצמו.

⚠️ שתי מלכודות ב-`grid` שנמדדו: `lsteps`/`wsteps` הם **מרווחי מפתחים ולא מספר חלוקות** (`lsteps=1` בונה מפתח של מילימטר), וה-**LENGTH של הרשת רץ על ציר Y העולמי וה-WIDTH על ציר X**.

הוחל לראשונה ב-`projects/sandbox/E-structural-elements.dwg`. ראה [[no-silent-skipping]] ו-[[two-axes-authority]].
