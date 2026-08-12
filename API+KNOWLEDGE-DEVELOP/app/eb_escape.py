# -*- coding: utf-8 -*-
"""Cancel a pending command line prompt in AutoCAD, without touching the model.

A prompt left hanging (`Specify corner of window ... <real time>: _a`) is not a modal
dialog -- `modal_dialogs()` sees nothing, COM refuses every call, and the session looks
dead. ESC clears it and cannot change geometry: an unfinished command has committed
nothing.

Targets the AutoCAD window BY TITLE, the same discipline eb_shot.py uses, so the keys
can never land in Amir's own window.
"""
import ctypes
import ctypes.wintypes as wt
import time

user32 = ctypes.windll.user32
WM_KEYDOWN, WM_KEYUP, VK_ESCAPE = 0x0100, 0x0101, 0x1B


def _windows():
    found = []
    CB = ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)

    def cb(hwnd, _l):
        if not user32.IsWindowVisible(hwnd):
            return True
        n = user32.GetWindowTextLengthW(hwnd)
        if n:
            buf = ctypes.create_unicode_buffer(n + 1)
            user32.GetWindowTextW(hwnd, buf, n + 1)
            found.append((hwnd, buf.value))
        return True

    user32.EnumWindows(CB(cb), 0)
    return found


def escape(dwg=None, times=3, verbose=True):
    """Send ESC to the AutoCAD window holding `dwg` (or any AutoCAD window).

    Returns (hwnd, title) of the window keyed, or (None, reason).
    """
    hits = [(h, t) for (h, t) in _windows() if "AutoCAD" in t]
    if dwg:
        want = dwg.lower()
        narrowed = [(h, t) for (h, t) in hits if want in t.lower()]
        if not narrowed:
            return (None, "no AutoCAD window titled %r; saw %s" % (dwg, [t for _h, t in hits]))
        hits = narrowed
    if not hits:
        return (None, "no AutoCAD window found")
    if len(hits) > 1:
        return (None, "several AutoCAD windows match -- refusing to guess: %s"
                % [t for _h, t in hits])

    hwnd, title = hits[0]
    for _ in range(times):
        user32.PostMessageW(hwnd, WM_KEYDOWN, VK_ESCAPE, 0)
        user32.PostMessageW(hwnd, WM_KEYUP, VK_ESCAPE, 0)
        time.sleep(0.4)
    if verbose:
        print("ESC x%d -> %s" % (times, title))
    return (hwnd, title)


if __name__ == "__main__":
    import sys
    print(escape(sys.argv[1] if len(sys.argv) > 1 else None))
