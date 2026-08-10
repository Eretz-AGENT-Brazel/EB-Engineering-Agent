# -*- coding: utf-8 -*-
"""THE GUARD — run this before every commit.  `python qc/consistency.py`

WHY IT EXISTS
On 10/08/2026 a fourteen-chapter audit retracted four conclusions, and three of them stayed alive
in the skill the agent actually loads. One of them ended with the sentence *"recorded so this is
never re-investigated from scratch"* — stale text actively instructing a future reader to stop
looking at something that had just been proved to work. Three commits touched no skill file at
all, and `sync.py` reported "backup already current" the whole time, which was TRUE and useless:
it compares the backup to the live skill, and says nothing about whether the live skill was ever
written to.

The failure was not untidiness. It was that the same fact lives in several places and **nothing
kept them agreeing**. This script is that something. It is deliberately cheap to run and rude
when it fails.

WHAT IT CHECKS
  1. RETRACTIONS   no file states a claim that has been withdrawn, unless it is quoted inside a
                   retraction. Data: qc/retracted.tsv
  2. MEMORY INDEX  every memory file appears in MEMORY.md, and every MEMORY.md link resolves
  3. WIKILINKS     every [[link]] inside a memory resolves to a memory that exists
  4. SKILL BACKUP  the repo's copy of the skill matches the live skill, both directions
  5. PLUGIN        the version app/eb_api.py loads exists, and is the newest source present
  6. CHAPTER SYNC  every chapter the audit record covers carries an audit marker in its own note

EXIT CODE 0 = clean.  Anything else = do not commit yet.
"""
import io
import os
import re
import sys
import filecmp

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
HOME = os.path.expanduser("~")

SKILL_LIVE = os.path.join(HOME, ".claude", "skills", "prosteel-modeling")
SKILL_COPY = os.path.join(REPO, "agent-brain", "skill-prosteel-modeling")
MEM_LIVE = os.path.join(HOME, ".claude", "projects", "C--Users-User-Desktop", "memory")
KNOW = os.path.join(REPO, "knowledge")
RECORD_DIR = os.path.join(KNOW, "learning", "audits")
NOTES_DIR = os.path.join(KNOW, "learning", "manual")
RETRACTED = os.path.join(HERE, "retracted.tsv")

# A quoted claim inside a retraction is correct and must not trip the check. These markers open a
# window in which the withdrawn wording is expected to appear.
RETRACTION_MARKERS = ("retracted", "corrected", "withdrawn", "withdraw", "retract",
                      "closed 10/08", "closed 2026", "used to say", "used to read",
                      "used to be headed", "used to end", "this line used to",
                      "this block used to", "no longer true", "disproved", "disproven",
                      # Hebrew: match the STEM, not one inflection. The rule block in PROGRAM.md
                      # quotes a retracted phrase and used the forms הפריכה / מפריכים / הפרכה,
                      # none of which matched the single form הופרך -- so the guard flagged the
                      # very document that explains the retractions.
                      "הופרך", "הפרכ", "הפריכ", "מופרכ", "נסגר", "תוקן", "בטל")

FAILS = []
WARNS = []


def fail(check, msg):
    FAILS.append((check, msg))


def warn(check, msg):
    WARNS.append((check, msg))


def read(p):
    try:
        return io.open(p, encoding="utf-8", errors="replace").read()
    except Exception:
        return ""


def md_files(root, skip_dirs=()):
    for dirpath, dirs, files in os.walk(root):
        dirs[:] = [d for d in dirs if d not in skip_dirs and not d.startswith(".")]
        for f in files:
            if f.lower().endswith((".md", ".txt")):
                yield os.path.join(dirpath, f)


