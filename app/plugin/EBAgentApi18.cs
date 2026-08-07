// EBAgentApi.cs - EB PROSTEEL AGENT native modeling API (v9 - adds dumpmodel model-reader).
// Runs INSIDE AutoCAD 2015 + ProStructures V8i SS6 (NETLOAD).
// Creates REAL ProSteel objects (PsShape beams, PsPlate, PsBolt, miter cuts)
// programmatically - NO dialogs. Discovered via reflection dump of
// ProStructuresNet.dll (see api_dump_ProStructuresNet.txt).
//
// Protocol (file-based, avoids command-line quoting + supports Hebrew):
//   1. Python writes  eb_cmd.txt  (key=value lines, op=... first)
//   2. Python sends command  EB_RUN18
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
using Bentley.ProStructures.Modification;
using Bentley.ProStructures.Modification.ObjectData;
using Bentley.ProStructures;
using Bentley.ProStructures.Modeling;
// PsShapeLoader lives in Steel.Shape (already imported)

[assembly: CommandClass(typeof(EBAgent.ApiCmds18))]
[assembly: ExtensionApplication(typeof(EBAgent.EBApp18))]

namespace EBAgent
{
    // Registers an assembly resolver so ProSteel's managed assemblies are found
    // in the Prg folder even from a cold AutoCAD session (before any ProSteel cmd).
    public class EBApp18 : IExtensionApplication
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

    public class ApiCmds18
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

        [CommandMethod("EB_RUN18", CommandFlags.Modal)]
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

                        Entity e3 = o as Entity;
                        sb.Append("OTHER\t").Append(hnd).Append('\t').Append(cls).Append('\t')
                          .Append(e3 != null ? e3.Layer : "").AppendLine();
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
                        Entity e3 = o as Entity;
                        sb.Append("OTHER\t").Append(hnd).Append('\t').Append(cls).Append('\t')
                          .Append(e3 != null ? e3.Layer : "").AppendLine();
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
            int lhm = int.Parse(Get(kv, "lhm", "0"));
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
            int lhm = int.Parse(Get(kv, "lhm", "0"));
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
                    if (slot > 0)
                    {
                        // oblong: elongate along X of the hole plane
                        try { d.SetRotateSlottedHoles(false); } catch { }
                        try { d.SetHoleStep(slot, dia); } catch { }
                    }
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
    }
}
