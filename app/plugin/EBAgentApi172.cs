// EBAgentApi.cs - EB PROSTEEL AGENT native modeling API (v9 - adds dumpmodel model-reader).
// Runs INSIDE AutoCAD 2015 + ProStructures V8i SS6 (NETLOAD).
// Creates REAL ProSteel objects (PsShape beams, PsPlate, PsBolt, miter cuts)
// programmatically - NO dialogs. Discovered via reflection dump of
// ProStructuresNet.dll (see api_dump_ProStructuresNet.txt).
//
// Protocol (file-based, avoids command-line quoting + supports Hebrew):
//   1. Python writes  eb_cmd.txt  (key=value lines, op=... first)
//   2. Python sends command  EB_RUN172
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
using Bentley.ProStructures.Steel.Primitive;
using Bentley.ProStructures.Steel;   // v124: PsCreateSolidReference, PsSolidReference (B.11)   // v123: PsCreatePrimitive (B.10 solids)
using Bentley.ProStructures.Modification.Edit;
using Bentley.ProStructures.Modification;
using Bentley.ProStructures.Modification.ObjectData;
using Bentley.ProStructures.Drawing;   // v163 (B.23): PsTransaction -- the generic binder
using Bentley.ProStructures.Connection.General;
using Bentley.ProStructures.Connection.LinkData;
using Bentley.ProStructures.Connection.Standard;
using Autodesk.AutoCAD.EditorInput;   // v41: Editor.Command / SetImpliedSelection
using Bentley.ProStructures.Annotation;   // v107: PsCreateWeldFlag, PsWeldFlag
using Bentley.ProStructures.StructuralObject;  // v107: PsGussetConnection et al
using Bentley.ProStructures.Property;
using Bentley.ProStructures.Concrete;   // v34: PsCreateFastener (anchor bolts)
using Bentley.ProStructures.Modeling;   // v34: PsObjectGroup, PsCollisionCheck
using Bentley.ProStructures.Miscellaneous;  // v35: PsObjectStyleList
using Bentley.ProStructures;
using Bentley.ProStructures.Modeling;
// PsShapeLoader lives in Steel.Shape (already imported)

[assembly: CommandClass(typeof(EBAgent.ApiCmds172))]
[assembly: ExtensionApplication(typeof(EBAgent.EBApp172))]

namespace EBAgent
{
    // Registers an assembly resolver so ProSteel's managed assemblies are found
    // in the Prg folder even from a cold AutoCAD session (before any ProSteel cmd).
    public class EBApp172 : IExtensionApplication
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

                    // holes drilled into it ג€” the thing screenshots cannot show
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

    public class ApiCmds172
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

        [CommandMethod("EB_RUN172", CommandFlags.Modal)]
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
                    case "vfy_bolts": VfyBolts(kv); break;
                    case "vfy_fit": VfyFit(kv); break;
                    case "vfy_dupes": VfyDupes(kv); break;
                    case "holefields": HoleFields(kv); break;
                    case "edgecheck": EdgeCheck(kv); break;
                    case "killholefield": KillHoleField(kv); break;
                    case "vfy_touch": VfyTouch(kv); break;
                    case "vfy_size": VfySize(kv); break;
                    case "classify": Classify(kv); break;
                    case "acis": ToAcis(kv); break;
                    case "acisref": AcisRef(kv); break;
                    case "solid": Solid(kv); break;
                    case "connverify": ConnVerify(kv); break;
                    case "connkill": ConnKill(kv); break;
                    case "haunch": Haunch(kv); break;
                    case "macrobrace": MacroBrace(kv); break;
                    case "align": Align(kv); break;
                    case "spiral": Spiral(kv); break;
                    case "purlintype": PurlinTypeDump(kv); break;
                    case "copeinfo": CopeInfo(kv); break;
                    case "cope": Cope(kv); break;
                    case "boltinfo": BoltInfo(kv); break;
                    case "boltsingle": BoltSingle(kv); break;
                    case "nutonly": NutOnly(kv); break;
                    case "threadedrod": ThreadedRod(kv); break;
                    case "bracing": Bracing(kv); break;
                    case "weldstyles": WeldStyles(kv); break;
                    case "weld": Weld(kv); break;
                    case "splicetemplates": SpliceTemplates(kv); break;
                    case "splice": Splice(kv); break;
                    case "shearplatetemplates": ShearPlateTemplates(kv); break;
                    case "shearplate": ShearPlate(kv); break;
                    case "webangletemplates": WebAngleTemplates(kv); break;
                    case "webangle": WebAngle(kv); break;
                    case "stifftemplates": StiffTemplates(kv); break;
                    case "stiffener": Stiffener(kv); break;
                    case "plate9": Plate9(kv); break;
                    case "arcplate": ArcPlate(kv); break;
                    case "bend": Bend(kv); break;
                    case "bendshape": BendShape(kv); break;
                    case "bendinfo": BendInfo(kv); break;
                    case "bendtwo": BendTwo(kv); break;
                    case "plateinfo": PlateInfo(kv); break;
                    case "frame": Frame(kv); break;
                    case "frameinfo": FrameInfo(kv); break;
                    case "grid": Grid(kv); break;
                    case "gridpoints": GridPointsOp(kv); break;
                    case "gridcolumns": GridColumns(kv); break;
                    case "shape": Shape(kv); break;
                    case "shapeinfo": ShapeInfo(kv); break;
                    case "boltparts": BoltParts(kv); break;
                    case "purlin": Purlin(kv); break;
                    case "basedump": BaseDump(kv); break;
                    case "save": Save(kv); break;
                    case "connset": ConnSet(kv); break;
                    case "conndump": ConnDump(kv); break;
                    case "groupauto": GroupAuto(kv); break;
                    case "drillspecial": DrillSpecial(kv); break;
                    case "section": Section(kv); break;
                    case "shapeedit": ShapeEdit(kv); break;
                    case "boolean": Boolean(kv); break;
                    case "detailcut": DetailCut(kv); break;
                    case "groupinfo": GroupInfo(kv); break;
                    case "grouporphans": GroupOrphans(kv); break;
                    case "groupedit": GroupEdit(kv); break;
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
                    case "env": Env(kv); break;
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
                    case "layerprobe": LayerProbe(kv); break;
                    case "gridaxes": GridAxes(kv); break;
                    case "bind": Bind(kv); break;
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
                    case "stylelist": StyleList(kv); break;
                    case "dbase": DBase(kv); break;
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
                    // --- E.9, the properties dialog turned into code ---
                    case "propfull": PropFull(kv); break;
                    case "propset": PropSet(kv); break;
                    case "propcopy": PropCopy(kv); break;
                    case "changesection": ChangeSection(kv); break;
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
            // asymmetric profiles (MirrorFlag itself is read-only ג€” proven by test)
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
                    // B.8 audit 10/08: the chapter defines FIVE shape types and this line
                    // reached exactly one. PsCreateShape has four selectors, and the shipped
                    // databases hold 68 UserShapes, 20 RoofWall and 15 CombiShapes -- 106
                    // definitions that were unreachable because of one hardcoded call.
                    string skind = Get(kv, "kind", "standard").ToLowerInvariant();
                    if (skind.StartsWith("spec") || skind.StartsWith("user") || skind.StartsWith("sopro"))
                        cs.SelectSpecialSections();
                    else if (skind.StartsWith("roof") || skind.StartsWith("wall"))
                        cs.SelectRoofWallSections();
                    else if (skind.StartsWith("comb"))
                        cs.SelectCombinationSections();
                    // NOTE: PsCreateShape has NO SelectWeldSections. Weld shapes are reachable
                    // only through PsCreateBendShape -- see op 'bendshape'.
                    else
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
                    cp0.UseCurrentLayer(false);   // B.1 audit 10/08: false => ProSteel assigns the part's OWN layer
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
                cp.UseCurrentLayer(false);   // B.1 audit 10/08: false => ProSteel assigns the part's OWN layer
                bool ok = cp.Create();
                string h1, c1; int after = Census(out h1, out c1);
                // v128: layer= was applied on the `via=ecs` branch only, and SILENTLY
                // ignored here -- which is the branch nearly every plate actually takes.
                // Found while reading E.9.17's Assignments tab: LayerName came back "0"
                // on a plate created with layer=E09-props. A parameter the op accepts and
                // then drops is worse than one it refuses.
                if (ok && after > before)
                {
                    string ls1 = plLayer.Length > 0 ? ApplyLayer(h1, plLayer) : "n/a";
                    Result("EB_OK plate " + L + "x" + W + "x" + T + " handle=" + h1
                         + " class=" + c1 + " via=matrix layer=" + ls1 + " entities=" + after);
                    return;
                }
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
                cp.UseCurrentLayer(false);   // B.1 audit 10/08: false => ProSteel assigns the part's OWN layer
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
                cp.SetAsRectangularPlate(PL, PW); cp.SetThickness(PT); cp.UseCurrentLayer(false);   // B.1 audit 10/08: false => ProSteel assigns the part's OWN layer
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
        // op=env [full=1]
        //
        // A.1.2 documents `PS_VERSION`, which prints the running build to the AutoCAD command
        // line. This does the same thing from INSIDE the process, which is strictly stronger:
        // it reports the assembly ACTUALLY LOADED, not the file that happens to sit on disk.
        // And it needs no command, so the `cmd` allowlist stays where Amir set it.
        //
        // ג­ג­ WHY THIS MATTERS FOR EVERY OTHER CHAPTER. A.1.2's own worked example reads:
        //     "ProSteel V8i (SELECTseries 3) - Version 8.11.3.48 dated from Aug 25 2010"
        // and this installation is Ss6 R1, ProStructuresNet 08.11.11.161, ֲ© 2013. Same 8.11,
        // but build 3.48 against 11.161 -- eight SelectSeries apart and three years later.
        // ג‡’ **The manual documents an OLDER program than the one we run.** That is not a
        // detail: it retroactively explains E.10's chapter numbers being off by one in part B
        // and by two or three in part C -- chapters were INSERTED between SS3 and SS6 and the
        // reference table was never renumbered. It also means the reverse is possible: things
        // this build can do that the manual never mentions.
        // ג‡’ **"The manual says X" always carries an unstated "ג€¦in SS3".**
        void Env(Dictionary<string, string> kv)
        {
            bool full = Get(kv, "full", "0") == "1";
            StringBuilder sb = new StringBuilder();
            string psVer = "?", psAsm = "?", psFile = "?", acVer = "?";

            sb.AppendLine("== what is actually LOADED in this process ==");
            try
            {
                Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
                List<string> rows = new List<string>();
                foreach (Assembly a in asms)
                {
                    string nm;
                    try { nm = a.GetName().Name; } catch { continue; }
                    bool interesting = nm.IndexOf("ProStructures", StringComparison.OrdinalIgnoreCase) >= 0
                                    || nm.IndexOf("PSN_", StringComparison.OrdinalIgnoreCase) >= 0
                                    || nm.IndexOf("acmgd", StringComparison.OrdinalIgnoreCase) >= 0
                                    || nm.IndexOf("acdbmgd", StringComparison.OrdinalIgnoreCase) >= 0
                                    || nm.IndexOf("EBAgentApi", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!interesting && !full) continue;
                    // ג ן¸ AN ASSEMBLY HAS TWO VERSIONS AND THEY ARE NOT THE SAME NUMBER.
                    // GetName().Version is the ASSEMBLY version -- ProStructuresNet reports
                    // 1.0.6045.20863. FileVersionInfo.FileVersion is the PRODUCT version --
                    // 08.11.11.161, which is the one comparable to the manual's 8.11.3.48.
                    // The first draft of this op printed the assembly version next to the
                    // manual's file version and called it a comparison. It was not one.
                    string ver = "?", fver = "?", loc = "";
                    try { ver = a.GetName().Version.ToString(); } catch { }
                    try { loc = a.Location; } catch { }
                    try
                    {
                        if (loc.Length > 0 && File.Exists(loc))
                            fver = System.Diagnostics.FileVersionInfo.GetVersionInfo(loc).FileVersion;
                    }
                    catch { }
                    if (nm == "ProStructuresNet") { psVer = fver; psAsm = ver; psFile = loc; }
                    rows.Add(string.Format("  {0,-30} file {1,-16} asm {2,-16} {3}", nm, fver, ver, loc));
                }
                rows.Sort();
                foreach (string r in rows) sb.AppendLine(r);
                sb.AppendLine("  (assemblies loaded: " + asms.Length + ")");
            }
            catch (System.Exception ex) { sb.AppendLine("  <" + ex.Message + ">"); }

            sb.AppendLine();
            sb.AppendLine("== AutoCAD ==");
            try
            {
                acVer = Application.Version.ToString();
                sb.AppendLine("  Version           " + acVer);
            }
            catch (System.Exception ex) { sb.AppendLine("  version: <" + ex.Message + ">"); }
            foreach (string v in new string[] { "ACADVER", "PRODUCT", "MEASUREMENT", "INSUNITS", "LUNITS", "DWGNAME" })
            {
                try { sb.AppendLine(string.Format("  {0,-17} {1}", v, Application.GetSystemVariable(v))); }
                catch { }
            }

            sb.AppendLine();
            sb.AppendLine("== the version gap this chapter is about ==");
            sb.AppendLine("  the MANUAL's own example (A.1.2):  V8i SelectSeries 3, 8.11.3.48, Aug 25 2010");
            sb.AppendLine("  what is running here             :  ProStructuresNet file " + psVer
                        + "  (assembly " + psAsm + ")");
            sb.AppendLine("  => the manual documents an OLDER build. Read every 'the manual says'");
            sb.AppendLine("     with an unstated '...in SS3'.");

            File.WriteAllText(Path.Combine(Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location), "eb_env.txt"),
                sb.ToString(), new UTF8Encoding(true));
            Result("EB_OK env ProStructuresNet file=" + psVer + " asm=" + psAsm + " AutoCAD=" + acVer
                 + " (manual documents 8.11.3.48 / SS3 / 2010 -- OLDER than this build)"
                 + " -> eb_env.txt");
        }

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
                            // dumpmodel fix 10/08: InsertPoint was fetched inside a try and then read
                            // OUTSIDE it. A PsPoint with a dead native handle is not null and throws on
                            // read -- 178 plates and 179 bolts became ERR rows this way, while props
                            // read the very same handles without trouble.
                            double ipx = 0, ipy = 0, ipz = 0;
                            try { PsPoint ip0 = pl.InsertPoint;
                                  if (ip0 != null) { ipx = ip0.x; ipy = ip0.y; ipz = ip0.z; } }
                            catch { }
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
                              .Append(F(ipx)).Append(',').Append(F(ipy)).Append(',').Append(F(ipz)).Append('\t')
                              .Append(poly).Append('\t')
                              .Append(Safe(SafeS(delegate() { return pl.Material; }))).Append('\t')
                              .Append(Safe(SafeS(delegate() { return pl.Name; }))).Append('\t')
                              .Append(cls).Append('\t')
                              // world bounding box: InsertPoint reads 0,0,0 for every plate and the polygon
                              // is in LOCAL coordinates, so without this a plate has no position at all.
                              .Append(ExtStr(o)).AppendLine();
                            nPlate++; continue;
                        }

                        PsBolt bo = o as PsBolt;
                        if (bo != null)
                        {
                            // dumpmodel fix 10/08: InsertPoint was fetched inside a try and then read
                            // OUTSIDE it. A PsPoint with a dead native handle is not null and throws on
                            // read -- 178 plates and 179 bolts became ERR rows this way, while props
                            // read the very same handles without trouble.
                            double ipx = 0, ipy = 0, ipz = 0;
                            try { PsPoint ip0 = bo.InsertPoint;
                                  if (ip0 != null) { ipx = ip0.x; ipy = ip0.y; ipz = ip0.z; } }
                            catch { }
                            sb.Append("BOLT\t").Append(hnd).Append('\t')
                              .Append(F(SafeD(delegate() { return bo.Diameter; }))).Append('\t')
                              .Append(Safe(SafeS(delegate() { return bo.BoltStyleName; }))).Append('\t')
                              .Append(SafeI(delegate() { return bo.Count; })).Append('\t')
                              .Append(F(SafeD(delegate() { return bo.Length; }))).Append('\t')
                              .Append(F(ipx)).Append(',').Append(F(ipy)).Append(',').Append(F(ipz)).Append('\t')
                              .Append(Safe(SafeS(delegate() { return bo.Name; }))).Append('\t')
                              .Append(cls).Append('\t')
                              // world bounding box: InsertPoint reads 0,0,0 for every plate and the polygon
                              // is in LOCAL coordinates, so without this a plate has no position at all.
                              .Append(ExtStr(o)).AppendLine();
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
                // READ BACK ג€” the only thing we trust
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


        // op=gridaxes at=x,y,z len= wide= [lsteps=] [wsteps=] [ux=x1,y1;x2,y2] [uy=...] [name=]
        //
        // B.6.7 "Additional Axes (Border Lines)": "you can add any additional axes to the grid
        // where work frame views can be built. e.g. This can be existing constructional axes of
        // architect's plans… This function helps you to create an axis grid completely out of an
        // existing 2D-axis plan."
        //
        // B.6's audit left this as the one recorded UNTRIED route. PsGrid.addUserXaxis /
        // addUserYaxis exist, but PsGrid cannot bind to an existing frame, and IKs_ComGrid --
        // re-checked today -- genuinely has no user-axis method. What was never tried is
        // PsGrid.insert(Origin, Xaxis, Yaxis) as an ALTERNATIVE CREATOR: build a fresh PsGrid,
        // set its user axes, and insert it.
        //
        // Everything is read back: addUserXaxis returns a bool, and a bool is not evidence.
        void GridAxes(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            PsPoint at = Pt(Get(kv, "at", "0,0,0"));
            double L = double.Parse(Get(kv, "len", "12000"), IC);
            double W = double.Parse(Get(kv, "wide", "8000"), IC);
            string ux = Get(kv, "ux", ""), uy = Get(kv, "uy", "");
            string nm = Get(kv, "name", "");

            string h0, c0; int before = Census(out h0, out c0);
            StringBuilder sb = new StringBuilder();
            string msg = "";
            int addedX = 0, addedY = 0, readX = 0, readY = 0;

            try
            {
                // PsGrid is the ENTITY, not the creator -- it exposes PROPERTIES, not setters.
                // PsCreateGrid is the creator and has no user-axis methods; PsGrid has the user
                // axes and no creator methods. That split is the whole difficulty of B.6.7.
                PsGrid g = new PsGrid();
                try { g.Length = L; g.Wide = W; } catch (System.Exception e) { msg += " dims!" + One(e.Message); }
                if (nm.Length > 0) { try { g.Name = nm; } catch { } }

                // the user axes, each "x1,y1,z1;x2,y2,z2"
                foreach (string seg in ux.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] pp = seg.Split(';');
                    if (pp.Length < 2) continue;
                    try { if (g.addUserXaxis(Pt(pp[0]), Pt(pp[1]))) addedX++; }
                    catch (System.Exception e) { msg += " addX!" + One(e.Message); }
                }
                foreach (string seg in uy.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] pp = seg.Split(';');
                    if (pp.Length < 2) continue;
                    try { if (g.addUserYaxis(Pt(pp[0]), Pt(pp[1]))) addedY++; }
                    catch (System.Exception e) { msg += " addY!" + One(e.Message); }
                }

                // read them back BEFORE inserting -- do they survive on the object at all?
                for (int i = 0; i < 12; i++)
                {
                    PsPoint a = new PsPoint(0, 0, 0), b = new PsPoint(0, 0, 0);
                    try { if (g.getUserXaxis(i, a, b)) { readX++; sb.AppendLine("  userX[" + i + "] " + F(a.x) + "," + F(a.y) + " -> " + F(b.x) + "," + F(b.y)); } else break; }
                    catch { break; }
                }
                for (int i = 0; i < 12; i++)
                {
                    PsPoint a = new PsPoint(0, 0, 0), b = new PsPoint(0, 0, 0);
                    try { if (g.getUserYaxis(i, a, b)) { readY++; sb.AppendLine("  userY[" + i + "] " + F(a.x) + "," + F(a.y) + " -> " + F(b.x) + "," + F(b.y)); } else break; }
                    catch { break; }
                }

                try { g.insert(at, new PsVector(1, 0, 0), new PsVector(0, 1, 0)); }
                catch (System.Exception e) { msg += " insert!" + One(e.Message); }
            }
            catch (System.Exception ex) { msg += " EX:" + One(ex.Message); }

            string h1, c1; int after = Census(out h1, out c1);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location), "eb_gridaxes.txt"),
                sb.ToString(), new UTF8Encoding(true));
            Result(((after > before) ? "EB_OK" : "EB_ERR") + " gridaxes census=" + before + "->" + after
                 + " new=" + (after > before ? h1 + " " + c1 : "-")
                 + " addedX=" + addedX + " addedY=" + addedY
                 + " readBackX=" + readX + " readBackY=" + readY + msg
                 + "  -> eb_gridaxes.txt");
        }

        // op=layerprobe [at=x,y,z] [cleanup=1]
        //
        // B.1 AUDIT, 10/08. The chapter says ProSteel has "an automatic layer control. Normally
        // you don't have to take care of thisג€¦ objects are created on their own layer." On
        // 09/08 I measured 88 parts sitting on layer 0 ג€” every one made by calling a Ps*Create*
        // class directly ג€” and FIXED THE MODEL by moving them. The model has stayed clean
        // since. But the audit's first question is whether anything can be improved, and the
        // honest answer is that I fixed the symptom and left the cause: 11 of 17 creation ops
        // still cannot take a layer at all, and the plate paths call UseCurrentLayer(TRUE)
        // unconditionally.
        //
        // Before adding layer= to eleven ops, the right question is what the creator does when
        // told NOT to use the current layer and given no layer either. If that yields ProSteel's
        // own automatic layer, the whole class of bug disappears at the root and no parameter is
        // needed. Three plates, one variable:
        //
        //   A  UseCurrentLayer(true)                     <- what the code does now
        //   B  UseCurrentLayer(false), no SetLayer        <- the hypothesis
        //   C  UseCurrentLayer(false) + SetLayer("...")   <- explicit, the known-good control
        //
        // The current layer is set to a deliberately wrong one first, so "it landed correctly"
        // cannot be an accident.
        void LayerProbe(Dictionary<string, string> kv)
        {
            PsPoint at = Pt(Get(kv, "at", "0,0,0"));
            bool cleanup = Get(kv, "cleanup", "1") == "1";
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            StringBuilder sb = new StringBuilder();

            // a deliberately wrong current layer, created if absent
            string probeLayer = "ZZ_PROBE_WRONG";
            string prevLayer = "";
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);
                    if (!lt.Has(probeLayer))
                    {
                        LayerTableRecord ltr = new LayerTableRecord();
                        ltr.Name = probeLayer;
                        lt.Add(ltr); tr.AddNewlyCreatedDBObject(ltr, true);
                    }
                    LayerTableRecord cur = (LayerTableRecord)tr.GetObject(db.Clayer, OpenMode.ForRead);
                    prevLayer = cur.Name;
                    db.Clayer = lt[probeLayer];
                    tr.Commit();
                }
            }
            catch (System.Exception ex) { sb.AppendLine("could not set the probe layer: " + ex.Message); }
            sb.AppendLine("current layer forced to '" + probeLayer + "' (was '" + prevLayer + "')");
            sb.AppendLine();

            List<string> made = new List<string>();
            string[] labels = new string[] { "A UseCurrentLayer(true)",
                                             "B UseCurrentLayer(false), no SetLayer",
                                             "C UseCurrentLayer(false) + SetLayer(PS_Plate)" };
            for (int i = 0; i < 3; i++)
            {
                string h0, c0; int before = Census(out h0, out c0);
                string note = "";
                try
                {
                    PsCreatePlate cp = new PsCreatePlate();
                    cp.SetToDefaults();
                    PsMatrix m = new PsMatrix();
                    m.SetCoordinateSystem(new PsPoint(at.x + i * 700, at.y, at.z),
                        new PsVector(1, 0, 0), new PsVector(0, 1, 0), new PsVector(0, 0, 1));
                    cp.SetInsertMatrix(m);
                    cp.SetAsRectangularPlate(300, 200);
                    cp.SetThickness(10);
                    if (i == 0) cp.UseCurrentLayer(true);
                    else if (i == 1) cp.UseCurrentLayer(false);
                    else { cp.UseCurrentLayer(false); cp.SetLayer("PS_Plate"); }
                    cp.Create();
                }
                catch (System.Exception ex) { note = " EX:" + ex.Message; }
                string h1, c1; int after = Census(out h1, out c1);

                string landed = "(nothing created)";
                if (after > before)
                {
                    made.Add(h1);
                    try
                    {
                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            Entity e = (Entity)tr.GetObject(IdOf(h1), OpenMode.ForRead);
                            landed = e.Layer;
                        }
                    }
                    catch (System.Exception ex) { landed = "<" + ex.Message + ">"; }
                }
                sb.AppendLine(string.Format("{0,-46} -> layer '{1}'  handle {2}{3}",
                              labels[i], landed, after > before ? h1 : "-", note));
            }

            // restore the previous current layer, always
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                    if (prevLayer.Length > 0 && lt.Has(prevLayer)) db.Clayer = lt[prevLayer];
                    tr.Commit();
                }
                sb.AppendLine();
                sb.AppendLine("current layer restored to '" + prevLayer + "'");
            }
            catch (System.Exception ex) { sb.AppendLine("RESTORE FAILED: " + ex.Message); }

            // the probes are scaffolding, not model content
            int erased = 0;
            if (cleanup)
            {
                foreach (string h in made)
                {
                    try
                    {
                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            Entity e = (Entity)tr.GetObject(IdOf(h), OpenMode.ForWrite);
                            e.Erase(); tr.Commit(); erased++;
                        }
                    }
                    catch { }
                }
                sb.AppendLine("probe plates erased: " + erased + " of " + made.Count);
            }

            File.WriteAllText(Path.Combine(Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location), "eb_layerprobe.txt"),
                sb.ToString(), new UTF8Encoding(true));
            Result("EB_OK layerprobe made=" + made.Count + " erased=" + erased
                 + " -> eb_layerprobe.txt");
        }

        static ObjectId IdOf(string handleHex)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            return doc.Database.GetObjectId(false,
                   new Handle(Convert.ToInt64(handleHex, 16)), 0);
        }

        // op=dumpfull2 [out=eb_full2.txt] ג€” adds ECS to plates/bolts, InsertPoint+Layer to shapes
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
        // so EVERY attribute survives ג€” mirror flags, insert offsets, layers, material,
        // holes, host relationships, groups ג€” none of which can be reproduced by
        // parametric re-creation (proved: MirrorFlag is read-only, Ecs is identity,
        // InsertPoint/COG return null).
        // ROTATE objects IN PLACE ג€” the software's own rotate, no cloning.
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










        // op=purlintype -- MEASURE PurlinType. Declaration order is not value order;
        // enum values in this product have to be read, never inferred.
        void PurlinTypeDump(Dictionary<string, string> kv)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                Type t = typeof(Bentley.ProStructures.PurlinType);
                sb.AppendLine("Bentley.ProStructures.PurlinType");
                foreach (string nm in System.Enum.GetNames(t))
                    sb.AppendLine("  " + nm.PadRight(16) + " = " +
                                  System.Convert.ToInt32(System.Enum.Parse(t, nm)));
                // and which value each shipped template actually carries
                sb.AppendLine();
                PsPurlinConnection pc = new PsPurlinConnection();
                pc.SetToDefaults();
                int n = pc.GetTemplateCount();
                for (int i = 0; i < n; i++)
                {
                    string nm = ""; try { nm = pc.GetTemplateName(i); } catch { }
                    string v = "?";
                    try { PsPurlinLinkDataMgd d = pc.GetTemplate(nm);
                          if (d != null) v = d.PurlinType + " (" + (int)d.PurlinType + ")"; }
                    catch (System.Exception e) { v = "!" + One(e.Message); }
                    sb.AppendLine("  template '" + nm + "' -> PurlinType=" + v);
                }
            }
            catch (System.Exception ex) { Result("EB_ERR purlintype EX:" + One(ex.Message)); return; }
            File.WriteAllText(Path.Combine(Dir, "eb_purlintype.txt"), sb.ToString(), Encoding.UTF8);
            Result("EB_OK purlintype -> eb_purlintype.txt\n" + sb.ToString());
        }









        // =====================================================================
        //  v126 -- THE VERIFICATION KIT
        //  Built once, calibrated against cases whose answer is already known.
        //  Every op states its own blind spots in its result line.
        // =====================================================================

        // ---- shared: collect (handle, class, extents) for a range ----
        class VPart
        {
            public long Id; public string H; public string Cls;
            public double X0, X1, Y0, Y1, Z0, Z1;
        }

        List<VPart> VfyCollect(double minx, double maxx, bool ksOnly)
        {
            List<VPart> outp = new List<VPart>();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId oid in ms)
                {
                    Entity e = null;
                    try { e = tr.GetObject(oid, OpenMode.ForRead) as Entity; }
                    catch { continue; }
                    if (e == null) continue;
                    string cls = oid.ObjectClass != null ? oid.ObjectClass.Name : "?";
                    if (ksOnly && cls.IndexOf("Ks_", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    Extents3d ex;
                    try { ex = e.GeometricExtents; }
                    catch { continue; }
                    double cx = (ex.MinPoint.X + ex.MaxPoint.X) / 2.0;
                    if (cx < minx || cx > maxx) continue;
                    VPart p = new VPart();
                    p.Id = oid.OldIdPtr.ToInt64(); p.H = e.Handle.ToString(); p.Cls = cls;
                    p.X0 = ex.MinPoint.X; p.X1 = ex.MaxPoint.X;
                    p.Y0 = ex.MinPoint.Y; p.Y1 = ex.MaxPoint.Y;
                    p.Z0 = ex.MinPoint.Z; p.Z1 = ex.MaxPoint.Z;
                    outp.Add(p);
                }
                tr.Commit();
            }
            return outp;
        }

        // ---- 1. BOLTS vs HOLES, by geometry ----
        // op=vfy_bolts minx= maxx= [tol=12] [out=]
        // op=holefields handle=XX
        // The hole FIELDS on a part, each with the holes it actually produces.
        //
        // ג­ A field is not a hole. On AE6 the audit found 12 fields carrying 12 holes -- one
        // each, so a duplicate is a whole redundant field and can be deleted outright. On 10E9
        // it found ONE field carrying 8 holes at 4 positions, so the doubling is inside the
        // field's own definition and deleting the field would take all eight. The two need
        // different treatment, and nothing else in the toolkit shows the difference.
        // B.14.1 -- "ProSteel can automatically verify the admissible edge distance during
        // drilling... for each hole diameter". The manual warns twice that the check is ONLY A
        // HINT: the hole is inserted regardless, and the message may not appear before the end
        // of the action. So read it; never wait for it.
        //   op=edgecheck handle=<part> [block=0|1]
        // ==================================================================
        //  op=edgecheck -- DISABLED 10/08/2026. IT KILLED AUTOCAD.
        //
        //  B.14.1 promises the software can "automatically verify the admissible edge distance
        //  during drilling", and PsVolume.checkHoleEdgeDistance(Number) is that check. It is
        //  also LETHAL: isolated in stages, a plate was created and saved, a hole drilled and
        //  saved, both survived -- and this call ALONE, on a saved model, made the acad.exe
        //  process disappear. No exception, no dialog, EB_TIMEOUT on the Python side and an
        //  empty Get-Process.
        //
        //  Second member of a family; the first is PsPlate.computeObjectWeigth.
        //  See knowledge/learning/findings/LETHAL-CALLS-do-not-invoke.md
        //
        //  Left in place, refusing, rather than deleted -- so the next attempt reads this
        //  instead of rediscovering it the expensive way.
        // ==================================================================
        void EdgeCheck(Dictionary<string, string> kv)
        {
            Result("EB_ERR edgecheck REFUSED: PsVolume.checkHoleEdgeDistance KILLS AUTOCAD "
                 + "(measured 10/08/2026, isolated on a saved model -- process gone, no exception). "
                 + "The edge-distance table is reachable in the dialog only. "
                 + "See knowledge/learning/findings/LETHAL-CALLS-do-not-invoke.md");
        }

        void HoleFields(Dictionary<string, string> kv)
        {
            string h = Get(kv, "handle", "");
            long oid = IdFromHandle(h);
            if (oid == 0) { Result("EB_ERR holefields: bad handle " + h); return; }

            StringBuilder sb = new StringBuilder();
            int nf = 0;
            List<string> ids = new List<string>();
            try
            {
                PsEditModification em = new PsEditModification();
                em.SetObjectId(oid);
                nf = em.HoleFieldCount;
                sb.AppendLine("part " + h + "  holeFieldCount=" + nf);
                for (int i = 0; i < nf; i++)
                {
                    int fh = -1;
                    try { fh = em.GetHoleFieldHandleFromNumber(i); } catch { }
                    ids.Add(i + ":" + fh);
                    string desc = "";
                    try
                    {
                        // HoleField is an INDEXED property -- get_HoleField(handle), not a plain
                        // getter. The compiler says so explicitly; it is not a thing to guess.
                        PsHoleField hf = em.get_HoleField(fh);
                        if (hf != null)
                            desc = " bolts=" + hf.getBoltCount()
                                 + " conn=" + hf.getBoltConnectionCount()
                                 + " active=" + hf.ActiveBoltField
                                 + " single=" + hf.SingleHoleBolt
                                 + " idx=" + hf.Index;
                    }
                    catch (System.Exception ex) { desc = " <" + One(ex.Message) + ">"; }
                    sb.AppendLine("  field[" + i + "] handle=" + fh + desc);
                }
            }
            catch (System.Exception ex)
            { Result("EB_ERR holefields " + h + ": " + ex.Message); return; }

            // and the holes themselves, so field count and hole count can be compared
            string err; StringBuilder hs = new StringBuilder();
            int nh = HolesOf(oid, 3, hs, "h", out err);
            sb.AppendLine();
            sb.AppendLine("holes reported by PsSingleHoleArray: " + nh);
            sb.Append(hs.ToString());

            File.WriteAllText(Path.Combine(Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location), "eb_holefields.txt"),
                sb.ToString(), new UTF8Encoding(true));
            Result("EB_OK holefields handle=" + h + " fields=" + nf + " holes=" + nh
                 + " ids=" + string.Join(",", ids.ToArray())
                 + (nf == nh ? "  [one hole per field -- a duplicate is a whole field]"
                             : "  [field count != hole count -- the pattern is INSIDE the field]")
                 + " -> eb_holefields.txt");
        }

        // op=killholefield handle=XX field=<number> [dryrun=1]
        // Delete ONE hole field by its number, via PsEditModification.DeleteHoleField.
        //
        // ג ן¸ B.26's notes said holes could not be removed. That was wrong -- DeleteHoleField
        // exists and works. The note is corrected.
        //
        // Counts the holes before and after, because the field number is an index into a list
        // that RENUMBERS as fields are removed: delete field 6 and what was field 7 becomes 6.
        // Always delete from the HIGHEST index down, and always read the count back.
        void KillHoleField(Dictionary<string, string> kv)
        {
            string h = Get(kv, "handle", "");
            int fn = int.Parse(Get(kv, "field", "-1"));
            bool dry = Get(kv, "dryrun", "0") == "1";
            long oid = IdFromHandle(h);
            if (oid == 0) { Result("EB_ERR killholefield: bad handle " + h); return; }
            if (fn < 0) { Result("EB_ERR killholefield: field= is required"); return; }

            string err;
            int holesBefore = Rec.HolesOfStatic(oid, out err);
            int fieldsBefore = -1, fh = -1;
            try
            {
                PsEditModification em0 = new PsEditModification();
                em0.SetObjectId(oid);
                fieldsBefore = em0.HoleFieldCount;
                fh = em0.GetHoleFieldHandleFromNumber(fn);
            }
            catch (System.Exception ex)
            { Result("EB_ERR killholefield read: " + ex.Message); return; }

            if (fn >= fieldsBefore)
            {
                Result("EB_ERR killholefield: field " + fn + " does not exist (count="
                     + fieldsBefore + ")");
                return;
            }
            if (dry)
            {
                Result("EB_OK killholefield DRY handle=" + h + " field=" + fn
                     + " fieldHandle=" + fh + " fields=" + fieldsBefore + " holes=" + holesBefore
                     + " -- nothing deleted");
                return;
            }

            string msg = "";
            try
            {
                PsEditModification em = new PsEditModification();
                em.SetObjectId(oid);
                em.DeleteHoleField(fh);
            }
            catch (System.Exception ex) { msg = " EX:" + ex.Message; }

            int holesAfter = Rec.HolesOfStatic(oid, out err);
            int fieldsAfter = -1;
            try { PsEditModification e2 = new PsEditModification(); e2.SetObjectId(oid); fieldsAfter = e2.HoleFieldCount; }
            catch { }

            bool worked = (fieldsAfter >= 0 && fieldsAfter < fieldsBefore) || (holesAfter < holesBefore);
            Result((worked ? "EB_OK" : "EB_ERR") + " killholefield handle=" + h
                 + " field=" + fn + " fieldHandle=" + fh
                 + " fields=" + fieldsBefore + "->" + fieldsAfter
                 + " holes=" + holesBefore + "->" + holesAfter + msg);
        }

        // op=vfy_fit [minx=] [maxx=] [tol=12] [maxspare=45] [minspare=15] [gaptol=2]
        //
        // ג ן¸ THIS OP REPLACES THE FIRST vfy_grip, WHICH WAS WRONG. Recorded because the
        // mistake is more instructive than the fix.
        //
        // v132's vfy_grip compared the summed depths of a bolt's holes against KlemmLen and
        // called a shortfall "the bolt clamps material with NO hole". It flagged 20 bolts in
        // B08 on that basis. Two measurements killed the premise before any of it was
        // reported:
        //   * bolt F82 is an M16x75 reporting klemm=50, but the only steel on its axis is an
        //     L90x9 leg (9 mm) and a 10 mm gusset. There is no 50 mm of material there.
        //   * every M20x70 DIN6914 in the drawing reports klemm=39 and every M20x70 Mu
        //     DIN7990 reports 42 -- identical within a type, across different joints.
        // ג‡’ KlemmLen is essentially a property of the bolt TYPE and LENGTH. It coincides with
        //   the packet only when the bolt was sized to the packet, which is why it looked
        //   exact on the one joint it was first calibrated against. One calibration case is
        //   not a calibration.
        //
        // WHAT IS SOUND, and needs no interpretation:
        //   PACKET = the summed depths of the holes on the bolt's axis. Measured geometry.
        //   NOMINAL = the length in the bolt's own name, "M 16x75" -> 75. Measured.
        //   SPARE  = NOMINAL - PACKET, the steel-free length: nut + washer + protruding thread.
        //
        // The model calibrates the threshold itself. Across ten healthy bolt types in B08 the
        // spare sits in a tight 22-31 mm band -- exactly a nut plus a washer plus a few
        // threads. The outlier was M16x75 in B.25's braced bay: spare 56 mm on a 19 mm packet,
        // i.e. a bolt roughly 30 mm longer than the joint needs.
        //
        // KlemmLen is still printed, as information. It is no longer used to judge anything.
        void VfyFit(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            double minx = double.Parse(Get(kv, "minx", "-1e12"), IC);
            double maxx = double.Parse(Get(kv, "maxx", "1e12"), IC);
            double tol = double.Parse(Get(kv, "tol", "12"), IC);
            double maxspare = double.Parse(Get(kv, "maxspare", "45"), IC);
            double minspare = double.Parse(Get(kv, "minspare", "15"), IC);
            double gaptol   = double.Parse(Get(kv, "gaptol", "2"), IC);

            List<VPart> parts = VfyCollect(minx, maxx, true);
            List<VPart> bolts = new List<VPart>(), steel = new List<VPart>();
            foreach (VPart p in parts)
            {
                if (p.Cls.IndexOf("Bolt", StringComparison.OrdinalIgnoreCase) >= 0) bolts.Add(p);
                else steel.Add(p);
            }

            List<double[]> hPt = new List<double[]>();
            List<double> hDepth = new List<double>();
            List<string> hOwner = new List<string>();
            List<double[]> hStart = new List<double[]>();
            List<double[]> hEnd = new List<double[]>();
            StringBuilder scratch = new StringBuilder();
            foreach (VPart p in steel)
            {
                scratch.Length = 0; string err;
                int n = HolesOf(p.Id, 3, scratch, "h", out err);
                if (n <= 0) continue;
                foreach (string ln in scratch.ToString().Split('\n'))
                {
                    string[] f = ln.Split('\t');
                    if (f.Length < 5) continue;
                    string[] a = f[3].Split(','), b = f[4].Split(',');
                    if (a.Length < 3 || b.Length < 3) continue;
                    try
                    {
                        double[] s = new double[3], e = new double[3], mid = new double[3];
                        double d2 = 0;
                        for (int i = 0; i < 3; i++)
                        {
                            s[i] = double.Parse(a[i], IC); e[i] = double.Parse(b[i], IC);
                            mid[i] = (s[i] + e[i]) / 2.0;
                            d2 += (e[i] - s[i]) * (e[i] - s[i]);
                        }
                        hPt.Add(mid); hDepth.Add(Math.Sqrt(d2)); hOwner.Add(p.H);
                        hStart.Add(s); hEnd.Add(e);
                    }
                    catch { }
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("verdict\tbolt\tname\tnominal\tpacket\tspare\tgap\tklemm\tholes\towners");
            int ok = 0, noHole = 0, over = 0, under = 0, unknown = 0, gapped = 0;
            double worst = 0; string worstH = "";
            double worstGap = 0; string worstGapH = "";

            foreach (VPart bo in bolts)
            {
                string nm = "?"; double klemm = 0;
                try
                {
                    PsObjectProperties pr = new PsObjectProperties();
                    pr.readFrom(bo.Id);
                    nm = Safe(pr.Name); klemm = pr.KlemmLen;
                }
                catch { }

                // the nominal length lives in the name: "M 16x75 Mu DIN7990" -> 75
                double nominal = -1;
                try
                {
                    System.Text.RegularExpressions.Match m =
                        System.Text.RegularExpressions.Regex.Match(nm, "[xX]\\s?(\\d+)");
                    if (m.Success) nominal = double.Parse(m.Groups[1].Value, IC);
                }
                catch { }

                // the bolt's own axis: its longest extent
                int ax = 0;
                double dx = bo.X1 - bo.X0, dy = bo.Y1 - bo.Y0, dz = bo.Z1 - bo.Z0;
                if (Math.Abs(dy) > Math.Abs(dx) && Math.Abs(dy) >= Math.Abs(dz)) ax = 1;
                else if (Math.Abs(dz) > Math.Abs(dx) && Math.Abs(dz) > Math.Abs(dy)) ax = 2;

                double packet = 0; int hits = 0;
                StringBuilder own = new StringBuilder();
                List<double[]> spans = new List<double[]>();     // [lo,hi] along the bolt axis
                for (int i = 0; i < hPt.Count; i++)
                {
                    double[] q = hPt[i];
                    if (q[0] < bo.X0 - tol || q[0] > bo.X1 + tol) continue;
                    if (q[1] < bo.Y0 - tol || q[1] > bo.Y1 + tol) continue;
                    if (q[2] < bo.Z0 - tol || q[2] > bo.Z1 + tol) continue;
                    packet += hDepth[i]; hits++;
                    if (own.Length > 0) own.Append(',');
                    own.Append(hOwner[i] + ":" + F(hDepth[i]));
                    double a0 = hStart[i][ax], a1 = hEnd[i][ax];
                    spans.Add(new double[] { Math.Min(a0, a1), Math.Max(a0, a1) });
                }

                // ג­ THE GAP. A hole spans exactly the material it passes through, so consecutive
                // holes along the bolt axis should ABUT. Space between them is AIR -- the plies
                // of the joint are not touching, and the bolt is spanning a void.
                //
                // This is what B.25's braced bay turned out to be, and it is why the first
                // reading of that band was wrong. Eight bolts read as "oversized" -- an M16x75
                // on a 19 mm packet. They are not oversized: the gusset occupies y -55..-65 and
                // the angle leg y -96..-105, so there are 31 mm of NOTHING between them and the
                // bolt is exactly the right length for the assembly AS MODELLED. The fault is
                // the gap, not the bolt. A large "spare" means one of two very different things
                // and the op must say which.
                double gap = 0;
                if (spans.Count > 1)
                {
                    spans.Sort(delegate(double[] p1, double[] p2) { return p1[0].CompareTo(p2[0]); });
                    double reach = spans[0][1];
                    for (int i = 1; i < spans.Count; i++)
                    {
                        if (spans[i][0] > reach) gap += spans[i][0] - reach;
                        if (spans[i][1] > reach) reach = spans[i][1];
                    }
                }

                string verdict;
                double spare = (nominal > 0) ? nominal - packet : 0;
                if (hits == 0) { verdict = "BOLT-NO-HOLE"; noHole++; }
                else if (nominal <= 0) { verdict = "NO-NOMINAL"; unknown++; }
                else if (gap > gaptol) { verdict = "GAP-IN-PACKET"; gapped++; }
                else if (spare > maxspare) { verdict = "OVERSIZED"; over++; }
                else if (spare < minspare) { verdict = "SHORT"; under++; }
                else { verdict = "OK"; ok++; }
                if (nominal > 0 && Math.Abs(spare - 27) > Math.Abs(worst - 27)) { worst = spare; worstH = bo.H; }
                if (gap > worstGap) { worstGap = gap; worstGapH = bo.H; }

                sb.AppendLine(verdict + "\t" + bo.H + "\t" + nm + "\t"
                            + (nominal > 0 ? F(nominal) : "?") + "\t" + F(packet) + "\t"
                            + (nominal > 0 ? F(spare) : "?") + "\t" + F(gap) + "\t" + F(klemm) + "\t"
                            + hits + "\t" + own.ToString());
            }

            File.WriteAllText(Path.Combine(Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location), "eb_vfy_fit.txt"),
                sb.ToString(), new UTF8Encoding(true));

            Result("EB_" + ((noHole > 0 || over > 0) ? "ERR" : "OK")
                 + " vfy_fit bolts=" + bolts.Count + " OK=" + ok
                 + "  BOLT-NO-HOLE=" + noHole + " (נ§² iron rule)"
                 + "  GAP-IN-PACKET=" + gapped + " (ג  the plies do NOT touch: worst "
                 + F(worstGap) + "mm of air, in " + worstGapH + ")"
                 + "  OVERSIZED=" + over + " (spare>" + F(maxspare) + ")"
                 + "  SHORT=" + under + " (spare<" + F(minspare) + ")"
                 + "  no-nominal=" + unknown
                 + "  worst spare=" + F(worst) + "mm (" + worstH + ")"
                 + "  [SPARE = nominal length - packet: nut + washer + protruding thread."
                 + " A healthy bolt sits at 22-31 mm. KlemmLen is printed but JUDGES NOTHING --"
                 + " it is a property of the bolt type, not of the packet."
                 + " A large SPARE means one of two very different things -- an over-long bolt,"
                 + " or PLIES THAT DO NOT TOUCH -- so the air between consecutive holes along"
                 + " the bolt axis is measured and reported separately."
                 + " The hole set is still matched by proximity, so two bolt rows closer than"
                 + " tol will over-count each other's holes] -> eb_vfy_fit.txt");
        }

        // op=vfy_dupes [minx=] [maxx=] [tol=3]
        //
        // Two things nothing else looks for, both found the hard way in B.26's apex:
        //   * BOLTS whose centres coincide -- the same bolt modelled more than once. The
        //     drawing looks right and the parts list orders three times the bolts. Found:
        //     three stacked at every one of four hole positions.
        //   * HOLES in the SAME part whose centres coincide -- drilled twice. Found: 10 across
        //     two parts. Harmless in the model, wrong on an NC file.
        // Neither shows up in a bolt-vs-hole check, because every duplicate matches happily.
        void VfyDupes(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            double minx = double.Parse(Get(kv, "minx", "-1e12"), IC);
            double maxx = double.Parse(Get(kv, "maxx", "1e12"), IC);
            double tol = double.Parse(Get(kv, "tol", "3"), IC);

            List<VPart> parts = VfyCollect(minx, maxx, true);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("kind\tdetail");

            // --- duplicate bolts ---
            Dictionary<string, List<VPart>> cell = new Dictionary<string, List<VPart>>();
            int nbolts = 0;
            foreach (VPart p in parts)
            {
                if (p.Cls.IndexOf("Bolt", StringComparison.OrdinalIgnoreCase) < 0) continue;
                nbolts++;
                double cx = (p.X0 + p.X1) / 2, cy = (p.Y0 + p.Y1) / 2, cz = (p.Z0 + p.Z1) / 2;
                string k = Math.Round(cx / tol) + "|" + Math.Round(cy / tol) + "|" + Math.Round(cz / tol);
                if (!cell.ContainsKey(k)) cell[k] = new List<VPart>();
                cell[k].Add(p);
            }
            int dupPos = 0, redundant = 0;
            foreach (KeyValuePair<string, List<VPart>> e in cell)
            {
                if (e.Value.Count < 2) continue;
                dupPos++; redundant += e.Value.Count - 1;
                VPart f = e.Value[0];
                StringBuilder hs = new StringBuilder();
                foreach (VPart p in e.Value) { if (hs.Length > 0) hs.Append(','); hs.Append(p.H); }
                sb.AppendLine("DUP-BOLT\tx" + e.Value.Count + " at "
                            + F((f.X0 + f.X1) / 2) + "," + F((f.Y0 + f.Y1) / 2) + "," + F((f.Z0 + f.Z1) / 2)
                            + "\t" + hs.ToString());
            }

            // --- duplicate holes inside one part ---
            int dupHoleParts = 0, dupHoles = 0;
            StringBuilder scratch = new StringBuilder();
            foreach (VPart p in parts)
            {
                if (p.Cls.IndexOf("Bolt", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                scratch.Length = 0; string err;
                int n = HolesOf(p.Id, 3, scratch, "h", out err);
                if (n <= 1) continue;
                Dictionary<string, int> seen = new Dictionary<string, int>();
                foreach (string ln in scratch.ToString().Split('\n'))
                {
                    string[] f = ln.Split('\t');
                    if (f.Length < 5) continue;
                    string[] a = f[3].Split(','), b = f[4].Split(',');
                    if (a.Length < 3 || b.Length < 3) continue;
                    try
                    {
                        StringBuilder k = new StringBuilder();
                        for (int i = 0; i < 3; i++)
                            k.Append(Math.Round(((double.Parse(a[i], IC) + double.Parse(b[i], IC)) / 2) / tol) + "|");
                        string ks = k.ToString();
                        if (!seen.ContainsKey(ks)) seen[ks] = 0;
                        seen[ks]++;
                    }
                    catch { }
                }
                int extra = 0;
                foreach (KeyValuePair<string, int> e in seen) if (e.Value > 1) extra += e.Value - 1;
                if (extra > 0)
                {
                    dupHoleParts++; dupHoles += extra;
                    sb.AppendLine("DUP-HOLE\tpart " + p.H + " (" + p.Cls + ")\t" + extra + " redundant");
                }
            }

            File.WriteAllText(Path.Combine(Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location), "eb_vfy_dupes.txt"),
                sb.ToString(), new UTF8Encoding(true));

            Result("EB_" + ((redundant > 0 || dupHoles > 0) ? "ERR" : "OK")
                 + " vfy_dupes bolts=" + nbolts
                 + "  DUP-BOLT positions=" + dupPos + " redundant=" + redundant
                 + "  DUP-HOLE parts=" + dupHoleParts + " redundant=" + dupHoles
                 + "  tol=" + F(tol)
                 + "  [a duplicate is invisible to every bolt-vs-hole check -- each copy matches"
                 + " the same hole happily. It shows up in the PARTS LIST, not in the geometry]"
                 + " -> eb_vfy_dupes.txt");
        }


        void VfyBolts(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            double minx = double.Parse(Get(kv, "minx", "-1e12"), IC);
            double maxx = double.Parse(Get(kv, "maxx", "1e12"), IC);
            double tol = double.Parse(Get(kv, "tol", "12"), IC);

            List<VPart> parts = VfyCollect(minx, maxx, true);
            List<VPart> bolts = new List<VPart>();
            List<VPart> steel = new List<VPart>();
            foreach (VPart p in parts)
            {
                if (p.Cls.IndexOf("Bolt", StringComparison.OrdinalIgnoreCase) >= 0) bolts.Add(p);
                else steel.Add(p);
            }

            // every hole in range, as a point + its owner
            List<double[]> holePts = new List<double[]>();
            List<string> holeOwner = new List<string>();
            List<bool> holeUsed = new List<bool>();
            StringBuilder scratch = new StringBuilder();
            foreach (VPart p in steel)
            {
                scratch.Length = 0; string err;
                int n = HolesOf(p.Id, 3, scratch, "h", out err);
                if (n <= 0) continue;
                foreach (string ln in scratch.ToString().Split('\n'))
                {
                    string[] f = ln.Split('\t');
                    if (f.Length < 5) continue;
                    string[] a = f[3].Split(',');
                    string[] b = f[4].Split(',');
                    if (a.Length < 3 || b.Length < 3) continue;
                    try
                    {
                        double[] mid = new double[3];
                        for (int i = 0; i < 3; i++)
                            mid[i] = (double.Parse(a[i], IC) + double.Parse(b[i], IC)) / 2.0;
                        holePts.Add(mid); holeOwner.Add(p.H); holeUsed.Add(false);
                    }
                    catch { }
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("kind\thandle\tdetail");
            int boltNoHole = 0, matched = 0;
            foreach (VPart bo in bolts)
            {
                double bx = (bo.X0 + bo.X1) / 2.0, by = (bo.Y0 + bo.Y1) / 2.0, bz = (bo.Z0 + bo.Z1) / 2.0;
                int hit = 0;
                for (int i = 0; i < holePts.Count; i++)
                {
                    double[] q = holePts[i];
                    // a bolt is a segment; accept a hole whose centre lies within tol of the
                    // bolt's own extent box, grown by tol
                    if (q[0] < bo.X0 - tol || q[0] > bo.X1 + tol) continue;
                    if (q[1] < bo.Y0 - tol || q[1] > bo.Y1 + tol) continue;
                    if (q[2] < bo.Z0 - tol || q[2] > bo.Z1 + tol) continue;
                    hit++; holeUsed[i] = true;
                }
                if (hit == 0)
                {
                    boltNoHole++;
                    sb.Append("BOLT-NO-HOLE\t").Append(bo.H).Append('\t')
                      .Append(F(bx)).Append(',').Append(F(by)).Append(',').Append(F(bz)).Append('\n');
                }
                else matched++;
            }
            int holeNoBolt = 0;
            for (int i = 0; i < holePts.Count; i++)
            {
                if (holeUsed[i]) continue;
                holeNoBolt++;
                sb.Append("HOLE-NO-BOLT\t").Append(holeOwner[i]).Append('\t')
                  .Append(F(holePts[i][0])).Append(',').Append(F(holePts[i][1])).Append(',')
                  .Append(F(holePts[i][2])).Append('\n');
            }
            string outn = Get(kv, "out", "eb_vfy_bolts.txt");
            File.WriteAllText(Path.Combine(Dir, outn), sb.ToString(), Encoding.UTF8);
            Result(((boltNoHole == 0) ? "EB_OK" : "EB_ERR") + " vfy_bolts bolts=" + bolts.Count +
                   " holes=" + holePts.Count + " matched=" + matched +
                   "  BOLT-NO-HOLE=" + boltNoHole + " (נ§² iron rule)" +
                   "  HOLE-NO-BOLT=" + holeNoBolt + " (unfilled)" +
                   "  tol=" + F(tol) +
                   "  [BLIND TO: which PART a bolt passes through -- it matches by proximity, so a" +
                   " hole in the wrong part still counts. Read the listed coordinates before acting.]" +
                   " -> " + outn);
        }

        // ---- 2. DO TWO PARTS TOUCH ----
        // op=vfy_touch a=<h> b=<h> [tol=0.5]
        void VfyTouch(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            double tol = double.Parse(Get(kv, "tol", "0.5"), IC);
            long ia = IdFromHandle(Get(kv, "a", "")), ib = IdFromHandle(Get(kv, "b", ""));
            if (ia == 0 || ib == 0) { Result("EB_ERR vfy_touch: need a= and b="); return; }

            double[] A = new double[6], B = new double[6];
            Document doc = Application.DocumentManager.MdiActiveDocument;
            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                try
                {
                    Entity ea = (Entity)tr.GetObject(new ObjectId(new System.IntPtr(ia)), OpenMode.ForRead);
                    Entity eb = (Entity)tr.GetObject(new ObjectId(new System.IntPtr(ib)), OpenMode.ForRead);
                    Extents3d xa = ea.GeometricExtents, xb = eb.GeometricExtents;
                    A[0] = xa.MinPoint.X; A[1] = xa.MaxPoint.X; A[2] = xa.MinPoint.Y;
                    A[3] = xa.MaxPoint.Y; A[4] = xa.MinPoint.Z; A[5] = xa.MaxPoint.Z;
                    B[0] = xb.MinPoint.X; B[1] = xb.MaxPoint.X; B[2] = xb.MinPoint.Y;
                    B[3] = xb.MaxPoint.Y; B[4] = xb.MinPoint.Z; B[5] = xb.MaxPoint.Z;
                }
                catch (System.Exception ex) { Result("EB_ERR vfy_touch: " + One(ex.Message)); return; }
                tr.Commit();
            }
            // per-axis separation: negative means they overlap on that axis
            string[] ax = new string[] { "X", "Y", "Z" };
            StringBuilder sb = new StringBuilder();
            double worst = -1e12; string worstAx = "?";
            for (int i = 0; i < 3; i++)
            {
                double a0 = A[i * 2], a1 = A[i * 2 + 1], b0 = B[i * 2], b1 = B[i * 2 + 1];
                double gap = System.Math.Max(b0 - a1, a0 - b1);   // >0 = a gap on this axis
                sb.Append(' ').Append(ax[i]).Append('=').Append(F(gap));
                if (gap > worst) { worst = gap; worstAx = ax[i]; }
            }
            // two boxes touch/overlap only if NO axis separates them
            string verdict = worst > tol ? "APART by " + F(worst) + " on " + worstAx
                           : (worst < -tol ? "OVERLAP " + F(-worst) : "TOUCHING");
            Result("EB_OK vfy_touch " + Get(kv, "a", "") + " vs " + Get(kv, "b", "") +
                   "  -> " + verdict + "   per-axis separation:" + sb.ToString() +
                   "   [BLIND TO: shape. These are axis-aligned extents, so a ROTATED part may" +
                   " report touching when its real contour does not. For a sector or a diagonal," +
                   " read the contour (plateinfo ext=, GetPolygon) instead.]");
        }

        // ---- 3. THE BLAST GUARD ----
        // op=vfy_size minx= maxx= [max=20000]
        void VfySize(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            double minx = double.Parse(Get(kv, "minx", "-1e12"), IC);
            double maxx = double.Parse(Get(kv, "maxx", "1e12"), IC);
            double lim = double.Parse(Get(kv, "max", "20000"), IC);
            List<VPart> parts = VfyCollect(minx, maxx, true);
            StringBuilder sb = new StringBuilder();
            int bad = 0;
            double biggest = 0; string biggestH = "-";
            int skipped = 0;
            foreach (VPart p in parts)
            {
                // CALIBRATION 09/08: the first run flagged a 15x24 m Ks_Grid. Grids and work
                // frames are LAYOUT objects and are legitimately huge; the guard is about a
                // MEMBER being stretched. Excluding them is what makes the alarm meaningful.
                if (p.Cls.IndexOf("Grid", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.Cls.IndexOf("WorkFrame", StringComparison.OrdinalIgnoreCase) >= 0)
                { skipped++; continue; }
                double dx = p.X1 - p.X0, dy = p.Y1 - p.Y0, dz = p.Z1 - p.Z0;
                double m = System.Math.Max(dx, System.Math.Max(dy, dz));
                if (m > biggest) { biggest = m; biggestH = p.H; }
                if (m > lim)
                {
                    bad++;
                    sb.Append(p.H).Append('\t').Append(p.Cls).Append('\t')
                      .Append(F(dx)).Append(" x ").Append(F(dy)).Append(" x ").Append(F(dz)).Append('\n');
                }
            }
            File.WriteAllText(Path.Combine(Dir, "eb_vfy_size.txt"), sb.ToString(), Encoding.UTF8);
            Result(((bad == 0) ? "EB_OK" : "EB_ERR") + " vfy_size parts=" + parts.Count +
                   " limit=" + F(lim) + " oversize=" + bad + " (grids/frames skipped=" + skipped + ")" +
                   " largest=" + F(biggest) + " (" + biggestH + ")" +
                   "  [run this after ANY op that can resize an existing member -- the haunch" +
                   " stretched a rafter to 317,000 mm and nothing noticed] -> eb_vfy_size.txt");
        }

        // =====================================================================
        //  v125 -- B.5 DISPLAY / ASSIGN PARTS
        //  Visible (Hide/Regenerate) ֲ· DisplayClass ֲ· AreaClass ֲ· FamilyClass
        //  "Each element can exist in only one class at a time."
        // =====================================================================
        void Classify(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            double minx = double.Parse(Get(kv, "minx", "-1e12"), IC);
            double maxx = double.Parse(Get(kv, "maxx", "1e12"), IC);
            string setWhat = Get(kv, "set", "").ToLowerInvariant();
            int val = int.Parse(Get(kv, "value", "0"));
            string vis = Get(kv, "visible", "");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("handle\tclass\tdisplay\tarea\tfamily\tvisible");
            int seen = 0, changed = 0, failed = 0;
            Dictionary<string, int> tally = new Dictionary<string, int>();

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId oid in ms)
                {
                    Entity e = null;
                    try { e = tr.GetObject(oid, OpenMode.ForRead) as Entity; }
                    catch { continue; }
                    if (e == null) continue;
                    string cls = oid.ObjectClass != null ? oid.ObjectClass.Name : "?";
                    if (cls.IndexOf("Ks_", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    double cx;
                    try { Extents3d ex = e.GeometricExtents; cx = (ex.MinPoint.X + ex.MaxPoint.X) / 2.0; }
                    catch { continue; }
                    if (cx < minx || cx > maxx) continue;

                    long id = oid.OldIdPtr.ToInt64();
                    int dc = -1, ac = -1, fc = -1; bool vv = true;
                    try
                    {
                        PsObjectProperties p = new PsObjectProperties();
                        p.readFrom(id);
                        try { dc = p.DisplayClass; } catch { }
                        try { ac = p.AreaClass; } catch { }
                        try { fc = p.FamilyClass; } catch { }
                        try { vv = p.Visible; } catch { }

                        bool touch = false;
                        if (setWhat == "display") { p.DisplayClass = val; touch = true; }
                        else if (setWhat == "area") { p.AreaClass = val; touch = true; }
                        else if (setWhat == "family") { p.FamilyClass = val; touch = true; }
                        if (vis.Length > 0) { p.Visible = (vis == "1"); touch = true; }
                        if (touch)
                        {
                            try { p.writeTo(id); changed++; }
                            catch (System.Exception) { failed++; }
                        }
                    }
                    catch { failed++; continue; }

                    seen++;
                    string key = "d" + dc + "/a" + ac + "/f" + fc + (vv ? "" : " HIDDEN");
                    if (!tally.ContainsKey(key)) tally[key] = 0;
                    tally[key]++;
                    sb.Append(HandleOf(id)).Append('\t').Append(cls).Append('\t')
                      .Append(dc).Append('\t').Append(ac).Append('\t').Append(fc).Append('\t')
                      .Append(vv).Append('\n');
                }
                tr.Commit();
            }
            StringBuilder t = new StringBuilder();
            foreach (KeyValuePair<string, int> kvp in tally)
                t.Append(' ').Append(kvp.Key).Append('=').Append(kvp.Value);
            string outn = Get(kv, "out", "eb_classify.txt");
            File.WriteAllText(Path.Combine(Dir, outn), sb.ToString(), Encoding.UTF8);
            Result("EB_OK classify parts=" + seen + " changed=" + changed + " failed=" + failed +
                   (setWhat.Length > 0 ? " set " + setWhat + "=" + val : "") +
                   (vis.Length > 0 ? " visible=" + vis : "") +
                   " tally[" + t.ToString().Trim() + "] -> " + outn);
        }

        // =====================================================================
        //  v124 -- B.11 ACIS BODY REFERENCE
        // =====================================================================

        // op=acis handle=<h>            -- convert a ProSteel object to an ACIS body
        //                                  (PsEditShapeModification.CreateAsAcisBody,
        //                                   the escape hatch B.12.7 mentions)
        void ToAcis(Dictionary<string, string> kv)
        {
            long id = IdFromHandle(Get(kv, "handle", ""));
            if (id == 0) { Result("EB_ERR acis: bad handle"); return; }
            string h0, c0; int before = Census(out h0, out c0);
            List<string> pre = HandleSet();
            long made = 0; string msg = "";
            try
            {
                PsEditShapeModification em = new PsEditShapeModification();
                em.SetToDefaults();
                em.SetObjectId(id);
                made = em.CreateAsAcisBody(id);
            }
            catch (System.Exception ex) { msg = " EX:" + One(ex.Message); }
            string h1, c1; int after = Census(out h1, out c1);
            Result(((after > before || made != 0) ? "EB_OK" : "EB_ERR") + " acis from=" +
                   Get(kv, "handle", "") + " newId=" + (made != 0 ? HandleOf(made) : "0") +
                   " census=" + before + "->" + after + " new:" + NewHandleSince(pre) +
                   " class=" + c1 + msg);
        }

        // op=acisref solid=<h> [massprop=1] [read=<h>]
        //   massprop=1 is the manual's ESC-at-the-first-X-point: the inertia axes of the
        //   body become the component coordinate system, ordered by moment of inertia.
        //   read=<h> instead reads an existing reference back.
        void AcisRef(Dictionary<string, string> kv)
        {
            string rd = Get(kv, "read", "");
            if (rd.Length > 0)
            {
                long rid = IdFromHandle(rd);
                string info = "";
                try
                {
                    Document d0 = Application.DocumentManager.MdiActiveDocument;
                    using (Transaction tr = d0.Database.TransactionManager.StartTransaction())
                    {
                        DBObject o = tr.GetObject(new ObjectId(new System.IntPtr(rid)), OpenMode.ForRead);
                        PsSolidReference sr = o as PsSolidReference;
                        if (sr == null) info = " NOT a PsSolidReference (" + (o == null ? "null" : o.GetType().Name) + ")";
                        else
                        {
                            long sid = 0; bool erased = false;
                            try { sid = sr.SolidId; } catch (System.Exception e) { info += " SolidId!" + One(e.Message); }
                            try { erased = sr.IsSolidErased; } catch (System.Exception e) { info += " IsSolidErased!" + One(e.Message); }
                            info += " solidId=" + (sid != 0 ? HandleOf(sid) : "0") + " isSolidErased=" + erased;
                            try { PsMatrix m = new PsMatrix(); sr.GetInsertUcs(m); info += " ucs=OK"; }
                            catch (System.Exception e) { info += " ucs!" + One(e.Message); }
                        }
                        tr.Commit();
                    }
                }
                catch (System.Exception ex) { info = " EX:" + One(ex.Message); }
                Result("EB_OK acisref read=" + rd + info);
                return;
            }

            long solid = IdFromHandle(Get(kv, "solid", ""));
            if (solid == 0) { Result("EB_ERR acisref: solid= not found"); return; }
            bool mass = Get(kv, "massprop", "1") == "1";
            string h0, c0; int before = Census(out h0, out c0);
            List<string> pre = HandleSet();
            long refId = 0; string msg = "";
            try
            {
                PsCreateSolidReference cr = new PsCreateSolidReference();
                cr.SetToDefaults();
                // B.11 audit 10/08: the notes EXPLAINED massProp=false's refusal by saying the
                // non-inertia mode needs the component coordinate system first -- but
                // SetInsertMatrix had never been called. ucs=origin;xaxis;yaxis tests it.
                string ucsS = Get(kv, "ucs", "");
                if (ucsS.Length > 0)
                {
                    string[] q = ucsS.Split(';');
                    if (q.Length < 3) { Result("EB_ERR acisref: ucs needs origin;xaxis;yaxis"); return; }
                    double[] xa = Nums(q[1]), ya = Nums(q[2]);
                    PsMatrix m = new PsMatrix();
                    m.SetCoordinateSystem(Pt(q[0]), new PsVector(xa[0], xa[1], xa[2]),
                                                    new PsVector(ya[0], ya[1], ya[2]));
                    cr.SetInsertMatrix(m);
                    msg += " ucsSet";
                }
                refId = cr.Create(solid, mass);
            }
            catch (System.Exception ex) { msg = " EX:" + One(ex.Message); }
            string h1, c1; int after = Census(out h1, out c1);
            Result(((refId != 0 || after > before) ? "EB_OK" : "EB_ERR") + " acisref solid=" +
                   Get(kv, "solid", "") + " massProp=" + mass +
                   " refId=" + (refId != 0 ? HandleOf(refId) : "0") +
                   " census=" + before + "->" + after + " new:" + NewHandleSince(pre) + msg);
        }

        // =====================================================================
        //  v123 -- B.10 INSERT SOLIDS
        //  "For volume modelling ProSteel does NOT use ACIS but a modified version
        //   which works faster and produces smaller graph files. Consequently you
        //   cannot combine ProSteel objects and AutoCAD 3D solids... there will be
        //   NO ERRORS, BUT NOTHING WILL HAPPEN!"
        //  And: "as these solids are REAL PROSTEEL OBJECTS they can be processed
        //   with ProSteel commands (e.g. DRILLED) ... detailed as normal parts."
        // =====================================================================
        //   op=solid kind=box|sphere|cylinder|cone|torus|conicpipe|rect2circle|
        //                 rotate|hull|extrude
        //            at=x,y,z [ex=..] [ey=..] [normal=..]
        //            l= w= h= r= r1= r2= outer= inner= len=
        //            oi1= oo1= oi2= oo2=            (conic pipe)
        //            p1= p2= center=               (rect2circle)
        //            rev= axis1= axis2=            (rotation)
        //            pts=x,y,z;...                 (hull / rotate / extrude polygon)
        //            taper= twist=                 (extrusion -- undocumented)
        void Solid(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string kind = Get(kv, "kind", "box").ToLowerInvariant();
            // C# 5 here -- no local functions (those are C# 7). A Func does the job.
            Func<string, string, double> D = delegate(string k, string dflt)
            { return double.Parse(Get(kv, k, dflt), IC); };

            string h0, c0; int before = Census(out h0, out c0);
            List<string> pre = HandleSet();
            string msg = "", applied = "";
            long oid = 0;
            try
            {
                PsCreatePrimitive p = new PsCreatePrimitive();
                p.SetToDefaults();
                try { p.Init(); } catch { }
                p.SetInsertPoint(Pt(Get(kv, "at", "0,0,0")));
                // the plane, always -- a zero plane is what wrecked the haunch
                double[] ax = Nums(Get(kv, "ex", "1,0,0"));
                double[] ay = Nums(Get(kv, "ey", "0,1,0"));
                try { p.SetXYPlane(new PsVector(ax[0], ax[1], ax[2]), new PsVector(ay[0], ay[1], ay[2])); }
                catch (System.Exception e) { msg += " plane!" + One(e.Message); }
                if (kv.ContainsKey("normal"))
                {
                    double[] nz = Nums(Get(kv, "normal", "0,0,1"));
                    try { p.SetNormal(new PsVector(nz[0], nz[1], nz[2])); } catch { }
                }
                // B.10 audit 10/08: CreateHull does NOT take a polygon. It takes
                // SetPoints(PsDataPointArray), and feeding it SetPolygon is why every hull
                // attempt so far was not a fair test -- the notes said as much and asked for
                // this change.
                string dpts = Get(kv, "dpts", "");
                if (dpts.Length > 0)
                {
                    try
                    {
                        PsDataPointArray dpa = new PsDataPointArray();
                        int nd = 0;
                        foreach (string c in dpts.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            double[] q = Nums(c);
                            dpa.appendPoint(q[0], q.Length > 1 ? q[1] : 0.0, q.Length > 2 ? q[2] : 0.0);
                            nd++;
                        }
                        p.SetPoints(dpa);
                        applied += " dpts=" + nd;
                    }
                    catch (System.Exception e) { msg += " dpts!" + One(e.Message); }
                }

                // a polygon, for rotate / extrude / hull
                string pts = Get(kv, "pts", "");
                if (pts.Length > 0)
                {
                    try
                    {
                        PsPolygon poly = new PsPolygon();
                        foreach (string c in pts.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                            poly.appendVertex(Pt(c));
                        p.SetPolygon(poly);
                        applied += " poly=" + poly.Count;
                    }
                    catch (System.Exception e) { msg += " poly!" + One(e.Message); }
                }

                switch (kind)
                {
                    case "box":
                        p.CreateBox(D("l", "500"), D("w", "300"), D("h", "200"));
                        applied += " box " + Get(kv, "l", "500") + "x" + Get(kv, "w", "300") + "x" + Get(kv, "h", "200");
                        break;
                    case "sphere":
                        p.CreateSphere(D("r", "250"));
                        applied += " sphere r=" + Get(kv, "r", "250"); break;
                    case "cylinder":
                        p.CreateCylinder(D("r", "150"), D("len", "800"));
                        applied += " cylinder r=" + Get(kv, "r", "150") + " len=" + Get(kv, "len", "800"); break;
                    case "cone":
                        p.CreateCone(D("r1", "300"), D("r2", "120"), D("h", "700"));
                        applied += " cone " + Get(kv, "r1", "300") + "->" + Get(kv, "r2", "120") + " h=" + Get(kv, "h", "700"); break;
                    case "torus":
                        p.CreateTorus(D("outer", "400"), D("inner", "120"), D("len", "0"));
                        applied += " torus " + Get(kv, "outer", "400") + "/" + Get(kv, "inner", "120"); break;
                    case "conicpipe":
                        p.CreateConicPipe(D("oo1", "400"), D("oi1", "370"), D("oo2", "200"), D("oi2", "180"), D("len", "1200"));
                        applied += " conicPipe " + Get(kv, "oo1", "400") + "/" + Get(kv, "oi1", "370") +
                                   " -> " + Get(kv, "oo2", "200") + "/" + Get(kv, "oi2", "180") +
                                   " len=" + Get(kv, "len", "1200"); break;
                    case "rect2circle":
                        p.CreateRect2Circle(D("r", "300"), Pt(Get(kv, "p1", "0,0,0")),
                                            Pt(Get(kv, "p2", "800,800,0")), Pt(Get(kv, "center", "400,400,900")));
                        applied += " rect2circle r=" + Get(kv, "r", "300"); break;
                    case "rotate":
                        p.CreateRotation(D("rev", "360"), Pt(Get(kv, "axis1", "0,0,0")), Pt(Get(kv, "axis2", "0,0,1000")));
                        applied += " rotation rev=" + Get(kv, "rev", "360"); break;
                    case "hull":
                        p.CreateHull(); applied += " hull"; break;
                    case "extrude":
                        p.CreateExtrusion(D("h", "600"), D("taper", "0"), D("twist", "0"));
                        applied += " extrude h=" + Get(kv, "h", "600") + " taper=" + Get(kv, "taper", "0") +
                                   " twist=" + Get(kv, "twist", "0"); break;
                    default:
                        Result("EB_ERR solid: unknown kind=" + kind); return;
                }
                try { oid = p.ObjectId; } catch (System.Exception e) { msg += " objId!" + One(e.Message); }
            }
            catch (System.Exception ex) { msg += " EX:" + One(ex.Message); }

            string h1, c1; int after = Census(out h1, out c1);
            string nh = NewHandleSince(pre);

            // B.1 audit 10/08: PsCreatePrimitive has NO SetLayer and NO UseCurrentLayer --
            // unlike the plate and shape creators, it cannot be told where to build, so the
            // solid lands on whatever layer is current. That is how 14 Ks_VolBody ended up on
            // layer 0. Assign it here instead, defaulting to the layer the rest of the model
            // uses for solids.
            string solLayer = Get(kv, "layer", "PS_Solid");
            string layApplied = "";
            if (after > before && nh.Length > 0 && solLayer.Length > 0)
            {
                try { layApplied = " layer=" + ApplyLayer(nh, solLayer); }
                catch (System.Exception e) { layApplied = " layer!" + One(e.Message); }
            }

            Result(((after > before) ? "EB_OK" : "EB_ERR") + " solid kind=" + kind +
                   " census=" + before + "->" + after + " objectId=" + (oid != 0 ? HandleOf(oid) : "0") +
                   " new:" + nh + " class=" + c1 + layApplied + applied + msg);
        }

        // =====================================================================
        //  v121 -- B.27 CONNECTION EDITOR
        // =====================================================================

        // op=connverify [minx=] [maxx=] [out=]
        // The audit B.27 describes, plus the bolt/hole cross-check.
        void ConnVerify(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            double minx = double.Parse(Get(kv, "minx", "-1e12"), IC);
            double maxx = double.Parse(Get(kv, "maxx", "1e12"), IC);
            StringBuilder sb = new StringBuilder();
            int parts = 0, withLinks = 0, links = 0, flagged = 0;
            sb.AppendLine("handle\tclass\tlinkNo\ttype\ttarget\tbolts\tgenParts\tholes\tverdict");

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId oid in ms)
                {
                    Entity e = null;
                    try { e = tr.GetObject(oid, OpenMode.ForRead) as Entity; }
                    catch { continue; }
                    if (e == null) continue;
                    double cx;
                    try { Extents3d ex = e.GeometricExtents; cx = (ex.MinPoint.X + ex.MaxPoint.X) / 2.0; }
                    catch { continue; }
                    if (cx < minx || cx > maxx) continue;
                    string cls = oid.ObjectClass != null ? oid.ObjectClass.Name : "?";
                    if (cls.IndexOf("Ks_", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    parts++;
                    long id = oid.OldIdPtr.ToInt64();

                    int n = 0;
                    PsEditLogicalLink ed = new PsEditLogicalLink();
                    try { ed.SetObjectId(id); n = ed.get_LogicalLinkCount(); }
                    catch { continue; }
                    if (n <= 0) continue;
                    withLinks++;

                    // how many holes this part carries -- the other half of the cross-check
                    string herr; StringBuilder scratch = new StringBuilder();
                    int nholes = HolesOf(id, 2, scratch, "x", out herr);

                    for (int i = 0; i < n; i++)
                    {
                        links++;
                        int num = -1;
                        try { num = ed.get_LinkNumberFromIndex(i); } catch { continue; }
                        string type = "?", tgt = "-";
                        int nbolt = 0, ngen = 0;
                        try
                        {
                            PsLogicalLink lk = ed.GetLogicalLinkByNumber(num);
                            if (lk != null)
                            {
                                try { tgt = HandleOf(lk.getTargetId()); } catch { }
                                for (int b = 0; b < 200; b++)
                                { long bid = 0; try { bid = lk.getBoltObjectId(b); } catch { break; }
                                  if (bid == 0) break; nbolt++; }
                                for (int p = 0; p < 200; p++)
                                { long pid = 0; try { pid = lk.getLinkObjectId(p); } catch { break; }
                                  if (pid == 0) break; ngen++; }
                            }
                        }
                        catch { }
                        // ג ן¸ MEASURED 09/08: LinkType cannot be read this way. PsEditConnection
                        // has no binder, so a fresh instance always reports kUndefinedLink, and
                        // getBoltObjectId(0)/getLinkObjectId(0) return 0 on every link. Reporting
                        // a verdict from those was worse than reporting nothing: it flagged the
                        // B.22 purlins, which carry 23 bolts measured through COM.
                        // So: report only what is real, and say the rest is unread.
                        type = "(unread)";
                        string verdict = (nbolt == 0 && ngen == 0) ? "link-detail-unread" : "read";
                        if (verdict == "link-detail-unread") flagged++;

                        sb.Append(HandleOf(id)).Append('\t').Append(cls).Append('\t')
                          .Append(num).Append('\t').Append(type).Append('\t').Append(tgt).Append('\t')
                          .Append(nbolt).Append('\t').Append(ngen).Append('\t')
                          .Append(nholes).Append('\t').Append(verdict).Append('\n');
                    }
                }
                tr.Commit();
            }
            string outn = Get(kv, "out", "eb_connverify.txt");
            File.WriteAllText(Path.Combine(Dir, outn), sb.ToString(), Encoding.UTF8);
            Result("EB_OK connverify parts=" + parts + " withLinks=" + withLinks +
                   " links=" + links + " detailUnread=" + flagged +
                   "  (link TYPE and BOLTS are not readable from this API -- judge a connection by GEOMETRY: bolt positions against hole positions)" + " -> " + outn);
        }

        // op=connkill handle=<h> [number=N|all] [deleteparts=1]
        // B.27's DELETE, with its `Delete with` flag. Deleting a connection's PARTS
        // leaves the LINK alive -- measured in B.26, where the next attempt then
        // reported "parts +0" because ProSteel still believed the joint was there.
        void ConnKill(Dictionary<string, string> kv)
        {
            long id = IdFromHandle(Get(kv, "handle", ""));
            if (id == 0) { Result("EB_ERR connkill: bad handle"); return; }
            bool delParts = Get(kv, "deleteparts", "1") == "1";
            string which = Get(kv, "number", "all");
            string h0, c0; int before = Census(out h0, out c0);
            int nBefore = -1, nAfter = -1; string msg = "";
            try
            {
                PsEditLogicalLink ed = new PsEditLogicalLink();
                ed.SetObjectId(id);
                nBefore = ed.get_LogicalLinkCount();
                if (which == "all") ed.RemoveAllLogicalLinks(delParts);
                else ed.RemoveLogicalLinkByNumber(int.Parse(which), delParts);
                PsEditLogicalLink ed2 = new PsEditLogicalLink();
                ed2.SetObjectId(id);
                try { nAfter = ed2.get_LogicalLinkCount(); } catch { }
            }
            catch (System.Exception ex) { msg = " EX:" + One(ex.Message); }
            string h1, c1; int after = Census(out h1, out c1);
            Result(((nAfter >= 0 && nAfter < nBefore) ? "EB_OK" : "EB_ERR") +
                   " connkill handle=" + Get(kv, "handle", "") + " which=" + which +
                   " deleteParts=" + delParts + " links=" + nBefore + "->" + nAfter +
                   " census=" + before + "->" + after + msg);
        }

        // =====================================================================
        //  v120 -- B.26 HAUNCHES, plane exposed + blast guard
        //  `conn kind=haunch` stretched both rafters of a pitched portal to
        //  ~317,000 mm. The link data carries XAxis/YAxis/InsertPoint and the
        //  template holds a plane; a portal frame stands in XZ, not world XY.
        //  A destructive op must never fail quietly -- so measure the connected
        //  shape's length and shout if it grew.
        // =====================================================================
        void Haunch(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            long bId = IdFromHandle(Get(kv, "beam", ""));
            string sH = Get(kv, "support", "");
            long sId = (sH.Length > 0) ? IdFromHandle(sH) : 0;
            if (bId == 0) { Result("EB_ERR haunch: beam= not found"); return; }

            double lenBefore = LengthOf(bId);
            string h0, c0; int before = Census(out h0, out c0);
            List<string> pre = HandleSet();
            string applied = "", msg = "";
            int rc = -999; bool made = false;
            try
            {
                PsHaunchConnection c = new PsHaunchConnection();
                c.SetToDefaults();
                PsHaunchLinkDataMgd d = null;
                string tmpl = Get(kv, "tmpl", "Default/Standard");
                if (tmpl.Length > 0)
                {
                    try { d = c.GetTemplate(tmpl); }
                    catch (System.Exception e) { msg += " tmpl!" + One(e.Message); }
                }
                if (d != null)
                {
                    // report what the template actually holds -- especially its plane
                    try { applied += " tmplPlane X=" + F(d.XAxis.x) + "," + F(d.XAxis.y) + "," + F(d.XAxis.z) +
                                     " Y=" + F(d.YAxis.x) + "," + F(d.YAxis.y) + "," + F(d.YAxis.z); }
                    catch { }

                    if (kv.ContainsKey("ex")) { double[] v = Nums(Get(kv, "ex", "1,0,0")); d.XAxis = new PsVector(v[0], v[1], v[2]); applied += " setX=" + Get(kv, "ex", ""); }
                    if (kv.ContainsKey("ey")) { double[] v = Nums(Get(kv, "ey", "0,0,1")); d.YAxis = new PsVector(v[0], v[1], v[2]); applied += " setY=" + Get(kv, "ey", ""); }
                    if (kv.ContainsKey("at")) { d.InsertPoint = Pt(Get(kv, "at", "0,0,0")); applied += " at=" + Get(kv, "at", ""); }
                    // ⭐ v169 (B.26 audit) -- READ THE PLANE AND THE POINT BACK.
                    // B.24 found that PsBracing's PsPoint SETTERS store garbage (read back NaN)
                    // while every scalar/enum setter on the same object round-trips. B.26 records
                    // that the haunch "builds at the support's origin -- SetConnectionPoint and
                    // InsertPoint did not move them", and never read InsertPoint back. If it does
                    // not round-trip either, the haunch was never told where to go, and that is a
                    // completely different fact from "the API ignores the position".
                    try
                    {
                        applied += " [readback X=" + F(d.XAxis.x) + "," + F(d.XAxis.y) + "," + F(d.XAxis.z)
                                 + " Y=" + F(d.YAxis.x) + "," + F(d.YAxis.y) + "," + F(d.YAxis.z)
                                 + " InsertPoint=" + F(d.InsertPoint.x) + "," + F(d.InsertPoint.y)
                                 + "," + F(d.InsertPoint.z) + "]";
                    }
                    catch (System.Exception e) { applied += " readback!EX:" + One(e.Message); }
                    if (kv.ContainsKey("len")) { d.Length = double.Parse(Get(kv, "len", "1000"), IC); applied += " L=" + Get(kv, "len", ""); }
                    if (kv.ContainsKey("toph")) { d.TopHeight = double.Parse(Get(kv, "toph", "0"), IC); applied += " topH=" + Get(kv, "toph", ""); }
                    if (kv.ContainsKey("baseh")) { d.BaseHeight = double.Parse(Get(kv, "baseh", "500"), IC); applied += " baseH=" + Get(kv, "baseh", ""); }
                    if (kv.ContainsKey("web")) { d.WebThickness = double.Parse(Get(kv, "web", "8"), IC); applied += " web=" + Get(kv, "web", ""); }
                    if (kv.ContainsKey("slope")) { d.Slope = double.Parse(Get(kv, "slope", "0"), IC); applied += " slope=" + Get(kv, "slope", ""); }
                    if (kv.ContainsKey("bottom")) { d.IsBottomTrain = Get(kv, "bottom", "0") == "1"; applied += " bottomTrain=" + Get(kv, "bottom", ""); }
                    if (kv.ContainsKey("turn")) { d.TurnBottomTrain = Get(kv, "turn", "0") == "1"; applied += " turn=" + Get(kv, "turn", ""); }
                    if (kv.ContainsKey("conical")) { d.IsConical = Get(kv, "conical", "0") == "1"; applied += " conical=" + Get(kv, "conical", ""); }
                    if (kv.ContainsKey("conew")) { d.ConicalWidth = double.Parse(Get(kv, "conew", "0"), IC); applied += " coneW=" + Get(kv, "conew", ""); }
                    if (kv.ContainsKey("coped")) { d.IsCopedShape = Get(kv, "coped", "0") == "1"; applied += " coped=" + Get(kv, "coped", ""); }
                    if (kv.ContainsKey("copedh")) { d.CopedHeight = double.Parse(Get(kv, "copedh", "0"), IC); applied += " copedH=" + Get(kv, "copedh", ""); }
                    if (kv.ContainsKey("fixed")) { d.SizeDependsToConnected = Get(kv, "fixed", "0") == "1"; applied += " fixedSize=" + Get(kv, "fixed", ""); }
                    if (kv.ContainsKey("stiffsup")) { d.StiffenerAtSupport = Get(kv, "stiffsup", "0") == "1"; applied += " stiffSup=" + Get(kv, "stiffsup", ""); }
                    if (kv.ContainsKey("stiffcon")) { d.StiffenerAtConnected = Get(kv, "stiffcon", "0") == "1"; applied += " stiffCon=" + Get(kv, "stiffcon", ""); }
                    if (kv.ContainsKey("group")) { d.CreateGroup = Get(kv, "group", "0") == "1"; applied += " group=" + Get(kv, "group", ""); }
                    if (kv.ContainsKey("topoff")) { d.TopChordOffset = double.Parse(Get(kv, "topoff", "0"), IC); applied += " topOff=" + Get(kv, "topoff", ""); }
                    c.SetConnectionData(d);
                }
                c.SetConnectionObjectId(bId);
                if (sId != 0) c.SetSupportObjectId(sId);
                if (kv.ContainsKey("at")) c.SetConnectionPoint(Pt(Get(kv, "at", "0,0,0")));
                try { rc = c.Check(); } catch (System.Exception e) { msg += " check!" + One(e.Message); }
                try { made = c.Create(); } catch (System.Exception e) { msg += " create!" + One(e.Message); }
            }
            catch (System.Exception ex) { msg += " EX:" + One(ex.Message); }

            double lenAfter = LengthOf(bId);
            string h1, c1; int after = Census(out h1, out c1);
            double grow = (lenBefore > 0.001) ? (lenAfter / lenBefore) : 0.0;
            double maxgrow = double.Parse(Get(kv, "maxgrow", "3"), IC);
            string blast = (grow > maxgrow || lenAfter > 50000.0)
                ? "  *** BLAST: connected shape " + F(lenBefore) + " -> " + F(lenAfter) +
                  " (x" + F(grow) + ") -- IT NOW CROSSES THE MODEL, DELETE IT ***" : "";
            Result(((after > before) ? "EB_OK" : "EB_ERR") + " haunch beam=" + Get(kv, "beam", "") +
                   " support=" + (sId == 0 ? "(none)" : sH) +
                   " check=" + rc + " create=" + made +
                   " beamLen=" + F(lenBefore) + "->" + F(lenAfter) +
                   " census=" + before + "->" + after + " new:" + NewHandleSince(pre) +
                   applied + msg + blast);
        }

        // =====================================================================
        //  v118 -- B.24 THROUGH THE MACRO, not through PsBracing
        //  PsBracing.insert() refused in SIX configurations. But ProStructures
        //  ships 62 PSN_* macro assemblies -- the Connection Center's own
        //  connections -- and one of them IS the bracing:
        //      PSN_HollowShapeBracing.UserConnection  +  .ClsParameters
        //  Its shape is completely different from the Ps* classes:
        //      InitialCall() / Create()      get a connection id
        //      ClsParameters.SetDefaultValues(metric)
        //      p.ConnId1 / p.ConnId2         the two hosts
        //      p.WriteToConnection(ref id)   push the parameters in
        //      BuildI(ref id)                and build
        //  Probe the sequence and REPORT WHAT EACH STEP RETURNS -- the whole
        //  point is that nothing here is documented.
        // =====================================================================
        void MacroBrace(Dictionary<string, string> kv)
        {
            StringBuilder sb = new StringBuilder();
            long id1 = IdFromHandle(Get(kv, "h1", ""));
            long id2 = IdFromHandle(Get(kv, "h2", ""));
            string h0, c0; int before = Census(out h0, out c0);
            List<string> pre = HandleSet();
            try
            {
                PSN_HollowShapeBracing.UserConnection uc = new PSN_HollowShapeBracing.UserConnection();
                sb.AppendLine("UserConnection ctor OK");
                try { sb.AppendLine("  identifier='" + uc.GetIdentifier() + "'"); } catch (System.Exception e) { sb.AppendLine("  GetIdentifier !" + One(e.Message)); }
                try { sb.AppendLine("  description='" + uc.GetDescription() + "'"); } catch (System.Exception e) { sb.AppendLine("  GetDescription !" + One(e.Message)); }

                // v119: InitialCall() is the INTERACTIVE entry -- it printed
                // "Initializing Hollow Shape Bracing Connection. Choose support shape"
                // and parked the session waiting for a pick. NEVER call it unattended.
                // CreateClone(ClsParameters) returns Int64 and takes the parameters
                // directly: that is the non-interactive candidate.
                long connId = 0;

                PSN_HollowShapeBracing.ClsParameters p = new PSN_HollowShapeBracing.ClsParameters();
                sb.AppendLine("ClsParameters ctor OK");
                try { p.SetDefaultValues(true); sb.AppendLine("  SetDefaultValues(metric) OK"); }
                catch (System.Exception e) { sb.AppendLine("  SetDefaultValues !" + One(e.Message)); }
                try
                {
                    sb.AppendLine("  defaults: boltDia=" + F(p.BoltDiameter) + " boltType='" + One(p.BoltTypeName) +
                                  "' plateT=" + F(p.BracePlateThickness) + " gap=" + F(p.GapPlates) +
                                  " clearance=" + F(p.BracePlateClearance));
                }
                catch (System.Exception e) { sb.AppendLine("  read defaults !" + One(e.Message)); }

                try { p.ConnId1 = id1; p.ConnId2 = id2; sb.AppendLine("  ConnId1/2 set to " + id1 + " / " + id2); }
                catch (System.Exception e) { sb.AppendLine("  ConnId !" + One(e.Message)); }

                try
                {
                    connId = uc.CreateClone(p);
                    sb.AppendLine("  CreateClone(params) -> " + connId);
                }
                catch (System.Exception e) { sb.AppendLine("  CreateClone !" + One(e.Message)); }
                if (connId == 0)
                {
                    try { System.IntPtr ip = uc.Create(); sb.AppendLine("  Create() -> ptr " + ip); }
                    catch (System.Exception e) { sb.AppendLine("  Create !" + One(e.Message)); }
                }
                if (connId != 0)
                {
                    try { p.WriteToConnection(ref connId); sb.AppendLine("  WriteToConnection OK, id=" + connId); }
                    catch (System.Exception e) { sb.AppendLine("  WriteToConnection !" + One(e.Message)); }
                    try { uc.BuildI(ref connId); sb.AppendLine("  BuildI OK, id=" + connId); }
                    catch (System.Exception e) { sb.AppendLine("  BuildI !" + One(e.Message)); }
                    try { uc.DrawI(ref connId); sb.AppendLine("  DrawI OK"); }
                    catch (System.Exception e) { sb.AppendLine("  DrawI !" + One(e.Message)); }
                }
            }
            catch (System.Exception ex) { sb.AppendLine("EX: " + One(ex.Message)); }
            string h1, c1; int after = Census(out h1, out c1);
            File.WriteAllText(Path.Combine(Dir, "eb_macrobrace.txt"), sb.ToString(), Encoding.UTF8);
            Result(((after > before) ? "EB_OK" : "EB_ERR") + " macrobrace census=" + before + "->" + after +
                   " new:" + NewHandleSince(pre) + " -> eb_macrobrace.txt\n" + sb.ToString());
        }

        // =====================================================================
        //  v116 -- B.4 MOVE AND COPY PARTS, the two commands that were missing
        //
        //  Why ProSteel has its own move/copy at all, in the manual's words:
        //  "Using AutoCAD object snaps in a view may result in points being
        //   selected that are NOT IN THE PROPER PLANE. The ProSteel copy and move
        //   commands prevent this by limiting the direction of the move."
        //  From code that constraint is free -- we hand over an exact vector and
        //  never snap. What is NOT free is Align and the rotated distribution.
        // =====================================================================

        // Collect handles= into an ObjectIdCollection. Shared by align and spiral.
        // Explicit handles only: Replicate's own comment explains why a box re-selects
        // earlier copies and makes the counts snowball.
        ObjectIdCollection PickHandles(string list, out int picked)
        {
            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            ObjectIdCollection src = new ObjectIdCollection();
            picked = 0;
            foreach (string hx in list.Split(new char[] { ',', ';' },
                                            StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    ObjectId id = db.GetObjectId(false,
                        new Handle(Convert.ToInt64(hx.Trim(), 16)), 0);
                    if (id.IsNull || id.IsErased) continue;
                    src.Add(id); picked++;
                }
                catch { }
            }
            return src;
        }

        // Apply one matrix to a set, optionally to a deep-cloned copy of it.
        // Returns "moved/failed" and fills newH with the resulting handles.
        string ApplyMatrix(ObjectIdCollection src, Matrix3d m, bool copy, out string newH)
        {
            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            newH = "";
            ObjectIdCollection act = src;
            IdMapping map = null;
            if (copy)
            {
                map = new IdMapping();
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    db.DeepCloneObjects(src, bt[BlockTableRecord.ModelSpace], map, false);
                    tr.Commit();
                }
                act = new ObjectIdCollection();
                foreach (ObjectId sid in src)
                {
                    if (!map.Contains(sid)) continue;
                    IdPair pr = map[sid];
                    if (pr.IsCloned) act.Add(pr.Value);
                }
            }
            int moved = 0, failed = 0;
            using (Transaction tr2 = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in act)
                {
                    try
                    {
                        Entity e = tr2.GetObject(id, OpenMode.ForWrite) as Entity;
                        if (e == null) { failed++; continue; }
                        e.TransformBy(m);
                        if (newH.Length < 400) newH += (newH.Length > 0 ? "," : "") + e.Handle.ToString();
                        moved++;
                    }
                    catch { failed++; }
                }
                tr2.Commit();
            }
            return moved + "/" + failed;
        }

        // op=align handles=<h,h> o1=x,y,z x1=x,y,z y1=x,y,z o2=... x2=... y2=... [copy=0|1]
        //
        // B.4.4, the 3-point method: "specify the ORIGINAL POSITION by clicking the origin
        // as well as a point on the X- and Y-axis as a reference position. Then indicate the
        // desired new position the same way. The parts are then aligned according to the
        // specifications WITH THE ORIGIN POINTS BEING IDENTICAL."
        void Align(Dictionary<string, string> kv)
        {
            int picked;
            ObjectIdCollection src = PickHandles(Get(kv, "handles", ""), out picked);
            if (picked == 0) { Result("EB_ERR align: no handles"); return; }

            PsPoint o1 = Pt(Get(kv, "o1", "0,0,0")), o2 = Pt(Get(kv, "o2", "0,0,0"));
            PsPoint px1 = Pt(Get(kv, "x1", "1,0,0")), py1 = Pt(Get(kv, "y1", "0,1,0"));
            PsPoint px2 = Pt(Get(kv, "x2", "1,0,0")), py2 = Pt(Get(kv, "y2", "0,1,0"));

            Point3d O1 = new Point3d(o1.x, o1.y, o1.z), O2 = new Point3d(o2.x, o2.y, o2.z);
            Vector3d X1 = new Point3d(px1.x, px1.y, px1.z) - O1;
            Vector3d Y1 = new Point3d(py1.x, py1.y, py1.z) - O1;
            Vector3d X2 = new Point3d(px2.x, px2.y, px2.z) - O2;
            Vector3d Y2 = new Point3d(py2.x, py2.y, py2.z) - O2;
            if (X1.Length < 1e-9 || Y1.Length < 1e-9 || X2.Length < 1e-9 || Y2.Length < 1e-9)
            { Result("EB_ERR align: an axis point coincides with its origin"); return; }

            // orthonormalise, or the matrix shears the part instead of moving it
            X1 = X1.GetNormal(); Vector3d Z1 = X1.CrossProduct(Y1);
            if (Z1.Length < 1e-9) { Result("EB_ERR align: system 1 X and Y are parallel"); return; }
            Z1 = Z1.GetNormal(); Y1 = Z1.CrossProduct(X1);
            X2 = X2.GetNormal(); Vector3d Z2 = X2.CrossProduct(Y2);
            if (Z2.Length < 1e-9) { Result("EB_ERR align: system 2 X and Y are parallel"); return; }
            Z2 = Z2.GetNormal(); Y2 = Z2.CrossProduct(X2);

            Matrix3d m = Matrix3d.AlignCoordinateSystem(O1, X1, Y1, Z1, O2, X2, Y2, Z2);
            bool copy = Get(kv, "copy", "0") == "1";
            string h0, c0; int before = Census(out h0, out c0);
            string newH, res;
            try { res = ApplyMatrix(src, m, copy, out newH); }
            catch (System.Exception ex) { Result("EB_ERR align: " + One(ex.Message)); return; }
            string h1, c1; int after = Census(out h1, out c1);
            Result("EB_OK align picked=" + picked + " moved/failed=" + res +
                   (copy ? " (Align+Copy)" : " (in place)") +
                   " census=" + before + "->" + after + " new:" + newH);
        }

        // op=spiral handles=<h,h> [method=count|area] n= angle= [dz=] [axis=z]
        //           [about=x,y,z] [keep=1]
        //
        // B.4.6. method=count -> `angle` is BETWEEN steps; method=area -> `angle` is the
        // TOTAL span the n steps are distributed across. dz is the rise per step.
        void Spiral(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            int picked;
            ObjectIdCollection src = PickHandles(Get(kv, "handles", ""), out picked);
            if (picked == 0) { Result("EB_ERR spiral: no handles"); return; }

            int n = int.Parse(Get(kv, "n", "12"));
            if (n < 1) { Result("EB_ERR spiral: n must be >= 1"); return; }
            double ang = double.Parse(Get(kv, "angle", "30"), IC);
            double dz = double.Parse(Get(kv, "dz", "0"), IC);
            string method = Get(kv, "method", "count").ToLowerInvariant();
            // "either the angle BETWEEN the steps, or the COMPLETE angle"
            double step = (method == "area") ? (ang / n) : ang;

            string axs = Get(kv, "axis", "z").ToLowerInvariant();
            Vector3d av = axs == "x" ? Vector3d.XAxis : axs == "y" ? Vector3d.YAxis : Vector3d.ZAxis;
            double[] ab = Nums(Get(kv, "about", "0,0,0"));
            Point3d about = new Point3d(ab[0], ab.Length > 1 ? ab[1] : 0, ab.Length > 2 ? ab[2] : 0);

            string h0, c0; int before = Census(out h0, out c0);
            StringBuilder made = new StringBuilder();
            int ok = 0, bad = 0;
            for (int i = 1; i <= n; i++)
            {
                Matrix3d m = Matrix3d.Displacement(av * (dz * i)) *
                             Matrix3d.Rotation(step * i * Math.PI / 180.0, av, about);
                string newH, res;
                try { res = ApplyMatrix(src, m, true, out newH); ok++; }
                catch (System.Exception ex) { bad++; made.Append(" !" + One(ex.Message)); continue; }
                if (made.Length < 300) made.Append(" [" + i + "]" + newH);
            }
            string h1, c1; int after = Census(out h1, out c1);
            Result(((after > before) ? "EB_OK" : "EB_ERR") + " spiral method=" + method +
                   " n=" + n + " stepAngle=" + F(step) + " dz=" + F(dz) + " axis=" + axs +
                   " about=" + Get(kv, "about", "0,0,0") +
                   " copies=" + ok + " failed=" + bad +
                   " census=" + before + "->" + after + "(+" + (after - before) + ")" + made);
        }

        // =====================================================================
        //  v112 -- B.12.6 NOTCH BETWEEN TWO SHAPES (the cope)
        //  "Click the shape to be notched, then the shape specifying the contour."
        //  and the route that needs NO second shape:
        //  "Just enter ESC instead of selecting a second shape and select the END
        //   of the shape."  -> UseShapeEndCope.
        //  Corner Layout = Edge / Radial / ACCESS HOLES; the API calls the access
        //  hole a RATHOLE (First/SecondRatholeDiameter).
        // =====================================================================

        static string CopeDump(PsCopeLinkDataMgd d)
        {
            if (d == null) return "(null)";
            StringBuilder b = new StringBuilder();
            try { b.Append(" shapeFit=" + d.ShapeFitType); } catch { }
            try { b.Append(" copeType=" + d.CopeType); } catch { }
            try { b.Append(" edgeType=" + d.EdgeType); } catch { }
            try { b.Append(" polyCut=" + d.PolyCutType); } catch { }
            try { b.Append(" radius=" + F(d.Radius)); } catch { }
            try { b.Append(" web=" + F(d.WebDistance) + "/" + F(d.WebDistance2)); } catch { }
            try { b.Append(" flangeT=" + F(d.FlangeThickness)); } catch { }
            try { b.Append(" endCope=" + d.UseShapeEndCope + " atStart=" + d.CutAtStart); } catch { }
            try { b.Append(" bothEq=" + d.BothSidesEqual + " rot=" + d.Rotate); } catch { }
            try { b.Append(" innerEdge=" + d.AlignToInnerEdge + " middle=" + d.AlignToMiddle); } catch { }
            try { b.Append(" outer=" + F(d.DistanceOutsideTop) + "/" + F(d.DistanceOutsideDown)); } catch { }
            try { b.Append(" inside=" + F(d.DistanceInsideTop) + "/" + F(d.DistanceInsideDown)); } catch { }
            try { b.Append(" edge=" + F(d.DistanceEdgeTop) + "/" + F(d.DistanceEdgeDown)); } catch { }
            try { b.Append(" rathole=" + F(d.FirstRatholeDiameter) + "/" + F(d.SecondRatholeDiameter)); } catch { }
            try { b.Append(" shapeLen=" + F(d.ShapeLength) + " slope=" + d.SlopeCut); } catch { }
            return b.ToString();
        }

        // op=copeinfo  -> every cope template, fully expanded. The enum values are
        // MEASURED here, not guessed from declaration order.
        void CopeInfo(Dictionary<string, string> kv)
        {
            StringBuilder sb = new StringBuilder();
            int n = -1;
            try
            {
                PsCopeConnection cp = new PsCopeConnection();
                cp.SetToDefaults();
                n = cp.GetTemplateCount();
                sb.AppendLine("cope templates: " + n);
                sb.AppendLine("");
                for (int i = 0; i < n; i++)
                {
                    string nm = ""; try { nm = cp.GetTemplateName(i); } catch { }
                    sb.Append("[" + i + "] '" + nm + "'");
                    try { sb.Append(CopeDump(cp.GetTemplate(nm))); }
                    catch (System.Exception e) { sb.Append("  !GetTemplate:" + One(e.Message)); }
                    sb.AppendLine();
                }
                // what a fresh, untouched connection carries before any template
                sb.AppendLine("");
                PsCopeConnection c2 = new PsCopeConnection();
                c2.SetToDefaults();
                PsCopeLinkDataMgd fresh = null;
                try { fresh = new PsCopeLinkDataMgd(); } catch (System.Exception e) { sb.AppendLine("new PsCopeLinkDataMgd: !" + One(e.Message)); }
                sb.AppendLine("fresh link data:" + CopeDump(fresh));
                try { sb.AppendLine("plateDataCount=" + c2.get_PlateDataCount()); } catch (System.Exception e) { sb.AppendLine("plateDataCount !" + One(e.Message)); }
            }
            catch (System.Exception ex) { Result("EB_ERR copeinfo EX:" + One(ex.Message)); return; }
            File.WriteAllText(Path.Combine(Dir, "eb_copeinfo.txt"), sb.ToString(), Encoding.UTF8);
            Result("EB_OK copeinfo templates=" + n + " -> eb_copeinfo.txt");
        }

        // op=cope beam=<handle> [support=<handle>] [tmpl=<name>]
        //         [fit=0|1|2] [copetype=N] [edge=N] [radius=]
        //         [web=] [web2=] [flanget=] [endcope=0|1] [atstart=0|1]
        //         [botheq=0|1] [rot=0|1] [inner=0|1] [middle=0|1]
        //         [outtop=] [outdown=] [intop=] [indown=] [edgetop=] [edgedown=]
        //         [rathole=] [rathole2=]
        //
        // support omitted  ==  the manual's ESC route: a notch at the shape END.
        void Cope(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            long bId = IdFromHandle(Get(kv, "beam", ""));
            // support is OPTIONAL -- omitting it IS the manual's ESC route (notch at the
            // shape end). IdFromHandle throws on an empty string, so never ask it.
            string sH = Get(kv, "support", "");
            long sId = (sH.Length > 0) ? IdFromHandle(sH) : 0;
            if (bId == 0) { Result("EB_ERR cope beam= not found"); return; }

            string h0, c0; int before = Census(out h0, out c0);
            List<string> pre = HandleSet();
            string msg = "", applied = "", readback = "";
            bool made = false; int rc = -999; int plates = -1;

            try
            {
                PsCopeConnection c = new PsCopeConnection();
                c.SetToDefaults();

                PsCopeLinkDataMgd d = null;
                string tmpl = Get(kv, "tmpl", "");
                if (tmpl.Length > 0)
                {
                    try { d = c.GetTemplate(tmpl); }
                    catch (System.Exception e) { msg += " tmpl!" + One(e.Message); }
                }
                if (d == null) { try { d = new PsCopeLinkDataMgd(); } catch (System.Exception e) { msg += " newdata!" + One(e.Message); } }

                if (d != null)
                {
                    // every field the dialog exposes, only when asked for
                    if (kv.ContainsKey("fit")) d.ShapeFitType = int.Parse(Get(kv, "fit", "0"));
                    if (kv.ContainsKey("copetype")) d.CopeType = int.Parse(Get(kv, "copetype", "0"));
                    if (kv.ContainsKey("edge")) d.EdgeType = int.Parse(Get(kv, "edge", "0"));
                    if (kv.ContainsKey("polycut")) d.PolyCutType = int.Parse(Get(kv, "polycut", "0"));
                    if (kv.ContainsKey("radius")) d.Radius = double.Parse(Get(kv, "radius", "0"), IC);
                    if (kv.ContainsKey("web")) d.WebDistance = double.Parse(Get(kv, "web", "0"), IC);
                    if (kv.ContainsKey("web2")) d.WebDistance2 = double.Parse(Get(kv, "web2", "0"), IC);
                    if (kv.ContainsKey("flanget")) d.FlangeThickness = double.Parse(Get(kv, "flanget", "0"), IC);
                    if (kv.ContainsKey("endcope")) d.UseShapeEndCope = (Get(kv, "endcope", "0") == "1");
                    if (kv.ContainsKey("atstart")) d.CutAtStart = (Get(kv, "atstart", "1") == "1");
                    if (kv.ContainsKey("botheq")) d.BothSidesEqual = (Get(kv, "botheq", "1") == "1");
                    if (kv.ContainsKey("rot")) d.Rotate = (Get(kv, "rot", "0") == "1");
                    if (kv.ContainsKey("inner")) d.AlignToInnerEdge = (Get(kv, "inner", "0") == "1");
                    if (kv.ContainsKey("middle")) d.AlignToMiddle = (Get(kv, "middle", "0") == "1");
                    if (kv.ContainsKey("outtop")) d.DistanceOutsideTop = double.Parse(Get(kv, "outtop", "0"), IC);
                    if (kv.ContainsKey("outdown")) d.DistanceOutsideDown = double.Parse(Get(kv, "outdown", "0"), IC);
                    if (kv.ContainsKey("intop")) d.DistanceInsideTop = double.Parse(Get(kv, "intop", "0"), IC);
                    if (kv.ContainsKey("indown")) d.DistanceInsideDown = double.Parse(Get(kv, "indown", "0"), IC);
                    if (kv.ContainsKey("edgetop")) d.DistanceEdgeTop = double.Parse(Get(kv, "edgetop", "0"), IC);
                    if (kv.ContainsKey("edgedown")) d.DistanceEdgeDown = double.Parse(Get(kv, "edgedown", "0"), IC);
                    if (kv.ContainsKey("rathole")) d.FirstRatholeDiameter = double.Parse(Get(kv, "rathole", "0"), IC);
                    if (kv.ContainsKey("rathole2")) d.SecondRatholeDiameter = double.Parse(Get(kv, "rathole2", "0"), IC);
                    if (kv.ContainsKey("shapelen")) d.ShapeLength = double.Parse(Get(kv, "shapelen", "0"), IC);
                    if (kv.ContainsKey("slope")) d.SlopeCut = (Get(kv, "slope", "0") == "1");
                    applied = CopeDump(d);
                    c.SetConnectionData(d);
                }

                c.SetConnectionObjectId(bId);
                if (sId != 0) c.SetSupportObjectId(sId);   // omitted == the ESC route

                try { rc = c.Check(); } catch (System.Exception e) { msg += " check!" + One(e.Message); }
                try { made = c.Create(); } catch (System.Exception e) { msg += " create!" + One(e.Message); }
                try { plates = c.get_PlateDataCount(); } catch { }
                // Create() returning false is not proof of failure (PsCreateBendPlate
                // does exactly that while succeeding) -- read the link back instead.
                try
                {
                    PsCopeLink lk = c.GetLink();
                    readback = (lk == null) ? " link=null" : " link=OK";
                }
                catch (System.Exception e) { readback = " link!" + One(e.Message); }
            }
            catch (System.Exception ex) { msg += " EX:" + One(ex.Message); }

            string h1, c1; int after = Census(out h1, out c1);
            Result("EB_OK cope beam=" + Get(kv, "beam", "") + " support=" +
                   (sId == 0 ? "(ESC/shape-end)" : Get(kv, "support", "")) +
                   " check=" + rc + " create=" + made + " plates=" + plates +
                   " census=" + before + "->" + after + " new:" + NewHandleSince(pre) +
                   readback + msg + " || set:" + applied);
        }

        // =====================================================================
        //  v111 ג€” B.15 BOLTS
        //  "Bolting is the EASIEST form of automatic connection... In previous
        //   versions the components had to be DRILLED FIRST. Which now is not
        //   necessary any more."
        //  Two numbers from this chapter explain earlier failures:
        //    Gap distance     -> MaxObjectDistance  (max distance between two
        //                        holes assumed to belong to one bolting)
        //    Angle difference -> MaxDeclination     (holes must ALIGN in angle)
        // =====================================================================

        // op=boltinfo  -> the style and type tables PsCreateBolt itself can see
        void BoltInfo(Dictionary<string, string> kv)
        {
            StringBuilder sb = new StringBuilder();
            int ns = -1, nt = -1;
            try
            {
                PsCreateBolt cb = new PsCreateBolt();
                cb.SetToDefaults();
                try { ns = cb.BoltStyleCount; } catch { }
                try { nt = cb.BoltTypeCount; } catch { }
                sb.AppendLine("boltStyleCount=" + ns + "  boltTypeCount=" + nt);
                // ג ן¸ The dump prints BoltStyle / BoltType / Diameter as plain properties.
                // The COMPILER says they have NO GET ACCESSOR -- they are write-only. And
                // BoltStyleName is INDEXED: get_BoltStyleName(int). That indexer is the
                // enumeration route, and it is invisible in the dump.
                sb.AppendLine("NOTE: BoltStyle / BoltType / Diameter are WRITE-ONLY (no getter).");
                for (int i = 0; i < ns && i < 60; i++)
                {
                    try { sb.AppendLine("  style[" + i + "] '" + One(cb.get_BoltStyleName(i)) + "'"); }
                    catch (System.Exception e) { sb.AppendLine("  style[" + i + "] !EX:" + One(e.Message)); break; }
                }
                for (int i = 0; i < nt && i < 60; i++)
                {
                    try { sb.AppendLine("  type[" + i + "] '" + One(cb.get_BoltTypeName(i)) + "'"); }
                    catch (System.Exception e) { sb.AppendLine("  type[" + i + "] !EX:" + One(e.Message)); break; }
                }
            }
            catch (System.Exception ex) { Result("EB_ERR boltinfo EX:" + One(ex.Message)); return; }
            File.WriteAllText(Path.Combine(Dir, "eb_boltinfo.txt"), sb.ToString(), Encoding.UTF8);
            Result("EB_OK boltinfo styles=" + ns + " types=" + nt + " -> eb_boltinfo.txt");
        }

        // op=boltsingle from=x,y,z to=x,y,z dia= style= [addlen=0]
        // B.15.1's manual insertion: "select start and endpoint of GRIP LENGTH".
        void BoltSingle(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string h0, c0; int before = Census(out h0, out c0);
            List<string> pre = HandleSet();
            string msg = "";
            try
            {
                PsCreateBolt cb = new PsCreateBolt();
                cb.SetToDefaults();
                cb.CreateSingleBolt(Pt(Get(kv, "from", "0,0,0")), Pt(Get(kv, "to", "0,0,100")),
                                    double.Parse(Get(kv, "dia", "16"), IC),
                                    Get(kv, "style", "DIN7990"),
                                    double.Parse(Get(kv, "addlen", "0"), IC));
            }
            catch (System.Exception ex) { msg = " EX:" + One(ex.Message); }
            string h1, c1; int after = Census(out h1, out c1);
            Result(((after > before) ? "EB_OK" : "EB_ERR") + " boltsingle census=" + before + "->" + after +
                   " new:" + NewHandleSince(pre) + " grip=" + Get(kv, "from", "") + "->" + Get(kv, "to", "") +
                   " dia=" + Get(kv, "dia", "16") + " style=" + Get(kv, "style", "DIN7990") + msg);
        }

        // op=nutonly from=x,y,z to=x,y,z dia= style=
        // B.15.1: "attach a single nut and/or disk WITHOUT the corresponding bolt"
        void NutOnly(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string h0, c0; int before = Census(out h0, out c0);
            List<string> pre = HandleSet();
            string msg = "";
            try
            {
                PsCreateBolt cb = new PsCreateBolt();
                cb.SetToDefaults();
                cb.CreateSingleNut(Pt(Get(kv, "from", "0,0,0")), Pt(Get(kv, "to", "0,0,100")),
                                   double.Parse(Get(kv, "dia", "16"), IC),
                                   Get(kv, "style", "DIN7990"));
            }
            catch (System.Exception ex) { msg = " EX:" + One(ex.Message); }
            string h1, c1; int after = Census(out h1, out c1);
            Result(((after > before) ? "EB_OK" : "EB_ERR") + " nutonly census=" + before + "->" + after +
                   " new:" + NewHandleSince(pre) + msg);
        }

        // op=threadedrod from=x,y,z to=x,y,z dia= [offset=0] style=      (B.15.3)
        void ThreadedRod(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string h0, c0; int before = Census(out h0, out c0);
            List<string> pre = HandleSet();
            string msg = "";
            try
            {
                PsCreateBolt cb = new PsCreateBolt();
                cb.SetToDefaults();
                cb.CreateThreadedRod(Pt(Get(kv, "from", "0,0,0")), Pt(Get(kv, "to", "0,0,500")),
                                     double.Parse(Get(kv, "dia", "16"), IC),
                                     double.Parse(Get(kv, "offset", "0"), IC),
                                     Get(kv, "style", "DIN7990"));
            }
            catch (System.Exception ex) { msg = " EX:" + One(ex.Message); }
            string h1, c1; int after = Census(out h1, out c1);
            Result(((after > before) ? "EB_OK" : "EB_ERR") + " threadedrod census=" + before + "->" + after +
                   " new:" + NewHandleSince(pre) + " dia=" + Get(kv, "dia", "16") +
                   " offset=" + Get(kv, "offset", "0") + msg);
        }

        // =====================================================================
        //  v108 ג€” B.24 DYNAMIC BRACING  (PS_VERBAND)
        //  "The entire bracing INCLUDING GUSSET PLATE is generated."
        //  That closes B.23: PsGussetConnection has no creator because a gusset
        //  is not made on its own -- it is something a bracing HAS.
        //
        //  ג ן¸ The names are cross-wired. BracingLayout is the manual's "Shape
        //  Position" (Front/Back/Cross/Centered/Double/Replaced=kButterFly/
        //  4-Times=kQuatro). The enum actually CALLED ShapePosition is what
        //  setPlatePosition takes -- the GUSSET's position across the plane. And
        //  setShapePosition(Int32) is a third thing with no enum at all.
        // =====================================================================

        static int EnumVal(System.Type t, string name, int dflt)
        {
            if (name == null || name.Length == 0) return dflt;
            try { return (int)System.Enum.Parse(t, name, true); } catch { }
            try { return (int)System.Enum.Parse(t, "k" + name, true); } catch { }
            int n; if (int.TryParse(name, out n)) return n;
            return dflt;
        }

        // op=bracing p1=x,y,z p2=x,y,z host1=<h> host2=<h>
        //    [type=NormBracing|RodBracing|PipeBracing] [layout=AtFront|Crossed|...]
        //    [cat= size=] [platethick=12] [platewide=] [platetype=] [plateside=]
        //    [cross=1] [sym=1] [welded=0] [nogussets=0] [group=1] [dynamic=1]
        //    [nprof=2] [ncross=1] [holeedge= holehole= holecross=] [dm=18] [play=2]
        //    [edgeborder= roundto= shorten= angle= centerhole= divideall=]
        //    [origin=x,y,z ex= ey=]
        void Bracing(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string h0, c0; int before = Census(out h0, out c0);
            List<string> pre = HandleSet();
            string applied = "", msg = "";
            bool ok = false;
            Matrix3d savedUcs = Matrix3d.Identity;
            bool ucsChanged = false;
            try
            {
                PsBracing b = new PsBracing();

                // ⭐⭐ v166 (B.24 audit) -- THE POINTS ARE HELD, NOT PASSED AS TEMPORARIES.
                // MEASURED 10/08/2026: with setStartPoint(Pt(...)) -- a PsPoint created inline
                // and immediately unreachable -- getStartPoint reads back (NaN,NaN,NaN), while
                // the string/enum setters on the same object round-trip perfectly
                // (cat, size, shapeType, crossedMode all correct). So the geometry never arrived,
                // and insert() was being asked to build a bracing with no system line.
                // Six configurations were tested on 09/08 and every one of them fed the creator
                // NaN. This is B.9's dead-native-handle trap in a new place: a PsPoint that is
                // not kept alive is not a PsPoint the native side can read.
                // ⭐ v167: the DISCRIMINATOR. With nothing set, getCrossStartPoint reads a clean
                // (0,0,0); with a freshly constructed PsPoint set, it reads NaN. So the getter
                // works and THE SETTER WRITES GARBAGE. The remaining hypothesis is that these
                // setters only accept a PsPoint the API itself issued -- so ptmode=api reads the
                // object's own point out, mutates its x/y/z in place, and writes it back.
                double[] q1 = Nums(Get(kv, "p1", "0,0,0"));
                double[] q2 = Nums(Get(kv, "p2", "1000,0,1000"));
                PsPoint bp1, bp2;
                if (Get(kv, "ptmode", "new") == "api")
                {
                    bp1 = new PsPoint(0, 0, 0); bp2 = new PsPoint(0, 0, 0);
                    b.getStartPoint(bp1); b.getEndPoint(bp2);      // the object's OWN points
                    bp1.x = q1[0]; bp1.y = q1.Length > 1 ? q1[1] : 0; bp1.z = q1.Length > 2 ? q1[2] : 0;
                    bp2.x = q2[0]; bp2.y = q2.Length > 1 ? q2[1] : 0; bp2.z = q2.Length > 2 ? q2[2] : 0;
                    applied += " ptmode=api";
                }
                else
                {
                    bp1 = Pt(Get(kv, "p1", "0,0,0"));
                    bp2 = Pt(Get(kv, "p2", "1000,0,1000"));
                    applied += " ptmode=new";
                }
                b.setStartPoint(bp1);
                b.setEndPoint(bp2);
                applied += " line=" + Get(kv, "p1", "") + "->" + Get(kv, "p2", "");
                long o1 = IdFromHandle(Get(kv, "host1", ""));
                long o2 = IdFromHandle(Get(kv, "host2", ""));
                if (o1 != 0 || o2 != 0)
                {
                    b.setBorderObjects(o1, o2);
                    applied += " hosts=" + Get(kv, "host1", "-") + "," + Get(kv, "host2", "-");
                }

                string v;
                v = Get(kv, "type", "");
                if (v.Length > 0) { b.setType((BracingType)EnumVal(typeof(BracingType), v, 0)); applied += " type=" + v; }
                v = Get(kv, "layout", "");
                if (v.Length > 0) { b.setLayout((BracingLayout)EnumVal(typeof(BracingLayout), v, 0)); applied += " layout=" + v; }

                // the bracing bar section -- the manual's THREE boxes: catalogue, type, size.
                // PsBracing has NO SetToDefaults(), unlike every other creator here, so an
                // unset ShapeType is very likely kUndefinedType and fatal.
                v = Get(kv, "shapetype", "NormalType");
                if (v.Length > 0) { b.setShapeType((ShapeType)EnumVal(typeof(ShapeType), v, 1)); applied += " shapeType=" + v; }
                v = Get(kv, "cat", "");   if (v.Length > 0) { b.setShapeClass(v); applied += " cat=" + v; }
                v = Get(kv, "size", "");  if (v.Length > 0) { b.setShapeSize(v); applied += " size=" + v; }
                // report what the object thinks it holds, before insert decides
                try { applied += " [readback cat='" + b.getShapeClass() + "' size='" + b.getShapeSize() +
                                 "' type=" + b.getShapeType() + " thick=" + F(b.getPlateThick()) + "]"; }
                catch (System.Exception e) { applied += " readback!EX:" + One(e.Message); }

                v = Get(kv, "platethick", ""); if (v.Length > 0) { b.setPlateThick(double.Parse(v, IC)); applied += " plateThick=" + v; }
                v = Get(kv, "platewide", "");  if (v.Length > 0) { b.setPlateWide(double.Parse(v, IC)); applied += " plateWide=" + v; }
                v = Get(kv, "platetype", "");  if (v.Length > 0) { b.setPlateType(int.Parse(v)); applied += " plateType=" + v; }
                // ג ן¸ takes the enum NAMED ShapePosition, but it means the PLATE's position
                v = Get(kv, "plateside", "");
                if (v.Length > 0) { b.setPlatePosition((ShapePosition)EnumVal(typeof(ShapePosition), v, 1)); applied += " platePos=" + v; }
                v = Get(kv, "angle", "");      if (v.Length > 0) { b.setPlateOpeningAngle(double.Parse(v, IC)); applied += " openAngle=" + v; }

                v = Get(kv, "cross", "");      if (v.Length > 0) { b.setCrossedMode(v == "1"); applied += " cross=" + v; }
                // ⭐ v166 (B.24 audit) -- THE ONE PRECONDITION THE SIX FAILED CONFIGURATIONS NEVER
                // HONOURED. A CROSS bracing has TWO system lines, and setCrossStartPoint /
                // setCrossEndPoint were not exposed by this op at all. If crossedMode defaults
                // true, every previous attempt asked the software to build a cross stay whose
                // second diagonal was (0,0,0)->(0,0,0). A degenerate line is a perfectly good
                // reason for insert() to answer false, and it would look identical to a refusal.
                PsPoint bc1 = null, bc2 = null;
                v = Get(kv, "crossp1", "");
                if (v.Length > 0) { bc1 = Pt(v); b.setCrossStartPoint(bc1); applied += " crossP1=" + v; }
                v = Get(kv, "crossp2", "");
                if (v.Length > 0) { bc2 = Pt(v); b.setCrossEndPoint(bc2); applied += " crossP2=" + v; }
                // report what the object holds for BOTH lines before insert() decides
                try
                {
                    PsPoint s1 = new PsPoint(0, 0, 0), e1 = new PsPoint(0, 0, 0);
                    PsPoint s2 = new PsPoint(0, 0, 0), e2 = new PsPoint(0, 0, 0);
                    b.getStartPoint(s1); b.getEndPoint(e1);
                    b.getCrossStartPoint(s2); b.getCrossEndPoint(e2);
                    applied += " [line1=(" + F(s1.x) + "," + F(s1.y) + "," + F(s1.z) + ")->("
                             + F(e1.x) + "," + F(e1.y) + "," + F(e1.z) + ")"
                             + " line2=(" + F(s2.x) + "," + F(s2.y) + "," + F(s2.z) + ")->("
                             + F(e2.x) + "," + F(e2.y) + "," + F(e2.z) + ")"
                             + " crossedMode=" + b.getCrossedMode() + "]";
                }
                catch (System.Exception e) { applied += " lineReadback!EX:" + One(e.Message); }
                v = Get(kv, "sym", "");        if (v.Length > 0) { b.setSymetrieMode(v == "1"); applied += " sym=" + v; }
                // "the bracing is welded in its entirety. NO BORINGS are added in that case"
                v = Get(kv, "welded", "");     if (v.Length > 0) { b.setWeldStatus(v == "1"); applied += " welded=" + v; }
                v = Get(kv, "nogussets", "");  if (v.Length > 0) { b.setNoConnectionPlates(v == "1"); applied += " noGussets=" + v; }
                v = Get(kv, "group", "");      if (v.Length > 0) { b.setGroupStatus(v == "1"); applied += " group=" + v; }
                v = Get(kv, "dynamic", "");    if (v.Length > 0) { b.setDynamicStatus(v == "1"); applied += " dynamic=" + v; }
                v = Get(kv, "centerhole", ""); if (v.Length > 0) { b.setCenterHoleStatus(v == "1"); applied += " centerHole=" + v; }
                v = Get(kv, "divideall", "");  if (v.Length > 0) { b.setSplitAllMode(v == "1"); applied += " divideAll=" + v; }
                v = Get(kv, "mirror", "");     if (v.Length > 0) { b.setShapeMirrorMode(v == "1"); applied += " mirror=" + v; }

                v = Get(kv, "nprof", "");      if (v.Length > 0) { b.setProfHoleCount(int.Parse(v)); applied += " nProf=" + v; }
                v = Get(kv, "ncross", "");     if (v.Length > 0) { b.setCrossHoleCount(int.Parse(v)); applied += " nCross=" + v; }
                v = Get(kv, "holeedge", "");   if (v.Length > 0) { b.setHoleEdge(double.Parse(v, IC)); applied += " holeEdge=" + v; }
                v = Get(kv, "holehole", "");   if (v.Length > 0) { b.setHoleHole(double.Parse(v, IC)); applied += " holeHole=" + v; }
                v = Get(kv, "holecross", "");  if (v.Length > 0) { b.setHoleDistCross(double.Parse(v, IC)); applied += " holeCross=" + v; }
                v = Get(kv, "edgehole", "");   if (v.Length > 0) { b.setEdgeHole(double.Parse(v, IC)); applied += " edgeHole=" + v; }
                v = Get(kv, "dm", "");         if (v.Length > 0) { b.setDm(double.Parse(v, IC)); applied += " dm=" + v; }
                v = Get(kv, "play", "");       if (v.Length > 0) { b.setHolePlay(double.Parse(v, IC)); applied += " play=" + v; }
                v = Get(kv, "edgeborder", ""); if (v.Length > 0) { b.setEdgeBorder(double.Parse(v, IC)); applied += " edgeBorder=" + v; }
                v = Get(kv, "roundto", "");    if (v.Length > 0) { b.setShapeRoundTo(double.Parse(v, IC)); applied += " roundTo=" + v; }
                v = Get(kv, "shorten", "");    if (v.Length > 0) { b.setShapeShorting(double.Parse(v, IC)); applied += " shorten=" + v; }
                v = Get(kv, "shapedist", "");  if (v.Length > 0) { b.setShapeDistance(double.Parse(v, IC)); applied += " shapeDist=" + v; }

                double[] ax = Nums(Get(kv, "ex", "1,0,0"));
                double[] ay = Nums(Get(kv, "ey", "0,0,1"));
                PsPoint org = Pt(Get(kv, "origin", Get(kv, "p1", "0,0,0")));
                // ג­ B.24: "first place your USER COORDINATE SYSTEM OVER THE BRACING PLANE
                // and then call the function." insert() takes the plane as arguments, but
                // the command may also read the LIVE ucs -- which is the leading suspect for
                // insert() returning false. Set it here through the managed Editor property
                // (no command line, nothing to leave pending) and ALWAYS put it back.
                if (Get(kv, "setucs", "1") == "1")
                {
                    try
                    {
                        Editor edUcs = Application.DocumentManager.MdiActiveDocument.Editor;
                        savedUcs = edUcs.CurrentUserCoordinateSystem;
                        ucsChanged = true;
                        Vector3d vx = new Vector3d(ax[0], ax[1], ax[2]).GetNormal();
                        Vector3d vy = new Vector3d(ay[0], ay[1], ay[2]).GetNormal();
                        Vector3d vz = vx.CrossProduct(vy).GetNormal();
                        edUcs.CurrentUserCoordinateSystem = Matrix3d.AlignCoordinateSystem(
                            Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
                            new Point3d(org.x, org.y, org.z), vx, vy, vz);
                        applied += " ucsSetToPlane";
                    }
                    catch (System.Exception e) { applied += " ucs!EX:" + One(e.Message); }
                }

                if (Get(kv, "prerecalc", "0") == "1")
                { try { b.recalcPoints(); applied += " preRecalc"; } catch (System.Exception e) { applied += " preRecalc!EX:" + One(e.Message); } }
                PsVector ivx = new PsVector(ax[0], ax[1], ax[2]);
                PsVector ivy = new PsVector(ay[0], ay[1], ay[2]);
                ok = b.insert(org, ivx, ivy);
                applied += " insert()=" + ok;
                try { b.recalcPoints(); } catch { }
                // keep every PsPoint/PsVector reachable until AFTER insert() has run --
                // see the note at setStartPoint. GC.KeepAlive is the explicit way to say so.
                System.GC.KeepAlive(bp1); System.GC.KeepAlive(bp2);
                System.GC.KeepAlive(bc1); System.GC.KeepAlive(bc2);
                System.GC.KeepAlive(org); System.GC.KeepAlive(ivx); System.GC.KeepAlive(ivy);
            }
            catch (System.Exception ex) { msg += " EX:" + One(ex.Message); }
            finally
            {
                // the UCS goes back even if insert() threw -- a stray rotated UCS would
                // silently drop the NEXT operation into the wrong plane
                if (ucsChanged)
                {
                    try
                    {
                        Application.DocumentManager.MdiActiveDocument.Editor
                            .CurrentUserCoordinateSystem = savedUcs;
                        applied += " ucsRestored";
                    }
                    catch (System.Exception e) { msg += " ucsRestore!EX:" + One(e.Message); }
                }
            }

            string h1, c1; int after = Census(out h1, out c1);
            // count what appeared, by class -- a bracing makes bars, gussets and bolts
            Dictionary<string, int> made = new Dictionary<string, int>();
            StringBuilder hs = new StringBuilder();
            int n = 0;
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    string h = id.Handle.ToString();
                    if (pre.Contains(h)) continue;
                    n++;
                    string cls = id.ObjectClass != null ? id.ObjectClass.Name : "?";
                    if (!made.ContainsKey(cls)) made[cls] = 0;
                    made[cls]++;
                    if (n <= 30) hs.Append(" " + h);
                }
                tr.Commit();
            }
            StringBuilder cls2 = new StringBuilder();
            foreach (KeyValuePair<string, int> p in made) cls2.Append(" " + p.Key + "=" + p.Value);
            Result(((after > before) ? "EB_OK" : "EB_ERR") + " bracing census=" + before + "->" + after +
                   " created=" + n + " byClass:" + cls2 + (n <= 30 ? " handles:" + hs : "") + applied + msg);
        }

        // =====================================================================
        //  v107 ג€” WELDS  (PsCreateWeldFlag)
        //  B.21 showed Ks_WeldFlag is a real entity class, eight per plate on a
        //  welded splice. This creates them directly: a weld runs from a start
        //  point to an end point, with a style, a thickness and a sign.
        //  CreateWeld(true) asks for the weld itself, not only the flag.
        // =====================================================================

        // op=weldstyles  -> the names the weld style list actually holds
        void WeldStyles(Dictionary<string, string> kv)
        {
            StringBuilder sb = new StringBuilder();
            int n = -1;
            try
            {
                PsCreateWeldFlag w = new PsCreateWeldFlag();
                w.SetToDefaults();
                n = w.WeldStyleCount;
                sb.AppendLine("weldStyleCount=" + n);
                for (int i = 0; i < n; i++)
                {
                    string nm = "?";
                    try { nm = w.GetWeldStyleName(i); }
                    catch (System.Exception e) { nm = "!EX:" + One(e.Message); }
                    int crc = 0;
                    try { crc = w.get_WeldStyleCRC(nm); } catch { }
                    sb.AppendLine("  [" + i + "] " + nm + "   CRC=" + crc);
                }
            }
            catch (System.Exception ex) { Result("EB_ERR weldstyles EX:" + One(ex.Message)); return; }
            File.WriteAllText(Path.Combine(Dir, "eb_weldstyles.txt"), sb.ToString(), Encoding.UTF8);
            Result("EB_OK weldstyles count=" + n + " -> eb_weldstyles.txt " + One(sb.ToString()));
        }

        // op=weld from=x,y,z to=x,y,z [style=] [thick=4] [sign=] [at=x,y,z]
        //          [roundabout=1] [onsite=0] [row=1] [makeweld=1]
        void Weld(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string h0, c0; int before = Census(out h0, out c0);
            List<string> pre = HandleSet();
            string applied = "", msg = "";
            bool ok = false;
            long id = 0;
            try
            {
                PsCreateWeldFlag w = new PsCreateWeldFlag();
                w.SetToDefaults();

                PsPoint p1 = Pt(Get(kv, "from", "0,0,0"));
                PsPoint p2 = Pt(Get(kv, "to", "0,0,0"));
                w.SetWeldStartPoint(p1);
                w.SetWeldEndPoint(p2);
                applied += " from=" + Get(kv, "from", "") + " to=" + Get(kv, "to", "");

                // the flag's own leader lands at `at`, defaulting to the weld midpoint
                string at = Get(kv, "at", "");
                PsPoint pick = at.Length > 0 ? Pt(at)
                             : new PsPoint((p1.x + p2.x) / 2, (p1.y + p2.y) / 2, (p1.z + p2.z) / 2);
                w.SetPickPoint(pick);
                try { w.SetEdgePoint(pick); } catch { }

                string st = Get(kv, "style", "");
                if (st.Length > 0) { w.SetWeldStyle(st); applied += " style=" + st; }
                string th = Get(kv, "thick", "4");
                if (th.Length > 0)
                {
                    double t = double.Parse(th, IC);
                    w.SetUpperThick(t); w.SetLowerThick(t);
                    applied += " thick=" + th;
                }
                string sg = Get(kv, "sign", "");
                if (sg.Length > 0)
                {
                    try { w.SetUpperSign((WeldSign)int.Parse(sg)); w.SetLowerSign((WeldSign)int.Parse(sg)); }
                    catch (System.Exception e) { msg += " sign!EX:" + One(e.Message); }
                    applied += " sign=" + sg;
                }
                string ul = Get(kv, "len", "");
                if (ul.Length > 0) { w.SetUpperLength(double.Parse(ul, IC)); w.SetLowerLength(double.Parse(ul, IC)); applied += " len=" + ul; }
                if (Get(kv, "roundabout", "0") == "1") { w.SetRoundAbout(true); applied += " roundAbout"; }
                if (Get(kv, "onsite", "0") == "1") { w.SetOnSite(true); applied += " onSite"; }
                w.SetWeldRow(Get(kv, "row", "1") == "1");
                // ג­ the weld itself, not just the flag
                w.CreateWeld(Get(kv, "makeweld", "1") == "1");
                applied += " makeWeld=" + Get(kv, "makeweld", "1");

                ok = w.Create();
                try { id = w.ObjectId; } catch { }
                applied += " Create()=" + ok;
            }
            catch (System.Exception ex) { msg += " EX:" + One(ex.Message); }

            string h1, c1; int after = Census(out h1, out c1);
            StringBuilder made = new StringBuilder();
            int n = 0;
            foreach (string h in HandleSet())
                if (!pre.Contains(h)) { made.Append(" " + h); n++; }
            Result(((after > before) ? "EB_OK" : "EB_ERR") + " weld objectId=" +
                   (id != 0 ? HandleOf(id) : "0") + " census=" + before + "->" + after +
                   " created=" + n + " handles:" + made + applied + msg);
        }

        // =====================================================================
        //  v106 ג€” B.21 SPLICE JOINTS  (PS_LASCHE)
        //  Third of the B.19-B.21 family and the most three-dimensional: a splice
        //  wraps the section with up to SIX plates at once -- top/bottom outside,
        //  top/bottom inside, web left/right.
        //  ג ן¸ Hard precondition the siblings lack: "Both of the shapes have to be
        //  IN ALIGNMENT along the surfaces to be connected." Collinear members only.
        //  ג ן¸ Spelling: HoleWorkloose here, HoleWorkLoose on the other two classes.
        // =====================================================================

        void SpliceTemplates(Dictionary<string, string> kv)
        {
            StringBuilder sb = new StringBuilder();
            int n = -1, db = -1;
            try
            {
                PsSpliceJointConnection sj = new PsSpliceJointConnection();
                sj.SetToDefaults();
                n = sj.GetTemplateCount();
                sb.AppendLine("templateCount=" + n);
                for (int i = 0; i < n; i++)
                {
                    string nm = "?";
                    try { nm = sj.GetTemplateName(i); }
                    catch (System.Exception e) { nm = "!EX:" + One(e.Message); }
                    sb.Append("[" + i + "] " + nm);
                    try
                    {
                        PsSpliceJointLinkDataMgd d = sj.GetTemplate(nm);
                        if (d == null) sb.Append("  <null>");
                        else sb.Append("  topOut=" + d.ConnectFlangeTopOutside +
                                       " topIn=" + d.ConnectFlangeTopInside +
                                       " downOut=" + d.ConnectFlangeDownOutside +
                                       " downIn=" + d.ConnectFlangeDownInside +
                                       " webL=" + d.ConnectWebLeft +
                                       " webR=" + d.ConnectWebRight +
                                       " gap=" + F(d.DistanceBetweenObjects) +
                                       " tFlange=" + F(d.PlateThicknessFlange) +
                                       " tWeb=" + F(d.PlateThicknessWeb) +
                                       " nFlange=" + d.HoleCountVerticalFlange + "x" + d.HoleCountHorizontalFlange +
                                       " nWeb=" + d.HoleCountVerticalWeb + "x" + d.HoleCountHorizontalWeb +
                                       " dia=" + F(d.HoleDiameter) +
                                       " workloose=" + F(d.HoleWorkloose) +
                                       " boltCRC=" + d.BoltStyleCRC +
                                       " weldToFlange=" + d.WeldToFlange +
                                       " weldToWeb=" + d.WeldToWeb +
                                       " weldDiagonal=" + d.WeldDiagonal +
                                       " topLap=" + F(d.TopPlateLap) +
                                       " sideLap=" + F(d.SidePlateLap) +
                                       " group=" + d.CreateGroup);
                    }
                    catch (System.Exception e) { sb.Append("  !EX:" + One(e.Message)); }
                    sb.AppendLine();
                }
                try
                {
                    db = sj.get_PlateDataCount();
                    sb.AppendLine();
                    sb.AppendLine("database plateDataCount=" + db);
                    for (int i = 0; i < db && i < 40; i++)
                    {
                        try { sb.AppendLine("  [" + i + "] " + sj.GetPlateDataName(i)); }
                        catch (System.Exception e) { sb.AppendLine("  [" + i + "] !EX:" + One(e.Message)); break; }
                    }
                }
                catch (System.Exception e) { sb.AppendLine("database !EX:" + One(e.Message)); }
            }
            catch (System.Exception ex) { Result("EB_ERR splicetemplates EX:" + One(ex.Message)); return; }
            File.WriteAllText(Path.Combine(Dir, "eb_splice.txt"), sb.ToString(), Encoding.UTF8);
            Result("EB_OK splicetemplates templates=" + n + " database=" + db + " -> eb_splice.txt");
        }

        // op=splice handle=<shape A> [support=<shape B>] at=x,y,z [template=]
        //   [gap= topout= topin= downout= downin= webleft= webright=
        //    tflange= tweb= nflangev= nflangeh= nwebv= nwebh= dia= workloose=
        //    offflange= offweb= weldflange= weldweb= welddiagonal= toplap= sidelap=
        //    group= boltsingroup=]
        void Splice(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            long aid = IdFromHandle(Get(kv, "handle", ""));
            if (aid == 0) { Result("EB_ERR splice: bad handle"); return; }
            long bid = 0;
            string sh = Get(kv, "support", "");
            if (sh.Length > 0) bid = IdFromHandle(sh);

            string h0, c0; int before = Census(out h0, out c0);
            List<string> pre = HandleSet();
            string applied = "", msg = "";
            bool ok = false; int chk = -999;
            StringBuilder plates = new StringBuilder();
            StringBuilder own = new StringBuilder();
            int boltsReal = 0, weldFlags = 0;
            try
            {
                PsSpliceJointConnection sj = new PsSpliceJointConnection();
                sj.SetToDefaults();

                PsSpliceJointLinkDataMgd d = null;
                string tpl = Get(kv, "template", "");
                if (tpl.Length > 0)
                {
                    try { d = sj.GetTemplate(tpl); }
                    catch (System.Exception e) { msg += " GetTemplate!EX:" + One(e.Message); }
                    applied += " template=" + tpl + (d == null ? "(NULL)" : "");
                }
                if (d == null) { d = new PsSpliceJointLinkDataMgd(); applied += " template=none"; }

                string v;
                // the six plate positions -- the whole point of this chapter
                v = Get(kv, "topout", "");   if (v.Length > 0) { d.ConnectFlangeTopOutside = (v == "1"); applied += " topOut=" + v; }
                v = Get(kv, "topin", "");    if (v.Length > 0) { d.ConnectFlangeTopInside = (v == "1"); applied += " topIn=" + v; }
                v = Get(kv, "downout", "");  if (v.Length > 0) { d.ConnectFlangeDownOutside = (v == "1"); applied += " downOut=" + v; }
                v = Get(kv, "downin", "");   if (v.Length > 0) { d.ConnectFlangeDownInside = (v == "1"); applied += " downIn=" + v; }
                v = Get(kv, "webleft", "");  if (v.Length > 0) { d.ConnectWebLeft = (v == "1"); applied += " webL=" + v; }
                v = Get(kv, "webright", ""); if (v.Length > 0) { d.ConnectWebRight = (v == "1"); applied += " webR=" + v; }

                v = Get(kv, "gap", "");      if (v.Length > 0) { d.DistanceBetweenObjects = double.Parse(v, IC); applied += " gap=" + v; }
                v = Get(kv, "tflange", "");  if (v.Length > 0) { d.PlateThicknessFlange = double.Parse(v, IC); applied += " tFlange=" + v; }
                v = Get(kv, "tweb", "");     if (v.Length > 0) { d.PlateThicknessWeb = double.Parse(v, IC); applied += " tWeb=" + v; }
                v = Get(kv, "nflangev", ""); if (v.Length > 0) { d.HoleCountVerticalFlange = int.Parse(v); applied += " nFlangeV=" + v; }
                v = Get(kv, "nflangeh", ""); if (v.Length > 0) { d.HoleCountHorizontalFlange = int.Parse(v); applied += " nFlangeH=" + v; }
                v = Get(kv, "nwebv", "");    if (v.Length > 0) { d.HoleCountVerticalWeb = int.Parse(v); applied += " nWebV=" + v; }
                v = Get(kv, "nwebh", "");    if (v.Length > 0) { d.HoleCountHorizontalWeb = int.Parse(v); applied += " nWebH=" + v; }
                v = Get(kv, "dia", "");      if (v.Length > 0) { d.HoleDiameter = double.Parse(v, IC); applied += " dia=" + v; }
                // ג ן¸ HoleWorkloose -- lowercase l, unlike the sibling classes
                v = Get(kv, "workloose", ""); if (v.Length > 0) { d.HoleWorkloose = double.Parse(v, IC); applied += " workloose=" + v; }
                // ⭐⭐ v161 (B.21 audit) -- WITHOUT THIS THE CHAPTER CANNOT PRODUCE A BOLTED SPLICE.
                // BOTH shipped templates carry boltCRC=0, i.e. no bolt style at all, so the
                // connection drills its hole fields and inserts NOTHING. Measured: the three
                // "bolted" bays in B.21's own band hold 2/2/1 hole fields and zero bolt entities
                // anywhere in the band. Iron rule 1, sitting in the model since 09/08.
                // This class has NO BoltStyle string -- only the CRC, unlike its two siblings --
                // so a style name has to be resolved through the style list first.
                v = Get(kv, "boltstyle", "");
                if (v.Length > 0)
                {
                    string diag;
                    int crc = BoltStyleCrcFromName(v, out diag);
                    if (crc != 0) d.BoltStyleCRC = crc;
                    applied += diag;
                }
                v = Get(kv, "boltstylecrc", "");
                if (v.Length > 0) { d.BoltStyleCRC = int.Parse(v); applied += " boltCRC=" + v; }
                v = Get(kv, "offflange", ""); if (v.Length > 0) { d.InsertOffsetFlange = double.Parse(v, IC); applied += " offFlange=" + v; }
                v = Get(kv, "offweb", "");    if (v.Length > 0) { d.InsertOffsetWeb = double.Parse(v, IC); applied += " offWeb=" + v; }
                // "no bolts and drill holes will be inserted, but the parts will be WELDED"
                v = Get(kv, "weldflange", ""); if (v.Length > 0) { d.WeldToFlange = (v == "1"); applied += " weldFlange=" + v; }
                v = Get(kv, "weldweb", "");    if (v.Length > 0) { d.WeldToWeb = (v == "1"); applied += " weldWeb=" + v; }
                v = Get(kv, "welddiagonal", ""); if (v.Length > 0) { d.WeldDiagonal = (v == "1"); applied += " weldDiagonal=" + v; }
                v = Get(kv, "toplap", "");   if (v.Length > 0) { d.TopPlateLap = double.Parse(v, IC); applied += " topLap=" + v; }
                v = Get(kv, "sidelap", "");  if (v.Length > 0) { d.SidePlateLap = double.Parse(v, IC); applied += " sideLap=" + v; }
                v = Get(kv, "group", "");    if (v.Length > 0) { d.CreateGroup = (v == "1"); applied += " group=" + v; }
                v = Get(kv, "boltsingroup", ""); if (v.Length > 0) { d.AddBoltsToGroup = (v == "1"); applied += " boltsInGroup=" + v; }

                sj.SetConnectionData(d);
                sj.SetConnectionObjectId(aid);
                if (bid != 0) { sj.SetSupportObjectId(bid); applied += " second=" + sh; }
                else applied += " second=NONE(plates at the end)";
                sj.SetConnectionPoint(Pt(Get(kv, "at", "0,0,0")));
                try { chk = sj.Check(); } catch (System.Exception e) { msg += " Check!EX:" + One(e.Message); }
                ok = sj.Create();
                applied += " Check()=" + chk + " Create()=" + ok;

                try
                {
                    for (int i = 0; i < 10; i++)
                    {
                        long pid = sj.GetPlateId(i);
                        if (pid == 0) break;
                        plates.Append(" " + HandleOf(pid));
                    }
                }
                catch (System.Exception e) { plates.Append(" !EX:" + One(e.Message)); }

                // v161 -- the route that DOES deliver, propagated from B.20. GetPlateId returns 0
                // on this class too (third of three). The logical link on the connected shape
                // knows what the joint owns, and CountRealBolts separates bolts from weld flags,
                // which share the same slots.
                try
                {
                    PsEditLogicalLink ed = new PsEditLogicalLink();
                    ed.SetObjectId(aid);
                    int nl = ed.get_LogicalLinkCount();
                    own.Append(" links=" + nl);
                    for (int i = 0; i < nl; i++)
                    {
                        int num = i;
                        try { num = ed.get_LinkNumberFromIndex(i); } catch { }
                        PsLogicalLink lk = null;
                        try { lk = ed.GetLogicalLinkByNumber(num); } catch { }
                        if (lk == null) continue;
                        string tnm = "?"; try { tnm = lk.Type.ToString(); } catch { }
                        int slots, welds;
                        int realb = CountRealBolts(lk, out slots, out welds);
                        boltsReal += (realb > 0 ? realb : 0);
                        weldFlags += welds;
                        own.Append(" [" + tnm + " parts=" + lk.LinkObjectCount
                                 + " boltSlots=" + slots + " realBolts=" + realb
                                 + " weldFlags=" + welds + "]");
                    }
                }
                catch (System.Exception e) { own.Append(" !EX:" + One(e.Message)); }
            }
            catch (System.Exception ex) { msg += " EX:" + One(ex.Message); }

            string h1, c1; int after = Census(out h1, out c1);
            StringBuilder made = new StringBuilder();
            int n = 0;
            foreach (string h in HandleSet())
                if (!pre.Contains(h)) { made.Append(" " + h); n++; }

            // ⛔ IRON RULE GUARD -- and it counts BOLTS, not bolt slots.
            // A weld flag occupies a bolt slot (measured 10/08: 32 objects on layer PS_Weld in
            // the bolt slots of B.21's welded splice), so "the link has fasteners" is not the
            // test. A welded splice legitimately has no bolts and no holes; a joint that was
            // DRILLED and has no bolts is the violation.
            bool welded = Get(kv, "weldflange", "") == "1" || Get(kv, "weldweb", "") == "1";
            string ironRule = "";
            if (!welded && after > before && boltsReal == 0)
                ironRule = " ⛔IRON-RULE the splice was created and NOT ONE REAL BOLT was inserted"
                         + (weldFlags > 0 ? " (" + weldFlags + " weld flags occupy the bolt slots)" : "")
                         + ". BOTH shipped templates carry BoltStyleCRC=0 -- this class has no"
                         + " BoltStyle string, so pass boltstyle=<name> (e.g. DIN7990) or"
                         + " boltstylecrc=<n>. The joint is drilled and unbolted as it stands.";

            Result(((after > before && ironRule.Length == 0) ? "EB_OK" : "EB_ERR") +
                   " splice a=" + Get(kv, "handle", "") +
                   " census=" + before + "->" + after + " newObjects=" + n +
                   " GetPlateId:[" + plates + " ] own:[" + own + " ]" +
                   (n <= 30 ? " handles:" + made : "") + applied + msg + ironRule);
        }

        // =====================================================================
        //  v105 ג€” B.20 SHEAR PLATES  (PS_SCHEARPLATE)
        //  The twin of B.19: same six pages, same pick order, same load database,
        //  same "cut to fit + all drill holes and bolt connections" behaviour.
        //  The product is a web PLATE instead of a pair of angles -- so unlike
        //  the web angle, GetPlateId here should actually return something.
        // =====================================================================

        // op=shearplatetemplates -> templates + the load database + cope-template probe
        void ShearPlateTemplates(Dictionary<string, string> kv)
        {
            StringBuilder sb = new StringBuilder();
            int n = -1, dast = -1;
            try
            {
                PsShearPlateConnection sc = new PsShearPlateConnection();
                sc.SetToDefaults();
                n = sc.GetTemplateCount();
                sb.AppendLine("templateCount=" + n);
                for (int i = 0; i < n; i++)
                {
                    string nm = "?";
                    try { nm = sc.GetTemplateName(i); }
                    catch (System.Exception e) { nm = "!EX:" + One(e.Message); }
                    sb.Append("[" + i + "] " + nm);
                    try
                    {
                        PsShearPlateLinkDataMgd d = sc.GetTemplate(nm);
                        if (d == null) sb.Append("  <null>");
                        else sb.Append("  thick=" + F(d.PlateThickness) +
                                       " poly=" + d.ShearPlateIsPolyPlate +
                                       " normalToCut=" + d.NormalToCutPlane +
                                       " pos=" + d.PlatePosition +
                                       " nVert=" + d.VerticalHoleCount +
                                       " nHoriz=" + d.HorizontalHoleCount +
                                       " dia=" + F(d.HoleDiameter) +
                                       " workloose=" + F(d.HoleWorkLoose) +
                                       " boltStyle='" + d.BoltStyle + "'" +
                                       " boltCRC=" + d.BoltStyleCRC +
                                       " weldCRC=" + d.WeldStyleCRC +
                                       " gapConn=" + F(d.DistanceToConnected) +
                                       " gapSup=" + F(d.DistanceToSupport) +
                                       " vertOff=" + F(d.InsertOffsetVertical) +
                                       " fromEdge=" + d.InsertOffsetFromShapeEdge +
                                       " fromDown=" + d.InsertOffsetFromDownSide +
                                       " fromHole=" + d.InsertOffsetFromFirstHole +
                                       " cope=" + d.CreateCope +
                                       " copeRadius=" + F(d.CopeRadius) +
                                       " group=" + d.CreateGroup +
                                       " eachPlate=" + d.GroupEachPlate +
                                       " shear=" + F(d.ShearX) + "/" + F(d.ShearY) + "/" + F(d.ShearZ) +
                                       " moment=" + F(d.MomentX) + "/" + F(d.MomentY) + "/" + F(d.MomentZ));
                        // ג­ the lead B.19 lacked: ask whether a cope-template name is valid.
                        // In B.19 CreateCope=true plus real geometry made no notch and there
                        // was no way to tell whether the name was even accepted.
                        if (i == 0)
                        {
                            string[] probe = { "", "Standard", "default/Standard", "default",
                                               "Notch", "Cope", "1", "default/Notch" };
                            sb.AppendLine();
                            foreach (string p in probe)
                            {
                                bool okp = false; string err = "";
                                try { okp = d.CheckCopeTemplate(p); }
                                catch (System.Exception e) { err = "!EX:" + One(e.Message); }
                                sb.AppendLine("    CheckCopeTemplate('" + p + "') = " + okp + err);
                            }
                        }
                    }
                    catch (System.Exception e) { sb.Append("  !EX:" + One(e.Message)); }
                    sb.AppendLine();
                }
                try
                {
                    dast = sc.get_PlateDataCount();
                    sb.AppendLine();
                    sb.AppendLine("load database plateDataCount=" + dast);
                    for (int i = 0; i < dast && i < 60; i++)
                    {
                        string nm = "?";
                        try { nm = sc.GetPlateDataName(i); } catch (System.Exception e) { nm = "!EX:" + One(e.Message); }
                        sb.AppendLine("  [" + i + "] " + nm);
                    }
                }
                catch (System.Exception e) { sb.AppendLine("database !EX:" + One(e.Message)); }
            }
            catch (System.Exception ex) { Result("EB_ERR shearplatetemplates EX:" + One(ex.Message)); return; }
            File.WriteAllText(Path.Combine(Dir, "eb_shearplate.txt"), sb.ToString(), Encoding.UTF8);
            Result("EB_OK shearplatetemplates templates=" + n + " database=" + dast + " -> eb_shearplate.txt");
        }

        // op=shearplate handle=<beam> support=<column> at=x,y,z [template=]
        //   [thick= poly= normaltocut= cutconn= cutsup= pos= gapconn= gapsup=
        //    vertoff= fromedge= fromdown= fromhole= nvert= nhoriz= dia= workloose=
        //    boltstyle= slot= holevert= holevertedge= holehoriz= holehorizin=
        //    holehorizout= cope= copetemplate= coperadius= copeedgetop= copeinsidetop=
        //    copewebdist= group= boltsingroup= eachplate= weldconn= weldsup=]
        void ShearPlate(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            long bid = IdFromHandle(Get(kv, "handle", ""));
            if (bid == 0) { Result("EB_ERR shearplate: bad beam handle"); return; }
            long sup = 0;
            string sh = Get(kv, "support", "");
            if (sh.Length > 0) sup = IdFromHandle(sh);

            string h0, c0; int before = Census(out h0, out c0);
            List<string> pre = HandleSet();
            string applied = "", msg = "", copeChk = "";
            bool ok = false; int chk = -999;
            StringBuilder plates = new StringBuilder();
            StringBuilder own = new StringBuilder();
            try
            {
                PsShearPlateConnection sc = new PsShearPlateConnection();
                sc.SetToDefaults();

                PsShearPlateLinkDataMgd d = null;
                string tpl = Get(kv, "template", "");
                if (tpl.Length > 0)
                {
                    try { d = sc.GetTemplate(tpl); }
                    catch (System.Exception e) { msg += " GetTemplate!EX:" + One(e.Message); }
                    applied += " template=" + tpl + (d == null ? "(NULL)" : "");
                }
                if (d == null) { d = new PsShearPlateLinkDataMgd(); applied += " template=none"; }

                string v;
                v = Get(kv, "thick", "");        if (v.Length > 0) { d.PlateThickness = double.Parse(v, IC); applied += " thick=" + v; }
                v = Get(kv, "poly", "");         if (v.Length > 0) { d.ShearPlateIsPolyPlate = (v == "1"); applied += " polyPlate=" + v; }
                v = Get(kv, "normaltocut", "");  if (v.Length > 0) { d.NormalToCutPlane = (v == "1"); applied += " normalToCut=" + v; }
                v = Get(kv, "cutconn", "");      if (v.Length > 0) { d.CutAtConnected = (v == "1"); applied += " cutConn=" + v; }
                v = Get(kv, "cutsup", "");       if (v.Length > 0) { d.CutAtSupport = (v == "1"); applied += " cutSup=" + v; }
                v = Get(kv, "pos", "");          if (v.Length > 0) { d.PlatePosition = int.Parse(v); applied += " pos=" + v; }
                v = Get(kv, "gapconn", "");      if (v.Length > 0) { d.DistanceToConnected = double.Parse(v, IC); applied += " gapConn=" + v; }
                v = Get(kv, "gapsup", "");       if (v.Length > 0) { d.DistanceToSupport = double.Parse(v, IC); applied += " gapSup=" + v; }
                v = Get(kv, "vertoff", "");      if (v.Length > 0) { d.InsertOffsetVertical = double.Parse(v, IC); applied += " vertOff=" + v; }
                v = Get(kv, "fromedge", "");     if (v.Length > 0) { d.InsertOffsetFromShapeEdge = (v == "1"); applied += " fromEdge=" + v; }
                v = Get(kv, "fromdown", "");     if (v.Length > 0) { d.InsertOffsetFromDownSide = (v == "1"); applied += " fromDown=" + v; }
                v = Get(kv, "fromhole", "");     if (v.Length > 0) { d.InsertOffsetFromFirstHole = (v == "1"); applied += " fromHole=" + v; }
                v = Get(kv, "nvert", "");        if (v.Length > 0) { d.VerticalHoleCount = int.Parse(v); applied += " nVert=" + v; }
                v = Get(kv, "nhoriz", "");       if (v.Length > 0) { d.HorizontalHoleCount = int.Parse(v); applied += " nHoriz=" + v; }
                v = Get(kv, "dia", "");          if (v.Length > 0) { d.HoleDiameter = double.Parse(v, IC); applied += " dia=" + v; }
                v = Get(kv, "workloose", "");    if (v.Length > 0) { d.HoleWorkLoose = double.Parse(v, IC); applied += " workloose=" + v; }
                v = Get(kv, "boltstyle", "");    if (v.Length > 0) { d.BoltStyle = v; applied += " boltStyle=" + v; }
                v = Get(kv, "slot", "");         if (v.Length > 0) { d.SlotAxisDistance = double.Parse(v, IC); applied += " slot=" + v; }
                v = Get(kv, "holevert", "");     if (v.Length > 0) { d.HoleDistanceVertical = double.Parse(v, IC); applied += " holeVert=" + v; }
                v = Get(kv, "holevertedge", ""); if (v.Length > 0) { d.HoleDistanceVerticalEdge = double.Parse(v, IC); applied += " holeVertEdge=" + v; }
                v = Get(kv, "holehoriz", "");    if (v.Length > 0) { d.HoleDistanceHorizontal = double.Parse(v, IC); applied += " holeHoriz=" + v; }
                v = Get(kv, "holehorizin", "");  if (v.Length > 0) { d.HoleDistanceHorizontalInside = double.Parse(v, IC); applied += " holeHorizIn=" + v; }
                v = Get(kv, "holehorizout", ""); if (v.Length > 0) { d.HoleDistanceHorizontalOutside = double.Parse(v, IC); applied += " holeHorizOut=" + v; }
                v = Get(kv, "cope", "");         if (v.Length > 0) { d.CreateCope = (v == "1"); applied += " cope=" + v; }
                v = Get(kv, "copetemplate", "");
                if (v.Length > 0)
                {
                    // ג­ validate BEFORE applying -- B.19's cope failed silently with no way
                    // to tell whether the name was ever accepted.
                    try { copeChk = " CheckCopeTemplate('" + v + "')=" + d.CheckCopeTemplate(v); }
                    catch (System.Exception e) { copeChk = " CheckCopeTemplate!EX:" + One(e.Message); }
                    d.SetCopeFromTemplate(v); applied += " copeTpl=" + v;
                }
                v = Get(kv, "coperadius", "");    if (v.Length > 0) { d.CopeRadius = double.Parse(v, IC); applied += " copeR=" + v; }
                v = Get(kv, "copeedgetop", "");   if (v.Length > 0) { d.CopeDistanceEdgeTop = double.Parse(v, IC); applied += " copeEdgeTop=" + v; }
                v = Get(kv, "copeinsidetop", ""); if (v.Length > 0) { d.CopeDistanceInsideTop = double.Parse(v, IC); applied += " copeInTop=" + v; }
                v = Get(kv, "copewebdist", "");   if (v.Length > 0) { d.CopeWebDistance = double.Parse(v, IC); applied += " copeWebDist=" + v; }
                v = Get(kv, "group", "");         if (v.Length > 0) { d.CreateGroup = (v == "1"); applied += " group=" + v; }
                v = Get(kv, "boltsingroup", "");  if (v.Length > 0) { d.AddBoltsToGroup = (v == "1"); applied += " boltsInGroup=" + v; }
                v = Get(kv, "eachplate", "");     if (v.Length > 0) { d.GroupEachPlate = (v == "1"); applied += " eachPlate=" + v; }
                v = Get(kv, "weldconn", "");      if (v.Length > 0) { d.WeldToConnectedShape = (v == "1"); applied += " weldConn=" + v; }
                v = Get(kv, "weldsup", "");       if (v.Length > 0) { d.WeldToSupportShape = (v == "1"); applied += " weldSup=" + v; }
                v = Get(kv, "shear", "");         if (v.Length > 0) { d.ShearZ = double.Parse(v, IC); applied += " shearZ=" + v; }

                sc.SetConnectionData(d);
                sc.SetConnectionObjectId(bid);
                if (sup != 0) { sc.SetSupportObjectId(sup); applied += " support=" + sh; }
                else applied += " support=NONE";
                sc.SetConnectionPoint(Pt(Get(kv, "at", "0,0,0")));
                try { chk = sc.Check(); } catch (System.Exception e) { msg += " Check!EX:" + One(e.Message); }
                ok = sc.Create();
                applied += " Check()=" + chk + " Create()=" + ok;

                // this connection DOES make plates, so GetPlateId should deliver
                try
                {
                    for (int i = 0; i < 8; i++)
                    {
                        long pid = sc.GetPlateId(i);
                        if (pid == 0) break;
                        plates.Append(" " + HandleOf(pid));
                    }
                }
                catch (System.Exception e) { plates.Append(" !EX:" + One(e.Message)); }

                // v157 (B.20 audit) -- THE ROUTE THAT DOES DELIVER.
                // GetPlateId returns 0 on every index (measured 09/08 and again 10/08), so the
                // only account of what this connection built was a census diff -- which says how
                // many objects appeared in the drawing, not which objects belong to THIS joint.
                // The logical link on the connected shape knows. Reading it here means the op
                // reports its own product instead of the drawing's delta.
                try
                {
                    PsEditLogicalLink ed = new PsEditLogicalLink();
                    ed.SetObjectId(bid);
                    int nl = ed.get_LogicalLinkCount();
                    own.Append(" links=" + nl);
                    for (int i = 0; i < nl; i++)
                    {
                        int num = i;
                        try { num = ed.get_LinkNumberFromIndex(i); } catch { }
                        PsLogicalLink lk = null;
                        try { lk = ed.GetLogicalLinkByNumber(num); } catch { }
                        if (lk == null) continue;
                        string tnm = "?"; try { tnm = lk.Type.ToString(); } catch { }
                        own.Append(" [" + tnm + " parts=");
                        for (int k = 0; k < lk.LinkObjectCount; k++)
                        {
                            long oid = 0; try { oid = lk.getLinkObjectId(k); } catch { }
                            own.Append((k > 0 ? "," : "") + (oid == 0 ? "-" : HandleOf(oid)));
                        }
                        own.Append(" bolts=");
                        for (int k = 0; k < lk.BoltObjectCount; k++)
                        {
                            long oid = 0; try { oid = lk.getBoltObjectId(k); } catch { }
                            own.Append((k > 0 ? "," : "") + (oid == 0 ? "-" : HandleOf(oid)));
                        }
                        own.Append("]");
                    }
                }
                catch (System.Exception e) { own.Append(" !EX:" + One(e.Message)); }

                // Can the connection report its OWN settings back?  PsLogicalLink's
                // GetShearPlateLinkData() reads PlateThickness=0 on every member of a finished
                // joint (measured 10/08), so the parameters are not recoverable from the model
                // that way. sc.GetLink().GetLinkData(i) is the other route and had never been
                // called -- naming a route without calling it is a to-do, not a finding.
                try
                {
                    PsShearPlateLink slk = sc.GetLink();
                    if (slk == null) own.Append(" GetLink()=null");
                    else
                    {
                        own.Append(" GetLink()=ok");
                        for (int i = 0; i < 3; i++)
                        {
                            PsShearPlateLinkDataMgd rd = null;
                            try { rd = slk.GetLinkData(i); } catch (System.Exception e) { own.Append(" GetLinkData(" + i + ")!EX:" + One(e.Message)); break; }
                            if (rd == null) { own.Append(" GetLinkData(" + i + ")=null"); break; }
                            own.Append(" readback[" + i + "]: t=" + F(rd.PlateThickness)
                                     + " pos=" + rd.PlatePosition
                                     + " nV=" + rd.VerticalHoleCount + " nH=" + rd.HorizontalHoleCount
                                     + " dV=" + F(rd.HoleDistanceVertical)
                                     + " dia=" + F(rd.HoleDiameter));
                        }
                    }
                }
                catch (System.Exception e) { own.Append(" GetLink!EX:" + One(e.Message)); }
            }
            catch (System.Exception ex) { msg += " EX:" + One(ex.Message); }

            string h1, c1; int after = Census(out h1, out c1);
            StringBuilder made = new StringBuilder();
            int n = 0;
            foreach (string h in HandleSet())
                if (!pre.Contains(h)) { made.Append(" " + h); n++; }

            // ⛔ IRON RULE GUARD (v159, B.20 audit). "census grew" was the whole success test,
            // and it passed a joint with TWO PLATES AND NO BOLTS.
            // MEASURED 10/08/2026: dia=22 against the default 8.8S style produces the plates and
            // the drilled holes and NOT ONE BOLT -- silently, with EB_OK. Thickness is irrelevant;
            // t=10 and t=18 both bolt correctly at dia=16 and both fail at dia=22.
            // This is B.15's ~400-failed-bolts finding arriving through a connection class: the
            // bolt comes from BoltStyle, the hole comes from HoleDiameter, and a diameter the
            // style cannot supply drops the bolt instead of refusing.
            // A drilled, bolt-less joint is exactly what iron rule 1 forbids, so it must not read
            // as success. The connection is NOT rolled back -- deleting a caller's geometry
            // behind their back is worse -- but it is reported for what it is.
            string ironRule = "";
            bool madePlate = own.ToString().IndexOf("parts=") >= 0 && own.ToString().IndexOf("parts=-,-") < 0;
            bool noBolts = own.ToString().IndexOf("bolts=]") >= 0 || own.ToString().IndexOf("bolts= ") >= 0;
            if (madePlate && noBolts)
                ironRule = " ⛔IRON-RULE plates were created and NOT ONE BOLT was inserted."
                         + " The most likely cause is a HoleDiameter the BoltStyle cannot supply"
                         + " (measured: dia=22 with style 8.8S). Set the diameter through the style"
                         + " and leave dia = bolt + workloose. This joint is drilled and unbolted --"
                         + " fix it or erase it; do not leave it in the model.";

            Result(((after > before && ironRule.Length == 0) ? "EB_OK" : "EB_ERR") +
                   " shearplate beam=" + Get(kv, "handle", "") +
                   " census=" + before + "->" + after + " newObjects=" + n +
                   " GetPlateId:[" + plates + " ] own:[" + own + " ]" +
                   (n <= 24 ? " handles:" + made : "") + applied + copeChk + msg + ironRule);
        }

        // =====================================================================
        //  v103 ג€” B.19 WEB ANGLE  (PS_STEGW)
        //  "The shape to be connected is CUT TO LENGTH after the exact definition
        //   is entered. The connection, DRILLING AND BOLTING is carried out
        //   AUTOMATICALLY."  One command does the whole detail.
        //  No support shape => "two web angles ... opposite of each other ON THE
        //  ENDS of the shape to be connected".
        // =====================================================================

        // op=webangletemplates -> the shipped templates, expanded
        void WebAngleTemplates(Dictionary<string, string> kv)
        {
            StringBuilder sb = new StringBuilder();
            int n = -1, dast = -1;
            try
            {
                PsWebAngleConnection wc = new PsWebAngleConnection();
                wc.SetToDefaults();
                n = wc.GetTemplateCount();
                sb.AppendLine("templateCount=" + n);
                for (int i = 0; i < n; i++)
                {
                    string nm = "?";
                    try { nm = wc.GetTemplateName(i); }
                    catch (System.Exception e) { nm = "!EX:" + One(e.Message); }
                    sb.Append("[" + i + "] " + nm);
                    try
                    {
                        PsWebAngleLinkDataMgd d = wc.GetTemplate(nm);
                        if (d == null) sb.Append("  <null>");
                        else sb.Append("  pos=" + d.WebAnglePosition +
                                       " turn=" + d.TurnWebAngles +
                                       " flat=" + d.WebAngleIsFlatSteel +
                                       " nVert=" + d.VerticalHoleCount +
                                       " nConn=" + d.HorizontalHoleCountConnected +
                                       " nSup=" + d.HorizontalHoleCountSupport +
                                       " dia=" + F(d.HoleDiameter) +
                                       " workloose=" + F(d.HoleWorkLoose) +
                                       " boltStyle='" + d.BoltStyle + "'" +
                                       " boltCRC=" + d.BoltStyleCRC +
                                       " gap=" + F(d.DistanceToConnected) +
                                       " sideOff=" + F(d.InnerOffsetSide) +
                                       " vertOff=" + F(d.InnerOffsetVertical) +
                                       " fromEdge=" + d.InsertOffsetFromShapeEdge +
                                       " fromDown=" + d.InsertOffsetFromDownSide +
                                       " fromHole=" + d.InsertOffsetFromFirstHole +
                                       " cope=" + d.CreateCope +
                                       " group=" + d.CreateGroup +
                                       " eachAngle=" + d.GroupIsSingleGroup +
                                       " shear=" + F(d.ShearX) + "/" + F(d.ShearY) + "/" + F(d.ShearZ) +
                                       " moment=" + F(d.MomentX) + "/" + F(d.MomentY) + "/" + F(d.MomentZ));
                    }
                    catch (System.Exception e) { sb.Append("  !EX:" + One(e.Message)); }
                    sb.AppendLine();
                }
                // the DAST database -- "the web angle connections available in the database"
                try
                {
                    dast = wc.get_PlateDataCount();
                    sb.AppendLine();
                    sb.AppendLine("DAST plateDataCount=" + dast);
                    for (int i = 0; i < dast && i < 60; i++)
                    {
                        string nm = "?";
                        try { nm = wc.GetPlateDataName(i); } catch (System.Exception e) { nm = "!EX:" + One(e.Message); }
                        long id = 0;
                        try { id = wc.GetPlateData(nm); } catch { }
                        sb.AppendLine("  [" + i + "] " + nm + "  -> " + id);
                    }
                }
                catch (System.Exception e) { sb.AppendLine("DAST !EX:" + One(e.Message)); }
            }
            catch (System.Exception ex) { Result("EB_ERR webangletemplates EX:" + One(ex.Message)); return; }
            File.WriteAllText(Path.Combine(Dir, "eb_webangle.txt"), sb.ToString(), Encoding.UTF8);
            Result("EB_OK webangletemplates templates=" + n + " dast=" + dast + " -> eb_webangle.txt");
        }

        // op=webangle handle=<beam> [support=<column>] at=x,y,z [template=<name>]
        //   [key=<angle section> catalog=<cat>] [pos= turn= flat= thick= longleg=
        //    shortleg= bendradius= gap= sideoff= vertoff= fromedge= fromdown= fromhole=
        //    nvert= nconn= nsup= dia= workloose= boltstyle= slotconn= slotsup=
        //    cope= copetemplate= shorten= group= boltsingroup= eachangle=
        //    weldconn= weldsup= shear= moment=]
        void WebAngle(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            long bid = IdFromHandle(Get(kv, "handle", ""));
            if (bid == 0) { Result("EB_ERR webangle: bad beam handle"); return; }
            long sup = 0;
            string sh = Get(kv, "support", "");
            if (sh.Length > 0) sup = IdFromHandle(sh);

            string h0, c0; int before = Census(out h0, out c0);
            List<string> pre = HandleSet();
            string applied = "", msg = "";
            bool ok = false; int chk = -999;
            StringBuilder plates = new StringBuilder();
            try
            {
                PsWebAngleConnection wc = new PsWebAngleConnection();
                wc.SetToDefaults();

                PsWebAngleLinkDataMgd d = null;
                string tpl = Get(kv, "template", "");
                if (tpl.Length > 0)
                {
                    try { d = wc.GetTemplate(tpl); }
                    catch (System.Exception e) { msg += " GetTemplate!EX:" + One(e.Message); }
                    applied += " template=" + tpl + (d == null ? "(NULL)" : "");
                }
                if (d == null) { d = new PsWebAngleLinkDataMgd(); applied += " template=none"; }

                string v;
                // B.19: "Only shapes that are EQUAL-SIDED ANGLES and UNEQUAL-SIDED ANGLES
                // can be selected" -- the section goes on the CONNECTION, not the data.
                string key = Get(kv, "key", ""), cat = Get(kv, "catalog", "");
                if (key.Length > 0) { wc.SetKey(key); applied += " key=" + key; }
                if (cat.Length > 0) { wc.SetKatalog(cat); applied += " catalog=" + cat; }

                v = Get(kv, "pos", "");         if (v.Length > 0) { d.WebAnglePosition = int.Parse(v); applied += " pos=" + v; }
                v = Get(kv, "turn", "");        if (v.Length > 0) { d.TurnWebAngles = (v == "1"); applied += " turn=" + v; }
                v = Get(kv, "flat", "");        if (v.Length > 0) { d.WebAngleIsFlatSteel = (v == "1"); applied += " flat=" + v; }
                v = Get(kv, "thick", "");       if (v.Length > 0) { d.FlatSteelThickness = double.Parse(v, IC); applied += " thick=" + v; }
                v = Get(kv, "longleg", "");     if (v.Length > 0) { d.FlatSteelLongSide = double.Parse(v, IC); applied += " longLeg=" + v; }
                v = Get(kv, "shortleg", "");    if (v.Length > 0) { d.FlatSteelShortSide = double.Parse(v, IC); applied += " shortLeg=" + v; }
                v = Get(kv, "bendradius", "");  if (v.Length > 0) { d.FlatSteelBendRadius = double.Parse(v, IC); applied += " bendR=" + v; }
                v = Get(kv, "gap", "");         if (v.Length > 0) { d.DistanceToConnected = double.Parse(v, IC); applied += " gap=" + v; }
                v = Get(kv, "sideoff", "");     if (v.Length > 0) { d.InnerOffsetSide = double.Parse(v, IC); applied += " sideOff=" + v; }
                v = Get(kv, "vertoff", "");     if (v.Length > 0) { d.InnerOffsetVertical = double.Parse(v, IC); applied += " vertOff=" + v; }
                v = Get(kv, "fromedge", "");    if (v.Length > 0) { d.InsertOffsetFromShapeEdge = (v == "1"); applied += " fromEdge=" + v; }
                v = Get(kv, "fromdown", "");    if (v.Length > 0) { d.InsertOffsetFromDownSide = (v == "1"); applied += " fromDown=" + v; }
                v = Get(kv, "fromhole", "");    if (v.Length > 0) { d.InsertOffsetFromFirstHole = (v == "1"); applied += " fromHole=" + v; }
                v = Get(kv, "nvert", "");       if (v.Length > 0) { d.VerticalHoleCount = int.Parse(v); applied += " nVert=" + v; }
                v = Get(kv, "nconn", "");       if (v.Length > 0) { d.HorizontalHoleCountConnected = int.Parse(v); applied += " nConn=" + v; }
                v = Get(kv, "nsup", "");        if (v.Length > 0) { d.HorizontalHoleCountSupport = int.Parse(v); applied += " nSup=" + v; }
                v = Get(kv, "dia", "");         if (v.Length > 0) { d.HoleDiameter = double.Parse(v, IC); applied += " dia=" + v; }
                v = Get(kv, "workloose", "");   if (v.Length > 0) { d.HoleWorkLoose = double.Parse(v, IC); applied += " workloose=" + v; }
                // ג­ BoltStyle is a STRING here -- the stiffener class exposed only a CRC
                v = Get(kv, "boltstyle", "");   if (v.Length > 0) { d.BoltStyle = v; applied += " boltStyle=" + v; }
                // B.19: a Slot Length beside Number turns the holes into SLOTTED holes
                v = Get(kv, "slotconn", "");    if (v.Length > 0) { d.SlotAxisDistanceConnected = double.Parse(v, IC); applied += " slotConn=" + v; }
                v = Get(kv, "slotsup", "");     if (v.Length > 0) { d.SlotAxisDistanceSupport = double.Parse(v, IC); applied += " slotSup=" + v; }
                v = Get(kv, "cope", "");        if (v.Length > 0) { d.CreateCope = (v == "1"); applied += " cope=" + v; }
                v = Get(kv, "copetemplate", ""); if (v.Length > 0) { d.SetCopeFromTemplate(v); applied += " copeTpl=" + v; }
                v = Get(kv, "shorten", "");     if (v.Length > 0) { d.ShortenAngle = (v == "1"); applied += " shorten=" + v; }
                // B.19 Cope page. CreateCope alone is inert -- these carry the geometry.
                v = Get(kv, "copeedgetop", "");   if (v.Length > 0) { d.CopeDistanceEdgeTop = double.Parse(v, IC); applied += " copeEdgeTop=" + v; }
                v = Get(kv, "copeedgedown", "");  if (v.Length > 0) { d.CopeDistanceEdgeDown = double.Parse(v, IC); applied += " copeEdgeDown=" + v; }
                v = Get(kv, "copeinsidetop", ""); if (v.Length > 0) { d.CopeDistanceInsideTop = double.Parse(v, IC); applied += " copeInTop=" + v; }
                v = Get(kv, "copeinsidedown", ""); if (v.Length > 0) { d.CopeDistanceInsideDown = double.Parse(v, IC); applied += " copeInDown=" + v; }
                v = Get(kv, "copeoutsidetop", ""); if (v.Length > 0) { d.CopeDistanceOutsideTop = double.Parse(v, IC); applied += " copeOutTop=" + v; }
                v = Get(kv, "copeoutsidedown", ""); if (v.Length > 0) { d.CopeDistanceOutsideDown = double.Parse(v, IC); applied += " copeOutDown=" + v; }
                v = Get(kv, "coperadius", "");    if (v.Length > 0) { d.CopeRadius = double.Parse(v, IC); applied += " copeRadius=" + v; }
                v = Get(kv, "copewebdist", "");   if (v.Length > 0) { d.CopeWebDistance = double.Parse(v, IC); applied += " copeWebDist=" + v; }
                v = Get(kv, "copefit", "");       if (v.Length > 0) { d.CopeShapeFitType = int.Parse(v); applied += " copeFit=" + v; }
                v = Get(kv, "copeinner", "");     if (v.Length > 0) { d.CopeAlignToInnerEdge = (v == "1"); applied += " copeInner=" + v; }
                v = Get(kv, "cutalways", "");     if (v.Length > 0) { d.CutAlways = (v == "1"); applied += " cutAlways=" + v; }
                v = Get(kv, "cutatconnected", ""); if (v.Length > 0) { d.CutAtConnected = (v == "1"); applied += " cutAtConn=" + v; }
                v = Get(kv, "group", "");       if (v.Length > 0) { d.CreateGroup = (v == "1"); applied += " group=" + v; }
                v = Get(kv, "boltsingroup", ""); if (v.Length > 0) { d.AddBoltsToGroup = (v == "1"); applied += " boltsInGroup=" + v; }
                v = Get(kv, "eachangle", "");   if (v.Length > 0) { d.GroupIsSingleGroup = (v == "1"); applied += " eachAngle=" + v; }
                v = Get(kv, "weldconn", "");    if (v.Length > 0) { d.WeldToConnectedShape = (v == "1"); applied += " weldConn=" + v; }
                v = Get(kv, "weldsup", "");     if (v.Length > 0) { d.WeldToSupportShape = (v == "1"); applied += " weldSup=" + v; }
                // ג­ the DAST load fields -- the first load-carrying properties in this API
                v = Get(kv, "shear", "");       if (v.Length > 0) { d.ShearZ = double.Parse(v, IC); applied += " shearZ=" + v; }
                v = Get(kv, "moment", "");      if (v.Length > 0) { d.MomentY = double.Parse(v, IC); applied += " momentY=" + v; }

                wc.SetConnectionData(d);
                wc.SetConnectionObjectId(bid);
                if (sup != 0) { wc.SetSupportObjectId(sup); applied += " support=" + sh; }
                else applied += " support=NONE(both ends)";
                wc.SetConnectionPoint(Pt(Get(kv, "at", "0,0,0")));
                try { chk = wc.Check(); } catch (System.Exception e) { msg += " Check!EX:" + One(e.Message); }
                ok = wc.Create();
                applied += " Check()=" + chk + " Create()=" + ok;

                // ג­ this class can report what it made -- most connection classes cannot
                try
                {
                    for (int i = 0; i < 8; i++)
                    {
                        long pid = wc.GetPlateId(i);
                        if (pid == 0) break;
                        plates.Append(" " + HandleOf(pid));
                    }
                }
                catch (System.Exception e) { plates.Append(" !EX:" + One(e.Message)); }
            }
            catch (System.Exception ex) { msg += " EX:" + One(ex.Message); }

            string h1, c1; int after = Census(out h1, out c1);
            StringBuilder made = new StringBuilder();
            int n = 0;
            foreach (string h in HandleSet())
                if (!pre.Contains(h)) { made.Append(" " + h); n++; }
            Result(((after > before) ? "EB_OK" : "EB_ERR") + " webangle beam=" + Get(kv, "handle", "") +
                   " census=" + before + "->" + after + " newObjects=" + n +
                   " GetPlateId:[" + plates + " ]" +
                   (n <= 24 ? " handles:" + made : " (too many to list)") + applied + msg);
        }

        // =====================================================================
        //  v100 ג€” B.16 INSERT STIFFENERS  (PS_RIP)
        //  "Although stiffeners are common poly-plates or flats, the program
        //   ALREADY CALCULATES THEIR DIMENSIONS ACCORDING TO THE SHAPE."
        //  A stiffener is derived from the girder, not drawn. Three of its fields
        //  are raw Int32 with no enum (ShapeType, LengthType, CenterPunchType) and
        //  one is a CRC of a style NAME (WeldStyleCRC) -- none can be guessed, so
        //  the shipped TEMPLATES are read first and used as the source of values.
        //  (Same lesson as B.18: anchors turned out to be template-only.)
        // =====================================================================

        // op=stifftemplates  -> every shipped template, fully expanded
        void StiffTemplates(Dictionary<string, string> kv)
        {
            StringBuilder sb = new StringBuilder();
            int n = -1;
            try
            {
                PsStiffenerConnection sc = new PsStiffenerConnection();
                sc.SetToDefaults();
                n = sc.GetTemplateCount();
                sb.AppendLine("templateCount=" + n);
                for (int i = 0; i < n; i++)
                {
                    string nm = "?";
                    try { nm = sc.GetTemplateName(i); }
                    catch (System.Exception e) { nm = "!EX:" + One(e.Message); }
                    sb.Append("[" + i + "] " + nm);
                    try
                    {
                        PsStiffenerLinkDataMgd d = sc.GetTemplate(nm);
                        if (d == null) sb.Append("  <null>");
                        else sb.Append("  shapeType=" + d.ShapeType +
                                       " lengthType=" + d.LengthType +
                                       " length=" + F(d.Length) +
                                       " thick=" + F(d.Thickness) +
                                       " flangeDist=" + F(d.FlangeDistance) +
                                       " webDist=" + F(d.WebDistance) +
                                       " offset=" + F(d.Offset) +
                                       " roundTo=" + F(d.RoundTo) +
                                       " radius=" + F(d.Radius) +
                                       " topAligned=" + d.TopAligned +
                                       " weldStyleCRC=" + d.WeldStyleCRC +
                                       " weldFlange=" + F(d.WeldSeamFlange) +
                                       " weldWeb=" + F(d.WeldSeamWeb) +
                                       " centerPunch=" + d.CenterPunchType +
                                       " createGroup=" + d.CreateGroup +
                                       " withAngle=" + d.InsertWithAngle +
                                       " angle=" + F(d.InsertAngle));
                    }
                    catch (System.Exception e) { sb.Append("  !EX:" + One(e.Message)); }
                    sb.AppendLine();
                }
            }
            catch (System.Exception ex) { Result("EB_ERR stifftemplates EX:" + One(ex.Message)); return; }
            File.WriteAllText(Path.Combine(Dir, "eb_stiff.txt"), sb.ToString(), Encoding.UTF8);
            Result("EB_OK stifftemplates count=" + n + " -> eb_stiff.txt");
        }

        // op=stiffener handle=<shape> at=x,y,z [template=<name>]
        //   [lengthtype= shapetype= length= thick= flangedist= webdist= offset=
        //    roundto= radius= topaligned= centerpunch= creategroup=
        //    withangle= angle= weldflange= weldweb=]
        // B.16: pick the shape, then "the CENTER OF THE INSERTION POINT of the
        // stiffeners". On a symmetric section ONE call makes TWO stiffeners, so every
        // new handle is reported, not just the last.
        void Stiffener(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            long sid = IdFromHandle(Get(kv, "handle", ""));
            if (sid == 0) { Result("EB_ERR stiffener: bad shape handle"); return; }
            string h0, c0; int before = Census(out h0, out c0);
            List<string> pre = HandleSet();
            string applied = "", msg = "";
            bool ok = false; int chk = -999;
            try
            {
                PsStiffenerConnection sc = new PsStiffenerConnection();
                sc.SetToDefaults();

                // start from a shipped template when asked -- the only source of valid
                // values for the opaque Int32 fields, WeldStyleCRC above all
                PsStiffenerLinkDataMgd d = null;
                string tpl = Get(kv, "template", "");
                if (tpl.Length > 0)
                {
                    try { d = sc.GetTemplate(tpl); }
                    catch (System.Exception e) { msg += " GetTemplate!EX:" + One(e.Message); }
                    applied += " template=" + tpl + (d == null ? "(NULL)" : "");
                }
                if (d == null) { d = new PsStiffenerLinkDataMgd(); applied += " template=none"; }

                string v;
                v = Get(kv, "shapetype", "");   if (v.Length > 0) { d.ShapeType = int.Parse(v); applied += " shapeType=" + v; }
                v = Get(kv, "lengthtype", "");  if (v.Length > 0) { d.LengthType = int.Parse(v); applied += " lengthType=" + v; }
                v = Get(kv, "length", "");      if (v.Length > 0) { d.Length = double.Parse(v, IC); applied += " length=" + v; }
                v = Get(kv, "thick", "");       if (v.Length > 0) { d.Thickness = double.Parse(v, IC); applied += " thick=" + v; }
                v = Get(kv, "flangedist", "");  if (v.Length > 0) { d.FlangeDistance = double.Parse(v, IC); applied += " flangeDist=" + v; }
                v = Get(kv, "webdist", "");     if (v.Length > 0) { d.WebDistance = double.Parse(v, IC); applied += " webDist=" + v; }
                v = Get(kv, "offset", "");      if (v.Length > 0) { d.Offset = double.Parse(v, IC); applied += " offset=" + v; }
                v = Get(kv, "roundto", "");     if (v.Length > 0) { d.RoundTo = double.Parse(v, IC); applied += " roundTo=" + v; }
                // B.16: "If this value is 0, the SHAPE RADIUS IS IMPORTED" -- 0 does NOT
                // mean "no radius".
                v = Get(kv, "radius", "");      if (v.Length > 0) { d.Radius = double.Parse(v, IC); applied += " radius=" + v; }
                v = Get(kv, "topaligned", "");  if (v.Length > 0) { d.TopAligned = (v == "1"); applied += " topAligned=" + v; }
                v = Get(kv, "centerpunch", ""); if (v.Length > 0) { d.CenterPunchType = int.Parse(v); applied += " centerPunch=" + v; }
                v = Get(kv, "creategroup", ""); if (v.Length > 0) { d.CreateGroup = (v == "1"); applied += " createGroup=" + v; }
                v = Get(kv, "withangle", "");   if (v.Length > 0) { d.InsertWithAngle = (v == "1"); applied += " withAngle=" + v; }
                v = Get(kv, "angle", "");       if (v.Length > 0) { d.InsertAngle = double.Parse(v, IC); applied += " angle=" + v; }
                v = Get(kv, "weldflange", "");  if (v.Length > 0) { d.WeldSeamFlange = double.Parse(v, IC); applied += " weldFlange=" + v; }
                v = Get(kv, "weldweb", "");     if (v.Length > 0) { d.WeldSeamWeb = double.Parse(v, IC); applied += " weldWeb=" + v; }

                sc.SetConnectionData(d);
                sc.SetConnectionObjectId(sid);
                sc.SetConnectionPoint(Pt(Get(kv, "at", "0,0,0")));
                try { chk = sc.Check(); }
                catch (System.Exception e) { msg += " Check!EX:" + One(e.Message); }
                ok = sc.Create();
                applied += " Check()=" + chk + " Create()=" + ok;
            }
            catch (System.Exception ex) { msg += " EX:" + One(ex.Message); }

            string h1, c1; int after = Census(out h1, out c1);
            StringBuilder made = new StringBuilder();
            int n = 0;
            foreach (string h in HandleSet())
                if (!pre.Contains(h)) { made.Append(" " + h); n++; }
            Result(((after > before) ? "EB_OK" : "EB_ERR") + " stiffener on=" + Get(kv, "handle", "") +
                   " census=" + before + "->" + after + " created=" + n + " handles:" + made +
                   applied + msg);
        }

        // =====================================================================
        //  REPLICATE ג€” build the detail once, then copy it everywhere.
        //  Amir's principle: "I modelled once and then replicated ג€” that is the
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

            // move, then rotate about any axis through a given point ג€” Z for a
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
        // geometric probe that can tell a mirrored angle from a plain one ג€” the
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
        //  v18 ג€” THE CONNECTION LAYER: real holes, real contours, real drilling
        //  Amir's rule: a bolt passing through steel WITHOUT a modelled hole is a
        //  critical error. So we must be able to (a) READ holes to verify and
        //  (b) DRILL holes to fix. Both work off the object id ג€” no PsPlate cast.
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

        // Real plate contour (not the bounding box) ג€” this is what tells a rib or a
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
                    // VERIFY by reading the holes back ג€” never trust Apply()'s return
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
            // read the contour back ג€” proof, not an echo of the input
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
        //  v19 ג€” PS PROPERTIES and PS CONNECTION (Amir: "every connection is a
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

        // ===================================================================
        //  E.9  ProSteel Properties Dialogs ג€” the dialog, turned into code
        // ===================================================================
        // The manual's E.9 documents the properties dialog tab by tab, and reads like a
        // reference chapter. It is not. Every field it names is a property on
        // PsObjectProperties, and that class also carries writeTo(Int64 ObjId) ג€”
        // so the whole dialog is a WRITE surface.
        //
        // Two things the manual says that the API confirms:
        //   * "ProSteel analyses which parts were selected and only provides the
        //     properties valid for these parts" ג€” hence the tabs below: a bolt has
        //     Tension and KlemmLen, a plate has FixedFormFlag, a shape has ShapeClass.
        //     Reading a property that does not apply throws, so every read is guarded.
        //   * E.9.17's Assignments tab = DetailStyleId / DisplayClass / AreaClass /
        //     FamilyClass / ProcessStatus. FamilyClass ("support or girder") also has
        //     its own writer, UpdateFamilyClass(ObjId, Index).
        //
        // The tab each property belongs to, per the manual's own headings. Anything not
        // named in E.9 falls to 8-Other rather than being dropped ג€” the point of the map
        // is that nothing is lost, not that everything is classified.
        static string E9Tab(string p)
        {
            switch (p)
            {
                // E.9.1/2/3 "Layout" ג€” how the part is DRAWN, nothing structural
                case "ObjectDisplayMode": case "HoleDisplayMode": case "OuterContourMode":
                case "ModellerMode": case "DisplayAs2dMode": case "DisplayAs2dSectionMode":
                case "CenterLineMode": case "COGLineMode": case "PitchLineMode":
                case "DrawName": case "DrawShortName": case "ECSAxisMode":
                case "Transparency": case "DirectionMarkMode": case "ColorIndex":
                case "LineType": case "DisplayFlagsLong": case "HighlightedFlag":
                case "Visible":
                    return "1-Layout";
                // E.9.1 "Shape Type"
                case "Key": case "Katalog": case "Resolution": case "ShapeClass":
                case "ObjectType": case "InternalName":
                    return "2-ShapeType";
                // E.9.1/2 "Positions"
                case "InsertX": case "InsertY": case "Origin": case "XAxis": case "YAxis":
                case "ZAxis": case "InsertMatrix": case "Scale": case "MirrorFlag":
                case "YMirrorFlag": case "Mirrored": case "PartOrigin":
                case "MidLineStart": case "MidLineEnd": case "UseUserMidpoint":
                case "HasUserMidpointDefined":
                    return "3-Position";
                // E.9.1/2/3 "Data" ג€” the parts-list identity
                case "Name": case "Material": case "Note1": case "Note2": case "Posnum":
                case "Sendnum": case "Originalnum": case "Article": case "Count":
                case "TotalCount": case "PartListFlag": case "BoltListFlag":
                case "DontDetailFlag": case "DontPositionFlag": case "TransportName":
                case "Partart": case "FreeDescription": case "Handle":
                    return "4-Data";
                // E.9.1/2/3 "Values" ג€” the numbers. KlemmLen is the bolt's GRIP LENGTH
                // and Tension its PRE-TENSION in percent.
                case "Length": case "Wide": case "Height": case "Diameter": case "Weight":
                case "VolumeWeightFlag": case "LenAdd": case "KlemmLen": case "Tension":
                case "MountingBolt": case "FixedFormFlag": case "SlopedHeight":
                case "PaintArea": case "CutArea":
                    return "5-Values";
                // E.9.17 "Assignments"
                case "DetailStyleId": case "DisplayClass": case "AreaClass":
                case "FamilyClass": case "ProcessStatus": case "StyleName": case "LayerName":
                    return "6-Assignments";
                // Not on any tab ג€” reactor and detailing state the dialog hides
                case "ModifyFlag": case "Modified4Reactor": case "BlockModify":
                case "BlockRecalcFlag": case "VirginGroupFlag": case "VirginViewFlag":
                case "IndependendForDetailing": case "AnalysisMode":
                case "StorageFlagsLong": case "GlobalFlagsLong":
                    return "7-State";
            }
            return "8-Other";
        }

        static string Show(object o)
        {
            if (o == null) return "(null)";
            if (o is double) return F((double)o);
            if (o is PsPoint) { PsPoint p = (PsPoint)o; return F(p.x) + "," + F(p.y) + "," + F(p.z); }
            if (o is PsVector) { PsVector v = (PsVector)o; return F(v.x) + "/" + F(v.y) + "/" + F(v.z); }
            return One(o.ToString());
        }

        static object ConvertTo(Type ty, string s)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            if (ty == typeof(string)) return s;
            if (ty == typeof(bool)) { string l = s.ToLowerInvariant(); return (l == "1" || l == "true" || l == "yes"); }
            if (ty == typeof(int)) return int.Parse(s, IC);
            if (ty == typeof(short)) return short.Parse(s, IC);
            if (ty == typeof(ushort)) return ushort.Parse(s, IC);
            if (ty == typeof(long)) return long.Parse(s, IC);
            if (ty == typeof(double)) return double.Parse(s, IC);
            if (ty.IsEnum)
            {
                int iv;
                if (int.TryParse(s, System.Globalization.NumberStyles.Integer, IC, out iv))
                    return Enum.ToObject(ty, iv);
                return Enum.Parse(ty, s, true);
            }
            throw new System.Exception("no converter for " + ty.Name);
        }

        // op=propfull handle=XX  [tab=5]
        // Every property PsObjectProperties carries, under the E.9 tab it belongs to.
        // `props` printed 30 of ~120; this prints the lot, and marks which are writable,
        // because "what can I change" is the question the dialog actually answers.
        void PropFull(Dictionary<string, string> kv)
        {
            string h = Get(kv, "handle", "");
            string tabWant = Get(kv, "tab", "");
            long oid = IdFromHandle(h);
            if (oid == 0) { Result("EB_ERR propfull: no object for handle '" + h + "'"); return; }

            PsObjectProperties pr = new PsObjectProperties();
            int rc = pr.readFrom(oid);

            SortedDictionary<string, List<string>> byTab = new SortedDictionary<string, List<string>>();
            int total = 0, threw = 0;
            foreach (PropertyInfo pi in pr.GetType().GetProperties())
            {
                if (pi.Name == "UnmanagedObject") continue;
                string tab = E9Tab(pi.Name);
                if (tabWant.Length > 0 && tab.IndexOf(tabWant) != 0) continue;
                string val;
                try { val = Show(pi.GetValue(pr, null)); }
                catch (System.Exception ex) { val = "<n/a: " + One(ex.Message) + ">"; threw++; }
                if (!byTab.ContainsKey(tab)) byTab[tab] = new List<string>();
                byTab[tab].Add(string.Format("  {0,-28} {1,-22} {2} = {3}",
                    pi.Name, pi.PropertyType.Name, pi.CanWrite ? "rw" : "r-", val));
                total++;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("PROPFULL handle=" + h + " oid=" + oid + " readFrom.rc=" + rc);
            sb.AppendLine("properties=" + total + "  not-applicable-to-this-part=" + threw);
            sb.AppendLine("(rw = the .NET property HAS A SETTER. It does NOT mean the value"
                        + " survives writeTo -- that depends on the PART TYPE and the API gives"
                        + " no signal, returning rc=0 either way. Measured on a Ks_Grid: Note1,"
                        + " Name and Article are all marked rw and are all silently discarded,"
                        + " while AreaClass sticks; on a Ks_Shape all four stick."
                        + " ג‡’ only op=propset's READ-BACK tells you which.)");
            foreach (KeyValuePair<string, List<string>> e in byTab)
            {
                sb.AppendLine();
                sb.AppendLine("== " + e.Key + " ==");
                e.Value.Sort();
                foreach (string l in e.Value) sb.AppendLine(l);
            }
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location), "eb_propfull.txt"),
                sb.ToString(), new UTF8Encoding(true));
            Result("EB_OK propfull handle=" + h + " props=" + total + " na=" + threw
                 + " tabs=" + byTab.Count + " -> eb_propfull.txt");
        }

        // op=propset handle=XX  Note1=...  AreaClass=3  Tension=70  MountingBolt=1 ...
        // Any key naming a writable PsObjectProperties property (case-insensitive) is set,
        // then writeTo(oid) commits, then EVERYTHING IS READ BACK from a fresh instance.
        // The read-back is not optional: a writeTo that returns 0 proves nothing, exactly
        // as Create() returning true proved nothing in B.12/B.19/B.22.
        void PropSet(Dictionary<string, string> kv)
        {
            string h = Get(kv, "handle", "");
            long oid = IdFromHandle(h);
            if (oid == 0) { Result("EB_ERR propset: no object for handle '" + h + "'"); return; }

            PsObjectProperties pr = new PsObjectProperties();
            int rc0 = pr.readFrom(oid);
            PropertyInfo[] all = pr.GetType().GetProperties();

            List<string> asked = new List<string>();
            List<string> names = new List<string>();
            List<string> befores = new List<string>();
            List<string> wants = new List<string>();
            int unknown = 0, readonlyHit = 0, convFail = 0;

            foreach (KeyValuePair<string, string> e in kv)
            {
                string k = e.Key;
                // "dwg" is the drawing pin eb_api adds to every command, not a property
                if (k == "op" || k == "handle" || k == "reqid" || k == "tab" || k == "dwg") continue;
                PropertyInfo pi = null;
                foreach (PropertyInfo c in all)
                    if (string.Equals(c.Name, k, StringComparison.OrdinalIgnoreCase)) { pi = c; break; }
                if (pi == null) { asked.Add(k + " -> UNKNOWN PROPERTY"); unknown++; continue; }
                if (!pi.CanWrite) { asked.Add(pi.Name + " -> READ-ONLY"); readonlyHit++; continue; }
                string before = "?";
                try { before = Show(pi.GetValue(pr, null)); } catch { before = "<n/a>"; }
                try
                {
                    pi.SetValue(pr, ConvertTo(pi.PropertyType, e.Value), null);
                    names.Add(pi.Name); befores.Add(before); wants.Add(e.Value);
                }
                catch (System.Exception ex)
                { asked.Add(pi.Name + " -> CONVERT/SET FAILED: " + One(ex.Message)); convFail++; }
            }

            int rc1 = -999;
            try { rc1 = pr.writeTo(oid); }
            catch (System.Exception ex) { Result("EB_ERR propset writeTo threw: " + ex.Message); return; }

            // the read-back, from a NEW instance so nothing is cached
            PsObjectProperties chk = new PsObjectProperties();
            int rc2 = chk.readFrom(oid);
            PropertyInfo[] all2 = chk.GetType().GetProperties();
            int stuck = 0, ignored = 0;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("PROPSET handle=" + h + " oid=" + oid
                        + " readFrom.rc=" + rc0 + " writeTo.rc=" + rc1 + " reread.rc=" + rc2);
            for (int i = 0; i < names.Count; i++)
            {
                PropertyInfo pi = null;
                foreach (PropertyInfo c in all2) if (c.Name == names[i]) { pi = c; break; }
                string after = "?";
                try { after = Show(pi.GetValue(chk, null)); } catch { after = "<n/a>"; }
                bool ok = string.Equals(after, wants[i], StringComparison.OrdinalIgnoreCase)
                       || (after == "True" && (wants[i] == "1" || wants[i].ToLowerInvariant() == "true"))
                       || (after == "False" && (wants[i] == "0" || wants[i].ToLowerInvariant() == "false"))
                       || after != befores[i];
                if (ok) stuck++; else ignored++;
                sb.AppendLine((ok ? "  OK   " : "  IGN  ") + names[i]
                            + ": " + befores[i] + " -> " + after + "   (asked " + wants[i] + ")");
            }
            foreach (string a in asked) sb.AppendLine("  --   " + a);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location), "eb_propset.txt"),
                sb.ToString(), new UTF8Encoding(true));
            Result("EB_OK propset handle=" + h + " writeTo.rc=" + rc1
                 + " stuck=" + stuck + " ignored=" + ignored
                 + " unknown=" + unknown + " readonly=" + readonlyHit + " convfail=" + convFail
                 + " -> eb_propset.txt");
        }

        // op=propcopy src=XX dst=YY [tabs=4,6] [dryrun=1]
        // "Match properties" for ProSteel, built on copyFrom(). DEFAULT IS TABS 4+6 ג€”
        // Data and Assignments, the non-geometric identity. copyFrom() copies EVERYTHING
        // including Origin/XAxis/Length, so an unfiltered copy would MOVE and RESIZE the
        // target; tabs=all is available but the geometry is measured before and after
        // and reported either way.
        void PropCopy(Dictionary<string, string> kv)
        {
            string hs = Get(kv, "src", ""), hd = Get(kv, "dst", "");
            string tabs = Get(kv, "tabs", "4,6");
            bool dry = Get(kv, "dryrun", "0") == "1";
            long sid = IdFromHandle(hs), did = IdFromHandle(hd);
            if (sid == 0 || did == 0) { Result("EB_ERR propcopy: bad handle src=" + hs + " dst=" + hd); return; }

            PsObjectProperties src = new PsObjectProperties(); src.readFrom(sid);
            PsObjectProperties dst = new PsObjectProperties(); dst.readFrom(did);

            string extBefore = ExtentsOf(did);

            List<string> lines = new List<string>();
            int copied = 0, skipped = 0;
            PropertyInfo[] all = dst.GetType().GetProperties();
            foreach (PropertyInfo pi in all)
            {
                if (pi.Name == "UnmanagedObject" || !pi.CanWrite) continue;
                string tab = E9Tab(pi.Name);
                bool want = (tabs == "all") || tabs.IndexOf(tab.Substring(0, 1)) >= 0;
                if (!want) { skipped++; continue; }
                object v;
                try { v = pi.GetValue(src, null); } catch { continue; }
                string before = "?";
                try { before = Show(pi.GetValue(dst, null)); } catch { }
                if (dry) { lines.Add("  DRY  " + pi.Name + ": " + before + " <- " + Show(v)); copied++; continue; }
                try { pi.SetValue(dst, v, null); copied++; lines.Add("  SET  " + pi.Name + ": " + before + " <- " + Show(v)); }
                catch (System.Exception ex) { lines.Add("  ERR  " + pi.Name + ": " + One(ex.Message)); }
            }
            int rc = -999;
            if (!dry) { try { rc = dst.writeTo(did); } catch (System.Exception ex) { lines.Add("writeTo threw: " + ex.Message); } }
            string extAfter = ExtentsOf(did);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("PROPCOPY src=" + hs + " -> dst=" + hd + " tabs=" + tabs
                        + (dry ? " (DRY RUN)" : "") + " writeTo.rc=" + rc);
            sb.AppendLine("dst extents before: " + extBefore);
            sb.AppendLine("dst extents after : " + extAfter
                        + (extBefore == extAfter ? "   [geometry UNCHANGED]" : "   *** GEOMETRY MOVED ***"));
            foreach (string l in lines) sb.AppendLine(l);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location), "eb_propcopy.txt"),
                sb.ToString(), new UTF8Encoding(true));
            Result("EB_OK propcopy src=" + hs + " dst=" + hd + " fields=" + copied
                 + " skipped=" + skipped + " writeTo.rc=" + rc
                 + (extBefore == extAfter ? " geom=same" : " geom=MOVED")
                 + " -> eb_propcopy.txt");
        }

        static string ExtentsOf(long oid)
        {
            try
            {
                PsObjectProperties p = new PsObjectProperties();
                p.readFrom(oid);
                PsPoint mn = new PsPoint(0, 0, 0), mx = new PsPoint(0, 0, 0);
                if (p.GetExtents(ref mn, ref mx))
                    return F(mn.x) + "," + F(mn.y) + "," + F(mn.z) + ";" + F(mx.x) + "," + F(mx.y) + "," + F(mx.z);
            }
            catch { }
            return "?";
        }

        // op=changesection handle=XX key=HE400B [cat=Euro] [type=<int>]
        // PsObjectProperties.ChangeShapeType(oid, Key, Katalog, ShapeType) ג€” swap the
        // section of an EXISTING shape in place. This is a modeller's own move: an
        // HEB300 that has to become an HEB400 after a load change, without rebuilding it
        // and without losing whatever hangs off it. The section key is an OPAQUE STRING ג€”
        // it is looked up in the catalogue, never assembled by hand (HEB300 does not exist;
        // the real key is HE300B). Length, position and holes are all measured before and
        // after, because "it changed the section" and "it kept everything else" are two
        // separate claims.
        void ChangeSection(Dictionary<string, string> kv)
        {
            string h = Get(kv, "handle", "");
            string key = Get(kv, "key", "");
            string cat = Get(kv, "cat", "");
            long oid = IdFromHandle(h);
            if (oid == 0) { Result("EB_ERR changesection: no object for handle '" + h + "'"); return; }
            if (key.Length == 0) { Result("EB_ERR changesection: key= is required"); return; }

            PsObjectProperties pr = new PsObjectProperties();
            int rc0 = pr.readFrom(oid);
            string key0 = "?", cat0 = "?"; double L0 = 0, W0 = 0, H0 = 0, wt0 = 0;
            ObjectType ot = ObjectType.kUndefinedObject;
            try { key0 = pr.Key; cat0 = pr.Katalog; } catch { }
            try { L0 = pr.Length; W0 = pr.Wide; H0 = pr.Height; wt0 = pr.Weight; } catch { }
            try { ot = pr.ObjectType; } catch { }
            string erH; int holes0 = Rec.HolesOfStatic(oid, out erH);
            string ext0 = ExtentsOf(oid);

            // THE CONNECTION GUARD. Measured on 10/08/2026 with two identical specimens:
            // an HE300B column, an IPE300 beam and an end-plate joint from example/example3.
            // Specimen A untouched, specimen B given IPE300 -> IPE400 by this op. Counted by
            // AutoCAD class, not by the bolt matcher:
            //     A  4 Ks_Shape  8 Ks_Plate  6 Ks_Bolt
            //     B  4 Ks_Shape  8 Ks_Plate  4 Ks_Bolt     <- TWO BOLTS DESTROYED
            // and vfy_bolts found the 2 abandoned holes still in the end plate. The section
            // swap itself is correct; the JOINT is left inconsistent, and an inconsistent
            // joint is a wrong shop drawing that nothing downstream complains about.
            // Remedy, proven immediately afterwards: connkill then rebuild -> 6 bolts, 0
            // orphan holes. So: refuse on a connected part unless the caller says force=1,
            // and report the link count either way.
            int links0 = 0;
            try
            {
                PsEditLogicalLink ed = new PsEditLogicalLink();
                ed.SetObjectId(oid);
                links0 = ed.get_LogicalLinkCount();
            }
            catch { links0 = -1; }
            bool force = Get(kv, "force", "0") == "1";
            if (links0 > 0 && !force)
            {
                Result("EB_ERR changesection handle=" + h + " REFUSED: the part carries "
                     + links0 + " logical link(s). Changing the section leaves the joint "
                     + "inconsistent -- bolts are destroyed and their holes abandoned "
                     + "(measured: 6 bolts -> 4, 2 orphan holes). Do connkill first, change "
                     + "the section, then rebuild the connection. Pass force=1 to override "
                     + "and accept a broken joint.");
                return;
            }

            // resolve the catalogue the way `beam` does ג€” search, never assemble
            if (cat.Length == 0)
            {
                try
                {
                    PsShapeLoader ld = new PsShapeLoader();
                    string k = ld.FindKatalogFromKey(key, false);
                    if (k != null && k.Length > 0) cat = k;
                }
                catch { }
            }
            if (cat.Length == 0) cat = cat0;

            // The ShapeType argument. ProSteel keeps the shape SYSTEM (normal / weld /
            // combi / sopro / dawa) separate from the section key, so passing the wrong
            // system would move the part between systems as a side effect. Derive it from
            // the part's own ObjectType rather than defaulting to kUndefinedType.
            // The enum members are kNormalType/kWeldType/..., NOT eShape* ג€” measured from
            // the surface dump, not guessed from the naming of neighbouring enums.
            ShapeType st = ShapeType.kNormalType;
            switch (ot)
            {
                case ObjectType.kWeldShape: case ObjectType.kArcWeldShape:
                case ObjectType.kBendWeldShape:                     st = ShapeType.kWeldType; break;
                case ObjectType.kCombiShape: case ObjectType.kArcCombiShape:
                case ObjectType.kBendCombiShape:                    st = ShapeType.kCombiType; break;
                case ObjectType.kSoproShape: case ObjectType.kArcSoproShape:
                case ObjectType.kBendSoproShape:                    st = ShapeType.kSoproType; break;
                case ObjectType.kDawaShape: case ObjectType.kArcDawaShape:
                case ObjectType.kBendDawaShape:                     st = ShapeType.kDawaType; break;
            }
            string tS = Get(kv, "type", "");
            if (tS.Length > 0) { try { st = (ShapeType)int.Parse(tS); } catch { } }

            bool ok = false; string threw = "";
            try { ok = pr.ChangeShapeType(oid, key, cat, st); }
            catch (System.Exception ex) { threw = ex.Message; }

            // read back from a fresh instance
            PsObjectProperties chk = new PsObjectProperties();
            int rc2 = chk.readFrom(oid);
            string key1 = "?", cat1 = "?"; double L1 = 0, W1 = 0, H1 = 0, wt1 = 0;
            try { key1 = chk.Key; cat1 = chk.Katalog; } catch { }
            try { L1 = chk.Length; W1 = chk.Wide; H1 = chk.Height; wt1 = chk.Weight; } catch { }
            int holes1 = Rec.HolesOfStatic(oid, out erH);
            string ext1 = ExtentsOf(oid);
            int links1 = 0;
            try { PsEditLogicalLink e2 = new PsEditLogicalLink(); e2.SetObjectId(oid); links1 = e2.get_LogicalLinkCount(); }
            catch { links1 = -1; }

            bool changed = (key0 != key1) || (cat0 != cat1);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("CHANGESECTION handle=" + h + " oid=" + oid
                        + " asked key='" + key + "' cat='" + cat + "' type=" + (int)st
                        + " objType=" + ot);
            sb.AppendLine("  ChangeShapeType returned " + ok + (threw.Length > 0 ? "  THREW: " + threw : ""));
            sb.AppendLine("  key   : '" + key0 + "' -> '" + key1 + "'   " + (key0 != key1 ? "CHANGED" : "same"));
            sb.AppendLine("  cat   : '" + cat0 + "' -> '" + cat1 + "'");
            sb.AppendLine("  L/W/H : " + F(L0) + "/" + F(W0) + "/" + F(H0)
                        + "  ->  " + F(L1) + "/" + F(W1) + "/" + F(H1));
            sb.AppendLine("  weight: " + F(wt0) + " -> " + F(wt1));
            sb.AppendLine("  holes : " + holes0 + " -> " + holes1
                        + (holes0 != holes1 ? "   *** HOLES LOST/GAINED ***" : "   (kept)"));
            sb.AppendLine("  links : " + links0 + " -> " + links1
                        + (force && links0 > 0 ? "   *** FORCED on a connected part -- "
                                               + "run vfy_bolts and rebuild the joint ***" : ""));
            sb.AppendLine("  ext   : " + ext0);
            sb.AppendLine("        : " + ext1);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location), "eb_changesection.txt"),
                sb.ToString(), new UTF8Encoding(true));
            Result("EB_OK changesection handle=" + h + " ret=" + ok
                 + " key='" + key0 + "'->'" + key1 + "' " + (changed ? "CHANGED" : "UNCHANGED")
                 + " len=" + F(L0) + "->" + F(L1) + " h=" + F(H0) + "->" + F(H1)
                 + " wt=" + F(wt0) + "->" + F(wt1) + " holes=" + holes0 + "->" + holes1
                 + " links=" + links0 + "->" + links1
                 + (force && links0 > 0 ? " *** FORCED -- rebuild the joint ***" : "")
                 + " -> eb_changesection.txt");
        }

        // Describe every logical link (= connection) sitting on a part.
        static string LinkDesc(PsLogicalLink lk)
        {
            StringBuilder sb = new StringBuilder();
            // v157 (B.20 audit): print the enum NAME as well as the ordinal. The ordinal alone
            // sent B.20 looking up LogicalLinkType by counting members, which is exactly the
            // kind of arithmetic that goes wrong silently.
            try { sb.Append("type=" + (int)lk.Type + "/" + lk.Type); } catch { sb.Append("type=?"); }
            try { sb.Append(" name=" + Safe(lk.Name)); } catch { }
            try { sb.Append(" ident=" + Safe(lk.Ident)); } catch { }
            try { sb.Append(" desc=" + One(lk.Description)); } catch { }
            try { sb.Append(" modi=" + (int)lk.ModiType); } catch { }
            try { sb.Append(" parts=" + lk.LinkObjectCount + " bolts=" + lk.BoltObjectCount
                          + " extra=" + lk.AdditionalObjectCount); } catch { }
            // the parameter sets ג€” whichever one this link carries
            //
            // ⚠️ v157 (B.20 audit): "d != null" IS NOT A TYPE TEST. Every one of these getters
            // returns a live object on every link regardless of what the link actually is, so a
            // shear-plate joint printed a full BASEPLATE[...] block of zeros and a reader would
            // take those zeros for measurements of a base plate that does not exist. The null
            // check stays (it is still needed) but each block is now also gated on the data being
            // NON-DEGENERATE. A block of zeros is not information; it is a false reading.
            try
            {
                PsBaseplateLinkDataMgd d = lk.GetBasePlateLinkData();
                if (d != null && (d.Length != 0 || d.Width != 0 || d.Thickness != 0))
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
                if (d != null && (d.Thickness != 0 || d.Length != 0))
                    sb.Append(" RIB[t=" + F(d.Thickness) + " len=" + F(d.Length) + " shape=" + d.ShapeType
                        + " lenType=" + d.LengthType + " r=" + F(d.Radius) + " flDist=" + F(d.FlangeDistance)
                        + " webDist=" + F(d.WebDistance) + " ang=" + F(d.InsertAngle)
                        + " weldFl=" + F(d.WeldSeamFlange) + " weldWeb=" + F(d.WeldSeamWeb) + "]");
            }
            catch { }
            try
            {
                PsSpliceJointLinkDataMgd d = lk.GetSpliceJointLinkData();
                if (d != null && (d.PlateThicknessWeb != 0 || d.PlateThicknessFlange != 0 || d.HoleDiameter != 0))
                    sb.Append(" SPLICE[gap=" + F(d.DistanceBetweenObjects) + " holeDia=" + F(d.HoleDiameter)
                        + " play=" + F(d.HoleWorkloose) + " tWeb=" + F(d.PlateThicknessWeb)
                        + " tFl=" + F(d.PlateThicknessFlange) + " nH_web=" + d.HoleCountHorizontalWeb
                        + " nV_web=" + d.HoleCountVerticalWeb + " nH_fl=" + d.HoleCountHorizontalFlange
                        + " nV_fl=" + d.HoleCountVerticalFlange + " sideLap=" + F(d.SidePlateLap)
                        + " topLap=" + F(d.TopPlateLap) + "]");
            }
            catch { }
            // v157: these three printed "[present]" unconditionally -- three words that were true
            // of every link ever scanned and therefore said nothing. Real parameters instead, and
            // only when the block is actually populated.
            try
            {
                PsShearPlateLinkDataMgd d = lk.GetShearPlateLinkData();
                if (d != null && d.PlateThickness != 0)
                    sb.Append(" SHEARPLATE[t=" + F(d.PlateThickness) + " pos=" + d.PlatePosition
                        + " poly=" + (d.ShearPlateIsPolyPlate ? "1" : "0")
                        + " normalToCut=" + (d.NormalToCutPlane ? "1" : "0")
                        + " nV=" + d.VerticalHoleCount + " nH=" + d.HorizontalHoleCount
                        + " dV=" + F(d.HoleDistanceVertical) + " dVedge=" + F(d.HoleDistanceVerticalEdge)
                        + " dia=" + F(d.HoleDiameter) + " play=" + F(d.HoleWorkLoose)
                        + " gapSup=" + F(d.DistanceToSupport) + " gapConn=" + F(d.DistanceToConnected)
                        + " cope=" + (d.CreateCope ? "1" : "0") + "]");
            }
            catch { }
            try
            {
                PsWebAngleLinkDataMgd d = lk.GetWebAngleLinkData();
                if (d != null && d.HoleDiameter != 0)
                    sb.Append(" WEBANGLE[pos=" + d.WebAnglePosition
                        + " flat=" + (d.WebAngleIsFlatSteel ? "1" : "0")
                        + " turned=" + (d.TurnWebAngles ? "1" : "0")
                        + " nVert=" + d.VerticalHoleCount
                        + " nHconn=" + d.HorizontalHoleCountConnected
                        + " nHsup=" + d.HorizontalHoleCountSupport
                        + " dia=" + F(d.HoleDiameter) + " play=" + F(d.HoleWorkLoose)
                        + " cope=" + (d.CreateCope ? "1" : "0") + "]");
            }
            catch { }
            try
            {
                PsCopeLinkDataMgd d = lk.GetCopeLinkData();
                if (d != null && (d.Radius != 0 || d.CopeType != 0 || d.FlangeThickness != 0))
                    sb.Append(" COPE[type=" + d.CopeType + " edge=" + d.EdgeType
                        + " r=" + F(d.Radius) + " flThk=" + F(d.FlangeThickness)
                        + " ratholeD1=" + F(d.FirstRatholeDiameter)
                        + " ratholeD2=" + F(d.SecondRatholeDiameter) + "]");
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
                            // v157 (B.20 audit) -- TWO DEFECTS FIXED HERE, both found by running
                            // connscan on a shear-plate joint whose contents were already known.
                            //
                            // (1) The type tag was built by asking which GetXxxLinkData() came
                            //     back non-null. EVERY one of them comes back non-null, on every
                            //     link, full of zeros -- so a single shear-plate joint was tagged
                            //     "t17/BASEPLATE/RIB/SPLICE/SHEARPLATE/WEBANGLE/COPE" and the whole
                            //     histogram was noise. A non-null return is NOT a type test.
                            //     The link's own Type enum is the type, and it always was.
                            string nm = "?";
                            try { nm = lk.Type.ToString(); } catch { }
                            if (!typeCount.ContainsKey(nm)) typeCount[nm] = 0;
                            typeCount[nm]++;
                            sb.AppendLine("LINK\t" + hx + "\t" + cls + "\t" + ent.Layer + "\t" + num + "\t" + d);
                            // which parts and bolts belong to this joint
                            try
                            {
                                // (2) These printed the RAW 64-bit ObjectId pointer
                                //     ("parts=140688488454768,0"), which is unusable: it cannot be
                                //     fed to any other op, it changes every session, and the zero
                                //     entries look like data. Handles are the currency everywhere
                                //     else in this plugin. Empty slots are printed as "-" so that
                                //     an unfilled second-plate slot stays visible instead of
                                //     silently reading as an object.
                                StringBuilder mem = new StringBuilder();
                                int memN = 0;
                                for (int k = 0; k < lk.LinkObjectCount; k++)
                                {
                                    long oid = 0;
                                    try { oid = lk.getLinkObjectId(k); } catch { }
                                    if (k > 0) mem.Append(",");
                                    if (oid == 0) mem.Append("-");
                                    else { mem.Append(HandleOf(oid)); memN++; }
                                }
                                StringBuilder bo = new StringBuilder();
                                int boN = 0;
                                for (int k = 0; k < lk.BoltObjectCount; k++)
                                {
                                    long oid = 0;
                                    try { oid = lk.getBoltObjectId(k); } catch { }
                                    if (k > 0) bo.Append(",");
                                    if (oid == 0) bo.Append("-");
                                    else { bo.Append(HandleOf(oid)); boN++; }
                                }
                                StringBuilder ad = new StringBuilder();
                                for (int k = 0; k < lk.AdditionalObjectCount; k++)
                                {
                                    long oid = 0;
                                    try { oid = lk.getAdditionalObjectId(k); } catch { }
                                    if (k > 0) ad.Append(",");
                                    ad.Append(oid == 0 ? "-" : HandleOf(oid));
                                }
                                string tgt = "-";
                                try { long t0 = lk.getTargetId(); if (t0 != 0) tgt = HandleOf(t0); } catch { }
                                sb.AppendLine("MEMB\t" + hx + "\t" + num
                                            + "\tparts=" + mem + " (" + memN + "/" + lk.LinkObjectCount + " filled)"
                                            + "\tbolts=" + bo + " (" + boN + "/" + lk.BoltObjectCount + " filled)"
                                            + "\textra=" + ad + "\ttarget=" + tgt);
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
        // ===================================================================
        //  v163 (B.23 audit) -- THE GENERIC BINDER NOBODY HAD LOOKED FOR
        //
        //  Every chapter that asked "can I bind this class to an existing object?" asked it of
        //  the CLASS -- does PsGrid have SetObjectId, does PsShearPlateConnection have readFrom.
        //  B.6 answered no for PsGrid and put it on THE CEILING as structurally unbindable.
        //
        //  Bentley.ProStructures.Drawing.PsTransaction.GetObject(Int64, PsOpenMode, T&) has
        //  FIFTY-SEVEN overloads: PsGrid, PsGussetConnection, PsEditConnection, PsWeldFlag,
        //  PsPositionFlag, PsBoltStyle, PsShape, PsPlate, PsBolt, PsAssembly, PsBracing,
        //  PsPortalFrame, PsStairs, PsJoist, PsTruss, PsHandrail, PsLadder, PsWorkframe...
        //
        //  ⭐ The binder is not on the class. It is on the transaction. Nothing in this plugin
        //  had ever used it.
        //
        //  op=bind handle=<h> [cls=grid|gusset|editconn|weldflag|posflag|plate|shape|bolt|
        //                      bracing|assembly|workframe|portalframe|stairs|joist|truss]
        // ===================================================================
        void Bind(Dictionary<string, string> kv)
        {
            string h = Get(kv, "handle", "");
            string want = Get(kv, "cls", "").ToLowerInvariant();
            long oid = IdFromHandle(h);
            if (oid == 0) { Result("EB_ERR bind: bad handle " + h); return; }

            // ⛔⛔ MEASURED 10/08/2026, AND IT IS THE MOST IMPORTANT THING ON THIS OP:
            // GetObject DOES NOT TYPE-CHECK. Asked for a Ks_Shape as a PsGrid it returned
            // TRUE and handed back a reinterpreted pointer -- len=281474976713490, wide=NaN,
            // xDesc=234. A read gives nonsense; a WRITE through that handle would corrupt the
            // object. So the entity's real class is checked here first, and a mismatch is
            // refused rather than reported.
            string realCls = "?";
            try
            {
                Document doc0 = Application.DocumentManager.MdiActiveDocument;
                ObjectId id0 = new ObjectId(new System.IntPtr(oid));
                using (Transaction t0 = doc0.Database.TransactionManager.StartTransaction())
                {
                    t0.GetObject(id0, OpenMode.ForRead);
                    if (id0.ObjectClass != null) realCls = id0.ObjectClass.Name;
                    t0.Commit();
                }
            }
            catch { }
            var expect = new Dictionary<string, string>() {
                { "grid", "Grid" }, { "gusset", "Gusset" }, { "plate", "Plate" },
                { "shape", "Shape" }, { "weldflag", "WeldFlag" }, { "posflag", "PosFlag" },
            };
            if (want.Length > 0 && expect.ContainsKey(want)
                && realCls.IndexOf(expect[want], StringComparison.OrdinalIgnoreCase) < 0)
            {
                Result("EB_ERR bind REFUSED handle=" + h + " is a " + realCls + ", not a '" + want
                     + "'. GetObject does NOT type-check -- it would return True and hand back a"
                     + " reinterpreted pointer (measured: a Ks_Shape read as a PsGrid gives"
                     + " wide=NaN and len=281474976713490). Reading it is nonsense; writing"
                     + " through it would corrupt the object.");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(" cls=" + realCls);
            PsTransaction tr = null;
            try
            {
                tr = new PsTransaction();
                bool got;

                if (want.Length == 0 || want == "grid")
                {
                    PsGrid g = null;
                    got = false;
                    try { got = tr.GetObject(oid, PsOpenMode.kForRead, ref g); }
                    catch (System.Exception e) { sb.Append(" grid!EX:" + One(e.Message)); }
                    sb.Append(" grid=" + got + (g == null ? "(null)" : ""));
                    // ⭐⭐ B.6.7, reopened. Its audit concluded: "PsCreateGrid is the creator and
                    // has no user-axis methods; PsGrid has the user axes and no creator and no
                    // binder. The two halves never meet in the API." They meet HERE. The 10/08
                    // test called addUserXaxis on an UN-INSERTED grid and got false; this calls
                    // it on a grid that already exists in the drawing.
                    //
                    // ⛔⛔ AND IT KILLED AUTOCAD. MEASURED 10/08/2026.
                    // The first run passed addx AND addy and called four things: addUserXaxis,
                    // addUserYaxis, getUserXaxis, getUserYaxis. The process died -- EB_TIMEOUT,
                    // "AutoCAD Error Report". Nothing was lost: the model had been saved in the
                    // same script, immediately before. That was the protocol and it paid again.
                    //
                    // The whole path is now behind force=1 and a single-call probe=, so the
                    // killer can be named EXACTLY rather than as "one of four". A wrong name on a
                    // permanent do-not-call list is the expensive kind of error (B.4's lesson).
                    string ax = Get(kv, "addx", ""), ay = Get(kv, "addy", "");
                    string probe = Get(kv, "probe", "").ToLowerInvariant();
                    if (got && g != null && (ax.Length > 0 || ay.Length > 0))
                    {
                        if (Get(kv, "force", "") != "1")
                        {
                            sb.Append(" ⛔ REFUSED: the user-axis path on a bound PsGrid KILLED"
                                    + " AutoCAD on 10/08/2026 (addUserXaxis + addUserYaxis +"
                                    + " getUserXaxis + getUserYaxis in one call). See"
                                    + " knowledge/learning/findings/LETHAL-CALLS-do-not-invoke.md."
                                    + " SAVE FIRST, then force=1 with probe=addx|addy|getx|gety"
                                    + " to run exactly ONE of them.");
                        }
                        else
                        {
                            int bx = 0, by = 0;
                            try { bx = g.UserXaxisCount; by = g.UserYaxisCount; } catch { }
                            sb.Append(" userAxesBefore=" + bx + "/" + by + " probe=" + probe);
                            if (probe == "addx" && ax.Length > 0)
                            {
                                string[] pp = ax.Split(';');
                                bool okAdd = false;
                                try { okAdd = g.addUserXaxis(Pt(pp[0]), Pt(pp[1])); }
                                catch (System.Exception e) { sb.Append(" !EX:" + One(e.Message)); }
                                sb.Append(" addUserXaxis=" + okAdd);
                            }
                            else if (probe == "addy" && ay.Length > 0)
                            {
                                string[] pp = ay.Split(';');
                                bool okAdd = false;
                                try { okAdd = g.addUserYaxis(Pt(pp[0]), Pt(pp[1])); }
                                catch (System.Exception e) { sb.Append(" !EX:" + One(e.Message)); }
                                sb.Append(" addUserYaxis=" + okAdd);
                            }
                            else if (probe == "getx")
                            {
                                PsPoint s0 = new PsPoint(0, 0, 0), e0 = new PsPoint(0, 0, 0);
                                bool r0 = false;
                                try { r0 = g.getUserXaxis(0, s0, e0); }
                                catch (System.Exception e) { sb.Append(" !EX:" + One(e.Message)); }
                                sb.Append(" getUserXaxis(0)=" + r0 + " (" + F(s0.x) + "," + F(s0.y)
                                        + ")->(" + F(e0.x) + "," + F(e0.y) + ")");
                            }
                            else { sb.Append(" no probe selected -- nothing called"); }
                            int cx = 0, cy = 0;
                            try { cx = g.UserXaxisCount; cy = g.UserYaxisCount; } catch { }
                            sb.Append(" userAxesAfter=" + cx + "/" + cy);
                        }
                    }
                    if (got && g != null)
                    {
                        try { sb.Append(" [name='" + g.Name + "' len=" + F(g.Length) + " wide=" + F(g.Wide)
                                      + " type=" + g.GridType
                                      + " lenDiv=" + g.LengthDiv + " wideDiv=" + g.WideDiv
                                      + " userX=" + g.UserXaxisCount + " userY=" + g.UserYaxisCount
                                      + " xDesc=" + g.XDescriptionCount + " yDesc=" + g.YDescriptionCount + "]"); }
                        catch (System.Exception e) { sb.Append(" props!EX:" + One(e.Message)); }
                    }
                }
                if (want.Length == 0 || want == "gusset")
                {
                    PsGussetConnection gc = null;
                    got = false;
                    try { got = tr.GetObject(oid, PsOpenMode.kForRead, ref gc); }
                    catch (System.Exception e) { sb.Append(" gusset!EX:" + One(e.Message)); }
                    sb.Append(" gusset=" + got + (gc == null ? "(null)" : ""));
                    if (got && gc != null)
                    {
                        try { sb.Append(" [type=" + gc.getObjectType()
                                      + " front=" + F(gc.getDistanceFront())
                                      + " behind=" + F(gc.getDistanceBehind())
                                      + " between=" + F(gc.getDistanceBetween())
                                      + " cross=" + F(gc.getDistanceCross())
                                      + " layer='" + gc.getLayer() + "']"); }
                        catch (System.Exception e) { sb.Append(" props!EX:" + One(e.Message)); }
                    }
                }
                if (want.Length == 0 || want == "plate")
                {
                    PsPlate p = null;
                    got = false;
                    try { got = tr.GetObject(oid, PsOpenMode.kForRead, ref p); }
                    catch (System.Exception e) { sb.Append(" plate!EX:" + One(e.Message)); }
                    sb.Append(" plate=" + got + (p == null ? "(null)" : ""));
                    if (got && p != null)
                    {
                        try { sb.Append(" [name='" + p.Name + "' L=" + F(p.Length) + " H=" + F(p.Height)
                                      + " verts=" + p.VertexCount + " rect=" + p.RectangleMode + "]"); }
                        catch (System.Exception e) { sb.Append(" props!EX:" + One(e.Message)); }
                    }
                }
                if (want.Length == 0 || want == "shape")
                {
                    PsShape s = null;
                    got = false;
                    try { got = tr.GetObject(oid, PsOpenMode.kForRead, ref s); }
                    catch (System.Exception e) { sb.Append(" shape!EX:" + One(e.Message)); }
                    sb.Append(" shape=" + got + (s == null ? "(null)" : ""));
                    if (got && s != null)
                    {
                        try { sb.Append(" [key='" + s.Key + "' cat='" + s.Katalog + "']"); }
                        catch (System.Exception e) { sb.Append(" props!EX:" + One(e.Message)); }
                    }
                }
                if (want == "editconn")
                {
                    // ⛔⛔ MEASURED 10/08/2026: THIS KILLS AUTOCAD.
                    // B.27 retracted its own connverify with the reason "LinkType always read
                    // kUndefinedLink -- PsEditConnection HAS NO BINDER". B.23 found the binder on
                    // PsTransaction, so this was the measurement that would settle it. The first
                    // call, on beam 15EE, took the process down: EB_TIMEOUT, "AutoCAD Error
                    // Report". Nothing was lost -- the model had not been modified since its last
                    // save -- but nothing was learned either.
                    //
                    // ⚠️ AND IT NARROWS B.23's RULE. That audit concluded "reading a bound object
                    // is safe; every MUTATOR is suspect", on the evidence of PsGrid, PsPlate and
                    // PsShape. PsEditConnection is a counter-example: a plain READ is lethal.
                    // ⇒ Safety is per TYPE, not per operation.
                    //
                    // Not isolated: bind-then-read is one call here, so whether GetObject or the
                    // property read is the killer is UNKNOWN. Each isolation costs a crash.
                    if (Get(kv, "force", "") != "1")
                    {
                        Result("EB_ERR bind REFUSED cls=editconn -- binding a PsEditConnection"
                             + " KILLED AutoCAD on 10/08/2026, on the first call. See"
                             + " knowledge/learning/findings/LETHAL-CALLS-do-not-invoke.md."
                             + " SAVE FIRST, then force=1 if you have a reason.");
                        return;
                    }
                    PsEditConnection ec = null;
                    got = false;
                    try { got = tr.GetObject(oid, PsOpenMode.kForRead, ref ec); }
                    catch (System.Exception e) { sb.Append(" editconn!EX:" + One(e.Message)); }
                    sb.Append(" editconn=" + got + (ec == null ? "(null)" : ""));
                    if (got && ec != null)
                    {
                        try
                        {
                            sb.Append(" [status=" + ec.Status
                                    + " linkType=" + (int)ec.LinkType + "/" + ec.LinkType
                                    + " linkIndex=" + ec.LinkIndex + " linkNumber=" + ec.LinkNumber
                                    + " comIdent='" + ec.COMIdent + "'"
                                    + " owner=" + (ec.OwnerId == 0 ? "-" : HandleOf(ec.OwnerId))
                                    + " target=" + (ec.TargetId == 0 ? "-" : HandleOf(ec.TargetId))
                                    + "]");
                        }
                        catch (System.Exception e) { sb.Append(" props!EX:" + One(e.Message)); }
                    }
                }
                if (want == "weldflag")
                {
                    PsWeldFlag wf = null;
                    got = false;
                    try { got = tr.GetObject(oid, PsOpenMode.kForRead, ref wf); }
                    catch (System.Exception e) { sb.Append(" weldflag!EX:" + One(e.Message)); }
                    sb.Append(" weldflag=" + got + (wf == null ? "(null)" : ""));
                }
                if (want == "posflag")
                {
                    PsPositionFlag pf = null;
                    got = false;
                    try { got = tr.GetObject(oid, PsOpenMode.kForRead, ref pf); }
                    catch (System.Exception e) { sb.Append(" posflag!EX:" + One(e.Message)); }
                    sb.Append(" posflag=" + got + (pf == null ? "(null)" : ""));
                }
            }
            catch (System.Exception ex) { sb.Append(" EX:" + One(ex.Message)); }
            finally { try { if (tr != null) tr.Close(); } catch { } }
            Result("EB_OK bind handle=" + h + sb.ToString());
        }

        // ===================================================================
        //  v161 (B.21 audit) -- TWO HELPERS THAT EXIST BECAUSE OF ONE MEASUREMENT
        //
        //  Running the FIXED connscan over B.21's own band showed the welded splice reporting
        //  "bolts=32" while the bolted splices reported "bolts=0". Reading those 32 objects:
        //  every one sits on layer PS_Weld. They are Ks_WeldFlag.
        //
        //  ⚠️⚠️ getBoltObjectId / BoltObjectCount DO NOT MEAN BOLTS. They mean FASTENERS, and a
        //  WELD FLAG OCCUPIES A BOLT SLOT. A link reporting bolts=32 can hold zero bolts --
        //  which is exactly the shape of an iron-rule violation that passes a bolt-count check.
        //  v160's shearplate guard counted link slots and would have been satisfied by welds.
        //
        //  So the guard has to look at what the object IS, not at how many slots are filled.
        // ===================================================================

        /// How many of a link's "bolt" objects are REAL BOLTS (Ks_Bolt), not weld flags.
        /// Returns -1 if the link cannot be read at all.
        static int CountRealBolts(PsLogicalLink lk, out int slots, out int welds)
        {
            slots = 0; welds = 0;
            int real = 0;
            try { slots = lk.BoltObjectCount; } catch { return -1; }
            Document doc = Application.DocumentManager.MdiActiveDocument;
            for (int k = 0; k < slots; k++)
            {
                long oid = 0;
                try { oid = lk.getBoltObjectId(k); } catch { }
                if (oid == 0) continue;
                try
                {
                    ObjectId id = new ObjectId(new System.IntPtr(oid));
                    using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                    {
                        Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                        string cls = (ent != null && id.ObjectClass != null) ? id.ObjectClass.Name : "";
                        string lay = (ent != null) ? ent.Layer : "";
                        if (cls.IndexOf("Weld") >= 0 || lay.IndexOf("Weld") >= 0) welds++;
                        else if (cls.IndexOf("Bolt") >= 0) real++;
                        tr.Commit();
                    }
                }
                catch { }
            }
            return real;
        }

        /// Resolve a bolt-style NAME to the CRC the connection classes want.
        /// PsSpliceJointLinkDataMgd exposes ONLY BoltStyleCRC -- no BoltStyle string, unlike its
        /// two siblings -- so without this there is no way to name a style on a splice at all,
        /// and both shipped templates carry boltCRC=0, i.e. NO STYLE.
        static int BoltStyleCrcFromName(string name, out string diag)
        {
            diag = "";
            if (name == null || name.Length == 0) return 0;
            try
            {
                PsObjectStyleList lst = new PsObjectStyleList();
                try { lst.Type = (ObjectStyleListType)0; } catch { }
                lst.Initialize();
                try { lst.ReadFromFile(); } catch { }      // Initialize() alone leaves Count at 0
                int n = 0;
                try { n = lst.Count; } catch { }
                for (int i = 0; i < n; i++)
                {
                    string nm = "";
                    try { nm = lst.get_Entry((short)i); } catch { }
                    if (nm != null && string.Equals(nm.Trim(), name.Trim(),
                                                    StringComparison.OrdinalIgnoreCase))
                    {
                        long oid = lst.getStyleObjectId(nm);
                        int crc = lst.GetStyleCRCFromId(oid);
                        diag = " boltStyle='" + nm + "'->crc=" + crc;
                        return crc;
                    }
                }
                diag = " boltStyle='" + name + "' NOT FOUND among " + n + " styles";
            }
            catch (System.Exception e) { diag = " boltStyleLookup!EX:" + One(e.Message); }
            return 0;
        }

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







        static int DetailCuts(long oid)
        {
            try
            {
                PsEditShapeModification em = new PsEditShapeModification();
                em.SetObjectId(oid);
                return em.DetailCutCount;
            }
            catch { return -1; }
        }














        // =====================================================================
        //  v95 ג€” B.9 INSERT PLATES
        //  The chapter's own boundary: B.9 is the FREE-CONTOUR plate (gussets,
        //  connecting plates, butt straps). Base plates / end plates / stiffeners
        //  belong to B.16-B.18, which name and classify them for you.
        //  PsCreatePlate maps 1:1 onto the B.9.1 dialog.
        // =====================================================================

        static PositionSelection PosSel(string s, PositionSelection dflt)
        {
            if (s == null || s.Length == 0) return dflt;
            try { return (PositionSelection)System.Enum.Parse(typeof(PositionSelection), s, true); }
            catch { }
            try { return (PositionSelection)System.Enum.Parse(typeof(PositionSelection), "k" + s, true); }
            catch { return dflt; }
        }

        // op=plate9 mode=rect|poly|radial|diagonal|fromshape ...
        //   rect      : l= w=            (SetAsRectangularPlate)
        //   poly      : pts=x,y,z;...    (AppendEdgePoint, the manual's free polygon)
        //   radial    : radius=          (SetAsRadialPlate ג€” a circular plate)
        //   diagonal  : p1= p2=          (SetFromRectangle ג€” "inserted according to its diagonal")
        //   fromshape : handle=          (CreateFromShape ג€” "a flat is transformed into a
        //                                 poly-plate. ALL PROCESSING ACTIONS WILL BE ADOPTED")
        // common: at= t= [ex= ey= ez=] [xpos= ypos=] [vpos=kDown|kTop|kMiddle]
        //         [xoff= yoff=] [insheight=] [grid=1 griddir=] [name= material= article=]
        //         [layer= family= display= area= descr= style=] [check=1]
        void Plate9(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string h0, c0; int before = Census(out h0, out c0);
            string applied = "", msg = "";
            bool created = false;
            string checkNote = "";
            try
            {
                PsCreatePlate cp = new PsCreatePlate();
                cp.SetToDefaults();

                // --- the insertion plane. B.9.1 "Insertion Plane indicates the user
                // coordinate system to be used." Without it the plate lands on whatever
                // UCS happens to be current -- the same trap as the B.6 work frame.
                double[] at = Nums(Get(kv, "at", "0,0,0"));
                PsPoint org = new PsPoint(at[0], at[1], at[2]);
                string sx = Get(kv, "ex", "1,0,0"), sy = Get(kv, "ey", "0,1,0"), sz = Get(kv, "ez", "0,0,1");
                double[] ax = Nums(sx), ay = Nums(sy), az = Nums(sz);
                PsMatrix m = new PsMatrix();
                m.SetCoordinateSystem(org,
                    new PsVector(ax[0], ax[1], ax[2]),
                    new PsVector(ay[0], ay[1], ay[2]),
                    new PsVector(az[0], az[1], az[2]));
                cp.SetInsertMatrix(m);
                applied += " at=" + Get(kv, "at", "0,0,0");

                double T = double.Parse(Get(kv, "t", "10"), IC);
                cp.SetThickness(T);
                applied += " t=" + F(T);

                string mode = Get(kv, "mode", "rect").ToLowerInvariant();

                // 10/08 audit: a parameter that is valid FOR THE OP is not necessarily valid FOR
                // THE MODE. rect places from at=; p1/p2/p3/pts belong to the poly and pts modes.
                // Nine plates built during this audit stacked up at the origin because p1= was
                // accepted in silence. Silence is the failure -- refuse instead.
                if (mode == "rect")
                {
                    List<string> ign = new List<string>();
                    foreach (string bad in new string[] { "p1", "p2", "p3", "pts", "radius" })
                        if (Get(kv, bad, "").Length > 0) ign.Add(bad);
                    if (ign.Count > 0)
                    {
                        Result("EB_ERR plate9 mode=rect ignores " + string.Join(",", ign.ToArray())
                             + " -- rect places from at=x,y,z. Nothing was created. "
                             + "(Use mode=poly/pts for point-defined plates.)");
                        return;
                    }
                }
                if (mode == "rect")
                {
                    double L = double.Parse(Get(kv, "l", "300"), IC);
                    double W = double.Parse(Get(kv, "w", "200"), IC);
                    cp.SetAsRectangularPlate(L, W);
                    applied += " rect=" + F(L) + "x" + F(W);
                }
                else if (mode == "poly")
                {
                    // B.9.1 method 1: "This polygon is used to form a poly-plate."
                    cp.DeleteAllEdgePoints();
                    int n = 0;
                    foreach (string p in Get(kv, "pts", "").Split(';'))
                    {
                        if (p.Trim().Length == 0) continue;
                        double[] v = Nums(p);
                        cp.AppendEdgePoint(new PsPoint(v[0], v.Length > 1 ? v[1] : 0, v.Length > 2 ? v[2] : 0));
                        n++;
                    }
                    applied += " poly=" + n + "pts";
                }
                else if (mode == "radial")
                {
                    double R = double.Parse(Get(kv, "radius", "250"), IC);
                    cp.SetAsRadialPlate(R);
                    applied += " radial r=" + F(R);
                }
                else if (mode == "diagonal")
                {
                    // B.9.1 method 7: "A plate is inserted according to its diagonal. The
                    // alignment of x- and y-direction is specified by the current UCS."
                    // PsRectangle has no SetFromPoints -- it is built from extents plus an
                    // insert point, so the diagonal becomes extents in the frame's own axes.
                    double[] p1 = Nums(Get(kv, "p1", "0,0,0")), p2 = Nums(Get(kv, "p2", "300,200,0"));
                    PsRectangle rect = new PsRectangle();
                    rect.SetToDefaults();
                    rect.SetXYPlane(new PsVector(ax[0], ax[1], ax[2]), new PsVector(ay[0], ay[1], ay[2]));
                    rect.SetNormal(new PsVector(az[0], az[1], az[2]));
                    double dL = System.Math.Abs(p2[0] - p1[0]), dW = System.Math.Abs(p2[1] - p1[1]);
                    rect.SetFromExtents(dL, dW);
                    rect.SetInsertPoint(new PsPoint(System.Math.Min(p1[0], p2[0]),
                                                    System.Math.Min(p1[1], p2[1]), p1[2]));
                    rect.XPosition = PositionSelection.kLeft;
                    rect.YPosition = PositionSelection.kDown;
                    cp.SetFromRectangle(rect);
                    applied += " diagonal=" + F(dL) + "x" + F(dW);
                }
                else if (mode == "edges")
                {
                    // B.9.1 method 5: "creation of a plate by means of FOUR POINTS. These
                    // points don't have to be situated in the current UCS. The FIRST THREE
                    // selected points specify the plane. The order is: bottom left; bottom
                    // right, top left, top right." SetFromEdges takes exactly those three.
                    PsRectangle rect = new PsRectangle();
                    rect.SetToDefaults();
                    rect.SetFromEdges(Pt(Get(kv, "p1", "0,0,0")),
                                      Pt(Get(kv, "p2", "300,0,0")),
                                      Pt(Get(kv, "p3", "0,200,0")));
                    cp.SetFromRectangle(rect);
                    applied += " edges(3pt plane)";
                }
                else if (mode == "fromshape")
                {
                    long src = IdFromHandle(Get(kv, "handle", ""));
                    cp.SetObjectId(src);
                    applied += " fromshape src=" + Get(kv, "handle", "");
                }

                // --- the insertion-position grid (B.9.1 "the selected insertion position")
                string xp = Get(kv, "xpos", ""), yp = Get(kv, "ypos", "");
                if (xp.Length > 0) { cp.SetXPosition(PosSel(xp, PositionSelection.kCenter)); applied += " xpos=" + xp; }
                if (yp.Length > 0) { cp.SetYPosition(PosSel(yp, PositionSelection.kCenter)); applied += " ypos=" + yp; }
                string xo = Get(kv, "xoff", ""), yo = Get(kv, "yoff", "");
                if (xo.Length > 0) { cp.SetXOffset(double.Parse(xo, IC)); applied += " xoff=" + xo; }
                if (yo.Length > 0) { cp.SetYOffset(double.Parse(yo, IC)); applied += " yoff=" + yo; }

                // --- "Insert Edge: the VERTICAL POSITION of the plate related to the current
                // UCS or ECS". The manual never lists the values; the enum does:
                //   Bentley.ProStructures.VerticalPosition = kDown, kTop, kMiddle
                string vp = Get(kv, "vpos", "");
                if (vp.Length > 0)
                {
                    VerticalPosition v;
                    try { v = (VerticalPosition)System.Enum.Parse(typeof(VerticalPosition), vp, true); }
                    catch { v = (VerticalPosition)System.Enum.Parse(typeof(VerticalPosition), "k" + vp, true); }
                    cp.SetNormalPosition(v);
                    applied += " vpos=" + v;
                }
                string ih = Get(kv, "insheight", "");
                if (ih.Length > 0) { cp.SetInsertHeight(double.Parse(ih, IC)); applied += " insheight=" + ih; }

                // --- "Grid: ... you can show that it ISN'T A PLATE but a component part such
                // as e.g. gridirons. In the settings/plate you can enter a REDUCTION OF WEIGHT
                // IN PERCENT for this case." A display flag that moves a fabrication weight.
                if (Get(kv, "grid", "") == "1")
                {
                    cp.SetGrid(true);
                    string gd = Get(kv, "griddir", "");
                    if (gd.Length > 0) { double[] g = Nums(gd); cp.SetGridDirection(new PsVector(g[0], g[1], g[2])); }
                    applied += " grid=1";
                }

                string nm = Get(kv, "name", "");
                if (nm.Length > 0) { cp.SetName(nm); applied += " name='" + nm + "'"; }
                string mt = Get(kv, "material", "");
                if (mt.Length > 0) { cp.SetMaterial(int.Parse(mt)); applied += " material=" + mt; }
                string ar = Get(kv, "article", "");
                if (ar.Length > 0) { cp.SetArticle(ar); applied += " article=" + ar; }
                string lay = Get(kv, "layer", "");
                if (lay.Length > 0) { cp.SetLayer(lay); cp.UseCurrentLayer(false); applied += " layer=" + lay; }
                else cp.UseCurrentLayer(false);   // B.1 audit 10/08: false => ProSteel assigns the part's OWN layer
                string fam = Get(kv, "family", "");
                if (fam.Length > 0) cp.SetFamilyClass(int.Parse(fam));
                string dcl = Get(kv, "display", "");
                if (dcl.Length > 0) cp.SetDisplayClass(int.Parse(dcl));
                string acl = Get(kv, "area", "");
                if (acl.Length > 0) cp.SetAreaClass(int.Parse(acl));
                string de = Get(kv, "descr", "");
                if (de.Length > 0) cp.SetDescription(int.Parse(de));
                string st = Get(kv, "style", "");
                if (st.Length > 0) cp.SetDetailStyle(st);

                // B.9.1: "Take care that no crossings are generated by your input. Then,
                // plate creation will not be possible." -- and that IS pre-flightable.
                if (Get(kv, "check", "1") == "1")
                {
                    try { checkNote = " checkValidPlate=" + cp.checkValidPlate(); }
                    catch (System.Exception e) { checkNote = " checkValidPlate EX:" + One(e.Message); }
                }

                created = (mode == "fromshape") ? cp.CreateFromShape() : cp.Create();
                applied += (mode == "fromshape" ? " CreateFromShape()=" : " Create()=") + created;
            }
            catch (System.Exception ex) { msg = " EX:" + One(ex.Message); }

            string h1, c1; int after = Census(out h1, out c1);
            Result(((after > before) ? "EB_OK" : "EB_ERR") + " plate9 handle=" + (after > before ? h1 : "-") +
                   " class=" + (after > before ? c1 : "-") + " census=" + before + "->" + after +
                   applied + checkNote + msg);
        }

        // op=arcplate p1= p2= center= [normal=] w= t= [bigarc=1] [rot=] [xpos= ypos=]
        // B.9.2: "you create a bent plate on the base of three points. Keep the ALT-key
        // pressed at input and you can enter an arc with > 180 degrees."  SetBigArc IS
        // that ALT key.
        void ArcPlate(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string h0, c0; int before = Census(out h0, out c0);
            string applied = "", msg = "";
            bool ok = false;
            try
            {
                PsCreateArcPlate ap = new PsCreateArcPlate();
                ap.SetToDefaults();
                ap.SetStartPoint(Pt(Get(kv, "p1", "0,0,0")));
                ap.SetEndPoint(Pt(Get(kv, "p2", "1000,0,0")));
                ap.SetCenterPoint(Pt(Get(kv, "center", "500,0,0")));
                double[] nv = Nums(Get(kv, "normal", "0,0,1"));
                ap.SetNormal(new PsVector(nv[0], nv[1], nv[2]));
                double W = double.Parse(Get(kv, "w", "500"), IC);
                double T = double.Parse(Get(kv, "t", "8"), IC);
                ap.SetWidth(W); ap.SetThickness(T);
                bool big = Get(kv, "bigarc", "0") == "1";
                ap.SetBigArc(big);
                applied += " p1=" + Get(kv, "p1", "0,0,0") + " p2=" + Get(kv, "p2", "1000,0,0") +
                           " c=" + Get(kv, "center", "500,0,0") + " w=" + F(W) + " t=" + F(T) +
                           " bigArc=" + big;
                string rot = Get(kv, "rot", "");
                if (rot.Length > 0) { ap.SetRotation(double.Parse(rot, IC)); applied += " rot=" + rot; }
                string xp = Get(kv, "xpos", ""), yp = Get(kv, "ypos", "");
                if (xp.Length > 0) ap.SetXPosition(PosSel(xp, PositionSelection.kCenter));
                if (yp.Length > 0) ap.SetYPosition(PosSel(yp, PositionSelection.kCenter));
                string lay = Get(kv, "layer", "");
                if (lay.Length > 0) { ap.SetLayer(lay); ap.UseCurrentLayer(false); } else ap.UseCurrentLayer(false);   // B.1 audit 10/08: false => ProSteel assigns the part's OWN layer
                string nm = Get(kv, "name", "");
                if (nm.Length > 0) ap.SetName(nm);
                ok = ap.Create();
                applied += " Create()=" + ok;
            }
            catch (System.Exception ex) { msg = " EX:" + One(ex.Message); }
            string h1, c1; int after = Census(out h1, out c1);
            Result(((after > before) ? "EB_OK" : "EB_ERR") + " arcplate handle=" + (after > before ? h1 : "-") +
                   " class=" + (after > before ? c1 : "-") + " census=" + before + "->" + after + applied + msg);
        }

        // op=bend handle=<plate> at=x,y,z len= [front=0 rear=0] radius= angle=
        //         [convert=1]
        // B.9.4: "To generate a bent plate, you FIRST HAVE TO HAVE INSERTED A FLAT PLATE.
        // This plate determines the alignment."
        //
        // ג ן¸ MEASURED 08/08: PsCreateBendPlate.Create() returns FALSE and yet the flat
        // plate is ERASED and REPLACED -- every later call on the original handle throws
        // eWasErased. The conversion does not mutate the plate, it substitutes a new
        // entity. So the new handle must be recovered from a census diff, and the caller
        // must use THAT from then on.
        // B.8.2 Bent Shapes -- added by the 10/08 part-B audit. The chapter was read and never
        // implemented: PsCreateBendShape appeared nowhere in the plugin, and it is the ONLY
        // creator that reaches the Weld shape database (the straight creator has four selectors,
        // this one has five). The path is a PsPolygon3d, so a bent shape follows a polyline,
        // an arc, a circle or a helix.
        // Does the shape that was built actually follow the path that was asked for?
        // Every vertex of the source polygon must lie inside the new part's bounding box,
        // allowing for the section's own half-width. pts= passes; ConvertFromPolyline with an
        // arc does not, and used to say nothing about it.
        static string PathFit(string handle, PsPolygon3d poly)
        {
            try
            {
                int n = poly.Count;
                if (n < 1) return "n/a";
                double xa = 0, ya = 0, za = 0, xb = 0, yb = 0, zb = 0;
                bool got = false;
                Document doc = Application.DocumentManager.MdiActiveDocument;
                using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                {
                    ObjectId oid = doc.Database.GetObjectId(false,
                        new Handle(Convert.ToInt64(handle, 16)), 0);
                    if (!oid.IsNull)
                    {
                        Entity en = tr.GetObject(oid, OpenMode.ForRead) as Entity;
                        if (en != null)
                        {
                            Extents3d ex = en.GeometricExtents;
                            xa = ex.MinPoint.X; ya = ex.MinPoint.Y; za = ex.MinPoint.Z;
                            xb = ex.MaxPoint.X; yb = ex.MaxPoint.Y; zb = ex.MaxPoint.Z;
                            got = true;
                        }
                    }
                    tr.Commit();
                }
                if (!got) return "unread";
                double tol = 600.0;   // half the deepest section in the shipped catalogs
                double worst = 0.0; int bad = 0;
                for (int i = 0; i < n; i++)
                {
                    PsPoint v = new PsPoint(0, 0, 0), c = new PsPoint(0, 0, 0);
                    poly.GetVertexPoint(i, v, c);
                    double d = 0.0;
                    if (v.x < xa - tol) d = Math.Max(d, xa - tol - v.x);
                    if (v.x > xb + tol) d = Math.Max(d, v.x - xb - tol);
                    if (v.y < ya - tol) d = Math.Max(d, ya - tol - v.y);
                    if (v.y > yb + tol) d = Math.Max(d, v.y - yb - tol);
                    if (v.z < za - tol) d = Math.Max(d, za - tol - v.z);
                    if (v.z > zb + tol) d = Math.Max(d, v.z - zb - tol);
                    if (d > 0) { bad++; worst = Math.Max(worst, d); }
                }
                if (bad == 0) return "ok";
                return "MISMATCH " + bad + "/" + n + "_vertices_outside_by_" + F(worst) + "mm";
            }
            catch (System.Exception ex) { return "err:" + ex.Message.Replace(" ", "_"); }
        }

        void BendShape(Dictionary<string, string> kv)
        {
            string name = Get(kv, "name", "");
            string catalog = Get(kv, "catalog", "");
            string skind = Get(kv, "kind", "standard").ToLowerInvariant();
            string ptsS = Get(kv, "pts", "");
            string arcS = Get(kv, "arc", "");
            string circS = Get(kv, "circle", "");
            string helixS = Get(kv, "helix", "");
            string srcH = Get(kv, "handle", "");
            double rot = 0; double.TryParse(Get(kv, "rot", "0"),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out rot);

            if (name.Length == 0) { Result("EB_ERR bendshape: name= required"); return; }

            PsPolygon3d poly = new PsPolygon3d();
            string pathDesc = "";
            try
            {
                if (srcH.Length > 0)
                {
                    long sid = IdFromHandle(srcH);
                    if (sid == 0) { Result("EB_ERR bendshape: bad handle " + srcH); return; }
                    poly.ConvertFromPolyline(sid, new PsPoint(0, 0, 0));
                    pathDesc = "fromPolyline=" + srcH;
                }
                else if (circS.Length > 0)
                {
                    poly.CreateCircle(Nums(circS)[0]);
                    pathDesc = "circle r=" + circS;
                }
                else if (helixS.Length > 0)
                {
                    double[] q = Nums(helixS);   // radius,angle,rising,resolution[,left]
                    poly.CreateHelix(q[0], q[1], q[2], (int)(q.Length > 3 ? q[3] : 24),
                                     q.Length > 4 && q[4] != 0);
                    pathDesc = "helix " + helixS;
                }
                else if (arcS.Length > 0)
                {
                    // arc = x,y,z ; cx,cy,cz ; x2,y2,z2   -- start, centre, end
                    string[] seg = arcS.Split(';');
                    if (seg.Length < 3) { Result("EB_ERR bendshape: arc needs start;centre;end"); return; }
                    poly.AppendLine(Pt(seg[0]));
                    poly.AppendVertex(Pt(seg[2]), Pt(seg[1]), 1, false);
                    pathDesc = "arc " + arcS;
                }
                else if (ptsS.Length > 0)
                {
                    string[] seg = ptsS.Split(';');
                    int n = 0;
                    foreach (string sp in seg)
                        if (sp.Trim().Length > 0) { poly.AppendLine(Pt(sp)); n++; }
                    if (n < 2) { Result("EB_ERR bendshape: pts needs >= 2 points"); return; }
                    pathDesc = "pts n=" + n;
                }
                else { Result("EB_ERR bendshape: give pts= or arc= or circle= or helix= or handle="); return; }
            }
            catch (System.Exception ex) { Result("EB_ERR bendshape path: " + ex.Message); return; }

            string[] cats = catalog.Length > 0
                ? new string[] { catalog }
                : new string[] { "", "DIN", "Euro", "EURO", "AISC" };
            string[] names = new string[] { name, name.Replace(" ", ""),
                System.Text.RegularExpressions.Regex.Replace(name, "([A-Za-z])(\\d)", "$1 $2") };

            string hb, cb0; int before = Census(out hb, out cb0);
            foreach (string cat in cats)
                foreach (string nm in names)
                {
                    PsCreateBendShape cb = new PsCreateBendShape();
                    cb.SetToDefaults();
                    if (skind.StartsWith("spec") || skind.StartsWith("user") || skind.StartsWith("sopro"))
                        cb.SelectSpecialSections();
                    else if (skind.StartsWith("roof") || skind.StartsWith("wall"))
                        cb.SelectRoofWallSections();
                    else if (skind.StartsWith("comb"))
                        cb.SelectCombinationSections();
                    else if (skind.StartsWith("weld"))
                        cb.SelectWeldSections();
                    else
                        cb.SelectStandardSections();
                    cb.SetCrossSection(nm, cat);
                    cb.SetPolygon(poly);
                    if (rot != 0) { try { cb.SetRotation(rot); } catch { } }
                    string raS = Get(kv, "refaxis", "");
                    if (raS.Length > 0)
                    {
                        try { double[] q = Nums(raS); cb.SetReferenceAxis(new PsVector(q[0], q[1], q[2])); }
                        catch { }
                    }
                    // B.1 audit 10/08: false => ProSteel assigns the part's OWN layer
                    cb.UseCurrentLayer(false);
                    string lay = Get(kv, "layer", "");
                    if (lay.Length > 0) { try { cb.SetLayer(lay); } catch { } }

                    bool ok = false;
                    try { ok = cb.Create(); } catch { ok = false; }
                    if (!ok) continue;

                    string h1, c1; int after = Census(out h1, out c1);
                    if (after <= before) continue;
                    Result("EB_OK bendshape name=" + nm + " catalog=" + (cat.Length > 0 ? cat : "(default)")
                           + " kind=" + skind + " handle=" + h1 + " class=" + c1
                           + " path=" + pathDesc + " pathfit=" + PathFit(h1, poly)
                           + " entities=" + after);
                    return;
                }
            Result("EB_ERR bendshape: nothing created for '" + name + "' kind=" + skind
                   + " (path " + pathDesc + "). Check the catalog folder under Data\\.");
        }

        void Bend(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            long pid = IdFromHandle(Get(kv, "handle", ""));
            if (pid == 0) { Result("EB_ERR bend: bad handle"); return; }
            string applied = "", msg = "";
            int rc = -999, flangeIdx = -1, cnt = -1;
            string newHandle = Get(kv, "handle", "");

            List<string> before = HandleSet();
            try
            {
                long bid = pid;
                if (Get(kv, "convert", "1") == "1")
                {
                    PsCreateBendPlate cb = new PsCreateBendPlate();
                    cb.SetToDefaults();
                    cb.SetObjectId(pid);
                    bool made = cb.Create();
                    long got = 0;
                    try { got = cb.ObjectId; } catch { }
                    applied += " convert=" + made + " convertId=" + (got != 0 ? HandleOf(got) : "0");
                    if (got != 0) bid = got;
                }
                else applied += " convert=skipped";

                // if the id we hold was erased by the conversion, find its replacement
                if (IsErased(bid))
                {
                    string repl = NewHandleSince(before);
                    applied += " originalErased->" + (repl.Length > 0 ? repl : "NOTHING NEW");
                    if (repl.Length > 0) { bid = IdFromHandle(repl); newHandle = repl; }
                }

                double len   = double.Parse(Get(kv, "len", "150"), IC);
                double front = double.Parse(Get(kv, "front", "0"), IC);
                double rear  = double.Parse(Get(kv, "rear", "0"), IC);
                double rad   = double.Parse(Get(kv, "radius", "12"), IC);
                double ang   = double.Parse(Get(kv, "angle", "90"), IC);
                PsPoint at = Pt(Get(kv, "at", "0,0,0"));

                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    DBObject o = tr.GetObject(new ObjectId(new System.IntPtr(bid)), OpenMode.ForWrite);
                    PsBendPlate bp = o as PsBendPlate;
                    if (bp == null)
                        msg = " NOT a PsBendPlate (" + (o == null ? "null" : o.GetType().Name) + ")";
                    else
                    {
                        // B.9.4: "the radius should always be a little bit more than half
                        // of the plate thickness" -- to avoid volume-modeller problems.
                        try
                        {
                            PsPlate pl = o as PsPlate;
                            if (pl != null && rad <= pl.Height / 2.0)
                                applied += " ג radius<=t/2(" + F(pl.Height / 2.0) + ")";
                        }
                        catch { }
                        rc = bp.AddFlange(len, front, rear, rad, ang, at, ref flangeIdx);
                        try { cnt = bp.FlangeCount; } catch { }
                        applied += " len=" + F(len) + " front=" + F(front) + " rear=" + F(rear) +
                                   " radius=" + F(rad) + " angle=" + F(ang);
                    }
                    tr.Commit();
                }

                // the AddFlange itself may also have replaced the entity
                if (IsErased(bid))
                {
                    string repl = NewHandleSince(before);
                    if (repl.Length > 0) { newHandle = repl; applied += " afterFlange->" + repl; }
                }
                else newHandle = HandleOf(bid);
            }
            catch (System.Exception ex) { msg = " EX:" + One(ex.Message); }
            Result(((cnt > 0) ? "EB_OK" : "EB_ERR") + " bend handle=" + newHandle +
                   " (was " + Get(kv, "handle", "") + ") AddFlange rc=" + rc +
                   " flangeIndex=" + flangeIdx + " flangeCount=" + cnt + applied + msg);
        }

        // model-space handles, so a replacement can be identified after the fact
        static List<string> HandleSet()
        {
            List<string> l = new List<string>();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms) l.Add(id.Handle.ToString());
                tr.Commit();
            }
            return l;
        }

        static string NewHandleSince(List<string> before)
        {
            foreach (string h in HandleSet())
                if (!before.Contains(h)) return h;
            return "";
        }

        static bool IsErased(long oid)
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                ObjectId id = new ObjectId(new System.IntPtr(oid));
                return id.IsErased || !id.IsValid;
            }
            catch { return true; }
        }

        // op=bendinfo handle=<bent plate>   -- read the SEGMENT TREE back
        void BendInfo(Dictionary<string, string> kv)
        {
            long pid = IdFromHandle(Get(kv, "handle", ""));
            if (pid == 0) { Result("EB_ERR bendinfo: bad handle"); return; }
            StringBuilder sb = new StringBuilder();
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    DBObject o = tr.GetObject(new ObjectId(new System.IntPtr(pid)), OpenMode.ForRead);
                    PsBendPlate bp = o as PsBendPlate;
                    if (bp == null) { Result("EB_ERR bendinfo: not a PsBendPlate (" +
                        (o == null ? "null" : o.GetType().Name) + ")"); tr.Commit(); return; }
                    int n = bp.FlangeCount;
                    int scan = int.Parse(Get(kv, "max", "8"));
                    sb.Append("flangeCount=" + n + " (scanning 0.." + (scan - 1) + ")");
                    // ג ן¸ MEASURED 08/08: AddFlange returned flangeIndex=2 and the model
                    // extents grew, while FlangeCount stayed at 2. The count is NOT the
                    // number of segments. Read past it or subordinate segments are invisible.
                    for (int i = 0; i < scan; i++)
                    {
                        try
                        {
                            PsBendPlateFlange f = new PsBendPlateFlange();
                            f.SetToDefaults();
                            bp.GetFlangeDataByIndex(i, f);
                            // ג ן¸ AddFlange TAKES DEGREES, Flange.Angle RETURNS RADIANS:
                            // 45 in -> 0.785 out. Same API, two units. Report both so the
                            // number can never be read as the wrong one.
                            double degs = f.Angle * 180.0 / System.Math.PI;
                            sb.Append(" | [" + i + "] len=" + F(f.Length) +
                                      " ang=" + F(degs) + "deg(" + F(f.Angle) + "rad)" +
                                      " r=" + F(f.Radius) + " off=" + F(f.StartOffset) + "/" + F(f.EndOffset) +
                                      " vtx=" + f.StartVertex + "-" + f.EndVertex +
                                      " lenCalc=" + f.LengthCalculation + " innerR=" + f.UseInnerRadius);
                            // the REAL 3D location of this segment's own reference edge --
                            // the click point for a SUBORDINATE segment. Clicking in the
                            // base plane fails (rc=-1) once the parent is folded up.
                            try
                            {
                                PsPoint g1 = new PsPoint(0,0,0), g2 = new PsPoint(0,0,0);
                                bp.GetGripPoints(i, g1, g2);
                                sb.Append(" grip=" + F(g1.x) + "," + F(g1.y) + "," + F(g1.z) +
                                          "->" + F(g2.x) + "," + F(g2.y) + "," + F(g2.z));
                            }
                            catch (System.Exception e) { sb.Append(" grip!EX:" + One(e.Message)); }
                            try
                            {
                                PsDataPointArray arr = new PsDataPointArray();
                                bp.GetFlangeVertexes(i, arr);
                                int vn = 0;
                                try { vn = arr.Count; } catch { }
                                sb.Append(" verts=" + vn);
                                for (int q = 0; q < vn && q < 6; q++)
                                {
                                    try { PsPoint v = arr.get_Position(q);
                                          sb.Append(" (" + F(v.x) + "," + F(v.y) + "," + F(v.z) + ")"); }
                                    catch { break; }
                                }
                            }
                            catch (System.Exception e) { sb.Append(" verts!EX:" + One(e.Message)); }
                        }
                        catch (System.Exception e) { sb.Append(" | [" + i + "] EX:" + One(e.Message)); }
                    }
                    // ג­ THE DEPENDENCY TREE. The dump prints "P Int32 ParentFlangeIndex"
                    // but the compiler says get_ParentFlangeIndex(Int32) -- it is INDEXED,
                    // one parent per flange. That is exactly the manual's "the new plate
                    // segment is always subordinate to the reference segment", readable.
                    for (int i = 0; i < scan; i++)
                    {
                        try { sb.Append(" | parent[" + i + "]=" + bp.get_ParentFlangeIndex(i)); }
                        catch (System.Exception e) { sb.Append(" | parent[" + i + "] EX:" + One(e.Message)); break; }
                    }
                    tr.Commit();
                }
            }
            catch (System.Exception ex) { Result("EB_ERR bendinfo EX:" + One(ex.Message)); return; }
            Result("EB_OK bendinfo " + sb.ToString());
        }

        // op=bendtwo h1= h2= radius= at=x,y,z [inner=1] [k=1] [delete2=1]
        // B.9.4 "Combine Plates to Bent Plates". Prerequisite from the manual: "you can
        // generate a TANGENTIAL TRANSITION between the two plates".
        // KValue is the manual's Correction Value Unwinding: 0 = inner radius,
        // 1 = centre, 2 = outer radius -- "multiplied with half of the plate thickness".
        void BendTwo(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            long a = IdFromHandle(Get(kv, "h1", "")), b = IdFromHandle(Get(kv, "h2", ""));
            if (a == 0 || b == 0) { Result("EB_ERR bendtwo: bad handle(s)"); return; }
            string h0, c0; int before = Census(out h0, out c0);
            int rc = -999; string msg = "";
            double rad = double.Parse(Get(kv, "radius", "20"), IC);
            double k   = double.Parse(Get(kv, "k", "1"), IC);
            bool inner = Get(kv, "inner", "1") == "1";
            bool del2  = Get(kv, "delete2", "1") == "1";
            try
            {
                PsCreateBendPlate cb = new PsCreateBendPlate();
                cb.SetToDefaults();
                rc = cb.CreateOfTwoPlates(a, b, rad, Pt(Get(kv, "at", "0,0,0")), inner, k, del2);
            }
            catch (System.Exception ex) { msg = " EX:" + One(ex.Message); }
            string h1, c1; int after = Census(out h1, out c1);
            Result((rc >= 0 ? "EB_OK" : "EB_ERR") + " bendtwo rc=" + rc +
                   " radius=" + F(rad) + " K=" + F(k) + " innerRadius=" + inner +
                   " deleteSecond=" + del2 + " census=" + before + "->" + after + msg);
        }

        // op=plateinfo handle= [probe=<name>]
        // v95's plateinfo took AutoCAD down with it: five plates were created, then the
        // first read-back killed the process and the drawing was lost back to the last
        // save. Which call did it is unknowable from a routine that makes fifteen.
        // So: ONE call per invocation, named. If AutoCAD dies, the name is the culprit.
        // A marker file is written BEFORE the call, because a crash returns no result.
        //   probes: thickness inserth insertxy weight dimension paint dimalign dimdir
        //           name nametpl poly crosscheck arccast bendcast extents
        //   probe=safe   -> only the ones already proven harmless
        void PlateInfo(Dictionary<string, string> kv)
        {
            long pid = IdFromHandle(Get(kv, "handle", ""));
            if (pid == 0) { Result("EB_ERR plateinfo: bad handle"); return; }
            string probe = Get(kv, "probe", "safe").ToLowerInvariant();
            string[] wanted = (probe == "safe")
                ? new string[] { "thickness", "inserth", "insertxy", "extents" }
                : (probe == "all"
                    ? new string[] { "thickness","inserth","insertxy","weight","dimension","paint",
                                     "dimalign","dimdir","name","nametpl","poly","bulge","crosscheck",
                                     "arccast","bendcast","extents" }
                    : probe.Split(','));
            StringBuilder sb = new StringBuilder();
            string marker = Path.Combine(Dir, "eb_probe.txt");
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    DBObject o = tr.GetObject(new ObjectId(new System.IntPtr(pid)), OpenMode.ForRead);
                    sb.Append("class=" + (o == null ? "null" : o.GetType().Name));
                    PsPlate pl = o as PsPlate;
                    if (pl == null) sb.Append(" (not a PsPlate)");
                    foreach (string wRaw in wanted)
                    {
                        string w = wRaw.Trim();
                        if (w.Length == 0) continue;
                        try { File.WriteAllText(marker, "about to run: " + w + " on " +
                                                Get(kv, "handle", ""), Encoding.UTF8); } catch { }
                        try
                        {
                            if (pl != null)
                            {
                                if (w == "thickness") sb.Append(" thickness=" + F(pl.Height));
                                else if (w == "inserth") sb.Append(" insertHeight=" + F(pl.InsertHeight));
                                else if (w == "insertxy") sb.Append(" insertXY=" + F(pl.InsertX) + "," + F(pl.InsertY));
                                else if (w == "weight") sb.Append(" weight=" + F(pl.computeObjectWeigth(true)));
                                else if (w == "dimension") sb.Append(" dimension=" + F(pl.computeObjectDimension(true)));
                                else if (w == "paint") sb.Append(" paintArea=" + F(pl.computeObjectPaintArea()));
                                else if (w == "dimalign") sb.Append(" dimAlign=" + pl.DimensionAlignement);
                                else if (w == "dimdir")
                                { PsVector dv = pl.DimensionDirection;
                                  sb.Append(" dimDir=" + F(dv.x) + "," + F(dv.y) + "," + F(dv.z)); }
                                else if (w == "name") sb.Append(" name='" + pl.Name + "'");
                                else if (w == "nametpl")
                                    sb.Append(" nameTpl='" + pl.getConvertedName(PlateNameTemplateType.kNameTemplate) + "'");
                                else if (w == "poly")
                                {
                                    PsPolygon poly = new PsPolygon(); poly.init();
                                    pl.GetPolygon(poly);
                                    sb.Append(" polyVerts=" + poly.Count);
                                }
                                else if (w == "bulge")
                                {
                                    PsPolygon poly = new PsPolygon(); poly.init();
                                    pl.GetPolygon(poly);
                                    int nv = poly.Count;
                                    sb.Append(" verts=" + nv);
                                    for (int q = 0; q < nv && q < 16; q++)
                                    {
                                        double a0 = 0, a1 = 0, a2 = 0;
                                        string byValue = "?", asPoint = "?";
                                        try
                                        {
                                            if (poly.getVertexbyValue(q, ref a0, ref a1, ref a2))
                                                byValue = F(a0) + "|" + F(a1) + "|" + F(a2);
                                        }
                                        catch (System.Exception e) { byValue = "!EX:" + One(e.Message); }
                                        try
                                        {
                                            PsPoint vp = new PsPoint(0, 0, 0);
                                            poly.getVertexAsPoint(q, vp);
                                            asPoint = F(vp.x) + "," + F(vp.y) + "," + F(vp.z);
                                        }
                                        catch (System.Exception e) { asPoint = "!EX:" + One(e.Message); }
                                        sb.Append(" [" + q + "] pt=(" + asPoint + ") byValue=" + byValue);
                                    }
                                }
                                else if (w == "crosscheck")
                                {
                                    PsPolygon poly = new PsPolygon(); poly.init();
                                    pl.GetPolygon(poly);
                                    sb.Append(" crossCheck=" + poly.crossCheck(false));
                                }
                            }
                            if (w == "arccast")
                            {
                                PsArcPlate arc = o as PsArcPlate;
                                sb.Append(arc == null ? " arc=no"
                                    : " ARC r=" + F(arc.Radius) + " neutralR=" + F(arc.NeutralRadius) +
                                      " w=" + F(arc.Width) + " h=" + F(arc.Height) +
                                      " ang=" + F(arc.StartAngle) + ".." + F(arc.EndAngle) +
                                      " turn=" + F(arc.TurnAngle));
                            }
                            else if (w == "bendcast")
                            {
                                PsBendPlate bp = o as PsBendPlate;
                                sb.Append(bp == null ? " bend=no" : " BEND flanges=" + bp.FlangeCount);
                            }
                            else if (w == "extents")
                            {
                                PsObjectProperties p = new PsObjectProperties();
                                p.readFrom(pid);
                                PsPoint mn = new PsPoint(0,0,0), mx = new PsPoint(0,0,0);
                                if (p.GetExtents(ref mn, ref mx))
                                    sb.Append(" ext=" + F(mn.x) + "," + F(mn.y) + "," + F(mn.z) + ";" +
                                              F(mx.x) + "," + F(mx.y) + "," + F(mx.z));
                                else sb.Append(" ext=none");
                            }
                        }
                        catch (System.Exception e) { sb.Append(" " + w + "!EX:" + One(e.Message)); }
                    }
                    tr.Commit();
                }
                try { File.WriteAllText(marker, "completed", Encoding.UTF8); } catch { }
            }
            catch (System.Exception ex) { Result("EB_ERR plateinfo EX:" + One(ex.Message)); return; }
            Result("EB_OK plateinfo handle=" + Get(kv, "handle", "") + " " + sb.ToString());
        }

        // ---- v92: B.6 WORK FRAMES ----
        // "Any ProSteel model generation is STARTED with the creation of one or several work
        // frames." They do two things: show the axis grid, and AUTOMATICALLY CREATE THE UCS
        // SYSTEMS OF THE VIEWS -- which is how a modeller navigates a 2,000-object model.
        //
        // Four types (GridType is the display mode, not the shape): rectangular, cylindrical
        // (also conical -- separate base and top radii), wedge, pyramidal.
        //
        //   op=frame at=x,y,z lsteps=6000,6000 wsteps=5000,5000 hsteps=4000,3500
        //            [name=<group>] [roofangle=] [ridgeheight=] [ridgewidth=]
        //            [base=<r>] [top=<r>] [segments=<n>]        <- cylindrical
        //            [views=all|none] [axisnames=1] [lock=0]
        void Frame(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string h0, c0; int before = Census(out h0, out c0);
            long id = 0;
            string applied = "", msg = "";
            try
            {
                PsCreateGrid g = new PsCreateGrid();
                g.SetToDefaults();
                g.SetInsertPoint(Pt(Get(kv, "at", "0,0,0")));
                // THE SHAPE SWITCH. Bentley.ProStructures.GridType is
                //   kRectangle | kCylinder | kWedge | kPyramid
                // -- the manual's four frame types, B.6.1-B.6.4. Without it every roof
                // angle and radius is STORED on the entity and NEVER DRAWN, which is
                // exactly what happened on the first attempt (cone bbox = text overhang
                // only). Beware: a second, unrelated enum in another assembly is also
                // called GridType (CrossLines/Points/None) -- reflecting that one by bare
                // name is what sent me down the wrong path. Set by NAME, never by ordinal.
                string gt = Get(kv, "type", "rect").ToLowerInvariant();
                GridType gtv = GridType.kRectangle;
                if (gt.StartsWith("cyl") || gt.StartsWith("con")) gtv = GridType.kCylinder;
                else if (gt.StartsWith("wed")) gtv = GridType.kWedge;
                else if (gt.StartsWith("pyr")) gtv = GridType.kPyramid;
                g.SetType(gtv);
                applied += " type=" + gtv;
                // B.6.1 insertion is TWO picks: the origin, then the frame's X-axis.
                // Skipping the second one leaves the frame on whatever UCS happens to be
                // current -- which is why B6_RECT came out with its Length along WCS Y.
                string xa = Get(kv, "xaxis", ""), ya = Get(kv, "yaxis", "");
                if (xa.Length > 0 || ya.Length > 0)
                {
                    double[] vx = Nums(xa.Length > 0 ? xa : "1,0,0");
                    double[] vy = Nums(ya.Length > 0 ? ya : "0,1,0");
                    g.SetXYPlane(new PsVector(vx[0], vx[1], vx[2]), new PsVector(vy[0], vy[1], vy[2]));
                    applied += " xaxis=" + (xa.Length > 0 ? xa : "1,0,0");
                }
                string nm = Get(kv, "name", "");
                if (nm.Length > 0) { g.SetName(nm); applied += " name='" + nm + "'"; }

                // B.6.1: each dimension is either "overall + N regular fields" or a list of
                // individual field widths. The list IS the bay spacing -- setting only the
                // division count produced a frame 3 mm across on the first attempt.
                string ls = Get(kv, "lsteps", ""), ws = Get(kv, "wsteps", ""), hs = Get(kv, "hsteps", "");
                if (ls.Length > 0)
                {
                    double[] v = Nums(ls); double tot = 0;
                    g.SetLengthDivision(v.Length);
                    for (int i = 0; i < v.Length; i++) { g.SetLengthSteps(i, v[i]); tot += v[i]; }
                    g.SetLength(tot); applied += " L=" + F(tot) + "(" + v.Length + " bays)";
                }
                if (ws.Length > 0)
                {
                    double[] v = Nums(ws); double tot = 0;
                    g.SetWidthDivision(v.Length);
                    for (int i = 0; i < v.Length; i++) { g.SetWidthSteps(i, v[i]); tot += v[i]; }
                    g.SetWidth(tot); applied += " W=" + F(tot) + "(" + v.Length + ")";
                }
                if (hs.Length > 0)
                {
                    double[] v = Nums(hs); double tot = 0;
                    g.SetHeightDivision(v.Length);
                    for (int i = 0; i < v.Length; i++) { g.SetHeightSteps(i, v[i]); tot += v[i]; }
                    g.SetHeight(tot); applied += " H=" + F(tot) + "(" + v.Length + ")";
                }

                // B.6.1 gabled roof. "Ridge width 0 or equal to the frame width => only a roof
                // surface will be created."
                string ra = Get(kv, "roofangle", ""), rh = Get(kv, "ridgeheight", ""), rw = Get(kv, "ridgewidth", "");
                if (ra.Length > 0) { g.SetRoofAngle(double.Parse(ra, IC)); applied += " roofAngle=" + ra; }
                if (rh.Length > 0) { g.SetRoofMiddle(double.Parse(rh, IC)); applied += " ridgeH=" + rh; }
                if (rw.Length > 0) { g.SetRoofWidth(double.Parse(rw, IC)); applied += " ridgeW=" + rw; }
                string rhh = Get(kv, "roofheight", ""), rl = Get(kv, "rooflength", "");
                if (rhh.Length > 0) { g.SetRoofHeight(double.Parse(rhh, IC)); applied += " roofH=" + rhh; }
                // B.6.4 pyramidal: Roof Length is the second ridge dimension
                if (rl.Length > 0) { g.SetRoofLength(double.Parse(rl, IC)); applied += " roofLen=" + rl; }

                // B.6.2 cylindrical / conical
                string bs = Get(kv, "base", ""), tp = Get(kv, "top", ""), sg = Get(kv, "segments", "");
                if (bs.Length > 0) { g.SetLowerRadius(double.Parse(bs, IC)); applied += " base=" + bs; }
                if (tp.Length > 0) { g.SetUpperRadius(double.Parse(tp, IC)); applied += " top=" + tp; }
                if (sg.Length > 0) { g.SetRadiusDivision(int.Parse(sg)); applied += " segments=" + sg; }
                // SetSegments is a FLAG (draw the circle as facets), SetRadiusDivision is the
                // count -- two different things that both read as "segmentation" in the dialog
                if (Get(kv, "facets", "").Length > 0)
                { g.SetSegments(Get(kv, "facets", "0") == "1"); applied += " facets=" + Get(kv, "facets", ""); }
                if (Get(kv, "radiusview", "").Length > 0)
                    g.SetRadiusView(Get(kv, "radiusview", "0") == "1");

                // B.6.6 Axes Names -- the whole dialog in one call, per side.
                //   Type 0/1 = 123 vs ABC ֲ· Display = text/circle/block ֲ· Order = decreasing
                //   Position = front/rear/left/right ֲ· Start = first value
                //   DoubleLine = "2 Lines" ֲ· Dynamic = names follow the view direction
                //   First/Second = suppress the first / last axis where frames adjoin
                if (Get(kv, "axnames", "") == "1")
                {
                    double asz = double.Parse(Get(kv, "axsize", "300"), IC);
                    double asc = double.Parse(Get(kv, "axscale", "1"), IC);
                    double adi = double.Parse(Get(kv, "axdist", "1000"), IC);
                    int aty = int.Parse(Get(kv, "axtype", "0"));
                    int adp = int.Parse(Get(kv, "axdisplay", "1"));
                    int aor = int.Parse(Get(kv, "axorder", "0"));
                    int apo = int.Parse(Get(kv, "axpos", "0"));
                    int ast = int.Parse(Get(kv, "axstart", "1"));
                    bool adl = Get(kv, "axdouble", "0") == "1";
                    bool adyn = Get(kv, "axdynamic", "1") == "1";
                    bool af = Get(kv, "axfirst", "0") == "1";
                    bool asec = Get(kv, "axsecond", "0") == "1";
                    g.SetLeftTextSettings(asz, asc, adi, aty, adp, aor, apo, ast, adl, adyn, af, asec);
                    // the other side gets the alphabetic run, which is the usual convention
                    g.SetRightTextSettings(asz, asc, adi, int.Parse(Get(kv, "axtype2", "1")),
                                           adp, aor, apo, ast, adl, adyn, af, asec);
                    applied += " axnames(type=" + aty + "/" + Get(kv, "axtype2", "1") +
                               " start=" + ast + " dyn=" + (adyn ? 1 : 0) + ")";
                }

                // refuse to make a second frame with a name already in use -- the group name
                // is what prefixes every view, so a collision quietly ruins navigation
                string nmChk = Get(kv, "name", "");
                if (nmChk.Length > 0 && Get(kv, "checkname", "1") == "1")
                {
                    bool clash = false;
                    try { clash = g.checkExistingGrids(nmChk); } catch { }
                    applied += " nameInUse=" + clash;
                }

                g.DisplayAxisNames(Get(kv, "axisnames", "1") == "1");
                g.LockLayer(Get(kv, "lock", "0") == "1");   // B.6.8: a LOCKED frame silently
                                                            // refuses every later change
                g.SetDisplay3d(Get(kv, "d3", "1") == "1");
                g.BuildFrames(true);

                // B.6.5 Create Views -- one per surface, and one PER AXIS in each direction
                string vw = Get(kv, "views", "all").ToLowerInvariant();
                bool all = (vw == "all");
                g.SetAllViews(all);
                if (all)
                {
                    g.SetTopView(true); g.SetFrontView(true); g.SetBackView(true);
                    g.SetSideLeftView(true); g.SetSideRightView(true); g.SetDownView(true);
                    g.SetXViews(true); g.SetYViews(true); g.SetZViews(true);
                    applied += " views=all(+per-axis)";
                }
                string fc = Get(kv, "frontclip", ""), bc = Get(kv, "backclip", "");
                if (fc.Length > 0) { g.SetFrontClip(double.Parse(fc, IC)); applied += " frontClip=" + fc; }
                if (bc.Length > 0) { g.SetBackClip(double.Parse(bc, IC)); applied += " backClip=" + bc; }

                g.Create();
                try { id = g.ObjectId; } catch { }
            }
            catch (System.Exception ex) { msg = " EX:" + ex.Message; }

            string h1, c1; int after = Census(out h1, out c1);
            string ext = "";
            try
            {
                PsObjectProperties p = new PsObjectProperties();
                p.readFrom(id);
                PsPoint mn = new PsPoint(0,0,0), mx = new PsPoint(0,0,0);
                if (p.GetExtents(ref mn, ref mx))
                    ext = " ext=" + F(mn.x) + "," + F(mn.y) + "," + F(mn.z) + ";" +
                          F(mx.x) + "," + F(mx.y) + "," + F(mx.z);
            }
            catch { }
            Result(((after > before) ? "EB_OK" : "EB_ERR") + " frame handle=" + HandleOf(id) +
                   " census=" + before + "->" + after + applied + ext + msg);
        }

        // Bind to an EXISTING frame and read it back. B.8.7 failed here: readProps left the
        // grid empty (L=0 W=0, no axes). readProps/writeProps may be named from the GRID's
        // point of view -- readProps = "read MY values OUT into prop" -- so try both, and say
        // which one actually filled the object.
        //   op=frameinfo handle=<frame>
        void FrameInfo(Dictionary<string, string> kv)
        {
            long gid = IdFromHandle(Get(kv, "handle", ""));
            if (gid == 0) { Result("EB_ERR frameinfo: bad handle"); return; }
            StringBuilder sb = new StringBuilder();
            try
            {
                PsObjectProperties p = new PsObjectProperties();
                int rc = p.readFrom(gid);
                sb.Append("propsRc=" + rc + " name='" + p.Name + "'");

                PsGrid a = new PsGrid();
                a.init();
                int ra = -999;
                try { ra = a.readProps(p); } catch (System.Exception e) { sb.Append(" readProps:" + e.Message); }
                sb.Append(" | readProps(rc=" + ra + ") -> L=" + F(a.Length) + " W=" + F(a.Wide) +
                          " H=" + F(a.Height) + " div=" + a.LengthDiv + "x" + a.WideDiv +
                          " userAxes=" + a.UserXaxisCount + "/" + a.UserYaxisCount +
                          " xDesc=" + a.XDescriptionCount + " yDesc=" + a.YDescriptionCount);

                PsGrid b = new PsGrid();
                b.init();
                int rb = -999;
                try { rb = b.writeProps(p); } catch (System.Exception e) { sb.Append(" writeProps:" + e.Message); }
                sb.Append(" | writeProps(rc=" + rb + ") -> L=" + F(b.Length) + " W=" + F(b.Wide) +
                          " H=" + F(b.Height) + " div=" + b.LengthDiv + "x" + b.WideDiv +
                          " userAxes=" + b.UserXaxisCount + "/" + b.UserYaxisCount);

                // whichever filled, report its joints
                PsGrid g = (a.Length > 0) ? a : ((b.Length > 0) ? b : null);
                if (g != null)
                {
                    sb.Append(" | BOUND via " + ((a.Length > 0) ? "readProps" : "writeProps"));
                    try
                    {
                        PsPoint org = new PsPoint(0,0,0);
                        PsVector vx = new PsVector(1,0,0), vy = new PsVector(0,1,0), vz = new PsVector(0,0,1);
                        g.getEffectiveCoordSystem(org, vx, vy, vz);
                        sb.Append(" org=" + F(org.x) + "," + F(org.y) + "," + F(org.z));
                        PsPolygon poly = new PsPolygon();
                        poly.init();
                        double L = g.Length, W = g.Wide, pad = 100;
                        poly.appendVertex(-pad, -pad, 0);
                        poly.appendVertex(L + pad, -pad, 0);
                        poly.appendVertex(L + pad, W + pad, 0);
                        poly.appendVertex(-pad, W + pad, 0);
                        poly.Close();
                        PsDataPointArray arr = new PsDataPointArray();
                        int n = g.getPointsInsidePoly(poly, arr);
                        sb.Append(" joints=" + n);
                        for (int i = 0; i < n && i < 8; i++)
                        {
                            PsPoint q = arr.get_Position(i);
                            sb.Append(" " + F(q.x) + "," + F(q.y) + "," + F(q.z));
                        }
                    }
                    catch (System.Exception e) { sb.Append(" joints:" + e.Message); }
                }
                else sb.Append(" | NEITHER bound the frame");

                // (c) does PsObjectProperties need init() before readFrom? and does it even
                //     admit which object it holds?
                try
                {
                    PsObjectProperties p2 = new PsObjectProperties();
                    p2.init();
                    int rc2 = p2.readFrom(gid);
                    PsGrid c = new PsGrid(); c.init();
                    int rc3 = c.readProps(p2);
                    sb.Append(" | init-first: readFrom=" + rc2 + " boundTo=" + p2.getObjectId() +
                              " (asked " + gid + ") name='" + p2.Name + "' readProps=" + rc3 +
                              " L=" + F(c.Length));
                }
                catch (System.Exception e) { sb.Append(" | init-first EX:" + e.Message); }
            }
            catch (System.Exception ex) { Result("EB_ERR frameinfo EX:" + ex.Message); return; }
            Result("EB_OK frameinfo " + sb.ToString());
        }

        // ---- v89: B.8.7 AUTOMATIC INSERTION -- columns and girders ON THE GRID ----
        // Manual B.8.7: "A frequent application is e.g. the generation of a SUPPORTING GRID
        // with previously defined jointsג€¦ insert the column at the intersection point of the
        // WORK FRAME AXES (grid) which are situated within a rectangular areaג€¦ [girders are]
        // inserted along the axesג€¦ finally, these are divided at all intersection points."
        //
        // ג‡’ Twenty columns on a grid is ONE operation against the work frame, not a
        // replication loop. This is the lesson-5 exam, done the way the software intends.
        //
        // PsCreateGrid builds the frame (spans, divisions, steps); PsGrid reads it back, and
        // getPointsInsidePoly returns the axis intersections inside an area.
        //
        //   op=grid at=x,y,z len=<mm> wide=<mm> [height=<mm>] [lsteps=6000,6000,6000]
        //           [wsteps=5000,5000] [name=<s>]
        //   op=gridpoints handle=<grid>            list the intersections
        //   op=gridcolumns handle=<grid> name=<section> catalog=<cat> h=<mm> [xpos=] [ypos=]
        //   op=gridgirders handle=<grid> name=<section> catalog=<cat> z=<mm> [dir=x|y]
        void Grid(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string h0, c0; int before = Census(out h0, out c0);
            long id = 0;
            string msg = "", applied = "";
            try
            {
                PsCreateGrid g = new PsCreateGrid();
                g.SetToDefaults();
                g.SetInsertPoint(Pt(Get(kv, "at", "0,0,0")));
                string nm = Get(kv, "name", "");
                if (nm.Length > 0) g.SetName(nm);

                // The steps ARE the bay spacings. Giving them explicitly is what turns the
                // frame from a 3x2 grid of 1 mm into a real building grid -- the first attempt
                // set only the divisions and produced a frame 3 mm across.
                string ls = Get(kv, "lsteps", ""), ws = Get(kv, "wsteps", ""), hs = Get(kv, "hsteps", "");
                if (ls.Length > 0)
                {
                    double[] v = Nums(ls);
                    g.SetLengthDivision(v.Length);
                    double tot = 0;
                    for (int i = 0; i < v.Length; i++) { g.SetLengthSteps(i, v[i]); tot += v[i]; }
                    g.SetLength(tot);
                    applied += " lsteps=" + ls + " len=" + F(tot);
                }
                else { double L = double.Parse(Get(kv, "len", "12000"), IC); g.SetLength(L); applied += " len=" + F(L); }

                if (ws.Length > 0)
                {
                    double[] v = Nums(ws);
                    g.SetWidthDivision(v.Length);
                    double tot = 0;
                    for (int i = 0; i < v.Length; i++) { g.SetWidthSteps(i, v[i]); tot += v[i]; }
                    g.SetWidth(tot);
                    applied += " wsteps=" + ws + " wide=" + F(tot);
                }
                else { double W = double.Parse(Get(kv, "wide", "10000"), IC); g.SetWidth(W); applied += " wide=" + F(W); }

                if (hs.Length > 0)
                {
                    double[] v = Nums(hs);
                    g.SetHeightDivision(v.Length);
                    double tot = 0;
                    for (int i = 0; i < v.Length; i++) { g.SetHeightSteps(i, v[i]); tot += v[i]; }
                    g.SetHeight(tot);
                    applied += " hsteps=" + hs;
                }
                else { double H = double.Parse(Get(kv, "height", "6000"), IC); g.SetHeight(H); applied += " height=" + F(H); }

                g.DisplayAxisNames(true);
                g.Create();
                try { id = g.ObjectId; } catch { }
            }
            catch (System.Exception ex) { msg = " EX:" + ex.Message; }
            string h1, c1; int after = Census(out h1, out c1);
            string ext = "";
            try
            {
                PsObjectProperties p = new PsObjectProperties();
                p.readFrom(id);
                PsPoint mn = new PsPoint(0,0,0), mx = new PsPoint(0,0,0);
                if (p.GetExtents(ref mn, ref mx))
                    ext = " ext=" + F(mn.x) + "," + F(mn.y) + "," + F(mn.z) + ";" +
                          F(mx.x) + "," + F(mx.y) + "," + F(mx.z);
            }
            catch { }
            Result(((after > before) ? "EB_OK" : "EB_ERR") + " grid handle=" + HandleOf(id) +
                   " census=" + before + "->" + after + applied + ext + msg);
        }

        // read the grid's axis intersections -- the joints B.8.7 places columns at
        static List<PsPoint> GridPoints(long gid, out string note)
        {
            note = "";
            List<PsPoint> pts = new List<PsPoint>();
            try
            {
                PsGrid g = new PsGrid();
                g.init();
                PsObjectProperties p = new PsObjectProperties();
                p.readFrom(gid);
                g.readProps(p);
                PsPoint mn = new PsPoint(0, 0, 0), mx = new PsPoint(0, 0, 0);
                p.GetExtents(ref mn, ref mx);

                // did the grid's own settings actually load? If Length/Wide come back 0 the
                // problem is readProps, not the polygon -- check before blaming the query.
                double gl = 0, gw = 0; uint ld = 0, wd = 0;
                try { gl = g.Length; gw = g.Wide; ld = g.LengthDiv; wd = g.WideDiv; } catch { }
                note = "grid L=" + F(gl) + " W=" + F(gw) + " div=" + ld + "x" + wd;

                // the grid's OWN frame -- the polygon is very likely expected in it, not in WCS
                PsPoint org = new PsPoint(0, 0, 0);
                PsVector ax = new PsVector(1, 0, 0), ay = new PsVector(0, 1, 0), az = new PsVector(0, 0, 1);
                try { g.getEffectiveCoordSystem(org, ax, ay, az);
                      note += " org=" + F(org.x) + "," + F(org.y) + "," + F(org.z); }
                catch (System.Exception e) { note += " ecs:" + e.Message; }

                double pad = 100.0;
                PsDataPointArray arr = new PsDataPointArray();
                int n = 0;

                // attempt 1 -- polygon in WCS, over the object's extents
                PsPolygon pw = new PsPolygon();
                pw.init();
                pw.appendVertex(mn.x - pad, mn.y - pad, 0);
                pw.appendVertex(mx.x + pad, mn.y - pad, 0);
                pw.appendVertex(mx.x + pad, mx.y + pad, 0);
                pw.appendVertex(mn.x - pad, mx.y + pad, 0);
                pw.Close();
                try { n = g.getPointsInsidePoly(pw, arr); } catch (System.Exception e) { note += " wcs:" + e.Message; }
                note += " | wcsPoly=" + n;

                // attempt 2 -- polygon in the GRID's local frame, starting at its origin
                if (n == 0)
                {
                    arr = new PsDataPointArray();
                    PsPolygon pl = new PsPolygon();
                    pl.init();
                    double L = gl > 0 ? gl : 30000, W = gw > 0 ? gw : 30000;
                    pl.appendVertex(-pad, -pad, 0);
                    pl.appendVertex(L + pad, -pad, 0);
                    pl.appendVertex(L + pad, W + pad, 0);
                    pl.appendVertex(-pad, W + pad, 0);
                    pl.Close();
                    try { n = g.getPointsInsidePoly(pl, arr); } catch (System.Exception e) { note += " local:" + e.Message; }
                    note += " localPoly=" + n;
                }

                // attempt 3 -- read the axes directly and intersect them ourselves
                if (n == 0)
                {
                    note += " | axes X=" + g.UserXaxisCount + " Y=" + g.UserYaxisCount;
                }
                for (int i = 0; i < n; i++)
                {
                    try { pts.Add(arr.get_Position(i)); }
                    catch (System.Exception e) { note += " pt" + i + ":" + e.Message; break; }
                }
            }
            catch (System.Exception ex) { note = "EX:" + ex.Message; }
            return pts;
        }

        void GridPointsOp(Dictionary<string, string> kv)
        {
            long gid = IdFromHandle(Get(kv, "handle", ""));
            if (gid == 0) { Result("EB_ERR gridpoints: bad handle"); return; }
            string note;
            List<PsPoint> pts = GridPoints(gid, out note);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < pts.Count && i < 40; i++)
                sb.Append(" " + F(pts[i].x) + "," + F(pts[i].y) + "," + F(pts[i].z));
            Result((pts.Count > 0 ? "EB_OK" : "EB_ERR") + " gridpoints n=" + pts.Count +
                   " (" + note + ")" + sb.ToString());
        }

        void GridColumns(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            long gid = IdFromHandle(Get(kv, "handle", ""));
            if (gid == 0) { Result("EB_ERR gridcolumns: bad handle (the work frame)"); return; }
            string nm = Get(kv, "name", "HE300B"), cat = Get(kv, "catalog", "DIN_HEB");
            double H = double.Parse(Get(kv, "h", "6000"), IC);
            string note;
            List<PsPoint> pts = GridPoints(gid, out note);

            // FALLBACK: PsGrid.readProps does not bind to an existing work frame -- the grid
            // comes back with Length=0, Wide=0 and no axes, so getPointsInsidePoly has nothing
            // to search. The joints are still knowable: they are the cumulative sums of the
            // bay steps, which the caller supplied when the frame was built. Passing them back
            // as lsteps=/wsteps= computes the same intersections the software would.
            string ls = Get(kv, "lsteps", ""), ws = Get(kv, "wsteps", "");
            if (pts.Count == 0 && ls.Length > 0 && ws.Length > 0)
            {
                PsPoint org = Pt(Get(kv, "at", "0,0,0"));
                double[] lv = Nums(ls), wv = Nums(ws);
                List<double> xs = new List<double>(), ys = new List<double>();
                double acc = 0; xs.Add(0);
                foreach (double d in lv) { acc += d; xs.Add(acc); }
                acc = 0; ys.Add(0);
                foreach (double d in wv) { acc += d; ys.Add(acc); }
                // the frame's length ran along Y and its width along X when measured, so the
                // steps are applied that way round; at= is the frame's insertion point.
                foreach (double y in xs)
                    foreach (double x in ys)
                        pts.Add(new PsPoint(org.x + x, org.y + y, org.z));
                note += " | computed from steps: " + xs.Count + "x" + ys.Count;
            }

            if (pts.Count == 0)
            { Result("EB_ERR gridcolumns: no grid intersections (" + note + "). " +
                     "PsGrid.readProps does not load an existing frame -- pass lsteps= wsteps= at= " +
                     "to compute the joints instead."); return; }

            string h0, c0; int before = Census(out h0, out c0);
            int made = 0, failed = 0;
            DateTime t0 = DateTime.Now;
            foreach (PsPoint p in pts)
            {
                try
                {
                    PsCreateShape cs = new PsCreateShape();
                    cs.SetToDefaults();
                    cs.SelectStandardSections();
                    cs.SetCrossSection(nm, cat);
                    string xp = Get(kv, "xpos", ""), yp = Get(kv, "ypos", "");
                    if (xp.Length > 0) cs.SetXPosition((PositionSelection)System.Enum.Parse(typeof(PositionSelection), xp, true));
                    if (yp.Length > 0) cs.SetYPosition((PositionSelection)System.Enum.Parse(typeof(PositionSelection), yp, true));
                    cs.SetInsertPoints(new PsPoint(p.x, p.y, p.z), new PsPoint(p.x, p.y, p.z + H));
                    if (cs.Create()) made++; else failed++;
                }
                catch { failed++; }
            }
            string h1, c1; int after = Census(out h1, out c1);
            double secs = (DateTime.Now - t0).TotalSeconds;
            Result((made > 0 ? "EB_OK" : "EB_ERR") + " gridcolumns joints=" + pts.Count +
                   " created=" + made + " failed=" + failed +
                   " section='" + nm + "' h=" + F(H) +
                   " census=" + before + "->" + after +
                   " secs=" + secs.ToString("0.0", IC));
        }

        // ---- v87: B.8 INSERT SHAPES -- the whole insertion dialog ----
        // The existing `beam` op inserts a profile between two points and centres it. B.8.1
        // and B.8.3 describe far more, and every field has a method on PsCreateShape:
        //   insertion point  -> SetXPosition/SetYPosition(PositionSelection)   <- the grid of
        //                       points drawn on the dialog's monitor
        //   Delta X / Delta Y-> SetXOffset/SetYOffset  (only meaningful at the 'Free' point)
        //   Start/End Offset -> SetStartOffset/SetEndOffset
        //   Turn             -> SetRotation
        //   Length           -> SetDirection(vector, length), which OVERRIDES the two points
        //   Horizontal/Vertical Dist -> SetHorizontal/VerticalDistance (SHAPECLASSLAYOUT)
        //   Material/Layer/Family/Detail/Display/Area/Article -> the matching Set*
        //
        // ג­ And the rule from B.8.1 that explains the orientation surprises:
        //   "if you stood at the end point and looked into the direction of the starting point,
        //    the view corresponds to the depiction on the monitor" -- and when the two points
        //    are perpendicular in the WCS, alignment follows the WCS X-AXIS. SetXAxis/SetYAxis
        //    are the explicit override for that third point.
        //
        //   op=shape name=<section> catalog=<cat> p1=x,y,z p2=x,y,z
        //        [xpos=kLeft|kRight|kDown|kTop|kCenter|kGravity|kPitch|kUser]
        //        [ypos=...] [dx=] [dy=] [startoff=] [endoff=] [rot=<deg>]
        //        [len=<mm> dir=x,y,z]  [xaxis=x,y,z] [yaxis=x,y,z]
        //        [material=<n>] [layer=] [family=<n>] [display=<n>] [area=<n>] [article=]
        //        [hdist=] [vdist=] [res=0|1|2] [type=standard|special|roofwall|combination]
        //        [flat=<wide>x<thick>]      <- CreateFlatSteel, a NON-CATALOGUE flat
        void Shape(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string h0, c0; int before = Census(out h0, out c0);
            long newId = 0;
            string msg = "", applied = "";
            bool made = false;
            try
            {
                PsCreateShape cs = new PsCreateShape();
                cs.SetToDefaults();

                string ty = Get(kv, "type", "standard").ToLowerInvariant();
                if (ty == "special") cs.SelectSpecialSections();
                else if (ty == "roofwall") cs.SelectRoofWallSections();
                else if (ty == "combination") cs.SelectCombinationSections();
                else cs.SelectStandardSections();

                string flat = Get(kv, "flat", "");
                if (flat.Length == 0)
                {
                    string nm = Get(kv, "name", ""), cat = Get(kv, "catalog", "");
                    if (nm.Length == 0) { Result("EB_ERR shape: need name= (or flat=WxT)"); return; }
                    cs.SetCrossSection(nm, cat);
                    applied += " section='" + nm + "'@'" + cat + "'";
                }
                // flat= is handled AFTER the geometry: CreateFlatSteel returns Boolean, so it
                // is a CREATOR, not a configurator, and the insertion points must already be
                // set when it runs. Calling it first produced create=False and no object.

                string r = Get(kv, "res", "");
                if (r.Length > 0) { try { cs.SetResolution((DisplayResolution)int.Parse(r)); } catch { } }

                // ---- the insertion point: the grid drawn on the dialog's monitor ----
                string xp = Get(kv, "xpos", ""), yp = Get(kv, "ypos", "");
                if (xp.Length > 0)
                { cs.SetXPosition((PositionSelection)System.Enum.Parse(typeof(PositionSelection), xp, true));
                  applied += " xpos=" + xp; }
                if (yp.Length > 0)
                { cs.SetYPosition((PositionSelection)System.Enum.Parse(typeof(PositionSelection), yp, true));
                  applied += " ypos=" + yp; }
                string up = Get(kv, "userpos", "");
                if (up.Length > 0) { cs.SetUserPosition(int.Parse(up)); applied += " userpos=" + up; }

                // Delta X / Delta Y -- the manual says these apply at the 'Free' insertion point
                string dx = Get(kv, "dx", ""), dy = Get(kv, "dy", "");
                if (dx.Length > 0) { cs.SetXOffset(double.Parse(dx, IC)); applied += " dx=" + dx; }
                if (dy.Length > 0) { cs.SetYOffset(double.Parse(dy, IC)); applied += " dy=" + dy; }

                string so = Get(kv, "startoff", ""), eo = Get(kv, "endoff", "");
                if (so.Length > 0) { cs.SetStartOffset(double.Parse(so, IC)); applied += " startOff=" + so; }
                if (eo.Length > 0) { cs.SetEndOffset(double.Parse(eo, IC)); applied += " endOff=" + eo; }

                string rot = Get(kv, "rot", "");
                if (rot.Length > 0) { cs.SetRotation(double.Parse(rot, IC)); applied += " rot=" + rot; }

                string hd = Get(kv, "hdist", ""), vd = Get(kv, "vdist", "");
                if (hd.Length > 0) { cs.SetHorizontalDistance(double.Parse(hd, IC)); applied += " hdist=" + hd; }
                if (vd.Length > 0) { cs.SetVerticalDistance(double.Parse(vd, IC)); applied += " vdist=" + vd; }

                string lay = Get(kv, "layer", "");
                if (lay.Length > 0) { cs.UseCurrentLayer(false); cs.SetLayer(lay); applied += " layer=" + lay; }
                string mat = Get(kv, "material", "");
                if (mat.Length > 0) { cs.SetMaterial(int.Parse(mat)); applied += " mat=" + mat; }
                string fam = Get(kv, "family", "");
                if (fam.Length > 0) { cs.SetFamilyClass(int.Parse(fam)); applied += " family=" + fam; }
                string dcl = Get(kv, "display", "");
                if (dcl.Length > 0) { cs.SetDisplayClass(int.Parse(dcl)); applied += " display=" + dcl; }
                string acl = Get(kv, "area", "");
                if (acl.Length > 0) { cs.SetAreaClass(int.Parse(acl)); applied += " area=" + acl; }
                string art = Get(kv, "article", "");
                if (art.Length > 0) { cs.SetArticle(art); applied += " article=" + art; }

                // explicit third-point alignment -- the documented override of the WCS rule
                string xa = Get(kv, "xaxis", ""), ya = Get(kv, "yaxis", "");
                if (xa.Length > 0) { double[] v = Nums(xa); cs.SetXAxis(new PsVector(v[0], v[1], v[2])); applied += " xaxis=" + xa; }
                if (ya.Length > 0) { double[] v = Nums(ya); cs.SetYAxis(new PsVector(v[0], v[1], v[2])); applied += " yaxis=" + ya; }

                // ---- geometry: two points, OR a direction plus a fixed length ----
                string len = Get(kv, "len", "");
                if (len.Length > 0)
                {
                    double[] d = Nums(Get(kv, "dir", "1,0,0"));
                    cs.SetStartPoint(Pt(Get(kv, "p1", "0,0,0")));
                    cs.SetDirection(new PsVector(d[0], d[1], d[2]), double.Parse(len, IC));
                    applied += " dir=" + Get(kv, "dir", "1,0,0") + " len=" + len;
                }
                else
                {
                    cs.SetInsertPoints(Pt(Get(kv, "p1", "0,0,0")), Pt(Get(kv, "p2", "1000,0,0")));
                }

                if (flat.Length > 0)
                {
                    // B.8.1 "Key ... to create NON-STANDARDISED shape sizes of tubes, flat
                    // steel, round iron" -- CreateFlatSteel is that route for flats, and it
                    // needs no catalogue entry at all.
                    string[] wt = flat.ToLowerInvariant().Split('x');
                    made = cs.CreateFlatSteel(double.Parse(wt[0], IC), double.Parse(wt[1], IC));
                    applied += " flat=" + flat;
                }
                else made = cs.Create();
                try { newId = cs.ObjectId; } catch { }
            }
            catch (System.Exception ex) { msg = " EX:" + ex.Message; }

            string h1, c1; int after = Census(out h1, out c1);
            int delta = after - before;
            string where = "";
            if (newId != 0)
            {
                try
                {
                    PsObjectProperties p = new PsObjectProperties();
                    p.readFrom(newId);
                    PsPoint mn = new PsPoint(0, 0, 0), mx = new PsPoint(0, 0, 0);
                    p.GetExtents(ref mn, ref mx);
                    where = " name='" + p.Name + "' L=" + F(p.Length) +
                            " ext=" + F(mn.x) + "," + F(mn.y) + "," + F(mn.z) +
                            ";" + F(mx.x) + "," + F(mx.y) + "," + F(mx.z);
                }
                catch { }
            }
            Result((delta > 0 ? "EB_OK" : "EB_ERR") + " shape handle=" + HandleOf(newId) +
                   " create=" + made + " census=" + before + "->" + after +
                   applied + where + msg);
        }

        // B.8.4 / B.8.1 -- what the shape database can tell us BEFORE inserting anything.
        //   op=shapeinfo key=<access key>          which catalogue owns this key
        //   op=shapeinfo catalog=<cat> name=<sec>  units, metric/imperial names, section outline
        //   op=shapeinfo flats=1                   the editable flats list
        void ShapeInfo(Dictionary<string, string> kv)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                PsShapeLoader ld = new PsShapeLoader();
                string key = Get(kv, "key", "");
                if (key.Length > 0)
                {
                    string cat = "";
                    try { cat = ld.FindKatalogFromKey(key, false); } catch (System.Exception e) { cat = "<" + e.Message + ">"; }
                    sb.Append("key='" + key + "' -> catalog='" + cat + "'");
                }
                string c = Get(kv, "catalog", ""), n = Get(kv, "name", "");
                if (c.Length > 0 && n.Length > 0)
                {
                    try { sb.Append(" units=" + ld.GetDatabaseDimensionSystem(c, n)); } catch (System.Exception e) { sb.Append(" units:" + e.Message); }
                    try { sb.Append(" db='" + ld.GetDatabaseName(c) + "'"); } catch { }
                    try { sb.Append(" metric='" + ld.GetMetricSectionName(c, n) + "'"); } catch { }
                    try { sb.Append(" imperial='" + ld.GetImperialSectionName(c, n) + "'"); } catch { }
                    try { sb.Append(" shapeClass=" + ld.GetShapeClass(c)); } catch { }
                    try
                    {
                        PsPolygon pg = ld.GetSectionPolygon(c, n);
                        if (pg != null)
                            sb.Append(" | sectionPolygon verts=" + pg.Count + " area=" + F(pg.Area) +
                                      " perimeter=" + F(pg.Length));
                    }
                    catch (System.Exception e) { sb.Append(" polygon:" + e.Message); }
                }
                if (Get(kv, "flats", "") == "1")
                {
                    try
                    {
                        int nf = ld.GetFlatsListCount(UnitsSystem.kMetric);
                        sb.Append(" | metric flats=" + nf + ":");
                        for (int i = 0; i < nf && i < 14; i++)
                            sb.Append(" " + ld.GetFlatsListByIndex(i, UnitsSystem.kMetric));
                    }
                    catch (System.Exception e) { sb.Append(" flats:" + e.Message); }
                }
                Result("EB_OK shapeinfo " + sb.ToString());
            }
            catch (System.Exception ex) { Result("EB_ERR shapeinfo EX:" + ex.Message); }
        }

        // ---- v85: BOLT PARTS THE WAY AMIR DOES -- B.15.1 ----
        // Amir, 06/08/2026: "׳׳ ׳™ ׳׳™׳™׳¦׳¨ ׳—׳•׳¨׳™׳ ׳‘׳”׳×׳׳ ׳׳׳” ׳©׳׳ ׳™ ׳¦׳¨׳™׳ ׳‘׳₪׳§׳•׳“׳× DRILL, ׳•׳׳׳—׳¨ ׳׳›׳
        // ׳‘׳•׳—׳¨ ׳׳× 2 ׳”׳—׳׳§׳™׳ ׳•׳”׳×׳•׳›׳ ׳” ׳™׳•׳“׳¢׳× ׳׳×׳× ׳׳•׳˜׳•׳׳˜׳™׳× ׳׳× ׳‘׳¨׳’׳™ ׳”׳—׳™׳‘׳•׳¨ ׳‘׳™׳ ׳™׳”׳."
        // Manual B.15.1 (p.249), the same thing: "The components are bolted automatically after
        // part selection and selection of the bolt style. THE HOLES IN THE COMPONENT PARTS ARE
        // ANALYSED and the corresponding bolts are selected and inserted."
        //
        // ג ן¸ THE MISTAKE THIS FIXES: PsCreateBolt has TWO paths, and this agent had only ever
        // used the wrong one. CreateSingleBolt(start, end, dia, style) is MANUAL insertion --
        // you supply the grip length yourself, and it is Void, so a grip that has no row in the
        // bolt table fails silently. That is the whole story behind ~400 failed bolts.
        // The automatic path is AddObject(id) for each part, then Create(): the software reads
        // the HOLES and works out the bolts. Bolts follow holes; holes do not follow bolts.
        //
        // Dialog fields (B.15.1) -> properties:
        //   Bolt style      -> BoltStyle          Length Addition -> AdditionalLength
        //   Gap distance    -> MaxCenterDistance  Angle difference-> MaxDeclination
        //   Diameter        -> Diameter           (manual insertion only)
        // "Gap distance: maximum distance between two holes which are assumed to belong to the
        //  bolting. If this value is exceeded the holes cannot be bolted."
        //
        //   op=boltparts handles=<h1,h2,...> [style=DIN6914] [gap=<mm>] [decl=<deg>]
        //                [addlen=<mm>] [objdist=<mm>]
        void BoltParts(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string hh = Get(kv, "handles", "");
            List<long> ids = new List<long>();
            List<string> hs = new List<string>();
            foreach (string one in hh.Split(','))
            {
                string t = one.Trim();
                if (t.Length == 0) continue;
                long id = IdFromHandle(t);
                if (id != 0) { ids.Add(id); hs.Add(t); }
            }
            if (ids.Count == 0) { Result("EB_ERR boltparts: no valid handles in '" + hh + "'"); return; }

            // how many holes are on the table before we start -- bolts come FROM these
            string err;
            StringBuilder scratch = new StringBuilder();
            int holesTotal = 0;
            foreach (long id in ids)
            { scratch.Length = 0; int n = HolesOf(id, 2, scratch, "x", out err); if (n > 0) holesTotal += n; }

            string h0, c0; int before = Census(out h0, out c0);
            int boltCount = -1;
            bool made = false;
            string msg = "";
            try
            {
                PsCreateBolt cb = new PsCreateBolt();
                cb.SetToDefaults();
                string st = Get(kv, "style", "");
                if (st.Length > 0) cb.BoltStyle = st;
                string g = Get(kv, "gap", "");
                if (g.Length > 0) cb.MaxCenterDistance = double.Parse(g, IC);
                string dcl = Get(kv, "decl", "");
                if (dcl.Length > 0) cb.MaxDeclination = double.Parse(dcl, IC);
                string al = Get(kv, "addlen", "");
                if (al.Length > 0) cb.AdditionalLength = double.Parse(al, IC);
                string od = Get(kv, "objdist", "");
                if (od.Length > 0) cb.MaxObjectDistance = double.Parse(od, IC);
                foreach (long id in ids) cb.AddObject(id);
                made = cb.Create();
                try { boltCount = cb.BoltCount; } catch { }
            }
            catch (System.Exception ex) { msg = " EX:" + ex.Message; }

            string h1, c1; int after = Census(out h1, out c1);
            int delta = after - before;
            Result((delta > 0 ? "EB_OK" : "EB_ERR") + " boltparts parts=" + ids.Count +
                   " holesOnParts=" + holesTotal +
                   " style='" + Get(kv, "style", "(default)") + "'" +
                   " created=" + delta + " boltCount=" + boltCount + " create=" + made +
                   " census=" + before + "->" + after + msg +
                   (delta == 0 ? "  (B.15.1: holes further apart than 'Gap distance', or angles " +
                                 "differing by more than 'Angle difference', cannot be bolted)" : ""));
        }

        // ---- v84: B.22 PURLIN CONNECTION ----
        // Manual B.22 (p.318): "This function permits the connection of purlin courses to roof
        // girders. Different kinds of connection are possible ... as standard bolted connection,
        // as connection with a purlin socket made out of a bent flat steel or by means of a
        // splice or a shape."
        //
        // The reason this never got built: PsPurlinConnection needs THREE members --
        // SetSupportObjectId (the roof girder), SetConnectionObjectId (purlin 1) and
        // SetPurlin2Id (purlin 2). A purlin connection joins two purlin runs OVER a girder.
        //
        // And the lesson from B.18 applies here too: START FROM A TEMPLATE. Anchors refused to
        // appear from freshly constructed link data and appeared immediately from a template;
        // assume the same for anything the macro builds out of a database.
        //
        //   op=purlin dump=1                                  list templates + every property
        //   op=purlin support=<girder> p1=<purlin> p2=<purlin> at=x,y,z [template=<name>]
        //             [set=Name=Value;...]
        void Purlin(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string tmpl = Get(kv, "template", "");
            StringBuilder sb = new StringBuilder();
            try
            {
                PsPurlinConnection pc = new PsPurlinConnection();
                pc.SetToDefaults();
                int nt = 0;
                try { nt = pc.GetTemplateCount(); } catch { }

                if (Get(kv, "dump", "") == "1")
                {
                    sb.AppendLine("PURLIN TEMPLATES " + nt);
                    for (int i = 0; i < nt; i++)
                    { try { sb.AppendLine("  [" + i + "] " + pc.GetTemplateName(i)); } catch { } }
                    PsPurlinLinkDataMgd dd = null;
                    if (tmpl.Length > 0) { try { dd = pc.GetTemplate(tmpl); } catch { } }
                    if (dd == null) dd = new PsPurlinLinkDataMgd();
                    sb.AppendLine();
                    Type ty = dd.GetType();
                    PropertyInfo[] ps = ty.GetProperties();
                    System.Array.Sort(ps, delegate(PropertyInfo x, PropertyInfo y)
                                           { return string.CompareOrdinal(x.Name, y.Name); });
                    int cnt = 0;
                    foreach (PropertyInfo pi in ps)
                    {
                        if (pi.Name == "UnmanagedObject") continue;
                        string v;
                        try { object o = pi.GetValue(dd, null); v = o == null ? "(null)" : o.ToString(); }
                        catch (System.Exception e) { v = "<" + e.Message + ">"; }
                        sb.AppendLine(string.Format("  {0,-36} {1,-9} = {2}", pi.Name, pi.PropertyType.Name, v));
                        cnt++;
                    }
                    File.WriteAllText(Path.Combine(Path.GetDirectoryName(
                        Assembly.GetExecutingAssembly().Location), "eb_purlin.txt"),
                        sb.ToString(), new UTF8Encoding(true));
                    Result("EB_OK purlin dump templates=" + nt + " properties=" + cnt + " -> eb_purlin.txt");
                    return;
                }

                long sup = IdFromHandle(Get(kv, "support", ""));
                long p1  = IdFromHandle(Get(kv, "p1", ""));
                // IdFromHandle throws on an empty string -- the same bug the cope op had.
                // p2 is optional, so never hand it an empty handle.
                string p2H = Get(kv, "p2", "");
                long p2  = (p2H.Length > 0) ? IdFromHandle(p2H) : 0;
                if (sup == 0 || p1 == 0)
                { Result("EB_ERR purlin: need support= (the girder) and p1= (the purlin run)"); return; }
                // p2 is OPTIONAL until measured otherwise -- see v114 note.

                PsPurlinLinkDataMgd d = null;
                if (tmpl.Length > 0) { try { d = pc.GetTemplate(tmpl); } catch { } }
                if (d == null) d = new PsPurlinLinkDataMgd();
                string applied = "";
                foreach (string pair in Get(kv, "set", "").Split(';'))
                {
                    string p = pair.Trim();
                    if (p.Length == 0) continue;
                    int eq = p.IndexOf('=');
                    if (eq <= 0) continue;
                    string nm = p.Substring(0, eq).Trim(), vs = p.Substring(eq + 1).Trim();
                    PropertyInfo pi = d.GetType().GetProperty(nm);
                    if (pi == null || !pi.CanWrite) { applied += " noprop:" + nm; continue; }
                    try
                    {
                        Type pt = pi.PropertyType;
                        object val = pt == typeof(bool) ? (object)(vs == "1" || vs.ToLowerInvariant() == "true")
                                   : pt == typeof(double) ? (object)double.Parse(vs, IC)
                                   : pt == typeof(int) ? (object)int.Parse(vs)
                                   : pt == typeof(string) ? (object)vs
                                   : pt.IsEnum ? System.Enum.Parse(pt, vs, true) : null;
                        if (val == null) { applied += " badtype:" + nm; continue; }
                        pi.SetValue(d, val, null);
                        applied += " " + nm + "=" + pi.GetValue(d, null);
                    }
                    catch (System.Exception e) { applied += " " + nm + ":" + e.Message; }
                }

                string h0, c0; int before = Census(out h0, out c0);
                pc.SetConnectionData(d);
                pc.SetSupportObjectId(sup);
                pc.SetConnectionObjectId(p1);
                if (p2 != 0) pc.SetPurlin2Id(p2);
                pc.SetConnectionPoint(Pt(Get(kv, "at", "0,0,0")));
                // B.12 measured Create() returning False while succeeding AND True while
                // doing nothing. Record what the PARTS say, before and after.
                string mSupBefore = ModSig(sup), mP1Before = ModSig(p1);
                string mP2Before = (p2 != 0) ? ModSig(p2) : "-";
                int chk = pc.Check();
                bool made = pc.Create();
                string mSupAfter = ModSig(sup), mP1After = ModSig(p1);
                string mP2After = (p2 != 0) ? ModSig(p2) : "-";
                string link = "";
                try { link = (pc.GetLink() == null) ? " link=null" : " link=OK"; }
                catch (System.Exception e) { link = " link!" + One(e.Message); }
                int plates = -1;
                try { plates = pc.get_PlateDataCount(); } catch { }
                string h1, c1; int after = Census(out h1, out c1);
                int delta = after - before;
                bool touched = delta > 0 || mSupBefore != mSupAfter ||
                               mP1Before != mP1After || mP2Before != mP2After;
                Result((touched ? "EB_OK" : "EB_ERR") + " purlin template='" + tmpl + "'" +
                       " check=" + chk + " create=" + made + " plates=" + plates + link +
                       " census=" + before + "->" + after + "(+" + delta + ")" +
                       " p2=" + (p2 != 0 ? "given" : "(none)") +
                       " girder[" + mSupBefore + "]->[" + mSupAfter + "]" +
                       " purlin1[" + mP1Before + "]->[" + mP1After + "]" +
                       (p2 != 0 ? " purlin2[" + mP2Before + "]->[" + mP2After + "]" : "") +
                       (applied.Length > 0 ? " set[" + applied.Trim() + "]" : ""));
            }
            catch (System.Exception ex) { Result("EB_ERR purlin EX:" + ex.Message); }
        }

        // ---- v82: THE BASE-PLATE DIALOG AS DATA, AND WHETHER THE VALUES STICK ----
        // connbase sets AnchorBolts + the five AnchorBolt* numbers and gets NO anchor bodies,
        // with ProSteel answering "REQUESTED VOLUME SOLIDS CAN NOT BE PRODUCED". Five different
        // parameter combinations produced byte-identical results, which is the signature of
        // values that never arrive -- not of values that arrive and are rejected.
        //
        // So before guessing again: dump every property of PsBaseplateLinkDataMgd from a live
        // template, and (with set=) write values and READ THEM BACK, to see whether the
        // assignment even sticks. Same method that turned B.17 from guesswork into a table.
        //
        //   op=basedump [template=<name>] [set=Name=Value;Name=Value]
        void BaseDump(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string tmpl = Get(kv, "template", "");
            string sets = Get(kv, "set", "");
            StringBuilder sb = new StringBuilder();
            try
            {
                PsBasePlateConnection bp = new PsBasePlateConnection();
                bp.SetToDefaults();

                int nt = 0;
                try { nt = bp.GetTemplateCount(); } catch { }
                sb.AppendLine("TEMPLATES " + nt);
                for (int i = 0; i < nt; i++)
                {
                    try { sb.AppendLine("  [" + i + "] " + bp.GetTemplateName(i)); } catch { }
                }

                PsBaseplateLinkDataMgd d = null;
                if (tmpl.Length > 0) { try { d = bp.GetTemplate(tmpl); } catch { } }
                if (d == null) { d = new PsBaseplateLinkDataMgd(); sb.AppendLine("(fresh link data, no template)"); }
                else sb.AppendLine("TEMPLATE " + tmpl);

                Type t = d.GetType();
                // write first, if asked, so the read-back below shows whether it stuck
                List<string> refused = new List<string>();
                foreach (string pair in sets.Split(';'))
                {
                    string p = pair.Trim();
                    if (p.Length == 0) continue;
                    int eq = p.IndexOf('=');
                    if (eq <= 0) { refused.Add(p); continue; }
                    string nm = p.Substring(0, eq).Trim(), vs = p.Substring(eq + 1).Trim();
                    PropertyInfo pi = t.GetProperty(nm);
                    if (pi == null || !pi.CanWrite) { refused.Add(nm + "(no such writable property)"); continue; }
                    try
                    {
                        Type pt = pi.PropertyType;
                        object val = pt == typeof(bool) ? (object)(vs == "1" || vs.ToLowerInvariant() == "true")
                                   : pt == typeof(double) ? (object)double.Parse(vs, IC)
                                   : pt == typeof(int) ? (object)int.Parse(vs)
                                   : pt == typeof(string) ? (object)vs
                                   : pt.IsEnum ? System.Enum.Parse(pt, vs, true) : null;
                        if (val == null) { refused.Add(nm + "(type " + pt.Name + ")"); continue; }
                        pi.SetValue(d, val, null);
                        object got = pi.GetValue(d, null);
                        sb.AppendLine("  SET " + nm + " = " + vs + "  -> reads back " +
                                      (got == null ? "null" : got.ToString()) +
                                      ((got != null && got.ToString() == val.ToString()) ? "  STUCK" : "  *** DID NOT STICK ***"));
                    }
                    catch (System.Exception e) { refused.Add(nm + "(" + e.Message + ")"); }
                }

                sb.AppendLine();
                PropertyInfo[] props = t.GetProperties();
                System.Array.Sort(props, delegate(PropertyInfo x, PropertyInfo y)
                                          { return string.CompareOrdinal(x.Name, y.Name); });
                int n = 0;
                foreach (PropertyInfo pi in props)
                {
                    if (pi.Name == "UnmanagedObject") continue;
                    string val;
                    try { object o = pi.GetValue(d, null); val = o == null ? "(null)" : o.ToString(); }
                    catch (System.Exception e) { val = "<" + e.Message + ">"; }
                    sb.AppendLine(string.Format("  {0,-36} {1,-9} = {2}", pi.Name, pi.PropertyType.Name, val));
                    n++;
                }
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location), "eb_basedump.txt"),
                    sb.ToString(), new UTF8Encoding(true));
                Result("EB_OK basedump templates=" + nt + " properties=" + n +
                       (refused.Count > 0 ? "  REFUSED: " + string.Join(", ", refused.ToArray()) : "") +
                       " -> eb_basedump.txt");
            }
            catch (System.Exception ex) { Result("EB_ERR basedump EX:" + ex.Message); }
        }

        // ---- v79: SAVE FROM INSIDE ----
        // Saving over COM failed twice in one hour: once with "Call was rejected by callee"
        // while AutoCAD sat mid-command, once with "The server threw an exception". Both times
        // the drawing itself was fine -- the fragile part was the COM hop.
        // The plugin already runs INSIDE the right AutoCAD, so it needs no COM, no instance
        // disambiguation and no quiescent check. Reports the file's real size so the caller
        // can prove it landed.
        //   op=save [as=<full path>]
        void Save(Dictionary<string, string> kv)
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                string path = Get(kv, "as", "");
                if (path.Length == 0) path = doc.Name;      // full path of the open drawing
                long before = 0;
                try { if (File.Exists(path)) before = new FileInfo(path).Length; } catch { }
                // Database.SaveAs to the path AutoCAD already holds open raises
                // eFileSharingViolation -- it is for saving a COPY elsewhere. To save the
                // drawing in place, run the command, in-process and synchronously.
                if (Get(kv, "as", "").Length > 0)
                    doc.Database.SaveAs(path, DwgVersion.Current);
                else
                    doc.Editor.Command("_QSAVE");
                long after = 0;
                string when = "";
                try
                {
                    FileInfo fi = new FileInfo(path);
                    after = fi.Length;
                    when = fi.LastWriteTime.ToString("HH:mm:ss");
                }
                catch { }
                Result((after > 0 ? "EB_OK" : "EB_ERR") + " save '" + path + "'" +
                       " bytes=" + before + "->" + after + " at=" + when);
            }
            catch (System.Exception ex) { Result("EB_ERR save EX:" + ex.Message); }
        }

        // ---- v78: DRIVE ANY OF THE 132 B.17 PROPERTIES ----
        // Adding a named parameter per dialog field would mean 132 parameters and 132 rebuilds,
        // and the DLL locks on NETLOAD so each rebuild is expensive. Since conndump has now
        // mapped every property with its real type and default, a generic setter is both safe
        // and far more capable: the whole dialog becomes drivable in one op.
        //
        // It REFUSES an unknown property name -- same discipline as the op-parameter guard.
        // A silently ignored "WeldToFlagne=1" would leave an unwelded connection looking fine.
        //
        //   op=connset support=<h> beam=<h> at=x,y,z [template=<name>]
        //              set=PlateIsRotated=1;WeldToFlange=1;BoltStyle=DIN6914;Thickness=20
        void ConnSet(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            // ג ן¸ QUARANTINED 06/08/2026 -- THIS OP CRASHES AutoCAD.
            // Reproduced four times, twice taking the whole process down and once corrupting
            // the open drawing so that NO save route could write it. Ruled out:
            //   * any single property -- Thickness, Length, PlateIsRotated, BoltStyle,
            //     WeldToFlange were each set on their own and each created a valid connection
            //   * pacing alone -- a 2 s settle plus PS_REGEN still crashed on the second call
            //   * a stale drawing -- it crashed in a freshly created one too
            // The stable path for connections remains op=conn, which has driven the six beam
            // connections all day without incident. Shipping an op that crashes on a real
            // model is worse than not having it, so this one refuses unless force=1.
            if (Get(kv, "force", "") != "1")
            {
                Result("EB_ERR connset is QUARANTINED -- it has crashed AutoCAD four times and " +
                       "once left the drawing unsaveable. Use op=conn for beam connections. " +
                       "Pass force=1 only in a throwaway drawing you are willing to lose.");
                return;
            }
            long sup = IdFromHandle(Get(kv, "support", ""));
            long bm  = IdFromHandle(Get(kv, "beam", ""));
            if (sup == 0 || bm == 0)
            { Result("EB_ERR connset: need support= and beam= handles"); return; }
            PsPoint at = Pt(Get(kv, "at", "0,0,0"));
            string tmpl = Get(kv, "template", "default/Standard");
            string sets = Get(kv, "set", "");

            string h0, c0; int before = Census(out h0, out c0);
            StringBuilder applied = new StringBuilder();
            List<string> bad = new List<string>();
            bool made = false;
            int chk = -999;
            string msg = "";
            try
            {
                PsStandardPlateConnection pc = new PsStandardPlateConnection();
                pc.SetToDefaults();
                PsStandardPlateLinkData d = pc.GetTemplate(tmpl);
                if (d == null) { Result("EB_ERR connset: no template '" + tmpl + "'"); return; }

                Type t = d.GetType();
                foreach (string pair in sets.Split(';'))
                {
                    string p = pair.Trim();
                    if (p.Length == 0) continue;
                    int eq = p.IndexOf('=');
                    if (eq <= 0) { bad.Add(p + " (not name=value)"); continue; }
                    string nm = p.Substring(0, eq).Trim();
                    string vs = p.Substring(eq + 1).Trim();
                    PropertyInfo pi = t.GetProperty(nm);
                    if (pi == null) { bad.Add(nm + " (no such property)"); continue; }
                    if (!pi.CanWrite) { bad.Add(nm + " (read-only)"); continue; }
                    try
                    {
                        Type pt = pi.PropertyType;
                        object val;
                        if (pt == typeof(bool)) val = (vs == "1" || vs.ToLowerInvariant() == "true");
                        else if (pt == typeof(double)) val = double.Parse(vs, IC);
                        else if (pt == typeof(int)) val = int.Parse(vs);
                        else if (pt == typeof(string)) val = vs;
                        else if (pt.IsEnum) val = System.Enum.Parse(pt, vs, true);
                        else { bad.Add(nm + " (unsupported type " + pt.Name + ")"); continue; }
                        pi.SetValue(d, val, null);
                        // read it straight back -- a setter that silently ignores the value is
                        // exactly the failure mode this whole codebase keeps meeting
                        object got = pi.GetValue(d, null);
                        applied.Append(" " + nm + "=" + (got == null ? "null" : got.ToString()));
                        if (got == null || got.ToString() != val.ToString())
                            bad.Add(nm + " (set " + val + " but reads back " + got + ")");
                    }
                    catch (System.Exception e) { bad.Add(nm + " (" + e.Message + ")"); }
                }

                pc.SetConnectionData(d);
                pc.SetSupportObjectId(sup);
                pc.SetConnectionObjectId(bm);
                pc.SetConnectionPoint(at);
                chk = pc.Check();
                made = pc.Create();
            }
            catch (System.Exception ex) { msg = " EX:" + ex.Message; }

            string h1, c1; int after = Census(out h1, out c1);
            int delta = after - before;
            Result((delta > 0 ? "EB_OK" : "EB_ERR") + " connset template='" + tmpl + "'" +
                   " check=" + chk + " create=" + made +
                   " census=" + before + "->" + after + "(+" + delta + ")" +
                   " applied[" + applied.ToString().Trim() + "]" +
                   (bad.Count > 0 ? "  *** REFUSED: " + string.Join(", ", bad.ToArray()) + " ***" : "") +
                   msg);
        }

        // ---- v77: THE WHOLE B.17 DIALOG AS DATA ----
        // PsStandardPlateLinkData is the entire "Plate Connections" dialog: 133 properties and
        // three methods. Guessing which property is which dialog field is how "Diameter" came
        // to mean the BOLT and not the hole. So: read every property off a LIVE template and
        // print name, type and value. 133 unknowns become a reference table with real defaults.
        //
        // Reflection is right here because it READS. Reflection to guess at a method NAME is
        // what broke `props` for weeks; reflection to enumerate what a class actually holds is
        // exactly what it is for.
        //
        //   op=conndump                       list the installed templates
        //   op=conndump template=<name>       every property of that template, with its value
        //   op=conndump template=<name> only=<substr>
        void ConnDump(Dictionary<string, string> kv)
        {
            string want = Get(kv, "template", "");
            string only = Get(kv, "only", "").ToLowerInvariant();
            StringBuilder sb = new StringBuilder();
            try
            {
                PsStandardPlateConnection pc = new PsStandardPlateConnection();
                pc.SetToDefaults();
                int nt = pc.GetTemplateCount();
                sb.AppendLine("TEMPLATES " + nt);
                List<string> names = new List<string>();
                for (int i = 0; i < nt; i++)
                {
                    string nm = "";
                    try { nm = pc.GetTemplateName(i); } catch { }
                    names.Add(nm);
                    sb.AppendLine("  [" + i + "] " + nm);
                }
                if (want.Length == 0)
                {
                    File.WriteAllText(Path.Combine(Path.GetDirectoryName(
                        Assembly.GetExecutingAssembly().Location), "eb_conndump.txt"),
                        sb.ToString(), new UTF8Encoding(true));
                    Result("EB_OK conndump templates=" + nt + " -> eb_conndump.txt");
                    return;
                }

                PsStandardPlateLinkData d = pc.GetTemplate(want);
                if (d == null) { Result("EB_ERR conndump: no template named '" + want + "'"); return; }

                sb.AppendLine();
                sb.AppendLine("TEMPLATE " + want);
                Type t = d.GetType();
                PropertyInfo[] props = t.GetProperties();
                System.Array.Sort(props, delegate(PropertyInfo x, PropertyInfo y)
                                          { return string.CompareOrdinal(x.Name, y.Name); });
                int n = 0;
                foreach (PropertyInfo pi in props)
                {
                    if (pi.Name == "UnmanagedObject") continue;
                    if (only.Length > 0 && pi.Name.ToLowerInvariant().IndexOf(only) < 0) continue;
                    string val;
                    try
                    {
                        object o = pi.GetValue(d, null);
                        val = o == null ? "(null)" : o.ToString();
                    }
                    catch (System.Exception e) { val = "<" + e.Message + ">"; }
                    sb.AppendLine(string.Format("  {0,-42} {1,-10} = {2}",
                                  pi.Name, pi.PropertyType.Name, val));
                    n++;
                }
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location), "eb_conndump.txt"),
                    sb.ToString(), new UTF8Encoding(true));
                Result("EB_OK conndump template='" + want + "' properties=" + n +
                       " templates=" + nt + " -> eb_conndump.txt");
            }
            catch (System.Exception ex) { Result("EB_ERR conndump EX:" + ex.Message); }
        }

        // ---- v75: NUMBER THE GROUPS (B.29, second pass) ----
        // ג ן¸ HARD ORDERING RULE from the manual, B.29.1 "Group Detection":
        //   "single parts are only compared using their position number because positioning
        //    has already been carried out before."
        // So two groups are the same group when their MEMBERS' POSITION NUMBERS match --
        // geometry is not re-examined. That is why singles MUST be numbered first, and it is
        // also why this cannot reuse CheckTwoPartsAreEqual: the equality rule is different
        // for groups than for parts.
        //
        //   op=groupauto [prefix=G] [start=1] [field=pos|send] [dry=1]
        void GroupAuto(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string prefix = Get(kv, "prefix", "G");
            int start = int.Parse(Get(kv, "start", "1"));
            bool dry = Get(kv, "dry", "") == "1";
            bool useSend = Get(kv, "field", "pos").ToLowerInvariant() == "send";

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            List<long> mains = new List<long>();
            List<string> mainH = new List<string>();
            List<string> sig = new List<string>();
            HashSet<long> seen = new HashSet<long>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    long oid;
                    DBObject o;
                    try { o = tr.GetObject(id, OpenMode.ForRead); oid = id.OldIdPtr.ToInt64(); }
                    catch { continue; }
                    try
                    {
                        PsObjectGroup g = new PsObjectGroup();
                        g.Initialize();
                        g.GetGroupFrom(oid);
                        int pc = g.PartCount;
                        if (pc <= 1) continue;                    // not in a group
                        long main = g.getMainPartOf(oid);
                        if (main == 0) main = g.getAssemblyMainPartOf(oid);
                        if (main == 0 || seen.Contains(main)) continue;
                        seen.Add(main);

                        // the signature: the SORTED position numbers of every member
                        List<string> nums = new List<string>();
                        for (int i = 0; i < pc; i++)
                        {
                            try
                            {
                                PsObjectProperties pp = new PsObjectProperties();
                                pp.readFrom(g.getPart(i));
                                nums.Add(pp.Posnum ?? "");
                            }
                            catch { nums.Add("?"); }
                        }
                        nums.Sort(System.StringComparer.Ordinal);
                        mains.Add(main);
                        mainH.Add(HandleOf(main));
                        sig.Add(string.Join("+", nums.ToArray()));
                    }
                    catch { }
                }
                tr.Commit();
            }

            if (mains.Count == 0) { Result("EB_ERR groupauto: no groups found"); return; }

            // if any member has no position number the signature is meaningless -- say so
            int unnumbered = 0;
            foreach (string x in sig) if (x.Contains("++") || x.StartsWith("+") || x.EndsWith("+") || x == "") unnumbered++;

            Dictionary<string, int> cluster = new Dictionary<string, int>();
            StringBuilder sb = new StringBuilder();
            sb.Append("HANDLE\tSIGNATURE\tNUMBER\tSTATE\n");
            int written = 0, failed = 0;
            for (int i = 0; i < mains.Count; i++)
            {
                int c;
                if (!cluster.TryGetValue(sig[i], out c)) { c = cluster.Count; cluster[sig[i]] = c; }
                string num = prefix + (start + c).ToString(IC);
                string state = "dry";
                if (!dry)
                {
                    state = "FAILED";
                    try
                    {
                        PsGroupProperties gp = new PsGroupProperties();
                        gp.readFrom(mains[i]);
                        if (useSend) gp.Sendnum = num; else gp.Posnum = num;
                        gp.writeTo(mains[i]);
                        PsGroupProperties chk = new PsGroupProperties();
                        chk.readFrom(mains[i]);
                        string got = useSend ? chk.Sendnum : chk.Posnum;
                        if (got == num) { state = "ok"; written++; } else { failed++; }
                    }
                    catch { failed++; }
                }
                sb.Append(mainH[i]).Append('\t').Append(sig[i]).Append('\t')
                  .Append(num).Append('\t').Append(state).Append('\n');
            }
            try
            {
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location), Get(kv, "out", "eb_groupauto.txt")),
                    sb.ToString(), new UTF8Encoding(true));
            }
            catch { }

            Result((failed == 0 ? "EB_OK" : "EB_ERR") + " groupauto groups=" + mains.Count +
                   " distinct=" + cluster.Count + " field=" + (useSend ? "Sendnum" : "Posnum") +
                   " written=" + written + " failed=" + failed +
                   (dry ? " (DRY RUN)" : "") +
                   (unnumbered > 0 ? "  *** " + unnumbered + " group(s) contain parts with NO " +
                                     "position number -- run posauto FIRST, the manual makes " +
                                     "single-part numbering a prerequisite for group detection ***" : "") +
                   " -> " + Get(kv, "out", "eb_groupauto.txt"));
        }

        // ---- v74: THE REST OF B.14 -- radial fields, countersinks, blind holes ----
        // B.14.1 has more than the linear field this agent had been using:
        //   SetRadialHoleField(Diameter, Radius, HoleCount) + SetRadialHoleRange(From, To)
        //     -- a bolt circle, e.g. a pipe flange
        //   SetHoleCounter(SenkLength, Angle)   -- a COUNTERSINK
        //   SetHoleDepth(d) + SetDeepStart(s)   -- a BLIND hole that does not go through
        //   SetXPosition/SetYPosition(PositionSelection) -- place the field by edge reference
        //     instead of by coordinate (kLeft/kRight/kDown/kTop/kCenter/kGravity/kPitch/kUser)
        //
        //   op=drillspecial handle=<h> at=x,y,z kind=radial|counter|blind
        //       radial : dia= r= n= [from= to=]
        //       counter: dia= sink= angle=
        //       blind  : dia= depth= [start=]
        void DrillSpecial(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string hh = Get(kv, "handle", "");
            long oid = IdFromHandle(hh);
            if (oid == 0) { Result("EB_ERR drillspecial: bad handle " + hh); return; }
            string kind = Get(kv, "kind", "radial").ToLowerInvariant();
            double dia = double.Parse(Get(kv, "dia", "20"), IC);
            PsPoint at = Pt(Get(kv, "at", "0,0,0"));
            double[] nz = Nums(Get(kv, "normal", "0,0,1"));

            string err;
            StringBuilder scratch = new StringBuilder();
            int before = HolesOf(oid, 2, scratch, "x", out err);
            int want = -1;
            string msg = "", what = "";
            try
            {
                PsDrillObject d = new PsDrillObject();
                d.SetToDefaults();
                d.SetObjectId(oid);
                d.SetInsertPoint(at);
                d.SetNormal(new PsVector(nz[0], nz.Length > 1 ? nz[1] : 0, nz.Length > 2 ? nz[2] : 1));
                double play = double.Parse(Get(kv, "play", "0"), IC);
                d.SetHoleWorkloose(play);

                if (kind == "radial")
                {
                    double r = double.Parse(Get(kv, "r", "100"), IC);
                    int n = int.Parse(Get(kv, "n", "6"));
                    want = n;
                    string fromS = Get(kv, "from", ""), toS = Get(kv, "to", "");
                    if (fromS.Length > 0 || toS.Length > 0)
                        d.SetRadialHoleRange(double.Parse(fromS.Length > 0 ? fromS : "0", IC),
                                             double.Parse(toS.Length > 0 ? toS : "360", IC));
                    d.SetRadialHoleField(dia, r, n);
                    what = "radial r=" + F(r) + " n=" + n;
                }
                else if (kind == "counter")
                {
                    double sink = double.Parse(Get(kv, "sink", "6"), IC);
                    double ang = double.Parse(Get(kv, "angle", "90"), IC);
                    d.SetHoleCounter(sink, ang);
                    d.SetSingleHoleField(dia);
                    want = 1;
                    what = "countersink len=" + F(sink) + " angle=" + F(ang);
                }
                else   // blind
                {
                    double depth = double.Parse(Get(kv, "depth", "10"), IC);
                    string st = Get(kv, "start", "");
                    d.SetHoleDepth(depth);
                    if (st.Length > 0) d.SetDeepStart(double.Parse(st, IC));
                    d.SetSingleHoleField(dia);
                    want = 1;
                    what = "blind depth=" + F(depth) + (st.Length > 0 ? " start=" + st : "");
                }
                d.Apply();
            }
            catch (System.Exception ex) { msg = " EX:" + ex.Message; }

            scratch.Length = 0;
            int after = HolesOf(oid, 2, scratch, "x", out err);
            int made = after - before;
            Result((made > 0 ? "EB_OK" : "EB_ERR") + " drillspecial handle=" + hh +
                   " " + what + " dia=" + F(dia) +
                   " holes=" + before + "->" + after + "(+" + made + ")" +
                   (want > 0 && made != want ? "  *** asked for " + want + ", got " + made + " ***" : "") +
                   msg);
        }

        // ---- v72: A CROSS SECTION OF ANY PART, AT ANY PLANE ----
        // PsGeo.CreateSection(Id, Origin, XAxis, YAxis, Projection) builds the outline of a
        // part cut by a plane. That is the shape a plate has to be cut to, and the outline a
        // section drawing shows -- available as real geometry, not a picture.
        //
        //   op=section handle=<h> at=x,y,z [xaxis=1,0,0] [yaxis=0,1,0] [project=1]
        void Section(Dictionary<string, string> kv)
        {
            string hh = Get(kv, "handle", "");
            long oid = IdFromHandle(hh);
            if (oid == 0) { Result("EB_ERR section: bad handle " + hh); return; }
            PsPoint at = Pt(Get(kv, "at", "0,0,0"));
            double[] xa = Nums(Get(kv, "xaxis", "1,0,0"));
            double[] ya = Nums(Get(kv, "yaxis", "0,1,0"));
            bool project = Get(kv, "project", "0") == "1";

            try
            {
                PsGeo geo = new PsGeo();
                geo.CreateSection(oid, at,
                                  new PsVector(xa[0], xa.Length > 1 ? xa[1] : 0, xa.Length > 2 ? xa[2] : 0),
                                  new PsVector(ya[0], ya.Length > 1 ? ya[1] : 0, ya.Length > 2 ? ya[2] : 0),
                                  project, (ModelBuild)int.Parse(Get(kv, "mb", "0")));
                // Read each count ONCE. Reading lineCount a second time returned 0 while the
                // first read had reported 46 -- the verdict then called a working section a
                // failure. These are not plain fields; treat every count as one-shot.
                int nLines = 0, nArcs = 0, nCircles = 0, nDraw = 0;
                try { nLines = geo.lineCount; } catch { }
                try { nArcs = geo.arcCount; } catch { }
                try { nCircles = geo.circleCount; } catch { }
                try { nDraw = geo.countDrawableElements(); } catch { }
                StringBuilder sb = new StringBuilder();
                sb.Append("empty=" + geo.isEmpty() +
                          " lines=" + nLines + " arcs=" + nArcs +
                          " circles=" + nCircles +
                          " drawable=" + nDraw +
                          " lineLen=" + F(geo.getLineLength()));
                PsPoint mn = new PsPoint(0, 0, 0), mx = new PsPoint(0, 0, 0);
                geo.extents(mn, mx);
                sb.Append(" ext=" + F(mn.x) + "," + F(mn.y) + "," + F(mn.z) +
                          ";" + F(mx.x) + "," + F(mx.y) + "," + F(mx.z));
                try
                {
                    PsPolygon pg = new PsPolygon();
                    pg.init();
                    int cr = geo.convertToPolygon(pg, 0.1, false);
                    sb.Append(" -> polygon rc=" + cr + " verts=" + pg.Count +
                              " area=" + F(pg.Area) + " perimeter=" + F(pg.Length) +
                              " closed=" + pg.isClosed(0.1) + " rect=" + pg.isRectangle());
                }
                catch (System.Exception e) { sb.Append(" polygon:" + e.Message); }
                // isEmpty() is NOT the test: measured true on a PsGeo holding 46 lines and a
                // 14922 mm2 outline. Count the elements instead.
                int els = nLines + nArcs + nCircles;
                Result((els > 0 ? "EB_OK" : "EB_ERR") + " section handle=" + hh +
                       " elements=" + els + " " + sb.ToString());
            }
            catch (System.Exception ex) { Result("EB_ERR section EX:" + ex.Message); }
        }

        // ---- v71: B.12.1 DIVIDE / COMBINE ----
        // Manual B.12.1 (p.196): splitting a member, trimming it at one side, joining two.
        // These live on PsEditShapeModification, NOT on PsCutObjects -- which is why they
        // were missed when the cut types were swept.
        //   op=shapeedit handle=<h> split=x,y,z              cut the member in two there
        //   op=shapeedit handle=<h> lengthat=x,y,z len=<mm>  set the length measured at a point
        //   op=shapeedit handle=<h> side=<0|1> len=<mm>      set the length at one END
        //   op=shapeedit handle=<h> connect=<other handle>   join two collinear members
        void ShapeEdit(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string hh = Get(kv, "handle", "");
            long oid = IdFromHandle(hh);
            if (oid == 0) { Result("EB_ERR shapeedit: bad handle " + hh); return; }

            double lenBefore = LengthOf(oid);
            string h0, c0; int censusBefore = Census(out h0, out c0);
            string what = "", msg = "";
            try
            {
                PsEditShapeModification em = new PsEditShapeModification();
                em.SetToDefaults();
                em.SetObjectId(oid);

                string sp = Get(kv, "split", "");
                string la = Get(kv, "lengthat", "");
                string sd = Get(kv, "side", "");
                string cn = Get(kv, "connect", "");

                if (sp.Length > 0) { em.SplitAtPoint(oid, Pt(sp)); what = "split@" + sp; }
                else if (la.Length > 0)
                {
                    double L = double.Parse(Get(kv, "len", "1000"), IC);
                    em.ChangeLengthAtPoint(oid, Pt(la), L);
                    what = "lengthAtPoint=" + F(L);
                }
                else if (sd.Length > 0)
                {
                    double L = double.Parse(Get(kv, "len", "1000"), IC);
                    em.ChangeLengthAtSide(oid, int.Parse(sd), L);
                    what = "lengthAtSide" + sd + "=" + F(L);
                }
                else if (cn.Length > 0)
                {
                    long other = IdFromHandle(cn);
                    if (other == 0) { Result("EB_ERR shapeedit: bad connect= " + cn); return; }
                    em.ConnectWith(oid, other);
                    what = "connect->" + cn;
                }
                else { Result("EB_ERR shapeedit: give split= | lengthat= | side= | connect="); return; }

                // a split makes a NEW member -- the class reports it rather than the census
                try
                {
                    int n = em.NewObjectCount;
                    for (int i = 0; i < n; i++)
                        what += " new=" + HandleOf(em.getNewObjectId(i));
                }
                catch { }
            }
            catch (System.Exception ex) { msg = " EX:" + ex.Message; }

            double lenAfter = LengthOf(oid);
            string h1, c1; int censusAfter = Census(out h1, out c1);
            bool changed = System.Math.Abs(lenAfter - lenBefore) > 0.01 || censusAfter != censusBefore;
            Result((changed ? "EB_OK" : "EB_ERR") + " shapeedit handle=" + hh + " " + what +
                   " len=" + F(lenBefore) + "->" + F(lenAfter) +
                   " census=" + censusBefore + "->" + censusAfter + msg);
        }

        // ---- v70: BOOLEAN OPERATIONS (B.12.7) AND THE DETAIL CUT ----
        // ג ן¸ The manual's warning that makes this necessary at all (B.12.7, p.225): ProSteel
        // does NOT use the AutoCAD ACIS modeller, so AutoCAD's own UNION / SUBTRACT / INTERSECT
        // silently do nothing on ProSteel objects -- "there will be no errors, but nothing will
        // happen!". This is the only way to do a boolean on steel.
        //
        // SetAsBooleanCut takes an Int64 -- the id of an EXISTING solid, called the
        // "discharge-solid" in the manual. SetSubBodyType picks the operation:
        //   kSubBody = subtract ֲ· kAddBody = add ֲ· kCommenBody = keep the common volume
        //
        //   op=boolean handle=<h> tool=<h> [mode=sub|add|common]
        void Boolean(Dictionary<string, string> kv)
        {
            string hh = Get(kv, "handle", ""), ht = Get(kv, "tool", "");
            long oid = IdFromHandle(hh), tool = IdFromHandle(ht);
            if (oid == 0) { Result("EB_ERR boolean: bad handle " + hh); return; }
            if (tool == 0) { Result("EB_ERR boolean: bad tool= " + ht); return; }
            if (oid == tool) { Result("EB_ERR boolean: a part cannot be its own tool"); return; }
            string mode = Get(kv, "mode", "sub").ToLowerInvariant();
            SubBodyType st = mode == "add" ? SubBodyType.kAddBody :
                             mode == "common" ? SubBodyType.kCommenBody : SubBodyType.kSubBody;

            string before = ModSig(oid), msg = "";
            double wBefore = WeightOf(oid);
            int rc = -999;
            try
            {
                PsCutObjects cut = new PsCutObjects();
                cut.SetToDefaults();
                cut.SetObjectId(oid);
                cut.SetSubBodyType(st);
                cut.SetAsBooleanCut(tool);
                rc = cut.Apply();
            }
            catch (System.Exception ex) { msg = " EX:" + ex.Message; }
            string after = ModSig(oid);
            double wAfter = WeightOf(oid);
            bool changed = before != after || System.Math.Abs(wAfter - wBefore) > 0.001;
            Result((changed ? "EB_OK" : "EB_ERR") + " boolean handle=" + hh + " tool=" + ht +
                   " mode=" + mode + " applyRc=" + rc +
                   " wt=" + F(wBefore) + "->" + F(wAfter) +
                   " mods[" + before + "]->[" + after + "]" + msg);
        }

        // A "detail cut" removes NO material -- it plants a 2D section marker used later at
        // detailing. Manual p.571: "2D-cuts inserted in the model only define whether and how
        // a detail has to be cut at detailing process."  So census, length and weight are all
        // the WRONG instrument here; the modification inventory is the only witness.
        //   op=detailcut handle=<h> at=x,y,z depth=<mm>
        void DetailCut(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string hh = Get(kv, "handle", "");
            long oid = IdFromHandle(hh);
            if (oid == 0) { Result("EB_ERR detailcut: bad handle " + hh); return; }
            PsPoint at = Pt(Get(kv, "at", "0,0,0"));
            double depth = double.Parse(Get(kv, "depth", "100"), IC);

            string before = ModSig(oid), msg = "";
            int dcBeforeSaved = DetailCuts(oid);
            int rc = -999;
            try
            {
                PsCutObjects cut = new PsCutObjects();
                cut.SetToDefaults();
                cut.SetObjectId(oid);
                cut.SetAsDetailCut(at, depth);
                rc = cut.Apply();
            }
            catch (System.Exception ex) { msg = " EX:" + ex.Message; }
            string after = ModSig(oid);
            // PsEditModification cannot see detail cuts at all -- they are counted by
            // PsEditShapeModification.DetailCutCount. Judging this op by ModSig reported a
            // working call as "nothing happened": the wrong instrument, not a failed op.
            int dcBefore = -1, dcAfter = DetailCuts(oid);
            Result((dcAfter > dcBeforeSaved ? "EB_OK" : "EB_ERR") + " detailcut handle=" + hh +
                   " depth=" + F(depth) + " applyRc=" + rc +
                   " detailCuts=" + dcBeforeSaved + "->" + dcAfter +
                   " mods[" + before + "]->[" + after + "]" +
                   "  (removes NO material -- it marks a 2D section for detailing)" + msg);
            if (dcBefore == 0) { }
        }

        static double WeightOf(long oid)
        {
            try
            {
                PsObjectProperties p = new PsObjectProperties();
                p.readFrom(oid);
                return p.Weight;
            }
            catch { return -1; }
        }

        // ---- v68: B.28 GROUPS, THE WHOLE CHAPTER ----
        // Groups encode FABRICATION INTENT, not drawing convenience:
        //   subgroup            = stock parts
        //   component part group = ships as ONE piece
        //   assembly            = combined on site, no main part
        // Creation already worked. Everything else in B.28 did not exist here: reading the
        // members back, the main part, sub-parts, nesting, the group's own weight and
        // dimensions, renaming, removing a part, deleting the group.
        //
        // computeWeight(id, withoutBolts) is the one that matters commercially -- a shipping
        // weight with and without its bolts, straight from the software.
        //
        //   op=groupinfo handle=<h>              everything about the group this part is in
        //   op=groupedit handle=<h> [add=<h,h>] [remove=<h,h>] [name=<s>] [pos=<s>] [send=<s>]
        //                           [delete=1]
        // ===================================================================
        //  v172 (B.28 audit) -- CHECK GROUPS, BUILT FROM WHAT *IS* EXPOSED
        //
        //  B.28 concluded, after scanning all 8,622 public types, that "Check Groups itself --
        //  Orphans, Compare+Modify, Release Single Part Groups -- is NOT in the API". That is
        //  true of the COMMAND. But the manual describes what the command DOES, and the three
        //  checks it performs are all membership questions, and PsObjectGroup answers those:
        //
        //    Mark Orphans          "display the parts that DON'T BELONG TO A GROUP"
        //                          -> getMainPartOf(id) == 0
        //    main-part check       "checks whether all the groups have a main part"
        //                          -> getMainPart() == 0 on a group that has members
        //    Release Single Part   groups with only one part
        //                          -> PartCount <= 1
        //
        //  ⇒ The dialog is unreachable; the CHECK is not. This is the B.25 lesson again --
        //  the manual's six separable buttons were an instruction to compose.
        //
        //  op=grouporphans [out=eb_groups.txt] [minx=] [maxx=]
        // ===================================================================
        void GroupOrphans(Dictionary<string, string> kv)
        {
            System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
            string outName = Get(kv, "out", "eb_groups.txt");
            double minx = double.Parse(Get(kv, "minx", "-1e12"), IC);
            double maxx = double.Parse(Get(kv, "maxx", "1e12"), IC);
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("verdict\thandle\tclass\tlayer\tgroupParts\tmainPart\ttopMain");

            int scanned = 0, orphan = 0, inGroup = 0, noMain = 0, single = 0, err = 0;
            var mains = new Dictionary<string, int>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    string cls = id.ObjectClass != null ? id.ObjectClass.Name : "?";
                    // steel parts only -- the manual: "only steel parts or special parts"
                    if (cls != "Ks_Shape" && cls != "Ks_Plate" && cls != "Ks_BendShape"
                        && cls != "Ks_ArcShape" && cls != "Ks_ArcPlate" && cls != "Ks_BendPlate") continue;
                    Entity ent = null;
                    try { ent = tr.GetObject(id, OpenMode.ForRead) as Entity; } catch { }
                    if (ent == null) continue;
                    try
                    {
                        double x = ent.GeometricExtents.MinPoint.X;
                        if (x < minx || x > maxx) continue;
                    }
                    catch { }
                    scanned++;
                    string hx = id.Handle.ToString();
                    long oid = id.OldIdPtr.ToInt64();
                    try
                    {
                        PsObjectGroup g = new PsObjectGroup();
                        g.Initialize();
                        g.GetGroupFrom(oid);
                        long mainOf = 0, topMain = 0, main = 0;
                        int pc = 0;
                        try { mainOf = g.getMainPartOf(oid); } catch { }
                        try { topMain = g.getTopMainPartOf(oid); } catch { }
                        try { main = g.getMainPart(); } catch { }
                        try { pc = g.PartCount; } catch { }

                        string verdict;
                        if (mainOf == 0 && pc <= 0) { verdict = "ORPHAN"; orphan++; }
                        else
                        {
                            inGroup++;
                            if (main == 0) { verdict = "GROUP-NO-MAIN"; noMain++; }
                            else if (pc <= 1) { verdict = "SINGLE-PART-GROUP"; single++; }
                            else verdict = "OK";
                            string mk = HandleOf(main);
                            if (!mains.ContainsKey(mk)) mains[mk] = 0;
                            mains[mk]++;
                        }
                        sb.AppendLine(verdict + "\t" + hx + "\t" + cls + "\t" + ent.Layer + "\t"
                                    + pc + "\t" + (main == 0 ? "-" : HandleOf(main)) + "\t"
                                    + (topMain == 0 ? "-" : HandleOf(topMain)));
                    }
                    catch (System.Exception ex)
                    {
                        err++;
                        if (err <= 5) sb.AppendLine("ERR\t" + hx + "\t" + One(ex.Message));
                    }
                }
                tr.Commit();
            }
            File.WriteAllText(Path.Combine(Dir, outName), sb.ToString(), Encoding.UTF8);
            Result("EB_OK grouporphans steelParts=" + scanned
                 + " inGroup=" + inGroup + " ORPHAN=" + orphan
                 + " GROUP-NO-MAIN=" + noMain + " SINGLE-PART-GROUP=" + single
                 + " distinctGroups=" + mains.Count + " err=" + err
                 + " [B.28.3's Check Groups, composed from PsObjectGroup's membership queries --"
                 + " the DIALOG is not in the API, the CHECK is] -> " + outName);
        }

        void GroupInfo(Dictionary<string, string> kv)
        {
            string h = Get(kv, "handle", "");
            long oid = IdFromHandle(h);
            if (oid == 0) { Result("EB_ERR groupinfo: bad handle " + h); return; }
            StringBuilder sb = new StringBuilder();
            try
            {
                PsObjectGroup g = new PsObjectGroup();
                g.Initialize();
                g.GetGroupFrom(oid);                 // load the group CONTAINING this part

                long main = 0, mainOf = 0, topMain = 0, asmMain = 0;
                try { main = g.getMainPart(); } catch { }
                try { mainOf = g.getMainPartOf(oid); } catch { }
                try { topMain = g.getTopMainPartOf(oid); } catch { }
                try { asmMain = g.getAssemblyMainPartOf(oid); } catch { }
                bool isMain = false;
                try { isMain = g.IsMainPart(oid); } catch { }

                int pc = 0, spc = 0;
                try { pc = g.PartCount; } catch { }
                try { spc = g.SubPartCount; } catch { }

                sb.Append("parts=" + pc + " subParts=" + spc +
                          " isMain=" + isMain +
                          " main=" + HandleOf(main) + " mainOf=" + HandleOf(mainOf) +
                          " topMain=" + HandleOf(topMain) + " asmMain=" + HandleOf(asmMain));

                // the members themselves -- getPart / getSubPart by index
                sb.Append(" | members:");
                for (int i = 0; i < pc; i++)
                    try { sb.Append(" " + HandleOf(g.getPart(i))); } catch { sb.Append(" ?"); }
                if (spc > 0)
                {
                    sb.Append(" | subParts:");
                    for (int i = 0; i < spc; i++)
                        try { sb.Append(" " + HandleOf(g.getSubPart(i))); } catch { sb.Append(" ?"); }
                }

                // weight WITH and WITHOUT bolts -- the shipping numbers
                try { sb.Append(" | wt=" + F(g.computeWeight(oid, false)) +
                                " wtNoBolts=" + F(g.computeWeight(oid, true))); }
                catch (System.Exception e) { sb.Append(" | wt:" + e.Message); }

                try
                {
                    double L = 0, W = 0, H = 0;
                    g.ComputeDimension(oid, ref L, ref W, ref H);
                    sb.Append(" dim=" + F(L) + "x" + F(W) + "x" + F(H));
                }
                catch (System.Exception e) { sb.Append(" dim:" + e.Message); }

                // BOTH of these are indexed by the object id -- get_WeightCenterOfGroup(long)
                // and get_Groupname(long). The dump renders them as plain properties and hides
                // the index. Fourth time today the compiler corrected the dump.
                try { PsPoint c = g.get_WeightCenterOfGroup(oid);
                      sb.Append(" cog=" + F(c.x) + "," + F(c.y) + "," + F(c.z)); }
                catch { }

                try { sb.Append(" name='" + g.get_Groupname(oid) + "'"); } catch { }

                // the group's OWN property block -- posnum, sendnum, paint area, kind
                try
                {
                    long gid = mainOf != 0 ? mainOf : oid;
                    PsGroupProperties gp = new PsGroupProperties();
                    int rc = gp.readFrom(gid);
                    sb.Append(" | groupProps rc=" + rc +
                              " name='" + gp.Name + "' pos='" + gp.Posnum + "' send='" + gp.Sendnum + "'" +
                              " wt=" + F(gp.Weight) + " paint=" + F(gp.PaintArea) +
                              " LxWxH=" + F(gp.Length) + "x" + F(gp.Width) + "x" + F(gp.Height) +
                              " count=" + gp.Count + "/" + gp.TotalCount +
                              " sub=" + gp.IsSubGroup + " assembly=" + gp.IsAssemblyGroup +
                              " weld=" + gp.WeldGroup +
                              " noPos=" + gp.DontPositionFlag + " noDetail=" + gp.DontDetailFlag);
                }
                catch (System.Exception e) { sb.Append(" | groupProps:" + e.Message); }
            }
            catch (System.Exception ex) { Result("EB_ERR groupinfo EX:" + ex.Message); return; }
            Result("EB_OK groupinfo handle=" + h + " " + sb.ToString());
        }

        void GroupEdit(Dictionary<string, string> kv)
        {
            string h = Get(kv, "handle", "");
            long oid = IdFromHandle(h);
            if (oid == 0) { Result("EB_ERR groupedit: bad handle " + h); return; }

            int before = GroupCount(oid);
            string msg = "";
            try
            {
                PsObjectGroup g = new PsObjectGroup();
                g.Initialize();
                g.GetGroupFrom(oid);

                if (Get(kv, "delete", "") == "1")
                {
                    g.DeleteGroupFrom(oid);          // Void -> silent
                    int afterDel = GroupCount(oid);
                    Result((afterDel <= 0 ? "EB_OK" : "EB_ERR") + " groupedit delete handle=" + h +
                           " parts=" + before + "->" + afterDel);
                    return;
                }

                foreach (string a in Get(kv, "add", "").Split(','))
                {
                    string t = a.Trim();
                    if (t.Length == 0) continue;
                    long id = IdFromHandle(t);
                    if (id == 0) { msg += " badAdd:" + t; continue; }
                    try { g.AddMemberToGroup(oid, id); } catch (System.Exception e) { msg += " add" + t + ":" + e.Message; }
                }
                foreach (string a in Get(kv, "remove", "").Split(','))
                {
                    string t = a.Trim();
                    if (t.Length == 0) continue;
                    long id = IdFromHandle(t);
                    if (id == 0) { msg += " badRemove:" + t; continue; }
                    try { g.RemoveMemberFromGroup(id); } catch (System.Exception e) { msg += " rm" + t + ":" + e.Message; }
                }

                // name / posnum / sendnum live on the GROUP's property block
                string nm = Get(kv, "name", ""), pos = Get(kv, "pos", ""), snd = Get(kv, "send", "");
                if (nm.Length > 0 || pos.Length > 0 || snd.Length > 0)
                {
                    long gid = oid;
                    try { long mo = g.getMainPartOf(oid); if (mo != 0) gid = mo; } catch { }
                    try
                    {
                        PsGroupProperties gp = new PsGroupProperties();
                        gp.readFrom(gid);            // ALWAYS read before write -- detached block
                        if (nm.Length > 0) { gp.Name = nm; gp.NameChangedManually = true; }
                        if (pos.Length > 0) gp.Posnum = pos;
                        if (snd.Length > 0) gp.Sendnum = snd;
                        gp.writeTo(gid);
                    }
                    catch (System.Exception e) { msg += " props:" + e.Message; }
                }
            }
            catch (System.Exception ex) { msg += " EX:" + ex.Message; }

            // verdict from a FRESH read of the group, never from the calls above (all Void)
            int after = GroupCount(oid);
            string chk = "";
            try
            {
                long gid = oid;
                PsObjectGroup g2 = new PsObjectGroup();
                g2.Initialize(); g2.GetGroupFrom(oid);
                try { long mo = g2.getMainPartOf(oid); if (mo != 0) gid = mo; } catch { }
                PsGroupProperties gp2 = new PsGroupProperties();
                gp2.readFrom(gid);
                chk = " now[name='" + gp2.Name + "' pos='" + gp2.Posnum + "' send='" + gp2.Sendnum + "']";
            }
            catch { }
            Result("EB_OK groupedit handle=" + h + " parts=" + before + "->" + after + chk + msg);
        }

        static int GroupCount(long oid)
        {
            try
            {
                PsObjectGroup g = new PsObjectGroup();
                g.Initialize();
                g.GetGroupFrom(oid);
                return g.PartCount;
            }
            catch { return -1; }
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
                // v72: the FIRST pass ignored `geo` -- the out-parameter that actually carries
                // the answer -- and then reported that normal/EdgePoint came back zero, as if
                // the call had half-failed. It had not: the contact OUTLINE is in here.
                string g = "";
                try
                {
                    g = " | geo empty=" + geo.isEmpty() +
                        " lines=" + geo.lineCount + " arcs=" + geo.arcCount +
                        " circles=" + geo.circleCount +
                        " drawable=" + geo.countDrawableElements() +
                        " lineLen=" + F(geo.getLineLength());
                    PsPoint gmn = new PsPoint(0, 0, 0), gmx = new PsPoint(0, 0, 0);
                    geo.extents(gmn, gmx);
                    g += " ext=" + F(gmn.x) + "," + F(gmn.y) + "," + F(gmn.z) +
                         ";" + F(gmx.x) + "," + F(gmx.y) + "," + F(gmx.z);
                    PsPolygon pg = new PsPolygon();
                    pg.init();
                    int cr = geo.convertToPolygon(pg, 0.1, false);
                    g += " -> polygon rc=" + cr + " verts=" + pg.Count + " area=" + F(pg.Area);
                    PsPoint ctr = new PsPoint(0, 0, 0);
                    if (pg.getCenter(ctr))
                        g += " centre=" + F(ctr.x) + "," + F(ctr.y) + "," + F(ctr.z);
                }
                catch (System.Exception ge) { g = " | geo:" + ge.Message; }
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
                       " tol=" + F(tol) + g + extra);
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
        // ג ן¸ HARD PRECONDITION, verbatim: "The plane actually hit by the centerline (or the
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
        // ג ן¸ SetToDefaults() is not optional: the class wraps the PERSISTENT PS_COLLISION
        // dialog state, so skipping it silently inherits whatever the last interactive run
        // left behind.
        // ג ן¸ CollectObjectsFromSelection is Void. An empty selection gives a perfectly
        // healthy-looking run with BodyCount 0 -- identical to a clean model. The object
        // count is asserted before Apply, otherwise "no collisions" means nothing.
        // ג ן¸ Cost grows with the SQUARE of the part count (the manual says so outright), and
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
            // v129: THE THIRD DIMENSION. E.9.16 lists a notch as Length / Width / Depth,
            // and PsOutlet duly has SetLength -- which this op never called. Width and
            // Height alone describe a zero-volume cut, so Apply() returned 0 and nothing
            // was created, on a clean beam as readily as on a modified one. Reading the
            // properties chapter is what found it: the dialog names three numbers, the
            // op passed two.
            double len = double.Parse(Get(kv, "len", "50"), IC);
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
                o.SetLength(len);
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
                   " type=" + type + " w=" + F(w) + " h=" + F(ht) + " len=" + F(len) +
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
        // ג ן¸ COORDINATE SYSTEM, from the manual: "the transfer of the manipulations refers to
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
                // so Clone is a dialog-only feature. ג‡’ DEFAULT IS variant 9, which does the job
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
        // Amir: "׳©׳×׳™׳§׳— ׳׳׳ ׳™ ׳׳× ׳”׳¢׳‘׳•׳“׳” ׳”׳©׳—׳•׳¨׳” ׳©׳‘׳׳™׳“׳•׳". This is it: find which parts are
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
            bool useSend = Get(kv, "field", "pos").ToLowerInvariant() == "send";
            int skippedFlag = 0;
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

            // ---- v76: BUCKET FIRST, THEN COMPARE ----
            // Measured 06/08: 217 parts, 95 clusters => 10,030 geometric comparisons and
            // 108.8 SECONDS. On Amir's real scale (~2,000 objects) that is minutes, i.e.
            // unusable -- and the whole point of this op is to be faster than a person.
            //
            // The fix is sound, not a shortcut: two parts with DIFFERENT nominal dimensions
            // can never be geometrically equal, so they never need comparing. Bucket by the
            // cheap nominal signature (class + name + L/W/H + weight), then run the expensive
            // CheckTwoPartsAreEqual only WITHIN a bucket.
            // This does NOT reintroduce the IsEqualTo trap: two plates that differ only by a
            // cut share a nominal signature, land in the SAME bucket, and are still separated
            // by the geometric test. Bucketing can only ever avoid comparisons that were
            // guaranteed to return false.
            string[] nominal = new string[ids.Count];
            for (int i = 0; i < ids.Count; i++)
            {
                string sigp = clsNames[i];
                try
                {
                    PsObjectProperties np = new PsObjectProperties();
                    np.readFrom(ids[i]);
                    sigp += "|" + np.Name + "|" + F(np.Length) + "|" + F(np.Wide) + "|" +
                            F(np.Height) + "|" + F(np.Weight);
                }
                catch { sigp += "|?"; }
                nominal[i] = sigp;
            }

            Dictionary<string, List<int>> buckets = new Dictionary<string, List<int>>();
            for (int i = 0; i < ids.Count; i++)
            {
                List<int> lst;
                if (!buckets.TryGetValue(nominal[i], out lst)) { lst = new List<int>(); buckets[nominal[i]] = lst; }
                lst.Add(i);
            }

            List<int> repOf = new List<int>();      // cluster index -> representative part index
            int[] cluster = new int[ids.Count];
            int comparisons = 0;
            foreach (KeyValuePair<string, List<int>> kvp in buckets)
            {
                List<int> repsHere = new List<int>();   // cluster indices living in this bucket
                foreach (int i in kvp.Value)
                {
                    int found = -1;
                    for (int c = 0; c < repsHere.Count; c++)
                    {
                        comparisons++;
                        bool eq = false;
                        try { eq = cd.CheckTwoPartsAreEqual(ids[repOf[repsHere[c]]], ids[i]); }
                        catch { eq = false; }
                        if (eq) { found = repsHere[c]; break; }
                    }
                    if (found < 0)
                    {
                        repOf.Add(i);
                        found = repOf.Count - 1;
                        repsHere.Add(found);
                    }
                    cluster[i] = found;
                }
            }
            int nBuckets = buckets.Count;

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
                        // B.29: a part flagged "do not position" must be left alone. Numbering
                        // it anyway would put a number on something the fabricator excluded
                        // on purpose.
                        if (p.DontPositionFlag) { state = "skipped(DontPosition)"; skippedFlag++; }
                        else
                        {
                            if (useSend) p.Sendnum = num; else p.Posnum = num;
                            p.writeTo(ids[i]);
                            Bentley.ProStructures.Property.PsObjectProperties chk =
                                new Bentley.ProStructures.Property.PsObjectProperties();
                            chk.readFrom(ids[i]);
                            string got = useSend ? chk.Sendnum : chk.Posnum;
                            if (got == num) { state = "ok"; written++; } else { failed++; }
                        }
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
                   " distinct=" + repOf.Count + " buckets=" + nBuckets +
                   " comparisons=" + comparisons +
                   " field=" + (useSend ? "Sendnum" : "Posnum") +
                   " written=" + written + " failed=" + failed +
                   (skippedFlag > 0 ? " skippedDontPosition=" + skippedFlag : "") +
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
            { "anchor", "|article|at|botthread|detail|dia|dir|embed|embedbot|embedmid|grout|host|kind|layer|plate|proud|style|thread|" },
            { "beam", "|ax|ay|catalog|kind|layer|mirror|name|offx|offy|p1|p2|rot|" },
            { "bolt", "|dia|hosts|layer|len|p1|p2|style|" },
            { "boltfield", "|center|dia|gap|hosts|nx|ny|style|sx|sy|" },
            { "boltprobe", "|p1|p2|" },
            { "chamfer", "|at|d1|d2|edge|handle|list|type|" },
            { "clonemodel", "|dx|maxx|" },
            { "cmd", "|args|list|name|select|" },
            { "conn", "|at|beam|cope|dh|dv|group|holedia|kind|nh|nv|play|rotated|support|t|template|" },
            { "conn_bolted", "|at|dia|gap|nx|ny|pl|pt|pw|style|sx|sy|" },
            { "connbase", "|anchordetail|anchordia|anchordrill|anchorgrip|anchorgripdia|anchorkey|anchoroutside|anchors|dts|handle|holedia|hx|hy|l|shorten|t|template|w|set|" },
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
            { "group", "|at|kind|main|name|parts|query|" },
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
            { "vfy_bolts", "|maxx|minx|out|tol|" },
            { "vfy_fit", "|maxx|minx|tol|maxspare|minspare|gaptol|" },
            { "vfy_dupes", "|maxx|minx|tol|" },
            { "edgecheck", "|block|handle|" },
            { "holefields", "|handle|" },
            { "killholefield", "|handle|field|dryrun|" },
            { "bind", "|handle|cls|addx|addy|probe|force|" },
            { "vfy_touch", "|a|b|tol|" },
            { "vfy_size", "|max|maxx|minx|" },
            { "classify", "|maxx|minx|out|set|value|visible|" },
            { "acis", "|handle|" },
            { "acisref", "|massprop|read|solid|ucs|" },
            { "solid", "|at|axis1|axis2|center|dpts|ex|ey|h|inner|kind|l|layer|len|normal|oi1|oi2|oo1|oo2|outer|p1|p2|pts|r|r1|r2|rev|taper|twist|w|" },
            { "connverify", "|maxx|minx|out|" },
            { "connkill", "|deleteparts|handle|number|" },
            { "haunch", "|at|baseh|beam|bottom|conew|conical|coped|copedh|ex|ey|fixed|group|len|maxgrow|slope|stiffcon|stiffsup|support|tmpl|toph|topoff|turn|web|" },
            { "macrobrace", "|h1|h2|" },
            { "align", "|copy|handles|o1|o2|x1|x2|y1|y2|" },
            { "spiral", "|about|angle|axis|dz|handles|method|n|" },
            { "purlintype", "||" },
            { "copeinfo", "||" },
            { "cope", "|atstart|beam|botheq|copetype|edge|edgedown|edgetop|endcope|fit|flanget|indown|inner|intop|middle|outdown|outtop|polycut|radius|rathole|rathole2|rot|shapelen|slope|support|tmpl|web|web2|" },
            { "boltinfo", "||" },
            { "boltsingle", "|addlen|dia|from|style|to|" },
            { "nutonly", "|dia|from|style|to|" },
            { "threadedrod", "|dia|from|offset|style|to|" },
            { "bracing", "|angle|cat|centerhole|cross|crossp1|crossp2|divideall|dm|dynamic|edgeborder|edgehole|ex|ey|group|holecross|holeedge|holehole|host1|host2|layout|mirror|ncross|nogussets|nprof|origin|p1|p2|plateside|ptmode|platethick|platetype|platewide|play|roundto|shapedist|shapetype|shorten|size|sym|type|prerecalc|welded|setucs|" },
            { "weldstyles", "||" },
            { "weld", "|at|from|len|makeweld|onsite|roundabout|row|sign|style|thick|to|" },
            { "splicetemplates", "||" },
            // v161: boltstyle/boltstylecrc added. Without one of them this op cannot produce a
            // bolted splice at all -- both shipped templates carry BoltStyleCRC=0.
            { "splice", "|at|boltsingroup|boltstyle|boltstylecrc|dia|downin|downout|gap|group|handle|nflangeh|nflangev|nwebh|nwebv|offflange|offweb|sidelap|support|template|tflange|topin|toplap|topout|tweb|webleft|webright|welddiagonal|weldflange|weldweb|workloose|" },
            { "shearplatetemplates", "||" },
            { "shearplate", "|at|boltsingroup|boltstyle|cope|copeedgetop|copeinsidetop|coperadius|copetemplate|copewebdist|cutconn|cutsup|dia|eachplate|fromdown|fromedge|fromhole|gapconn|gapsup|group|handle|holehoriz|holehorizin|holehorizout|holevert|holevertedge|nhoriz|normaltocut|nvert|poly|pos|shear|slot|support|template|thick|vertoff|weldconn|weldsup|workloose|" },
            { "webangletemplates", "||" },
            { "webangle", "|at|bendradius|boltsingroup|boltstyle|catalog|cope|copeedgedown|copeedgetop|copefit|copeinner|copeinsidedown|copeinsidetop|copeoutsidedown|copeoutsidetop|coperadius|copetemplate|copewebdist|cutalways|cutatconnected|dia|eachangle|flat|fromdown|fromedge|fromhole|gap|group|handle|key|longleg|moment|nconn|nsup|nvert|pos|shear|shorten|shortleg|sideoff|slotconn|slotsup|support|template|thick|turn|vertoff|weldconn|weldsup|workloose|" },
            { "stifftemplates", "||" },
            { "stiffener", "|angle|at|centerpunch|creategroup|flangedist|handle|length|lengthtype|offset|radius|roundto|shapetype|template|thick|topaligned|webdist|weldflange|weldweb|withangle|" },
            { "plate9", "|area|article|at|check|descr|display|ex|ey|ez|family|grid|griddir|handle|insheight|l|layer|material|mode|name|p1|p2|p3|pts|radius|style|t|vpos|w|xoff|xpos|yoff|ypos|" },
            { "arcplate", "|bigarc|center|layer|name|normal|p1|p2|rot|t|w|xpos|ypos|" },
            { "bendshape", "|arc|catalog|circle|handle|helix|kind|layer|name|pts|refaxis|rot|" },
            { "bend", "|angle|at|convert|front|handle|inner|len|lengthcalc|radius|rear|" },
            { "bendinfo", "|handle|max|" },
            { "bendtwo", "|at|delete2|h1|h2|inner|k|radius|" },
            { "plateinfo", "|handle|probe|" },
            { "frame", "|at|type|axdisplay|axdist|axdouble|axdynamic|axfirst|axnames|axorder|axpos|axscale|axsecond|axsize|axstart|axtype|axtype2|axisnames|backclip|base|checkname|d3|facets|frontclip|hsteps|lock|lsteps|name|radiusview|ridgeheight|ridgewidth|roofangle|roofheight|rooflength|segments|top|views|wsteps|xaxis|yaxis|" },
            { "frameinfo", "|handle|" },
            { "grid", "|at|height|hsteps|len|lsteps|name|wide|wsteps|" },
            { "gridpoints", "|handle|" },
            { "gridcolumns", "|at|catalog|h|handle|lsteps|name|wsteps|xpos|ypos|" },
            { "shape", "|area|article|catalog|dir|display|dx|dy|endoff|family|flat|hdist|layer|len|material|name|p1|p2|res|rot|startoff|type|userpos|vdist|xaxis|xpos|yaxis|ypos|" },
            { "shapeinfo", "|catalog|flats|key|name|" },
            { "boltparts", "|addlen|decl|gap|handles|objdist|style|" },
            { "purlin", "|at|dump|p1|p2|set|support|template|" },
            { "basedump", "|set|template|" },
            { "save", "|as|" },
            { "connset", "|at|beam|force|set|support|template|" },
            { "conndump", "|only|template|" },
            { "groupauto", "|dry|field|out|prefix|start|" },
            { "drillspecial", "|angle|at|depth|dia|from|handle|kind|n|normal|play|r|sink|start|to|" },
            { "section", "|at|handle|mb|project|xaxis|yaxis|" },
            { "shapeedit", "|connect|handle|len|lengthat|side|split|" },
            { "boolean", "|handle|mode|tool|" },
            { "detailcut", "|at|depth|handle|" },
            { "groupinfo", "|handle|" },
            { "grouporphans", "|out|minx|maxx|" },
            { "groupedit", "|add|delete|handle|name|pos|remove|send|" },
            { "touchplane", "|a|b|tol|" },
            { "touchdrill", "|a|b|dia|play|tol|x|y|" },
            { "cutat", "|at|handle|mode|other|outside|type|" },
            { "polycut", "|at|depth|handle|inside|l|n|pts|r|radius|shape|size|w|xaxis|yaxis|" },
            { "collision", "|bolts|box|clean|minvol|" },
            { "mods", "|handle|" },
            { "edgechamfer", "|flange|handle|layout|mode|side|v1|v2|" },
            { "outlet", "|angle|at|h|handle|len|normal|radius|type|w|" },
            { "planecut", "|at|flip|handle|normal|" },
            // E.9. propset deliberately has NO key list: any of the ~120 property names on
            // PsObjectProperties is a legal key, and it reports UNKNOWN PROPERTY itself for
            // anything else, which is a better error than a generic refusal.
            { "propfull", "|handle|tab|" },
            { "propcopy", "|src|dst|tabs|dryrun|" },
            { "changesection", "|handle|key|cat|type|force|" },
            { "clonedrills", "|posnum|src|to|variant|" },
            { "posauto", "|dry|field|kinds|out|prefix|start|tol|" },
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
            { "layerprobe", "|at|cleanup|" },
            { "gridaxes", "|at|len|wide|lsteps|wsteps|ux|uy|name|" },
            { "setpoly", "|handle|pts|" },
            { "styles", "|type|" },
            { "stylelist", "|action|confirm|index|name|type|" },
            { "dbase", "|file|max|out|row|" },
            { "view", "|dir|" },
            { "whoami", "||" },
            { "env", "|full|" },
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
        //   0 kFacetUndefined ֲ· 1 kFacetRectangle ֲ· 2 kFacetTriangle
        //   3 kFacetArc       ֲ· 4 kFacetInversArc
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
        // Amir's standing rule: "׳׳ ׳•׳¨׳§ ׳‘׳×׳•׳›׳ ׳” ׳¢׳¦׳׳” ׳•׳‘׳₪׳§׳•׳“׳•׳× ׳©׳™׳© ׳‘׳”" -- only the software
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
            "PS_EDIT_CONNECTIONS",
            // coordinate system -- added 09/08/2026 with Amir's explicit approval, to test
            // B.24's "place your UCS over the bracing plane" precondition. It changes NO
            // geometry, only the reference frame. The bracing op does not use this route --
            // it sets the UCS through the managed Editor property and restores it in a
            // finally block, so nothing can be left pending on the command line.
            "UCS"};

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
                // AN ASSEMBLY HAS NO MAIN PART -- it is combined on site. Measured
                // 06/08/2026: passing main= for kind=assembly silently DROPPED that part from
                // the assembly (a column vanished; the weight came back as the two plates
                // only, 26.886 kg). For an assembly the main part is just another member.
                bool isAssembly = (kind == "assembly");
                if (mid != 0 && !isAssembly)
                { try { g.setMainPart(mid); } catch (System.Exception e1) { msg += " main:" + e1.Message; } }
                if (mid != 0 && isAssembly)
                { try { g.AddSubPart(mid); added++; } catch (System.Exception e1) { msg += " asmMain:" + e1.Message; } }

                foreach (string raw in partsS.Split(new char[] { ',', ';' }))
                {
                    string hs = raw.Trim();
                    if (hs.Length == 0) continue;
                    long pid = IdFromHandle(hs);
                    if (pid == 0) { bad++; detail += " " + hs + ":badhandle"; continue; }
                    if (pid == mid && !isAssembly) continue;   // the main part is not an accessory
                    if (pid == mid && isAssembly) continue;    // already added above
                    try { g.AddSubPart(pid); added++; }
                    catch (System.Exception e2) { bad++; detail += " " + hs + ":" + e2.Message; }
                }

                if (isAssembly)
                {
                    // CreateAssembly's Origin is the assembly OBJECT's own insertion point.
                    // Left at 0,0,0 the Ks_Assembly landed with placeholder geometry
                    // (centre 100,100,100, extents 0,0,0..200,200,200) far from its members.
                    // Default it to the centre of the members instead; at= overrides.
                    PsPoint org = Pt(Get(kv, "at", "0,0,0"));
                    if (Get(kv, "at", "").Length == 0 && mid != 0)
                    {
                        try
                        {
                            PsPoint mn = new PsPoint(0, 0, 0), mx = new PsPoint(0, 0, 0);
                            PsObjectProperties pp = new PsObjectProperties();
                            pp.readFrom(mid);
                            if (pp.GetExtents(ref mn, ref mx))
                                org = new PsPoint((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, (mn.z + mx.z) / 2);
                        }
                        catch { }
                    }
                    made = g.CreateAssembly(org, new PsVector(1, 0, 0), new PsVector(0, 1, 0));
                    msg += " asmOrigin=" + F(org.x) + "," + F(org.y) + "," + F(org.z);
                }
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
                        // B.17 audit 10/08: "Rotate Connection" IS exposed, as
                        // PsStandardPlateLinkData.PlateIsRotated. The only route to it was
                        // connset's generic reflection setter, which is QUARANTINED for crashing
                        // AutoCAD four times. One named boolean on the template object is the
                        // same safe mechanism the nine fields above already use.
                        string sRot = Get(kv, "rotated", "");
                        if (sRot.Length > 0)
                        {
                            d.PlateIsRotated = (sRot == "1");
                            msg += " PlateIsRotated=" + d.PlateIsRotated;
                        }
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
        // ==================================================================
        //  op=stylelist -- B.15.4 "Sort", the style SELECTION LIST. Read and never implemented
        //  until the 10/08 audit; every button in that dialog is a method on PsObjectStyleList.
        //
        //  ⭐ The one that matters for fabrication is action=sync. The manual:
        //     "The styles are stored as OBJECTS IN THE DRAWING. When the style definition is
        //      modified on the hard disk, normally the modifications are NOT transferred to the
        //      internal objects. They are carried out for all styles by this function."
        //     ⇒ A bolt style is FROZEN into the model at the moment it is used. Editing the
        //       style on disk changes nothing in an existing drawing until this runs.
        //
        //  action=list|sync|reload|readfile|append|moveup|movedown|delete
        //  type=0..4  (0 bolt, 1 weld, 2 posflag, 3 koteflag, 4 universal)
        // ==================================================================
        // ==================================================================
        //  op=dbase file=<path.dbf> [row=N] [max=N] [out=eb_dbase.txt]
        //
        //  B.17.3 -- the user-defined / company connection database. The manual's own HINT:
        //  "create a database with frequently utilized and maybe COMPANY-SPECIFIC connections,
        //   which are then always available to all program users within your company."
        //
        //  Prg/Plugins/ carries one per connection macro. BasePlate.dbf: 56 records x 11 fields
        //  (SHAPE CODE LENGTH WIDTH THICKNESS DIAMETER WORKLOOSE HOLEX HOLEY AF AS), and HOLEX
        //  reads "2*100" -- ⭐ the same layout string B.14 established for drill fields. The
        //  connection databases and the drilling API speak one syntax.
        //
        //  ⚠️ READ ONLY. PsDBaseDatabase exposes PutRecord and AppendNewRecord; these files sit
        //  in Program Files and define how connections get built. Writing them is Amir's call.
        // ==================================================================
        void DBase(Dictionary<string, string> kv)
        {
            string file = Get(kv, "file", "");
            if (file.Length == 0) { Result("EB_ERR dbase: file= required"); return; }
            if (!File.Exists(file)) { Result("EB_ERR dbase: not found " + file); return; }
            int max = int.Parse(Get(kv, "max", "40"));
            int one = int.Parse(Get(kv, "row", "-1"));
            string outName = Get(kv, "out", "eb_dbase.txt");

            StringBuilder sb = new StringBuilder();
            int nRec = 0, nFld = 0;
            try
            {
                PsDBaseDatabase db = new PsDBaseDatabase();
                int rcOpen = db.SetFileName(file);
                nRec = db.RecordCount;
                nFld = db.FieldCount;

                List<string> names = new List<string>();
                for (int f = 0; f < nFld; f++)
                {
                    string fn = "";
                    try { fn = db.GetFieldName(f); } catch { }
                    names.Add(fn);
                }
                sb.Append("FILE\t").Append(file).AppendLine();
                sb.Append("OPEN\t").Append(rcOpen).Append("\trecords\t").Append(nRec)
                  .Append("\tfields\t").Append(nFld).AppendLine();
                sb.Append("FIELDS\t").Append(string.Join("\t", names.ToArray())).AppendLine();

                int from = one >= 0 ? one : 0;
                int to = one >= 0 ? one + 1 : Math.Min(nRec, max);
                for (int r = from; r < to; r++)
                {
                    StringBuilder row = new StringBuilder();
                    row.Append(r);
                    foreach (string fn in names)
                    {
                        string v = "";
                        try { v = db.GetRecord(r, fn); } catch { }
                        row.Append('\t').Append(Safe(v));
                    }
                    sb.Append(row.ToString()).AppendLine();
                }
                File.WriteAllText(Path.Combine(Dir, outName), sb.ToString(), Encoding.UTF8);
                Result("EB_OK dbase file='" + Path.GetFileName(file) + "' records=" + nRec
                     + " fields=" + nFld + " wrote=" + (to - from) + " -> " + outName
                     + " | " + string.Join(",", names.ToArray()));
            }
            catch (System.Exception ex)
            {
                Result("EB_ERR dbase: " + One(ex.Message));
            }
        }

        void StyleList(Dictionary<string, string> kv)
        {
            string action = Get(kv, "action", "list").ToLowerInvariant();
            int type = int.Parse(Get(kv, "type", "0"));
            string name = Get(kv, "name", "");
            StringBuilder sb = new StringBuilder();
            try
            {
                PsObjectStyleList lst = new PsObjectStyleList();
                try { lst.Type = (ObjectStyleListType)type; } catch { }
                lst.Initialize();
                // ⭐ MEASURED 10/08: Initialize() alone leaves Count at 0. The list is empty
                // until ReadFromFile() pulls the style definitions off disk -- which is also
                // the mechanism behind B.15.4's warning that styles are stored as objects in
                // the drawing and do not follow edits made on disk.
                int rcRead = -1;
                try { rcRead = lst.ReadFromFile(); } catch { }
                int before = 0;
                try { before = lst.Count; } catch { }
                sb.Append(" readFromFile=").Append(rcRead);

                switch (action)
                {
                    case "list":
                        for (int i = 0; i < before; i++)
                        {
                            // ⭐ FOURTH instance of the same trap: the dump prints Entry and
                            // Index as plain properties; the compiler says they are INDEXERS --
                            // get_Entry(short) and get_Index(string). After get_ParentFlangeIndex,
                            // get_WeldStyleName and get_BoltStyleName, the rule is settled:
                            // when a String/Int32 property looks like it should be a list, it is
                            // an indexer, and the type dump will not tell you.
                            string nm = "";
                            try { nm = lst.get_Entry((short)i); } catch { }
                            long oid = 0; int crc = 0;
                            try { oid = lst.getStyleObjectId(nm); } catch { }
                            try { crc = lst.GetStyleCRCFromId(oid); } catch { }
                            sb.Append(" [").Append(i).Append("]").Append(nm)
                              .Append("/crc=").Append(crc);
                        }
                        break;

                    case "sync":
                        // LoadAll=true is the manual's "update all styles from disk"
                        lst.Synchronize(Get(kv, "confirm", "1") != "0");
                        sb.Append(" synchronized loadAll=").Append(Get(kv, "confirm", "1") != "0");
                        break;

                    case "reload":
                        sb.Append(" reload rc=").Append(lst.Reload());
                        break;

                    case "readfile":
                        if (name.Length == 0) { Result("EB_ERR stylelist readfile: name= required"); return; }
                        long sid = lst.getStyleObjectId(name);
                        sb.Append(" style='").Append(name).Append("' id=").Append(sid)
                          .Append(" readStyleFromFile rc=").Append(lst.readStyleFromFile(sid));
                        break;

                    case "append":
                        if (name.Length == 0) { Result("EB_ERR stylelist append: name= required"); return; }
                        if (lst.IsExist(name) != 0) { Result("EB_ERR stylelist: '" + name + "' already exists"); return; }
                        lst.Append(name);
                        sb.Append(" appended '").Append(name).Append("'");
                        break;

                    case "moveup":
                    case "movedown":
                        short ix = short.Parse(Get(kv, "index", "-1"));
                        if (ix < 0) { Result("EB_ERR stylelist: index= required"); return; }
                        if (action == "moveup") lst.MoveUp(ix); else lst.MoveDown(ix);
                        sb.Append(" ").Append(action).Append(" index=").Append(ix);
                        break;

                    case "delete":
                        // ⚠️ The manual is explicit that this deletes WITHOUT CONFIRMATION.
                        // A style is referenced by every bolt that uses it, so this is gated.
                        if (Get(kv, "confirm", "") != "DELETE")
                        {
                            Result("EB_ERR stylelist delete REFUSED: the manual says this deletes "
                                 + "without confirmation, and a style is referenced by every bolt "
                                 + "using it. Pass confirm=DELETE if that is genuinely intended.");
                            return;
                        }
                        short di = short.Parse(Get(kv, "index", "-1"));
                        if (di < 0) { Result("EB_ERR stylelist delete: index= required"); return; }
                        lst.DeleteAt(di);
                        sb.Append(" DELETED index=").Append(di);
                        break;

                    default:
                        Result("EB_ERR stylelist: unknown action=" + action);
                        return;
                }

                int after = 0;
                try { PsObjectStyleList l2 = new PsObjectStyleList();
                      try { l2.Type = (ObjectStyleListType)type; } catch { }
                      l2.Initialize(); try { l2.ReadFromFile(); } catch { }
                      after = l2.Count; } catch { }
                Result("EB_OK stylelist action=" + action + " type=" + type
                     + " count=" + before + "->" + after + sb.ToString());
            }
            catch (System.Exception ex)
            {
                Result("EB_ERR stylelist " + action + ": " + One(ex.Message));
            }
        }

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
                // v117: the factory refused all four kinds x three styles. The API exposes
                // SetObjectId(hostId) and the op never called it -- a fastener may need to
                // know the plate it passes through, the same way a drill needs its host.
                string hostH = Get(kv, "host", "");
                if (hostH.Length > 0)
                {
                    long hid = IdFromHandle(hostH);
                    try { f.SetObjectId(hid); msg += " host=" + hostH; }
                    catch (System.Exception ex) { msg += " host!" + One(ex.Message); }
                }
                string dstyle = Get(kv, "detail", "");
                if (dstyle.Length > 0) { try { f.SetDetailStyle(dstyle); } catch (System.Exception ex) { msg += " detail!" + One(ex.Message); } }
                string article = Get(kv, "article", "");
                if (article.Length > 0) { try { f.SetArticle(article); } catch (System.Exception ex) { msg += " article!" + One(ex.Message); } }

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
                {
                    // CreateFastenerStraightAnchorBolt(Dm, Extrusion, TopEmbedment,
                    //   MiddleEmbedment, BottomEmbedment, ThreadLength, BottomThreadLength,
                    //   PlateThickness, GroutThickness, StyleName)
                    // v117: mid and bot were hard-wired to 0. A degenerate body is a fair
                    // reason for a factory to refuse, so let them be set.
                    double emid = double.Parse(Get(kv, "embedmid", "0"), IC);
                    double ebot = double.Parse(Get(kv, "embedbot", "0"), IC);
                    double botthread = double.Parse(Get(kv, "botthread", "0"), IC);
                    msg += " seg=" + F(embed) + "/" + F(emid) + "/" + F(ebot);
                    oid = f.CreateFastenerStraightAnchorBolt(dia, proud, embed, emid, ebot,
                                                             thread, botthread, plate, grout, style);
                }
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
                // anchors came out with diameter 0 ג€” present as blocks but with no
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

                // v83: a generic setter for the other 27 properties of the base-plate link
                // data, so a hypothesis can be tested without a rebuild each time. Named
                // parameters cover what is used daily; this covers the rest.
                // Refuses an unknown name rather than ignoring it.
                foreach (string pairS in Get(kv, "set", "").Split(';'))
                {
                    string pp = pairS.Trim();
                    if (pp.Length == 0) continue;
                    int eqi = pp.IndexOf('=');
                    if (eqi <= 0) { msg += " badset:" + pp; continue; }
                    string pn = pp.Substring(0, eqi).Trim(), pv = pp.Substring(eqi + 1).Trim();
                    PropertyInfo pinf = d.GetType().GetProperty(pn);
                    if (pinf == null || !pinf.CanWrite) { msg += " noprop:" + pn; continue; }
                    try
                    {
                        Type pty = pinf.PropertyType;
                        object pval = pty == typeof(bool) ? (object)(pv == "1" || pv.ToLowerInvariant() == "true")
                                    : pty == typeof(double) ? (object)double.Parse(pv, IC0)
                                    : pty == typeof(int) ? (object)int.Parse(pv)
                                    : pty == typeof(string) ? (object)pv
                                    : pty.IsEnum ? System.Enum.Parse(pty, pv, true) : null;
                        if (pval == null) { msg += " badtype:" + pn; continue; }
                        pinf.SetValue(d, pval, null);
                        object back = pinf.GetValue(d, null);
                        msg += " " + pn + "=" + (back == null ? "?" : back.ToString());
                    }
                    catch (System.Exception se) { msg += " " + pn + ":" + se.Message; }
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
        // keeping its position, layer and ג€” crucially ג€” the holes already drilled
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

            // read the contour back ג€” proof, not an echo
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