# ----------------------------------------------------------------- 1. retractions
def check_retractions():
    """A withdrawn claim must not stand as a live assertion anywhere."""
    if not os.path.exists(RETRACTED):
        warn("retractions", "qc/retracted.tsv does not exist -- nothing to check against")
        return
    entries = []
    for line in io.open(RETRACTED, encoding="utf-8"):
        line = line.rstrip("\n")
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        parts = line.split("\t")
        if len(parts) < 3:
            warn("retractions", "malformed row in retracted.tsv: %r" % line[:60])
            continue
        if parts[0].strip().lower() == "date" and parts[1].strip().lower() == "phrase":
            continue                                  # the column header, not an entry
        entries.append({"date": parts[0].strip(),
                        "phrase": parts[1].strip(),
                        "why": parts[2].strip()})
    if not entries:
        warn("retractions", "retracted.tsv has no entries")
        return

    roots = [(KNOW, ()), (SKILL_LIVE, ()), (MEM_LIVE, ()), (REPO, ("knowledge", "agent-brain",
                                                                  "app", "projects", "qc",
                                                                  "assets", "data", "standards"))]
    scanned = 0
    for root, skip in roots:
        if not os.path.isdir(root):
            continue
        for path in md_files(root, skip_dirs=skip + ("__pycache__", "_attic", "_archive")):
            base = os.path.basename(path)
            # The audit record IS the register of retractions and qc/retracted.tsv is the list
            # itself: both must be free to quote what they withdraw. Everything else must agree
            # with them.
            if base.startswith("AUDIT-PART-") or base == "retracted.tsv":
                continue
            text = read(path)
            if not text:
                continue
            scanned += 1
            lines = text.split("\n")

            # Protection is SECTION-scoped, not distance-scoped. Quoting a withdrawn claim inside
            # the retraction that withdraws it is correct; asserting it in some other section --
            # which is exactly what happened on 10/08, the retraction 1000 lines from the stale
            # text -- is not. So: a phrase is allowed only if the markdown section it sits in
            # also contains a retraction marker.
            sec_of = []                      # section index per line
            sec_has_marker = []
            cur = 0
            sec_has_marker.append(False)
            for l in lines:
                if l.startswith("#"):
                    cur += 1
                    sec_has_marker.append(False)
                sec_of.append(cur)
                low = l.lower()
                if any(m in low for m in RETRACTION_MARKERS):
                    sec_has_marker[cur] = True

            for e in entries:
                for i, l in enumerate(lines):
                    if e["phrase"] not in l:
                        continue
                    if sec_has_marker[sec_of[i]]:
                        continue
                    rel = os.path.relpath(path, HOME)
                    fail("retractions",
                         "%s:%d states a claim retracted on %s\n"
                         "        phrase : %s\n"
                         "        why    : %s\n"
                         "        line   : %s"
                         % (rel, i + 1, e["date"], e["phrase"], e["why"], l.strip()[:110]))
    print("  scanned %d documents against %d retracted phrases" % (scanned, len(entries)))


# ----------------------------------------------------------------- 2 + 3. memory
def check_memory():
    if not os.path.isdir(MEM_LIVE):
        fail("memory", "memory directory missing: %s" % MEM_LIVE)
        return
    index_path = os.path.join(MEM_LIVE, "MEMORY.md")
    index = read(index_path)
    if not index:
        fail("memory", "MEMORY.md missing or empty")
        return

    on_disk = sorted(f for f in os.listdir(MEM_LIVE)
                     if f.lower().endswith(".md") and f != "MEMORY.md")
    linked = set(re.findall(r"\(([^)]+\.md)\)", index))

    for f in on_disk:
        if f not in linked:
            fail("memory", "%s exists but is NOT indexed in MEMORY.md" % f)
    for f in sorted(linked):
        if not os.path.exists(os.path.join(MEM_LIVE, f)):
            fail("memory", "MEMORY.md links to %s which does not exist" % f)

    # wikilinks between memories
    names = set()
    for f in on_disk:
        m = re.search(r"^name:\s*(\S+)", read(os.path.join(MEM_LIVE, f)), re.M)
        names.add(m.group(1).strip() if m else os.path.splitext(f)[0])
    dangling = {}
    for f in on_disk:
        for link in re.findall(r"\[\[([^\]]+)\]\]", read(os.path.join(MEM_LIVE, f))):
            if link not in names:
                dangling.setdefault(link, []).append(f)
    for link, where in sorted(dangling.items()):
        warn("memory", "[[%s]] does not resolve (in %s) -- a placeholder, or a typo?"
             % (link, ", ".join(where)))
    print("  %d memories, %d indexed, %d dangling wikilinks"
          % (len(on_disk), len(linked), len(dangling)))


# ----------------------------------------------------------------- 4. skill backup
def check_skill_backup():
    if not os.path.isdir(SKILL_LIVE):
        fail("skill", "live skill missing: %s" % SKILL_LIVE)
        return
    if not os.path.isdir(SKILL_COPY):
        fail("skill", "repo copy missing: %s -- run agent-brain/sync.py" % SKILL_COPY)
        return

    def rel_files(root):
        out = set()
        for dirpath, dirs, files in os.walk(root):
            dirs[:] = [d for d in dirs if not d.startswith(".")]
            for f in files:
                out.add(os.path.relpath(os.path.join(dirpath, f), root).replace("\\", "/"))
        return out

    live, copy = rel_files(SKILL_LIVE), rel_files(SKILL_COPY)
    for f in sorted(live - copy):
        fail("skill", "%s is in the live skill and NOT in the repo copy -- run sync.py" % f)
    for f in sorted(copy - live):
        if f == "README.md":
            continue
        fail("skill", "%s is in the repo copy and NOT live -- deleted upstream, remove it" % f)
    for f in sorted(live & copy):
        a, b = os.path.join(SKILL_LIVE, f), os.path.join(SKILL_COPY, f)
        if not filecmp.cmp(a, b, shallow=False):
            fail("skill", "%s differs between live and repo copy -- run sync.py" % f)
    print("  %d skill files compared" % len(live))


