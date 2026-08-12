"""
prosteel.py - ProSteel quick-command library for EB PROSTEEL AGENT.

Gives the agent a fast vocabulary: a natural request (Hebrew or English) maps to
the exact ProSteel command, which is fired into AutoCAD via the acad bridge.

This is how the agent "controls ProSteel like a pro" - it knows which of the
82 ProSteel commands to use, instantly.

Usage:
    from prosteel import dispatch, run, CATALOG
    dispatch("תכניס קורה")     -> opens the Shapes tool (PS_INS_PROF)
    dispatch("add bolts")       -> opens the Bolt dialog (PS_BOLT)
    run("PS_POS")               -> fire a command directly

Note: most ProSteel commands open a DIALOG (pick profile/params there). The agent
fires the right tool instantly; the modeler confirms parameters. Pure geometry
(lines, copy, arrays) is done via acad.py. See knowledge/KNOWLEDGE.md.
"""

# key -> (ProSteel command, human description)
CATALOG = {
    # ---- core modeling ----
    "shape":        ("PS_INS_PROF",      "Insert a shape/beam/column from the SHAPES database"),
    "plate":        ("PS_PLATE",         "Insert a flat plate / polyplate"),
    "bend_plate":   ("PS_ADD_PLATE_FLANGE", "Add a bent/edged segment to a plate"),
    "solid":        ("PS_MODIFY",        "Modify 3D element"),
    "stiffener":    ("PS_RIP",           "Insert stiffeners in a shape"),
    "shape_segment":("PS_ADD_SECTION",   "Add a cranked/bent segment to a shape"),
    # ---- holes & bolts ----
    "bolt":         ("PS_BOLT",          "Add bolts / nuts / washers / holes"),
    "anchor":       ("PS_ANCHORBOLT",    "Anchor bolts / dowels / studs"),
    "drill":        ("PS_DRILL",         "Drill holes for bolted connections"),
    # ---- connections ----
    "endplate":     ("PS_ENDPLATE",      "End plate connection"),
    "splice":       ("PS_LASCHE",        "Splice connection between colinear shapes"),
    "shear_plate":  ("PS_SCHEARPLATE",   "Shear plate (web) connection"),
    "web_angle":    ("PS_STEGW",         "Web angle connection"),
    "gusset":       ("PS_GUSSET_PLATE",  "Gusset plate connection"),
    "baseplate":    ("PS_GROUNDPL",      "DSTV base plate"),
    "haunch":       ("PS_VOUTE",         "Haunch"),
    "cope":         ("PS_NOTCH",         "Cope / notch a shape end"),
    "purlin_conn":  ("PS_PURLIN_CONN",   "Purlin-to-girder connection"),
    "edit_conn":    ("PS_EDIT_CONNECTIONS", "Connection Editor - edit/exchange connections"),
    # ---- structures / macros ----
    "frame":        ("PS_FRAME",         "Portal frame"),
    "truss":        ("PS_TRUSS",         "Truss"),
    "bracing":      ("PS_BRACING",       "Bracing + gusset connections"),
    "static_brace": ("PS_VERBAND",       "Static bracing"),
    "purlin":       ("PS_PFETTE",        "Purlin"),
    "joist":        ("PS_JOIST",         "American joists"),
    "stairs":       ("PS_STAIRS",        "Straight stair"),
    "circ_stairs":  ("PS_CIRCULAR_STAIRS","Circular stair"),
    "handrail":     ("PS_HANDRAIL",      "Handrail along a polyline"),
    "ladder":       ("PS_LADDER",        "Ladder"),
    # ---- frames / grids / views ----
    "workframe":    ("PS_WORKFRAME",     "Create a workframe (grid)"),
    "construction": ("PS_CONST",         "Construction lines / measure"),
    "view":         ("PS_SETBKS",        "Select / manage views"),
    # ---- move / copy ----
    "copy":         ("PS_COPY",          "Copy / mirror / move / align (with steel constraints)"),
    # ---- organize ----
    "group":        ("PS_GROUP",         "Manage groups / sub-groups / assemblies"),
    "family":       ("PS_FAMILY_CLASS",  "Organize parts into Part Families"),
    "search":       ("PS_SEARCH",        "Search parts by criteria"),
    "collision":    ("PS_COLLISION",     "Collision check"),
    # ---- detailing / production (the SHOP DRAWING goal) ----
    "position":     ("PS_POS",           "Position numbers / flags for parts"),
    "partlist":     ("PS_CREATE_PARTLIST","Parts list processing"),
    "detail":       ("PS_DETCENTER",     "DetailCenter - generate 2D shop drawings"),
    "detail_exp":   ("PS_DETCENTER_EXPRESS", "DetailCenter Express"),
    "nc":           ("PS_NC_DATA",       "Generate NC/CNC data for the factory"),
    "dim":          ("PS_DIM",           "Dimensions for 2D detailing"),
    "elevation":    ("PS_KOTE",          "Elevation flags"),
    "annotate":     ("PS_TEXTFLAG",      "Annotation labels"),
    # ---- data ----
    "export":       ("PS_EXPORT",        "Export model data (exchange files)"),
    "import":       ("PS_IMPORT",        "Import data from exchange files"),
    "project":      ("PS_PROJECT",       "ProSteel project settings management"),
    "settings":     ("PS_GLOBAL_SETTINGS","ProSteel global options"),
}

