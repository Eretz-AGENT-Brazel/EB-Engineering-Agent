# -*- coding: utf-8 -*-
"""Refresh the repo's backup of the agent's skill and memory.

Both live under ~/.claude/, outside the development folder, so nothing in git ever
saw them. Until 09/08/2026 that meant the whole accumulated knowledge base existed in
exactly one copy, on one machine.

Run this as step 4 of each chapter, next to embedding in the skill, before committing.
It reports what changed so the commit message can say so.
"""
import filecmp
import os
import shutil
import sys

HOME = os.path.expanduser("~")
HERE = os.path.dirname(os.path.abspath(__file__))

PAIRS = [
    (os.path.join(HOME, ".claude", "skills", "prosteel-modeling"),
     os.path.join(HERE, "skill-prosteel-modeling"),
     "skill"),
    (os.path.join(HOME, ".claude", "projects", "C--Users-User-Desktop", "memory"),
     os.path.join(HERE, "memory"),
     "memory"),
]


def sync(src, dst, label):
    if not os.path.isdir(src):
        print("  !! source missing: %s" % src)
        return 0, 0, 0
    added = changed = same = 0
    for root, _dirs, files in os.walk(src):
        rel = os.path.relpath(root, src)
        target_dir = dst if rel == "." else os.path.join(dst, rel)
        os.makedirs(target_dir, exist_ok=True)
        for f in files:
            s = os.path.join(root, f)
            d = os.path.join(target_dir, f)
            if not os.path.exists(d):
                shutil.copy2(s, d)
                print("  + %s/%s" % (label, f))
                added += 1
            elif not filecmp.cmp(s, d, shallow=False):
                shutil.copy2(s, d)
                print("  ~ %s/%s" % (label, f))
                changed += 1
            else:
                same += 1
    # anything here that is no longer in the source is stale -- say so, do not delete
    for root, _dirs, files in os.walk(dst):
        rel = os.path.relpath(root, dst)
        src_dir = src if rel == "." else os.path.join(src, rel)
        for f in files:
            if f == "README.md" and rel == ".":
                continue
            if not os.path.exists(os.path.join(src_dir, f)):
                print("  ? %s/%s — in the backup but NOT in the live copy (deleted upstream?)"
                      % (label, f))
    return added, changed, same


def main():
    total = [0, 0, 0]
    for src, dst, label in PAIRS:
        print("%s  <-  %s" % (label, src))
        a, c, s = sync(src, dst, label)
        total[0] += a; total[1] += c; total[2] += s
    print()
    print("added %d, updated %d, unchanged %d" % tuple(total))
    if total[0] or total[1]:
        print("=> commit agent-brain/ with this chapter's push.")
    else:
        print("=> backup already current.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
