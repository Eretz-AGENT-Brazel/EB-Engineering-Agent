# -*- coding: utf-8 -*-
"""
eb_shot -- capture the AutoCAD window to a PNG.

Why this exists
---------------
Amir watches the AutoCAD window while the agent models. On 06/08 a chamfer
succeeded -- the facet was created, read back, and measured -- and he still
could not see it, because the view was pointing somewhere else. An operation
that succeeds outside the current view is indistinguishable from one that did
nothing. Reading the model back proves it to *me*; a screenshot proves it to
*him*, and those are two different obligations.

Pairs with the plugin's v47 view ops:
    eb.run("view", dir="iso")
    eb.run("zoom", handle=h)
    eb.run("hilite", handle=h)
    eb_shot.shot("...png")

The capture is a screen grab of the window rectangle, so the window must be
visible and unobscured; that is why it is raised first.
"""

import os
import subprocess
import tempfile

_PS = r"""
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing | Out-Null
Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
public class EbWin {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern int PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb, IntPtr l);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  public delegate bool EnumWindowsProc(IntPtr h, IntPtr l);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }

  // Process.MainWindowHandle is NOT reliable here. It returns the first top-level window
  // of the process, and AutoCAD 2015 owns WPF "HwndWrapper[...]" windows with EMPTY titles
  // that can win that race -- which is exactly what happened on 06/08/2026: the capture
  // failed with "no window titled like 'sandbox.dwg'" while AutoCAD was alive, responding,
  // and answering pings. Enumerate instead, and keep only real titled visible windows.
  public static List<KeyValuePair<IntPtr,string>> Titled(string procName) {
    var procs = new HashSet<uint>();
    foreach (var p in System.Diagnostics.Process.GetProcessesByName(procName)) {
      try { procs.Add((uint)p.Id); } catch {} }
    var found = new List<KeyValuePair<IntPtr,string>>();
    EnumWindows((h, l) => {
      uint pid; GetWindowThreadProcessId(h, out pid);
      if (!procs.Contains(pid) || !IsWindowVisible(h)) return true;
      var sb = new StringBuilder(512); GetWindowText(h, sb, 512);
      string t = sb.ToString();
      if (t.Length > 0) found.Add(new KeyValuePair<IntPtr,string>(h, t));
      return true; }, IntPtr.Zero);
    return found;
  }
}
"@ | Out-Null

$all = [EbWin]::Titled('__PROC__')
if ($all.Count -eq 0) { Write-Output 'EB_ERR shot: no visible titled __PROC__ window found'; exit 1 }

$want = '__MATCH__'
if ($want -ne '') {
  $hit = @($all | Where-Object { $_.Value -like ('*' + $want + '*') })
  if ($hit.Count -eq 0) {
    $titles = ($all | ForEach-Object { $_.Value }) -join ' | '
    Write-Output ("EB_ERR shot: no window titled like '" + $want + "'. open: " + $titles); exit 1
  }
  if ($hit.Count -gt 1) {
    $titles = ($hit | ForEach-Object { $_.Value }) -join ' | '
    Write-Output ("EB_ERR shot: '" + $want + "' matches " + $hit.Count + " windows: " + $titles); exit 1
  }
  $win = $hit[0]
} else {
  if ($all.Count -gt 1) {
    $titles = ($all | ForEach-Object { $_.Value }) -join ' | '
    Write-Output ("EB_ERR shot: " + $all.Count + " __PROC__ windows are open and no match= was given: " + $titles); exit 1
  }
  $win = $all[0]
}
$h = $win.Key
$title = $win.Value

if ([EbWin]::IsIconic($h)) { [EbWin]::ShowWindow($h, 9) | Out-Null; Start-Sleep -Milliseconds 300 }

$r = New-Object EbWin+RECT
[EbWin]::GetWindowRect($h, [ref]$r) | Out-Null
$w  = $r.Right - $r.Left
$ht = $r.Bottom - $r.Top
if ($w -le 0 -or $ht -le 0) { Write-Output 'EB_ERR shot: window has no size'; exit 1 }

# PrintWindow asks the window to draw ITSELF into our bitmap. It needs no focus, so it
# cannot photograph whatever happens to be on top -- and it does not interrupt the user.
# CopyFromScreen was the first implementation and it lied twice on 06/08/2026: Amir was
# working in his own AutoCAD, SetForegroundWindow silently failed (Windows refuses a
# focus steal from a background process), and the grab returned HIS window's pixels
# under MY window's title. A picture of the wrong window is worse than no picture.
$bmp = New-Object System.Drawing.Bitmap $w, $ht
$g   = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
$pw  = [EbWin]::PrintWindow($h, $hdc, 2)     # 2 = PW_RENDERFULLCONTENT
$g.ReleaseHdc($hdc)
$method = 'printwindow'

if ($pw -eq 0) {
  # fall back to a screen grab -- but ONLY if the target really is in front, and say so
  if (__RAISE__) {
    [EbWin]::SetForegroundWindow($h) | Out-Null
    Start-Sleep -Milliseconds __SETTLE__
  }
  $fg = [EbWin]::GetForegroundWindow()
  if ($fg -ne $h) {
    $g.Dispose(); $bmp.Dispose()
    Write-Output ("EB_ERR shot: PrintWindow failed and '" + $title +
                  "' is not the foreground window -- refusing to return a screen grab " +
                  "that would show whatever is on top of it")
    exit 1
  }
  $g.CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
  $method = 'screengrab'
}
$g.Dispose()
$bmp.Save('__OUT__', [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Output ("EB_OK shot " + $w + "x" + $ht + " via " + $method + " of [" + $title + "] -> __OUT__")
"""


def shot(out=None, proc="acad", raise_window=True, settle_ms=450, match=None):
    """Capture the window of `proc` (default AutoCAD) to a PNG. Returns the path.

    raise_window brings it to the front first -- required, because the capture
    is a screen grab and anything covering the window lands in the image.
    """
    if out is None:
        out = os.path.join(tempfile.gettempdir(), "eb_shot.png")
    out = os.path.abspath(out)
    d = os.path.dirname(out)
    if d and not os.path.isdir(d):
        os.makedirs(d)
    if os.path.exists(out):
        os.remove(out)

    # Which window? "the first acad process" is not an answer when two are open.
    # 06/08/2026: that default captured Amir's own Drawing1.dwg and the picture was
    # read as evidence the model had not changed -- when in fact the right window was
    # simply not the one photographed. Default to the pinned drawing.
    if match is None:
        try:
            import eb_api
            match = eb_api.EXPECT_DWG or ""
        except Exception:
            match = ""

    script = (_PS
              .replace("__PROC__", proc)
              .replace("__RAISE__", "$true" if raise_window else "$false")
              .replace("__SETTLE__", str(int(settle_ms)))
              .replace("__MATCH__", (match or '').replace("'", "''"))
              .replace("__OUT__", out.replace("'", "''")))

    p = subprocess.run(["powershell", "-NoProfile", "-NonInteractive", "-Command", script],
                       capture_output=True, text=True, timeout=90)
    msg = (p.stdout or "").strip() or (p.stderr or "").strip()
    if not os.path.exists(out):
        raise RuntimeError("screenshot failed: " + msg[:400])
    return out, msg


if __name__ == "__main__":
    import sys
    path, msg = shot(sys.argv[1] if len(sys.argv) > 1 else None)
    print(msg)
    print(path)
