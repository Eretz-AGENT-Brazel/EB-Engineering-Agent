"""
context.py - per-project session memory for the NLU interpreter.

Remembers the last profile/handle/points, a cursor point, and defaults, so free
speech like "the last beam", "same again", "continue from its end", "3 m apart"
resolves without re-specifying everything. Stored as projects/<id>/context.json.
"""
import os
import json

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROJECTS = os.path.join(ROOT, "projects")
ACTIVE = os.path.join(ROOT, "data", "project.txt")

DEFAULT = {
    "last_profile": "HEB 200",
    "cursor": [0, 0, 0],
    "objects": [],          # [{kind, profile, p1, p2, handle}]
    "defaults": {"length": 6000, "height": 3000, "spacing": 3000},
}


def _active():
    return open(ACTIVE, encoding="utf-8").read().strip() if os.path.exists(ACTIVE) else ""


def _path():
    a = _active()
    return os.path.join(PROJECTS, a, "context.json") if a else None


def load():
    p = _path()
    ctx = dict(DEFAULT)
    ctx["defaults"] = dict(DEFAULT["defaults"])
    ctx["objects"] = []
    if p and os.path.exists(p):
        try:
            saved = json.load(open(p, encoding="utf-8"))
            ctx.update(saved)
        except Exception:
            pass
    return ctx


def save(ctx):
    p = _path()
    if not p:
        return
    try:
        os.makedirs(os.path.dirname(p), exist_ok=True)
        json.dump(ctx, open(p, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    except Exception:
        pass


def add_object(ctx, kind, profile, p1, p2, handle):
    ctx["objects"].append({"kind": kind, "profile": profile,
                           "p1": list(p1), "p2": list(p2), "handle": handle})
    ctx["last_profile"] = profile
    ctx["cursor"] = list(p2)
    save(ctx)


def last(ctx, kind=None):
    for o in reversed(ctx["objects"]):
        if kind is None or o["kind"] == kind:
            return o
    return None
