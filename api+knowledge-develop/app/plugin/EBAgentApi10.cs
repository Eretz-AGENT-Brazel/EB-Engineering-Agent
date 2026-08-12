// EBAgentApi.cs - EB PROSTEEL AGENT native modeling API (v9 - adds dumpmodel model-reader).
// Runs INSIDE AutoCAD 2015 + ProStructures V8i SS6 (NETLOAD).
// Creates REAL ProSteel objects (PsShape beams, PsPlate, PsBolt, miter cuts)
// programmatically - NO dialogs. Discovered via reflection dump of
// ProStructuresNet.dll (see api_dump_ProStructuresNet.txt).
//
// Protocol (file-based, avoids command-line quoting + supports Hebrew):
//   1. Python writes  eb_cmd.txt  (key=value lines, op=... first)
//   2. Python sends command  EB_RUN10
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
using Bentley.ProStructures.Steel.Shape;
using Bentley.ProStructures.Steel.Plate;
using Bentley.ProStructures.Steel.Bolt;
using Bentley.ProStructures.Modification.Edit;
using Bentley.ProStructures.Modeling;
// PsShapeLoader lives in Steel.Shape (already imported)

[assembly: CommandClass(typeof(EBAgent.ApiCmds10))]
[assembly: ExtensionApplication(typeof(EBAgent.EBApp))]

