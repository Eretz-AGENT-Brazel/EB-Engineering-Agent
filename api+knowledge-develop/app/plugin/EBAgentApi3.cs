// EBAgentApi.cs - EB PROSTEEL AGENT native modeling API (v1).
// Runs INSIDE AutoCAD 2015 + ProStructures V8i SS6 (NETLOAD).
// Creates REAL ProSteel objects (PsShape beams, PsPlate, PsBolt, miter cuts)
// programmatically - NO dialogs. Discovered via reflection dump of
// ProStructuresNet.dll (see api_dump_ProStructuresNet.txt).
//
// Protocol (file-based, avoids command-line quoting + supports Hebrew):
//   1. Python writes  eb_cmd.txt  (key=value lines, op=... first)
//   2. Python sends command  EB_RUN3
//   3. Plugin executes, writes eb_result.txt: "EB_OK {info}" or "EB_ERR {reason}"
// C# 5 compatible (csc v4.0.30319).

using System;
using System.IO;
using System.Text;
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
// PsShapeLoader lives in Steel.Shape (already imported)

[assembly: CommandClass(typeof(EBAgent.ApiCmds3))]

namespace EBAgent
{
    public class ApiCmds3
    {
        const string Dir = @"C:\Users\User\Desktop\EB PROSTEEL AGENT\app\plugin";

        static void Result(string text)
        {
            try { File.WriteAllText(Path.Combine(Dir, "eb_result.txt"), text, Encoding.UTF8); } catch { }
            try
            {
                Document d = Application.DocumentManager.MdiActiveDocument;
                if (d != null) d.Editor.WriteMessage("\n" + text + "\n");
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

        [CommandMethod("EB_RUN3", CommandFlags.Modal)]
        public void Run()
        {
            var kv = ReadCmd();
            string op = Get(kv, "op", "");
            try
            {
                switch (op)
                {
                    case "ping": Result("EB_OK ping " + DateTime.Now.ToString("HH:mm:ss")); break;
                    case "beam": Beam(kv); break;
                    case "plate": Plate(kv); break;
                    case "bolt": Bolt(kv); break;
                    case "miter": Miter(kv); break;
                    case "list": ListModel(); break;
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

        // op=plate  center=x,y,z  l=430 w=220 t=20   (rectangular, in current UCS XY plane)
        void Plate(Dictionary<string, string> kv)
        {
            double[] c = Nums(Get(kv, "center", "0,0,0"));
            double L = double.Parse(Get(kv, "l", "300"), System.Globalization.CultureInfo.InvariantCulture);
            double W = double.Parse(Get(kv, "w", "200"), System.Globalization.CultureInfo.InvariantCulture);
            double T = double.Parse(Get(kv, "t", "20"), System.Globalization.CultureInfo.InvariantCulture);
            string h0, c0; int before = Census(out h0, out c0);

            PsCreatePlate cp = new PsCreatePlate();
            cp.SetToDefaults();
            cp.DeleteAllEdgePoints();
            cp.AppendEdgePoint(new PsPoint(c[0] - L / 2, c[1] - W / 2, c[2]));
            cp.AppendEdgePoint(new PsPoint(c[0] + L / 2, c[1] - W / 2, c[2]));
            cp.AppendEdgePoint(new PsPoint(c[0] + L / 2, c[1] + W / 2, c[2]));
            cp.AppendEdgePoint(new PsPoint(c[0] - L / 2, c[1] + W / 2, c[2]));
            cp.SetThickness(T);
            bool ok = cp.Create();
            string h1, c1; int after = Census(out h1, out c1);
            if (ok && after > before)
                Result("EB_OK plate " + L + "x" + W + "x" + T + " handle=" + h1 + " entities=" + after);
            else
                Result("EB_ERR plate create failed (ok=" + ok + ")");
        }

        // op=bolt  p1=..  p2=..  dia=20  style=M20 (bolt axis p1->p2)
        void Bolt(Dictionary<string, string> kv)
        {
            PsPoint p1 = Pt(Get(kv, "p1", "0,0,0"));
            PsPoint p2 = Pt(Get(kv, "p2", "0,0,50"));
            double dia = double.Parse(Get(kv, "dia", "20"), System.Globalization.CultureInfo.InvariantCulture);
            string style = Get(kv, "style", "M20");
            string h0, c0; int before = Census(out h0, out c0);

            PsCreateBolt cb = new PsCreateBolt();
            cb.SetToDefaults();
            cb.CreateSingleBolt(p1, p2, dia, style, 0.0);
            string h1, c1; int after = Census(out h1, out c1);
            if (after > before)
                Result("EB_OK bolt dia=" + dia + " style=" + style + " handle=" + h1 + " entities=" + after);
            else
                Result("EB_ERR bolt create failed (style '" + style + "' may not exist in bolt DB)");
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

        // op=list  -> handles + classes of all modelspace entities
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
