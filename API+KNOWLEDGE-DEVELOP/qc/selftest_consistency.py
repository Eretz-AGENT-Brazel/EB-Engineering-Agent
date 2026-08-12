# -*- coding: utf-8 -*-
"""Prove the guard still works.  `python qc/selftest_consistency.py`

A guard that has never failed has never been tested. This reproduces the ACTUAL failure of
10/08/2026 — the retracted sentence *"Clone (B.4.5) is dialog-only"* put back into the skill in a
section with no retraction marker — and asserts three things:

    1. the repo is clean before                 (or the test tells you nothing)
    2. the guard FAILS with the stale claim present, and NAMES the phrase
    3. the repo is clean again after the poison is removed

⚠️ It edits the live skill and puts it back **byte for byte**. The first version of this test
restored through text mode, changed CRLF to LF, and check 4 caught it — accidental proof that the
backup check works, and the reason the restore is done in binary here.

Run it after any change to qc/consistency.py, and whenever the guard has been silent for a while.
"""
import io
import os
import shutil
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))   # qc sits inside API+KNOWLEDGE-DEVELOP since 12/08/2026
HOME = os.path.expanduser("~")

LIVE = os.path.join(HOME, ".claude", "skills", "prosteel-modeling", "references", "plugin-ops.md")
COPY = os.path.join(REPO, "agent-brain", "skill-prosteel-modeling", "references", "plugin-ops.md")
GUARD = os.path.join(HERE, "consistency.py")

PHRASE = "Clone (B.4.5) is dialog-only"
POISON = ("\r\n## Self-test scratch section\r\n\r\n"
          + PHRASE + " and there is nothing more to say.\r\n").encode("utf-8")


def run_guard():
    p = subprocess.run([sys.executable, GUARD], cwd=REPO, capture_output=True,
                       text=True, encoding="utf-8", errors="replace")
    out = p.stdout or ""
    line = next((l.strip() for l in out.split("\n") if "FAILURE" in l or "CLEAN" in l), "?")
    return p.returncode, line, (PHRASE in out)


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass

    if not os.path.exists(LIVE):
        print("live skill file missing: %s" % LIVE)
        return 2
    if os.path.exists(COPY):
        shutil.copy2(COPY, LIVE)          # start from the known-good bytes

    rc0, msg0, _ = run_guard()
    print("  baseline              exit=%d  %s" % (rc0, msg0))
    if rc0 != 0:
        print()
        print("  The repo is not clean to begin with, so this test proves nothing.")
        print("  Fix what qc/consistency.py reports, then run the self-test again.")
        return 2

    blob = io.open(LIVE, "rb").read()
    try:
        io.open(LIVE, "wb").write(blob + POISON)
        rc1, msg1, named = run_guard()
        print("  with the stale claim  exit=%d  %s   (named the phrase: %s)" % (rc1, msg1, named))
    finally:
        io.open(LIVE, "wb").write(blob)    # byte-exact restore

    rc2, msg2, _ = run_guard()
    print("  after removing it     exit=%d  %s" % (rc2, msg2))

    print()
    ok = (rc0 == 0 and rc1 != 0 and named and rc2 == 0)
    if ok:
        print("  SELF-TEST PASSED — the guard catches the failure of 10/08/2026, and only that.")
        return 0
    print("  SELF-TEST FAILED — do not trust the guard until this passes.")
    return 1


if __name__ == "__main__":
    sys.exit(main())
