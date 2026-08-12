# =============================================================================
#  FROZEN -- 05/08/2026, by Amir's decision.
#
#  "תקפיא את הקונסולה, בטח. חשבתי שזה כבר היה ברור מההתחלה שאנחנו עוזבים אותה
#   לבינתיים."  ...  "אנחנו בונים את ה-API עכשיו כאן" (= in chat).
#
#  DO NOT maintain, extend, refactor or debug this file. It is 67 KB that nobody
#  uses; every minute spent here is a minute not spent on modelling. It is kept,
#  not deleted, in case the in-AutoCAD interface (option C) reuses parts of it.
#
#  The live interface is: chat -> eb_api.py -> plugin (EB_RUN32) -> ProStructures.
# =============================================================================

"""
EB PROSTEEL AGENT - console server + workspace UI (V2).

Home screen (New / Resume project) + a per-project workspace. Each project has
its own chat + files under  projects/<id>/ . The "brain" is Claude (Claude Code,
free); this is the cockpit.

Run (from app/):  python console.py
Agent helpers:    python console.py wait [secs] | say "text" | status
The agent acts on the ACTIVE project (data/project.txt).
"""

import os
import sys
import json
import time
import base64
import subprocess
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

APP = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(APP)
DATA = os.path.join(ROOT, "data")
UPLOADS = os.path.join(DATA, "uploads")          # global (screenshots, temp)
BRAND = os.path.join(DATA, "brand")
PROJECTS = os.path.join(ROOT, "projects")
ASSETS = os.path.join(ROOT, "assets")
ACTIVE = os.path.join(DATA, "project.txt")        # holds the active project id (folder name)
for d in (DATA, UPLOADS, BRAND, PROJECTS, ASSETS):
    os.makedirs(d, exist_ok=True)
PORT = 8788


def _append(path, obj):
    with open(path, "a", encoding="utf-8") as f:
        f.write(json.dumps(obj, ensure_ascii=False) + "\n")


