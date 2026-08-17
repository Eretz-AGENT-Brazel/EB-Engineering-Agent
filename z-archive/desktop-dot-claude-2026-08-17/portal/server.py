# -*- coding: utf-8 -*-
"""EB AI - Mission Control: local portal server.
Serves the static app and a small JSON API backed by data/*.json.
Local-only (binds 127.0.0.1). No database. Amir edits via the UI; Claude
reads/writes the same JSON files directly.
"""
import http.server, json, os, threading, uuid, datetime, urllib.parse

BASE = os.path.dirname(os.path.abspath(__file__))      # .../portal
APP  = os.path.join(BASE, "app")
DATA = os.path.join(BASE, "data")
ROOT = os.path.abspath(os.path.join(BASE, ".."))       # .../.claude  (for raw markdown)
PORT = 8190
LOCK = threading.Lock()

COLLECTIONS = {"program", "roadmap", "sessions", "decisions",
               "suggestions", "tasks", "questions", "projects"}
ARRAYS = COLLECTIONS - {"program"}   # program is a single object, read-only via API


def data_path(c):
    return os.path.join(DATA, c + ".json")


def load(c):
    p = data_path(c)
    if not os.path.exists(p):
        return [] if c in ARRAYS else {}
    with open(p, "r", encoding="utf-8") as f:
        return json.load(f)


def save(c, obj):
    with open(data_path(c), "w", encoding="utf-8") as f:
        json.dump(obj, f, ensure_ascii=False, indent=2)


class Handler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *a, **k):
        super().__init__(*a, directory=APP, **k)

    # ---- helpers ----
    def _json(self, obj, code=200):
        body = json.dumps(obj, ensure_ascii=False).encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(body)

    def _body(self):
        n = int(self.headers.get("Content-Length") or 0)
        if not n:
            return {}
        try:
            return json.loads(self.rfile.read(n).decode("utf-8"))
        except Exception:
            return {}

    def _route(self):
        u = urllib.parse.urlparse(self.path)
        parts = u.path.strip("/").split("/")
        return u, parts

    # ---- GET ----
    def do_GET(self):
        u, parts = self._route()
        if u.path == "/api":
            return self._json({c: load(c) for c in COLLECTIONS})
        if len(parts) == 2 and parts[0] == "api" and parts[1] in COLLECTIONS:
            return self._json(load(parts[1]))
        if u.path == "/raw":
            qs = urllib.parse.parse_qs(u.query)
            rel = (qs.get("path") or [""])[0]
            full = os.path.abspath(os.path.join(ROOT, rel))
            if not full.startswith(ROOT) or not os.path.isfile(full):
                self.send_response(404); self.end_headers(); return
            with open(full, "r", encoding="utf-8", errors="replace") as f:
                body = f.read().encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "text/plain; charset=utf-8")
            self.send_header("Content-Length", str(len(body)))
            self.send_header("Cache-Control", "no-store")
            self.end_headers(); self.wfile.write(body); return
        return super().do_GET()

    # ---- POST (add) ----
    def do_POST(self):
        u, parts = self._route()
        if len(parts) == 2 and parts[0] == "api" and parts[1] in ARRAYS:
            c = parts[1]
            item = self._body()
            with LOCK:
                arr = load(c)
                item.setdefault("id", uuid.uuid4().hex[:8])
                item.setdefault("date", datetime.date.today().isoformat())
                arr.append(item)
                save(c, arr)
            return self._json(item, 201)
        self.send_response(404); self.end_headers()

    # ---- PATCH (update) ----
    def do_PATCH(self):
        u, parts = self._route()
        if len(parts) == 3 and parts[0] == "api" and parts[1] in ARRAYS:
            c, _id = parts[1], parts[2]
            patch = self._body()
            with LOCK:
                arr = load(c); found = None
                for it in arr:
                    if str(it.get("id")) == _id:
                        it.update(patch); found = it; break
                if found is None:
                    return self._json({"error": "not found"}, 404)
                save(c, arr)
            return self._json(found)
        self.send_response(404); self.end_headers()

    # ---- DELETE ----
    def do_DELETE(self):
        u, parts = self._route()
        if len(parts) == 3 and parts[0] == "api" and parts[1] in ARRAYS:
            c, _id = parts[1], parts[2]
            with LOCK:
                arr = [it for it in load(c) if str(it.get("id")) != _id]
                save(c, arr)
            return self._json({"deleted": _id})
        self.send_response(404); self.end_headers()

    def log_message(self, *a):
        pass


if __name__ == "__main__":
    os.makedirs(DATA, exist_ok=True)
    httpd = http.server.ThreadingHTTPServer(("127.0.0.1", PORT), Handler)
    print(f"EB Mission Control running at http://localhost:{PORT}  (Ctrl+C to stop)")
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        print("\nstopped.")
