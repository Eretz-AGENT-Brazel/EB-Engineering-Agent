---
name: partlist-and-selection
description: "רשימת החלקים נגישה מקוד (posauto→partlist, sel=0,1,1, קורא Jet ב-32 ביט) ו-PsSelection לא יציב בין הרצות"
metadata: 
  node_type: memory
  type: project
  originSessionId: e1e7e219-55ad-4b68-8b41-c82b511ba272
  modified: 2026-08-21T12:02:40.614Z
---

⚡ שרשרת הפלט נפתחה 21/08/2026: ‏`op=partlist` קורא ל-`PsCreatePartlist.CreateMDBFile` **בלי דיאלוג**, והטבלה `Partlist` היא 127 עמודות של נתוני ייצור (משקל, אורך, שטח צביעה, מספר חורים/חיתוכים, מרכז כובד, קוטר בורג ואורך אחיזה, זוויות קיצוץ). מאומת על מודל מבוקר: 12 קורות → 12 שורות עם המשקלים הנכונים.

שלושה כללים שאין לעקוף:

1. **`posauto` קודם.** בלי מספרי פוזיציה נכתב קובץ **ריק ותקין** — 122,880 בתים של סכימה מלאה — ו-`CreateMDBFile` מחזיר `true`. בדיקת גודל אינה עדות.
2. **`sel=0,1,1`** הוא המצב היחיד שנמדד כותב שורות. ‏`1,1,1` בחר 19,897 חלקים וכתב אפס, כלומר `ObjectCount` לא מנבא אם תהיה כתיבה.
3. **קריאה חוזרת** דרך PowerShell 32 ביט + `Microsoft.Jet.OLEDB.4.0` (`C:\Windows\SysWOW64\WindowsPowerShell\v1.0\powershell.exe`); ל-Python 64 ביט אין ספק מותקן. בסוגריים מרובעים ב-SQL (`[_NAME]`), ובסריקת בתים לפענח **UTF-16LE** — סריקת ASCII של רשימה טובה בת 17.6MB מחזירה רעש בלבד.

⚠️ ‏**`PsSelection.SelectAllObjects` אינו יציב בין הרצות** — אותה קריאה על אותו שרטוט, מסמך פעיל מאומת ב-COM, החזירה 19,719 ואחר כך 386. ‏`collision` בנוי על אותה קריאה. לכן נבנה `op=selprobe` (קריאה בלבד) שמדווח את כל שמונת השילובים; להריץ אותו לפני שסומכים על אופ שבוחר "הכל".

⏸️ פתוח: מספר השורות בקנה מידה גדול — 166 שורות ל-14,062 חלקים ממוספרים בגשר, ובשרטוט יש 18 מקבצים בלבד אז זה לא ההסבר. הכלי הבא הוא `PerformPartlist2` עם `kPartlistExport`.

קשור: [[no-silent-skipping]] · [[read-back-what-you-gave]] · [[drill-exit-wall]]