namespace EBAgent
{
    // Registers an assembly resolver so ProSteel's managed assemblies are found
    // in the Prg folder even from a cold AutoCAD session (before any ProSteel cmd).
    public class EBApp : IExtensionApplication
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
            _curCmd = e.GlobalCommandName;
            Write("\"ev\":\"cmd_start\",\"name\":\"" + J(e.GlobalCommandName) + "\"");
        }
        static void OnCmdEnd(object s, CommandEventArgs e)
        {
            if (Skip(e.GlobalCommandName)) return;
            Write("\"ev\":\"cmd_end\",\"name\":\"" + J(e.GlobalCommandName) + "\"");
            _curCmd = "";
        }
        static void OnCmdCancel(object s, CommandEventArgs e)
        {
            if (Skip(e.GlobalCommandName)) return;
            Write("\"ev\":\"cmd_cancel\",\"name\":\"" + J(e.GlobalCommandName) + "\"");
            _curCmd = "";
        }
        static void OnAdd(object s, ObjectEventArgs e)
        {
            try
            {
                string cls;
                try { cls = e.DBObject.ObjectId.ObjectClass.Name; }
                catch { cls = e.DBObject.GetType().Name; }
                Write("\"ev\":\"obj_add\",\"class\":\"" + J(cls) + "\",\"handle\":\""
                    + e.DBObject.Handle.ToString() + "\",\"cmd\":\"" + J(_curCmd) + "\"");
            }
            catch { }
        }
        static void OnErase(object s, ObjectErasedEventArgs e)
        {
            if (!e.Erased) return;
            try { Write("\"ev\":\"obj_erase\",\"handle\":\"" + e.DBObject.Handle.ToString() + "\""); }
            catch { }
        }
    }

    public class ApiCmds10
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

        [CommandMethod("EB_RUN10", CommandFlags.Modal)]
        public void Run()
        {
            var kv = ReadCmd();
            string op = Get(kv, "op", "");
            CurReqId = Get(kv, "reqid", "");
            try
            {
                switch (op)
                {
                    case "ping": Result("EB_OK ping " + DateTime.Now.ToString("HH:mm:ss")); break;
                    case "whoami": WhoAmI(); break;
                    case "learn_on": Rec.Start(Get(kv, "log", "")); Result("EB_OK learn_on " + Rec.StatusLine()); break;
                    case "learn_off": Rec.Stop(); Result("EB_OK learn_off " + Rec.StatusLine()); break;
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
                    case "dumpmodel2": DumpModel2(kv); break;
                    case "sections": Sections(kv); break;
                    case "dumpcat": DumpCat(kv); break;
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
                    bool ok = false;
                    try { ok = cs.Create(); } catch { ok = false; }
                    if (ok)
                    {
                        string h1, c1; int after = Census(out h1, out c1);
                        if (after > before)
                        {
                            Result("EB_OK beam name=" + nm + " catalog=" + (cat.Length > 0 ? cat : "(default)")
                                 + " handle=" + h1 + " class=" + c1 + " entities=" + after);
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
            PsPoint origin = new PsPoint(c[0], c[1], c[2]);
            PsVector normal = new PsVector(nz[0], nz.Length > 1 ? nz[1] : 0, nz.Length > 2 ? nz[2] : 1);
            string h0, c0; int before = Census(out h0, out c0);
            StringBuilder diag = new StringBuilder();

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
            string h0, c0; int before = Census(out h0, out c0);
            PsCreateBolt cb = new PsCreateBolt();
            cb.SetToDefaults();
            foreach (long id in ParseHandles(hosts)) { try { cb.AddObject(id); } catch { } }
            cb.CreateSingleBolt(p1, p2, dia, style, 0.0);
            string h1, c1; int after = Census(out h1, out c1);
            if (after > before) Result("EB_OK bolt dia=" + dia + " style=" + style + " handle=" + h1 + " class=" + c1 + " entities=" + after);
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
                        Entity e = o as Entity;
                        sb.Append("OTHER\t").Append(hnd).Append('\t').Append(cls).Append('\t')
                          .Append(e != null ? e.Layer : "").AppendLine();
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
        static string F(double d) { return d.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture); }


        // op=dumpmodel2 [out=eb_model2.txt]
        // UNIVERSAL reflection-based model reader. Does not assume the managed
        // type: reads whatever properties the real object exposes, each guarded
        // individually, so one bad property never loses the whole element.
        void DumpModel2(Dictionary<string, string> kv)
        {
            string outName = Get(kv, "out", "eb_model2.txt");
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            StringBuilder sb = new StringBuilder();
            int n = 0, nPs = 0;

            // properties worth harvesting (scalar + point) per object
            string[] scalars = new string[] {
                "CrossSectionName", "CrossSectionCatalog", "Length", "Width", "Height",
                "Wide", "Thickness", "Diameter", "Count", "TotalCount", "BoltStyleName",
                "Material", "Name", "InternalName", "Weight", "KlemmLength" };

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                foreach (ObjectId id in ms)
                {
                    n++;
                    string dxf = (id.ObjectClass != null ? id.ObjectClass.Name : "?");
                    string hnd = id.Handle.ToString();
                    string netType = "";
                    DBObject o = null;
                    try { o = tr.GetObject(id, OpenMode.ForRead); } catch (System.Exception ex1)
                    {
                        sb.Append("OBJ\t").Append(hnd).Append('\t').Append(dxf)
                          .Append("\tGETOBJ_FAIL:").Append(One(ex1.Message)).AppendLine();
                        continue;
                    }
                    try { netType = o.GetType().FullName; } catch { netType = "?"; }

                    // only harvest ProStructures objects (Ks_*) - skip AutoCAD helpers
                    bool isPs = dxf.StartsWith("Ks_") || netType.IndexOf("ProStructures") >= 0;
                    if (!isPs)
                    {
                        Entity ee = o as Entity;
                        sb.Append("OBJ\t").Append(hnd).Append('\t').Append(dxf)
                          .Append('\t').Append(netType).Append("\tlayer=")
                          .Append(ee != null ? ee.Layer : "").AppendLine();
                        continue;
                    }
                    nPs++;

                    sb.Append("PS\t").Append(hnd).Append('\t').Append(dxf).Append('\t').Append(netType);
                    System.Type t = null;
                    try { t = o.GetType(); } catch { }
                    if (t != null)
                    {
                        for (int i = 0; i < scalars.Length; i++)
                        {
                            string val = TryProp(o, t, scalars[i]);
                            if (val.Length > 0)
                                sb.Append('\t').Append(scalars[i]).Append('=').Append(val);
                        }
                        // point-valued properties
                        string[] pts = new string[] { "InsertPoint", "COGPoint", "WeightCenter" };
                        for (int i = 0; i < pts.Length; i++)
                        {
                            string pv = TryPoint(o, t, pts[i]);
                            if (pv.Length > 0)
                                sb.Append('\t').Append(pts[i]).Append('=').Append(pv);
                        }
                        // midline (shapes): method with two out-ish PsPoint args
                        string ml = TryMidLine(o, t);
                        if (ml.Length > 0) sb.Append('\t').Append("MidLine=").Append(ml);
                    }
                    sb.AppendLine();
                }
                tr.Commit();
            }
            File.WriteAllText(Path.Combine(Dir, outName), sb.ToString(), Encoding.UTF8);
            Result("EB_OK dumpmodel2 total=" + n + " ps=" + nPs + " -> " + outName);
        }

        static string One(string s) { return s == null ? "" : s.Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' '); }

        static string TryProp(object o, System.Type t, string name)
        {
            try
            {
                PropertyInfo pi = t.GetProperty(name);
                if (pi == null || !pi.CanRead) return "";
                object v = pi.GetValue(o, null);
                if (v == null) return "";
                if (v is double) return ((double)v).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
                return One(v.ToString());
            }
            catch { return ""; }
        }

        static string TryPoint(object o, System.Type t, string name)
        {
            try
            {
                PropertyInfo pi = t.GetProperty(name);
                if (pi == null || !pi.CanRead) return "";
                object v = pi.GetValue(o, null);
                PsPoint p = v as PsPoint;
                if (p == null) return "";
                return F(p.x) + "," + F(p.y) + "," + F(p.z);
            }
            catch { return ""; }
        }

        static string TryMidLine(object o, System.Type t)
        {
            try
            {
                MethodInfo mi = t.GetMethod("GetMidLine", new System.Type[] { typeof(PsPoint), typeof(PsPoint) });
                if (mi == null) return "";
                PsPoint a = new PsPoint(0, 0, 0), b = new PsPoint(0, 0, 0);
                mi.Invoke(o, new object[] { a, b });
                return F(a.x) + "," + F(a.y) + "," + F(a.z) + ";" + F(b.x) + "," + F(b.y) + "," + F(b.z);
            }
            catch { return ""; }
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
    }
}