def _read(path):
    if not os.path.exists(path):
        return []
    out = []
    with open(path, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if line:
                try:
                    out.append(json.loads(line))
                except Exception:
                    pass
    return out


def _safe(name):
    keep = "-_ "
    return "".join(c for c in (name or "") if c.isalnum() or c in keep or ord(c) > 127).strip().replace(" ", "-") or "project"


def active_id():
    return open(ACTIVE, encoding="utf-8").read().strip() if os.path.exists(ACTIVE) else ""


def set_active(pid):
    open(ACTIVE, "w", encoding="utf-8").write(pid or "")


def pdir(pid=None):
    pid = pid or active_id()
    return os.path.join(PROJECTS, pid) if pid else None


def P(pid=None):
    """Resolve the active project's file paths (creating dirs)."""
    d = pdir(pid)
    if not d:
        return None
    files = os.path.join(d, "files")
    os.makedirs(files, exist_ok=True)
    return {
        "dir": d, "files": files,
        "inbox": os.path.join(d, "inbox.jsonl"),
        "outbox": os.path.join(d, "outbox.jsonl"),
        "conv": os.path.join(d, "conversation.jsonl"),
        "cursor": os.path.join(d, "cursor.txt"),
        "model": os.path.join(d, "MODEL.md"),
        "analysis": os.path.join(d, "plan_analysis.json"),
    }


def project_title(pid):
    """Display name: from MODEL.md H1 if present, else the folder id."""
    m = os.path.join(PROJECTS, pid, "MODEL.md")
    if os.path.exists(m):
        for line in open(m, encoding="utf-8"):
            if line.startswith("# "):
                return line[2:].replace("MODEL MEMORY -", "").replace("MODEL MEMORY", "").strip() or pid
    return pid


def list_projects():
    out = []
    if os.path.isdir(PROJECTS):
        for pid in sorted(os.listdir(PROJECTS)):
            d = os.path.join(PROJECTS, pid)
            if os.path.isdir(d):
                out.append({"id": pid, "name": project_title(pid),
                            "analyzed": os.path.exists(os.path.join(d, "plan_analysis.json")),
                            "files": len(os.listdir(os.path.join(d, "files"))) if os.path.isdir(os.path.join(d, "files")) else 0})
    return out


MODEL_TEMPLATE = """# MODEL MEMORY - {name}

> The agent reads this first when you return to this project.

- **Project:** {name}
- **Created:** {date}
- **Status:** new

## Specification
- (filled as we work)

## History
- {date} project created.
"""


def create_project(name):
    pid = _safe(name)
    d = os.path.join(PROJECTS, pid)
    os.makedirs(os.path.join(d, "files"), exist_ok=True)
    m = os.path.join(d, "MODEL.md")
    if not os.path.exists(m):
        open(m, "w", encoding="utf-8").write(
            MODEL_TEMPLATE.format(name=name, date=time.strftime("%Y-%m-%d")))
    return pid


# ---------- FAST local command interpreter (no Claude round-trip) ----------
# Common ops execute instantly in the server via COM, like a colleague at the keyboard.
_VIEWS = [
    ("0,0,1",  ("top", "מבט על", "מלמעלה", "תצוגת על")),
    ("0,0,-1", ("bottom", "מלמטה", "תחתית")),
    ("0,-1,0", ("front", "חזית", "מלפנים")),
    ("0,1,0",  ("back", "אחורי", "מאחור")),
    ("-1,0,0", ("left view", "מבט שמאל", "משמאל")),
    ("1,0,0",  ("right view", "מבט ימין", "מימין")),
    ("1,-1,1", ("iso", "איזומטרי", "תלת מימד", "תלת-מימד", "3d", "תלת ממד")),
]
_VIEW_HE = {"0,0,1": "מבט על (TOP)", "0,0,-1": "מבט תחתית", "0,-1,0": "מבט חזית (FRONT)",
            "0,1,0": "מבט אחורי", "-1,0,0": "מבט שמאל", "1,0,0": "מבט ימין",
            "1,-1,1": "מבט תלת-ממדי (ISO)"}
_ACTION_VERBS = ("תמדל", "תכניס", "הכנס", "הוסף", "צייר", "תצייר", "פתח", "תפתח", "שים",
                 "add", "draw", "insert", "model", "place", "create", "make", "open")
_ZOOM_WORDS = ("zoom", "הצג הכל", "התאם", "zoom extents", "הכל על המסך", "מרכז תצוגה")


def _find_view(t):
    for vec, keys in _VIEWS:
        for k in keys:
            if k in t:
                return vec
    return None


_PROF_RE = r"(HE[ABM]\s*\d+|IPE\s*\d+|IPN\s*\d+|UPN?\s*\d+|RHS\s*[\dxX.]+|SHS\s*[\dxX.]+|CHS\s*[\dxX.]+)"


def _try_model(text):
    """If the sentence names a profile + at least 2 points, build a native beam
    via eb_api (no dialog, ~3s). Returns a reply string or None."""
    import re as _re
    prof = _re.search(_PROF_RE, text, _re.I)
    pts = _re.findall(r"(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)(?:\s*,\s*(-?\d+(?:\.\d+)?))?", text)
    if not (prof and len(pts) >= 2):
        return None
    try:
        import eb_api
        p1 = tuple(float(x) if x else 0.0 for x in pts[0])
        p2 = tuple(float(x) if x else 0.0 for x in pts[1])
        r = eb_api.beam(prof.group(1), p1, p2)
        if r.startswith("EB_OK"):
            return "✓ מידלתי קורה %s מ-%s ל-%s" % (prof.group(1), p1, p2)
        return "לא הצלחתי למדל (%s): %s" % (prof.group(1), r)
    except Exception as e:
        return "שגיאת מידול: %s" % e


def _quick_action(text):
    """Execute a common command instantly via COM. Returns a reply string, or
    None if this isn't a recognized fast command (then it goes to the agent).
    Never blocks: bails fast if AutoCAD is busy/modal."""
    t = (text or "").strip().lower()
    if not t:
        return None
    # TIER 1: full NLU interpreter (free He/En modeling) - handles it in-server, ms-fast
    try:
        import nlu
        nlu_reply = nlu.handle(text)
        if nlu_reply:
            return nlu_reply
    except Exception as e:
        return "שגיאת פענוח: %s" % e
    # TIER 1b: engineering-standards consultation (cited, in-server, ms-fast)
    try:
        import standards_kb
        if standards_kb.is_standards_query(text):
            ans = standards_kb.consult(text)
            if ans:
                return ans
    except Exception:
        pass
    vec = _find_view(t)
    is_zoom = any(w in t for w in _ZOOM_WORDS)
    hit = None
    if any(v in t for v in _ACTION_VERBS):
        try:
            import prosteel
            hit = prosteel.command_for(text)
        except Exception:
            hit = None
    if not (vec or is_zoom or hit):
        return None
    try:
        import pythoncom
        import win32com.client
        pythoncom.CoInitialize()
        try:
            app = win32com.client.GetActiveObject("AutoCAD.Application")
            try:
                if not bool(app.GetAcadState().IsQuiescent):
                    return "⏳ AutoCAD עסוק כרגע (חלון פתוח או פקודה פעילה). לחץ ESC כמה פעמים או סגור את החלון, ונסה שוב."
            except Exception:
                pass
            doc = app.ActiveDocument
            if vec:
                xyz = [float(v) for v in vec.split(",")]
                vp = doc.ActiveViewport
                vp.Direction = win32com.client.VARIANT(
                    pythoncom.VT_ARRAY | pythoncom.VT_R8, xyz)
                doc.ActiveViewport = vp
                try:
                    app.ZoomExtents()
                except Exception:
                    pass
                return "עברתי ל" + _VIEW_HE.get(vec, "מבט") + " ✓"
            if is_zoom:
                app.ZoomExtents()
                return "התאמתי תצוגה (zoom extents) ✓"
            if hit:
                doc.SendCommand("\x1b\x1b_" + hit[0] + " ")
                return "פתחתי את הכלי: " + hit[1] + "  (" + hit[0] + ") ✓"
        finally:
            pythoncom.CoUninitialize()
    except Exception as e:
        return "לא הצלחתי לבצע (ודא ש-AutoCAD פתוח): " + str(e)
    return None


DEFAULT_LOGO = ("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 48 48'>"
                "<rect x='6' y='9' width='36' height='6' rx='1' fill='#3b9eff'/>"
                "<rect x='20' y='13' width='8' height='22' fill='#3b9eff'/>"
                "<rect x='6' y='33' width='36' height='6' rx='1' fill='#3b9eff'/></svg>")

HOME_BG_SVG = """<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1200 760' preserveAspectRatio='xMidYMid slice'>
<defs><linearGradient id='bg' x1='0' y1='0' x2='0' y2='1'>
<stop offset='0' stop-color='#0a1422'/><stop offset='1' stop-color='#060a12'/></linearGradient></defs>
<rect width='1200' height='760' fill='url(#bg)'/>
<g stroke='#16314e' stroke-width='1'>
<line x1='0' y1='190' x2='1200' y2='190'/><line x1='0' y1='380' x2='1200' y2='380'/><line x1='0' y1='570' x2='1200' y2='570'/>
<line x1='240' y1='0' x2='240' y2='760'/><line x1='600' y1='0' x2='600' y2='760'/><line x1='960' y1='0' x2='960' y2='760'/></g>
<g stroke='#3f7cb4' stroke-opacity='.55' fill='none' stroke-width='2'>
<ellipse cx='930' cy='250' rx='220' ry='60'/><ellipse cx='930' cy='250' rx='145' ry='40'/><ellipse cx='930' cy='250' rx='72' ry='20'/>
<line x1='930' y1='250' x2='1150' y2='250'/><line x1='930' y1='250' x2='710' y2='250'/><line x1='930' y1='250' x2='930' y2='190'/><line x1='930' y1='250' x2='930' y2='310'/>
<line x1='930' y1='250' x2='1085' y2='292'/><line x1='930' y1='250' x2='775' y2='208'/><line x1='930' y1='250' x2='1085' y2='208'/><line x1='930' y1='250' x2='775' y2='292'/></g>
<g fill='#2c4a66' fill-opacity='.5'><path d='M905 300 L898 560 L918 560 L922 300 Z'/><path d='M958 298 L952 560 L972 560 L978 298 Z'/></g>
<g stroke='#3f7cb4' stroke-opacity='.5' fill='#16314e' fill-opacity='.35' stroke-width='2'>
<rect x='150' y='250' width='150' height='270'/><ellipse cx='225' cy='250' rx='75' ry='20' fill='#2c4a66' fill-opacity='.5'/>
<line x1='150' y1='300' x2='300' y2='300'/><line x1='150' y1='350' x2='300' y2='350'/><line x1='150' y1='400' x2='300' y2='400'/><line x1='150' y1='450' x2='300' y2='450'/>
<polygon points='150,520 300,520 225,635'/></g>
<g stroke='#34597f' stroke-opacity='.45' stroke-width='6' fill='none'>
<path d='M430 620 L430 470 L600 470 L600 620'/><path d='M455 470 L575 600 M575 470 L455 600'/>
<path d='M620 640 L620 500 L760 500 L760 640'/></g>
</svg>"""


PALETTE = [
    ("מידול", [("פרופיל / קורה", "PS_INS_PROF", "🏗️"), ("לוח", "PS_PLATE", "▭"),
               ("מצטף", "PS_RIP", "⊥"), ("שינוי 3D", "PS_MODIFY", "✎")]),
    ("ברגים וחורים", [("ברגים", "PS_BOLT", "🔩"), ("עוגנים", "PS_ANCHORBOLT", "⚓"),
                      ("קידוח", "PS_DRILL", "⊙")]),
    ("מחברים", [("פלטת קצה", "PS_ENDPLATE", "▥"), ("צלחת קשר", "PS_GUSSET_PLATE", "◣"),
                ("פלטת גזירה", "PS_SCHEARPLATE", "▤"), ("זווית מגן", "PS_STEGW", "∟"),
                ("איחוי", "PS_LASCHE", "═"), ("פלטת בסיס", "PS_GROUNDPL", "⬓")]),
    ("מבנים", [("מסגרת", "PS_FRAME", "⊓"), ("סבכה", "PS_TRUSS", "△"),
               ("ייצוב", "PS_BRACING", "✕"), ("מדרגות", "PS_STAIRS", "▦"),
               ("מעקה", "PS_HANDRAIL", "▤")]),
    ("ייצור", [("מספור", "PS_POS", "①"), ("רשימת חלקים", "PS_CREATE_PARTLIST", "☰"),
               ("תוכניות ייצור", "PS_DETCENTER", "📐"), ("נתוני CNC", "PS_NC_DATA", "⚙")]),
    ("צירים / תצוגה", [("צירים", "PS_WORKFRAME", "▦"), ("תצוגות", "PS_SETBKS", "👁"),
                       ("העתק/שכפל", "PS_COPY", "⧉")]),
]


def build_page():
    pal = ""
    for group, items in PALETTE:
        pal += f'<div class="pgrp"><div class="pgh">{group}</div><div class="pgi">'
        for label, cmd, emo in items:
            pal += (f'<button class="tool" data-cmd="{cmd}" title="{cmd}">'
                    f'<span class="te">{emo}</span><span>{label}</span></button>')
        pal += "</div></div>"
    return PAGE_TMPL.replace("__PALETTE__", pal)


PAGE_TMPL = r"""<!doctype html><html id="root" lang="he" dir="rtl"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>EB PROSTEEL AGENT</title>
<style>
 :root{--bg:#070a10;--panel:#0e1420cc;--line:#1d2737;--txt:#e9eff6;--dim:#8a98ab;
   --steel:#3b9eff;--steel2:#67b8ff;--cyan:#43e0d0;--amber:#ffb454;--good:#2bd576;--glow:0 0 24px rgba(59,158,255,.25);}
 *{box-sizing:border-box;-webkit-tap-highlight-color:transparent}
 html,body{height:100%;margin:0}
 body{font-family:"Segoe UI",system-ui,Arial,sans-serif;color:var(--txt);overflow:hidden;height:100vh;
   background:radial-gradient(1200px 600px at 80% -10%,rgba(59,158,255,.10),transparent 60%),
     radial-gradient(900px 500px at 0% 110%,rgba(67,224,208,.07),transparent 60%),
     linear-gradient(180deg,#070a10,#05070b),
     repeating-linear-gradient(0deg,transparent 0 47px,rgba(59,158,255,.035) 47px 48px),
     repeating-linear-gradient(90deg,transparent 0 47px,rgba(59,158,255,.035) 47px 48px);}
 .hidden{display:none!important}
 /* ===== HOME ===== */
 #home{height:100vh;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:18px;position:relative;overflow:hidden}
 #home::before{content:"";position:absolute;width:780px;height:780px;border-radius:50%;
   background:conic-gradient(from 0deg,transparent,rgba(59,158,255,.10),transparent 40%);animation:spin 22s linear infinite;filter:blur(8px)}
 @keyframes spin{to{transform:rotate(360deg)}}
 .homebg{position:absolute;inset:0;z-index:0;background:linear-gradient(rgba(6,10,18,.72),rgba(6,10,18,.9)),url(/homebg) center/cover no-repeat}
 .hlogo{width:110px;height:110px;background:#fff;border-radius:22px;padding:9px;box-shadow:0 0 50px rgba(59,158,255,.45);z-index:1}
 .hlogo img,.hlogo svg{width:100%;height:100%;object-fit:contain}
 .htitle{font-size:42px;font-weight:800;letter-spacing:5px;z-index:1;
   background:linear-gradient(90deg,#bfe2ff,var(--steel2),var(--cyan));-webkit-background-clip:text;background-clip:text;color:transparent}
 .htag{color:var(--amber);letter-spacing:4px;font-size:13px;text-transform:uppercase;z-index:1;margin-top:-8px}
 .hcards{display:flex;gap:20px;margin-top:14px;z-index:1;flex-wrap:wrap;justify-content:center}
 .hcard{width:280px;background:var(--panel);border:1px solid var(--line);border-radius:18px;padding:26px;cursor:pointer;transition:.15s;text-align:center}
 .hcard:hover{border-color:var(--steel);box-shadow:var(--glow);transform:translateY(-3px)}
 .hcard .ic{font-size:42px}.hcard .ti{font-size:20px;font-weight:700;margin:10px 0 6px}.hcard .ds{font-size:13px;color:var(--dim)}
 #panelNew,#panelList{z-index:1;width:600px;max-width:92vw;background:var(--panel);border:1px solid var(--line);border-radius:16px;padding:20px}
 #panelNew input{width:100%;padding:13px;border-radius:10px;border:1px solid var(--line);background:#070b12;color:var(--txt);font-size:15px;outline:none}
 #panelNew input:focus{border-color:var(--steel)}
 .plist{display:flex;flex-direction:column;gap:8px;max-height:50vh;overflow-y:auto}
 .pitem{display:flex;align-items:center;gap:12px;padding:12px 14px;border:1px solid var(--line);border-radius:11px;cursor:pointer;transition:.12s;background:#0c1421}
 .pitem:hover{border-color:var(--steel);background:#10243c}
 .pitem .pn{font-weight:700}.pitem .pm{font-size:11.5px;color:var(--dim)}
 .btn{background:linear-gradient(180deg,var(--steel2),var(--steel));color:#03101f;border:0;border-radius:10px;padding:11px 18px;font-weight:800;cursor:pointer;font-size:14px}
 .btn.ghost{background:#0c1421;color:#cdd8e6;border:1px solid var(--line)}
 .hrow{display:flex;gap:10px;justify-content:flex-end;margin-top:14px}
 .showcase{z-index:1;margin-top:8px;text-align:center}
 .shead{color:var(--cyan);letter-spacing:2.5px;font-size:11px;margin-bottom:12px;opacity:.85}
 .srow{display:flex;gap:18px;justify-content:center;flex-wrap:wrap}
 .spic{width:236px;background:linear-gradient(180deg,#0d1726,#0a1019);border:1px solid var(--line);border-radius:14px;padding:12px;transition:.15s;box-shadow:0 6px 22px #0006}
 .spic:hover{border-color:var(--steel);box-shadow:var(--glow);transform:translateY(-3px)}
 .spic svg{width:100%;height:118px;display:block}
 .scap{font-size:12px;color:#cdd8e6;margin-top:8px;font-weight:600}
 /* ===== WORKSPACE ===== */
 #ws{height:100vh;display:grid;grid-template-rows:auto 1fr}
 header{display:flex;align-items:center;gap:14px;padding:11px 16px;background:linear-gradient(180deg,rgba(18,26,40,.95),rgba(10,15,23,.92));border-bottom:1px solid var(--line);box-shadow:0 4px 24px #0008;z-index:5}
 .logo{width:42px;height:42px;background:#fff;border-radius:10px;padding:3px;box-shadow:var(--glow)}
 .logo img,.logo svg{width:100%;height:100%;object-fit:contain}
 .brand{display:flex;flex-direction:column;line-height:1.08}
 .brand .t1{font-weight:800;letter-spacing:2.5px;font-size:16px;background:linear-gradient(90deg,#bfe2ff,var(--steel2),var(--cyan));-webkit-background-clip:text;background-clip:text;color:transparent}
 .brand .t2{font-size:9.5px;letter-spacing:3px;color:var(--amber);text-transform:uppercase}
 .home-btn{background:#0c1421;border:1px solid var(--line);color:#cdd8e6;border-radius:9px;padding:7px 12px;cursor:pointer;font-size:13px}
 .home-btn:hover{border-color:var(--steel);color:#fff}
 .stat{margin-inline-start:auto;display:flex;gap:9px;align-items:center;flex-wrap:wrap}
 .chip{display:flex;align-items:center;gap:7px;font-size:11.5px;color:var(--dim);background:#0b1320;border:1px solid var(--line);border-radius:22px;padding:5px 12px}
 .chip b{color:var(--txt);font-weight:600}
 .dot{width:8px;height:8px;border-radius:50%;background:#e0a000;box-shadow:0 0 7px #e0a000}
 .dot.on{background:var(--good);box-shadow:0 0 9px var(--good)}
 .clock{font-variant-numeric:tabular-nums;color:var(--steel2)}
 .wsgrid{display:grid;grid-template-columns:264px 1fr;min-height:0}
 aside{border-inline-end:1px solid var(--line);background:linear-gradient(180deg,rgba(11,15,23,.6),rgba(7,10,16,.6));overflow-y:auto;padding:12px;display:flex;flex-direction:column;gap:12px}
 .card{background:var(--panel);border:1px solid var(--line);border-radius:12px;padding:11px}
 .card h4{margin:0 0 8px;font-size:11px;letter-spacing:1.5px;color:var(--cyan);text-transform:uppercase;font-weight:700}
 .pj .nm{font-weight:700;color:#fff;font-size:13px}.pj .meta{font-size:11px;color:var(--dim);margin-top:3px}
 .filelist{display:flex;flex-direction:column;gap:5px}.filelist .f{font-size:12px;color:#cdd8e6;background:#0c1421;border:1px solid var(--line);border-radius:7px;padding:6px 9px;display:flex;gap:7px;align-items:center;justify-content:space-between}
 .f .fn{flex:1;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
 .f .fb{background:none;border:0;color:var(--dim);cursor:pointer;font-size:13px;padding:2px 4px}.f .fb:hover{color:#fff}.f .fb.del:hover{color:#ff7b72}
 .addf{background:#0c1421;border:1px solid var(--line);color:var(--steel2);border-radius:6px;font-size:11px;padding:2px 8px;cursor:pointer;float:left}.addf:hover{border-color:var(--steel);color:#fff}
 .fhint{font-size:10.5px;color:var(--dim);margin-top:7px;opacity:.8}
 .filelist .empty{font-size:12px;color:var(--dim)}
 .plan img{width:100%;border-radius:8px;border:1px solid var(--line);cursor:zoom-in;background:#fff}
 .pgrp{margin-bottom:9px}.pgh{font-size:10.5px;letter-spacing:1px;color:var(--dim);margin:4px 2px}
 .pgi{display:grid;grid-template-columns:1fr 1fr;gap:6px}
 .tool{display:flex;align-items:center;gap:7px;background:#0c1421;border:1px solid var(--line);color:#cdd8e6;border-radius:9px;padding:8px 9px;font-size:12px;cursor:pointer;transition:.12s;text-align:start}
 .tool:hover{border-color:var(--steel);color:#fff;background:#10243c;box-shadow:var(--glow);transform:translateY(-1px)}
 .tool .te{font-size:14px}
 main{display:grid;grid-template-rows:1fr auto;min-height:0;min-width:0}
 #log{overflow-y:auto;padding:18px 20px;display:flex;flex-direction:column;gap:12px}
 .msg{max-width:78%;padding:11px 14px;border-radius:15px;line-height:1.5;font-size:14px;word-wrap:break-word;overflow-wrap:anywhere;animation:pop .16s ease;box-shadow:0 2px 10px #0004}
 @keyframes pop{from{opacity:0;transform:translateY(6px)}to{opacity:1}}
 .me{align-self:flex-end;background:linear-gradient(180deg,#2a6cf0,#1e54d6);border:1px solid #4a86ff;border-end-end-radius:4px;color:#fff}
 .ag{align-self:flex-start;background:linear-gradient(180deg,#0f1d18,#0c1813);border:1px solid #19402e;border-inline-start:3px solid var(--cyan);border-end-start-radius:4px}
 .ag .who{font-size:9.5px;letter-spacing:2px;color:var(--cyan);margin-bottom:5px;opacity:.9}
 .msg img{max-width:100%;border-radius:9px;margin-top:8px;display:block;border:1px solid var(--line);cursor:zoom-in}
 .msg .file{font-size:12px;opacity:.85;margin-top:5px}.msg p{margin:.3em 0}.msg ul{margin:.3em 0;padding-inline-start:1.3em}
 .msg code{background:#00000055;padding:1px 5px;border-radius:5px;font-family:Consolas,monospace;font-size:12.5px;color:var(--amber)}
 .msg h3{margin:.4em 0;font-size:14.5px;color:var(--steel2)}
 .working{align-self:flex-start;display:flex;align-items:center;gap:9px;color:var(--dim);font-size:13px;background:#0c1813;border:1px solid #19402e;border-inline-start:3px solid var(--amber);border-radius:14px;padding:10px 14px}
 .working .d{width:7px;height:7px;border-radius:50%;background:var(--amber);animation:bl 1s infinite}
 .working .d:nth-child(2){animation-delay:.2s}.working .d:nth-child(3){animation-delay:.4s}
 @keyframes bl{0%,100%{opacity:.25}50%{opacity:1}}
 #atts{display:flex;gap:7px;flex-wrap:wrap;padding:0 20px}
 #atts .a{background:#0c1421;border:1px solid var(--line);border-radius:8px;padding:4px 10px;font-size:12px;display:flex;gap:7px;align-items:center;color:var(--dim)}
 #atts .a button{background:none;border:0;color:#ff7b72;cursor:pointer;font-size:15px}
 #draw{display:none;padding:9px 20px;background:rgba(14,20,32,.7);border-top:1px solid var(--line)}
 .palette{display:flex;gap:7px;align-items:center;flex-wrap:wrap;margin-bottom:7px}
 .sw{width:21px;height:21px;border-radius:50%;cursor:pointer;border:2px solid #0007}.sw.sel{outline:2px solid #fff}
 .mini{background:#0c1421;color:#cdd8e6;border:1px solid var(--line);border-radius:7px;padding:5px 10px;cursor:pointer;font-size:12px}.mini.on{background:var(--steel);color:#04101f;border-color:var(--steel)}
 #cv{width:100%;height:260px;background:#fff;border-radius:9px;touch-action:none;border:1px solid var(--line)}
 .bar{display:flex;gap:9px;align-items:flex-end;padding:12px 20px;background:linear-gradient(180deg,rgba(11,15,23,.6),rgba(6,9,14,.85));border-top:1px solid var(--line)}
 .iconb{background:#0c1421;border:1px solid var(--line);border-radius:11px;width:46px;height:46px;cursor:pointer;font-size:18px;color:#cdd8e6;transition:.12s;flex:0 0 auto}
 .iconb:hover{border-color:var(--steel);color:#fff;box-shadow:var(--glow)}
 .iconb.live{background:#c0392b;color:#fff;border-color:#e74c3c;animation:pulse 1s infinite}.iconb.on{background:var(--steel);color:#04101f;border-color:var(--steel)}
 @keyframes pulse{0%,100%{box-shadow:0 0 0 0 rgba(231,76,60,.5)}50%{box-shadow:0 0 0 7px rgba(231,76,60,0)}}
 textarea{flex:1;resize:none;height:48px;max-height:150px;border-radius:12px;border:1px solid var(--line);padding:13px;font-family:inherit;font-size:14px;background:#070b12;color:var(--txt);outline:none}
 textarea:focus{border-color:var(--steel);box-shadow:0 0 0 3px rgba(59,158,255,.16)}
 .send{background:linear-gradient(180deg,var(--steel2),var(--steel));color:#03101f;border:0;border-radius:12px;padding:0 20px;height:48px;cursor:pointer;font-size:14px;font-weight:800;box-shadow:var(--glow);flex:0 0 auto}
 .toprow{display:flex;gap:7px;padding:7px 20px 0;flex-wrap:wrap}
 input[type=file]{display:none}
 #lb{display:none;position:fixed;inset:0;background:#000d;z-index:50;align-items:center;justify-content:center;cursor:zoom-out}#lb img{max-width:94%;max-height:94%;border-radius:8px}
 #stdPanel{display:none;position:fixed;inset:0;z-index:60;background:linear-gradient(180deg,#0b0f17,#070a10);flex-direction:column}
 #stdPanel.show{display:flex}
 .stdbar{display:flex;align-items:center;justify-content:space-between;padding:12px 20px;border-bottom:1px solid var(--line);background:#121826}
 .stdbar b{color:var(--steel2);letter-spacing:1px;font-size:15px}
 .stdbar button{background:#0c1421;border:1px solid var(--line);color:#cdd8e6;border-radius:8px;padding:6px 12px;cursor:pointer}
 #stdBody{overflow-y:auto;padding:20px 26px;line-height:1.75;max-width:920px;margin:0 auto;width:100%;font-size:14px}
 #stdBody h1{font-size:20px;color:var(--steel2)}#stdBody h3{color:var(--cyan);margin-top:1.1em}
 #stdBody code{background:#00000055;padding:1px 5px;border-radius:5px;color:var(--amber);font-family:Consolas,monospace}
 #stdBody ul{padding-inline-start:1.4em}#stdBody li{margin:.25em 0}
 .stdlinks{display:flex;flex-wrap:wrap;gap:10px;align-items:center;margin:0 0 18px;padding:14px;border:1px solid var(--line);border-radius:12px;background:linear-gradient(180deg,#101826,#0b1220)}
 .stdlinks a{display:inline-block;background:#0c1a2b;border:1px solid var(--steel);color:var(--cyan);text-decoration:none;border-radius:9px;padding:8px 13px;font-size:13px;font-weight:600;transition:.15s}
 .stdlinks a:hover{background:var(--cyan);color:#04121f;border-color:var(--cyan)}
 .stdlinks .stdnote{flex-basis:100%;color:var(--dim);font-size:12px;line-height:1.5;margin-top:2px}
 #stdWrap{display:flex;flex:1;min-height:0}
 #stdSide{width:305px;flex:none;border-inline-end:1px solid var(--line);background:#0a0f18;display:flex;flex-direction:column;overflow:hidden}
 #stdSearch{margin:12px;padding:9px 11px;border:1px solid var(--line);border-radius:9px;background:#0c1421;color:#e6edf6;font-size:13px;outline:none}
 #stdSearch:focus{border-color:var(--cyan)}
 #stdList{overflow-y:auto;padding:0 8px 16px}
 .stdItem{padding:8px 11px;margin:3px 0;border-radius:8px;cursor:pointer;color:#cdd8e6;font-size:13px;border:1px solid transparent;line-height:1.35}
 .stdItem:hover{background:#111a28;border-color:var(--line)}
 .stdItem.on{background:var(--cyan);color:#04121f;font-weight:600}
 .stdItem .sc{color:var(--dim);font-size:11px;display:block;margin-top:1px}
 .stdItem.on .sc{color:#04121f}
 .stdGrp{color:var(--steel2);font-size:11px;letter-spacing:1px;margin:14px 8px 3px;text-transform:uppercase}
 #stdWrap #stdBody{max-width:none;margin:0;flex:1}
 @media(max-width:760px){.wsgrid{grid-template-columns:1fr}aside{display:none}}
</style></head><body>

<!-- ============ HOME ============ -->
<div id="home">
  <div class="homebg"></div>
  <div class="hlogo" id="hlogo"></div>
  <div class="htitle">EB PROSTEEL AGENT</div>
  <div class="htag">Eretz Barzel · Steel Modeling AI</div>
  <div class="hcards" id="hcards">
    <div class="hcard" id="cNew"><div class="ic">🆕</div><div class="ti">פרויקט חדש</div><div class="ds">התחל מודל חדש — מאפס או מתוכנית לקוח (PDF/DWG)</div></div>
    <div class="hcard" id="cOpen"><div class="ic">📂</div><div class="ti">פרויקט קיים</div><div class="ds">המשך מאיפה שעצרת — הסוכן זוכר הכול</div></div>
  </div>
  <div id="panelNew" class="hidden">
    <h3 style="margin:0 0 12px">פרויקט חדש</h3>
    <input id="newName" placeholder="שם הפרויקט (למשל: מבנה פלדה - לקוח X)" autocomplete="off">
    <div class="hrow"><button class="btn ghost" onclick="backHome()">ביטול</button><button class="btn" id="newGo">צור והתחל ➤</button></div>
  </div>
  <div id="panelList" class="hidden">
    <h3 style="margin:0 0 12px">בחר פרויקט להמשך</h3>
    <div class="plist" id="plist"></div>
    <div class="hrow"><button class="btn ghost" onclick="backHome()">חזרה</button></div>
  </div>
</div>

<!-- ============ WORKSPACE ============ -->
<div id="ws" class="hidden">
  <header>
    <div class="logo" id="logo"></div>
    <div class="brand"><span class="t1">EB&nbsp;PROSTEEL&nbsp;AGENT</span><span class="t2">Steel Modeling AI</span></div>
    <button class="home-btn" id="homeBtn">⌂ בית</button>
    <button class="home-btn" id="stdBtn">📐 תקנים</button>
    <div class="stat">
      <span class="chip" id="cConn"><span class="dot" id="dConn"></span><span id="connTxt">ProSteel</span></span>
      <span class="chip hidden" id="cRec" style="border-color:#e0484d;color:#ff9b9b"><span class="dot" style="background:#ff4d4d;box-shadow:0 0 8px #ff4d4d;animation:bl 1s infinite"></span>מקליט</span>
      <span class="chip"><span class="dot on"></span><b id="entCnt">—</b><span>אלמנטים</span></span>
      <span class="chip clock" id="clock">--:--</span>
    </div>
  </header>
  <div class="wsgrid">
    <aside>
      <div class="card pj"><h4>פרויקט נוכחי</h4><div class="nm" id="pjName">—</div><div class="meta" id="pjMeta"></div>
        <button class="mini" id="openCadBtn" style="margin-top:9px;width:100%;padding:8px">🔗 התחבר ל-AutoCAD + ProSteel</button>
        <div class="fhint">פתח את AutoCAD+ProSteel ידנית, ואז לחץ כאן כדי שאתחבר אליהם.</div>
        <button class="mini" id="learnBtn" style="margin-top:8px;width:100%;padding:8px">🎓 מצב למידה</button>
        <div class="fhint">הדלק, עבוד רגיל ב-ProSteel — אני צופה ולומד ממך.</div></div>
      <div class="card"><h4>קבצי הפרויקט <button class="addf" id="addFileBtn" title="הוסף קובץ">＋ הוסף</button></h4>
        <input id="pfileInput" type="file" multiple style="display:none">
        <div class="filelist" id="fileList"><div class="empty">אין קבצים עדיין.</div></div>
        <div class="fhint">לחץ ✎ לסמן הערות על תכנית · ✕ למחיקה</div></div>
      <div class="card plan"><h4>תוכנית הלקוח</h4><div id="planBox"><div class="empty" style="font-size:12px;color:var(--dim)">לא נטענה תוכנית.</div></div></div>
      <div class="card"><h4>כלי ProSteel</h4><div id="palette">__PALETTE__</div></div>
    </aside>
    <main>
      <div id="log"></div>
      <div id="atts"></div>
      <div id="draw">
        <div class="palette">
          <span style="font-size:12px;color:var(--dim)">עיפרון</span>
          <span class="sw sel" style="background:#d40000" data-c="#d40000"></span>
          <span class="sw" style="background:#111" data-c="#111"></span>
          <span class="sw" style="background:#1e90ff" data-c="#1e90ff"></span>
          <span class="sw" style="background:#19c37d" data-c="#19c37d"></span>
          <span class="sw" style="background:#ffb454" data-c="#ffb454"></span>
          <input type="range" id="size" min="1" max="26" value="3" style="width:90px">
          <button class="mini" id="eraser">🩹 מחק</button><button class="mini" id="clearcv">נקה</button><button class="mini" id="attachSketch">➕ צרף לצ'אט</button><button class="mini" id="saveMark">💾 שמור לפרויקט</button>
        </div>
        <canvas id="cv"></canvas>
      </div>
      <div class="toprow"><button class="mini on" id="ttsBtn">🔊 הקראה</button><button class="mini" id="dirBtn">⇄ RTL</button><button class="mini" id="langBtn">EN</button></div>
      <div class="bar">
        <button class="iconb" id="mic">🎤</button>
        <button class="iconb" id="shot" title="צילום מסך">📸</button>
        <button class="iconb" id="drawBtn" title="סקיצה">✏️</button>
        <button class="iconb" id="fileBtn" title="קובץ">📎</button>
        <input id="file" type="file" multiple accept="image/*,.pdf,.dwg">
        <textarea id="t" placeholder="דבר או כתוב לסוכן... (לדוגמה: תעבור למבט TOP ותמדל 2 קורות HEB 500)"></textarea>
        <button class="send" id="send">שלח ➤</button>
      </div>
    </main>
  </div>
</div>
<div id="lb"><img id="lbimg"></div>
<div id="stdPanel">
  <div class="stdbar"><b>📐 תקנים — מאסטר תכן מבני פלדה (ישראלי + אירופאי)</b><button id="stdClose">✕ סגור</button></div>
  <div id="stdWrap">
    <div id="stdSide">
      <input id="stdSearch" placeholder="🔎 חפש פרמטר/תקן: γM, עומס רוח, סיווג חתך, EXC...">
      <div id="stdList"></div>
    </div>
    <div id="stdBody"></div>
  </div>
</div>

<script>
const $=s=>document.querySelector(s);
let attachments=[],since=0,lang='he',dir='rtl',tts=true,penColor='#d40000',erasing=false,working=null,started=false;
function setLogo(el){const im=new Image();im.onload=()=>{el.innerHTML='';el.appendChild(im);};im.onerror=()=>fetch('/logo.svg').then(r=>r.text()).then(t=>el.innerHTML=t).catch(()=>{});im.src='/logo';}
setLogo($('#hlogo'));setLogo($('#logo'));

/* ---- HOME ---- */
function backHome(){$('#panelNew').classList.add('hidden');$('#panelList').classList.add('hidden');$('#hcards').classList.remove('hidden');}
$('#cNew').onclick=()=>{$('#hcards').classList.add('hidden');$('#panelNew').classList.remove('hidden');$('#newName').focus();};
$('#cOpen').onclick=async()=>{$('#hcards').classList.add('hidden');const j=await(await fetch('/projects')).json();
 const pl=$('#plist');pl.innerHTML='';(j.projects||[]).forEach(p=>{const d=document.createElement('div');d.className='pitem';
   d.innerHTML='<div style="flex:1"><div class="pn">'+p.name+'</div><div class="pm">'+(p.analyzed?'תוכנית נותחה ✓ · ':'')+p.files+' קבצים</div></div><div style="color:var(--steel)">המשך ➤</div>';
   d.onclick=()=>enter(p.id);pl.appendChild(d);});
 if(!(j.projects||[]).length)pl.innerHTML='<div class="pm">אין פרויקטים עדיין. צור פרויקט חדש.</div>';
 $('#panelList').classList.remove('hidden');};
$('#newGo').onclick=async()=>{const name=$('#newName').value.trim();if(!name)return;
 const j=await(await fetch('/project/new',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({name})})).json();enter(j.id);};
$('#newName').addEventListener('keydown',e=>{if(e.key==='Enter')$('#newGo').click();});
async function enter(pid){await fetch('/project/set',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({id:pid})});
 $('#home').classList.add('hidden');$('#ws').classList.remove('hidden');startWorkspace();}
$('#homeBtn').onclick=()=>{$('#ws').classList.add('hidden');$('#home').classList.remove('hidden');backHome();};

/* ---- markdown + messages ---- */
function md(s){s=s.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
 s=s.replace(/`([^`]+)`/g,'<code>$1</code>').replace(/\*\*([^*]+)\*\*/g,'<b>$1</b>');
 const lines=s.split(/\n/);let h='',inL=false;
 for(let ln of lines){if(/^###?\s+/.test(ln)){if(inL){h+='</ul>';inL=false;}h+='<h3>'+ln.replace(/^###?\s+/,'')+'</h3>';}
   else if(/^\s*[-•]\s+/.test(ln)){if(!inL){h+='<ul>';inL=true;}h+='<li>'+ln.replace(/^\s*[-•]\s+/,'')+'</li>';}
   else{if(inL){h+='</ul>';inL=false;}h+=ln.trim()?('<p>'+ln+'</p>'):'';}}if(inL)h+='</ul>';return h;}
function add(role,text,files,asMd){const log=$('#log');const d=document.createElement('div');d.className='msg '+(role==='me'?'me':'ag');
 if(role==='ag'){const w=document.createElement('div');w.className='who';w.textContent='◆ EB AGENT';d.appendChild(w);}
 if(text){const c=document.createElement('div');if(asMd&&role==='ag')c.innerHTML=md(text);else c.textContent=text;d.appendChild(c);}
 (files||[]).forEach(f=>{const u=f.dataurl||f.url;
   if(u&&(/\.(png|jpe?g|gif)$/i.test(u)||/^data:image/.test(u))){const im=new Image();im.src=u;im.onclick=()=>{$('#lbimg').src=u;$('#lb').style.display='flex';};d.appendChild(im);}
   else{const s=document.createElement('div');s.className='file';s.textContent='📎 '+(f.name||'file');d.appendChild(s);}});
 log.appendChild(d);log.scrollTop=log.scrollHeight;return d;}
$('#lb').onclick=()=>$('#lb').style.display='none';
function stdBanner(){return '<div class="stdlinks">'
  +'<a href="https://ibr.sii.org.il/ibr/" target="_blank" rel="noopener">📖 קריאה חינם — תקנים רשמיים (IBR)</a>'
  +'<a href="https://www.sii.org.il/he/standards-search?numberQuery=1225" target="_blank" rel="noopener">🔎 דף ת"י 1225 ב-SII</a>'
  +'<a href="https://eurocodes.jrc.ec.europa.eu" target="_blank" rel="noopener">🇪🇺 Eurocodes — רקע (JRC)</a>'
  +'<span class="stdnote">מקרא: ✓ אומת מקובץ רשמי · ⭑ מבוסס-Eurocode (לא בהכרח הערך הישראלי המחייב) · ⚠ טעון אימות מול הנוסח המחייב / הנספח הלאומי. ℹ️ כלי-עזר — האחריות על מהנדס מוסמך.</span></div>';}
function stdSel(slug){document.querySelectorAll('.stdItem').forEach(e=>e.classList.toggle('on',e.dataset.slug===slug));}
function stdOverview(){stdSel('overview');fetch('/standards').then(r=>r.json()).then(j=>{
  let html=stdBanner()+md(j.md||'');
  if(j.pdfs&&j.pdfs.length)html+='<h3>קבצי תקן רשמיים שהוטמעו (✓)</h3><ul>'+j.pdfs.map(f=>'<li>📄 '+f+'</li>').join('')+'</ul>';
  else html+='<p style="color:var(--dim)">— לא הוטמעו קובצי PDF רשמיים עדיין. הנח קבצים חוקיים ב-<code>standards/pdfs</code> כדי לשדרג ידע מ-⭑ ל-✓.</p>';
  $('#stdBody').innerHTML=html;$('#stdBody').scrollTop=0;});}
function stdDoc(slug){stdSel(slug);fetch('/standards/doc?slug='+encodeURIComponent(slug)).then(r=>r.json()).then(j=>{
  $('#stdBody').innerHTML=md(j.md||'');$('#stdBody').scrollTop=0;});}
function stdItem(slug,label,sub){return '<div class="stdItem" data-slug="'+slug+'">'+label+(sub?'<span class="sc">'+sub+'</span>':'')+'</div>';}
function stdBuildList(){fetch('/standards/kb').then(r=>r.json()).then(j=>{
  let h='<div class="stdGrp">מבט-על</div>';
  h+=stdItem('overview','📋 סקירה כללית + מדיניות');
  h+=stdItem('version-status','🗓️ סטטוס מהדורות (מול SII)');
  h+=stdItem('national-annex-checklist','⚠️ נספח לאומי — לאימות');
  h+='<div class="stdGrp">מודולי ידע ('+(j.modules||[]).length+')</div>';
  (j.modules||[]).forEach(m=>{h+=stdItem(m.slug,m.title);});
  $('#stdList').innerHTML=h;
  document.querySelectorAll('#stdList .stdItem').forEach(e=>e.onclick=()=>{
    const s=e.dataset.slug;if(s==='overview')stdOverview();else stdDoc(s);});
});}
let _stdT=null;
function stdRunSearch(q){fetch('/standards/search?q='+encodeURIComponent(q)).then(r=>r.json()).then(j=>{
  const rs=j.results||[];stdSel('');
  if(!rs.length){$('#stdBody').innerHTML='<p style="color:var(--dim)">אין תוצאות ל-«'+q+'». נסה מונח אחר (למשל γM, רוח, סיווג חתך, גילוון).</p>';return;}
  let h='<h3>🔎 תוצאות חיפוש: «'+q+'»</h3>';
  rs.forEach(r=>{h+='<div class="stdItem" style="margin:6px 0" data-slug="'+r.slug+'"><b>'+r.title+'</b>'+(r.snippet?'<span class="sc">'+r.snippet+'</span>':'')+'</div>';});
  $('#stdBody').innerHTML=h;$('#stdBody').scrollTop=0;
  $('#stdBody').querySelectorAll('.stdItem').forEach(e=>e.onclick=()=>stdDoc(e.dataset.slug));});}
$('#stdSearch').addEventListener('input',function(){const q=this.value.trim();clearTimeout(_stdT);
  if(q.length<2){return;}_stdT=setTimeout(()=>stdRunSearch(q),250);});
$('#stdBtn').onclick=()=>{$('#stdPanel').classList.add('show');stdBuildList();stdOverview();};
$('#stdClose').onclick=()=>$('#stdPanel').classList.remove('show');
function speak(t){if(!tts||!t)return;try{const u=new SpeechSynthesisUtterance(t.replace(/[*#`>]/g,''));u.lang=/[֐-׿]/.test(t)?'he-IL':'en-US';speechSynthesis.cancel();speechSynthesis.speak(u);}catch(e){}}
function showWorking(){if(working)return;const log=$('#log');working=document.createElement('div');working.className='working';working.innerHTML='<span class="d"></span><span class="d"></span><span class="d"></span> הסוכן עובד...';log.appendChild(working);log.scrollTop=log.scrollHeight;}
function clearWorking(){if(working){working.remove();working=null;}}
function renderAtts(){const a=$('#atts');a.innerHTML='';attachments.forEach((x,i)=>{const c=document.createElement('div');c.className='a';c.innerHTML='📎 '+(x.name||'file');const b=document.createElement('button');b.textContent='×';b.onclick=()=>{attachments.splice(i,1);renderAtts();};c.appendChild(b);a.appendChild(c);});}
function isImg(n){return /\.(png|jpe?g|gif)$/i.test(n);}
function renderFiles(files){const fl=$('#fileList');if(!files||!files.length){fl.innerHTML='<div class="empty">אין קבצים עדיין.</div>';return;}
 fl.innerHTML='';files.forEach(f=>{const d=document.createElement('div');d.className='f';
  const nm=document.createElement('span');nm.className='fn';nm.textContent=(isImg(f)?'🖼 ':'📄 ')+f;d.appendChild(nm);
  if(isImg(f)){const a=document.createElement('button');a.className='fb';a.title='סמן הערות';a.textContent='✎';a.onclick=()=>annotateFile(f);d.appendChild(a);}
  const x=document.createElement('button');x.className='fb del';x.title='מחק';x.textContent='✕';x.onclick=()=>delFile(f);d.appendChild(x);
  fl.appendChild(d);});}
function delFile(name){if(!confirm('למחוק את '+name+'?'))return;fetch('/project/file/delete',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({name})}).then(()=>status());}
function annotateFile(name){const im=new Image();im.onload=()=>{if(window._setbg){window._setbg(im);add('ag','טענתי את **'+name+'** ללוח הסימון. סמן עליה, ואז "💾 שמור לפרויקט" או "➕ צרף לצ\'אט" + שלח — ואטמיע אותה במידול.',null,true);}};im.src='/pfile/'+encodeURIComponent(name);}

/* ---- workspace init (run once) ---- */
function startWorkspace(){ if(started)return; started=true;
 setInterval(()=>{const d=new Date();$('#clock').textContent=String(d.getHours()).padStart(2,'0')+':'+String(d.getMinutes()).padStart(2,'0');},1000);
 $('#fileBtn').onclick=()=>$('#file').click();
 $('#file').onchange=e=>{[...e.target.files].forEach(f=>{const r=new FileReader();r.onload=()=>{attachments.push({name:f.name,dataurl:r.result});renderAtts();};r.readAsDataURL(f);});};
 const cv=$('#cv'),x=cv.getContext('2d');let drawing=false,last=null,bg=null;
 window._fit=()=>{cv.width=cv.clientWidth;cv.height=260;x.lineCap='round';x.lineJoin='round';if(bg)x.drawImage(bg,0,0,cv.width,cv.height);};
 document.querySelectorAll('.sw').forEach(s=>s.onclick=function(){document.querySelectorAll('.sw').forEach(e=>e.classList.remove('sel'));this.classList.add('sel');penColor=this.dataset.c;erasing=false;$('#eraser').classList.remove('on');});
 $('#eraser').onclick=function(){erasing=!erasing;this.classList.toggle('on',erasing);};
 $('#clearcv').onclick=()=>{x.clearRect(0,0,cv.width,cv.height);bg=null;};
 function P(e){const r=cv.getBoundingClientRect();const t=e.touches?e.touches[0]:e;return{x:t.clientX-r.left,y:t.clientY-r.top};}
 function dn(e){drawing=true;last=P(e);e.preventDefault();}
 function mv(e){if(!drawing)return;const p=P(e);x.strokeStyle=erasing?'#fff':penColor;x.lineWidth=erasing?22:(+$('#size').value);x.beginPath();x.moveTo(last.x,last.y);x.lineTo(p.x,p.y);x.stroke();last=p;e.preventDefault();}
 cv.addEventListener('mousedown',dn);cv.addEventListener('mousemove',mv);window.addEventListener('mouseup',()=>drawing=false);
 cv.addEventListener('touchstart',dn);cv.addEventListener('touchmove',mv);window.addEventListener('touchend',()=>drawing=false);
 $('#drawBtn').onclick=function(){const sh=$('#draw').style.display!=='block';$('#draw').style.display=sh?'block':'none';this.classList.toggle('on',sh);if(sh)setTimeout(window._fit,10);};
 $('#attachSketch').onclick=()=>{attachments.push({name:'sketch.png',dataurl:cv.toDataURL('image/png')});renderAtts();};
 $('#addFileBtn').onclick=()=>$('#pfileInput').click();
 $('#pfileInput').onchange=e=>{const files=[...e.target.files];let done=0;if(!files.length)return;files.forEach(f=>{const r=new FileReader();r.onload=()=>{fetch('/project/file/add',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({name:f.name,dataurl:r.result})}).then(()=>{if(++done===files.length)status();});};r.readAsDataURL(f);});};
 $('#saveMark').onclick=()=>{const name='mark_'+($('#log').childElementCount)+'_'+Math.floor(performance.now())+'.png';fetch('/project/file/add',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({name,dataurl:cv.toDataURL('image/png')})}).then(()=>{status();add('ag','שמרתי את הסימון לקבצי הפרויקט: **'+name+'**',null,true);});};
 $('#openCadBtn').onclick=function(){const btn=this;btn.textContent='🔗 מתחבר...';btn.disabled=true;
   add('ag','מתחבר ל-AutoCAD + ProSteel שפתחת... (טוען את מנוע המידול)',null,true);showWorking();
   fetch('/project/connect',{method:'POST',headers:{'Content-Type':'application/json'},body:'{}'}).then(r=>r.json()).then(j=>{
     clearWorking();btn.textContent='🔗 התחבר ל-AutoCAD + ProSteel';btn.disabled=false;
     add('ag', j.ok?('✅ מחובר! הקובץ **'+j.dwg+'** '+(j.action==='created'?'נוצר ו':'')+'מוכן. אפשר להתחיל למדל — תגיד לי מה לבנות!'):('❌ '+(j.error||'לא הצלחתי להתחבר')),null,true);
   }).catch(e=>{clearWorking();btn.textContent='🔗 התחבר ל-AutoCAD + ProSteel';btn.disabled=false;add('ag','שגיאה: '+e);});};
 $('#learnBtn').onclick=function(){const b=this;b.disabled=true;
   fetch('/learn/toggle',{method:'POST',headers:{'Content-Type':'application/json'},body:'{}'}).then(r=>r.json()).then(j=>{
     b.disabled=false;
     if(j.action==='started'&&j.ok){add('ag','🎓 מצב למידה פעיל — עבוד רגיל ב-ProSteel, אני צופה ולומד ממך. לחץ שוב לעצירה.',null,true);}
     else if(j.action==='stopped'){add('ag','⏹️ עצרתי את ההקלטה. תיעדתי '+(j.cmds||0)+' פקודות ו-'+(j.objs||0)+' אובייקטים. רוצה שאנתח? כתוב: **נתח את הלמידה**',null,true);}
     else{add('ag','לא הצלחתי: '+(j.error||'ודא ש-AutoCAD מחובר')); }
   }).catch(e=>{b.disabled=false;add('ag','שגיאה: '+e);});};
 window._setbg=(im)=>{bg=im;$('#draw').style.display='block';$('#drawBtn').classList.add('on');setTimeout(window._fit,10);};
 $('#shot').onclick=function(){this.classList.add('live');fetch('/screenshot').then(r=>r.json()).then(j=>{this.classList.remove('live');if(j.dataurl){const im=new Image();im.onload=()=>window._setbg(im);im.src=j.dataurl;}}).catch(e=>{this.classList.remove('live');alert('screenshot failed');});};
 const SR=window.SpeechRecognition||window.webkitSpeechRecognition;let rec=null,listening=false;
 $('#mic').onclick=function(){if(!SR){alert('קול לא נתמך כאן. השתמש ב-Win+H.');return;}if(listening){rec.stop();return;}rec=new SR();rec.lang=lang==='he'?'he-IL':'en-US';rec.interimResults=true;const base=$('#t').value;rec.onresult=e=>{let s='';for(const r of e.results)s+=r[0].transcript;$('#t').value=(base?base+' ':'')+s;};rec.onstart=()=>{listening=true;$('#mic').classList.add('live');};rec.onend=()=>{listening=false;$('#mic').classList.remove('live');};rec.start();};
 $('#ttsBtn').onclick=function(){tts=!tts;this.classList.toggle('on',tts);};
 $('#dirBtn').onclick=function(){dir=dir==='rtl'?'ltr':'rtl';$('#root').dir=dir;this.textContent='⇄ '+dir.toUpperCase();};
 $('#langBtn').onclick=function(){lang=lang==='he'?'en':'he';this.textContent=lang==='he'?'EN':'עב';};
 document.querySelectorAll('.tool').forEach(b=>b.onclick=()=>{const cmd=b.dataset.cmd;add('me','🔧 '+cmd);showWorking();
   fetch('/fire',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({cmd})}).then(r=>r.json()).then(j=>{clearWorking();add('ag',j.ok?('פתחתי את הכלי **'+cmd+'** ב-AutoCAD.'):('לא הצלחתי: '+(j.error||'')+' (ודא ש-AutoCAD פתוח)'),null,true);}).catch(e=>{clearWorking();add('ag','שגיאה: '+e);});});
 $('#send').onclick=send;
 $('#t').addEventListener('keydown',e=>{if(e.key==='Enter'&&!e.shiftKey){e.preventDefault();send();}});
 loadHistory().then(poll);status();
}
async function send(){const t=$('#t').value.trim();if(!t&&!attachments.length)return;
 add('me',t,attachments);const payload={text:t,attachments};$('#t').value='';attachments=[];renderAtts();showWorking();
 try{const r=await fetch('/send',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(payload)});const j=await r.json();
   if(j&&j.reply){clearWorking();add('ag',j.reply,null,true);speak(j.reply);}
 }catch(e){clearWorking();add('ag','(שרת לא זמין)');}}
async function loadHistory(){try{const j=await(await fetch('/history')).json();$('#log').innerHTML='';(j.conversation||[]).forEach(m=>add(m.role==='user'?'me':'ag',m.text,m.files,true));since=j.outbox_count||0;}catch(e){}}
async function poll(){try{const j=await(await fetch('/poll?since='+since)).json();if(j.messages.length){clearWorking();j.messages.forEach(m=>{add('ag',m.text,null,true);speak(m.text);});}since=j.next;}catch(e){}setTimeout(poll,400);}
async function status(){try{const j=await(await fetch('/status')).json();
 $('#dConn').classList.toggle('on',!!j.connected);$('#connTxt').textContent=j.connected?'ProSteel מחובר':'ProSteel';
 $('#cRec').classList.toggle('hidden',!j.learning);$('#learnBtn').classList.toggle('on',!!j.learning);if($('#learnBtn'))$('#learnBtn').textContent=j.learning?'⏹️ עצור למידה':'🎓 מצב למידה';
 if(j.entities!=null)$('#entCnt').textContent=j.entities;
 const p=j.project||'—';$('#pjName').textContent=p;if(j.project_meta)$('#pjMeta').textContent=j.project_meta;
 renderFiles(j.files||[]);
 if(j.plan_url){if(!$('#planBox img')){$('#planBox').innerHTML='<img src="'+j.plan_url+'">';$('#planBox img').onclick=function(){$('#lbimg').src=this.src;$('#lb').style.display='flex';};}}
}catch(e){}setTimeout(status,5000);}
</script></body></html>"""


class Handler(BaseHTTPRequestHandler):
    def log_message(self, *a):
        pass

    def _bytes(self, b, ctype, code=200):
        self.send_response(code)
        self.send_header("Content-Type", ctype)
        self.send_header("Content-Length", str(len(b)))
        self.end_headers()
        self.wfile.write(b)

    def _json(self, obj, code=200):
        self._bytes(json.dumps(obj, ensure_ascii=False).encode("utf-8"),
                    "application/json; charset=utf-8", code)

    def _body(self):
        length = int(self.headers.get("Content-Length", 0))
        return json.loads(self.rfile.read(length).decode("utf-8")) if length else {}

    def do_GET(self):
        p = self.path.split("?")[0]
        if p.startswith("/uploads/"):
            return self._file(UPLOADS, os.path.basename(p))
        if p.startswith("/pfile/"):
            pp = P()
            return self._file(pp["files"], os.path.basename(p)) if pp else self._bytes(b"", "image/png", 404)
        if self.path.startswith("/poll"):
            return self._poll()
        if p == "/history":
            pp = P()
            conv = _read(pp["conv"]) if pp else []
            oc = len(_read(pp["outbox"])) if pp else 0
            return self._json({"conversation": conv, "outbox_count": oc})
        if p == "/status":
            return self._status()
        if p == "/projects":
            return self._json({"projects": list_projects()})
        if p == "/logo":
            for d in (BRAND, ASSETS):
                for ext in ("png", "jpg", "jpeg", "svg"):
                    fp = os.path.join(d, "logo." + ext)
                    if os.path.exists(fp):
                        return self._bytes(open(fp, "rb").read(),
                                           "image/svg+xml" if ext == "svg" else "image/" + ext)
            return self._bytes(b"", "image/png", 404)
        if p == "/logo.svg":
            return self._bytes(DEFAULT_LOGO.encode("utf-8"), "image/svg+xml")
        if p == "/standards":
            fp = os.path.join(ROOT, "standards", "STANDARDS.md")
            txt = open(fp, encoding="utf-8").read() if os.path.exists(fp) else "# אין קובץ תקנים"
            # list purchased official PDFs, if any
            pdfs = []
            pd = os.path.join(ROOT, "standards", "pdfs")
            if os.path.isdir(pd):
                pdfs = [f for f in sorted(os.listdir(pd)) if f.lower().endswith(".pdf")]
            return self._json({"md": txt, "pdfs": pdfs})
        if p == "/standards/kb":
            try:
                import standards_kb
                return self._json({"modules": standards_kb.modules(),
                                   "legend": standards_kb._MARK_LEGEND})
            except Exception as e:
                return self._json({"modules": [], "error": str(e)})
        if p == "/standards/doc":
            slug = ""
            if "slug=" in self.path:
                from urllib.parse import unquote
                slug = unquote(self.path.split("slug=")[1].split("&")[0])
            # special repository docs live under standards/ (not kb/)
            special = {"version-status": "version-status.md",
                       "national-annex-checklist": "national-annex-checklist.md",
                       "overview": "STANDARDS.md"}
            if slug in special:
                fp = os.path.join(ROOT, "standards", special[slug])
                md = open(fp, encoding="utf-8").read() if os.path.exists(fp) else "# לא נמצא"
                return self._json({"md": md, "slug": slug})
            try:
                import standards_kb
                md = standards_kb.module_md(slug)
                if md is None:      # also serve the steel-modeling teaching folder
                    for d in (os.path.join(ROOT, "knowledge", "steel"),
                              os.path.join(ROOT, "knowledge")):
                        for cand in (slug + ".md", slug):
                            fp = os.path.join(d, cand)
                            if os.path.exists(fp):
                                md = open(fp, encoding="utf-8").read()
                                break
                        if md:
                            break
                return self._json({"md": md or "# מודול לא נמצא", "slug": slug})
            except Exception as e:
                return self._json({"md": "# שגיאה: " + str(e)})
        if p == "/standards/search":
            q = ""
            if "q=" in self.path:
                from urllib.parse import unquote
                q = unquote(self.path.split("q=")[1].split("&")[0]).replace("+", " ")
            try:
                import standards_kb
                return self._json({"results": standards_kb.search(q, 8), "q": q})
            except Exception as e:
                return self._json({"results": [], "error": str(e)})
        if p == "/homebg":
            for ext in ("jpg", "jpeg", "png"):
                fp = os.path.join(ASSETS, "home_bg." + ext)
                if os.path.exists(fp):
                    return self._bytes(open(fp, "rb").read(),
                                       "image/jpeg" if ext in ("jpg", "jpeg") else "image/png")
            return self._bytes(HOME_BG_SVG.encode("utf-8"), "image/svg+xml")
        if p == "/screenshot":
            return self._screenshot()
        return self._bytes(build_page().encode("utf-8"), "text/html; charset=utf-8")

    def _file(self, folder, name):
        fp = os.path.join(folder, name)
        if os.path.exists(fp):
            ext = os.path.splitext(fp)[1].lstrip(".").lower() or "png"
            ct = {"jpg": "image/jpeg", "jpeg": "image/jpeg", "png": "image/png",
                  "pdf": "application/pdf", "dwg": "application/octet-stream"}.get(ext, "application/octet-stream")
            return self._bytes(open(fp, "rb").read(), ct)
        return self._bytes(b"", "image/png", 404)

    def _poll(self):
        since = 0
        if "since=" in self.path:
            try:
                since = int(self.path.split("since=")[1].split("&")[0])
            except ValueError:
                since = 0
        pp = P()
        out = _read(pp["outbox"]) if pp else []
        msgs = [{"i": i, "text": m.get("text", "")} for i, m in enumerate(out) if i >= since]
        return self._json({"messages": msgs, "next": len(out)})

    def _status(self):
        pid = active_id()
        info = {"connected": False, "entities": None,
                "project": project_title(pid) if pid else "", "files": []}
        try:
            ls = json.load(open(os.path.join(DATA, "learning.json"), encoding="utf-8"))
            info["learning"] = bool(ls.get("on"))
        except Exception:
            info["learning"] = False
        pp = P()
        if pp:
            if os.path.isdir(pp["files"]):
                info["files"] = sorted(os.listdir(pp["files"]))[:30]
            if os.path.exists(pp["analysis"]):
                info["project_meta"] = "תוכנית נותחה ✓"
            # plan thumbnail: ONLY this project's own image files (no global fallback)
            imgs = [f for f in info["files"] if f.lower().endswith((".png", ".jpg", ".jpeg"))]
            pref = [f for f in imgs if f.lower().startswith("plan") or "plan" in f.lower()]
            pick = pref or imgs
            if pick:
                info["plan_url"] = "/pfile/" + pick[0]
        try:
            import pythoncom
            import win32com.client
            pythoncom.CoInitialize()
            try:
                app = win32com.client.GetActiveObject("AutoCAD.Application")
                info["connected"] = True
                try:
                    if bool(app.GetAcadState().IsQuiescent):
                        info["entities"] = app.ActiveDocument.ModelSpace.Count
                except Exception:
                    pass
            finally:
                pythoncom.CoUninitialize()
        except Exception:
            pass
        return self._json(info)

    def _screenshot(self):
        out = os.path.join(UPLOADS, "screen.png")
        ps = ("Add-Type -AssemblyName System.Windows.Forms,System.Drawing;"
              "$b=[System.Windows.Forms.SystemInformation]::VirtualScreen;"
              "$bmp=New-Object System.Drawing.Bitmap $b.Width,$b.Height;"
              "$g=[System.Drawing.Graphics]::FromImage($bmp);"
              "$g.CopyFromScreen($b.X,$b.Y,0,0,$bmp.Size);"
              "$bmp.Save('" + out.replace("\\", "\\\\") + "',[System.Drawing.Imaging.ImageFormat]::Png)")
        try:
            subprocess.run(["powershell", "-NoProfile", "-Command", ps], timeout=20, capture_output=True)
            durl = "data:image/png;base64," + base64.b64encode(open(out, "rb").read()).decode()
            return self._json({"dataurl": durl})
        except Exception as e:
            return self._json({"error": str(e)}, 500)

    def do_POST(self):
        data = self._body()
        p = self.path
        if p.startswith("/project/new"):
            pid = create_project(data.get("name", "project"))
            set_active(pid)
            return self._json({"ok": True, "id": pid, "name": project_title(pid)})
        if p.startswith("/project/set"):
            set_active(data.get("id", ""))
            return self._json({"ok": True})
        if p.startswith("/project/connect"):
            pid = active_id()
            if not pid:
                return self._json({"ok": False, "error": "no active project"}, 400)
            dwg = os.path.join(PROJECTS, pid, pid + ".dwg")
            try:
                import eb_api
                return self._json(eb_api.connect_project(dwg))   # connects, never launches
            except Exception as e:
                return self._json({"ok": False, "error": str(e)})
        if p.startswith("/learn/toggle"):
            try:
                import eb_api
                if eb_api.learn_state().get("on"):
                    return self._json({"action": "stopped", **eb_api.learn_stop()})
                return self._json({"action": "started", **eb_api.learn_start()})
            except Exception as e:
                return self._json({"ok": False, "error": str(e)})
        if p.startswith("/send"):
            return self._send(data)
        if p.startswith("/fire"):
            return self._fire(data.get("cmd", ""))
        if p.startswith("/project/file/delete"):
            pp = P()
            if pp:
                fp = os.path.join(pp["files"], os.path.basename(data.get("name", "")))
                if os.path.exists(fp):
                    os.remove(fp)
                    return self._json({"ok": True})
            return self._json({"ok": False}, 400)
        if p.startswith("/project/file/add"):
            pp = P()
            if not pp:
                return self._json({"ok": False, "error": "no project"}, 400)
            name = os.path.basename(data.get("name", "file"))
            durl = data.get("dataurl", "")
            raw = base64.b64decode(durl.split(",", 1)[1]) if "," in durl else b""
            open(os.path.join(pp["files"], name), "wb").write(raw)
            return self._json({"ok": True, "name": name})
        return self._json({"error": "unknown"}, 404)

    def _send(self, data):
        pp = P()
        if not pp:
            return self._json({"error": "no active project"}, 400)
        inbox = _read(pp["inbox"])
        idx = len(inbox)
        saved = []
        for k, a in enumerate(data.get("attachments", [])):
            name = a.get("name", f"file{k}")
            durl = a.get("dataurl", "")
            ext = os.path.splitext(name)[1] or ".bin"
            raw = base64.b64decode(durl.split(",", 1)[1]) if "," in durl else b""
            base = os.path.splitext(os.path.basename(name))[0][:40] or f"file{k}"
            fn = f"{idx:03d}_{base}{ext}"
            open(os.path.join(pp["files"], fn), "wb").write(raw)
            saved.append("/pfile/" + fn)
        text = data.get("text", "")
        _append(pp["conv"], {"role": "user", "text": text,
                             "files": [{"url": u} for u in saved]})
        # TIER 1: a recognized command with no attachments runs instantly in-server
        if not saved:
            t0 = time.time()
            reply = _quick_action(text)
            if reply:
                reply = "%s  ⚡%.1fs" % (reply, time.time() - t0)
                _append(pp["conv"], {"role": "agent", "text": reply})
                return self._json({"ok": True, "reply": reply})
        # TIER 2: complex/ambiguous -> instant ack + queue for the Claude planner
        _append(pp["inbox"], {"i": idx, "text": text, "files": saved})
        ack = "🧠 קיבלתי — בקשה מורכבת, מעביר לתכנון. אענה כאן בקרוב."
        _append(pp["conv"], {"role": "agent", "text": ack})
        return self._json({"ok": True, "i": idx, "reply": ack})

    def _fire(self, cmd):
        if not cmd:
            return self._json({"error": "no command"}, 400)
        try:
            import pythoncom
            import win32com.client
            pythoncom.CoInitialize()
            try:
                app = win32com.client.GetActiveObject("AutoCAD.Application")
                try:
                    if not bool(app.GetAcadState().IsQuiescent):
                        return self._json({"ok": False, "error": "AutoCAD עסוק — לחץ ESC או סגור חלון פתוח ונסה שוב"})
                except Exception:
                    pass
                app.ActiveDocument.SendCommand("\x1b\x1b_" + cmd + " ")
            finally:
                pythoncom.CoUninitialize()
            pp = P()
            if pp:
                _append(pp["conv"], {"role": "user", "text": "🔧 " + cmd, "files": []})
            return self._json({"ok": True})
        except Exception as e:
            return self._json({"ok": False, "error": str(e)})


# ---------- agent-side CLI (operates on the ACTIVE project) ----------
def cli_wait(timeout=300):
    """Block until the next user message on the ACTIVE project's console.
    Waits patiently even if no project is active yet (user still setting up)."""
    deadline = time.time() + timeout
    while time.time() < deadline:
        pp = P()
        if pp:
            cur = int(open(pp["cursor"]).read().strip()) if os.path.exists(pp["cursor"]) else 0
            inbox = _read(pp["inbox"])
            if len(inbox) > cur:
                msg = inbox[cur]
                open(pp["cursor"], "w").write(str(cur + 1))
                print(json.dumps({"project": active_id(), **msg}, ensure_ascii=False, indent=2))
                return
        time.sleep(0.5)
    print(json.dumps({"status": "timeout", "waited": timeout}))


def cli_say(text):
    pp = P()
    if not pp:
        print("no active project")
        return
    _append(pp["outbox"], {"text": text, "ts": time.strftime("%H:%M:%S")})
    _append(pp["conv"], {"role": "agent", "text": text, "files": []})
    print("posted to console")


def cli_status():
    pid = active_id()
    pp = P()
    inbox = _read(pp["inbox"]) if pp else []
    outbox = _read(pp["outbox"]) if pp else []
    cur = int(open(pp["cursor"]).read().strip()) if pp and os.path.exists(pp["cursor"]) else 0
    print(json.dumps({"active_project": pid, "inbox_total": len(inbox),
                      "unread": len(inbox) - cur, "outbox_total": len(outbox)}, indent=2))


if __name__ == "__main__":
    args = sys.argv[1:]
    if args and args[0] == "wait":
        cli_wait(int(args[1]) if len(args) > 1 else 300)
    elif args and args[0] == "say":
        cli_say(args[1] if len(args) > 1 else "")
    elif args and args[0] in ("status", "peek"):
        cli_status()
    else:
        print(f"EB PROSTEEL AGENT console -> http://localhost:{PORT}")
        print("Keep this window open. Ctrl+C to stop.")
        ThreadingHTTPServer(("127.0.0.1", PORT), Handler).serve_forever()