# natural-language phrases (he + en) -> catalog key
ALIASES = {
    "shape": ["beam", "column", "shape", "profile", "girder", "member",
              "קורה", "עמוד", "פרופיל", "קורות", "עמודים", "אלמנט"],
    "plate": ["plate", "gusset plate flat", "לוח", "פלטה", "פלטות", "לוחות"],
    "bolt": ["bolt", "bolts", "ברגים", "בורג", "ברגיים"],
    "anchor": ["anchor", "anchor bolt", "עוגן", "בורג עיגון"],
    "drill": ["drill", "hole", "holes", "קדח", "חורים", "חור"],
    "endplate": ["endplate", "end plate", "פלטת קצה", "אנדפלייט"],
    "splice": ["splice", "חיבור איחוי", "ספלייס"],
    "shear_plate": ["shear plate", "shear", "פלטת גזירה"],
    "web_angle": ["web angle", "זווית", "זווית חיבור"],
    "gusset": ["gusset", "צלחת קשר", "גאסט"],
    "baseplate": ["baseplate", "base plate", "פלטת בסיס", "בייספלייט"],
    "haunch": ["haunch", "הגבהה", "האנץ"],
    "cope": ["cope", "notch", "קופה", "חיתוך קצה"],
    "frame": ["portal frame", "frame", "מסגרת", "פורטל"],
    "truss": ["truss", "סבכה", "מסבך"],
    "bracing": ["brace", "bracing", "גיברית", "ייצוב", "אלכסונים"],
    "purlin": ["purlin", "מרזב", "פלזה", "פורלין"],
    "stairs": ["stair", "stairs", "מדרגות", "גרם מדרגות"],
    "handrail": ["handrail", "railing", "מעקה"],
    "ladder": ["ladder", "סולם"],
    "workframe": ["workframe", "grid", "axis", "axes", "צירים", "רשת", "גריד"],
    "copy": ["copy", "mirror", "move", "align", "העתק", "שכפל", "שיקוף", "הזז"],
    "group": ["group", "assembly", "קבוצה", "הרכבה", "אסמבלי"],
    "position": ["position", "pos number", "מספור", "מספרי פוזיציה", "פוזיציות"],
    "partlist": ["partlist", "parts list", "bom", "רשימת חלקים", "כתב כמויות"],
    "detail": ["shop drawing", "detail", "detailcenter", "תוכניות ייצור",
               "תוכניות יצור", "פירוט", "שופ דרואינג"],
    "nc": ["nc", "cnc", "nc data", "נתוני יצור", "סיאנסי"],
    "collision": ["collision", "clash", "התנגשות", "בדיקת התנגשות"],
    "settings": ["settings", "options", "הגדרות"],
    "project": ["project", "פרויקט"],
}


def resolve(text):
    """Return the catalog key best matching a natural request, or None."""
    t = (text or "").lower()
    best = None
    for key, words in ALIASES.items():
        for w in words:
            if w.lower() in t:
                # prefer the longest matching phrase
                if best is None or len(w) > best[1]:
                    best = (key, len(w))
    return best[0] if best else None


def command_for(text):
    """Return (PS_COMMAND, description) for a natural request, or None."""
    key = resolve(text)
    if key:
        return CATALOG[key]
    return None


def run(cmd_or_key, acad=None):
    """Fire a ProSteel command into AutoCAD. Accepts a PS_* command, a catalog
    key, or natural text. Returns a status string."""
    cmd = None
    if cmd_or_key in CATALOG:
        cmd = CATALOG[cmd_or_key][0]
    elif cmd_or_key.upper().startswith(("PS_", "PC_", "PSN_")):
        cmd = cmd_or_key.upper()
    else:
        hit = command_for(cmd_or_key)
        cmd = hit[0] if hit else None
    if not cmd:
        return f"no ProSteel command matched: {cmd_or_key!r}"
    if acad is None:
        from acad import Acad
        acad = Acad()
    acad.send("_" + cmd + " ")
    return f"fired ProSteel command: {cmd}"


def dispatch(text, acad=None):
    """Natural request -> fire the right ProSteel tool. The friendly entry point."""
    hit = command_for(text)
    if not hit:
        return f"(no matching ProSteel tool for: {text!r}) - see CATALOG"
    if acad is None:
        from acad import Acad
        acad = Acad()
    acad.send("_" + hit[0] + " ")
    return f"opened: {hit[0]} - {hit[1]}"


if __name__ == "__main__":
    import sys
    if len(sys.argv) > 1:
        q = " ".join(sys.argv[1:])
        hit = command_for(q)
        print(f"'{q}' -> {hit}")
    else:
        print(f"{len(CATALOG)} ProSteel commands catalogued. Examples:")
        for k in ("shape", "bolt", "plate", "frame", "position", "detail", "nc"):
            print(f"  {k:12} -> {CATALOG[k][0]:22} {CATALOG[k][1]}")
