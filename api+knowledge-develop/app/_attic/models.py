"""
models.py - per-model folder + memory manager for the Eretz Barzel agent.

Every model (tank/project) you build gets ITS OWN folder under  AcadAgent\\models\\ :

    models\\
        INDEX.md                 <- list of all your models
        Tank-A\\
            Tank-A.dwg           <- the AutoCAD drawing (saved here)
            MODEL.md             <- the model's MEMORY: dimensions, standard,
                                    decisions, history. The agent reads this
                                    when you come back to keep editing.
            (your ProSteel files live here too)

Usage:
    python models.py new "Tank-A"      # create a new model folder + memory + save dwg
    python models.py list              # list all models
    python models.py open "Tank-A"     # open that model's drawing in AutoCAD

Free. No API key needed.
"""

import os
import sys
import datetime

ROOT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "projects")


MODEL_TEMPLATE = """# MODEL MEMORY - {name}

> This file is the agent's memory for this model. When you return to keep
> working, the agent reads this first to remember everything about it.

- **Name:** {name}
- **Created:** {date}
- **Status:** in progress
- **Drawing file:** {name}.dwg
- **ProSteel files:** (kept in this folder)

## Specification
- Installation: (aboveground / underground)
- Governing standard: (UL 142 / UL 58)
- Construction type: (single / secondary / diked / Type I / Type II)
- Diameter (mm):
- Height / Length (mm):
- Shell thickness (mm):
- Capacity:

## Key decisions
- (the agent records important choices here)

## History log
- {date} - model folder created.
"""


def _safe(name):
    keep = "-_ "
    return "".join(c for c in name if c.isalnum() or c in keep).strip().replace(" ", "-")


def new_model(name):
    safe = _safe(name)
    folder = os.path.join(ROOT, safe)
    os.makedirs(folder, exist_ok=True)
    date = datetime.date.today().isoformat()

    md = os.path.join(folder, "MODEL.md")
    if not os.path.exists(md):
        with open(md, "w", encoding="utf-8") as f:
            f.write(MODEL_TEMPLATE.format(name=safe, date=date))

    # try to save the current AutoCAD drawing into this folder
    dwg = os.path.join(folder, safe + ".dwg")
    saved = "(AutoCAD not running - drawing not saved yet)"
    try:
        from acad import Acad
        a = Acad()
        saved = a.save_as(dwg)
    except Exception as e:
        saved = f"(could not save dwg now: {e})"

    _update_index()
    return f"created model folder: {folder}\n  memory: {md}\n  {saved}"


def list_models():
    if not os.path.isdir(ROOT):
        return "(no models yet)"
    rows = []
    for d in sorted(os.listdir(ROOT)):
        p = os.path.join(ROOT, d)
        if os.path.isdir(p):
            has_dwg = any(f.lower().endswith(".dwg") for f in os.listdir(p))
            rows.append(f"  - {d}" + ("  [dwg]" if has_dwg else ""))
    return "Models:\n" + ("\n".join(rows) if rows else "  (none)")


def open_model(name):
    safe = _safe(name)
    dwg = os.path.join(ROOT, safe, safe + ".dwg")
    if not os.path.exists(dwg):
        return f"no drawing found at {dwg}"
    from acad import Acad
    a = Acad()
    return a.open_drawing(dwg)


def _update_index():
    os.makedirs(ROOT, exist_ok=True)
    lines = ["# Models Index", "",
             "All models built with the Eretz Barzel agent. One folder each.", ""]
    for d in sorted(os.listdir(ROOT)):
        p = os.path.join(ROOT, d)
        if os.path.isdir(p):
            lines.append(f"- **{d}** - `models/{d}/` (memory: `models/{d}/MODEL.md`)")
    with open(os.path.join(ROOT, "INDEX.md"), "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")


if __name__ == "__main__":
    args = sys.argv[1:]
    if not args:
        print(__doc__)
    elif args[0] == "new" and len(args) > 1:
        print(new_model(args[1]))
    elif args[0] == "list":
        print(list_models())
    elif args[0] == "open" and len(args) > 1:
        print(open_model(args[1]))
    else:
        print("usage: python models.py [new <name> | list | open <name>]")
