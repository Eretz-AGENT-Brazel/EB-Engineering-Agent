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
DEV = os.path.dirname(HERE)     # API+KNOWLEDGE-DEVELOP -- development track 1 (12/08/2026 reorg)
REPO = os.path.dirname(DEV)     # the repo root: PROGRESS.md, agent-brain, standards, _archive
HOME = os.path.expanduser("~")

SKILL_LIVE = os.path.join(HOME, ".claude", "skills", "prosteel-modeling")
SKILL_COPY = os.path.join(REPO, "agent-brain", "skill-prosteel-modeling")
MEM_LIVE = os.path.join(HOME, ".claude", "projects", "C--Users-User-Desktop", "memory")
KNOW = os.path.join(DEV, "knowledge")
RECORD_DIR = os.path.join(KNOW, "learning", "audits")
NOTES_DIR = os.path.join(KNOW, "learning", "manual")
RETRACTED = os.path.join(HERE, "retracted.tsv")

# What the REPO-root scan skips (checks 1 and 7 share this — they must never disagree).
# Content roots scanned separately (knowledge, the live skill, the live memory) or dirs holding
# code/models rather than documents. ⚠️ agent-brain is NOT skipped as a whole since 12/08/2026:
# its root carries the program charter (PROGRAM.md) and must stay under the guard. Only its two
# auto-generated mirrors are excluded, because they duplicate the live skill and memory that are
# already scanned directly — and a finding inside a mirror belongs to its live original anyway.
# NOTE os.walk pruning is BY NAME AT ANY DEPTH, so these entries also prune the same-named dirs
# nested inside API+KNOWLEDGE-DEVELOP/ — which is exactly what we want. The dev folder's own
# root, lessons/ and research/ are NOT listed, so they ARE scanned.
REPO_SKIP = ("knowledge", "app", "projects", "qc", "assets", "data", "standards",
             "skill-prosteel-modeling", "memory")

# A quoted claim inside a retraction is correct and must not trip the check. These markers open a
# window in which the withdrawn wording is expected to appear.
# ⚠️ These must be UNAMBIGUOUS. The first version included bare "closed" and "נסגר", and
# RESUME-HERE.md has a section headed "## מה נסגר בהמשך היום" — "what got CLOSED today" in the
# sense of FINISHED, not RETRACTED. That false friend whitewashed the whole section and let a
# withdrawn claim sit in it undetected. A marker must mean "this was taken back", nothing else.
RETRACTION_MARKERS = ("retracted", "retraction", "withdrawn", "withdraw",
                      "corrected 10/08", "corrected 2026",
                      "used to say", "used to read", "used to be headed", "used to end",
                      "this line used to", "this block used to", "this section used to",
                      "no longer true", "disproved", "disproven", "🛑",
                      "הופרך", "הפרכ", "הפריכ", "מופרכ", "נמשך בחזרה", "בוטל")

# Protection is LINE-LOCAL, not section-wide. The first version scoped it to markdown sections
# found with a leading "#", which failed twice over: SKILL.md writes its guidance as ">" blocks
# so its real structure was invisible, and one marker anywhere in a long section whitewashed
# everything else in it. A quote inside a retraction sits within a few lines of it.
WINDOW = 6

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


