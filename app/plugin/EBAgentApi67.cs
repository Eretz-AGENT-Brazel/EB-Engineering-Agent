// EBAgentApi.cs - EB PROSTEEL AGENT native modeling API (v9 - adds dumpmodel model-reader).
// Runs INSIDE AutoCAD 2015 + ProStructures V8i SS6 (NETLOAD).
// Creates REAL ProSteel objects (PsShape beams, PsPlate, PsBolt, miter cuts)
// programmatically - NO dialogs. Discovered via reflection dump of
// ProStructuresNet.dll (see api_dump_ProStructuresNet.txt).
//
// Protocol (file-based, avoids command-line quoting + supports Hebrew):
//   1. Python writes  eb_cmd.txt  (key=value lines, op=... first)
//   2. Python sends command  EB_RUN67
//   3. Plugin executes, writes eb_result.txt: "EB_OK {info}" or "EB_ERR {reason}"
// C# 5 compatible (csc v4.0.30319).

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Bentley.ProStructures.Geometry.Data;
using Bentley.ProStructures.Geometry.Utilities;   // v67: PsSurfaceFinder
using Bentley.ProStructures.Steel.Shape;
using Bentley.ProStructures.Steel.Plate;
using Bentley.ProStructures.Steel.Bolt;
using Bentley.ProStructures.Modification.Edit;
using Bentley.ProStructures.Modification;
using Bentley.ProStructures.Modification.ObjectData;
using Bentley.ProStructures.Connection.General;
using Bentley.ProStructures.Connection.LinkData;
using Bentley.ProStructures.Connection.Standard;
using Autodesk.AutoCAD.EditorInput;   // v41: Editor.Command / SetImpliedSelection
using Bentley.ProStructures.Property;
using Bentley.ProStructures.Concrete;   // v34: PsCreateFastener (anchor bolts)
using Bentley.ProStructures.Modeling;   // v34: PsObjectGroup, PsCollisionCheck
using Bentley.ProStructures.Miscellaneous;  // v35: PsObjectStyleList
using Bentley.ProStructures;
using Bentley.ProStructures.Modeling;
// PsShapeLoader lives in Steel.Shape (already imported)

[assembly: CommandClass(typeof(EBAgent.ApiCmds67))]
[assembly: ExtensionApplication(typeof(EBAgent.EBApp67))]

namespace EBAgent
{
    // Registers an assembly resolver so ProSteel's managed assemblies are found
    // in the Prg folder even from a cold AutoCAD session (before any ProSteel cmd).
    public class EBApp67 : IExtensionApplication
    {
        const string PrgDir = @"C:\Program Files\Bentley\ProStructures Ss6 R1\AutoCAD 2015\Prg";
        public void Initialize() { AppDomain.CurrentDomain.AssemblyResolve += Resolve; }
        public void Terminate() { }
        static Assembly Resolve(object sender, ResolveEventArgs args)
        {
            try
            {
                string n = new AssemblyName(args.Name).Name;
                string p = Path.Combine(PrgDir, n + ".dll");
                if (File.Exists(p)) return Assembly.LoadFrom(p);
            }
            catch { }
            return null;
        }
    }

    // ---- Learning-mode recorder: subscribes to AutoCAD's real events and logs them ----
    static class Rec
    {
        public static bool On = false;
        public static string LogPath = "";
        static Document _doc;
        static Database _db;
        static string _curCmd = "";
        static string _lastCmd = "";
        static readonly string[] SKIP = { "EB_RUN", "REGEN", "ZOOM", "PAN", "VPOINT",
            "QSAVE", "NETLOAD", "EB_", "'", ".", "GRID", "SNAP", "OSNAP" };

        static string J(string x)
        {
            if (x == null) return "";
            return x.Replace("\\", "/").Replace("\"", "'");
        }

        static void Write(string body)
        {
            try { File.AppendAllText(LogPath,
                "{\"t\":\"" + DateTime.Now.ToString("HH:mm:ss") + "\"," + body + "}\n",
                Encoding.UTF8); }
            catch { }
        }

        static bool Skip(string name)
        {
            if (name == null) return true;
            string u = name.ToUpper();
            foreach (string k in SKIP) if (u.Contains(k)) return true;
            return false;
        }

        public static void Start(string path)
        {
            Stop();
            LogPath = path; On = true;
            DocumentCollection dm = Application.DocumentManager;
            try { dm.DocumentActivated += OnDocAct; } catch { }
            Attach(dm.MdiActiveDocument);
            Write("\"ev\":\"learn_start\"");
        }

        public static void Flush() { Enrich(); }

        public static void Stop()
        {
            if (On) Write("\"ev\":\"learn_stop\"");
            On = false;
            try { Application.DocumentManager.DocumentActivated -= OnDocAct; } catch { }
            Detach();
        }

        public static string StatusLine()
        {
            int n = 0;
            try { if (On && File.Exists(LogPath)) n = File.ReadAllLines(LogPath).Length; } catch { }
            return "on=" + (On ? "1" : "0") + " lines=" + n + " log=" + LogPath;
        }

        static void OnDocAct(object s, DocumentCollectionEventArgs e) { Attach(e.Document); }

        static void Attach(Document d)
        {
            if (d == null || d == _doc) return;
            Detach();
            _doc = d; _db = d.Database;
            try
            {
                _doc.CommandWillStart += OnCmdStart;
                _doc.CommandEnded += OnCmdEnd;
                _doc.CommandCancelled += OnCmdCancel;
                _db.ObjectAppended += OnAdd;
                _db.ObjectErased += OnErase;
            }
            catch { }
        }

        static void Detach()
        {
            try
            {
                if (_doc != null)
                {
                    _doc.CommandWillStart -= OnCmdStart;
                    _doc.CommandEnded -= OnCmdEnd;
                    _doc.CommandCancelled -= OnCmdCancel;
                }
                if (_db != null)
                {
                    _db.ObjectAppended -= OnAdd;
                    _db.ObjectErased -= OnErase;
                }
            }
            catch { }
            _doc = null; _db = null;
        }

        static void OnCmdStart(object s, CommandEventArgs e)
        {
            if (Skip(e.GlobalCommandName)) return;
            if (_pending.Count > 0) Enrich();   // safety net: previous command ended
            _curCmd = e.GlobalCommandName;
            Write("\"ev\":\"cmd_start\",\"name\":\"" + J(e.GlobalCommandName) + "\"");
        }
        static void OnCmdEnd(object s, CommandEventArgs e)
        {
            if (Skip(e.GlobalCommandName)) return;
            Write("\"ev\":\"cmd_end\",\"name\":\"" + J(e.GlobalCommandName) + "\"");
            _lastCmd = e.GlobalCommandName;
            _curCmd = "";
            Enrich();          // now the new objects are readable
        }
        static void OnCmdCancel(object s, CommandEventArgs e)
        {
            if (Skip(e.GlobalCommandName)) return;
            Write("\"ev\":\"cmd_cancel\",\"name\":\"" + J(e.GlobalCommandName) + "\"");
            _lastCmd = e.GlobalCommandName;
            _curCmd = "";
            _pending.Clear();  // cancelled: whatever appeared is gone again
        }
        static void OnAdd(object s, ObjectEventArgs e)
        {
            try
            {
                string cls;
                try { cls = e.DBObject.ObjectId.ObjectClass.Name; }
                catch { cls = e.DBObject.GetType().Name; }
                string hx = e.DBObject.Handle.ToString();
                Write("\"ev\":\"obj_add\",\"class\":\"" + J(cls) + "\",\"handle\":\""
                    + hx + "\",\"cmd\":\"" + J(_curCmd) + "\"");
                // remember it; we read its real data only once the command has
                // finished (during ObjectAppended the object is not usable yet)
                if (!_pending.Contains(hx)) _pending.Add(hx);
            }
            catch { }
        }

        // ---- L2: what was actually BUILT, read after the command completes ----
        // Learning "Amir pressed PS_ENDPLATE_NORM" is useless on its own. What
        // matters is "...and it produced 2 plates 185x90x10, 4 bolts, 4 holes
        // dia 19 and an Endplate connection". That is read here.
        static readonly List<string> _pending = new List<string>();

        static string Num(double d)
        {
            return d.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        }

        static void Enrich()
        {
            if (_pending.Count == 0) return;
            List<string> batch = new List<string>(_pending);
            _pending.Clear();
            Database db = _db;
            if (db == null) return;
            foreach (string hx in batch)
            {
                string cls = "?", lay = "", detail = "";
                try
                {
                    long hv = Convert.ToInt64(hx, 16);
                    ObjectId id = db.GetObjectId(false, new Handle(hv), 0);
                    if (id.IsNull || id.IsErased) continue;
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        DBObject o = tr.GetObject(id, OpenMode.ForRead);
                        cls = id.ObjectClass != null ? id.ObjectClass.Name : o.GetType().Name;
                        Entity ent = o as Entity;
                        if (ent != null) lay = ent.Layer;

                        // a shape: profile + catalog + end points + length
                        PsShape sh = o as PsShape;
                        if (sh != null)
                        {
                            string prof = "", cat = "";
                            try { prof = sh.CrossSectionName; } catch { }
                            try { cat = sh.CrossSectionCatalog; } catch { }
                            double L = 0;
                            try { L = sh.Length; } catch { }
                            detail = "\"profile\":\"" + J(prof) + "\",\"catalog\":\"" + J(cat)
                                   + "\",\"len\":" + Num(L);
                            try
                            {
                                PsPoint a = new PsPoint(0, 0, 0), b = new PsPoint(0, 0, 0);
                                sh.GetMidLine(a, b);
                                detail += ",\"p1\":\"" + Num(a.x) + "," + Num(a.y) + "," + Num(a.z)
                                        + "\",\"p2\":\"" + Num(b.x) + "," + Num(b.y) + "," + Num(b.z) + "\"";
                            }
                            catch { }
                        }
                        // a plate: real dimensions AND real contour vertex count
                        PsPlate pl = o as PsPlate;
                        if (pl != null)
                        {
                            double le = 0, wi = 0, th = 0;
                            try { le = pl.Length; } catch { }
                            try { wi = pl.Width; } catch { }
                            try { th = pl.Height; } catch { }
                            int nv = 0;
                            try { PsPolygon pg = new PsPolygon(); pl.GetPolygon(pg); nv = pg.Count; } catch { }
                            detail = "\"dims\":\"" + Num(le) + "x" + Num(wi) + "x" + Num(th)
                                   + "\",\"verts\":" + nv;
                        }
                        // extents for anything else, so position is never lost
                        if (detail.Length == 0 && ent != null)
                        {
                            try
                            {
                                Extents3d x = ent.GeometricExtents;
                                detail = "\"min\":\"" + Num(x.MinPoint.X) + "," + Num(x.MinPoint.Y)
                                       + "," + Num(x.MinPoint.Z) + "\",\"max\":\"" + Num(x.MaxPoint.X)
                                       + "," + Num(x.MaxPoint.Y) + "," + Num(x.MaxPoint.Z) + "\"";
                            }
                            catch { }
                        }
                        tr.Commit();
                    }

                    // holes drilled into it — the thing screenshots cannot show
                    try
                    {
                        string er;
                        int nh = HolesOfStatic(id.OldIdPtr.ToInt64(), out er);
                        if (nh > 0) detail += ",\"holes\":" + nh;
                    }
                    catch { }

                    // is there a CONNECTION on it, and of what kind?
                    try
                    {
                        PsEditLogicalLink ed = new PsEditLogicalLink();
                        ed.SetObjectId(id.OldIdPtr.ToInt64());
                        int n = ed.get_LogicalLinkCount();
                        if (n > 0)
                        {
                            StringBuilder cn = new StringBuilder();
                            for (int i = 0; i < n; i++)
                            {
                                PsLogicalLink lk = null;
                                try { lk = ed.GetLogicalLinkByNumber(ed.get_LinkNumberFromIndex(i)); }
                                catch { }
                                if (lk == null) continue;
                                if (cn.Length > 0) cn.Append("; ");
                                try { cn.Append(lk.Name + "(t" + (int)lk.Type
                                    + ",p" + lk.LinkObjectCount + ",b" + lk.BoltObjectCount + ")"); }
                                catch { }
                            }
                            if (cn.Length > 0) detail += ",\"conn\":\"" + J(cn.ToString()) + "\"";
                        }
                    }
                    catch { }
                }
                catch { }
                Write("\"ev\":\"obj_detail\",\"handle\":\"" + hx + "\",\"class\":\"" + J(cls)
                    + "\",\"layer\":\"" + J(lay) + "\",\"cmd\":\"" + J(_lastCmd) + "\""
                    + (detail.Length > 0 ? "," + detail : ""));
            }
        }

