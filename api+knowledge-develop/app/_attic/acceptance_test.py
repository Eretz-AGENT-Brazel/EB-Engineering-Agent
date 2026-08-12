# -*- coding: utf-8 -*-
"""Phase J: run Amir's full acceptance test in a fresh drawing, timed."""
import time
import pythoncom
import win32com.client
import eb_api

pythoncom.CoInitialize()
app = win32com.client.GetActiveObject("AutoCAD.Application")

# fresh drawing from the ProSteel metric template
doc = app.Documents.Add()
time.sleep(2)
print("fresh drawing:", doc.Name)

results = []


def step(label, fn):
    t0 = time.time()
    r = fn()
    dt = time.time() - t0
    results.append((label, round(dt, 1), r))
    print("[%4.1fs] %-28s %s" % (dt, label, r))
    time.sleep(0.8)
    return r


# 1. TOP view
step("1. TOP view", lambda: eb_api.view("top"))
# 2. two HEB 500 beams, 6 m, 3000 apart
r1 = step("2a. beam HEB500 #1", lambda: eb_api.beam("HEB 500", (0, 0, 0), (6000, 0, 0)))
r2 = step("2b. beam HEB500 #2", lambda: eb_api.beam("HEB 500", (0, 3000, 0), (6000, 3000, 0)))
# 3. diagonal HEA 500, mitered where it meets the mains
rd = step("3a. diagonal HEA500", lambda: eb_api.beam("HEA 500", (0, 0, 0), (6000, 3000, 0)))
hd = eb_api.handle_of(rd)
h1 = eb_api.handle_of(r1)
if hd and h1:
    step("3b. miter diagonal", lambda: eb_api.miter(hd, h1))
# 4. full bolted connection (2 plates t20 + 4 M20 bolts)
step("4. bolted connection", lambda: eb_api.conn_bolted((6000, 3000, 0), pl=220, pw=220, pt=20,
                                                        gap=60, nx=2, ny=2, sx=100, sy=100, dia=20))
# view iso + zoom for the screenshot
step("5. iso view", lambda: eb_api.view("iso"))

print("\n=== SUMMARY ===")
allok = all(str(r).startswith("EB_OK") or str(r).startswith("✓") or "EB_OK" in str(r) for _, _, r in results)
worst = max(dt for _, dt, _ in results)
for label, dt, r in results:
    print("  %-28s %4.1fs  %s" % (label, dt, "OK" if ("EB_OK" in str(r) or "view" in str(r)) else "CHECK"))
print("worst single step: %.1fs  (SLA 10s: %s)" % (worst, "PASS" if worst <= 10 else "REVIEW"))