def _is_quoted(line, phrase):
    """Is the phrase inside quotation marks on this line?

    Quoting a withdrawn claim is how a retraction is written; asserting it is the failure. Handles
    the straight ", the typographic pair “ ”, and the Hebrew-side ״ — and requires the phrase to
    sit BETWEEN an opening and a closing mark, not merely on a line that happens to contain one.
    """
    i = line.find(phrase)
    if i < 0:
        return False
    before, after = line[:i], line[i + len(phrase):]
    for op, cl in (('"', '"'), ('“', '”'), ('«', '»'), ('״', '״')):
        if op in before and cl in after:
            return True
    return False


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

    roots = [(KNOW, ()), (SKILL_LIVE, ()), (MEM_LIVE, ()), (REPO, REPO_SKIP)]
    scanned = 0
    for root, skip in roots:
        if not os.path.isdir(root):
            continue
        for path in md_files(root, skip_dirs=skip + ("__pycache__", "_attic", "_archive", "Z-ARCHIVE")):
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

            # Quoting a withdrawn claim inside the retraction that withdraws it is correct;
            # asserting it anywhere else is not. A genuine quote sits WITHIN A FEW LINES of the
            # marker, so the window is small and symmetric.
            marks = [i for i, l in enumerate(lines)
                     if any(m in l.lower() for m in RETRACTION_MARKERS)]

            for e in entries:
                for i, l in enumerate(lines):
                    if e["phrase"] not in l:
                        continue
                    if any(abs(i - m) <= WINDOW for m in marks):
                        continue
                    # ⭐ QUOTING IS REPORTING; UNQUOTED IS ASSERTING. A withdrawn claim shown in
                    # quotation marks is being described -- "it used to say X" -- and that is how
                    # every retraction in this corpus is written. The same words with no quotes
                    # around them are a live claim. This one rule separated four real findings
                    # from four false ones on 10/08.
                    if _is_quoted(l, e["phrase"]):
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
    api = os.path.join(DEV, "app", "eb_api.py")
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
    pdir = os.path.join(DEV, "app", "plugin")
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
# ----------------------------------------------------------------- 7. version drift
def check_version_claims():
    """No document may ASSERT a canonical plugin version that disagrees with app/eb_api.py.

    Found 10/08/2026: the skill's ops reference -- the first thing read before every single op --
    declared "Canonical build: EBAgentApi91.dll", 61 builds stale, while the memory declared v6
    and eb_api.py's own docstring said EB_RUN6 above code that ran v152. Four numbers, one truth.
    ⇒ The version is a COMPUTED fact with exactly one source. Everything else points at it.
    """
    src = read(os.path.join(DEV, "app", "eb_api.py"))
    m = re.search(r"^\s*RUN_CMD\s*=\s*[\"']EB_RUN(\d+)[\"']", src, re.M)
    if not m:
        warn("versions", "cannot read RUN_CMD from app/eb_api.py")
        return
    live = int(m.group(1))
    # Only lines that CLAIM canonicity count. A dated log line recording what was true then is
    # history and is left alone.
    CLAIMY = ("canonical", "points at", "current build", "the build is", "הקנונית", "הגרסה הנוכחית")
    hits = 0
    roots = [(KNOW, ()), (SKILL_LIVE, ()), (MEM_LIVE, ()), (REPO, REPO_SKIP)]
    for root, skip in roots:
        if not os.path.isdir(root):
            continue
        for path in md_files(root, skip_dirs=skip + ("__pycache__", "_attic", "_archive", "Z-ARCHIVE")):
            body = read(path)
            # A file may DECLARE itself an append-only dated log with <!-- DATED-LOG --> near the
            # top. Its sections record what was true when written and are not claims about now.
            # The declaration must be visible to a human reader too -- see acad-agent.md's banner.
            if "<!-- DATED-LOG -->" in body[:4000]:
                continue
            for i, l in enumerate(body.split("\n")):
                low = l.lower()
                if not any(c in low for c in CLAIMY):
                    continue
                # ⭐ A DATED claim is a RECORD of what was true then; an undated one is an
                # assertion about now. acad-agent.md is a session log whose headers legitimately
                # read "(2026-08-02) ... canonical plugin v31" -- that is history, not a lie.
                if re.search(r"\d{2}/\d{2}/\d{4}|\d{4}-\d{2}-\d{2}|\(\d{4}-\d{2}-\d{2}\)", l):
                    continue
                for mm in re.finditer(r"EBAgentApi(\d+)\.(?:dll|cs)|EB_RUN(\d+)|ApiCmds(\d+)", l):
                    n = int(mm.group(1) or mm.group(2) or mm.group(3))
                    if n != live:
                        fail("versions",
                             "%s:%d claims plugin v%d as canonical; app/eb_api.py runs v%d\n"
                             "        line   : %s"
                             % (os.path.relpath(path, HOME), i + 1, n, live, l.strip()[:110]))
                        hits += 1
    print("  live build v%d ; %d contradicting claims" % (live, hits))