        // hole count without needing the outer class (used inside the recorder)
        internal static int HolesOfStatic(long oid, out string err)
        {
            err = "";
            try
            {
                PsSingleHoleArray arr = new PsSingleHoleArray(oid, (LongHoleMode)0, false, false, false);
                return arr.Count;
            }
            catch (System.Exception ex) { err = ex.Message; return -1; }
        }
        static void OnErase(object s, ObjectErasedEventArgs e)
        {
            if (!e.Erased) return;
            try { Write("\"ev\":\"obj_erase\",\"handle\":\"" + e.DBObject.Handle.ToString() + "\""); }
            catch { }
        }
    }

    public class ApiCmds67
    {
        const string Dir = @"C:\Users\User\Desktop\EB PROSTEEL AGENT\app\plugin";
        static string CurReqId = "";

        static void Result(string text)
        {
            string final = text + (CurReqId.Length > 0 ? " reqid=" + CurReqId : "");
            try
            {
                string res = Path.Combine(Dir, "eb_result.txt");
                string tmp = res + ".tmp";
                File.WriteAllText(tmp, final, Encoding.UTF8);
                if (File.Exists(res)) File.Delete(res);
                File.Move(tmp, res);          // atomic on same volume
            }
            catch { }
            try
            {
                Document d = Application.DocumentManager.MdiActiveDocument;
                if (d != null) d.Editor.WriteMessage("\n" + final + "\n");
            }
            catch { }
        }

        static Dictionary<string, string> ReadCmd()
        {
            var kv = new Dictionary<string, string>();
            string p = Path.Combine(Dir, "eb_cmd.txt");
            if (!File.Exists(p)) return kv;
            foreach (string raw in File.ReadAllLines(p, Encoding.UTF8))
            {
                string line = raw.Trim();
                int i = line.IndexOf('=');
                if (i > 0) kv[line.Substring(0, i).Trim().ToLower()] = line.Substring(i + 1).Trim();
            }
            return kv;
        }

        static string Get(Dictionary<string, string> kv, string k, string dflt)
        {
            return kv.ContainsKey(k) && kv[k].Length > 0 ? kv[k] : dflt;
        }

        static double[] Nums(string s)
        {
            string[] parts = s.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            double[] r = new double[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                r[i] = double.Parse(parts[i], System.Globalization.CultureInfo.InvariantCulture);
            return r;
        }

        static PsPoint Pt(string s)
        {
            double[] n = Nums(s);
            return new PsPoint(n[0], n.Length > 1 ? n[1] : 0.0, n.Length > 2 ? n[2] : 0.0);
        }

        // ---- model census: count + newest handle (verification after create) ----
        static int Census(out string lastHandle, out string lastClass)
        {
            lastHandle = ""; lastClass = "";
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            int n = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    n++;
                    lastHandle = id.Handle.ToString();
                    lastClass = id.ObjectClass != null ? id.ObjectClass.Name : "?";
                }
                tr.Commit();
            }
            return n;
        }

        static long IdFromHandle(string handleHex)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            long h = Convert.ToInt64(handleHex, 16);
            ObjectId oid = db.GetObjectId(false, new Handle(h), 0);
            return oid.OldIdPtr.ToInt64();
        }

        [CommandMethod("EB_RUN67", CommandFlags.Modal)]
        public void Run()
        {
            var kv = ReadCmd();
            string op = Get(kv, "op", "");
            CurReqId = Get(kv, "reqid", "");

            // ---- v39: WRONG-DRAWING GUARD ------------------------------------
            // Twice on 06/08/2026 work went into a drawing that was not the intended
            // one: first two documents were open at once (Amir spotted the two windows),
            // then opening a Bentley sample silently became the active document and every
            // op after it -- drill fields, copes, connections -- landed there. Nothing was
            // lost, but the model was never the one being reasoned about.
            // Every op may now declare which drawing it expects. If the active document is
            // not that drawing, the op REFUSES rather than doing the right thing in the
            // wrong place. Omitting dwg= keeps the old behaviour, so nothing breaks.
            string wantDwg = Get(kv, "dwg", "");
            if (wantDwg.Length > 0)
            {
                string actual = "";
                try
                {
                    Document dchk = Application.DocumentManager.MdiActiveDocument;
                    actual = System.IO.Path.GetFileName(dchk.Name);
                }
                catch { }
                if (actual.Length == 0 ||
                    string.Compare(actual, wantDwg, System.StringComparison.OrdinalIgnoreCase) != 0)
                {
                    Result("EB_ERR wrongdoc op=" + op + " expected='" + wantDwg +
                           "' active='" + actual + "' -- refused, nothing was executed");
                    return;
                }
            }

            try
            {
                string badKeys = UnknownKeys(op, kv);
                    if (badKeys != null)
                    {
                        Result("EB_ERR unknown parameter(s): " + badKeys +
                               " -- refused, nothing was executed");
                        return;
                    }
                    switch (op)
                {
                    case "touchplane": TouchPlane(kv); break;
                    case "touchdrill": TouchDrill(kv); break;
                    case "cutat": CutAt(kv); break;
                    case "polycut": PolyCut(kv); break;
                    case "collision": Collision(kv); break;
                    case "mods": Mods(kv); break;
                    case "edgechamfer": EdgeChamferOp(kv); break;
                    case "outlet": OutletOp(kv); break;
                    case "planecut": PlaneCutOp(kv); break;
                    case "clonedrills": CloneDrills(kv); break;
                    case "posauto": PosAuto(kv); break;
                    case "posset": PosSet(kv); break;
                    case "equal": Equal(kv); break;
                    case "zoom": Zoom(kv); break;
                    case "view": View(kv); break;
                    case "hilite": Hilite(kv); break;
                    case "ping": Result("EB_OK ping " + DateTime.Now.ToString("HH:mm:ss")); break;
                    case "whoami": WhoAmI(); break;
                    case "learn_on": Rec.Start(Get(kv, "log", "")); Result("EB_OK learn_on " + Rec.StatusLine()); break;
                    case "learn_off": Rec.Stop(); Result("EB_OK learn_off " + Rec.StatusLine()); break;
                    case "learn_flush": Rec.Flush(); Result("EB_OK learn_flush " + Rec.StatusLine()); break;
                    case "learn_status": Result("EB_OK learn_status " + Rec.StatusLine()); break;
                    case "beam": Beam(kv); break;
                    case "plate": Plate(kv); break;
                    case "bolt": Bolt(kv); break;
                    case "miter": Miter(kv); break;
                    case "boltprobe": BoltProbe(kv); break;
                    case "workframe": Workframe(kv); break;
                    case "boltfield": BoltField(kv); break;
                    case "conn_bolted": ConnBolted(kv); break;
                    case "list": ListModel(); break;
                    case "dumpmodel": DumpModel(kv); break;
                    case "dumpfull": DumpFull(kv); break;
                    case "dumpfull2": DumpFull2(kv); break;
                    case "clonemodel": CloneModel(kv); break;
                    case "setlayer": SetLayer(kv); break;
                    case "sections": Sections(kv); break;
                    case "dumpcat": DumpCat(kv); break;
                    // ---- v18: holes / contours / drilling (the connection layer) ----
                    case "enumdump": EnumDump(kv); break;
                    case "holes": Holes(kv); break;
                    case "dumpholes": DumpHoles(kv); break;
                    case "platepoly": PlatePoly(kv); break;
                    case "dumppoly": DumpPoly(kv); break;
                    case "drill": Drill(kv); break;
                    case "polyplate": PolyPlate(kv); break;
                    // ---- v19: PS PROPERTIES + PS CONNECTION (Amir's pointer) ----
                    case "props": Props(kv); break;
                    case "connscan": ConnScan(kv); break;
                    case "conntemplates": ConnTemplates(kv); break;
                    case "anchor": Anchor(kv); break;
                    case "styles": Styles(kv); break;
                    case "conn": Conn(kv); break;
                    case "drillfield": DrillField(kv); break;
                    case "group": Group(kv); break;
                    case "cmd": Cmd(kv); break;
                    case "posnum": Posnum(kv); break;
                    case "mirror": Mirror(kv); break;
                    case "copy": CopyNative(kv); break;
                    case "chamfer": Chamfer(kv); break;
                    case "connbase": ConnBase(kv); break;
                    case "connstiff": ConnStiff(kv); break;
                    case "connsplice": ConnSplice(kv); break;
                    case "setpoly": SetPoly(kv); break;
                    case "connremove": ConnRemove(kv); break;
                    case "replicate": Replicate(kv); break;
                    case "rotate": RotateObjs(kv); break;
                    default: Result("EB_ERR unknown op '" + op + "'"); break;
                }
            }
            catch (System.Exception ex)
            {
                Result("EB_ERR " + op + " exception: " + ex.Message);
            }
        }

        // op=beam  name=HEB500  catalog=(optional)  p1=0,0,0  p2=6000,0,0  rot=0
        void Beam(Dictionary<string, string> kv)
        {
            string name = Get(kv, "name", "HEB500");
            string catalog = Get(kv, "catalog", "");
            PsPoint p1 = Pt(Get(kv, "p1", "0,0,0"));
            PsPoint p2 = Pt(Get(kv, "p2", "1000,0,0"));
            double rot = double.Parse(Get(kv, "rot", "0"), System.Globalization.CultureInfo.InvariantCulture);
            double offx = double.Parse(Get(kv, "offx", "0"), System.Globalization.CultureInfo.InvariantCulture);
            double offy = double.Parse(Get(kv, "offy", "0"), System.Globalization.CultureInfo.InvariantCulture);
            bool mirror = Get(kv, "mirror", "0") == "1";
            string wantLayer = Get(kv, "layer", "");
            // explicit section axes: a flipped axis reproduces MIRRORED geometry for
            // asymmetric profiles (MirrorFlag itself is read-only — proven by test)
            string axS = Get(kv, "ax", ""), ayS = Get(kv, "ay", "");

            string h0, c0; int before = Census(out h0, out c0);

            // resolve catalog from the shapes DB if not given
            if (catalog.Length == 0)
            {
                PsShapeLoader ld0 = new PsShapeLoader();
                foreach (string nm0 in new string[] { name, name.Replace(" ", ""),
                    System.Text.RegularExpressions.Regex.Replace(name, "([A-Za-z])([0-9])", "$1 $2") })
                {
                    try { string k = ld0.FindKatalogFromKey(nm0, false); if (k != null && k.Length > 0) { catalog = k; break; } }
                    catch { }
                }
            }
            string[] cats = catalog.Length > 0
                ? new string[] { catalog }
                : new string[] { "", "DIN", "Euro", "EURO", "EU", "GOST", "AISC" };
            string[] names = new string[] { name, name.Replace(" ", ""),
                System.Text.RegularExpressions.Regex.Replace(name, "([A-Za-z])(\\d)", "$1 $2") };

            foreach (string cat in cats)
                foreach (string nm in names)
                {
                    PsCreateShape cs = new PsCreateShape();
                    cs.SetToDefaults();
                    cs.SelectStandardSections();
                    cs.SetCrossSection(nm, cat);
                    cs.SetInsertPoints(p1, p2);
                    if (rot != 0) cs.SetRotation(rot);
                    if (offx != 0) { try { cs.SetXOffset(offx); } catch { } }
                    if (offy != 0) { try { cs.SetYOffset(offy); } catch { } }
                    if (axS.Length > 0) { try { double[] q = Nums(axS); cs.SetXAxis(new PsVector(q[0], q[1], q[2])); } catch { } }
                    if (ayS.Length > 0) { try { double[] q = Nums(ayS); cs.SetYAxis(new PsVector(q[0], q[1], q[2])); } catch { } }
                    bool ok = false;
                    try { ok = cs.Create(); } catch { ok = false; }
                    if (ok)
                    {
                        string h1, c1; int after = Census(out h1, out c1);
                        if (after > before)
                        {
                            string mirState = "n/a";
                            if (mirror) mirState = ApplyMirrorVerified(h1);
                            string layState = "n/a";
                            if (wantLayer.Length > 0) layState = ApplyLayer(h1, wantLayer);
                            Result("EB_OK beam name=" + nm + " catalog=" + (cat.Length > 0 ? cat : "(default)")
                                 + " handle=" + h1 + " class=" + c1 + " off=" + offx + "/" + offy
                                 + " mir_applied=" + mirState + " layer=" + layState + " entities=" + after);
                            return;
                        }
                    }
                }
            Result("EB_ERR beam: no catalog/name combination created '" + name + "'. Try op=list_sections or check the SHAPES DB name.");
        }

        // op=plate  center=x,y,z  l=430 w=220 t=20  [normal=0,0,1]
        void Plate(Dictionary<string, string> kv)
        {
            double[] c = Nums(Get(kv, "center", "0,0,0"));
            double L = double.Parse(Get(kv, "l", "300"), System.Globalization.CultureInfo.InvariantCulture);
            double W = double.Parse(Get(kv, "w", "200"), System.Globalization.CultureInfo.InvariantCulture);
            double T = double.Parse(Get(kv, "t", "20"), System.Globalization.CultureInfo.InvariantCulture);
            double[] nz = Nums(Get(kv, "normal", "0,0,1"));
            string sx = Get(kv, "ex", ""), sy = Get(kv, "ey", ""), sz = Get(kv, "ez", "");
            string plLayer = Get(kv, "layer", "");
            PsPoint origin = new PsPoint(c[0], c[1], c[2]);
            PsVector normal = new PsVector(nz[0], nz.Length > 1 ? nz[1] : 0, nz.Length > 2 ? nz[2] : 1);
            string h0, c0; int before = Census(out h0, out c0);
            StringBuilder diag = new StringBuilder();

            // STRATEGY 0 (v13): full coordinate system from the source object's ECS.
            // Fixes the 96 plates that came out rotated 90 deg in-plane.
            if (sx.Length > 0 && sy.Length > 0)
            {
                try
                {
                    double[] ax = Nums(sx), ay = Nums(sy), az = Nums(sz.Length > 0 ? sz : "0,0,1");
                    PsVector vx = new PsVector(ax[0], ax[1], ax[2]);
                    PsVector vy = new PsVector(ay[0], ay[1], ay[2]);
                    PsVector vz = new PsVector(az[0], az[1], az[2]);
                    PsMatrix m0 = new PsMatrix();
                    m0.SetCoordinateSystem(origin, vx, vy, vz);
                    PsCreatePlate cp0 = new PsCreatePlate();
                    cp0.SetToDefaults();
                    cp0.SetInsertMatrix(m0);
                    cp0.SetAsRectangularPlate(L, W);
                    cp0.SetThickness(T);
                    cp0.UseCurrentLayer(true);
                    bool ok0 = cp0.Create();
                    string hA, cA; int aft0 = Census(out hA, out cA);
                    if (ok0 && aft0 > before)
                    {
                        string ls = plLayer.Length > 0 ? ApplyLayer(hA, plLayer) : "n/a";
                        Result("EB_OK plate " + L + "x" + W + "x" + T + " handle=" + hA
                             + " class=" + cA + " via=ecs layer=" + ls + " entities=" + aft0);
                        return;
                    }
                }
                catch (System.Exception exE) { diag.Append("ecs EX:" + One(exE.Message) + "; "); }
            }
            // Strategy 1: insert matrix (plane) + rectangular plate
            try
            {
                PsMatrix m = new PsMatrix();
                m.SetFromPointAndNormal(origin, normal);
                PsCreatePlate cp = new PsCreatePlate();
                cp.SetToDefaults();
                cp.SetInsertMatrix(m);
                cp.SetAsRectangularPlate(L, W);
                cp.SetThickness(T);
                cp.UseCurrentLayer(true);
                bool ok = cp.Create();
                string h1, c1; int after = Census(out h1, out c1);
                if (ok && after > before) { Result("EB_OK plate " + L + "x" + W + "x" + T + " handle=" + h1 + " class=" + c1 + " via=matrix entities=" + after); return; }
                diag.Append("matrix(ok=" + ok + ",d=" + (after - before) + "); ");
            }
            catch (System.Exception ex) { diag.Append("matrix EX:" + ex.Message + "; "); }

            // Strategy 2: matrix + explicit edge points in that plane
            try
            {
                PsMatrix m = new PsMatrix();
                m.SetFromPointAndNormal(origin, normal);
                PsCreatePlate cp = new PsCreatePlate();
                cp.SetToDefaults();
                cp.SetInsertMatrix(m);
                cp.DeleteAllEdgePoints();
                cp.AppendEdgePoint(new PsPoint(-L / 2, -W / 2, 0));
                cp.AppendEdgePoint(new PsPoint(L / 2, -W / 2, 0));
                cp.AppendEdgePoint(new PsPoint(L / 2, W / 2, 0));
                cp.AppendEdgePoint(new PsPoint(-L / 2, W / 2, 0));
                cp.SetThickness(T);
                cp.UseCurrentLayer(true);
                bool ok = cp.Create();
                string h1, c1; int after = Census(out h1, out c1);
                if (ok && after > before) { Result("EB_OK plate " + L + "x" + W + "x" + T + " handle=" + h1 + " class=" + c1 + " via=edgepts entities=" + after); return; }
                diag.Append("edgepts(ok=" + ok + ",d=" + (after - before) + "); ");
            }
            catch (System.Exception ex) { diag.Append("edgepts EX:" + ex.Message + "; "); }

            Result("EB_ERR plate all strategies failed: " + diag.ToString());
        }

        // op=boltprobe  p1=..  p2=..  -> try candidate style names, report which create a bolt
        void BoltProbe(Dictionary<string, string> kv)
        {
            PsPoint p1 = Pt(Get(kv, "p1", "0,0,1000"));
            PsPoint p2 = Pt(Get(kv, "p2", "0,0,1060"));
            string[] cands = new string[] { "", "Standard", "STANDARD", "Default", "M20", "M 20",
                "DIN933", "DIN 933", "DIN 6914", "DIN6914", "HV", "ISO 4014", "ISO4014", "8.8", "4.6",
                "A325", "A325N", "Grade 8.8", "Standard M20" };
            StringBuilder sb = new StringBuilder();
            foreach (string cand in cands)
            {
                string h0, c0; int before = Census(out h0, out c0);
                string note = "ok";
                try { PsCreateBolt cb = new PsCreateBolt(); cb.SetToDefaults(); cb.CreateSingleBolt(p1, p2, 20.0, cand, 0.0); }
                catch (System.Exception ex) { note = "EX:" + ex.Message; }
                string h1, c1; int after = Census(out h1, out c1);
                sb.AppendLine("[" + (after > before ? "CREATED " + c1 : "no      ") + "] style='" + cand + "' " + note);
            }
            File.WriteAllText(Path.Combine(Dir, "eb_bolt.txt"), sb.ToString(), Encoding.UTF8);
            Result("EB_OK boltprobe -> eb_bolt.txt");
        }

        // op=workframe  at=x,y,z  x=6000 y=3000  (grid)
        void Workframe(Dictionary<string, string> kv)
        {
            PsPoint at = Pt(Get(kv, "at", "0,0,0"));
            double xe = double.Parse(Get(kv, "x", "6000"), System.Globalization.CultureInfo.InvariantCulture);
            double ye = double.Parse(Get(kv, "y", "3000"), System.Globalization.CultureInfo.InvariantCulture);
            string h0, c0; int before = Census(out h0, out c0);
            PsCreateWorkframe wf = new PsCreateWorkframe();
            wf.SetToDefaults();
            wf.SetInsertPoint(at);
            wf.SetXYPlane(new PsVector(1,0,0), new PsVector(0,1,0));
            wf.SetRectangularExtents(xe, ye);
            bool ok = wf.Create();
            string h1, c1; int after = Census(out h1, out c1);
            if (ok) Result("EB_OK workframe " + xe + "x" + ye + " handle=" + h1 + " entities=" + after);
            else Result("EB_ERR workframe create failed");
        }

        // op=bolt  p1=.. p2=.. dia=20 style=DIN6914 [hosts=h1,h2]
        void Bolt(Dictionary<string, string> kv)
        {
            PsPoint p1 = Pt(Get(kv, "p1", "0,0,0"));
            PsPoint p2 = Pt(Get(kv, "p2", "0,0,50"));
            double dia = double.Parse(Get(kv, "dia", "20"), System.Globalization.CultureInfo.InvariantCulture);
            string style = Get(kv, "style", "DIN6914");
            string hosts = Get(kv, "hosts", "");
            string boLayer = Get(kv, "layer", "");
            double wantLen = double.Parse(Get(kv, "len", "0"), System.Globalization.CultureInfo.InvariantCulture);
            string h0, c0; int before = Census(out h0, out c0);
            PsCreateBolt cb = new PsCreateBolt();
            cb.SetToDefaults();
            long[] hostIds2 = ParseHandles(hosts);
            int hostsAdded = 0;
            foreach (long id in hostIds2) { try { cb.AddObject(id); hostsAdded++; } catch { } }
            cb.CreateSingleBolt(p1, p2, dia, style, 0.0);
            string h1, c1; int after = Census(out h1, out c1);
            string blen = "n/a";
            if (after > before && wantLen > 0) blen = ApplyBoltLength(h1, wantLen);
            string bls = "n/a";
            if (after > before && boLayer.Length > 0) bls = ApplyLayer(h1, boLayer);
            if (after > before) Result("EB_OK bolt dia=" + dia + " style=" + style + " handle=" + h1
                 + " class=" + c1 + " hosts=" + hostsAdded + "/" + hostIds2.Length
                 + " len=" + blen + " layer=" + bls + " entities=" + after);
            else Result("EB_ERR bolt create failed (style '" + style + "')");
        }

        static long[] ParseHandles(string s)
        {
            string[] p = s.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            System.Collections.Generic.List<long> r = new System.Collections.Generic.List<long>();
            foreach (string x in p) { try { r.Add(IdFromHandle(x.Trim())); } catch { } }
            return r.ToArray();
        }

        // op=boltfield center=x,y,z nx=2 ny=2 sx=100 sy=100 dia=20 gap=60 style=DIN6914 hosts=..
        void BoltField(Dictionary<string, string> kv)
        {
            double[] c = Nums(Get(kv, "center", "0,0,0"));
            int nx = (int)double.Parse(Get(kv, "nx", "2"), System.Globalization.CultureInfo.InvariantCulture);
            int ny = (int)double.Parse(Get(kv, "ny", "2"), System.Globalization.CultureInfo.InvariantCulture);
            double sx = double.Parse(Get(kv, "sx", "100"), System.Globalization.CultureInfo.InvariantCulture);
            double sy = double.Parse(Get(kv, "sy", "100"), System.Globalization.CultureInfo.InvariantCulture);
            double dia = double.Parse(Get(kv, "dia", "20"), System.Globalization.CultureInfo.InvariantCulture);
            double gap = double.Parse(Get(kv, "gap", "60"), System.Globalization.CultureInfo.InvariantCulture);
            string style = Get(kv, "style", "DIN6914");
            long[] hostIds = ParseHandles(Get(kv, "hosts", ""));
            string h0, c0; int before = Census(out h0, out c0);
            int made = 0;
            for (int ix = 0; ix < nx; ix++)
                for (int iy = 0; iy < ny; iy++)
                {
                    double y = c[1] + (ix - (nx - 1) / 2.0) * sx;
                    double z = c[2] + (iy - (ny - 1) / 2.0) * sy;
                    PsCreateBolt cb = new PsCreateBolt();
                    cb.SetToDefaults();
                    foreach (long id in hostIds) { try { cb.AddObject(id); } catch { } }
                    cb.CreateSingleBolt(new PsPoint(c[0] - gap, y, z), new PsPoint(c[0] + gap, y, z), dia, style, 0.0);
                    made++;
                }
            string h1, c1; int after = Census(out h1, out c1);
            Result("EB_OK boltfield " + made + " bolts M" + dia + " added=" + (after - before) + " entities=" + after);
        }

        // op=miter  cut=<handle of beam to cut>  other=<handle of reference beam>  type=1
        void Miter(Dictionary<string, string> kv)
        {
            long idCut = IdFromHandle(Get(kv, "cut", ""));
            long idOther = IdFromHandle(Get(kv, "other", ""));
            bool type = Get(kv, "type", "1") != "0";
            PsCutObjects co = new PsCutObjects();
            co.SetToDefaults();
            co.SetObjectId(idCut);
            co.SetAsMiterCutId(idOther, type);
            int r = co.Apply();
            Result(r >= 0 ? "EB_OK miter applied=" + r : "EB_ERR miter Apply()=" + r);
        }

        // op=sections  filter=HEB  -> dump catalogs + matching section names
        void Sections(Dictionary<string, string> kv)
        {
            string filter = Get(kv, "filter", "").ToUpper();
            PsShapeLoader ld = new PsShapeLoader();
            StringBuilder sb = new StringBuilder();
            int nc = ld.CatalogCount;
            sb.AppendLine("CATALOGS " + nc);
            for (int i = 0; i < nc; i++)
            {
                string cat = "";
                try { cat = ld.GetCatalog(i); } catch { continue; }
                sb.AppendLine("[" + i + "] " + cat);
            }
            // find where the filter section lives + list matching names per catalog
            if (filter.Length > 0)
            {
                try { sb.AppendLine("FindKatalogFromKey(" + filter + ") = " + ld.FindKatalogFromKey(filter, false)); } catch (System.Exception ex) { sb.AppendLine("Find err " + ex.Message); }
                for (int i = 0; i < nc; i++)
                {
                    int nn = 0; string cat = "";
                    try { cat = ld.GetCatalog(i); nn = ld.get_NameCount(i); } catch { continue; }
                    int shown = 0;
                    for (int k = 0; k < nn; k++)
                    {
                        string nm = "";
                        try { nm = ld.GetName(i, k); } catch { continue; }
                        if (nm != null && nm.ToUpper().Contains(filter))
                        {
                            sb.AppendLine("  " + cat + " :: " + nm);
                            if (++shown >= 25) { sb.AppendLine("  ...more in " + cat); break; }
                        }
                    }
                }
            }
            File.WriteAllText(Path.Combine(Dir, "eb_sections.txt"), sb.ToString(), Encoding.UTF8);
            Result("EB_OK sections catalogs=" + nc + " -> eb_sections.txt");
        }


        // op=dumpcat catalog=DIN_HEB -> all section names in that catalog
        void DumpCat(Dictionary<string, string> kv)
        {
            string want = Get(kv, "catalog", "DIN_HEB");
            PsShapeLoader ld = new PsShapeLoader();
            StringBuilder sb = new StringBuilder();
            int nc = ld.CatalogCount;
            for (int i = 0; i < nc; i++)
            {
                string cat=""; try { cat=ld.GetCatalog(i); } catch { continue; }
                if (cat != want) continue;
                int nn=0; try { nn=ld.get_NameCount(i); } catch {}
                sb.AppendLine("CATALOG "+cat+" idx="+i+" names="+nn);
                for (int k=0;k<nn;k++){ try { sb.AppendLine("  ["+k+"] "+ld.GetName(i,k)); } catch {} }
            }
            File.WriteAllText(Path.Combine(Dir,"eb_cat.txt"), sb.ToString(), Encoding.UTF8);
            Result("EB_OK dumpcat "+want);
        }

        // op=conn_bolted at=x,y,z pl=220 pw=220 pt=20 gap=60 nx=2 ny=2 sx=100 sy=100 dia=20 style=DIN6914
        void ConnBolted(Dictionary<string, string> kv)
        {
            double[] c = Nums(Get(kv, "at", "0,0,0"));
            double PL = double.Parse(Get(kv, "pl", "220"), System.Globalization.CultureInfo.InvariantCulture);
            double PW = double.Parse(Get(kv, "pw", "220"), System.Globalization.CultureInfo.InvariantCulture);
            double PT = double.Parse(Get(kv, "pt", "20"), System.Globalization.CultureInfo.InvariantCulture);
            double gap = double.Parse(Get(kv, "gap", "60"), System.Globalization.CultureInfo.InvariantCulture);
            int nx = (int)double.Parse(Get(kv, "nx", "2"), System.Globalization.CultureInfo.InvariantCulture);
            int ny = (int)double.Parse(Get(kv, "ny", "2"), System.Globalization.CultureInfo.InvariantCulture);
            double sx = double.Parse(Get(kv, "sx", "100"), System.Globalization.CultureInfo.InvariantCulture);
            double sy = double.Parse(Get(kv, "sy", "100"), System.Globalization.CultureInfo.InvariantCulture);
            double dia = double.Parse(Get(kv, "dia", "20"), System.Globalization.CultureInfo.InvariantCulture);
            string style = Get(kv, "style", "DIN6914");
            StringBuilder res = new StringBuilder();
            PsVector nX = new PsVector(1, 0, 0);
            string[] ph = new string[2];
            double[] offs = new double[] { gap / 2, -gap / 2 };
            for (int i = 0; i < 2; i++)
            {
                string hb, cbx; int before = Census(out hb, out cbx);
                PsMatrix m = new PsMatrix();
                m.SetFromPointAndNormal(new PsPoint(c[0] + offs[i], c[1], c[2]), nX);
                PsCreatePlate cp = new PsCreatePlate();
                cp.SetToDefaults(); cp.SetInsertMatrix(m);
                cp.SetAsRectangularPlate(PL, PW); cp.SetThickness(PT); cp.UseCurrentLayer(true);
                cp.Create();
                string ha, ca; int after = Census(out ha, out ca);
                ph[i] = (after > before) ? ha : "";
                res.Append("plate" + (i + 1) + "=" + ph[i] + " ");
            }
            long[] hostIds = ParseHandles(ph[0] + "," + ph[1]);
            string hb2, cb3; int b2 = Census(out hb2, out cb3);
            int made = 0;
            for (int ix = 0; ix < nx; ix++)
                for (int iy = 0; iy < ny; iy++)
                {
                    double y = c[1] + (ix - (nx - 1) / 2.0) * sx;
                    double z = c[2] + (iy - (ny - 1) / 2.0) * sy;
                    PsCreateBolt cbolt = new PsCreateBolt();
                    cbolt.SetToDefaults();
                    foreach (long id in hostIds) { try { cbolt.AddObject(id); } catch { } }
                    cbolt.CreateSingleBolt(new PsPoint(c[0] - gap, y, z), new PsPoint(c[0] + gap, y, z), dia, style, 0.0);
                    made++;
                }
            string ha2, ca2; int a2 = Census(out ha2, out ca2);
            Result("EB_OK conn_bolted " + res.ToString().Trim() + " bolts=" + made + " boltentities+=" + (a2 - b2) + " total=" + a2);
        }

        // op=whoami -> active document name + entity count (transport/active-doc check)
        void WhoAmI()
        {
            string h, c; int n = Census(out h, out c);
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Result("EB_OK whoami doc=" + (doc != null ? doc.Name : "?") + " entities=" + n);
        }

        // op=list  -> handles + classes of all modelspace entities

        // op=dumpmodel  [out=eb_model.txt]
        // MODEL READER (L2): reads real semantics of every ProSteel object:
        // shapes -> profile + catalog + midline start/end + length
        // plates -> length/width/height(thickness) + insert point + polygon vertices
        // bolts  -> diameter + style + count + insert point
        // Output: one TSV line per object, so Python can rebuild the model.
        void DumpModel(Dictionary<string, string> kv)
        {
            string outName = Get(kv, "out", "eb_model.txt");
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            StringBuilder sb = new StringBuilder();
            int nShape = 0, nPlate = 0, nBolt = 0, nOther = 0, nErr = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    string cls = (id.ObjectClass != null ? id.ObjectClass.Name : "?");
                    string hnd = id.Handle.ToString();
                    try
                    {
                        DBObject o = tr.GetObject(id, OpenMode.ForRead);

                        PsShape sh = o as PsShape;
                        if (sh != null)
                        {
                            PsPoint a = new PsPoint(0, 0, 0), b = new PsPoint(0, 0, 0);
                            try { sh.GetMidLine(a, b); } catch { }
                            sb.Append("SHAPE\t").Append(hnd).Append('\t')
                              .Append(Safe(sh.CrossSectionName)).Append('\t')
                              .Append(Safe(sh.CrossSectionCatalog)).Append('\t')
                              .Append(F(a.x)).Append(',').Append(F(a.y)).Append(',').Append(F(a.z)).Append('\t')
                              .Append(F(b.x)).Append(',').Append(F(b.y)).Append(',').Append(F(b.z)).Append('\t')
                              .Append(F(SafeD(delegate() { return sh.Length; }))).Append('\t')
                              .Append(Safe(SafeS(delegate() { return sh.Material; }))).Append('\t')
                              .Append(Safe(SafeS(delegate() { return sh.Name; }))).Append('\t')
                              .Append(cls).AppendLine();
                            nShape++; continue;
                        }

                        PsPlate pl = o as PsPlate;
                        if (pl != null)
                        {
                            PsPoint ip = null;
                            try { ip = pl.InsertPoint; } catch { }
                            if (ip == null) ip = new PsPoint(0, 0, 0);
                            // polygon vertices (plate outline) - may be empty for non-rect plates
                            string poly = "";
                            try
                            {
                                PsPolygon pg = new PsPolygon();
                                pl.GetPolygon(pg);
                                int cnt = 0;
                                try { cnt = pg.Count; } catch { cnt = 0; }
                                StringBuilder pv = new StringBuilder();
                                for (int i = 0; i < cnt && i < 12; i++)
                                {
                                    try
                                    {
                                        PsPoint v = new PsPoint(0, 0, 0);
                                        pg.getVertexAsPoint(i, v);
                                        if (pv.Length > 0) pv.Append(';');
                                        pv.Append(F(v.x)).Append(',').Append(F(v.y)).Append(',').Append(F(v.z));
                                    }
                                    catch { break; }
                                }
                                poly = pv.ToString();
                            }
                            catch { }
                            sb.Append("PLATE\t").Append(hnd).Append('\t')
                              .Append(F(SafeD(delegate() { return pl.Length; }))).Append('\t')
                              .Append(F(SafeD(delegate() { return pl.Width; }))).Append('\t')
                              .Append(F(SafeD(delegate() { return pl.Height; }))).Append('\t')
                              .Append(F(ip.x)).Append(',').Append(F(ip.y)).Append(',').Append(F(ip.z)).Append('\t')
                              .Append(poly).Append('\t')
                              .Append(Safe(SafeS(delegate() { return pl.Material; }))).Append('\t')
                              .Append(Safe(SafeS(delegate() { return pl.Name; }))).Append('\t')
                              .Append(cls).AppendLine();
                            nPlate++; continue;
                        }

                        PsBolt bo = o as PsBolt;
                        if (bo != null)
                        {
                            PsPoint ip = null;
                            try { ip = bo.InsertPoint; } catch { }
                            if (ip == null) ip = new PsPoint(0, 0, 0);
                            sb.Append("BOLT\t").Append(hnd).Append('\t')
                              .Append(F(SafeD(delegate() { return bo.Diameter; }))).Append('\t')
                              .Append(Safe(SafeS(delegate() { return bo.BoltStyleName; }))).Append('\t')
                              .Append(SafeI(delegate() { return bo.Count; })).Append('\t')
                              .Append(F(SafeD(delegate() { return bo.Length; }))).Append('\t')
                              .Append(F(ip.x)).Append(',').Append(F(ip.y)).Append(',').Append(F(ip.z)).Append('\t')
                              .Append(Safe(SafeS(delegate() { return bo.Name; }))).Append('\t')
                              .Append(cls).AppendLine();
                            nBolt++; continue;
                        }

                        // not a ProSteel semantic object we model: record class only
                        // v32: OTHER carried NO coordinates at all. 126 rows in the lesson-5 model came
                        // out blind, 104 of them Ks_VolBody -- every anchor the exam was graded on --
                        // plus 10 Ks_BendShape. Emit centre + extents + ECS so the geometric gate and
                        // the synthetic eyes can see them.  Columns: OTHER hnd cls layer ctr ext ecs
                        Entity e = o as Entity;
                        string oCtr0 = "", oExt0 = ExtStr(o), oEcs0 = EcsStr(o);
                        try
                        {
                            if (e != null)
                            {
                                Extents3d ox0 = e.GeometricExtents;
                                oCtr0 = F((ox0.MinPoint.X + ox0.MaxPoint.X) / 2.0) + "," +
                                         F((ox0.MinPoint.Y + ox0.MaxPoint.Y) / 2.0) + "," +
                                         F((ox0.MinPoint.Z + ox0.MaxPoint.Z) / 2.0);
                            }
                        }
                        catch { }
                        sb.Append("OTHER\t").Append(hnd).Append('\t').Append(cls).Append('\t')
                          .Append(e != null ? e.Layer : "").Append('\t')
                          .Append(oCtr0).Append('\t').Append(oExt0).Append('\t')
                          .Append(oEcs0).AppendLine();
                        nOther++;
                    }
                    catch (System.Exception ex)
                    {
                        nErr++;
                        sb.Append("ERR\t").Append(hnd).Append('\t').Append(cls).Append('\t')
                          .Append(ex.Message.Replace('\t', ' ').Replace('\n', ' ')).AppendLine();
                    }
                }
                tr.Commit();
            }
            File.WriteAllText(Path.Combine(Dir, outName), sb.ToString(), Encoding.UTF8);
            Result("EB_OK dumpmodel shapes=" + nShape + " plates=" + nPlate + " bolts=" + nBolt
                 + " other=" + nOther + " err=" + nErr + " -> " + outName);
        }

        // --- tiny helpers so one bad property never kills the whole dump ---
        delegate double DGet(); delegate string SGet(); delegate int IGet();
        static double SafeD(DGet f) { try { return f(); } catch { return 0; } }
        static string SafeS(SGet f) { try { return f(); } catch { return ""; } }
        static int SafeI(IGet f) { try { return f(); } catch { return 0; } }
        static string Safe(string s) { return s == null ? "" : s.Replace('\t', ' ').Replace('\n', ' '); }
        static string One(string s) { string r = Safe(s).Replace('\r', ' '); return r.Length > 60 ? r.Substring(0, 60) : r; }
        static string F(double d) { return d.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture); }


        // op=dumpfull [out=eb_full.txt]
        // COMPLETE reader.
        //  SHAPE rows add: rotation about the member axis, insert offsets,
        //                  length addition, mirror flag, ECS axes.
        //  PLATE/BOLT rows use ONLY AutoCAD Entity geometry (GeometricExtents):
        //  ProSteel native property access on these objects is unsafe here
        //  (v9 -> NullReferenceException, v10 reflection -> fatal abort).
        void DumpFull(Dictionary<string, string> kv)
        {
            string outName = Get(kv, "out", "eb_full.txt");
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            StringBuilder sb = new StringBuilder();
            int nS = 0, nP = 0, nB = 0, nO = 0, nE = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    string cls = (id.ObjectClass != null ? id.ObjectClass.Name : "?");
                    string hnd = id.Handle.ToString();
                    try
                    {
                        DBObject o = tr.GetObject(id, OpenMode.ForRead);

                        // ---------- shapes: full typed read (proven safe) ----------
                        PsShape sh = o as PsShape;
                        if (sh != null)
                        {
                            PsPoint a = new PsPoint(0, 0, 0), b = new PsPoint(0, 0, 0);
                            try { sh.GetMidLine(a, b); } catch { }
                            // rotation: derive from the shape's ECS Y axis projected
                            // perpendicular to the member axis
                            double rot = 0; string ecs = "";
                            try
                            {
                                Matrix3d m = sh.Ecs;
                                Vector3d ex = m.CoordinateSystem3d.Xaxis;
                                Vector3d ey = m.CoordinateSystem3d.Yaxis;
                                Vector3d ez = m.CoordinateSystem3d.Zaxis;
                                ecs = V(ex) + ";" + V(ey) + ";" + V(ez);
                                Vector3d axis = new Vector3d(b.x - a.x, b.y - a.y, b.z - a.z);
                                if (axis.Length > 1e-6)
                                {
                                    axis = axis.GetNormal();
                                    // reference "up" perpendicular to the axis
                                    Vector3d up = Math.Abs(axis.Z) > 0.9
                                        ? new Vector3d(0, 1, 0) : new Vector3d(0, 0, 1);
                                    Vector3d r0 = axis.CrossProduct(up);
                                    if (r0.Length > 1e-6)
                                    {
                                        r0 = r0.GetNormal();
                                        Vector3d u0 = r0.CrossProduct(axis).GetNormal();
                                        // the profile's local Y within the section plane
                                        Vector3d py = ey - axis * ey.DotProduct(axis);
                                        if (py.Length > 1e-6)
                                        {
                                            py = py.GetNormal();
                                            rot = Math.Atan2(py.DotProduct(r0), py.DotProduct(u0)) * 180.0 / Math.PI;
                                        }
                                    }
                                }
                            }
                            catch { }
                            sb.Append("SHAPE\t").Append(hnd).Append('\t')
                              .Append(Safe(sh.CrossSectionName)).Append('\t')
                              .Append(Safe(sh.CrossSectionCatalog)).Append('\t')
                              .Append(F(a.x)).Append(',').Append(F(a.y)).Append(',').Append(F(a.z)).Append('\t')
                              .Append(F(b.x)).Append(',').Append(F(b.y)).Append(',').Append(F(b.z)).Append('\t')
                              .Append(F(SafeD(delegate() { return sh.Length; }))).Append('\t')
                              .Append(Safe(SafeS(delegate() { return sh.Material; }))).Append('\t')
                              .Append(Safe(SafeS(delegate() { return sh.Name; }))).Append('\t')
                              .Append(F(rot)).Append('\t')
                              .Append(F(SafeD(delegate() { return sh.InsertOffsetX; }))).Append(',')
                              .Append(F(SafeD(delegate() { return sh.InsertOffsetY; }))).Append('\t')
                              .Append(F(SafeD(delegate() { return sh.LengthAddition; }))).Append('\t')
                              .Append(SafeB(delegate() { return sh.MirrorFlag; })).Append('\t')
                              .Append(ecs).Append('\t')
                              .Append(cls).AppendLine();
                            nS++; continue;
                        }

                        // ---------- plates / bolts: geometry ONLY (safe path) ----------
                        bool isPlate = cls.IndexOf("Plate", StringComparison.OrdinalIgnoreCase) >= 0;
                        bool isBolt = cls.IndexOf("Bolt", StringComparison.OrdinalIgnoreCase) >= 0;
                        if (isPlate || isBolt)
                        {
                            Entity en = o as Entity;
                            string ext = "", ctr = "", dims = "", lay = "";
                            if (en != null)
                            {
                                try { lay = en.Layer; } catch { }
                                try
                                {
                                    Extents3d ex2 = en.GeometricExtents;
                                    Point3d mn = ex2.MinPoint, mx = ex2.MaxPoint;
                                    ext = F(mn.X) + "," + F(mn.Y) + "," + F(mn.Z) + ";" +
                                          F(mx.X) + "," + F(mx.Y) + "," + F(mx.Z);
                                    ctr = F((mn.X + mx.X) / 2) + "," + F((mn.Y + mx.Y) / 2) + "," + F((mn.Z + mx.Z) / 2);
                                    dims = F(mx.X - mn.X) + "," + F(mx.Y - mn.Y) + "," + F(mx.Z - mn.Z);
                                }
                                catch { }
                            }
                            sb.Append(isPlate ? "PLATE\t" : "BOLT\t").Append(hnd).Append('\t')
                              .Append(ctr).Append('\t').Append(dims).Append('\t')
                              .Append(ext).Append('\t').Append(lay).Append('\t')
                              .Append(cls).AppendLine();
                            if (isPlate) nP++; else nB++;
                            continue;
                        }

                        // v32: OTHER carried NO coordinates at all. 126 rows in the lesson-5 model came
                        // out blind, 104 of them Ks_VolBody -- every anchor the exam was graded on --
                        // plus 10 Ks_BendShape. Emit centre + extents + ECS so the geometric gate and
                        // the synthetic eyes can see them.  Columns: OTHER hnd cls layer ctr ext ecs
                        Entity e3 = o as Entity;
                        string oCtr1 = "", oExt1 = ExtStr(o), oEcs1 = EcsStr(o);
                        try
                        {
                            if (e3 != null)
                            {
                                Extents3d ox1 = e3.GeometricExtents;
                                oCtr1 = F((ox1.MinPoint.X + ox1.MaxPoint.X) / 2.0) + "," +
                                         F((ox1.MinPoint.Y + ox1.MaxPoint.Y) / 2.0) + "," +
                                         F((ox1.MinPoint.Z + ox1.MaxPoint.Z) / 2.0);
                            }
                        }
                        catch { }
                        sb.Append("OTHER\t").Append(hnd).Append('\t').Append(cls).Append('\t')
                          .Append(e3 != null ? e3.Layer : "").Append('\t')
                          .Append(oCtr1).Append('\t').Append(oExt1).Append('\t')
                          .Append(oEcs1).AppendLine();
                        nO++;
                    }
                    catch (System.Exception ex)
                    {
                        nE++;
                        sb.Append("ERR\t").Append(hnd).Append('\t').Append(cls).Append('\t')
                          .Append(Safe(ex.Message)).AppendLine();
                    }
                }
                tr.Commit();
            }
            File.WriteAllText(Path.Combine(Dir, outName), sb.ToString(), Encoding.UTF8);
            Result("EB_OK dumpfull shapes=" + nS + " plates=" + nP + " bolts=" + nB
                 + " other=" + nO + " err=" + nE + " -> " + outName);
        }

        delegate bool BGet2();
        static string SafeB(BGet2 f) { try { return f() ? "1" : "0"; } catch { return "?"; } }
        static string V(Vector3d v) { return F(v.X) + "/" + F(v.Y) + "/" + F(v.Z); }


        // ---- verified attribute application (never report the input) ----
        static ObjectId OidOf(string handleHex)
        {
            Document dm = Application.DocumentManager.MdiActiveDocument;
            long hh = Convert.ToInt64(handleHex, 16);
            return dm.Database.GetObjectId(false, new Handle(hh), 0);
        }

        // Tries several mirror strategies, RE-READS the flag after each, and
        // returns what actually stuck: "1:<strategy>" or "0:<diag>".
        static string ApplyMirrorVerified(string handle)
        {
            string diag = "";
            for (int strat = 0; strat < 2; strat++)  // MirrorFlag is read-only (compiler-confirmed)
            {
                try
                {
                    Document dm = Application.DocumentManager.MdiActiveDocument;
                    using (Transaction t = dm.Database.TransactionManager.StartTransaction())
                    {
                        PsShape ns = t.GetObject(OidOf(handle), OpenMode.ForWrite) as PsShape;
                        if (ns == null) { t.Abort(); return "0:notPsShape"; }
                        if (strat == 0) ns.SetShapeMirror();
                        else ns.YMirrorFlag = true;
                        // force geometry recalculation before commit
                        try { PsPoint a = new PsPoint(0,0,0), b = new PsPoint(0,0,0); ns.GetMidLine(a, b); } catch { }
                        t.Commit();
                    }
                }
                catch (System.Exception ex) { diag += "s" + strat + ":" + One(ex.Message) + ";"; continue; }
                // READ BACK — the only thing we trust
                try
                {
                    Document dm2 = Application.DocumentManager.MdiActiveDocument;
                    using (Transaction t2 = dm2.Database.TransactionManager.StartTransaction())
                    {
                        PsShape rs = t2.GetObject(OidOf(handle), OpenMode.ForRead) as PsShape;
                        bool got = false, gotY = false;
                        // MirrorFlag ONLY is the state the source model carries.
                        if (rs != null) { try { got = rs.MirrorFlag; } catch { } }
                        if (rs != null) { try { gotY = rs.YMirrorFlag; } catch { } }
                        t2.Commit();
                        diag += "s" + strat + "->M" + (got ? "1" : "0") + "Y" + (gotY ? "1" : "0") + ";";
                        if (got) return "1:s" + strat;
                    }
                }
                catch (System.Exception ex2) { diag += "rb" + strat + ":" + One(ex2.Message) + ";"; }
            }
            return "0:" + (diag.Length > 0 ? diag : "noStrategyStuck");
        }

        static string ApplyLayer(string handle, string layer)
        {
            try
            {
                Document dm = Application.DocumentManager.MdiActiveDocument;
                Database db = dm.Database;
                using (Transaction t = db.TransactionManager.StartTransaction())
                {
                    LayerTable lt = (LayerTable)t.GetObject(db.LayerTableId, OpenMode.ForWrite);
                    if (!lt.Has(layer))
                    {
                        LayerTableRecord ltr = new LayerTableRecord();
                        ltr.Name = layer;
                        lt.Add(ltr);
                        t.AddNewlyCreatedDBObject(ltr, true);
                    }
                    Entity e = t.GetObject(OidOf(handle), OpenMode.ForWrite) as Entity;
                    if (e == null) { t.Abort(); return "0:notEntity"; }
                    e.Layer = layer;
                    t.Commit();
                }
                // verify
                using (Transaction t2 = db.TransactionManager.StartTransaction())
                {
                    Entity e2 = t2.GetObject(OidOf(handle), OpenMode.ForRead) as Entity;
                    string got = e2 != null ? e2.Layer : "";
                    t2.Commit();
                    return (got == layer ? "1:" : "0:") + got;
                }
            }
            catch (System.Exception ex) { return "0:" + One(ex.Message); }
        }

        static string ApplyBoltLength(string handle, double len)
        {
            try
            {
                Document dm = Application.DocumentManager.MdiActiveDocument;
                using (Transaction t = dm.Database.TransactionManager.StartTransaction())
                {
                    PsBolt b = t.GetObject(OidOf(handle), OpenMode.ForWrite) as PsBolt;
                    if (b == null) { t.Abort(); return "0:notPsBolt"; }
                    b.Length = len;
                    t.Commit();
                }
                using (Transaction t2 = dm.Database.TransactionManager.StartTransaction())
                {
                    PsBolt r = t2.GetObject(OidOf(handle), OpenMode.ForRead) as PsBolt;
                    double got = 0; if (r != null) { try { got = r.Length; } catch { } }
                    t2.Commit();
                    return (Math.Abs(got - len) < 1.0 ? "1:" : "0:") + F(got);
                }
            }
            catch (System.Exception ex) { return "0:" + One(ex.Message); }
        }

        // op=setlayer handle=XX layer=YY
        void SetLayer(Dictionary<string, string> kv)
        {
            string h = Get(kv, "handle", ""), l = Get(kv, "layer", "");
            if (h.Length == 0 || l.Length == 0) { Result("EB_ERR setlayer needs handle+layer"); return; }
            Result("EB_OK setlayer " + ApplyLayer(h, l));
        }

        // op=dumpfull2 [out=eb_full2.txt] — adds ECS to plates/bolts, InsertPoint+Layer to shapes
        void DumpFull2(Dictionary<string, string> kv)
        {
            string outName = Get(kv, "out", "eb_full2.txt");
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            StringBuilder sb = new StringBuilder();
            int nS = 0, nP = 0, nB = 0, nO = 0, nE = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    string cls = (id.ObjectClass != null ? id.ObjectClass.Name : "?");
                    string hnd = id.Handle.ToString();
                    try
                    {
                        DBObject o = tr.GetObject(id, OpenMode.ForRead);
                        PsShape sh = o as PsShape;
                        if (sh != null)
                        {
                            PsPoint a = new PsPoint(0,0,0), b = new PsPoint(0,0,0);
                            try { sh.GetMidLine(a, b); } catch { }
                            string ecs = EcsStr(o);
                            string ip = "";
                            try { PsPoint q = sh.InsertPoint; if (q != null) ip = F(q.x)+","+F(q.y)+","+F(q.z); } catch { }
                            string lay = ""; Entity se = o as Entity; if (se != null) { try { lay = se.Layer; } catch { } }
                            sb.Append("SHAPE\t").Append(hnd).Append('\t')
                              .Append(Safe(sh.CrossSectionName)).Append('\t')
                              .Append(Safe(sh.CrossSectionCatalog)).Append('\t')
                              .Append(F(a.x)).Append(',').Append(F(a.y)).Append(',').Append(F(a.z)).Append('\t')
                              .Append(F(b.x)).Append(',').Append(F(b.y)).Append(',').Append(F(b.z)).Append('\t')
                              .Append(F(SafeD(delegate() { return sh.Length; }))).Append('\t')
                              .Append(Safe(SafeS(delegate() { return sh.Material; }))).Append('\t')
                              .Append(Safe(SafeS(delegate() { return sh.Name; }))).Append('\t')
                              .Append(F(RotOf(sh, a, b))).Append('\t')
                              .Append(F(SafeD(delegate() { return sh.InsertOffsetX; }))).Append(',')
                              .Append(F(SafeD(delegate() { return sh.InsertOffsetY; }))).Append('\t')
                              .Append(F(SafeD(delegate() { return sh.LengthAddition; }))).Append('\t')
                              .Append(SafeB(delegate() { return sh.MirrorFlag; })).Append('\t')
                              .Append(ecs).Append('\t').Append(ip).Append('\t').Append(Safe(lay)).Append('\t')
                              .Append(ExtStr(o)).Append('\t')
                              .Append(CogStr(sh)).Append('\t')
                              .Append(cls).AppendLine();
                            nS++; continue;
                        }
                        bool isPlate = cls.IndexOf("Plate", StringComparison.OrdinalIgnoreCase) >= 0;
                        bool isBolt = cls.IndexOf("Bolt", StringComparison.OrdinalIgnoreCase) >= 0;
                        if (isPlate || isBolt)
                        {
                            Entity en = o as Entity;
                            string ctr = "", dims = "", lay = "", ecs = EcsStr(o);
                            if (en != null)
                            {
                                try { lay = en.Layer; } catch { }
                                try
                                {
                                    Extents3d ex2 = en.GeometricExtents;
                                    Point3d mn = ex2.MinPoint, mx = ex2.MaxPoint;
                                    ctr = F((mn.X+mx.X)/2)+","+F((mn.Y+mx.Y)/2)+","+F((mn.Z+mx.Z)/2);
                                    dims = F(mx.X-mn.X)+","+F(mx.Y-mn.Y)+","+F(mx.Z-mn.Z);
                                }
                                catch { }
                            }
                            sb.Append(isPlate ? "PLATE\t" : "BOLT\t").Append(hnd).Append('\t')
                              .Append(ctr).Append('\t').Append(dims).Append('\t')
                              .Append(ecs).Append('\t').Append(Safe(lay)).Append('\t').Append(cls).AppendLine();
                            if (isPlate) nP++; else nB++;
                            continue;
                        }
                        // v32: OTHER carried NO coordinates at all. 126 rows in the lesson-5 model came
                        // out blind, 104 of them Ks_VolBody -- every anchor the exam was graded on --
                        // plus 10 Ks_BendShape. Emit centre + extents + ECS so the geometric gate and
                        // the synthetic eyes can see them.  Columns: OTHER hnd cls layer ctr ext ecs
                        Entity e3 = o as Entity;
                        string oCtr2 = "", oExt2 = ExtStr(o), oEcs2 = EcsStr(o);
                        try
                        {
                            if (e3 != null)
                            {
                                Extents3d ox2 = e3.GeometricExtents;
                                oCtr2 = F((ox2.MinPoint.X + ox2.MaxPoint.X) / 2.0) + "," +
                                         F((ox2.MinPoint.Y + ox2.MaxPoint.Y) / 2.0) + "," +
                                         F((ox2.MinPoint.Z + ox2.MaxPoint.Z) / 2.0);
                            }
                        }
                        catch { }
                        sb.Append("OTHER\t").Append(hnd).Append('\t').Append(cls).Append('\t')
                          .Append(e3 != null ? e3.Layer : "").Append('\t')
                          .Append(oCtr2).Append('\t').Append(oExt2).Append('\t')
                          .Append(oEcs2).AppendLine();
                        nO++;
                    }
                    catch (System.Exception ex)
                    {
                        nE++;
                        sb.Append("ERR\t").Append(hnd).Append('\t').Append(cls).Append('\t').Append(Safe(ex.Message)).AppendLine();
                    }
                }
                tr.Commit();
            }
            File.WriteAllText(Path.Combine(Dir, outName), sb.ToString(), Encoding.UTF8);
            Result("EB_OK dumpfull2 shapes=" + nS + " plates=" + nP + " bolts=" + nB + " other=" + nO + " err=" + nE + " -> " + outName);
        }

        // op=clonemodel dx=15000 [maxx=15000]
        // FAITHFUL COPY: deep-clone every model-space entity that sits below maxx and
        // translate the clones by dx. This is what AutoCAD's own COPY does internally,
        // so EVERY attribute survives — mirror flags, insert offsets, layers, material,
        // holes, host relationships, groups — none of which can be reproduced by
        // parametric re-creation (proved: MirrorFlag is read-only, Ecs is identity,
        // InsertPoint/COG return null).
        // ROTATE objects IN PLACE — the software's own rotate, no cloning.
        // op=rotate  handles=a,b,c   (or class= / layer= / box=)  rot=deg
        //            [axis=x|y|z]  [about=self|x,y,z]
        // about=self rotates each object around its own centre, which is what
        // "rotate the base plate 90 degrees" means.
        void RotateObjs(Dictionary<string, string> kv)
        {
            double rot = double.Parse(Get(kv, "rot", "90"),
                System.Globalization.CultureInfo.InvariantCulture);
            string axs = Get(kv, "axis", "z").ToLower();
            Vector3d av = axs == "x" ? Vector3d.XAxis
                        : axs == "y" ? Vector3d.YAxis : Vector3d.ZAxis;
            string want = Get(kv, "handles", "");
            string cls = Get(kv, "class", "");
            string lay = Get(kv, "layer", "");
            string nameLike = Get(kv, "name", "");
            string aboutS = Get(kv, "about", "self");
            double[] box = Nums(Get(kv, "box", "-1e12,-1e12,1e12,1e12"));

            var pick = new List<ObjectId>();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            var wanted = new List<string>();
            foreach (string w in want.Split(new char[] { ',', ';' },
                     StringSplitOptions.RemoveEmptyEntries))
                wanted.Add(w.Trim().ToUpper());

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    try
                    {
                        string cn = id.ObjectClass != null ? id.ObjectClass.Name : "";
                        if (wanted.Count > 0)
                        {
                            if (!wanted.Contains(id.Handle.ToString().ToUpper())) continue;
                        }
                        else
                        {
                            if (cls.Length > 0 && cn.IndexOf(cls, StringComparison.OrdinalIgnoreCase) < 0) continue;
                            Entity e0 = tr.GetObject(id, OpenMode.ForRead) as Entity;
                            if (e0 == null) continue;
                            if (lay.Length > 0 && e0.Layer != lay) continue;
                            if (nameLike.Length > 0)
                            {
                                PsShape sh0 = e0 as PsShape;
                                string nm = "";
                                if (sh0 != null) { try { nm = sh0.CrossSectionName; } catch { } }
                                if (nm.IndexOf(nameLike, StringComparison.OrdinalIgnoreCase) < 0) continue;
                            }
                            Extents3d ex0;
                            try { ex0 = e0.GeometricExtents; }
                            catch { continue; }
                            double cxx = (ex0.MinPoint.X + ex0.MaxPoint.X) / 2.0;
                            double cyy = (ex0.MinPoint.Y + ex0.MaxPoint.Y) / 2.0;
                            if (cxx < box[0] || cxx > box[2] || cyy < box[1] || cyy > box[3]) continue;
                        }
                        pick.Add(id);
                    }
                    catch { }
                }
                tr.Commit();
            }
            if (pick.Count == 0) { Result("EB_ERR rotate: nothing selected"); return; }

            int done = 0, failed = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in pick)
                {
                    try
                    {
                        Entity e = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                        if (e == null) { failed++; continue; }
                        Point3d about;
                        if (aboutS == "self")
                        {
                            Extents3d x = e.GeometricExtents;
                            about = new Point3d((x.MinPoint.X + x.MaxPoint.X) / 2.0,
                                                (x.MinPoint.Y + x.MaxPoint.Y) / 2.0,
                                                (x.MinPoint.Z + x.MaxPoint.Z) / 2.0);
                        }
                        else
                        {
                            double[] a = Nums(aboutS);
                            about = new Point3d(a[0], a.Length > 1 ? a[1] : 0,
                                                a.Length > 2 ? a[2] : 0);
                        }
                        e.TransformBy(Matrix3d.Rotation(rot * Math.PI / 180.0, av, about));
                        done++;
                    }
                    catch { failed++; }
                }
                tr.Commit();
            }
            Result("EB_OK rotate selected=" + pick.Count + " rotated=" + done
                 + " failed=" + failed + " rot=" + F(rot) + " axis=" + axs
                 + " about=" + aboutS);
        }

        // =====================================================================
        //  REPLICATE — build the detail once, then copy it everywhere.
        //  Amir's principle: "I modelled once and then replicated — that is the
        //  whole principle." This is the software's own copy (DeepCloneObjects),
        //  so every clone keeps its holes, connections, anchors and layers, plus
        //  a rotation about Z for details that serve a different face.
        //  op=replicate  box=x0,y0,x1,y1  to=x,y  [about=x,y rot=deg]
        // =====================================================================
        void Replicate(Dictionary<string, string> kv)
        {
            double[] box = Nums(Get(kv, "box", "0,0,0,0"));
            double[] to = Nums(Get(kv, "to", "0,0"));
            double rot = double.Parse(Get(kv, "rot", "0"),
                System.Globalization.CultureInfo.InvariantCulture);
            double[] ab = Nums(Get(kv, "about", Get(kv, "to", "0,0")));
            double dx = to[0], dy = to.Length > 1 ? to[1] : 0.0;

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            ObjectIdCollection src = new ObjectIdCollection();
            int picked = 0;

            // An EXPLICIT handle list is the safe way to replicate a detail: a box
            // re-selects whatever now sits inside it, so earlier copies get cloned
            // again and the counts snowball. Prefer handles= over box=.
            string want = Get(kv, "handles", "");
            var wanted = new List<string>();
            foreach (string w in want.Split(new char[] { ',', ';' },
                     StringSplitOptions.RemoveEmptyEntries))
                wanted.Add(w.Trim().ToUpper());

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (wanted.Count > 0)
                {
                    foreach (string hx in wanted)
                    {
                        try
                        {
                            ObjectId id = db.GetObjectId(false,
                                new Handle(Convert.ToInt64(hx, 16)), 0);
                            if (id.IsNull || id.IsErased) continue;
                            src.Add(id);
                            picked++;
                        }
                        catch { }
                    }
                }
                else
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                    foreach (ObjectId id in ms)
                    {
                        try
                        {
                            Entity e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                            if (e == null) continue;
                            // concrete illustration and managers are never replicated
                            string cn = id.ObjectClass != null ? id.ObjectClass.Name : "";
                            if (cn.IndexOf("3dSolid", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                            if (cn.IndexOf("RebarManager", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                            Extents3d ex;
                            try { ex = e.GeometricExtents; }
                            catch { continue; }
                            double cxx = (ex.MinPoint.X + ex.MaxPoint.X) / 2.0;
                            double cyy = (ex.MinPoint.Y + ex.MaxPoint.Y) / 2.0;
                            if (cxx < box[0] || cxx > box[2] || cyy < box[1] || cyy > box[3]) continue;
                            src.Add(id);
                            picked++;
                        }
                        catch { }
                    }
                }
                tr.Commit();
            }
            if (picked == 0) { Result("EB_ERR replicate: nothing selected"); return; }

            IdMapping map = new IdMapping();
            try
            {
                using (Transaction tr2 = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt2 = (BlockTable)tr2.GetObject(db.BlockTableId, OpenMode.ForRead);
                    db.DeepCloneObjects(src, bt2[BlockTableRecord.ModelSpace], map, false);
                    tr2.Commit();
                }
            }
            catch (System.Exception ex)
            { Result("EB_ERR replicate deepclone: " + One(ex.Message)); return; }

            // move, then rotate about any axis through a given point — Z for a
            // detail serving another face, X or Y to lay a vertical anchor down
            double dz = to.Length > 2 ? to[2] : 0.0;
            Matrix3d m = Matrix3d.Displacement(new Vector3d(dx, dy, dz));
            if (Math.Abs(rot) > 0.001)
            {
                string axs = Get(kv, "axis", "z").ToLower();
                Vector3d av = axs == "x" ? Vector3d.XAxis
                            : axs == "y" ? Vector3d.YAxis : Vector3d.ZAxis;
                Point3d about = new Point3d(ab[0],
                    ab.Length > 1 ? ab[1] : 0.0, ab.Length > 2 ? ab[2] : 0.0);
                m = Matrix3d.Rotation(rot * Math.PI / 180.0, av, about) * m;
            }
            int moved = 0, failed = 0;
            try
            {
                using (Transaction tr3 = db.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId sid in src)
                    {
                        if (!map.Contains(sid)) continue;
                        IdPair pr = map[sid];
                        if (!pr.IsCloned) continue;
                        try
                        {
                            Entity ce = tr3.GetObject(pr.Value, OpenMode.ForWrite) as Entity;
                            if (ce == null) { failed++; continue; }
                            ce.TransformBy(m);
                            moved++;
                        }
                        catch { failed++; }
                    }
                    tr3.Commit();
                }
            }
            catch (System.Exception ex)
            { Result("EB_PARTIAL replicate transform: " + One(ex.Message)); return; }

            Result("EB_OK replicate picked=" + picked + " cloned=" + moved
                 + " failed=" + failed + " to=" + F(dx) + "," + F(dy)
                 + " rot=" + F(rot));
        }

        void CloneModel(Dictionary<string, string> kv)
        {
            double dx = double.Parse(Get(kv, "dx", "15000"), System.Globalization.CultureInfo.InvariantCulture);
            double maxx = double.Parse(Get(kv, "maxx", "15000"), System.Globalization.CultureInfo.InvariantCulture);
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            int picked = 0, cloned = 0, moved = 0, skipped = 0;
            string err = "";

            ObjectIdCollection src = new ObjectIdCollection();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    try
                    {
                        Entity e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                        if (e == null) { skipped++; continue; }
                        double px;
                        try { px = e.GeometricExtents.MinPoint.X; }
                        catch { skipped++; continue; }      // no extents -> not real geometry
                        if (px >= maxx) { skipped++; continue; }
                        src.Add(id);
                        picked++;
                    }
                    catch { skipped++; }
                }
                tr.Commit();
            }
            if (picked == 0) { Result("EB_ERR clonemodel: nothing to clone below x=" + maxx); return; }

            IdMapping map = new IdMapping();
            try
            {
                using (Transaction tr2 = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt2 = (BlockTable)tr2.GetObject(db.BlockTableId, OpenMode.ForRead);
                    ObjectId msId = bt2[BlockTableRecord.ModelSpace];
                    db.DeepCloneObjects(src, msId, map, false);
                    tr2.Commit();
                }
            }
            catch (System.Exception ex) { Result("EB_ERR clonemodel deepclone: " + One(ex.Message)); return; }

            // translate every clone
            Matrix3d disp = Matrix3d.Displacement(new Vector3d(dx, 0, 0));
            try
            {
                using (Transaction tr3 = db.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId sid in src)
                    {
                        if (!map.Contains(sid)) continue;
                        IdPair pr = map[sid];
                        if (!pr.IsCloned) continue;
                        cloned++;
                        try
                        {
                            Entity ce = tr3.GetObject(pr.Value, OpenMode.ForWrite) as Entity;
                            if (ce == null) continue;
                            ce.TransformBy(disp);
                            moved++;
                        }
                        catch (System.Exception ex2) { if (err.Length < 90) err += One(ex2.Message) + ";"; }
                    }
                    tr3.Commit();
                }
            }
            catch (System.Exception ex3) { Result("EB_ERR clonemodel move: " + One(ex3.Message)); return; }

            Result("EB_OK clonemodel picked=" + picked + " cloned=" + cloned + " moved=" + moved
                 + " skipped=" + skipped + (err.Length > 0 ? " warn=" + err : ""));
        }

        // The COG of an asymmetric section is OFF-CENTRE, so it is the only cheap
        // geometric probe that can tell a mirrored angle from a plain one — the
        // bounding box of an L inside a 60x60 envelope is identical either way.
        static string CogStr(PsShape sh)
        {
            try
            {
                PsPoint p = null;
                try { p = sh.COGPoint; } catch { }
                if (p == null) { try { p = sh.WeightCenter; } catch { } }
                if (p == null) return "";
                return F(p.x) + "," + F(p.y) + "," + F(p.z);
            }
            catch { return ""; }
        }

        // ground truth of where the steel actually is (AutoCAD level, always safe)
        static string ExtStr(DBObject o)
        {
            try
            {
                Entity e = o as Entity;
                if (e == null) return "";
                Extents3d x = e.GeometricExtents;
                return F(x.MinPoint.X) + "," + F(x.MinPoint.Y) + "," + F(x.MinPoint.Z) + ";" +
                       F(x.MaxPoint.X) + "," + F(x.MaxPoint.Y) + "," + F(x.MaxPoint.Z);
            }
            catch { return ""; }
        }

        static string EcsStr(DBObject o)
        {
            try
            {
                Entity e = o as Entity;
                if (e == null) return "";
                Matrix3d m = e.Ecs;
                CoordinateSystem3d cs = m.CoordinateSystem3d;
                return V(cs.Xaxis) + ";" + V(cs.Yaxis) + ";" + V(cs.Zaxis);
            }
            catch { return ""; }
        }

        static double RotOf(PsShape sh, PsPoint a, PsPoint b)
        {
            try
            {
                Matrix3d m = sh.Ecs;
                Vector3d ey = m.CoordinateSystem3d.Yaxis;
                Vector3d axis = new Vector3d(b.x - a.x, b.y - a.y, b.z - a.z);
                if (axis.Length < 1e-6) return 0;
                axis = axis.GetNormal();
                Vector3d up = Math.Abs(axis.Z) > 0.9 ? new Vector3d(0,1,0) : new Vector3d(0,0,1);
                Vector3d r0 = axis.CrossProduct(up);
                if (r0.Length < 1e-6) return 0;
                r0 = r0.GetNormal();
                Vector3d u0 = r0.CrossProduct(axis).GetNormal();
                Vector3d py = ey - axis * ey.DotProduct(axis);
                if (py.Length < 1e-6) return 0;
                py = py.GetNormal();
                return Math.Atan2(py.DotProduct(r0), py.DotProduct(u0)) * 180.0 / Math.PI;
            }
            catch { return 0; }
        }

        void ListModel()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            StringBuilder sb = new StringBuilder();
            int n = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    n++;
                    Entity e = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    sb.AppendLine(id.Handle.ToString() + "|" +
                        (id.ObjectClass != null ? id.ObjectClass.Name : "?") + "|" +
                        (e != null ? e.Layer : ""));
                }
                tr.Commit();
            }
            File.WriteAllText(Path.Combine(Dir, "eb_list.txt"), sb.ToString(), Encoding.UTF8);
            Result("EB_OK list " + n + " entities -> eb_list.txt");
        }

        // =====================================================================
        //  v18 — THE CONNECTION LAYER: real holes, real contours, real drilling
        //  Amir's rule: a bolt passing through steel WITHOUT a modelled hole is a
        //  critical error. So we must be able to (a) READ holes to verify and
        //  (b) DRILL holes to fix. Both work off the object id — no PsPlate cast.
        // =====================================================================

        // Enum values are not in our reflection dump (names only). This reads the
        // enum METADATA (types, never live Ks_* objects) so we can pass the right
        // ints instead of guessing.
        void EnumDump(Dictionary<string, string> kv)
        {
            string[] names = { "LongHoleMode", "HoleType", "DrillType", "HoleBoltType",
                "PsOpenMode", "PositionSelection", "CoordSystem", "VerticalPosition",
                "HoleGeometrie", "DrillAcuracy", "ModificationType", "PolyStatus" };
            StringBuilder sb = new StringBuilder();
            Assembly asm = typeof(PsPoint).Assembly;
            sb.AppendLine("ASSEMBLY " + asm.FullName);
            foreach (string n in names)
            {
                Type t = null;
                try { t = asm.GetType("Bentley.ProStructures." + n); }
                catch { }
                if (t == null) { sb.AppendLine("ENUM " + n + " NOTFOUND"); continue; }
                try
                {
                    string[] mem = Enum.GetNames(t);
                    Array vals = Enum.GetValues(t);
                    StringBuilder l = new StringBuilder();
                    for (int i = 0; i < mem.Length; i++)
                    {
                        if (i > 0) l.Append(", ");
                        l.Append(mem[i] + "=" + Convert.ToInt64(vals.GetValue(i)));
                    }
                    sb.AppendLine("ENUM " + n + " : " + l.ToString());
                }
                catch (System.Exception ex) { sb.AppendLine("ENUM " + n + " ERR " + ex.Message); }
            }
            // optional: dump the method surface of a type family (e.g. types=Drill)
            string pat = Get(kv, "types", "");
            if (pat.Length > 0)
            {
                foreach (Type t in asm.GetExportedTypes())
                {
                    if (t.Name.IndexOf(pat, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    sb.AppendLine("=== TYPE " + t.FullName);
                    try
                    {
                        foreach (MethodInfo mi in t.GetMethods(BindingFlags.Public |
                            BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                        {
                            StringBuilder ps = new StringBuilder();
                            foreach (ParameterInfo pi in mi.GetParameters())
                            {
                                if (ps.Length > 0) ps.Append(", ");
                                ps.Append(pi.ParameterType.Name + (pi.IsOut ? "& out" : "") + " " + pi.Name);
                            }
                            sb.AppendLine("  METH " + mi.ReturnType.Name + " " + mi.Name + "(" + ps + ")");
                        }
                    }
                    catch { }
                }
            }
            File.WriteAllText(Path.Combine(Dir, "eb_enums.txt"), sb.ToString(), Encoding.UTF8);
            Result("EB_OK enumdump -> eb_enums.txt");
        }

        // Read the REAL holes of one object. This is the objective verification
        // instrument the audit demanded (a screenshot cannot prove a hole).
        static int HolesOf(long oid, int lhm, StringBuilder sb, string tag, out string err)
        {
            err = "";
            try
            {
                PsSingleHoleArray arr = new PsSingleHoleArray(oid, (LongHoleMode)lhm, false, false, false);
                int cnt = arr.Count;
                for (int i = 0; i < cnt; i++)
                {
                    PsPoint s = new PsPoint(0, 0, 0), e = new PsPoint(0, 0, 0);
                    double dm = 0;
                    try { arr.getHole(i, s, e, ref dm); }
                    catch { }
                    double maxlen = 0;
                    try { arr.getMaximalLength(i, ref maxlen); } catch { }
                    string slot = "?";
                    try { slot = arr.getFromSlottedHole(i) ? "1" : "0"; } catch { }
                    if (sb != null)
                        sb.AppendLine("HOLE\t" + tag + "\t" + i + "\t" +
                            F(s.x) + "," + F(s.y) + "," + F(s.z) + "\t" +
                            F(e.x) + "," + F(e.y) + "," + F(e.z) + "\t" +
                            F(dm) + "\t" + F(maxlen) + "\t" + slot);
                }
                return cnt;
            }
            catch (System.Exception ex) { err = ex.Message; return -1; }
        }

        void Holes(Dictionary<string, string> kv)
        {
            string h = Get(kv, "handle", "");
            int lhm = int.Parse(Get(kv, "lhm", "2"));
            long oid = IdFromHandle(h);
            StringBuilder sb = new StringBuilder();
            string err;
            int n = HolesOf(oid, lhm, sb, h, out err);
            File.WriteAllText(Path.Combine(Dir, "eb_holes.txt"), sb.ToString(), Encoding.UTF8);
            if (n < 0) { Result("EB_ERR holes " + h + " : " + err); return; }
            Result("EB_OK holes handle=" + h + " count=" + n + " -> eb_holes.txt");
        }

        // Whole-model hole census: per object, how many holes and of what diameter.
        void DumpHoles(Dictionary<string, string> kv)
        {
            int lhm = int.Parse(Get(kv, "lhm", "2"));
            string outName = Get(kv, "out", "eb_holes_all.txt");
            double maxx = double.Parse(Get(kv, "maxx", "1e9"), System.Globalization.CultureInfo.InvariantCulture);
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            StringBuilder sb = new StringBuilder();
            int objs = 0, withHoles = 0, total = 0, errs = 0, slotted = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    string cls = id.ObjectClass != null ? id.ObjectClass.Name : "?";
                    if (cls.IndexOf("Ks_") < 0) continue;
                    Entity ent = null;
                    try { ent = tr.GetObject(id, OpenMode.ForRead) as Entity; } catch { }
                    if (ent == null) continue;
                    // artefact filter: skip smoke-test zone
                    try { if (ent.GeometricExtents.MinPoint.X >= maxx) continue; } catch { }
                    objs++;
                    string hx = id.Handle.ToString();
                    StringBuilder one = new StringBuilder();
                    string err;
                    int n = HolesOf(id.OldIdPtr.ToInt64(), lhm, one, hx + "\t" + cls + "\t" + ent.Layer, out err);
                    if (n < 0) { errs++; sb.AppendLine("ERR\t" + hx + "\t" + cls + "\t" + One(err)); continue; }
                    sb.AppendLine("OBJ\t" + hx + "\t" + cls + "\t" + ent.Layer + "\t" + n);
                    if (n > 0)
                    {
                        withHoles++; total += n;
                        sb.Append(one.ToString());
                        foreach (string ln in one.ToString().Split('\n'))
                            if (ln.EndsWith("\t1") || ln.EndsWith("\t1\r")) slotted++;
                    }
                }
                tr.Commit();
            }
            File.WriteAllText(Path.Combine(Dir, outName), sb.ToString(), Encoding.UTF8);
            Result("EB_OK dumpholes objs=" + objs + " withholes=" + withHoles + " holes=" + total
                 + " slotted=" + slotted + " err=" + errs + " -> " + outName);
        }

        // Real plate contour (not the bounding box) — this is what tells a rib or a
        // cut gusset from a rectangle.
        static string PolyOf(DBObject o, out int nv, out string rectMode, out string err)
        {
            nv = 0; rectMode = "?"; err = "";
            try
            {
                PsPlate pl = o as PsPlate;
                if (pl == null) { err = "not-PsPlate"; return ""; }
                try { rectMode = pl.RectangleMode ? "1" : "0"; } catch { rectMode = "?"; }
                PsPolygon poly = new PsPolygon();
                pl.GetPolygon(poly);
                nv = poly.Count;
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < nv; i++)
                {
                    PsPoint p = new PsPoint(0, 0, 0);
                    try { poly.getVertexAsPoint(i, p); } catch { }
                    if (i > 0) sb.Append(";");
                    sb.Append(F(p.x) + "," + F(p.y) + "," + F(p.z));
                }
                return sb.ToString();
            }
            catch (System.Exception ex) { err = ex.Message; return ""; }
        }

        void PlatePoly(Dictionary<string, string> kv)
        {
            string h = Get(kv, "handle", "");
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            string line = ""; int nv = 0; string rm = "?", err = "";
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId id = db.GetObjectId(false, new Handle(Convert.ToInt64(h, 16)), 0);
                DBObject o = tr.GetObject(id, OpenMode.ForRead);
                line = PolyOf(o, out nv, out rm, out err);
                tr.Commit();
            }
            if (err.Length > 0 && nv == 0) { Result("EB_ERR platepoly " + h + " : " + err); return; }
            Result("EB_OK platepoly handle=" + h + " verts=" + nv + " rect=" + rm + " pts=" + line);
        }

        // Whole-model contour dump: which plates are NOT rectangles (ribs/gussets).
        void DumpPoly(Dictionary<string, string> kv)
        {
            string outName = Get(kv, "out", "eb_poly.txt");
            double maxx = double.Parse(Get(kv, "maxx", "1e9"), System.Globalization.CultureInfo.InvariantCulture);
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            StringBuilder sb = new StringBuilder();
            int plates = 0, nonRect = 0, gt4 = 0, errs = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    string cls = id.ObjectClass != null ? id.ObjectClass.Name : "?";
                    if (cls.IndexOf("Plate") < 0) continue;
                    DBObject o = null;
                    try { o = tr.GetObject(id, OpenMode.ForRead); } catch { }
                    if (o == null) continue;
                    Entity ent = o as Entity;
                    if (ent != null) { try { if (ent.GeometricExtents.MinPoint.X >= maxx) continue; } catch { } }
                    plates++;
                    int nv; string rm, err;
                    string pts = PolyOf(o, out nv, out rm, out err);
                    if (nv == 0) { errs++; sb.AppendLine("ERR\t" + id.Handle + "\t" + cls + "\t" + One(err)); continue; }
                    if (rm == "0") nonRect++;
                    if (nv > 4) gt4++;
                    sb.AppendLine("POLY\t" + id.Handle + "\t" + cls + "\t" + (ent != null ? ent.Layer : "")
                        + "\t" + nv + "\t" + rm + "\t" + pts);
                }
                tr.Commit();
            }
            File.WriteAllText(Path.Combine(Dir, outName), sb.ToString(), Encoding.UTF8);
            Result("EB_OK dumppoly plates=" + plates + " nonrect=" + nonRect + " verts>4=" + gt4
                 + " err=" + errs + " -> " + outName);
        }

        // DRILL a real hole into a host (plate OR profile). Amir: mandatory wherever
        // a bolt passes. slot>0 makes it an oblong/slotted hole (his field practice).
        void Drill(Dictionary<string, string> kv)
        {
            string hosts = Get(kv, "hosts", Get(kv, "handle", ""));
            PsPoint at = Pt(Get(kv, "at", "0,0,0"));
            double dia = double.Parse(Get(kv, "dia", "19"), System.Globalization.CultureInfo.InvariantCulture);
            double[] nrm = Nums(Get(kv, "n", "0,0,1"));
            double slot = double.Parse(Get(kv, "slot", "0"), System.Globalization.CultureInfo.InvariantCulture);
            bool rotslot = Get(kv, "rotslot", "0") == "1";
            string sInner = Get(kv, "innercontour", "");
            string sPlay = Get(kv, "play", "");
            string sFlange = Get(kv, "flange", "");      // 0=top 1=down 2=both
            string sBoltType = Get(kv, "bolttype", "");  // 0=normal 1=montage 3=in-house
            int htype = int.Parse(Get(kv, "htype", "-1"));
            int drilled = 0, failed = 0;
            StringBuilder rep = new StringBuilder();
            foreach (string hRaw in hosts.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string h = hRaw.Trim();
                if (h.Length == 0) continue;
                try
                {
                    long oid = IdFromHandle(h);
                    PsDrillObject d = new PsDrillObject();
                    d.SetToDefaults();
                    d.SetObjectId(oid);
                    d.SetInsertPoint(at);
                    d.SetNormal(new PsVector(nrm[0], nrm.Length > 1 ? nrm[1] : 0, nrm.Length > 2 ? nrm[2] : 1));
                    if (htype >= 0) { try { d.SetHoleType((HoleType)htype); } catch { } }
                    // v43 -- SLOTTED HOLES, corrected.
                    // The old code called SetHoleStep(slot, dia). SetHoleStep is for STEP
                    // HOLES ("Step Hole" in manual B.14.3: step depth + upper diameter),
                    // NOT for oblong holes. Manual B.14.1 calls the oblong dimension
                    // "Rectangle Hole Axis -- the LENGTH of the rectangle hole axis", and
                    // PsDrillObject has no slot-length setter except SetAxisDistance.
                    // HYPOTHESIS UNDER TEST: SetAxisDistance == Rectangle Hole Axis.
                    // Verified by reading PsSingleHole.FromSlottedHole / MaximalLength back.
                    if (slot > 0)
                    {
                        try { d.SetAxisDistance(slot); } catch { }
                        try { d.SetRotateSlottedHoles(rotslot); } catch { }
                    }
                    // B.14.3: without this a hollow section is drilled on ONE WALL only.
                    if (sInner.Length > 0) { try { d.SetIgnoreInnerContour(sInner == "1"); } catch { } }
                    // B.14.1: the hole clearance. Amir's shop rule is 3 mm; ProSteel default 2.
                    if (sPlay.Length > 0) { try { d.SetHoleWorkloose(double.Parse(sPlay, System.Globalization.CultureInfo.InvariantCulture)); } catch { } }
                    // B.14.1 Flange: upper / lower / BOTH  (kDrillFlangeTop/Down/Both)
                    if (sFlange.Length > 0) { try { d.SetDrillType((DrillType)int.Parse(sFlange)); } catch { } }
                    // shop bolt vs site (montage) bolt -- HoleBoltType, a real fabrication distinction
                    if (sBoltType.Length > 0) { try { d.SetHoleBoltType((HoleBoltType)int.Parse(sBoltType)); } catch { } }
                    d.SetSingleHoleField(dia);
                    int rc = d.Apply();
                    // VERIFY by reading the holes back — never trust Apply()'s return
                    string err;
                    int n = HolesOf(oid, 0, null, h, out err);
                    rep.Append(" " + h + ":rc=" + rc + ",holes=" + n);
                    if (n > 0) drilled++; else failed++;
                }
                catch (System.Exception ex) { failed++; rep.Append(" " + h + ":EX=" + One(ex.Message)); }
            }
            Result((failed == 0 ? "EB_OK" : "EB_PARTIAL") + " drill dia=" + F(dia)
                 + " hosts_ok=" + drilled + " failed=" + failed + rep.ToString());
        }

        // Create a NON-rectangular plate from an explicit contour (ribs, gussets).
        // pts=x,y,z;x,y,z;...  t=thickness  [layer=]
        void PolyPlate(Dictionary<string, string> kv)
        {
            string ptsS = Get(kv, "pts", "");
            double t = double.Parse(Get(kv, "t", "10"), System.Globalization.CultureInfo.InvariantCulture);
            string wantLayer = Get(kv, "layer", "");
            string[] chunks = ptsS.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (chunks.Length < 3) { Result("EB_ERR polyplate needs >=3 pts"); return; }

            string h0, c0; int before = Census(out h0, out c0);
            PsCreatePlate cp = new PsCreatePlate();
            cp.SetToDefaults();
            foreach (string c in chunks) cp.AppendEdgePoint(Pt(c));
            cp.SetThickness(t);
            bool made = false; string exs = "";
            try { made = cp.Create(); } catch (System.Exception ex) { exs = ex.Message; }
            string h1, c1; int after = Census(out h1, out c1);
            if (after <= before)
            {
                Result("EB_ERR polyplate not created (create=" + made + ") " + One(exs));
                return;
            }
            if (wantLayer.Length > 0) ApplyLayer(h1, wantLayer);
            // read the contour back — proof, not an echo of the input
            Document doc = Application.DocumentManager.MdiActiveDocument;
            int nv = 0; string rm = "?", err = "", pts = "";
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                ObjectId id = doc.Database.GetObjectId(false, new Handle(Convert.ToInt64(h1, 16)), 0);
                pts = PolyOf(tr.GetObject(id, OpenMode.ForRead), out nv, out rm, out err);
                tr.Commit();
            }
            Result("EB_OK polyplate handle=" + h1 + " class=" + c1 + " sent=" + chunks.Length
                 + " verts_readback=" + nv + " rect=" + rm + " t=" + F(t) + " pts=" + pts);
        }

        // =====================================================================
        //  v19 — PS PROPERTIES and PS CONNECTION (Amir: "every connection is a
        //  function in the software; you can see all its data").  A connection is
        //  a LOGICAL LINK on a part: it knows its type and its full parameter set
        //  (plate sizes, hole diameters and spacings, welds, bolts).  Reading those
        //  is how you UNDERSTAND a joint; writing them is how you BUILD one.
        // =====================================================================

        static string PropsOf(long oid, out string err)
        {
            // v57: this used REFLECTION to hunt for a loader called loadFrom/getFrom/
            // SetObjectId/load -- none of which exist. The method is readFrom(Int64), so every
            // call returned "no-loader" and the op has been dead for weeks. Written before the
            // API was mapped; kept guessing long after guessing stopped being necessary.
            //
            // And PsObjectProperties turns out to carry 100+ properties, of which this agent
            // was using five. The ones that matter most:
            //   Origin / XAxis / YAxis / ZAxis / InsertMatrix -- the PART COORDINATE SYSTEM,
            //     which is exactly what the manual's Clone warning is about
            //   PaintArea / CutArea -- surface area for painting: a real quotation quantity
            //   Length / Wide / Height / Diameter -- dimensions without any geometry maths
            //   KlemmLen -- grip length; Material, Katalog, Key -- profile identity
            err = "";
            try
            {
                PsObjectProperties pr = new PsObjectProperties();
                int rc = pr.readFrom(oid);          // 0 = eOk. Do NOT treat 0 as failure.
                StringBuilder sb = new StringBuilder();
                sb.Append("rc=" + rc);
                try { sb.Append(" name='" + pr.Name + "'"); } catch { }
                try { sb.Append(" key='" + pr.Key + "' cat='" + pr.Katalog + "'"); } catch { }
                try { sb.Append(" pos='" + pr.Posnum + "' send='" + pr.Sendnum + "' orig='" + pr.Originalnum + "'"); } catch { }
                try { sb.Append(" L=" + F(pr.Length) + " W=" + F(pr.Wide) + " H=" + F(pr.Height)); } catch { }
                try { sb.Append(" dia=" + F(pr.Diameter) + " lenAdd=" + F(pr.LenAdd)); } catch { }
                try { sb.Append(" wt=" + F(pr.Weight) + " volWt=" + pr.VolumeWeightFlag); } catch { }
                try { sb.Append(" paintArea=" + F(pr.PaintArea) + " cutArea=" + F(pr.CutArea)); } catch { }
                try { sb.Append(" klemm=" + F(pr.KlemmLen)); } catch { }
                try { sb.Append(" count=" + pr.Count + "/" + pr.TotalCount); } catch { }
                try { sb.Append(" layer='" + pr.LayerName + "' style='" + pr.StyleName + "'"); } catch { }
                try { sb.Append(" mat=" + pr.Material + " art='" + pr.Article + "'"); } catch { }
                try { sb.Append(" mir=" + pr.MirrorFlag + " ymir=" + pr.YMirrorFlag + " mirrored=" + pr.Mirrored); } catch { }
                try { sb.Append(" ins=" + F(pr.InsertX) + "," + F(pr.InsertY) + " scale=" + F(pr.Scale)); } catch { }
                try { PsPoint o = pr.Origin; sb.Append(" org=" + F(o.x) + "," + F(o.y) + "," + F(o.z)); } catch { }
                try { PsVector v = pr.XAxis; sb.Append(" X=" + F(v.x) + "/" + F(v.y) + "/" + F(v.z)); } catch { }
                try { PsVector v = pr.YAxis; sb.Append(" Y=" + F(v.x) + "/" + F(v.y) + "/" + F(v.z)); } catch { }
                try { PsVector v = pr.ZAxis; sb.Append(" Z=" + F(v.x) + "/" + F(v.y) + "/" + F(v.z)); } catch { }
                try { PsPoint a = pr.MidLineStart, b = pr.MidLineEnd;
                      sb.Append(" mid=" + F(a.x) + "," + F(a.y) + "," + F(a.z) +
                                "->" + F(b.x) + "," + F(b.y) + "," + F(b.z)); } catch { }
                try { PsPoint mn = new PsPoint(0,0,0), mx = new PsPoint(0,0,0);
                      if (pr.GetExtents(ref mn, ref mx))
                          sb.Append(" ext=" + F(mn.x) + "," + F(mn.y) + "," + F(mn.z) +
                                    ";" + F(mx.x) + "," + F(mx.y) + "," + F(mx.z)); } catch { }
                try { sb.Append(" partOrigin=" + pr.PartOrigin + " objType=" + pr.ObjectType); } catch { }
                try { sb.Append(" proc=" + pr.ProcessStatus + " visible=" + pr.Visible); } catch { }
                try { sb.Append(" noPos=" + pr.DontPositionFlag + " noDetail=" + pr.DontDetailFlag +
                                " partList=" + pr.PartListFlag + " boltList=" + pr.BoltListFlag); } catch { }
                return sb.ToString();
            }
            catch (System.Exception ex) { err = ex.Message; return ""; }
        }

        void Props(Dictionary<string, string> kv)
        {
            string h = Get(kv, "handle", "");
            string err;
            string s = PropsOf(IdFromHandle(h), out err);
            if (s.Length == 0) { Result("EB_ERR props " + h + " : " + err); return; }
            Result("EB_OK props handle=" + h + " " + s);
        }

        // Describe every logical link (= connection) sitting on a part.
        static string LinkDesc(PsLogicalLink lk)
        {
            StringBuilder sb = new StringBuilder();
            try { sb.Append("type=" + (int)lk.Type); } catch { sb.Append("type=?"); }
            try { sb.Append(" name=" + Safe(lk.Name)); } catch { }
            try { sb.Append(" ident=" + Safe(lk.Ident)); } catch { }
            try { sb.Append(" desc=" + One(lk.Description)); } catch { }
            try { sb.Append(" modi=" + (int)lk.ModiType); } catch { }
            try { sb.Append(" parts=" + lk.LinkObjectCount + " bolts=" + lk.BoltObjectCount
                          + " extra=" + lk.AdditionalObjectCount); } catch { }
            // the parameter sets — whichever one this link carries
            try
            {
                PsBaseplateLinkDataMgd d = lk.GetBasePlateLinkData();
                if (d != null)
                    sb.Append(" BASEPLATE[L=" + F(d.Length) + " W=" + F(d.Width) + " t=" + F(d.Thickness)
                        + " holeDia=" + F(d.HoleDiameter) + " hx=" + F(d.HoleDistanceHorizontal)
                        + " hy=" + F(d.HoleDistanceVertical) + " anchors=" + (d.AnchorBolts ? "1" : "0")
                        + " anchorDia=" + F(d.AnchorBoltDiameter)
                        + " grip=" + F(d.AnchorBoltGripLength)
                        + " gripDia=" + F(d.AnchorBoltGripDiameter)
                        + " drillLen=" + F(d.AnchorBoltDrillLength)
                        + " key=" + F(d.AnchorBoltKeySize)
                        + " lining=" + F(d.LiningThickness)
                        + " detailed=" + (d.CreateDetailedAnchorBolts ? "1" : "0")
                        + " outside=" + (d.AnchorBoltsOutside ? "1" : "0")
                        + " dts=" + F(d.DistanceToSupport)
                        + " shorten=" + (d.ShortenShape ? "1" : "0")
                        + " poly=" + (d.BasePlateIsPolyPlate ? "1" : "0")
                        + " weldFl=" + F(d.WeldSeamFlange) + " weldWeb=" + F(d.WeldSeamWeb) + "]");
            }
            catch { }
            try
            {
                PsStiffenerLinkDataMgd d = lk.GetStiffenerLinkData();
                if (d != null)
                    sb.Append(" RIB[t=" + F(d.Thickness) + " len=" + F(d.Length) + " shape=" + d.ShapeType
                        + " lenType=" + d.LengthType + " r=" + F(d.Radius) + " flDist=" + F(d.FlangeDistance)
                        + " webDist=" + F(d.WebDistance) + " ang=" + F(d.InsertAngle)
                        + " weldFl=" + F(d.WeldSeamFlange) + " weldWeb=" + F(d.WeldSeamWeb) + "]");
            }
            catch { }
            try
            {
                PsSpliceJointLinkDataMgd d = lk.GetSpliceJointLinkData();
                if (d != null)
                    sb.Append(" SPLICE[gap=" + F(d.DistanceBetweenObjects) + " holeDia=" + F(d.HoleDiameter)
                        + " play=" + F(d.HoleWorkloose) + " tWeb=" + F(d.PlateThicknessWeb)
                        + " tFl=" + F(d.PlateThicknessFlange) + " nH_web=" + d.HoleCountHorizontalWeb
                        + " nV_web=" + d.HoleCountVerticalWeb + " nH_fl=" + d.HoleCountHorizontalFlange
                        + " nV_fl=" + d.HoleCountVerticalFlange + " sideLap=" + F(d.SidePlateLap)
                        + " topLap=" + F(d.TopPlateLap) + "]");
            }
            catch { }
            try
            {
                PsShearPlateLinkDataMgd d = lk.GetShearPlateLinkData();
                if (d != null) sb.Append(" SHEARPLATE[present]");
            }
            catch { }
            try
            {
                PsWebAngleLinkDataMgd d = lk.GetWebAngleLinkData();
                if (d != null) sb.Append(" WEBANGLE[present]");
            }
            catch { }
            try
            {
                PsCopeLinkDataMgd d = lk.GetCopeLinkData();
                if (d != null) sb.Append(" COPE[present]");
            }
            catch { }
            return sb.ToString();
        }

        void ConnScan(Dictionary<string, string> kv)
        {
            string one = Get(kv, "handle", "");
            string outName = Get(kv, "out", "eb_conn.txt");
            double maxx = double.Parse(Get(kv, "maxx", "1e9"), System.Globalization.CultureInfo.InvariantCulture);
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            StringBuilder sb = new StringBuilder();
            int scanned = 0, withLinks = 0, links = 0, errs = 0;
            var typeCount = new Dictionary<string, int>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    string hx = id.Handle.ToString();
                    if (one.Length > 0 && hx != one) continue;
                    string cls = id.ObjectClass != null ? id.ObjectClass.Name : "?";
                    if (cls.IndexOf("Ks_") < 0) continue;
                    Entity ent = null;
                    try { ent = tr.GetObject(id, OpenMode.ForRead) as Entity; } catch { }
                    if (ent == null) continue;
                    try { if (ent.GeometricExtents.MinPoint.X >= maxx) continue; } catch { }
                    scanned++;
                    try
                    {
                        PsEditLogicalLink ed = new PsEditLogicalLink();
                        ed.SetObjectId(id.OldIdPtr.ToInt64());
                        int n = ed.get_LogicalLinkCount();
                        if (n <= 0) continue;
                        withLinks++;
                        for (int i = 0; i < n; i++)
                        {
                            int num = i;
                            try { num = ed.get_LinkNumberFromIndex(i); } catch { }
                            PsLogicalLink lk = null;
                            try { lk = ed.GetLogicalLinkByNumber(num); } catch { }
                            if (lk == null) continue;
                            links++;
                            string d = LinkDesc(lk);
                            string tk = "?";
                            int p = d.IndexOf("type=");
                            if (p >= 0) { int q = d.IndexOf(' ', p); tk = q > p ? d.Substring(p + 5, q - p - 5) : d.Substring(p + 5); }
                            string nm = "t" + tk;
                            if (d.IndexOf("BASEPLATE[") >= 0) nm += "/BASEPLATE";
                            if (d.IndexOf("RIB[") >= 0) nm += "/RIB";
                            if (d.IndexOf("SPLICE[") >= 0) nm += "/SPLICE";
                            if (d.IndexOf("SHEARPLATE[") >= 0) nm += "/SHEARPLATE";
                            if (d.IndexOf("WEBANGLE[") >= 0) nm += "/WEBANGLE";
                            if (d.IndexOf("COPE[") >= 0) nm += "/COPE";
                            if (!typeCount.ContainsKey(nm)) typeCount[nm] = 0;
                            typeCount[nm]++;
                            sb.AppendLine("LINK\t" + hx + "\t" + cls + "\t" + ent.Layer + "\t" + num + "\t" + d);
                            // which parts and bolts belong to this joint
                            try
                            {
                                StringBuilder mem = new StringBuilder();
                                for (int k = 0; k < lk.LinkObjectCount; k++)
                                    mem.Append((k > 0 ? "," : "") + lk.getLinkObjectId(k));
                                StringBuilder bo = new StringBuilder();
                                for (int k = 0; k < lk.BoltObjectCount; k++)
                                    bo.Append((k > 0 ? "," : "") + lk.getBoltObjectId(k));
                                sb.AppendLine("MEMB\t" + hx + "\t" + num + "\tparts=" + mem + "\tbolts=" + bo);
                            }
                            catch { }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        errs++;
                        if (errs <= 5) sb.AppendLine("ERR\t" + hx + "\t" + One(ex.Message));
                    }
                }
                tr.Commit();
            }
            StringBuilder tc = new StringBuilder();
            foreach (var p in typeCount) tc.Append(" " + p.Key + "=" + p.Value);
            File.WriteAllText(Path.Combine(Dir, outName), sb.ToString(), Encoding.UTF8);
            Result("EB_OK connscan scanned=" + scanned + " withlinks=" + withLinks
                 + " links=" + links + " err=" + errs + " |" + tc.ToString() + " -> " + outName);
        }

        // What connection TEMPLATES are configured in this ProSteel installation?
        // These are the named joint recipes the modeller (Amir) works with.
        // ---- v37: DRILL A HOLE FIELD, not a loop of single holes ----------------
        // Manual B.14 (read in full 06/08/2026): "The program manages drill holes in the
        // form of DRILL HOLE FIELDS. Groups consisting, for instance, of 2 x 2 holes will
        // be drilled in ONE operation." The plugin only ever called SetSingleHoleField --
        // one hole per call, in a Python loop. That is the wrong unit of work, and it is
        // why a rotated 3x2 pattern still counted as "6 holes correct".
        //
        // Field syntax, verbatim from the manual:
        //     Number1*Pitch1, IntermediatePitch1, Number2*Pitch2, ...
        //   x=2*60,200,1*,200,3*40   y=2*100
        //   - only longitudinal  -> leave y EMPTY
        //   - only crosswise     -> x MUST contain "1*"
        //   - "W" instead of a pitch uses the SHAPE'S OWN marking gauges  (2*W)
        //   - one field cannot mix one-hole and two-hole crosswise groups
        //
        // op=drillfield hosts=h1,h2 at=x,y,z dia=23 x=3*81 y=2*156
        //               [n=0,0,1] [play=3] [innercontour=1] [slot=<len>] [htype=]
        void DrillField(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string hosts = Get(kv, "hosts", Get(kv, "handle", ""));
            PsPoint at = Pt(Get(kv, "at", "0,0,0"));
            double dia = double.Parse(Get(kv, "dia", "23"), IC);
            string xf = Get(kv, "x", "");
            string yf = Get(kv, "y", "");
            string sPlay = Get(kv, "play", "");
            string sInner = Get(kv, "innercontour", "");
            string sSlot = Get(kv, "slot", "");
            double[] nv = Nums(Get(kv, "n", "0,0,1"));
            PsVector nrm = new PsVector(nv[0], nv.Length > 1 ? nv[1] : 0.0,
                                                nv.Length > 2 ? nv[2] : 1.0);

            if (xf.Length == 0 && yf.Length == 0)
            {
                Result("EB_ERR drillfield: no field given. Use x=3*81 and/or y=2*156 " +
                       "(crosswise-only still needs x=1*).");
                return;
            }
            // The manual is explicit: a crosswise-only field still needs "1*" in X.
            if (xf.Length == 0) xf = "1*";

            string[] hh = hosts.Split(new char[] { ',', ';' });
            int okParts = 0, failParts = 0;
            StringBuilder detail = new StringBuilder();
            int totalBefore = 0, totalAfter = 0;

            foreach (string raw in hh)
            {
                string hs = raw.Trim();
                if (hs.Length == 0) continue;
                long oid = IdFromHandle(hs);
                if (oid == 0) { failParts++; detail.Append(" " + hs + ":badhandle"); continue; }

                // count the holes on THIS part before and after -- a delta per part, never a total
                string errb = "";
                int before = Rec.HolesOfStatic(oid, out errb);
                string msg = "";
                try
                {
                    PsDrillObject d = new PsDrillObject();
                    d.SetToDefaults();
                    d.SetObjectId(oid);
                    d.SetInsertPoint(at);
                    d.SetNormal(nrm);
                    if (sPlay.Length > 0) d.SetHoleWorkloose(double.Parse(sPlay, IC));
                    // B.14.3: without this a hollow section is drilled on ONE WALL only
                    if (sInner.Length > 0) d.SetIgnoreInnerContour(sInner == "1");
                    if (sSlot.Length > 0) d.SetRotateSlottedHoles(true);
                    d.SetLinearHoleField(dia, xf, yf);
                    d.Apply();
                }
                catch (System.Exception ex) { msg = " EX:" + ex.Message; }

                string erra = "";
                int after = Rec.HolesOfStatic(oid, out erra);
                totalBefore += before; totalAfter += after;
                int made = after - before;
                if (made > 0) okParts++; else failParts++;
                detail.Append(" " + hs + ":" + before + "->" + after + "(+" + made + ")" + msg);
            }

            // ---- v63: REQUESTED vs MADE ----
            // Measured 06/08/2026: a drill FIELD is CENTRED on the insert point, not started
            // at it -- x='2*100' about 13000 gives holes at 12950 and 13050. And a hole that
            // lands exactly on the part boundary is DROPPED SILENTLY: the same field about
            // 12900 on a plate spanning 12850..13150 produced ONE hole, with parts_ok=1 and
            // no complaint from ProSteel on the command line either.
            // Losing a bolt hole without being told is a fabrication error, so the declared
            // count is compared with the achieved count and any shortfall is shouted about.
            int wantPer = FieldCount(xf) * FieldCount(yf);
            int wantTotal = wantPer * okParts;
            int madeTotal = totalAfter - totalBefore;
            string shortfall = "";
            if (okParts > 0 && madeTotal < wantTotal)
                shortfall = "  *** SHORT BY " + (wantTotal - madeTotal) + ": asked for " +
                            wantPer + " hole(s) per part, got " + madeTotal + " over " +
                            okParts + " part(s). A hole landing ON the part boundary is " +
                            "dropped silently -- the field is CENTRED on at=, so move at= " +
                            "or shrink the pitch. ***";

            // Status from the measured per-part delta. The old drill op reported the host's
            // TOTAL hole count, so re-drilling an already-drilled part "succeeded".
            string tail = " dia=" + F(dia) + " x='" + xf + "' y='" + yf + "'" +
                          " wanted=" + wantTotal + shortfall +
                          " parts_ok=" + okParts + " parts_failed=" + failParts +
                          " holes=" + totalBefore + "->" + totalAfter +
                          "(+" + (totalAfter - totalBefore) + ")" + detail.ToString();
            if (okParts > 0 && failParts == 0) Result("EB_OK drillfield" + tail);
            else Result("EB_ERR drillfield" + tail);
        }

        // handle text for a freshly created object (the inverse of IdFromHandle)
        static string HandleOf(long oid)
        {
            try
            {
                ObjectId id = new ObjectId(new System.IntPtr(oid));
                Document doc = Application.DocumentManager.MdiActiveDocument;
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    DBObject o = tr.GetObject(id, OpenMode.ForRead);
                    string h = o.Handle.ToString();
                    tr.Commit();
                    return h;
                }
            }
            catch { return "?"; }
        }









        // The collision API returns no pair list and no coordinates -- only a count. The
        // solids it leaves behind are the only evidence of WHERE, so read their centres.
        static string WhereAre(List<string> handles)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string hx in handles)
            {
                long oid = IdFromHandle(hx);
                if (oid == 0) continue;
                try
                {
                    PsPoint mn = new PsPoint(0, 0, 0), mx = new PsPoint(0, 0, 0);
                    PsObjectProperties p = new PsObjectProperties();
                    p.readFrom(oid);
                    if (p.GetExtents(ref mn, ref mx))
                        sb.Append(" " + hx + "@" + F((mn.x + mx.x) / 2) + "," +
                                  F((mn.y + mx.y) / 2) + "," + F((mn.z + mx.z) / 2));
                    else sb.Append(" " + hx + "@?");
                }
                catch { sb.Append(" " + hx + "@?"); }
            }
            return sb.ToString().Trim();
        }


        // How many holes a drill-field spec asks for. The manual's syntax is
        //   Number1*Pitch1, IntermediatePitch1, Number2*Pitch2
        // so the hole count on one axis is the sum of the Number terms; an axis left empty
        // contributes 1. Used to catch holes the software drops SILENTLY -- see below.
        static int FieldCount(string spec)
        {
            if (spec == null || spec.Trim().Length == 0) return 1;
            int total = 0;
            foreach (string part in spec.Split(','))
            {
                string p = part.Trim();
                int star = p.IndexOf('*');
                if (star <= 0) continue;                 // an intermediate pitch, not a group
                int n;
                if (int.TryParse(p.Substring(0, star).Trim(), out n)) total += n;
            }
            return total <= 0 ? 1 : total;
        }




        // ---- v67: WHERE DO TWO PARTS TOUCH? ----
        // "Touch Plane" in the bolted-connection dialog (manual B.14.2) has NO property or
        // method anywhere in the API. Its programmatic equivalent is to find the contact face
        // yourself: PsSurfaceFinder.FindCommonPlane returns the shared plane of two parts as
        // an origin + normal, which is exactly what PsDrillObject.SetInsertPoint/SetNormal
        // want. Manual B.14.2: "Normally, drill holes are created along the z-axis of the
        // current UCS. At bolted connections the axis is taken from the contact surface."
        //
        // This is the software answering "where do the bolts go" instead of the agent doing
        // arithmetic on extents -- which is the difference between modelling a joint and
        // guessing at one.
        //
        //   op=touchplane a=<h> b=<h> [tol=1]
        //   op=touchdrill a=<h> b=<h> [tol=1] dia=<bolt> [play=] [x=] [y=]
        //        -> find the contact face, then drill BOTH parts on its normal
        void TouchPlane(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            long a = IdFromHandle(Get(kv, "a", ""));
            long b = IdFromHandle(Get(kv, "b", ""));
            if (a == 0 || b == 0) { Result("EB_ERR touchplane: need two valid handles a= and b="); return; }
            double tol = double.Parse(Get(kv, "tol", "1"), IC);

            try
            {
                PsSurfaceFinder sf = new PsSurfaceFinder();
                sf.SetToDefaults();
                PsGeo geo = new PsGeo();
                PsPoint origin = new PsPoint(0, 0, 0);
                PsVector normal = new PsVector(0, 0, 1);
                bool ok = sf.FindCommonPlane(a, b, tol, geo, origin, normal);
                if (!ok)
                {
                    Result("EB_ERR touchplane: no common plane for " + Get(kv, "a", "") + " and " +
                           Get(kv, "b", "") + " at tol=" + F(tol) +
                           " -- the parts may not actually touch, or the tolerance is too tight");
                    return;
                }
                // EdgePoint is NOT a point -- the compiler exposed it as
                //   get_EdgePoint(PositionSelection, PositionSelection)
                // i.e. a PICKER on the contact face: "give me the point at (left|centre|right,
                // bottom|centre|top)". The dump renders it as a plain property and hides both
                // index parameters. That makes it exactly the tool for placing a bolt group on
                // the face two parts share -- corners and centre, straight from the software.
                string extra = "";
                try
                {
                    PositionSelection[] hs = { PositionSelection.kLeft, PositionSelection.kCenter, PositionSelection.kRight };
                    string[] hn = { "L", "C", "R" };
                    PositionSelection[] vs = { PositionSelection.kDown, PositionSelection.kCenter, PositionSelection.kTop };
                    string[] vn = { "B", "C", "T" };
                    for (int i = 0; i < 3; i++)
                        for (int j = 0; j < 3; j++)
                        {
                            try
                            {
                                PsPoint q = sf.get_EdgePoint(hs[i], vs[j]);
                                extra += " " + hn[i] + vn[j] + "=" + F(q.x) + "," + F(q.y) + "," + F(q.z);
                            }
                            catch { }
                        }
                }
                catch { }
                try { extra += " sfX=" + F(sf.XAxis.x) + "/" + F(sf.XAxis.y) + "/" + F(sf.XAxis.z) +
                               " sfY=" + F(sf.YAxis.x) + "/" + F(sf.YAxis.y) + "/" + F(sf.YAxis.z); }
                catch { }
                Result("EB_OK touchplane a=" + Get(kv, "a", "") + " b=" + Get(kv, "b", "") +
                       " origin=" + F(origin.x) + "," + F(origin.y) + "," + F(origin.z) +
                       " normal=" + F(normal.x) + "/" + F(normal.y) + "/" + F(normal.z) +
                       " tol=" + F(tol) + extra);
            }
            catch (System.Exception ex) { Result("EB_ERR touchplane EX:" + ex.Message); }
        }

        void TouchDrill(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            long a = IdFromHandle(Get(kv, "a", ""));
            long b = IdFromHandle(Get(kv, "b", ""));
            if (a == 0 || b == 0) { Result("EB_ERR touchdrill: need two valid handles a= and b="); return; }
            double tol  = double.Parse(Get(kv, "tol", "1"), IC);
            double dia  = double.Parse(Get(kv, "dia", "20"), IC);
            double play = double.Parse(Get(kv, "play", "3"), IC);
            string xf = Get(kv, "x", "2*80"), yf = Get(kv, "y", "2*80");

            PsPoint origin = new PsPoint(0, 0, 0);
            PsVector normal = new PsVector(0, 0, 1);
            try
            {
                PsSurfaceFinder sf = new PsSurfaceFinder();
                sf.SetToDefaults();
                PsGeo geo = new PsGeo();
                if (!sf.FindCommonPlane(a, b, tol, geo, origin, normal))
                { Result("EB_ERR touchdrill: no common plane -- the parts do not touch within " + F(tol)); return; }
            }
            catch (System.Exception ex) { Result("EB_ERR touchdrill find EX:" + ex.Message); return; }

            // drill BOTH parts on the contact normal, so the bolt passes through the joint
            string err;
            StringBuilder scratch = new StringBuilder();
            int aBefore = HolesOf(a, 2, scratch, "a", out err);
            scratch.Length = 0;
            int bBefore = HolesOf(b, 2, scratch, "b", out err);
            string msg = "";
            foreach (long id in new long[] { a, b })
            {
                try
                {
                    PsDrillObject d = new PsDrillObject();
                    d.SetToDefaults();
                    d.SetObjectId(id);
                    d.SetInsertPoint(origin);
                    d.SetNormal(normal);
                    d.SetHoleWorkloose(play);
                    d.SetLinearHoleField(dia, xf, yf);
                    d.Apply();
                }
                catch (System.Exception ex) { msg += " EX:" + ex.Message; }
            }
            scratch.Length = 0;
            int aAfter = HolesOf(a, 2, scratch, "a", out err);
            scratch.Length = 0;
            int bAfter = HolesOf(b, 2, scratch, "b", out err);

            int want = FieldCount(xf) * FieldCount(yf);
            bool ok = (aAfter - aBefore) == want && (bAfter - bBefore) == want;
            Result((ok ? "EB_OK" : "EB_ERR") + " touchdrill a=" + Get(kv, "a", "") +
                   " b=" + Get(kv, "b", "") +
                   " plane=" + F(origin.x) + "," + F(origin.y) + "," + F(origin.z) +
                   " n=" + F(normal.x) + "/" + F(normal.y) + "/" + F(normal.z) +
                   " wantedEach=" + want +
                   " a:" + aBefore + "->" + aAfter + " b:" + bBefore + "->" + bAfter + msg);
        }

        // ---- v66: CUT ONE PART AT ANOTHER -- the bread and butter of real steel ----
        // Manual B.12.1 "Cut at Shape" (p.197): "The shape is cut or extended at another
        // shape. When the shape is cut, the shorter section is always cut off."
        // ⚠️ HARD PRECONDITION, verbatim: "The plane actually hit by the centerline (or the
        // extended centerline) of the shape to be cut will be the cut plane. If the centerline
        // does not meet any surface, no cut can be made!"  -- so a beam whose axis misses the
        // column entirely cannot be cut, no matter how much the bodies overlap.
        // Manual p.208: "a logical link is created between the parts at these cutting commands"
        // -- the cut therefore UPDATES when the other part moves. That is the whole point of
        // doing it this way instead of trimming by hand.
        //
        // at= disambiguates WHICH END dies. Without it the software applies its own
        // "shorter section is cut off" rule, which is a guess about intent.
        //
        //   op=cutat handle=<h> other=<h> [mode=object|straight|rounded|miter] [at=x,y,z]
        //            [outside=1]  (straight only: cut at the OUTER edge)
        //            [type=1]     (miter only: which of the two miter variants)
        void CutAt(Dictionary<string, string> kv)
        {
            string hh = Get(kv, "handle", "");
            string ho = Get(kv, "other", "");
            long oid = IdFromHandle(hh), other = IdFromHandle(ho);
            if (oid == 0) { Result("EB_ERR cutat: bad handle " + hh); return; }
            if (other == 0) { Result("EB_ERR cutat: bad other= " + ho); return; }
            if (oid == other) { Result("EB_ERR cutat: a part cannot be cut at itself"); return; }
            string mode = Get(kv, "mode", "object").ToLowerInvariant();
            string atS = Get(kv, "at", "");

            double lenBefore = LengthOf(oid);
            string before = ModSig(oid), msg = "";
            int rc = -999;
            try
            {
                PsCutObjects cut = new PsCutObjects();
                cut.SetToDefaults();
                cut.SetObjectId(oid);
                if (mode == "straight")
                    cut.SetAsStraightCutId(other, Get(kv, "outside", "1") == "1");
                else if (mode == "rounded")
                    cut.SetAsRoundedCutId(other);
                else if (mode == "miter")
                    cut.SetAsMiterCutId(other, Get(kv, "type", "0") == "1");
                else if (atS.Length > 0)
                    cut.SetAsObjectCutId(other, Pt(atS));     // pick point = which end dies
                else
                    cut.SetAsObjectCutId(other);
                rc = cut.Apply();
            }
            catch (System.Exception ex) { msg = " EX:" + ex.Message; }

            double lenAfter = LengthOf(oid);
            string after = ModSig(oid);
            // A cut creates no objects and may leave the modification counts alone -- it
            // SHORTENS the member. Length is the honest instrument, as the cope proved.
            bool changed = System.Math.Abs(lenAfter - lenBefore) > 0.01 || before != after;
            Result((changed ? "EB_OK" : "EB_ERR") + " cutat handle=" + hh + " other=" + ho +
                   " mode=" + mode + " len=" + F(lenBefore) + "->" + F(lenAfter) +
                   " applyRc=" + rc + " mods[" + before + "]->[" + after + "]" + msg +
                   (changed ? "" : "  (manual B.12.1: if the CENTRELINE of the cut part does " +
                                   "not meet a surface of the other part, no cut can be made)"));
        }

        // ---- v65: A HOLE OF ANY SHAPE -- PsCutObjects.SetAsPolyCut ----
        // Cable penetrations, access openings, service holes: everything that is not a round
        // bolt hole. Until now the only tool for a non-rectangular opening was redrawing the
        // plate's whole outline, which is how lesson 3 reshaped 214 ribs.
        //
        // PsPolygon is a full 2D geometry library -- 120+ methods, of which this agent had
        // touched three. The presets below use its own constructors rather than hand-built
        // vertex lists: createRectangle(L, W, CornerRadius), createCircle(R),
        // createPolygon(NumSides, Size, Inside).
        //
        //   op=polycut handle=<h> at=x,y,z depth=<mm> shape=rect|circle|poly|pts
        //       rect   : l= w= [radius=<corner>]
        //       circle : r=
        //       poly   : n=<sides> size= [inside=1]
        //       pts    : pts=x1,y1;x2,y2;...   (plate-local, 2D)
        void PolyCut(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string hh = Get(kv, "handle", "");
            long oid = IdFromHandle(hh);
            if (oid == 0) { Result("EB_ERR polycut: bad handle " + hh); return; }
            string shape = Get(kv, "shape", "rect").ToLowerInvariant();
            double depth = double.Parse(Get(kv, "depth", "0"), IC);
            PsPoint at = Pt(Get(kv, "at", "0,0,0"));
            double[] xa = Nums(Get(kv, "xaxis", "1,0,0"));
            double[] ya = Nums(Get(kv, "yaxis", "0,1,0"));

            string before = ModSig(oid), msg = "";
            int rc = -999, verts = -1;
            double area = -1;
            try
            {
                PsPolygon pg = new PsPolygon();
                pg.init();
                if (shape == "circle")
                {
                    pg.createCircle(double.Parse(Get(kv, "r", "30"), IC));
                }
                else if (shape == "poly")
                {
                    pg.createPolygon(int.Parse(Get(kv, "n", "6")),
                                     double.Parse(Get(kv, "size", "60"), IC),
                                     Get(kv, "inside", "1") == "1");
                }
                else if (shape == "pts")
                {
                    foreach (string p in Get(kv, "pts", "").Split(';'))
                    {
                        string t = p.Trim();
                        if (t.Length == 0) continue;
                        double[] xy = Nums(t);
                        pg.appendVertex(xy[0], xy.Length > 1 ? xy[1] : 0.0,
                                        xy.Length > 2 ? xy[2] : 0.0);   // third value = bulge
                    }
                    pg.Close();
                }
                else
                {
                    double rr = double.Parse(Get(kv, "radius", "0"), IC);
                    if (rr > 0) pg.createRectangle(double.Parse(Get(kv, "l", "100"), IC),
                                                   double.Parse(Get(kv, "w", "60"), IC), rr);
                    else pg.createRectangle(double.Parse(Get(kv, "l", "100"), IC),
                                            double.Parse(Get(kv, "w", "60"), IC));
                }
                // The polygon can say whether it is sane BEFORE it is used to cut anything:
                // a self-intersecting or unclosed outline is a cut that will fail obscurely.
                try { pg.check(0.01, true); } catch { }
                verts = pg.Count;
                try { area = pg.Area; } catch { }
                if (verts < 3)
                { Result("EB_ERR polycut: the outline has " + verts + " vertices -- refused"); return; }

                PsCutObjects cut = new PsCutObjects();
                cut.SetToDefaults();
                cut.SetObjectId(oid);
                cut.SetAsPolyCut(pg, at,
                                 new PsVector(xa[0], xa.Length > 1 ? xa[1] : 0, xa.Length > 2 ? xa[2] : 0),
                                 new PsVector(ya[0], ya.Length > 1 ? ya[1] : 0, ya.Length > 2 ? ya[2] : 0),
                                 depth);
                rc = cut.Apply();
            }
            catch (System.Exception ex) { msg = " EX:" + ex.Message; }

            string after = ModSig(oid);
            Result((before != after ? "EB_OK" : "EB_ERR") + " polycut handle=" + hh +
                   " shape=" + shape + " verts=" + verts + " area=" + F(area) +
                   " depth=" + F(depth) + " applyRc=" + rc +
                   " before[" + before + "] after[" + after + "]" + msg);
        }

        // ---- v61: COLLISION CHECK ----
        // The check runs from code with no dialog. The RESULTS do not come back through the
        // API at all: PsCollisionCheck exposes only `BodyCount` and a ZoomToObject viewport
        // helper -- there is no GetBody(i), no pair list, no per-collision volume and no
        // report file anywhere in the dump or the manual. So the collision SOLIDS have to be
        // recovered by diffing the drawing's objects before and after, which is why
        // CreateBodys must be true.
        //
        // ⚠️ SetToDefaults() is not optional: the class wraps the PERSISTENT PS_COLLISION
        // dialog state, so skipping it silently inherits whatever the last interactive run
        // left behind.
        // ⚠️ CollectObjectsFromSelection is Void. An empty selection gives a perfectly
        // healthy-looking run with BodyCount 0 -- identical to a clean model. The object
        // count is asserted before Apply, otherwise "no collisions" means nothing.
        // ⚠️ Cost grows with the SQUARE of the part count (the manual says so outright), and
        // the class itself has no box/layer/subset parameter -- restriction lives on
        // PsSelection. box=x1,y1,z1;x2,y2,z2 limits the run to one joint.
        //
        //   op=collision [box=x1,y1,z1;x2,y2,z2] [minvol=<mm3>] [clean=1] [bolts=0]
        void Collision(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            double minvol = double.Parse(Get(kv, "minvol", "1"), IC);
            bool withBolts = Get(kv, "bolts", "1") != "0";
            string boxS = Get(kv, "box", "");
            DateTime t0 = DateTime.Now;

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            // snapshot BEFORE -- the only way to recover which solids the run created
            HashSet<string> beforeIds = new HashSet<string>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    try { beforeIds.Add(tr.GetObject(id, OpenMode.ForRead).Handle.ToString()); }
                    catch { }
                }
                tr.Commit();
            }

            int selCount = 0, bodyCount = -1, rc = -999;
            string msg = "";
            try
            {
                Bentley.ProStructures.Drawing.PsSelection sel =
                    new Bentley.ProStructures.Drawing.PsSelection();
                sel.Initialize();
                if (boxS.Length > 0)
                {
                    string[] pp = boxS.Split(';');
                    sel.SelectAllObjectsInRange(true, false, false, Pt(pp[0]), Pt(pp[1]));
                }
                else sel.SelectAllObjects(true, false, false);
                // The Int32 these return is a STATUS, not a count: measured 1 for a whole
                // 132-entity model and 0 for an empty range. ObjectCount is the real number.
                // Reporting the status as "parts=" made a 60-part run look like a 1-part run.
                selCount = sel.ObjectCount;

                if (selCount == 0)
                {
                    Result("EB_ERR collision: the selection is EMPTY -- a run on nothing reports " +
                           "zero collisions and looks exactly like a clean model. Refused.");
                    return;
                }

                PsCollisionCheck cc = new PsCollisionCheck();
                cc.SetToDefaults();                       // MUST be first: persistent dialog state
                if (Get(kv, "clean", "") == "1") cc.DeleteAllExistingBodies();
                cc.CreateBodys = true;                    // else the result is unreadable
                cc.MinVolume = minvol;
                cc.Verbose = true;                        // goes to the command line -> eb_log
                cc.CheckShapeToShape = true;
                cc.CheckShapeToPlate = true;
                cc.CheckPlateToPlate = true;
                cc.CheckShapeToBolt = withBolts;
                cc.CheckPlateToBolt = withBolts;
                cc.CheckBoltToBolt = false;               // bolt-to-bolt is noise on a real model
                cc.CollectObjectsFromSelection(sel);      // Void -> silent
                rc = cc.Apply();
                bodyCount = cc.BodyCount;
            }
            catch (System.Exception ex) { msg = " EX:" + ex.Message; }

            // diff AFTER -- the collision solids, recovered the only way available
            List<string> created = new List<string>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    try
                    {
                        DBObject o = tr.GetObject(id, OpenMode.ForRead);
                        string hx = o.Handle.ToString();
                        if (!beforeIds.Contains(hx)) created.Add(hx);
                    }
                    catch { }
                }
                tr.Commit();
            }

            double secs = (DateTime.Now - t0).TotalSeconds;
            // BodyCount and the id diff must agree. If they do not, say so rather than
            // picking whichever number is more convenient.
            string agree = (bodyCount == created.Count) ? "agree" :
                           "DISAGREE(bodyCount=" + bodyCount + " newIds=" + created.Count + ")";
            Result((bodyCount == 0 && created.Count == 0 ? "EB_OK" : "EB_OK") +
                   " collision parts=" + selCount + " collisions=" + bodyCount +
                   " newSolids=" + created.Count + " " + agree +
                   " applyRc=" + rc + " minvol=" + F(minvol) +
                   " secs=" + secs.ToString("0.0", IC) +
                   (created.Count > 0 ? " | " + WhereAre(created) : "") + msg);
        }

        // ---- v59: THE REST OF PsCutObjects, AND AN INVENTORY TO VERIFY IT WITH ----
        // PsEditModification counts every kind of modification a part carries. Until now the
        // only instrument for "did the cut happen" was FacetCount -- which is why a working
        // cope once reported as a failure. This is the general answer to that question.
        //
        //   op=mods handle=<h>
        void Mods(Dictionary<string, string> kv)
        {
            string h = Get(kv, "handle", "");
            long oid = IdFromHandle(h);
            if (oid == 0) { Result("EB_ERR mods: bad handle " + h); return; }
            StringBuilder sb = new StringBuilder();
            try
            {
                PsEditModification em = new PsEditModification();
                em.SetObjectId(oid);
                int nf = 0, nc = 0, nhf = 0, no = 0, np = 0, ns = 0;
                try { nf = em.FacetCount; } catch { }
                try { nc = em.CutPlaneCount; } catch { }
                try { nhf = em.HoleFieldCount; } catch { }
                try { no = em.OutletCount; } catch { }
                try { np = em.PolyCutCount; } catch { }
                try { ns = em.SubBodyCount; } catch { }
                sb.Append("facets=" + nf + " cutPlanes=" + nc + " holeFields=" + nhf +
                          " outlets=" + no + " polyCuts=" + np + " subBodies=" + ns);
                for (int i = 0; i < nf; i++)
                {
                    try
                    {
                        PsVertexChamfer f = em.get_Facet(em.GetFacetHandleFromNumber(i));
                        sb.Append(" | facet[" + i + "] type=" + (int)f.Type +
                                  " d1=" + F(f.Distance1) + " d2=" + F(f.Distance2) +
                                  " edge=" + f.EdgeIndex);
                    }
                    catch (System.Exception e) { sb.Append(" | facet[" + i + "]:" + e.Message); }
                }
                for (int i = 0; i < no; i++)
                {
                    try
                    {
                        PsOutlet o = em.get_Outlet(em.GetOutletHandleFromNumber(i));
                        sb.Append(" | outlet[" + i + "] type=" + (int)o.Type +
                                  " w=" + F(o.Width) + " h=" + F(o.Height));
                    }
                    catch (System.Exception e) { sb.Append(" | outlet[" + i + "]:" + e.Message); }
                }
                try
                {
                    PsEdgeChamfer be = em.PlateBreakEdge;
                    if (be != null)
                        sb.Append(" | breakEdge layout=" + (int)be.EdgeLayout +
                                  " top=" + be.Topside + "/" + (int)be.TopEdgeLayout +
                                  " tv=" + F(be.TopVar1) + "," + F(be.TopVar2) +
                                  " down=" + be.Downside + "/" + (int)be.DownEdgeLayout +
                                  " dv=" + F(be.DownVar1) + "," + F(be.DownVar2) +
                                  " flange=" + be.FlangeIndex +
                                  " desc='" + (be.Description ?? "") + "'" +
                                  " topDesc='" + (be.TopsideDescription ?? "") + "'" +
                                  " downDesc='" + (be.DownsideDescription ?? "") + "'" +
                                  " toString='" + (be.toString ?? "") + "'");
                }
                catch (System.Exception e) { sb.Append(" | breakEdge:" + e.Message); }
            }
            catch (System.Exception ex) { Result("EB_ERR mods EX:" + ex.Message); return; }
            Result("EB_OK mods handle=" + h + " " + sb.ToString());
        }

        // Edge processing along a plate EDGE -- manual B.13.3, the "six kinds".
        // PsEdgeChamfer has NO setter methods, only properties: assign them directly.
        // Manual: "Var1 is either the length of the first edge, the rounding radius or the
        // depth of the seam. Var2 is either the length of the second edge or the height of
        // the seam." So Var1/Var2 change MEANING with the layout -- another field whose name
        // does not tell you what it is.
        // WARNING: EdgeLayout values are NOT in the dump (names only). Sweep and measure them,
        // exactly as FacetType had to be -- assuming declaration order was wrong there.
        //   op=edgechamfer handle=<h> layout=<n> v1=<mm> [v2=<mm>] [side=top|down|both]
        void EdgeChamferOp(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string h = Get(kv, "handle", "");
            long oid = IdFromHandle(h);
            if (oid == 0) { Result("EB_ERR edgechamfer: bad handle " + h); return; }
            int layout = int.Parse(Get(kv, "layout", "1"));
            double v1 = double.Parse(Get(kv, "v1", "10"), IC);
            double v2 = double.Parse(Get(kv, "v2", Get(kv, "v1", "10")), IC);
            string side = Get(kv, "side", "both").ToLowerInvariant();
            int flange = int.Parse(Get(kv, "flange", "0"));

            string before = ModSig(oid), msg = "";
            int rc = -999;
            try
            {
                // mode=bind: fetch the object's OWN break-edge record and modify that.
                // PsEditModification.PlateBreakEdge is a read/WRITE property, so unlike the
                // facet case the reader may also be the writer here -- and a record obtained
                // from the object may carry the edge binding that a freshly constructed one
                // lacks. ProSteel's own complaint is "REQUESTED VOLUME SOLIDS CAN NOT BE
                // PRODUCED", which survived every dimension and side combination tried, so
                // the missing piece is most likely WHICH EDGES, not how big.
                bool bind = Get(kv, "mode", "") == "bind";
                PsEditModification emb = null;
                PsEdgeChamfer ec;
                if (bind)
                {
                    emb = new PsEditModification();
                    emb.SetObjectId(oid);
                    ec = emb.PlateBreakEdge;
                    if (ec == null) { Result("EB_ERR edgechamfer: PlateBreakEdge came back null"); return; }
                }
                else ec = new PsEdgeChamfer();
                ec.EdgeLayout = (EdgeLayout)layout;
                ec.TopEdgeLayout = (EdgeLayout)layout;
                ec.DownEdgeLayout = (EdgeLayout)layout;
                ec.Topside = (side == "top" || side == "both");
                ec.Downside = (side == "down" || side == "both");
                ec.TopVar1 = v1; ec.TopVar2 = v2;
                ec.DownVar1 = v1; ec.DownVar2 = v2;
                ec.FlangeIndex = flange;
                if (bind)
                {
                    emb.PlateBreakEdge = ec;      // write the modified record straight back
                    rc = -1;                      // property setter: no return to trust
                }
                else
                {
                    PsCutObjects cut = new PsCutObjects();
                    cut.SetToDefaults();
                    cut.SetObjectId(oid);
                    cut.SetAsPlateBreakEdgeCut(ec);
                    rc = cut.Apply();
                }
            }
            catch (System.Exception ex) { msg = " EX:" + ex.Message; }
            string after = ModSig(oid);
            Result((before != after ? "EB_OK" : "EB_ERR") + " edgechamfer handle=" + h +
                   " mode=" + Get(kv, "mode", "cut") +
                   " layout=" + layout + " v1=" + F(v1) + " v2=" + F(v2) + " side=" + side +
                   " applyRc=" + rc + " before[" + before + "] after[" + after + "]" + msg);
        }

        // An "outlet" is a milled-out notch / countersunk pocket -- manual p.200:
        // "you can insert simple geometrical shapes of outlets and countersunk parts into
        //  your shapes. You can create square, wedge-type, and circular shapes."
        // OutletType names mirror FacetType exactly, so the values are probably offset the
        // same way -- MEASURE, do not assume.
        //   op=outlet handle=<h> at=x,y,z type=<n> w=<mm> h=<mm> [radius=] [angle=] [normal=]
        void OutletOp(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string hh = Get(kv, "handle", "");
            long oid = IdFromHandle(hh);
            if (oid == 0) { Result("EB_ERR outlet: bad handle " + hh); return; }
            int type = int.Parse(Get(kv, "type", "0"));
            double w = double.Parse(Get(kv, "w", "40"), IC);
            double ht = double.Parse(Get(kv, "h", "40"), IC);
            double rad = double.Parse(Get(kv, "radius", "0"), IC);
            double ang = double.Parse(Get(kv, "angle", "0"), IC);
            PsPoint at = Pt(Get(kv, "at", "0,0,0"));
            double[] nz = Nums(Get(kv, "normal", "0,0,1"));

            string before = ModSig(oid), msg = "";
            int rc = -999;
            try
            {
                PsOutlet o = new PsOutlet();
                o.SetType((OutletType)type);
                o.SetInsertPoint(at);
                o.SetNormal(new PsVector(nz[0], nz.Length > 1 ? nz[1] : 0, nz.Length > 2 ? nz[2] : 1));
                o.SetWidth(w);
                o.SetHeight(ht);
                if (rad > 0) o.SetRadius(rad);
                if (ang != 0) o.SetOpenAngle(ang);
                PsCutObjects cut = new PsCutObjects();
                cut.SetToDefaults();
                cut.SetObjectId(oid);
                cut.SetAsOutletCut(o);
                rc = cut.Apply();
            }
            catch (System.Exception ex) { msg = " EX:" + ex.Message; }
            string after = ModSig(oid);
            Result((before != after ? "EB_OK" : "EB_ERR") + " outlet handle=" + hh +
                   " type=" + type + " w=" + F(w) + " h=" + F(ht) +
                   " applyRc=" + rc + " before[" + before + "] after[" + after + "]" + msg);
        }

        // A flat / diagonal cut by an infinite plane -- everything on the +normal side goes.
        //   op=planecut handle=<h> at=x,y,z normal=nx,ny,nz [flip=1]
        void PlaneCutOp(Dictionary<string, string> kv)
        {
            string hh = Get(kv, "handle", "");
            long oid = IdFromHandle(hh);
            if (oid == 0) { Result("EB_ERR planecut: bad handle " + hh); return; }
            PsPoint at = Pt(Get(kv, "at", "0,0,0"));
            double[] nz = Nums(Get(kv, "normal", "0,0,1"));

            string before = ModSig(oid), msg = "";
            double lenBefore = LengthOf(oid);
            int rc = -999;
            try
            {
                PsCutPlane cp = new PsCutPlane();
                cp.SetFromNormal(at, new PsVector(nz[0], nz.Length > 1 ? nz[1] : 0, nz.Length > 2 ? nz[2] : 1));
                if (Get(kv, "flip", "") == "1") cp.FlipNormal();
                PsCutObjects cut = new PsCutObjects();
                cut.SetToDefaults();
                cut.SetObjectId(oid);
                cut.SetAsPlaneCut(cp);
                rc = cut.Apply();
            }
            catch (System.Exception ex) { msg = " EX:" + ex.Message; }
            string after = ModSig(oid);
            double lenAfter = LengthOf(oid);
            bool changed = (before != after) || System.Math.Abs(lenAfter - lenBefore) > 0.01;
            Result((changed ? "EB_OK" : "EB_ERR") + " planecut handle=" + hh +
                   " applyRc=" + rc + " len=" + F(lenBefore) + "->" + F(lenAfter) +
                   " before[" + before + "] after[" + after + "]" + msg);
        }

        static double LengthOf(long oid)
        {
            try
            {
                PsObjectProperties p = new PsObjectProperties();
                p.readFrom(oid);
                return p.Length;
            }
            catch { return -1; }
        }

        // one compact signature of everything a part carries -- the before/after instrument
        static string ModSig(long oid)
        {
            try
            {
                PsEditModification em = new PsEditModification();
                em.SetObjectId(oid);
                string be = "-";
                try
                {
                    PsEdgeChamfer b = em.PlateBreakEdge;
                    if (b != null) be = ((int)b.EdgeLayout) + ":" + F(b.TopVar1) + "/" + F(b.TopVar2);
                }
                catch { }
                return "f" + em.FacetCount + " c" + em.CutPlaneCount + " hf" + em.HoleFieldCount +
                       " o" + em.OutletCount + " p" + em.PolyCutCount + " s" + em.SubBodyCount +
                       " be=" + be;
            }
            catch { return "?"; }
        }

        // ---- v52: CLONE THE DRILLING ONTO EVERY IDENTICAL PART ----
        // Manual B.4.5 "Clone" (p.113): "Use this command to transfer the manipulations
        // performed on a component ... to other components. A prerequisite for cloning is that
        // the parts have a position number and that these match ... only parts with the same
        // position number as the original part will be considered."
        // The manual lists five transferable kinds -- Cuts, Drill Holes, PolyCut, Notches,
        // Boolean. **Only ONE is exposed in the API**: PsDrillObject.TakeoverDrills. The
        // engine exists (the enum value kTakeOverModification is there) but is not surfaced.
        // Say so rather than implying Clone is available; drilling is the common case anyway.
        //
        // The position-number prerequisite is why this op could not exist before today:
        // op=posauto now assigns matching numbers to genuinely identical parts.
        //
        // ⚠️ COORDINATE SYSTEM, from the manual: "the transfer of the manipulations refers to
        // the coordinate system of the parts" -- a hole 100 mm from the right on a part whose
        // part-CS starts on the left lands 100 mm from the LEFT. Mirrored parts will receive
        // MIRRORED holes. Always check the result on a mirrored target before trusting a batch.
        //
        //   op=clonedrills src=<h> [to=<h,h,...>]   explicit targets
        //   op=clonedrills src=<h> posnum=1         every part sharing src's position number
        void CloneDrills(Dictionary<string, string> kv)
        {
            string srcH = Get(kv, "src", "");
            long src = IdFromHandle(srcH);
            if (src == 0) { Result("EB_ERR clonedrills: bad src handle " + srcH); return; }

            List<long> tgt = new List<long>();
            List<string> tgtH = new List<string>();
            string how;

            string toS = Get(kv, "to", "");
            if (toS.Length > 0)
            {
                how = "explicit";
                foreach (string one in toS.Split(','))
                {
                    long id = IdFromHandle(one.Trim());
                    if (id != 0 && id != src) { tgt.Add(id); tgtH.Add(one.Trim()); }
                }
            }
            else if (Get(kv, "posnum", "") == "1")
            {
                how = "posnum";
                string want = "";
                try
                {
                    Bentley.ProStructures.Property.PsObjectProperties p =
                        new Bentley.ProStructures.Property.PsObjectProperties();
                    p.readFrom(src);
                    want = p.Posnum ?? "";
                }
                catch { }
                if (want.Length == 0)
                {
                    Result("EB_ERR clonedrills: src has no position number -- run posauto first. " +
                           "The manual makes a matching position number a PREREQUISITE for cloning.");
                    return;
                }
                Document doc0 = Application.DocumentManager.MdiActiveDocument;
                using (Transaction tr = doc0.Database.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(doc0.Database.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                    foreach (ObjectId id in ms)
                    {
                        long oid;
                        DBObject o;
                        try { o = tr.GetObject(id, OpenMode.ForRead); oid = id.OldIdPtr.ToInt64(); }
                        catch { continue; }
                        if (oid == src) continue;
                        try
                        {
                            Bentley.ProStructures.Property.PsObjectProperties p2 =
                                new Bentley.ProStructures.Property.PsObjectProperties();
                            p2.readFrom(oid);
                            if ((p2.Posnum ?? "") == want)
                            { tgt.Add(oid); tgtH.Add(o.Handle.ToString()); }
                        }
                        catch { }
                    }
                    tr.Commit();
                }
                how = "posnum='" + want + "'";
            }
            else { Result("EB_ERR clonedrills: give to=<handles> or posnum=1"); return; }

            if (tgt.Count == 0) { Result("EB_ERR clonedrills: no targets (" + how + ")"); return; }

            // count holes BEFORE -- TakeoverDrills returns Void, so this is the only verdict
            string err;
            StringBuilder scratch = new StringBuilder();
            int srcHoles = HolesOf(src, 2, scratch, "src", out err);
            int[] before = new int[tgt.Count];
            for (int i = 0; i < tgt.Count; i++)
            { scratch.Length = 0; before[i] = HolesOf(tgt[i], 2, scratch, "t", out err); }

            string ex = "";
            try
            {
                Bentley.ProStructures.Drawing.PsSelection sSrc =
                    new Bentley.ProStructures.Drawing.PsSelection();
                sSrc.Initialize();
                sSrc.AddObject(src);
                Bentley.ProStructures.Drawing.PsSelection sTgt =
                    new Bentley.ProStructures.Drawing.PsSelection();
                sTgt.Initialize();
                for (int i = 0; i < tgt.Count; i++) sTgt.AddObject(tgt[i]);

                // INSTRUMENT THE STEP BEFORE THE ONE THAT FAILED. TakeoverDrills is Void, so
                // when it does nothing there is no way to tell "the call failed" from "the
                // selections I handed it were empty". Measure the selections themselves, and
                // check Find() actually locates the ids that were added.
                ex += " srcSel=" + sSrc.ObjectCount + " tgtSel=" + sTgt.ObjectCount +
                      " srcFind=" + sSrc.Find(src) +
                      " tgtFind0=" + (tgt.Count > 0 ? sTgt.Find(tgt[0]).ToString() : "-");

                // ---- RESULT OF THE INVESTIGATION, 06/08/2026 ----
                // PsDrillObject.TakeoverDrills(PsSelection, PsSelection) is the ONLY manipulation
                // transfer exposed in the whole API -- and it TRANSFERS NOTHING from code.
                // Five call sequences were tried with selections proven correct
                // (srcSel=1 tgtSel=3, Find() true for both, Apply() returning 0 = nothing to do):
                //   1 configure + takeover, no Apply      2 + Apply
                //   3 no SetToDefaults                    4 per-target subject
                //   5 selections built from AutoCAD's own pick set via GetCurrentSelections
                // All five: changed=0. There is no PS_CLONE command token in the manual either,
                // so Clone is a dialog-only feature. ⇒ DEFAULT IS variant 9, which does the job
                // by composing two things that DO work: read the holes, then make them again.
                // The failed variants are kept so this is not re-investigated from scratch.
                int variant = int.Parse(Get(kv, "variant", "9"));
                PsDrillObject d = new PsDrillObject();
                int applyRc = -999;
                switch (variant)
                {
                    case 1:  // as before: configure, takeover, no Apply
                        d.SetToDefaults();
                        d.SetObjectId(src);
                        d.TakeoverDrills(sSrc, sTgt);
                        break;
                    case 2:  // takeover CONFIGURES, Apply EXECUTES
                        d.SetToDefaults();
                        d.SetObjectId(src);
                        d.TakeoverDrills(sSrc, sTgt);
                        applyRc = d.Apply();
                        break;
                    case 3:  // no SetToDefaults -- it may be clearing the takeover
                        d.TakeoverDrills(sSrc, sTgt);
                        applyRc = d.Apply();
                        break;
                    case 5:  // AddObject may fill only the MANAGED array; build the selections
                             // the way a user does, through AutoCAD's own pick set.
                        {
                            Editor ed5 = Application.DocumentManager.MdiActiveDocument.Editor;
                            ed5.SetImpliedSelection(new ObjectId[] { new ObjectId(new System.IntPtr(src)) });
                            sSrc.Initialize(); sSrc.GetCurrentSelections(true);
                            ObjectId[] tids = new ObjectId[tgt.Count];
                            for (int i = 0; i < tgt.Count; i++) tids[i] = new ObjectId(new System.IntPtr(tgt[i]));
                            ed5.SetImpliedSelection(tids);
                            sTgt.Initialize(); sTgt.GetCurrentSelections(true);
                            ex += " viaPickSet src=" + sSrc.ObjectCount + " tgt=" + sTgt.ObjectCount;
                            d.SetToDefaults();
                            d.SetObjectId(src);
                            d.TakeoverDrills(sSrc, sTgt);
                            applyRc = d.Apply();
                            ed5.SetImpliedSelection(new ObjectId[0]);
                        }
                        break;
                    case 9:  // MY OWN implementation -- read the source holes, make them again
                        {   // on each target. Both halves are proven: PsSingleHoleArray reads,
                            // SetSingleHoleField + Apply creates.
                            //
                            // v58: now through the PART COORDINATE SYSTEM, which is what the
                            // manual says the real Clone uses: "the transfer of the manipulations
                            // refers to the coordinate system of the parts ... if you look at a
                            // shape whose parts coordinate system has its origin on the right
                            // side and you would like to transfer a drill hole to a part 100 mm
                            // from the right but its parts coordinate system originates from the
                            // left, this component will receive the new boring 100 mm from the
                            // left." PsObjectProperties.Origin/XAxis/YAxis/ZAxis give exactly
                            // that frame, so a ROTATED or MIRRORED copy now works instead of
                            // being refused -- and mirrored ribs are everywhere in real steel.
                            //
                            // Equality is checked on the part's OWN Length/Wide/Height, not on
                            // world extents: those are rotation-invariant, so a rotated twin
                            // passes while a genuinely different part still fails.
                            Bentley.ProStructures.Property.PsObjectProperties ps =
                                new Bentley.ProStructures.Property.PsObjectProperties();
                            ps.readFrom(src);
                            PsPoint sO = ps.Origin;
                            PsVector sX = ps.XAxis, sY = ps.YAxis, sZ = ps.ZAxis;
                            double sL = ps.Length, sW = ps.Wide, sH = ps.Height;

                            PsSingleHoleArray arr = new PsSingleHoleArray(src, LongHoleMode.kDoubleHole, false, false, false);
                            int nh = arr.Count;
                            int made = 0, skipped = 0;
                            for (int t = 0; t < tgt.Count; t++)
                            {
                                Bentley.ProStructures.Property.PsObjectProperties pt =
                                    new Bentley.ProStructures.Property.PsObjectProperties();
                                pt.readFrom(tgt[t]);
                                if (System.Math.Abs(pt.Length - sL) > 0.5 ||
                                    System.Math.Abs(pt.Wide   - sW) > 0.5 ||
                                    System.Math.Abs(pt.Height - sH) > 0.5)
                                { skipped++; ex += " " + tgtH[t] + ":SIZE-DIFFERS(" +
                                    F(pt.Length) + "x" + F(pt.Wide) + "x" + F(pt.Height) + ")-refused"; continue; }

                                PsPoint tO = pt.Origin;
                                PsVector tX = pt.XAxis, tY = pt.YAxis, tZ = pt.ZAxis;
                                for (int i = 0; i < nh; i++)
                                {
                                    PsPoint a0 = new PsPoint(0, 0, 0), b0 = new PsPoint(0, 0, 0);
                                    double dm = 0;
                                    arr.getHole(i, a0, b0, ref dm);
                                    // WCS -> source part frame
                                    double px = a0.x - sO.x, py = a0.y - sO.y, pz = a0.z - sO.z;
                                    double lx = px * sX.x + py * sX.y + pz * sX.z;
                                    double ly = px * sY.x + py * sY.y + pz * sY.z;
                                    double lz = px * sZ.x + py * sZ.y + pz * sZ.z;
                                    double dx = b0.x - a0.x, dy = b0.y - a0.y, dz = b0.z - a0.z;
                                    double nx = dx * sX.x + dy * sX.y + dz * sX.z;
                                    double ny = dx * sY.x + dy * sY.y + dz * sY.z;
                                    double nz = dx * sZ.x + dy * sZ.y + dz * sZ.z;
                                    // target part frame -> WCS
                                    PsPoint ip = new PsPoint(
                                        tO.x + lx * tX.x + ly * tY.x + lz * tZ.x,
                                        tO.y + lx * tX.y + ly * tY.y + lz * tZ.y,
                                        tO.z + lx * tX.z + ly * tY.z + lz * tZ.z);
                                    PsVector nv = new PsVector(
                                        nx * tX.x + ny * tY.x + nz * tZ.x,
                                        nx * tX.y + ny * tY.y + nz * tZ.y,
                                        nx * tX.z + ny * tY.z + nz * tZ.z);
                                    PsDrillObject dd = new PsDrillObject();
                                    dd.SetToDefaults();
                                    dd.SetObjectId(tgt[t]);
                                    dd.SetInsertPoint(ip);
                                    dd.SetNormal(nv);
                                    // dm is the measured HOLE diameter; workloose 0 so the clone
                                    // is that size and not that size plus a bolt clearance --
                                    // otherwise every clone would grow the hole.
                                    dd.SetHoleWorkloose(0);
                                    dd.SetSingleHoleField(dm);
                                    dd.Apply();
                                    made++;
                                }
                            }
                            ex += " selfClone holes=" + nh + " attempts=" + made + " refused=" + skipped;
                            applyRc = made;
                        }
                        break;
                    case 4:  // per target: the subject is the part being MODIFIED, not the source
                        for (int i = 0; i < tgt.Count; i++)
                        {
                            PsDrillObject di = new PsDrillObject();
                            di.SetToDefaults();
                            di.SetObjectId(tgt[i]);
                            Bentley.ProStructures.Drawing.PsSelection one =
                                new Bentley.ProStructures.Drawing.PsSelection();
                            one.Initialize();
                            one.AddObject(tgt[i]);
                            di.TakeoverDrills(sSrc, one);
                            applyRc = di.Apply();
                        }
                        break;
                }
                ex += " variant=" + variant + " applyRc=" + applyRc;
            }
            catch (System.Exception e) { ex += " EX:" + e.Message; }

            int changed = 0, matched = 0;
            StringBuilder rep = new StringBuilder();
            for (int i = 0; i < tgt.Count; i++)
            {
                scratch.Length = 0;
                int after = HolesOf(tgt[i], 2, scratch, "t", out err);
                if (after != before[i]) changed++;
                if (after == srcHoles) matched++;
                rep.Append(" ").Append(tgtH[i]).Append(":").Append(before[i]).Append("->").Append(after);
            }

            Result((changed > 0 ? "EB_OK" : "EB_ERR") + " clonedrills src=" + srcH +
                   " srcHoles=" + srcHoles + " targets=" + tgt.Count + " (" + how + ")" +
                   " changed=" + changed + " nowMatchSrc=" + matched + ex + " |" + rep.ToString());
        }

        // ---- v51: AUTOMATIC POSITION NUMBERING -- the grunt work, taken ----
        // Amir: "שתיקח ממני את העבודה השחורה שבמידול". This is it: find which parts are
        // genuinely identical, give each distinct part one number, give every copy the same
        // number. Repetition is his whole method -- fewer plate types, fewer cutting
        // drawings, fewer shop errors -- so the part that MUST be right is the equality test.
        //
        // The equality test is ProSteel's own: PsCompareDrawing.CheckTwoPartsAreEqual.
        // Measured 06/08/2026 on five ribs identical except for their corner cut:
        //     CheckTwoPartsAreEqual  straight vs arc -> different   (CORRECT)
        //     PsObjectProperties.IsEqualTo           -> EQUAL       (WRONG)
        // IsEqualTo compares the nominal property block and DOES NOT SEE MODIFICATIONS. Using
        // it would merge three different plates onto one position number and send the shop one
        // cutting drawing for three different parts. Never use IsEqualTo for this.
        //
        // Cost is O(n x clusters), not O(n^2): each part is compared only to one
        // representative per cluster already found.
        //
        //   op=posauto [prefix=P] [start=1] [tol=0.5] [kinds=shape,plate] [dry=1] [out=file]
        void PosAuto(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string prefix = Get(kv, "prefix", "P");
            int start = int.Parse(Get(kv, "start", "1"));
            double tol = double.Parse(Get(kv, "tol", "0.5"), IC);
            bool dry = Get(kv, "dry", "") == "1";
            string kinds = "," + Get(kv, "kinds", "shape,plate").ToLowerInvariant() + ",";
            string outName = Get(kv, "out", "eb_posauto.txt");

            DateTime t0 = DateTime.Now;
            List<long> ids = new List<long>();
            List<string> hnds = new List<string>();
            List<string> clsNames = new List<string>();

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    DBObject o = null;
                    try { o = tr.GetObject(id, OpenMode.ForRead); }
                    catch { continue; }
                    string cls = o.GetType().Name;                 // PsShape / PsPlate / PsBolt ...
                    string kind = cls.StartsWith("Ps") ? cls.Substring(2).ToLowerInvariant() : cls.ToLowerInvariant();
                    if (kinds.IndexOf("," + kind + ",", StringComparison.Ordinal) < 0) continue;
                    ids.Add(id.OldIdPtr.ToInt64());
                    try { hnds.Add(o.Handle.ToString()); } catch { hnds.Add("?"); }
                    clsNames.Add(cls);
                }
                tr.Commit();
            }

            if (ids.Count == 0)
            { Result("EB_ERR posauto: no parts matched kinds=" + Get(kv, "kinds", "shape,plate")); return; }

            Bentley.ProStructures.Miscellaneous.PsCompareDrawing cd =
                new Bentley.ProStructures.Miscellaneous.PsCompareDrawing();
            cd.SetTolerances(tol, 0.1);

            List<int> repOf = new List<int>();      // cluster index -> representative part index
            int[] cluster = new int[ids.Count];
            int comparisons = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                int found = -1;
                for (int c = 0; c < repOf.Count; c++)
                {
                    comparisons++;
                    bool eq = false;
                    try { eq = cd.CheckTwoPartsAreEqual(ids[repOf[c]], ids[i]); }
                    catch { eq = false; }
                    if (eq) { found = c; break; }
                }
                if (found < 0) { repOf.Add(i); found = repOf.Count - 1; }
                cluster[i] = found;
            }

            // write, then PROVE each one with a fresh read-back
            int written = 0, failed = 0;
            StringBuilder sb = new StringBuilder();
            sb.Append("HANDLE\tCLASS\tCLUSTER\tPOSNUM\tSTATE\n");
            for (int i = 0; i < ids.Count; i++)
            {
                string num = prefix + (start + cluster[i]).ToString(IC);
                string state = "dry";
                if (!dry)
                {
                    state = "FAILED";
                    try
                    {
                        Bentley.ProStructures.Property.PsObjectProperties p =
                            new Bentley.ProStructures.Property.PsObjectProperties();
                        p.readFrom(ids[i]);        // MANDATORY -- a blank block would erase Name
                        p.Posnum = num;
                        p.writeTo(ids[i]);
                        Bentley.ProStructures.Property.PsObjectProperties chk =
                            new Bentley.ProStructures.Property.PsObjectProperties();
                        chk.readFrom(ids[i]);
                        if (chk.Posnum == num) { state = "ok"; written++; } else { failed++; }
                    }
                    catch { failed++; }
                }
                sb.Append(hnds[i]).Append('\t').Append(clsNames[i]).Append('\t')
                  .Append(cluster[i]).Append('\t').Append(num).Append('\t').Append(state).Append('\n');
            }

            string path = Path.Combine(Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location), outName);
            try { File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true)); } catch { }

            double secs = (DateTime.Now - t0).TotalSeconds;
            Result((failed == 0 ? "EB_OK" : "EB_ERR") + " posauto parts=" + ids.Count +
                   " distinct=" + repOf.Count + " comparisons=" + comparisons +
                   " written=" + written + " failed=" + failed +
                   (dry ? " (DRY RUN -- nothing written)" : "") +
                   " secs=" + secs.ToString("0.0", IC) + " -> " + outName);
        }

        // ---- v49: POSITION NUMBERS FROM CODE, AND EQUAL-PART DETECTION ----
        // The positioning ENGINE really is locked behind the modal PS_POS dialog:
        // PsCreatePositioning has ~90 Set* configurators and ~60 Internal* step methods but
        // NO public Perform/Run/Execute/Apply/Create and no SetToDefaults -- and two of the
        // Internal* members (InternalPositioningOptions, InternalDisplay*Result) OPEN DIALOGS
        // and must never be called from here.
        //
        // But the three primitives the engine is built from are exposed separately:
        //   PsObjectProperties.Posnum/.Sendnum + writeTo(id)      -- write the number
        //   PsCompareDrawing.CheckTwoPartsAreEqual(id, id)        -- ProSteel's OWN equality
        //   PsCreatePositioning.ConvertNum2Posnum / GetNextPosnum -- the number format
        // So the numbering pass can be built here, dialog-free, out of their own parts.
        // Equality detection matters beyond numbering: "find the repetition" IS Amir's method.
        //
        //   op=posset handle=<h> pos=<string> [send=<string>]
        //   op=equal  a=<h> b=<h> [tol=<mm>] [filter=<int>]
        void PosSet(Dictionary<string, string> kv)
        {
            string h = Get(kv, "handle", "");
            long oid = IdFromHandle(h);
            if (oid == 0) { Result("EB_ERR posset: bad handle " + h); return; }
            string pos = Get(kv, "pos", "");
            string send = Get(kv, "send", "");
            if (pos.Length == 0 && send.Length == 0)
            { Result("EB_ERR posset: pos= or send= is required"); return; }

            string before = "";
            int rcRead = -1, rcWrite = -1;
            try
            {
                Bentley.ProStructures.Property.PsObjectProperties p =
                    new Bentley.ProStructures.Property.PsObjectProperties();
                // readFrom FIRST, always. PsObjectProperties is a DETACHED property block,
                // not a live handle -- writing a fresh one pushes a blank Name/Article/Style
                // over the object. This is the pitfall that makes the whole op dangerous.
                // The Int32 return is an ErrorStatus: 0 = eOk. The first version of this op
                // refused on 0 and so never wrote anything -- an invented success convention.
                // Note PsCutObjects.Apply() returns 1 on success and readFrom returns 0 on
                // success: THERE IS NO CONSISTENT CONVENTION ACROSS THESE CLASSES, which is
                // why the verdict below comes from a fresh read-back and nothing else.
                rcRead = p.readFrom(oid);
                before = "pos='" + p.Posnum + "' send='" + p.Sendnum + "' name='" + p.Name + "'";
                if (pos.Length > 0) p.Posnum = pos;
                if (send.Length > 0) p.Sendnum = send;
                rcWrite = p.writeTo(oid);
            }
            catch (System.Exception ex) { Result("EB_ERR posset EX:" + ex.Message); return; }

            // Verify with a BRAND NEW block: the one written from still holds the value in
            // memory and would happily confirm a write that never landed.
            try
            {
                Bentley.ProStructures.Property.PsObjectProperties chk =
                    new Bentley.ProStructures.Property.PsObjectProperties();
                chk.readFrom(oid);
                bool ok = (pos.Length == 0 || chk.Posnum == pos) &&
                          (send.Length == 0 || chk.Sendnum == send);
                Result((ok ? "EB_OK" : "EB_ERR") + " posset handle=" + h +
                       " rcRead=" + rcRead + " rcWrite=" + rcWrite +
                       " before[" + before + "]" +
                       " after[pos='" + chk.Posnum + "' send='" + chk.Sendnum +
                       "' name='" + chk.Name + "']");
            }
            catch (System.Exception ex) { Result("EB_ERR posset verify EX:" + ex.Message); }
        }

        void Equal(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            long a = IdFromHandle(Get(kv, "a", ""));
            long b = IdFromHandle(Get(kv, "b", ""));
            if (a == 0 || b == 0)
            { Result("EB_ERR equal: need two valid handles a= and b="); return; }
            double tol = double.Parse(Get(kv, "tol", "0.5"), IC);
            int filter = int.Parse(Get(kv, "filter", "0"));

            string r1 = "?", r2 = "?";
            try
            {
                Bentley.ProStructures.Miscellaneous.PsCompareDrawing cd =
                    new Bentley.ProStructures.Miscellaneous.PsCompareDrawing();
                cd.SetTolerances(tol, 0.1);
                r1 = cd.CheckTwoPartsAreEqual(a, b) ? "EQUAL" : "different";
            }
            catch (System.Exception ex) { r1 = "EX:" + ex.Message; }

            try
            {
                Bentley.ProStructures.Property.PsObjectProperties pa =
                    new Bentley.ProStructures.Property.PsObjectProperties();
                Bentley.ProStructures.Property.PsObjectProperties pb =
                    new Bentley.ProStructures.Property.PsObjectProperties();
                pa.readFrom(a); pb.readFrom(b);
                r2 = pa.IsEqualTo(pb, tol, filter) ? "EQUAL" : "different";
            }
            catch (System.Exception ex) { r2 = "EX:" + ex.Message; }

            Result("EB_OK equal a=" + Get(kv, "a", "") + " b=" + Get(kv, "b", "") +
                   " tol=" + F(tol) + " filter=" + filter +
                   " | CheckTwoPartsAreEqual=" + r1 + " | IsEqualTo=" + r2);
        }

        // ---- v48: REFUSE AN UNKNOWN PARAMETER ----
        // 06/08/2026: four plates were created with at="11000,0,0" and every one of them
        // landed on the origin, stacked. The plate op takes `center`, not `at` -- and the
        // dispatcher swallowed the unknown key without a word. Every op returned EB_OK.
        // That is the same silent-failure family as a Void-returning create method, and it
        // is the one thing this agent is not allowed to do. A misspelt parameter must be
        // as loud as a misspelt op.
        //
        // The table below is GENERATED from the source: for each op, the keys its own
        // method actually reads via Get(kv, "..."). Regenerate it whenever ops change --
        // a hand-maintained list would drift and start lying, which is worse than none.
        static readonly Dictionary<string, string> OpKeys = new Dictionary<string, string>()
        {
            { "anchor", "|at|dia|dir|embed|grout|kind|layer|plate|proud|style|thread|" },
            { "beam", "|ax|ay|catalog|layer|mirror|name|offx|offy|p1|p2|rot|" },
            { "bolt", "|dia|hosts|layer|len|p1|p2|style|" },
            { "boltfield", "|center|dia|gap|hosts|nx|ny|style|sx|sy|" },
            { "boltprobe", "|p1|p2|" },
            { "chamfer", "|at|d1|d2|edge|handle|list|type|" },
            { "clonemodel", "|dx|maxx|" },
            { "cmd", "|args|list|name|select|" },
            { "conn", "|at|beam|cope|dh|dv|group|holedia|kind|nh|nv|play|support|t|template|" },
            { "conn_bolted", "|at|dia|gap|nx|ny|pl|pt|pw|style|sx|sy|" },
            { "connbase", "|anchordetail|anchordia|anchordrill|anchorgrip|anchorgripdia|anchorkey|anchoroutside|anchors|dts|handle|holedia|hx|hy|l|shorten|t|template|w|" },
            { "connremove", "|delparts|handle|" },
            { "connscan", "|handle|maxx|out|" },
            { "connsplice", "|at|gap|handle|holedia|nhweb|nvweb|support|tfl|tweb|" },
            { "connstiff", "|at|fldist|handle|len|r|shape|t|template|webdist|" },
            { "conntemplates", "||" },
            { "copy", "|about|axis|handle|handles|rot|to|" },
            { "drill", "|at|bolttype|dia|flange|handle|hosts|htype|innercontour|n|play|rotslot|slot|" },
            { "drillfield", "|at|dia|handle|hosts|innercontour|n|play|slot|x|y|" },
            { "dumpcat", "|catalog|" },
            { "dumpfull", "|out|" },
            { "dumpfull2", "|out|" },
            { "dumpholes", "|lhm|maxx|out|" },
            { "dumpmodel", "|out|" },
            { "dumppoly", "|maxx|out|" },
            { "enumdump", "|types|" },
            { "group", "|kind|main|name|parts|query|" },
            { "hilite", "|clear|handle|" },
            { "holes", "|handle|lhm|" },
            { "learn_flush", "||" },
            { "learn_off", "||" },
            { "learn_on", "|log|" },
            { "learn_status", "||" },
            { "list", "||" },
            { "mirror", "|handle|handles|p1|p2|p3|" },
            { "miter", "|cut|other|type|" },
            { "ping", "||" },
            { "touchplane", "|a|b|tol|" },
            { "touchdrill", "|a|b|dia|play|tol|x|y|" },
            { "cutat", "|at|handle|mode|other|outside|type|" },
            { "polycut", "|at|depth|handle|inside|l|n|pts|r|radius|shape|size|w|xaxis|yaxis|" },
            { "collision", "|bolts|box|clean|minvol|" },
            { "mods", "|handle|" },
            { "edgechamfer", "|flange|handle|layout|mode|side|v1|v2|" },
            { "outlet", "|angle|at|h|handle|normal|radius|type|w|" },
            { "planecut", "|at|flip|handle|normal|" },
            { "clonedrills", "|posnum|src|to|variant|" },
            { "posauto", "|dry|kinds|out|prefix|start|tol|" },
            { "posset", "|handle|pos|send|" },
            { "equal", "|a|b|filter|tol|" },
            { "plate", "|center|ex|ey|ez|l|layer|normal|t|w|" },
            { "platepoly", "|handle|" },
            { "polyplate", "|layer|pts|t|" },
            { "posnum", "|out|" },
            { "props", "|handle|" },
            { "replicate", "|about|axis|box|handles|rot|to|" },
            { "rotate", "|about|axis|box|class|handles|layer|name|rot|" },
            { "sections", "|filter|" },
            { "setlayer", "|handle|layer|" },
            { "setpoly", "|handle|pts|" },
            { "styles", "|type|" },
            { "view", "|dir|" },
            { "whoami", "||" },
            { "workframe", "|at|x|y|" },
            { "zoom", "|all|handle|margin|" }
        };

        // op/dwg/reqid are consumed by the dispatcher itself, for every op.
        static string UnknownKeys(string op, Dictionary<string, string> kv)
        {
            string allowed;
            if (!OpKeys.TryGetValue(op, out allowed)) return null;
            List<string> bad = new List<string>();
            foreach (string k in kv.Keys)
            {
                if (k == "op" || k == "dwg" || k == "reqid") continue;
                if (allowed.IndexOf("|" + k + "|", StringComparison.Ordinal) < 0) bad.Add(k);
            }
            if (bad.Count == 0) return null;
            return string.Join(",", bad.ToArray()) +
                   "  -- op=" + op + " accepts: " + allowed.Trim('|').Replace("|", ",");
        }

        // ---- v47: SHOW THE USER WHAT I JUST DID ----
        // Amir watches the AutoCAD window while I model. Until now I had no way to
        // point the camera at my own work -- no zoom, no highlight. An operation that
        // succeeds outside the current view is indistinguishable from one that did
        // nothing at all, and that is exactly what happened with the first chamfer.
        // Native view control only: no LISP, and no widening of the Cmd allowlist.
        //
        //   op=zoom handle=<h[,h...]> [margin=0.25]
        //   op=zoom all=1             [margin=0.05]
        //   op=view dir=iso|sw|se|ne|nw|top|bottom|front|back|left|right
        //   op=hilite handle=<h[,h...]>      select them, so grips mark the spot
        //   op=hilite clear=1
        void Zoom(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;
            string h = Get(kv, "handle", "");
            if (Get(kv, "all", "") == "1") h = "";
            string marginS = Get(kv, "margin", "");
            double margin = marginS.Length > 0 ? double.Parse(marginS, IC) : (h.Length > 0 ? 0.25 : 0.05);
            Extents3d ext;
            string what;

            if (h.Length > 0)
            {
                Extents3d acc = new Extents3d();
                bool any = false;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (string one in h.Split(','))
                    {
                        long oid = IdFromHandle(one.Trim());
                        if (oid == 0) continue;
                        Entity en = tr.GetObject(new ObjectId(new System.IntPtr(oid)), OpenMode.ForRead) as Entity;
                        if (en == null) continue;
                        try
                        {
                            Extents3d e2 = en.GeometricExtents;
                            if (any) acc.AddExtents(e2); else { acc = e2; any = true; }
                        }
                        catch { }
                    }
                    tr.Commit();
                }
                if (!any) { Result("EB_ERR zoom: no geometric extents for handle=" + h); return; }
                ext = acc; what = "handle=" + h;
            }
            else
            {
                db.UpdateExt(true);
                ext = new Extents3d(db.Extmin, db.Extmax);
                what = "all";
            }

            double vw, vh;
            using (ViewTableRecord vtr = ed.GetCurrentView())
            {
                // WCS -> DCS, so the zoom is correct in an isometric view too and not
                // only in plan. Getting this wrong reads as "the object is missing".
                Matrix3d w2d =
                    (Matrix3d.Rotation(-vtr.ViewTwist, vtr.ViewDirection, vtr.Target) *
                     Matrix3d.Displacement(vtr.Target - Point3d.Origin) *
                     Matrix3d.PlaneToWorld(vtr.ViewDirection)).Inverse();
                ext.TransformBy(w2d);
                vw = ext.MaxPoint.X - ext.MinPoint.X;
                vh = ext.MaxPoint.Y - ext.MinPoint.Y;
                if (vw < 1e-6) vw = 1.0;
                if (vh < 1e-6) vh = 1.0;
                vw *= (1.0 + margin); vh *= (1.0 + margin);
                vtr.Width = vw;
                vtr.Height = vh;
                vtr.CenterPoint = new Point2d((ext.MinPoint.X + ext.MaxPoint.X) / 2.0,
                                              (ext.MinPoint.Y + ext.MaxPoint.Y) / 2.0);
                ed.SetCurrentView(vtr);
            }
            try { ed.UpdateScreen(); } catch { }
            Result("EB_OK zoom " + what + " w=" + F(vw) + " h=" + F(vh) + " margin=" + F(margin));
        }

        void View(Dictionary<string, string> kv)
        {
            string dir = Get(kv, "dir", "iso").ToLowerInvariant();
            Vector3d v;
            switch (dir)
            {
                case "top":    v = new Vector3d(0, 0, 1); break;
                case "bottom": v = new Vector3d(0, 0, -1); break;
                case "front":  v = new Vector3d(0, -1, 0); break;
                case "back":   v = new Vector3d(0, 1, 0); break;
                case "left":   v = new Vector3d(-1, 0, 0); break;
                case "right":  v = new Vector3d(1, 0, 0); break;
                case "sw":     v = new Vector3d(-1, -1, 1); break;
                case "ne":     v = new Vector3d(1, 1, 1); break;
                case "nw":     v = new Vector3d(-1, 1, 1); break;
                case "iso":
                case "se":     v = new Vector3d(1, -1, 1); break;
                default: Result("EB_ERR view: unknown dir=" + dir +
                                " (iso|sw|se|ne|nw|top|bottom|front|back|left|right)"); return;
            }
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            using (ViewTableRecord vtr = ed.GetCurrentView())
            {
                vtr.ViewDirection = v;
                vtr.ViewTwist = 0.0;
                ed.SetCurrentView(vtr);
            }
            try { ed.UpdateScreen(); } catch { }
            Result("EB_OK view dir=" + dir + " vector=" + F(v.X) + "," + F(v.Y) + "," + F(v.Z));
        }

        void Hilite(Dictionary<string, string> kv)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            if (Get(kv, "clear", "") == "1")
            {
                ed.SetImpliedSelection(new ObjectId[0]);
                try { ed.UpdateScreen(); } catch { }
                Result("EB_OK hilite cleared");
                return;
            }
            string h = Get(kv, "handle", "");
            List<ObjectId> ids = new List<ObjectId>();
            foreach (string one in h.Split(','))
            {
                long oid = IdFromHandle(one.Trim());
                if (oid != 0) ids.Add(new ObjectId(new System.IntPtr(oid)));
            }
            if (ids.Count == 0) { Result("EB_ERR hilite: no valid handles in '" + h + "'"); return; }
            ed.SetImpliedSelection(ids.ToArray());
            try { ed.UpdateScreen(); } catch { }
            Result("EB_OK hilite n=" + ids.Count + " handle=" + h);
        }

        // ---- v45: CHAMFER A CORNER -- three parameters, not a drawn polygon ----
        // Lesson 3 reshaped 214 ribs by replacing their whole contour with SetPolygon,
        // because no other way was known. Manual B.13.2 describes the real one:
        //   "Layout -- straight, convex or concave.
        //    Radius/1st Edge -- the radius, or the length of the FIRST edge.
        //    2nd Edge -- the length of the SECOND edge of the straight chamfer."
        // Amir's rib chamfer (80x80 off a 120x120 rib) is therefore exactly
        //   type=triangle  d1=80  d2=80
        //
        // FacetType (from the live enum dump, not from guessing):
        //   0 kFacetUndefined · 1 kFacetRectangle · 2 kFacetTriangle
        //   3 kFacetArc       · 4 kFacetInversArc
        // kFacetTriangle is the diagonal cut Amir uses.
        //
        // op=chamfer handle=<plate> at=x,y,z  d1=80 [d2=80] [type=2] [edge=<index>]
        // op=chamfer handle=<plate> list=1     -> report existing facets
        void Chamfer(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string h = Get(kv, "handle", "");
            long oid = IdFromHandle(h);
            if (oid == 0) { Result("EB_ERR chamfer: bad handle " + h); return; }

            bool listOnly = Get(kv, "list", "") == "1";
            double d1 = double.Parse(Get(kv, "d1", "0"), IC);
            double d2 = double.Parse(Get(kv, "d2", Get(kv, "d1", "0")), IC);
            int ftype = int.Parse(Get(kv, "type", "2"));       // 2 = kFacetTriangle
            string edgeS = Get(kv, "edge", "");
            string atS = Get(kv, "at", "");

            // measure the contour BEFORE: a chamfer must change the outline, and the
            // vertex count is the honest instrument -- not whatever Apply returns.
            int vBefore = ContourVertexCount(oid);
            int facetsBefore = -1;
            string msg = "";

            try
            {
                PsEditModification em = new PsEditModification();
                em.SetObjectId(oid);
                try { facetsBefore = em.FacetCount; } catch { }

                if (listOnly)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < facetsBefore; i++)
                    {
                        try
                        {
                            int hnd = em.GetFacetHandleFromNumber(i);
                            PsVertexChamfer f = em.get_Facet(hnd);
                            sb.Append(" [" + i + "] type=" + (int)f.Type +
                                      " d1=" + F(f.Distance1) + " d2=" + F(f.Distance2) +
                                      " edge=" + f.EdgeIndex);
                        }
                        catch (System.Exception e1) { sb.Append(" [" + i + "]:" + e1.Message); }
                    }
                    Result("EB_OK chamfer list handle=" + h + " facets=" + facetsBefore +
                           " verts=" + vBefore + sb.ToString());
                    return;
                }

                if (d1 <= 0) { Result("EB_ERR chamfer: d1= is required (the first edge length)"); return; }

                // PsEditModification READS and DELETES modifications; it does not create
                // them. Creation goes through PsCutObjects, which owns all ten cut types
                // including SetAsFacetCut and SetAsPlateBreakEdgeCut.
                PsVertexChamfer ch = new PsVertexChamfer();
                ch.SetType((FacetType)ftype);
                ch.SetDistance1(d1);
                ch.SetDistance2(d2);
                if (edgeS.Length > 0) { try { ch.SetEdgeIndex(short.Parse(edgeS)); } catch { } }
                if (atS.Length > 0)
                {
                    // select the corner by picking a point on it -- the manual's own way
                    try { ch.SetEdgePointId(oid, Pt(atS)); } catch (System.Exception e2) { msg += " edgepoint:" + e2.Message; }
                }
                PsCutObjects cut = new PsCutObjects();
                cut.SetToDefaults();
                cut.SetObjectId(oid);
                cut.SetAsFacetCut(ch);
                int rcCut = cut.Apply();
                msg += " applyRc=" + rcCut;
            }
            catch (System.Exception ex) { msg += " EX:" + ex.Message; }

            int vAfter = ContourVertexCount(oid);
            int facetsAfter = -1;
            try { PsEditModification em2 = new PsEditModification(); em2.SetObjectId(oid); facetsAfter = em2.FacetCount; } catch { }

            // Verdict from measurement: either the facet count rose or the contour changed.
            string tail = " handle=" + h + " type=" + ftype + " d1=" + F(d1) + " d2=" + F(d2) +
                          " facets=" + facetsBefore + "->" + facetsAfter +
                          " contourVerts=" + vBefore + "->" + vAfter + msg;
            if ((facetsAfter > facetsBefore && facetsBefore >= 0) || (vAfter != vBefore && vAfter > 0))
                Result("EB_OK chamfer" + tail);
            else
                Result("EB_ERR chamfer changed nothing." + tail);
        }

        // unique contour vertices of a plate -- the honest "did the outline change" probe
        static int ContourVertexCount(long oid)
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    DBObject o = tr.GetObject(new ObjectId(new System.IntPtr(oid)), OpenMode.ForRead);
                    PsPlate pl = o as PsPlate;
                    int n = -1;
                    if (pl != null)
                    {
                        PsPolygon pg = new PsPolygon();
                        pl.GetPolygon(pg);
                        n = pg.Count;
                    }
                    tr.Commit();
                    return n;
                }
            }
            catch { return -1; }
        }

        // ---- v42a: READ POSITION NUMBERS -- the before/after instrument --------
        // Manual B.29: positioning is not an output step, it is the PREREQUISITE for
        // Clone Manipulations ("only parts with the same position number are considered"),
        // for Compare+Modify, and for Equal Part Detection. Nothing in the plugin has ever
        // read Posnum, so there was no way to tell whether positioning had done anything.
        //
        // op=posnum [out=eb_posnum.txt]
        void Posnum(Dictionary<string, string> kv)
        {
            string outName = Get(kv, "out", "eb_posnum.txt");
            StringBuilder sb = new StringBuilder();
            int n = 0, withPos = 0;
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    DBObject o = null;
                    try { o = tr.GetObject(id, OpenMode.ForRead); }
                    catch { continue; }
                    string cls = o.GetType().Name;
                    string hnd = "";
                    try { hnd = o.Handle.ToString(); } catch { }
                    string pos = "", snd = "", nm = "", art = "";
                    double wt = 0;
                    try
                    {
                        PsObjectProperties pr = new PsObjectProperties();
                        // NOT SetObjectId -- PsObjectProperties reads with readFrom(id)
                        pr.readFrom(id.OldIdPtr.ToInt64());
                        try { pos = pr.Posnum; } catch { }
                        try { snd = pr.Sendnum; } catch { }
                        try { nm = pr.Name; } catch { }
                        try { art = pr.Article; } catch { }
                        try { wt = pr.Weight; } catch { }
                    }
                    catch { }
                    if (pos != null && pos.Length > 0) withPos++;
                    sb.Append("POS\t").Append(hnd).Append('\t').Append(cls).Append('\t')
                      .Append(Safe(nm)).Append('\t').Append(Safe(pos)).Append('\t')
                      .Append(Safe(snd)).Append('\t').Append(F(wt)).Append('\t')
                      .Append(Safe(art)).AppendLine();
                    n++;
                }
                tr.Commit();
            }
            File.WriteAllText(Path.Combine(Dir, outName), sb.ToString(), Encoding.UTF8);
            Result("EB_OK posnum objects=" + n + " withPosnum=" + withPos + " -> " + outName);
        }

        // ---- v42b: MIRROR -- never implemented, and 39 mirrors were missed in lesson 3
        // PsMiscTools.Mirror3d(Id, p1, p2, p3) mirrors a ProSteel object about the plane
        // through three points. PsShape.MirrorFlag is read-only (measured dead end), so
        // this is the only native route.
        //
        // op=mirror handles=h1,h2 p1=x,y,z p2=x,y,z p3=x,y,z
        void Mirror(Dictionary<string, string> kv)
        {
            string hs = Get(kv, "handles", Get(kv, "handle", ""));
            PsPoint a = Pt(Get(kv, "p1", "0,0,0"));
            PsPoint b = Pt(Get(kv, "p2", "1,0,0"));
            PsPoint c = Pt(Get(kv, "p3", "0,0,1"));
            string h0, c0; int before = Census(out h0, out c0);
            int done = 0, bad = 0; string msg = "";
            try
            {
                PsMiscTools mt = new PsMiscTools();
                foreach (string raw in hs.Split(new char[] { ',', ';' }))
                {
                    string t = raw.Trim();
                    if (t.Length == 0) continue;
                    long oid = IdFromHandle(t);
                    if (oid == 0) { bad++; continue; }
                    string g0 = GeomOf(oid);
                    try { mt.Mirror3d(oid, a, b, c); } catch (System.Exception e1) { bad++; msg += " " + t + ":" + e1.Message; continue; }
                    string g1 = GeomOf(oid);
                    if (g0 != g1) done++; else { bad++; msg += " " + t + ":no-geometry-change"; }
                }
            }
            catch (System.Exception ex) { msg += " EX:" + ex.Message; }
            string h1, c1; int after = Census(out h1, out c1);
            string tail = " mirrored=" + done + " failed=" + bad +
                          " census=" + before + "->" + after + msg;
            if (done > 0) Result("EB_OK mirror" + tail);
            else Result("EB_ERR mirror changed nothing." + tail);
        }

        // ---- v42c: NATIVE COPY -- PsMiscTools.ObjectsCopy -----------------------
        // replicate() went around this with Database.DeepCloneObjects + Matrix3d, at the
        // AutoCAD level. ProStructures has its own steel-aware copy; use it.
        //
        // op=copy handles=h1,h2 to=dx,dy,dz  [rot=deg] [axis=x|y|z] [about=x,y,z]
        void CopyNative(Dictionary<string, string> kv)
        {
            string hs = Get(kv, "handles", Get(kv, "handle", ""));
            double[] d = Nums(Get(kv, "to", "0,0,0"));
            string rotS = Get(kv, "rot", "");
            string axis = Get(kv, "axis", "z").ToLower();
            PsPoint about = Pt(Get(kv, "about", "0,0,0"));

            string h0, c0; int before = Census(out h0, out c0);
            int made = 0; string msg = "", newHandles = "";
            try
            {
                PsMiscTools mt = new PsMiscTools();
                PsMatrix m = new PsMatrix();
                if (rotS.Length > 0)
                {
                    double deg = double.Parse(rotS, System.Globalization.CultureInfo.InvariantCulture);
                    PsVector ax = axis == "x" ? new PsVector(1, 0, 0)
                                : axis == "y" ? new PsVector(0, 1, 0)
                                              : new PsVector(0, 0, 1);
                    m.SetToRotation(deg * System.Math.PI / 180.0, ax, about);
                }
                else
                {
                    m.SetToTranslation(new PsVector(d[0], d.Length > 1 ? d[1] : 0, d.Length > 2 ? d[2] : 0));
                }
                foreach (string raw in hs.Split(new char[] { ',', ';' }))
                {
                    string t = raw.Trim();
                    if (t.Length == 0) continue;
                    long oid = IdFromHandle(t);
                    if (oid == 0) continue;
                    long nid = 0;
                    try { nid = mt.ObjectCopy(oid, m); }
                    catch (System.Exception e1) { msg += " " + t + ":" + e1.Message; continue; }
                    if (nid != 0) { made++; newHandles += HandleOf(nid) + ";"; }
                }
            }
            catch (System.Exception ex) { msg += " EX:" + ex.Message; }
            string h1, c1; int after = Census(out h1, out c1);
            int delta = after - before;
            string tail = " copied=" + made + " census=" + before + "->" + after +
                          "(+" + delta + ")" + (newHandles.Length > 0 ? " new=" + newHandles : "") + msg;
            if (delta > 0) Result("EB_OK copy" + tail);
            else Result("EB_ERR copy produced nothing." + tail);
        }

        // ---- v41: run the SOFTWARE'S OWN COMMANDS, natively, with no LISP ------
        // Amir's standing rule: "אך ורק בתוכנה עצמה ובפקודות שיש בה" -- only the software
        // itself and the commands it has. Editor.Command(object[]) is exactly that: a
        // registered command token plus typed arguments. It lives in accoremgd.dll (NOT
        // acmgd.dll) and exists from AutoCAD 2015 onward -- the very version in use here.
        //
        // WHY THIS MATTERS: several capabilities have NO .NET class at all --
        // Check Groups (orphans, Compare+Modify), and positioning has only Internal*
        // methods whose call order would have to be GUESSED. Guessing an internal
        // sequence is "invention instead of inheritance", the root cause that produced
        // 428 mm anchors. Running the documented command is inheritance.
        //
        // SAFETY, in order:
        //   1. An explicit ALLOWLIST. Nothing runs that is not named here on purpose.
        //   2. No parentheses are ever emitted -- a command token and typed arguments
        //      only. Nothing here can become a LISP form.
        //   3. FILEDIA/CMDDIA are NOT touched: a command that wants a dialog must be
        //      seen to want one, not silently suppressed.
        //   4. A census + geometry delta is reported, so "it ran" is never assumed.
        //
        // op=cmd name=PS_POS [args=a|b|c] [select=h1,h2,...]
        // op=cmd list=1                      -> print the allowlist
        static readonly string[] CmdAllow = new string[] {
            // positioning
            "PS_POS", "PS_POS_SNG", "PS_POS_BGR", "PS_POS_DIFF",
            // quality gates
            "PS_COLLISION",
            // groups
            "PS_EXPLODE",
            // display / housekeeping (safe, no geometry change)
            "PS_REGEN",
            // connection editor
            "PS_EDIT_CONNECTIONS"
        };

        void Cmd(Dictionary<string, string> kv)
        {
            if (Get(kv, "list", "") == "1")
            {
                Result("EB_OK cmd allowlist=" + string.Join(",", CmdAllow));
                return;
            }
            string name = Get(kv, "name", "").Trim().ToUpper();
            string argsS = Get(kv, "args", "");
            string sel = Get(kv, "select", "");

            if (name.Length == 0) { Result("EB_ERR cmd: name= is required"); return; }
            bool allowed = false;
            foreach (string a in CmdAllow)
                if (string.Compare(a, name, System.StringComparison.OrdinalIgnoreCase) == 0) allowed = true;
            if (!allowed)
            {
                Result("EB_ERR cmd '" + name + "' is not on the allowlist. " +
                       "Add it deliberately in the plugin; nothing runs by accident. " +
                       "allowlist=" + string.Join(",", CmdAllow));
                return;
            }
            if (name.IndexOf('(') >= 0 || argsS.IndexOf('(') >= 0)
            {
                Result("EB_ERR cmd: parentheses are not permitted -- that would be a LISP form.");
                return;
            }

            string h0, c0; int before = Census(out h0, out c0);
            string msg = "";
            bool ran = false;
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc.Editor;

                // optional pre-selection, so a command that asks for objects gets them
                if (sel.Length > 0)
                {
                    System.Collections.Generic.List<ObjectId> ids =
                        new System.Collections.Generic.List<ObjectId>();
                    foreach (string raw in sel.Split(new char[] { ',', ';' }))
                    {
                        string hs = raw.Trim();
                        if (hs.Length == 0) continue;
                        long oid = IdFromHandle(hs);
                        if (oid != 0) ids.Add(new ObjectId(new System.IntPtr(oid)));
                    }
                    if (ids.Count > 0) { ed.SetImpliedSelection(ids.ToArray()); msg += " preselected=" + ids.Count; }
                }

                System.Collections.Generic.List<object> pars =
                    new System.Collections.Generic.List<object>();
                pars.Add(name);
                if (argsS.Length > 0)
                    foreach (string a in argsS.Split('|')) pars.Add(a);

                ed.Command(pars.ToArray());
                ran = true;
            }
            catch (System.Exception ex) { msg += " EX:" + ex.Message; }

            string h1, c1; int after = Census(out h1, out c1);
            string tail = " name=" + name + " ran=" + ran +
                          " census=" + before + "->" + after +
                          "(" + (after - before >= 0 ? "+" : "") + (after - before) + ")" + msg;
            if (ran) Result("EB_OK cmd" + tail);
            else Result("EB_ERR cmd did not execute." + tail);
        }

        // ---- v40: GROUPS -- the unit Amir actually models with -----------------
        // Manual B.28 (read in full 06/08/2026):
        //   "Certain functions apply to the COMPLETE GROUP, even if you only select one
        //    part of the group."
        //   "The group structure existing in the model is taken into account FOR THE PARTS
        //    LISTS and when the model is automatically detailed and transformed into 2D
        //    workshop drawings."
        //
        // This is the thing that was missing in lesson 5: the base plate did not rotate
        // with the column ("like talking to a wall") because the detail was a loose pile of
        // handles, not a group. And it is the prerequisite for the parts list and the
        // workshop drawings -- so it is not a modelling convenience, it is the unit of work.
        //
        // Three levels, and they mean different things on the shop floor (B.28.1):
        //   subgroup  = purchase / stock parts, preassembled
        //   group     = what SHIPS to site as one piece
        //   assembly  = what is combined ON SITE   (has NO main part)
        //
        // op=group main=<handle> parts=h1,h2,h3 [kind=group|subgroup|assembly] [name=...]
        // op=group query=<handle>          -> report the group a part belongs to
        void Group(Dictionary<string, string> kv)
        {
            string q = Get(kv, "query", "");
            string mainH = Get(kv, "main", "");
            string partsS = Get(kv, "parts", "");
            string kind = Get(kv, "kind", "group").ToLower();
            string name = Get(kv, "name", "");

            // ---- query mode: what group does this part belong to? ----
            if (q.Length > 0)
            {
                long qid = IdFromHandle(q);
                if (qid == 0) { Result("EB_ERR group query: bad handle " + q); return; }
                string info = "";
                try
                {
                    PsObjectGroup g = new PsObjectGroup();
                    g.Initialize();
                    g.GetGroupFrom(qid);
                    long mp = 0;
                    try { mp = g.getMainPartOf(qid); } catch { }
                    bool isMain = false;
                    try { isMain = g.IsMainPart(qid); } catch { }
                    int n = 0;
                    try { n = g.PartCount; } catch { }
                    int ns = 0;
                    try { ns = g.SubPartCount; } catch { }
                    string gn = "";
                    try { gn = g.get_Groupname(qid); } catch { }
                    double w = 0;
                    try { w = g.computeWeight(qid, false); } catch { }
                    double L = 0, W = 0, H = 0;
                    try { g.ComputeDimension(qid, ref L, ref W, ref H); } catch { }
                    info = " groupname='" + Safe(gn) + "' parts=" + n + " subparts=" + ns +
                           " isMain=" + isMain + " mainId=" + mp +
                           " weight=" + F(w) + " dims=" + F(L) + "x" + F(W) + "x" + F(H);
                }
                catch (System.Exception ex) { info = " EX:" + ex.Message; }
                Result("EB_OK group query handle=" + q + info);
                return;
            }

            // ---- create mode ----
            long mid = IdFromHandle(mainH);
            if (kind != "assembly" && mid == 0)
            {
                Result("EB_ERR group: a group/subgroup needs main=<handle>. " +
                       "Only an assembly may have no main part (manual B.28.1).");
                return;
            }

            string h0, c0; int before = Census(out h0, out c0);
            int added = 0, bad = 0;
            string msg = "", detail = "";
            bool made = false;
            try
            {
                PsObjectGroup g = new PsObjectGroup();
                g.Initialize();
                // Groupname is indexed by object id (get_Groupname(long)), so it is read,
                // not assigned on the group object. Naming is left to a later op.
                if (mid != 0) { try { g.setMainPart(mid); } catch (System.Exception e1) { msg += " main:" + e1.Message; } }

                foreach (string raw in partsS.Split(new char[] { ',', ';' }))
                {
                    string hs = raw.Trim();
                    if (hs.Length == 0) continue;
                    long pid = IdFromHandle(hs);
                    if (pid == 0) { bad++; detail += " " + hs + ":badhandle"; continue; }
                    if (pid == mid) continue;          // the main part is not an accessory
                    try { g.AddSubPart(pid); added++; }
                    catch (System.Exception e2) { bad++; detail += " " + hs + ":" + e2.Message; }
                }

                if (kind == "assembly")
                    made = g.CreateAssembly(new PsPoint(0, 0, 0), new PsVector(1, 0, 0), new PsVector(0, 1, 0));
                else if (kind == "subgroup")
                    made = g.CreateSubGroup();
                else
                    made = g.Create();
            }
            catch (System.Exception ex) { msg += " EX:" + ex.Message; }

            // Verify by READING THE GROUP BACK, not by trusting Create(). Same discipline
            // that caught the cope: the return value is not the measurement.
            int readback = -1;
            string rbName = "";
            try
            {
                PsObjectGroup g2 = new PsObjectGroup();
                g2.Initialize();
                long probe = mid != 0 ? mid : IdFromHandle(partsS.Split(',')[0].Trim());
                if (probe != 0)
                {
                    g2.GetGroupFrom(probe);
                    readback = g2.PartCount;
                    try { rbName = g2.get_Groupname(probe); } catch { }
                }
            }
            catch (System.Exception ex) { msg += " readback:" + ex.Message; }

            string h1, c1; int after = Census(out h1, out c1);
            string tail = " kind=" + kind + " main=" + mainH + " added=" + added +
                          " bad=" + bad + " create=" + made +
                          " groupPartCount=" + readback + " groupname='" + Safe(rbName) + "'" +
                          " census=" + before + "->" + after + detail + msg;
            // A group adds no entities, so the census is NOT the evidence -- the read-back is.
            if (readback > 0) Result("EB_OK group" + tail);
            else Result("EB_ERR group not readable after create." + tail);
        }

        // ---- v38: measure GEOMETRY, not only the census -----------------------
        // Measured 06/08/2026: a COPE shortened the beam 3900 -> 3850 and pulled its end
        // back 100 -> 150, while Create() returned FALSE and the census delta was 0.
        // Judging by census said "produced nothing" about an operation that had worked.
        // That is retrospective root cause #1 -- counting instead of measuring the
        // relationship -- committed AGAIN, after building a checker for exactly this.
        //
        // So every connection now reports what happened to the CONNECTED MEMBER too:
        // its length and its bounding box, before and after.
        static string GeomOf(long oid)
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    DBObject o = tr.GetObject(new ObjectId(new System.IntPtr(oid)), OpenMode.ForRead);
                    Entity e = o as Entity;
                    string len = "";
                    try
                    {
                        PsShape sh = o as PsShape;
                        if (sh != null) len = F(sh.Length);
                    }
                    catch { }
                    string ext = "";
                    try
                    {
                        Extents3d x = e.GeometricExtents;
                        ext = F(x.MinPoint.X) + "," + F(x.MinPoint.Y) + "," + F(x.MinPoint.Z) + ";" +
                              F(x.MaxPoint.X) + "," + F(x.MaxPoint.Y) + "," + F(x.MaxPoint.Z);
                    }
                    catch { }
                    tr.Commit();
                    return "L=" + len + " ext=" + ext;
                }
            }
            catch { return ""; }
        }

        // ---- v36: ONE generic op for all SIX beam-connection classes ---------
        // They share an identical surface, so one body covers them all:
        //   SetToDefaults -> GetTemplate(name) -> change ONLY what was sent ->
        //   SetConnectionObjectId(beam) + SetSupportObjectId(support) +
        //   SetConnectionPoint(pt) -> Check() -> Create() -> read the plates back.
        //
        // SetSupportObjectId is what a base plate does NOT have: a beam connection
        // knows TWO members. Getting that pair the wrong way round looks plausible
        // either way, so the op always reports which id went where.
        //
        // Discipline carried over from v30/v32: A PARAMETER THAT IS NOT SENT TOUCHES
        // NOTHING. Everything else comes from the factory template, never from me.
        //
        // op=conn kind=shear|webangle|endplate|cope|haunch|purlin
        //         beam=<handle> support=<handle> [at=x,y,z] [template=<name>]
        //         [holedia=] [play=] [nh=] [nv=] [dh=] [dv=] [t=] [cope=0|1] [group=0|1]
        void Conn(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string kind = Get(kv, "kind", "shear").ToLower();
            string bh = Get(kv, "beam", "");
            string sh = Get(kv, "support", "");
            string tmpl = Get(kv, "template", "");
            string atS = Get(kv, "at", "");

            string sHd = Get(kv, "holedia", ""), sPlay = Get(kv, "play", "");
            string sNh = Get(kv, "nh", ""), sNv = Get(kv, "nv", "");
            string sDh = Get(kv, "dh", ""), sDv = Get(kv, "dv", "");
            string sT = Get(kv, "t", ""), sCope = Get(kv, "cope", ""), sGrp = Get(kv, "group", "");

            long bId = IdFromHandle(bh);
            long sId = sh.Length > 0 ? IdFromHandle(sh) : 0;
            bool hasAt = atS.Length > 0;
            PsPoint at = hasAt ? Pt(atS) : new PsPoint(0, 0, 0);

            string h0, c0; int before = Census(out h0, out c0);
            string geomBefore = bId != 0 ? GeomOf(bId) : "";
            int rc = -999; bool made = false; string msg = ""; int plates = -1;
            string plateIds = "";

            try
            {
                if (kind == "shear")
                {
                    PsShearPlateConnection c = new PsShearPlateConnection();
                    c.SetToDefaults();
                    PsShearPlateLinkDataMgd d = null;
                    if (tmpl.Length > 0) { try { d = c.GetTemplate(tmpl); } catch (System.Exception e2) { msg += " tmpl:" + e2.Message; } }
                    if (d != null)
                    {
                        if (sT.Length > 0) d.PlateThickness = double.Parse(sT, IC);
                        if (sHd.Length > 0) d.HoleDiameter = double.Parse(sHd, IC);
                        if (sPlay.Length > 0) d.HoleWorkLoose = double.Parse(sPlay, IC);
                        if (sNh.Length > 0) d.HorizontalHoleCount = int.Parse(sNh);
                        if (sNv.Length > 0) d.VerticalHoleCount = int.Parse(sNv);
                        if (sDh.Length > 0) d.HoleDistanceHorizontal = double.Parse(sDh, IC);
                        if (sDv.Length > 0) d.HoleDistanceVertical = double.Parse(sDv, IC);
                        if (sCope.Length > 0) d.CreateCope = (sCope == "1");
                        if (sGrp.Length > 0) d.CreateGroup = (sGrp == "1");
                        c.SetConnectionData(d);
                    }
                    c.SetConnectionObjectId(bId);
                    if (sId != 0) c.SetSupportObjectId(sId);
                    if (hasAt) c.SetConnectionPoint(at);
                    try { rc = c.Check(); } catch (System.Exception e3) { msg += " check:" + e3.Message; }
                    made = c.Create();
                    try
                    {
                        plates = c.get_PlateDataCount();
                        for (int i = 0; i < plates && i < 8; i++) plateIds += c.GetPlateId(i) + ";";
                    }
                    catch { }
                }
                else if (kind == "webangle")
                {
                    PsWebAngleConnection c = new PsWebAngleConnection();
                    c.SetToDefaults();
                    PsWebAngleLinkDataMgd d = null;
                    if (tmpl.Length > 0) { try { d = c.GetTemplate(tmpl); } catch (System.Exception e2) { msg += " tmpl:" + e2.Message; } }
                    if (d != null)
                    {
                        if (sHd.Length > 0) d.HoleDiameter = double.Parse(sHd, IC);
                        if (sPlay.Length > 0) d.HoleWorkLoose = double.Parse(sPlay, IC);
                        if (sCope.Length > 0) d.CreateCope = (sCope == "1");
                        if (sGrp.Length > 0) d.CreateGroup = (sGrp == "1");
                        c.SetConnectionData(d);
                    }
                    c.SetConnectionObjectId(bId);
                    if (sId != 0) c.SetSupportObjectId(sId);
                    if (hasAt) c.SetConnectionPoint(at);
                    try { rc = c.Check(); } catch (System.Exception e3) { msg += " check:" + e3.Message; }
                    made = c.Create();
                    try
                    {
                        plates = c.get_PlateDataCount();
                        for (int i = 0; i < plates && i < 8; i++) plateIds += c.GetPlateId(i) + ";";
                    }
                    catch { }
                }
                else if (kind == "endplate")
                {
                    PsStandardPlateConnection c = new PsStandardPlateConnection();
                    c.SetToDefaults();
                    PsStandardPlateLinkData d = null;
                    if (tmpl.Length > 0) { try { d = c.GetTemplate(tmpl); } catch (System.Exception e2) { msg += " tmpl:" + e2.Message; } }
                    if (d != null)
                    {
                        if (sT.Length > 0) d.Thickness = double.Parse(sT, IC);
                        if (sHd.Length > 0) d.HoleDiameter = double.Parse(sHd, IC);
                        if (sPlay.Length > 0) d.HoleWorkLoose = double.Parse(sPlay, IC);
                        if (sNh.Length > 0) d.HorizontalHoleCount = int.Parse(sNh);
                        if (sNv.Length > 0) d.VerticalHoleCount = int.Parse(sNv);
                        if (sGrp.Length > 0) d.CreateGroup = (sGrp == "1");
                        c.SetConnectionData(d);
                    }
                    c.SetConnectionObjectId(bId);
                    if (sId != 0) c.SetSupportObjectId(sId);
                    if (hasAt) c.SetConnectionPoint(at);
                    try { rc = c.Check(); } catch (System.Exception e3) { msg += " check:" + e3.Message; }
                    made = c.Create();
                    try
                    {
                        plates = c.get_PlateDataCount();
                        for (int i = 0; i < plates && i < 8; i++) plateIds += c.GetPlateId(i) + ";";
                    }
                    catch { }
                }
                else if (kind == "cope")
                {
                    // NOTE: PsCopeConnection has NO SetConnectionPoint.
                    PsCopeConnection c = new PsCopeConnection();
                    c.SetToDefaults();
                    PsCopeLinkDataMgd d = null;
                    if (tmpl.Length > 0) { try { d = c.GetTemplate(tmpl); } catch (System.Exception e2) { msg += " tmpl:" + e2.Message; } }
                    if (d != null) c.SetConnectionData(d);
                    c.SetConnectionObjectId(bId);
                    if (sId != 0) c.SetSupportObjectId(sId);
                    try { rc = c.Check(); } catch (System.Exception e3) { msg += " check:" + e3.Message; }
                    made = c.Create();
                    try { plates = c.get_PlateDataCount(); } catch { }
                }
                else if (kind == "haunch")
                {
                    PsHaunchConnection c = new PsHaunchConnection();
                    c.SetToDefaults();
                    PsHaunchLinkDataMgd d = null;
                    if (tmpl.Length > 0) { try { d = c.GetTemplate(tmpl); } catch (System.Exception e2) { msg += " tmpl:" + e2.Message; } }
                    if (d != null)
                    {
                        if (sGrp.Length > 0) d.CreateGroup = (sGrp == "1");
                        c.SetConnectionData(d);
                    }
                    c.SetConnectionObjectId(bId);
                    if (sId != 0) c.SetSupportObjectId(sId);
                    if (hasAt) c.SetConnectionPoint(at);
                    try { rc = c.Check(); } catch (System.Exception e3) { msg += " check:" + e3.Message; }
                    made = c.Create();
                    try { plates = c.get_PlateDataCount(); } catch { }
                }
                else if (kind == "purlin")
                {
                    PsPurlinConnection c = new PsPurlinConnection();
                    c.SetToDefaults();
                    PsPurlinLinkDataMgd d = null;
                    if (tmpl.Length > 0) { try { d = c.GetTemplate(tmpl); } catch (System.Exception e2) { msg += " tmpl:" + e2.Message; } }
                    if (d != null)
                    {
                        if (sHd.Length > 0) d.HoleDiameter = double.Parse(sHd, IC);
                        if (sGrp.Length > 0) d.CreateGroup = (sGrp == "1");
                        c.SetConnectionData(d);
                    }
                    c.SetConnectionObjectId(bId);
                    if (sId != 0) c.SetSupportObjectId(sId);
                    if (hasAt) c.SetConnectionPoint(at);
                    try { rc = c.Check(); } catch (System.Exception e3) { msg += " check:" + e3.Message; }
                    made = c.Create();
                    try { plates = c.get_PlateDataCount(); } catch { }
                }
                else { msg += " unknown kind"; }
            }
            catch (System.Exception ex) { msg += " EX:" + ex.Message; }

            string h1, c1; int after = Census(out h1, out c1);
            int delta = after - before;
            string geomAfter = bId != 0 ? GeomOf(bId) : "";
            bool geomChanged = geomBefore.Length > 0 && geomBefore != geomAfter;

            // A connection can act in TWO ways and only one of them adds objects:
            //   plate/angle/endplate/haunch -> new parts      -> census delta > 0
            //   cope                        -> MODIFIES the beam -> delta stays 0
            // Judging by the census alone reported "produced nothing" about a cope that
            // had shortened the beam 3900 -> 3850. So the verdict is: EITHER signal.
            string tail = " kind=" + kind + " beamId=" + bId + " supId=" + sId +
                          " rc=" + rc + " create=" + made +
                          " delta=" + delta + " plates=" + plates +
                          " before=" + before + " after=" + after +
                          " geomChanged=" + geomChanged +
                          " beamBefore[" + geomBefore + "] beamAfter[" + geomAfter + "]" +
                          (plateIds.Length > 0 ? " plateIds=" + plateIds : "") + msg;
            if (delta > 0 || geomChanged) Result("EB_OK conn" + tail);
            else Result("EB_ERR conn changed nothing (no new objects AND no geometry change)." + tail);
        }

        // ---- v35: enumerate the installed BOLT/FASTENER STYLES ---------------
        // op=anchor created nothing at every style name I guessed. Guessing is the
        // banned move -- PsObjectStyleList enumerates what is actually installed, and
        // PsCreateBoltStyle.BoltDatabase says which .mdb each style comes from.
        // Duebel.mdb (= Duebel, anchors) and Gewinde-Kopfbolzen.mdb (headed studs) are
        // the two catalogues an anchor must come from.
        void Styles(Dictionary<string, string> kv)
        {
            StringBuilder sb = new StringBuilder();
            int n = 0;
            try
            {
                // The list has a Type, and it was never set -- an uninitialised Type is why
                // this returned 0 entries every time. Sweep all five and report each, instead
                // of picking one and calling the result "no styles installed".
                int wantType = int.Parse(Get(kv, "type", "-1"));
                PsObjectStyleList lst = new PsObjectStyleList();
                string[] tn = { "kBoltStyleList", "kWeldStyleList", "kPosFlagStyleList",
                                "kKoteFlagStyleList", "kUniversalStyleList" };
                for (int ti = 0; ti < 5; ti++)
                {
                    if (wantType >= 0 && ti != wantType) continue;
                    try
                    {
                        PsObjectStyleList probe = new PsObjectStyleList();
                        probe.Type = (ObjectStyleListType)ti;
                        probe.Initialize();
                        int rcRead = -999;
                        try { rcRead = probe.ReadFromFile(); } catch { }
                        try { probe.Synchronize(true); } catch { }
                        sb.AppendLine("PROBE type=" + ti + " " + tn[ti] +
                                      " count=" + probe.Count + " readRc=" + rcRead +
                                      " dict=" + Safe(probe.DictionaryName) +
                                      " folder=" + Safe(probe.FolderName) +
                                      " file=" + Safe(probe.FileName));
                        if (probe.Count > n) { lst = probe; n = probe.Count; }
                    }
                    catch (System.Exception e) { sb.AppendLine("PROBE type=" + ti + " EX:" + e.Message); }
                }
                if (n == 0) { lst.Type = ObjectStyleListType.kBoltStyleList; lst.Initialize(); }
                n = lst.Count;
                sb.AppendLine("STYLE LIST count=" + n +
                              " dict=" + Safe(lst.DictionaryName) +
                              " folder=" + Safe(lst.FolderName) +
                              " file=" + Safe(lst.FileName));
                for (int i = 0; i < n; i++)
                {
                    string nm = "";
                    // Entry is an indexed property -> the C# accessor is get_Entry(short)
                    try { nm = lst.get_Entry((short)i); } catch { }
                    string db = "", dia = "";
                    try
                    {
                        PsCreateBoltStyle bs = new PsCreateBoltStyle();
                        bs.SetToDefaults();
                        bs.ReadFrom(nm);
                        db = bs.BoltDatabase;
                        dia = F(bs.BoltLenAdd);
                    }
                    catch { }
                    sb.AppendLine("  [" + i + "] " + nm + "   db=" + db + "   lenAdd=" + dia);
                }
            }
            catch (System.Exception ex) { sb.AppendLine("STYLES ERR " + ex.Message); }
            File.WriteAllText(Path.Combine(Dir, "eb_styles.txt"), sb.ToString(), Encoding.UTF8);
            Result("EB_OK styles count=" + n + " -> eb_styles.txt");
        }

        // ---- v34: ANCHOR BOLTS AS REAL OBJECTS -------------------------------
        // Measured 06/08/2026: CreateSingleBolt has a hard grip ceiling (M20/DIN6914
        // fails above ~100 mm) and is declared Void, so it fails SILENTLY. That is the
        // whole story behind ~400 "bolt failures" and the 428 mm anchors. An anchor rod
        // is NOT a bolt -- PsCreateFastener is the class built for it, and it takes
        // embedment, plate thickness and grout thickness as named arguments.
        //
        // op=anchor at=x,y,z dir=dx,dy,dz dia=20 embed=120 plate=20 grout=0
        //            proud=<mm above plate>  thread=<len>  kind=straight|hook|bend|head
        //            style=<bolt style>  layer=<layer>
        void Anchor(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            PsPoint at = Pt(Get(kv, "at", "0,0,0"));
            double[] dv = Nums(Get(kv, "dir", "0,0,1"));
            PsVector dir = new PsVector(dv[0], dv.Length > 1 ? dv[1] : 0.0,
                                                dv.Length > 2 ? dv[2] : 1.0);
            double dia    = double.Parse(Get(kv, "dia", "20"), IC);
            double embed  = double.Parse(Get(kv, "embed", "120"), IC);
            double plate  = double.Parse(Get(kv, "plate", "20"), IC);
            double grout  = double.Parse(Get(kv, "grout", "0"), IC);
            double proud  = double.Parse(Get(kv, "proud", "0"), IC);
            double thread = double.Parse(Get(kv, "thread", "0"), IC);
            string kind   = Get(kv, "kind", "straight").ToLower();
            string style  = Get(kv, "style", "");
            string layer  = Get(kv, "layer", "");

            string h0, c0; int before = Census(out h0, out c0);
            long oid = 0;
            string msg = "";
            try
            {
                PsCreateFastener f = new PsCreateFastener();
                if (layer.Length > 0) { try { f.SetLayer(layer); } catch { } }

                // Orient by point + normal. PsVector has no CrossProduct() that RETURNS a
                // vector (only SetFromCrossProduct, which is void) -- and SetFromPointAndNormal
                // does exactly this job, so build nothing by hand.
                try
                {
                    dir.Normalize();
                    PsMatrix m = new PsMatrix();
                    m.SetFromPointAndNormal(at, dir);
                    f.SetInsertMatrix(m);
                }
                catch (System.Exception ex) { msg += " matrix:" + ex.Message; }

                if (kind == "hook")
                    oid = f.CreateFastenerHookAnchorBolt(dia, proud, embed, thread, plate, grout,
                                                         dia * 3.0, dia * 4.0, style);
                else if (kind == "bend")
                    oid = f.CreateFastenerBendAnchorBolt(dia, proud, embed, thread, plate, grout,
                                                         dia * 3.0, dia * 4.0, style);
                else if (kind == "head")
                    oid = f.CreateFastenerHeadBolt(dia, proud, embed, thread, plate, grout,
                                                   dia * 1.8, dia * 0.8, style);
                else
                    oid = f.CreateFastenerStraightAnchorBolt(dia, proud, embed, 0.0, 0.0,
                                                             thread, 0.0, plate, grout, style);
            }
            catch (System.Exception ex) { msg += " EX:" + ex.Message; }

            string h1, c1; int after = Census(out h1, out c1);
            int made = after - before;
            // Honest status: derived from the measured census delta, never from a return code.
            if (made > 0)
                Result("EB_OK anchor kind=" + kind + " dia=" + F(dia) + " embed=" + F(embed) +
                       " made=" + made + " id=" + oid + " before=" + before + " after=" + after + msg);
            else
                // CreateFastener* returns Int64 -- and that return IS the new ObjectId, with
                // 0 meaning the factory refused. This branch used to report only the census
                // delta and drop `oid`, i.e. it threw away the software's own answer and
                // then complained that the software said nothing. Report it.
                Result("EB_ERR anchor created nothing. kind=" + kind + " dia=" + F(dia) +
                       " returnedId=" + oid + " (0 = the fastener factory refused)" +
                       " before=" + before + " after=" + after + msg);
        }

        void ConnTemplates(Dictionary<string, string> kv)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                PsBasePlateConnection bp = new PsBasePlateConnection();
                bp.SetToDefaults();
                int n = bp.GetTemplateCount();
                sb.AppendLine("BASEPLATE templates: " + n);
                for (int i = 0; i < n; i++)
                {
                    string nm = "?";
                    try { nm = bp.GetTemplateName(i); } catch { }
                    sb.Append("  [" + i + "] " + nm);
                    try
                    {
                        PsBaseplateLinkDataMgd d = bp.GetTemplate(nm);
                        if (d != null)
                            sb.Append("  L=" + F(d.Length) + " W=" + F(d.Width) + " t=" + F(d.Thickness)
                                + " holeDia=" + F(d.HoleDiameter) + " hx=" + F(d.HoleDistanceHorizontal)
                                + " hy=" + F(d.HoleDistanceVertical) + " anchors=" + (d.AnchorBolts ? "1" : "0"));
                    }
                    catch { }
                    sb.AppendLine();
                }
            }
            catch (System.Exception ex) { sb.AppendLine("BASEPLATE ERR " + ex.Message); }
            try
            {
                PsStiffenerConnection st = new PsStiffenerConnection();
                st.SetToDefaults();
                int n = st.GetTemplateCount();
                sb.AppendLine("RIB (stiffener) templates: " + n);
                for (int i = 0; i < n; i++)
                {
                    string nm = "?";
                    try { nm = st.GetTemplateName(i); } catch { }
                    sb.Append("  [" + i + "] " + nm);
                    try
                    {
                        PsStiffenerLinkDataMgd d = st.GetTemplate(nm);
                        if (d != null)
                            sb.Append("  t=" + F(d.Thickness) + " len=" + F(d.Length) + " shape=" + d.ShapeType
                                + " r=" + F(d.Radius) + " flDist=" + F(d.FlangeDistance));
                    }
                    catch { }
                    sb.AppendLine();
                }
            }
            catch (System.Exception ex) { sb.AppendLine("RIB ERR " + ex.Message); }
            try
            {
                PsSpliceJointConnection sp = new PsSpliceJointConnection();
                sp.SetToDefaults();
                int n = sp.GetTemplateCount();
                sb.AppendLine("SPLICE templates: " + n);
                for (int i = 0; i < n; i++)
                {
                    string nm = "?";
                    try { nm = sp.GetTemplateName(i); } catch { }
                    sb.AppendLine("  [" + i + "] " + nm);
                }
            }
            catch (System.Exception ex) { sb.AppendLine("SPLICE ERR " + ex.Message); }

            // v33: the SIX beam-connection classes -- never enumerated before.
            // All nine share the identical shape, so one generic body covers them.
            // This is the factory's own vocabulary; anything present here must be
            // INHERITED via GetTemplate(), never re-typed by hand.
            try
            {
                sb.AppendLine();
                sb.AppendLine("---- BEAM CONNECTIONS (v33) ----");

                PsShearPlateConnection shp = new PsShearPlateConnection();
                shp.SetToDefaults();
                int nsh = shp.GetTemplateCount();
                sb.AppendLine("SHEARPLATE templates: " + nsh);
                for (int i = 0; i < nsh; i++)
                {
                    string nm = ""; try { nm = shp.GetTemplateName(i); } catch { }
                    string extra = "";
                    try
                    {
                        PsShearPlateLinkDataMgd d = shp.GetTemplate(nm);
                        if (d != null)
                            extra = "  t=" + F(d.PlateThickness) + " holeDia=" + F(d.HoleDiameter) +
                                    " nH=" + d.HorizontalHoleCount + " nV=" + d.VerticalHoleCount +
                                    " dH=" + F(d.HoleDistanceHorizontal) + " dV=" + F(d.HoleDistanceVertical) +
                                    " play=" + F(d.HoleWorkLoose) + " cope=" + d.CreateCope +
                                    " group=" + d.CreateGroup;
                    }
                    catch { }
                    sb.AppendLine("  [" + i + "] " + nm + extra);
                }

                PsWebAngleConnection wa = new PsWebAngleConnection();
                wa.SetToDefaults();
                int nwa = wa.GetTemplateCount();
                sb.AppendLine("WEBANGLE templates: " + nwa);
                for (int i = 0; i < nwa; i++)
                {
                    string nm = ""; try { nm = wa.GetTemplateName(i); } catch { }
                    string extra = "";
                    try
                    {
                        PsWebAngleLinkDataMgd d = wa.GetTemplate(nm);
                        if (d != null)
                            extra = "  holeDia=" + F(d.HoleDiameter) + " play=" + F(d.HoleWorkLoose) +
                                    " flat=" + d.WebAngleIsFlatSteel + " cope=" + d.CreateCope +
                                    " group=" + d.CreateGroup;
                    }
                    catch { }
                    sb.AppendLine("  [" + i + "] " + nm + extra);
                }

                PsStandardPlateConnection sp2 = new PsStandardPlateConnection();
                sp2.SetToDefaults();
                int nsp = sp2.GetTemplateCount();
                sb.AppendLine("ENDPLATE/STANDARDPLATE templates: " + nsp);
                for (int i = 0; i < nsp; i++)
                {
                    string nm = ""; try { nm = sp2.GetTemplateName(i); } catch { }
                    string extra = "";
                    try
                    {
                        PsStandardPlateLinkData d = sp2.GetTemplate(nm);
                        if (d != null)
                            extra = "  L=" + F(d.Length) + " W=" + F(d.Width) + " t=" + F(d.Thickness) +
                                    " holeDia=" + F(d.HoleDiameter) + " nH=" + d.HorizontalHoleCount +
                                    " nV=" + d.VerticalHoleCount + " play=" + F(d.HoleWorkLoose) +
                                    " stiff=" + d.WithStiffeners + " group=" + d.CreateGroup;
                    }
                    catch { }
                    sb.AppendLine("  [" + i + "] " + nm + extra);
                }

                PsCopeConnection cp = new PsCopeConnection();
                cp.SetToDefaults();
                int ncp = cp.GetTemplateCount();
                sb.AppendLine("COPE templates: " + ncp);
                for (int i = 0; i < ncp; i++)
                {
                    string nm = ""; try { nm = cp.GetTemplateName(i); } catch { }
                    string extra = "";
                    try
                    {
                        PsCopeLinkDataMgd d = cp.GetTemplate(nm);
                        if (d != null)
                            extra = "  radius=" + F(d.Radius) + " rathole1=" + F(d.FirstRatholeDiameter) +
                                    " rathole2=" + F(d.SecondRatholeDiameter) + " webDist=" + F(d.WebDistance);
                    }
                    catch { }
                    sb.AppendLine("  [" + i + "] " + nm + extra);
                }

                PsHaunchConnection hn = new PsHaunchConnection();
                hn.SetToDefaults();
                int nhn = hn.GetTemplateCount();
                sb.AppendLine("HAUNCH templates: " + nhn);
                for (int i = 0; i < nhn; i++)
                {
                    string nm = ""; try { nm = hn.GetTemplateName(i); } catch { }
                    string extra = "";
                    try
                    {
                        PsHaunchLinkDataMgd d = hn.GetTemplate(nm);
                        if (d != null)
                            extra = "  L=" + F(d.Length) + " topH=" + F(d.TopHeight) +
                                    " baseH=" + F(d.BaseHeight) + " web=" + F(d.WebThickness) +
                                    " group=" + d.CreateGroup;
                    }
                    catch { }
                    sb.AppendLine("  [" + i + "] " + nm + extra);
                }

                PsPurlinConnection pu = new PsPurlinConnection();
                pu.SetToDefaults();
                int npu = pu.GetTemplateCount();
                sb.AppendLine("PURLIN templates: " + npu);
                for (int i = 0; i < npu; i++)
                {
                    string nm = ""; try { nm = pu.GetTemplateName(i); } catch { }
                    string extra = "";
                    try
                    {
                        PsPurlinLinkDataMgd d = pu.GetTemplate(nm);
                        if (d != null)
                            extra = "  L=" + F(d.Length) + " W=" + F(d.Width) + " H=" + F(d.Height) +
                                    " t=" + F(d.Thickness) + " holeDia=" + F(d.HoleDiameter) +
                                    " group=" + d.CreateGroup;
                    }
                    catch { }
                    sb.AppendLine("  [" + i + "] " + nm + extra);
                }
            }
            catch (System.Exception ex) { sb.AppendLine("BEAMCONN ERR " + ex.Message); }

            File.WriteAllText(Path.Combine(Dir, "eb_conn_templates.txt"), sb.ToString(), Encoding.UTF8);
            Result("EB_OK conntemplates -> eb_conn_templates.txt");
        }

        // ---- BUILD a base plate as a real CONNECTION (plate + holes + welds +
        //      anchors in one parametric object, the way the modeller does it) ----
        void ConnBase(Dictionary<string, string> kv)
        {
            string h = Get(kv, "handle", "");
            // v32 -- COMPLETES THE v30 FIX. v30 stopped the plugin inventing ANCHOR defaults
            // but left these six GEOMETRY defaults baked in, and they were written to the
            // template unconditionally below. So GetTemplate() was read and then silently
            // overwritten with 300/250/10/23/200/160 -- the exact class of bug that produced
            // the 428 mm anchors. A parameter that is NOT SENT must touch nothing.
            System.Globalization.CultureInfo IC0 = System.Globalization.CultureInfo.InvariantCulture;
            string sL = Get(kv, "l", ""), sW = Get(kv, "w", ""), sT = Get(kv, "t", "");
            string sHd = Get(kv, "holedia", ""), sHx = Get(kv, "hx", ""), sHy = Get(kv, "hy", "");
            bool anchors = Get(kv, "anchors", "0") == "1";
            string tmpl = Get(kv, "template", "");

            string h0, c0; int before = Census(out h0, out c0);
            long oid = IdFromHandle(h);
            string msg = "";
            bool made = false;
            try
            {
                PsBasePlateConnection bp = new PsBasePlateConnection();
                bp.SetToDefaults();
                bp.SetConnectionObjectId(oid);
                PsBaseplateLinkDataMgd d = null;
                if (tmpl.Length > 0) { try { d = bp.GetTemplate(tmpl); } catch { } }
                if (d == null) d = new PsBaseplateLinkDataMgd();
                if (sL.Length  > 0) d.Length                  = double.Parse(sL,  IC0);
                if (sW.Length  > 0) d.Width                   = double.Parse(sW,  IC0);
                if (sT.Length  > 0) d.Thickness               = double.Parse(sT,  IC0);
                if (sHd.Length > 0) d.HoleDiameter            = double.Parse(sHd, IC0);
                if (sHx.Length > 0) d.HoleDistanceHorizontal  = double.Parse(sHx, IC0);
                if (sHy.Length > 0) d.HoleDistanceVertical    = double.Parse(sHy, IC0);
                d.AnchorBolts = anchors;
                // ANCHOR BOLTS must be visible. Amir marked 90/100 because the
                // anchors came out with diameter 0 — present as blocks but with no
                // body to see. Their LENGTH is deliberately graphic (his stated
                // boundary); their PRESENCE and DIAMETER are not optional.
                if (anchors)
                {
                    // A parameter that is NOT sent must not be touched. Baking my
                    // own default (grip=400) into the code overwrote the template
                    // and produced 428mm anchors where the software's own value is
                    // 157mm. From a template, change ONLY what the drawing says.
                    string sDia = Get(kv, "anchordia", "");
                    string sGrip = Get(kv, "anchorgrip", "");
                    string sGripD = Get(kv, "anchorgripdia", "");
                    string sDrill = Get(kv, "anchordrill", "");
                    string sKey = Get(kv, "anchorkey", "");
                    var IC = System.Globalization.CultureInfo.InvariantCulture;
                    if (sDia.Length > 0)
                    { try { d.AnchorBoltDiameter = double.Parse(sDia, IC); } catch { } }
                    if (sGripD.Length > 0)
                    { try { d.AnchorBoltGripDiameter = double.Parse(sGripD, IC); } catch { } }
                    if (sGrip.Length > 0)
                    { try { d.AnchorBoltGripLength = double.Parse(sGrip, IC); } catch { } }
                    if (sDrill.Length > 0)
                    { try { d.AnchorBoltDrillLength = double.Parse(sDrill, IC); } catch { } }
                    if (sKey.Length > 0)
                    { try { d.AnchorBoltKeySize = double.Parse(sKey, IC); } catch { } }
                    string sDet = Get(kv, "anchordetail", "");
                    if (sDet.Length > 0)
                    { try { d.CreateDetailedAnchorBolts = sDet == "1"; } catch { } }
                    string sOut = Get(kv, "anchoroutside", "");
                    if (sOut.Length > 0)
                    { try { d.AnchorBoltsOutside = sOut == "1"; } catch { } }
                }
                // Lesson 4: the macro SHORTENS the column by the plate thickness, so
                // the plate sits ON the floor (z=0) and the design level is preserved.
                try { d.ShortenShape = Get(kv, "shorten", "1") == "1"; }
                catch { }
                double dts = double.Parse(Get(kv, "dts", "0"),
                    System.Globalization.CultureInfo.InvariantCulture);
                if (dts != 0) { try { d.DistanceToSupport = dts; } catch { } }
                d.CreateGroup = true;
                bp.SetConnectionData(d);
                int chk = 0;
                try { chk = bp.Check(); } catch { }
                made = bp.Create();
                msg = "check=" + chk + " create=" + made;
            }
            catch (System.Exception ex) { msg = "EX=" + One(ex.Message); }

            string h1, c1; int after = Census(out h1, out c1);
            // verify by reading the holes the connection drilled into the column
            string err;
            int holes = HolesOf(oid, 0, null, h, out err);
            // and verify the anchors actually have a BODY (extents), not just a block
            int anchorsSeen = 0;
            string aExt = "";
            try
            {
                Document doc2 = Application.DocumentManager.MdiActiveDocument;
                using (Transaction tr = doc2.Database.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(doc2.Database.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                    foreach (ObjectId id2 in ms)
                    {
                        Entity e2 = null;
                        try { e2 = tr.GetObject(id2, OpenMode.ForRead) as Entity; } catch { }
                        if (e2 == null || e2.Layer != "PS_Bolt") continue;
                        try
                        {
                            Extents3d x = e2.GeometricExtents;
                            double dx = x.MaxPoint.X - x.MinPoint.X;
                            double dz = x.MaxPoint.Z - x.MinPoint.Z;
                            if (dx > 0.5 || dz > 0.5)
                            {
                                anchorsSeen++;
                                if (aExt.Length == 0)
                                    aExt = F(dx) + "x" + F(x.MaxPoint.Y - x.MinPoint.Y) + "x" + F(dz);
                            }
                        }
                        catch { }
                    }
                    tr.Commit();
                }
            }
            catch { }
            Result((after > before ? "EB_OK" : "EB_ERR") + " connbase host=" + h
                 + " added=" + (after - before) + " " + msg + " host_holes=" + holes
                 + " anchors_with_body=" + anchorsSeen
                 + (aExt.Length > 0 ? " anchor_bbox=" + aExt : ""));
        }

        void ConnStiff(Dictionary<string, string> kv)
        {
            string h = Get(kv, "handle", "");
            double T = double.Parse(Get(kv, "t", "10"), System.Globalization.CultureInfo.InvariantCulture);
            double L = double.Parse(Get(kv, "len", "0"), System.Globalization.CultureInfo.InvariantCulture);
            int shape = int.Parse(Get(kv, "shape", "0"));
            double r = double.Parse(Get(kv, "r", "0"), System.Globalization.CultureInfo.InvariantCulture);
            double fl = double.Parse(Get(kv, "fldist", "0"), System.Globalization.CultureInfo.InvariantCulture);
            double wd = double.Parse(Get(kv, "webdist", "0"), System.Globalization.CultureInfo.InvariantCulture);
            string at = Get(kv, "at", "");
            string tmpl = Get(kv, "template", "");

            string h0, c0; int before = Census(out h0, out c0);
            string msg = ""; bool made = false;
            try
            {
                PsStiffenerConnection st = new PsStiffenerConnection();
                st.SetToDefaults();
                st.SetConnectionObjectId(IdFromHandle(h));
                if (at.Length > 0) st.SetConnectionPoint(Pt(at));
                PsStiffenerLinkDataMgd d = null;
                if (tmpl.Length > 0) { try { d = st.GetTemplate(tmpl); } catch { } }
                if (d == null) d = new PsStiffenerLinkDataMgd();
                d.Thickness = T;
                if (L > 0) d.Length = L;
                d.ShapeType = shape;
                if (r > 0) d.Radius = r;
                if (fl > 0) d.FlangeDistance = fl;
                if (wd > 0) d.WebDistance = wd;
                d.CreateGroup = true;
                st.SetConnectionData(d);
                int chk = 0;
                try { chk = st.Check(); } catch { }
                made = st.Create();
                msg = "check=" + chk + " create=" + made;
            }
            catch (System.Exception ex) { msg = "EX=" + One(ex.Message); }
            string h1, c1; int after = Census(out h1, out c1);
            Result((after > before ? "EB_OK" : "EB_ERR") + " connstiff host=" + h
                 + " added=" + (after - before) + " newest=" + h1 + " " + msg);
        }

        void ConnSplice(Dictionary<string, string> kv)
        {
            string h = Get(kv, "handle", "");          // the connected shape
            string sup = Get(kv, "support", "");       // the supporting shape
            double hd = double.Parse(Get(kv, "holedia", "19"), System.Globalization.CultureInfo.InvariantCulture);
            double tw = double.Parse(Get(kv, "tweb", "10"), System.Globalization.CultureInfo.InvariantCulture);
            double tf = double.Parse(Get(kv, "tfl", "10"), System.Globalization.CultureInfo.InvariantCulture);
            int nhw = int.Parse(Get(kv, "nhweb", "2"));
            int nvw = int.Parse(Get(kv, "nvweb", "2"));
            double gap = double.Parse(Get(kv, "gap", "0"), System.Globalization.CultureInfo.InvariantCulture);
            string at = Get(kv, "at", "");

            string h0, c0; int before = Census(out h0, out c0);
            string msg = ""; bool made = false;
            try
            {
                PsSpliceJointConnection sp = new PsSpliceJointConnection();
                sp.SetToDefaults();
                sp.SetConnectionObjectId(IdFromHandle(h));
                if (sup.Length > 0) sp.SetSupportObjectId(IdFromHandle(sup));
                if (at.Length > 0) sp.SetConnectionPoint(Pt(at));
                PsSpliceJointLinkDataMgd d = new PsSpliceJointLinkDataMgd();
                d.HoleDiameter = hd;
                d.PlateThicknessWeb = tw;
                d.PlateThicknessFlange = tf;
                d.HoleCountHorizontalWeb = nhw;
                d.HoleCountVerticalWeb = nvw;
                if (gap > 0) d.DistanceBetweenObjects = gap;
                d.ConnectWebLeft = true;
                d.ConnectWebRight = true;
                d.CreateGroup = true;
                sp.SetConnectionData(d);
                int chk = 0;
                try { chk = sp.Check(); } catch { }
                made = sp.Create();
                msg = "check=" + chk + " create=" + made;
            }
            catch (System.Exception ex) { msg = "EX=" + One(ex.Message); }
            string h1, c1; int after = Census(out h1, out c1);
            string err;
            int holes = HolesOf(IdFromHandle(h), 0, null, h, out err);
            Result((after > before ? "EB_OK" : "EB_ERR") + " connsplice host=" + h
                 + " added=" + (after - before) + " " + msg + " host_holes=" + holes);
        }

        // Remove every connection on a part, deleting the steel it generated.
        // Needed to rebuild a joint with corrected parameters instead of leaving
        // an orphaned plate behind.
        void ConnRemove(Dictionary<string, string> kv)
        {
            string h = Get(kv, "handle", "");
            bool del = Get(kv, "delparts", "1") == "1";
            string h0, c0; int before = Census(out h0, out c0);
            string msg = "";
            try
            {
                PsEditLogicalLink ed = new PsEditLogicalLink();
                ed.SetObjectId(IdFromHandle(h));
                int n = ed.get_LogicalLinkCount();
                ed.RemoveAllLogicalLinks(del);
                msg = "links_removed=" + n;
            }
            catch (System.Exception ex) { msg = "EX=" + One(ex.Message); }
            string h1, c1; int after = Census(out h1, out c1);
            Result("EB_OK connremove host=" + h + " " + msg
                 + " entities " + before + "->" + after);
        }

        // Reshape an EXISTING plate: give it a new contour in its own plane while
        // keeping its position, layer and — crucially — the holes already drilled
        // in it. This is how a rectangle becomes a proper chamfered rib without
        // losing the connection work already done.
        // pts are LOCAL (plate-plane) coordinates: x,y[,0];x,y[,0];...
        void SetPoly(Dictionary<string, string> kv)
        {
            string h = Get(kv, "handle", "");
            string ptsS = Get(kv, "pts", "");
            string[] chunks = ptsS.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (chunks.Length < 3) { Result("EB_ERR setpoly needs >=3 pts"); return; }

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            int nvBefore = 0, nvAfter = 0, holesBefore = 0, holesAfter = 0;
            string rmBefore = "?", rmAfter = "?", err = "", msg = "";
            long oid = IdFromHandle(h);
            string e2;
            holesBefore = HolesOf(oid, 0, null, h, out e2);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId id = db.GetObjectId(false, new Handle(Convert.ToInt64(h, 16)), 0);
                DBObject o = tr.GetObject(id, OpenMode.ForWrite);
                PsPlate pl = o as PsPlate;
                if (pl == null) { tr.Abort(); Result("EB_ERR setpoly not a PsPlate: " + h); return; }
                PsPolygon cur = new PsPolygon();
                try { pl.GetPolygon(cur); nvBefore = cur.Count; } catch { }
                try { rmBefore = pl.RectangleMode ? "1" : "0"; } catch { }

                PsPolygon np = new PsPolygon();
                np.init();
                foreach (string c in chunks)
                {
                    double[] n = Nums(c);
                    np.appendVertex(n[0], n.Length > 1 ? n[1] : 0.0, 0.0);
                }
                try
                {
                    pl.SetPolygon(np);
                    // force the solid to be rebuilt from the new contour
                    try { pl.RecalculationFlag = true; } catch { }
                    try { pl.computeMidLine(false, false); } catch { }
                    try { pl.computeObjectDimension(false); } catch { }
                    msg = "set=ok";
                }
                catch (System.Exception ex) { msg = "EX=" + One(ex.Message); }
                tr.Commit();
            }

            // read the contour back — proof, not an echo
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId id = db.GetObjectId(false, new Handle(Convert.ToInt64(h, 16)), 0);
                string pts = PolyOf(tr.GetObject(id, OpenMode.ForRead), out nvAfter, out rmAfter, out err);
                tr.Commit();
                msg += " pts=" + pts;
            }
            holesAfter = HolesOf(oid, 0, null, h, out e2);
            Result((nvAfter >= chunks.Length ? "EB_OK" : "EB_ERR") + " setpoly handle=" + h
                 + " verts " + nvBefore + "->" + nvAfter + " rect " + rmBefore + "->" + rmAfter
                 + " holes " + holesBefore + "->" + holesAfter + " " + msg);
        }
    }
}
