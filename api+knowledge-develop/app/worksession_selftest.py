"""worksession_selftest.py - prove the work-session gate REFUSES. No AutoCAD needed.

A guard that has never been seen to fire has never been tested. Same rule as
qc/selftest_consistency.py: every refusal below is triggered on purpose, and a refusal
that does not appear is a failed test -- not a quiet pass.

Run:  python app/worksession_selftest.py
"""

import os
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import worksession as ws          # noqa: E402
import eb_api                     # noqa: E402

A = r"C:\models\alpha\test.dwg"          # same basename, different folder, on purpose
B = r"C:\models\beta\test.dwg"
C = r"C:\models\gamma\pump.dwg"

PASS, FAIL = [], []


def check(name, cond, detail=""):
    (PASS if cond else FAIL).append(name)
    print("%s %s%s" % ("  ok  " if cond else "  FAIL", name,
                       ("   <- " + detail) if detail and not cond else ""))


def refusal(op="beam", **kw):
    return eb_api._session_gate(op, kw)


def main():
    # keep the real registry out of harm's way
    backup = None
    if os.path.exists(ws.STATE):
        backup = open(ws.STATE, encoding="utf-8").read()
    os.environ["EB_AGENT_SESSION"] = "selftest-%d" % int(time.time())
    os.environ.pop("EB_MODEL", None)
    try:
        ws.save(ws._blank())

        # --- 0. the slot function is deterministic and separates identical basenames ----
        check("slot is stable", ws.slot_of(A) == ws.slot_of(A))
        check("same basename, different folder -> DIFFERENT mailbox",
              ws.slot_of(A) != ws.slot_of(B))
        check("different name -> different slot", ws.slot_of(A) != ws.slot_of(C))
        check("slot is filesystem-safe",
              all(ch.isalnum() or ch == "_" for ch in ws.slot_of("שוחה 7- DIKE.dwg")))

        # --- 1. no session, nobody else: legacy behaviour, allowed ---------------------
        check("open field: unpinned op allowed when no one holds anything",
              refusal() is None)

        # --- 2. R3 hands-off: Amir holds a model ---------------------------------------
        ws.claim(C, task="עובד על זה עכשיו")
        r = refusal(dwg=os.path.basename(C))
        check("R3 refuses an op aimed at Amir's model", bool(r) and "hands-off" in r, repr(r))

        r = refusal()
        check("R5 refuses an UNPINNED op while Amir holds a model",
              bool(r) and "no work session" in r, repr(r))

        # --- 3. the agent enters its own model ------------------------------------------
        eb_api.use(A, task="lesson 7 rebuild")
        check("agent session is current", (ws.current() or {}).get("dwg") == os.path.abspath(A))
        check("a bare basename resolves to the registered session",
              (ws.find(os.path.basename(A)) or {}).get("dwg") == os.path.abspath(A))
        check("own model is allowed", refusal() is None)
        check("Amir's model is STILL refused from inside our session",
              "hands-off" in (refusal(dwg=os.path.basename(C)) or ""))

        # --- 4. R2 conflict: explicit dwg= may not overrule the session ------------------
        r = refusal(dwg="something-else.dwg")
        check("R2 refuses an explicit dwg= that disagrees with the session",
              bool(r) and "pin conflict" in r, repr(r))
        check("matching dwg= passes", refusal(dwg=os.path.basename(A)) is None)

        # --- 5. R4 consent expires ------------------------------------------------------
        st = ws.load()
        key = ws.slot_of(A)
        st["sessions"][key]["touched"] = time.time() - (ws.STALE_HOURS + 1) * 3600
        ws.save(st)
        r = refusal()
        check("R4 refuses a stale session", bool(r) and "stale session" in r, repr(r))
        ws.confirm(A)
        check("confirm() clears staleness", refusal() is None)

        st = ws.load()
        st["sessions"][key]["agent_session"] = "yesterday"
        ws.save(st)
        r = refusal()
        check("R4 refuses a session opened by another agent run",
              bool(r) and "stale session" in r, repr(r))
        ws.confirm(A)

        # --- 6. diagnostics are never gated ---------------------------------------------
        ws.claim(C)
        check("ping is never gated", refusal(op="ping") is None)
        check("whoami is never gated", refusal(op="whoami") is None)

        # --- 7. the mailbox is per model -------------------------------------------------
        ch_a, ch_c = eb_api.channel(A), eb_api.channel(C)
        check("channels differ per model", ch_a != ch_c)
        check("channel dir is created", os.path.isdir(ch_a))
        check("cmd file lives in the channel",
              os.path.dirname(eb_api._cmd_path()) == ws.channel_dir(A))

        # --- 8. taking over someone else's model requires force --------------------------
        try:
            ws.open_session(C, owner=ws.OWNER_AGENT)
            check("open refuses to take over a held model", False, "no exception raised")
        except RuntimeError:
            check("open refuses to take over a held model", True)

        # --- 9. release gives it back -----------------------------------------------------
        ws.release(C)
        check("after release the model is free again",
              refusal(dwg=os.path.basename(C)) is not None)   # still a pin conflict, not hands-off
        check("...and the refusal is now the conflict, not hands-off",
              "pin conflict" in (refusal(dwg=os.path.basename(C)) or ""))
    finally:
        if backup is not None:
            open(ws.STATE, "w", encoding="utf-8").write(backup)
        elif os.path.exists(ws.STATE):
            os.remove(ws.STATE)

    print("\n%d passed, %d failed" % (len(PASS), len(FAIL)))
    if FAIL:
        print("FAILED: " + ", ".join(FAIL))
        return 1
    print("SELFTEST CLEAN - every refusal fired on purpose")
    return 0


if __name__ == "__main__":
    sys.exit(main())