# ----------------------------------------------------------------- 8. built, not just learned
def check_built():
    """A chapter marked closed must point at an ARTEFACT, not only at a lesson.

    ⚡ WHY THIS EXISTS. On 11/08/2026 E.1 was committed with a ✅ in PROGRESS.md, a full set of
    chapter notes, an embedded skill section and a pushed commit -- and NO STAIRCASE IN THE
    MODEL. The API's creator refuses, that refusal was measured and written up, and the write-up
    was mistaken for the implementation. Amir found it by opening the drawing.

    Step 2 of the five-step procedure is "build it in a dedicated band and read every result back
    FROM THE MODEL". Proving a command unreachable is a finding. It is not a build.

    So: every chapter row marked ✅ must have a notes file that cites real evidence -- a handle,
    a census, a part count or a verification line. A row that cites nothing measurable is a row
    that closed on reading alone.
    """
    prog = os.path.join(REPO, "PROGRESS.md")
    if not os.path.isfile(prog):
        warn("built", "no PROGRESS.md")
        return
    rows = re.findall(r"^\|\s*✅\s*\|\s*([A-Z]\.\d+)\s*\|", read(prog), re.M)
    if not rows:
        warn("built", "no closed chapters found in PROGRESS.md")
        return

    # ⚠️ THE FIRST VERSION OF THIS CHECK WOULD NOT HAVE CAUGHT E.1. It asked for "a measurement",
    # and E.1's bad notes were FULL of measurements -- seven refusal configurations, census 100
    # before and 100 after. Measuring a refusal precisely is still not building anything. So the
    # evidence demanded here is specifically a BUILD PROOF: parts that exist in the band, counted
    # or verified. Nothing else clears a chapter.
    BUILT = (r"vfy_fit", r"collisions?\s*=\s*\d+", r"\bparts\s*=\s*\d+",
             r"BOLT-NO-HOLE\s*=\s*\d+", r"\bbuilt\b.{0,40}\bband\b", r"נבנה")
    # ...and a chapter with genuinely nothing to build (a dialog, a settings page, a command list)
    # clears by SAYING SO, in one line, on the record. Declaring it is documentation. Leaving it
    # silent is what went wrong.
    DECLARED = r"NO MODEL ARTEFACT"

    legacy = {}
    lp = os.path.join(HERE, "built-legacy.tsv")
    if os.path.isfile(lp):
        for l in read(lp).splitlines():
            if l.strip() and not l.startswith("#") and "\t" in l:
                k, _, v = l.partition("\t")
                legacy[k.strip()] = v.strip()

    missing, unreviewed = [], []
    for ch in sorted(set(rows)):
        part, num = ch.split(".")
        pdir = os.path.join(NOTES_DIR, part)
        pat = re.compile(r"(MANUAL-NOTES-)?%s0?%s[-.]" % (part, num))
        note = None
        if os.path.isdir(pdir):
            for f in sorted(os.listdir(pdir)):
                if pat.match(f):
                    note = os.path.join(pdir, f)
                    break
        if note is None:
            missing.append("%s is marked ✅ but has no notes file at all" % ch)
            continue
        body = read(note)
        if re.search(DECLARED, body):
            continue
        if not any(re.search(p, body, re.I) for p in BUILT):
            if ch in legacy:
                unreviewed.append("%-5s %s" % (ch, legacy[ch]))
                continue
            missing.append(
                "%s is marked ✅ but its notes show NO BUILD PROOF -- no part count, no vfy_fit, "
                "no collision check. Either build it in its band, or write the line "
                "'NO MODEL ARTEFACT -- <why>' in %s."
                % (ch, os.path.relpath(note, REPO)))
    for m in missing:
        fail("built", m)
    print("  %d chapters marked closed, %d proven or declared, %d unreviewed backlog"
          % (len(set(rows)), len(set(rows)) - len(unreviewed) - len(missing), len(unreviewed)))
    if unreviewed:
        # printed on EVERY run, never silently -- this backlog is meant to itch
        print("  ⚠️  closed before the build-proof rule, still unreviewed:")
        for u in unreviewed:
            print("       %s" % u)
    stale = sorted(set(legacy) - set(rows) - set(u.split()[0] for u in unreviewed))
    for s in stale:
        warn("built", "built-legacy.tsv lists %s, which no longer needs it -- delete the row" % s)


