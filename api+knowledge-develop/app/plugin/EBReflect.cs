// EBReflect.cs - Stage-1 plugin for EB PROSTEEL AGENT.
// Purpose: run INSIDE AutoCAD 2015 (NETLOAD) and dump the public API surface
// of ProStructuresNet.dll / KsKernel.dll via reflection, so the agent can
// write the real modeling API (EBAgentApi) against true signatures.
// Only references acmgd/acdbmgd (stable known API) -> guaranteed to compile.
// Commands: EB_PING, EB_DUMPAPI
// C# 5 compatible (csc v4.0.30319) - no string interpolation.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;

[assembly: CommandClass(typeof(EBAgent.ReflectCmds))]

namespace EBAgent
{
    public class ReflectCmds
    {
        const string OutDir = @"C:\Users\User\Desktop\EB PROSTEEL AGENT\app\plugin";
        const string PrgDir = @"C:\Program Files\Bentley\ProStructures Ss6 R1\AutoCAD 2015\Prg";

        static void Result(string text)
        {
            try { File.WriteAllText(Path.Combine(OutDir, "eb_result.txt"), text, Encoding.UTF8); }
            catch { }
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null) doc.Editor.WriteMessage("\n" + text + "\n");
            }
            catch { }
        }

        [CommandMethod("EB_PING", CommandFlags.Modal)]
        public void Ping()
        {
            Result("EB_OK ping " + DateTime.Now.ToString("HH:mm:ss"));
        }

        [CommandMethod("EB_DUMPAPI", CommandFlags.Modal)]
        public void DumpApi()
        {
            StringBuilder log = new StringBuilder();
            int total = 0;
            string[] targets = new string[] {
                Path.Combine(PrgDir, "ProStructuresNet.dll"),
                Path.Combine(PrgDir, "KsKernel.dll")
            };
            foreach (string dllPath in targets)
            {
                string name = Path.GetFileNameWithoutExtension(dllPath);
                string outFile = Path.Combine(OutDir, "api_dump_" + name + ".txt");
                StringBuilder sb = new StringBuilder();
                try
                {
                    Assembly asm = Assembly.LoadFrom(dllPath);
                    Type[] types;
                    try { types = asm.GetExportedTypes(); }
                    catch (ReflectionTypeLoadException rex)
                    {
                        // keep whatever loaded
                        types = rex.Types;
                    }
                    int n = 0;
                    foreach (Type t in types)
                    {
                        if (t == null) continue;
                        n++;
                        sb.AppendLine("=== TYPE " + t.FullName +
                            (t.BaseType != null ? " : " + t.BaseType.FullName : ""));
                        try
                        {
                            foreach (ConstructorInfo c in t.GetConstructors())
                                sb.AppendLine("  CTOR (" + Params(c.GetParameters()) + ")");
                            foreach (PropertyInfo p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                                sb.AppendLine("  PROP " + p.PropertyType.Name + " " + p.Name +
                                    (p.CanWrite ? " {get;set;}" : " {get;}"));
                            foreach (MethodInfo m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                            {
                                if (m.IsSpecialName) continue;
                                sb.AppendLine("  METH " + m.ReturnType.Name + " " + m.Name +
                                    "(" + Params(m.GetParameters()) + ")" + (m.IsStatic ? " [static]" : ""));
                            }
                        }
                        catch (System.Exception ex)
                        {
                            sb.AppendLine("  (member reflection failed: " + ex.Message + ")");
                        }
                    }
                    File.WriteAllText(outFile, sb.ToString(), Encoding.UTF8);
                    log.Append(name + ":" + n + " types; ");
                    total += n;
                }
                catch (System.Exception ex)
                {
                    File.WriteAllText(outFile, "LOAD FAILED: " + ex.ToString(), Encoding.UTF8);
                    log.Append(name + ":FAILED " + ex.Message + "; ");
                }
            }
            Result("EB_OK dumpapi " + total + " types (" + log.ToString().Trim() + ")");
        }

        static string Params(ParameterInfo[] ps)
        {
            StringBuilder b = new StringBuilder();
            for (int i = 0; i < ps.Length; i++)
            {
                if (i > 0) b.Append(", ");
                b.Append(ps[i].ParameterType.Name).Append(" ").Append(ps[i].Name);
            }
            return b.ToString();
        }
    }
}