# ----------------------------------------------------------------- 5. plugin version
def check_plugin():
    api = os.path.join(REPO, "app", "eb_api.py")
    src = read(api)
    if not src:
        fail("plugin", "cannot read app/eb_api.py")
        return
    # Read the DECLARATIONS, not the first mention: the module docstring and several comments
    # name older EB_RUN* versions, and matching those made this check report a false mismatch.
    dll = re.search(r"^\s*DLL\w*\s*=.*EBAgentApi(\d+)\.dll", src, re.M) \
        or re.search(r"EBAgentApi(\d+)\.dll", src)
    cmd = re.search(r"^\s*RUN_CMD\s*=\s*[\"']EB_RUN(\d+)[\"']", src, re.M)
    if not dll or not cmd:
        fail("plugin", "eb_api.py names no plugin dll/command")
        return
    if dll.group(1) != cmd.group(1):
        fail("plugin", "eb_api.py loads EBAgentApi%s.dll but calls EB_RUN%s -- mismatch"
             % (dll.group(1), cmd.group(1)))
    v = int(dll.group(1))
    pdir = os.path.join(REPO, "app", "plugin")
    have_cs = sorted(int(m.group(1)) for m in
                     (re.match(r"EBAgentApi(\d+)\.cs$", f) for f in os.listdir(pdir)) if m)
    if v not in have_cs:
        fail("plugin", "eb_api.py loads v%d but app/plugin/EBAgentApi%d.cs does not exist"
             % (v, v))
    if have_cs and v < max(have_cs):
        warn("plugin", "eb_api.py loads v%d but v%d source exists -- is the newer one abandoned?"
             % (v, max(have_cs)))
    if not os.path.exists(os.path.join(pdir, "EBAgentApi%d.dll" % v)):
        warn("plugin", "EBAgentApi%d.dll is not built (gitignored, so this is only a local note)"
             % v)
    print("  eb_api.py -> v%d ; %d sources on disk, newest v%s"
          % (v, len(have_cs), max(have_cs) if have_cs else "?"))


# ----------------------------------------------------------------- 6. chapter sync
def check_chapters():
    """Every chapter the audit record covers must carry an audit marker in its own note."""
    if not os.path.isdir(RECORD_DIR):
        warn("chapters", "no audit directory")
        return
    records = [os.path.join(RECORD_DIR, f) for f in os.listdir(RECORD_DIR)
               if f.startswith("AUDIT-PART-") and f.endswith(".md")]
    if not records:
        warn("chapters", "no AUDIT-PART-*.md record found")
        return
    for rec in records:
        text = read(rec)
        date = re.search(r"(\d{4}-\d{2}-\d{2})", os.path.basename(rec))
        date = date.group(1) if date else "?"
        short = re.sub(r"^(\d{4})-(\d{2})-(\d{2})$", r"\3/\2/\1", date)
        covered = sorted(set(re.findall(r"^##\s+([A-Z]\.\d+)\s+", text, re.M)))
        if not covered:
            warn("chapters", "%s has no '## X.N' chapter sections" % os.path.basename(rec))
            continue
        missing = []
        for ch in covered:
            part, num = ch.split(".")
            pat = re.compile(r"MANUAL-NOTES-%s0?%s-" % (part, num))
            note = None
            pdir = os.path.join(NOTES_DIR, part)
            if os.path.isdir(pdir):
                for f in os.listdir(pdir):
                    if pat.match(f):
                        note = os.path.join(pdir, f)
                        break
            if note is None:
                missing.append("%s (no note file found)" % ch)
                continue
            body = read(note)
            dated = (short in body) or (date in body)
            marked = any(w in body.upper() for w in
                         ("AUDIT", "RETRACTED", "CORRECTED", "WITHDRAWN"))
            if not (dated and marked):
                missing.append("%s -> %s carries no dated audit/retraction marker (looked for "
                               "'%s' or '%s')" % (ch, os.path.basename(note), short, date))
        for m in missing:
            fail("chapters", "%s: %s" % (os.path.basename(rec), m))
        print("  %s: %d chapters covered, %d without a matching note marker"
              % (os.path.basename(rec), len(covered), len(missing)))


# ----------------------------------------------------------------- main
CHECKS = [
    ("1. retractions  ", check_retractions),
    ("2+3. memory     ", check_memory),
    ("4. skill backup ", check_skill_backup),
    ("5. plugin       ", check_plugin),
    ("6. chapter sync ", check_chapters),
]


def main():
    if sys.stdout.encoding and sys.stdout.encoding.lower() != "utf-8":
        try:
            sys.stdout.reconfigure(encoding="utf-8", errors="replace")
        except Exception:
            pass
    print("=" * 72)
    print("  EB PROSTEEL AGENT -- consistency guard")
    print("=" * 72)
    for label, fn in CHECKS:
        print()
        print(label)
        try:
            fn()
        except Exception as e:
            fail(label.strip(), "the check itself threw: %s" % e)

    print()
    print("=" * 72)
    if WARNS:
        print("  %d WARNING(S)" % len(WARNS))
        for c, m in WARNS:
            print("   ~ [%s] %s" % (c, m))
        print()
    if FAILS:
        print("  %d FAILURE(S) -- DO NOT COMMIT" % len(FAILS))
        for c, m in FAILS:
            print("   X [%s] %s" % (c, m))
        print("=" * 72)
        return 1
    print("  CLEAN -- safe to commit")
    print("=" * 72)
    return 0


if __name__ == "__main__":
    sys.exit(main())