# ----------------------------------------------------------------- 9. built, and actually JOINED
def check_joints():
    """⛔ NO MORE CUTTING CORNERS. Amir, 11/08/2026, after opening the E.4 portal frame.

    Check 8 asks whether a chapter BUILT anything. It cannot ask whether what was built is
    CORRECT -- and E.4 passed it with a frame whose two rafters carried zero holes and were
    joined to nothing. `vfy_fit bolts=196 OK=196 BOLT-NO-HOLE=0` was true and meaningless: it
    verifies each BOLT against the parts it is LINKED to, and is blind to a main member that was
    never in the joint at all.

    So a chapter that builds a CONNECTED assembly must cite a JOINT AUDIT -- app/vfy_joined.py,
    which asks the question from the member's side: what joins this part to anything? Every member
    must be bolted, or declared welded, part by part.

    A welded joint is legitimate (B.23: welds are not creatable from code). Declaring it is the
    price. Silence is what produced the frame.
    """
    prog = os.path.join(REPO, "PROGRESS.md")
    if not os.path.isfile(prog):
        warn("joints", "no PROGRESS.md")
        return
    rows = re.findall(r"^\|\s*✅\s*\|\s*([A-Z]\.\d+)\s*\|", read(prog), re.M)
    # ⚠️ The trigger must be CHAPTERS THAT BUILT A BOLTED ASSEMBLY, not chapters that mention
    # bolts. The first version fired on A.2 "language selection" because the word appears in it.
    # A guard that cries wolf gets switched off, which is the failure it was written to prevent.
    # The honest signal is the op that CREATES bolts, or a stated per-chapter bolt count.
    NEEDS = re.compile(r"\bboltparts\b|\bbolts\s+\d+\s*(?:×|x)\s*M\d|\d+\s*(?:×|x)\s*M\d\d?\s+DIN", re.I)
    CITES = re.compile(r"JOINT AUDIT|vfy_joined", re.I)
    legacy = {}
    lp = os.path.join(HERE, "joints-legacy.tsv")
    if os.path.isfile(lp):
        for l in read(lp).splitlines():
            if l.strip() and not l.startswith("#") and "\t" in l:
                k, _, v = l.partition("\t")
                legacy[k.strip()] = v.strip()

    missing, unaudited = [], []
    for ch in sorted(set(rows)):
        part, num = ch.split(".")
        pdir = os.path.join(NOTES_DIR, part)
        pat = re.compile(r"(MANUAL-NOTES-)?%s0?%s[-.]" % (part, num))
        note = None
        if os.path.isdir(pdir):
            for f in sorted(os.listdir(pdir)):
                if pat.match(f):
                    note = os.path.join(pdir, f)
                    break
        if note is None:
            continue                       # check 8 already owns the missing-notes case
        body = read(note)
        if NEEDS.search(body) and not CITES.search(body):
            if ch in legacy:
                unaudited.append("%-5s %s" % (ch, legacy[ch]))
                continue
            missing.append(
                "%s builds a BOLTED assembly but cites no JOINT AUDIT. `vfy_fit` cannot see a "
                "member that was never in the joint -- E.4's rafters had zero holes and it "
                "passed. Run app/vfy_joined.py over the band and paste the result into %s."
                % (ch, os.path.relpath(note, REPO)))
    for m in missing:
        fail("joints", m)
    print("  %d closed chapters, %d missing a joint audit, %d unaudited backlog"
          % (len(set(rows)), len(missing), len(unaudited)))
    if unaudited:
        print("  ⚠️  bolted assemblies closed before the joint-audit rule:")
        for u in unaudited:
            print("       %s" % u)


CHECKS = [
    ("1. retractions  ", check_retractions),
    ("2+3. memory     ", check_memory),
    ("4. skill backup ", check_skill_backup),
    ("5. plugin       ", check_plugin),
    ("6. chapter sync ", check_chapters),
    ("7. version drift", check_version_claims),
    ("8. built not read", check_built),
    ("9. joint audit  ", check_joints),
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
