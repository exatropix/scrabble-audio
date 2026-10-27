using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ScrabbleAudio
{
    /// <summary>
    /// Envoie le texte au lecteur d'écran du joueur, avec sa voix et ses réglages :
    /// NVDA d'abord, puis JAWS, et en dernier recours la voix Windows (SAPI).
    /// </summary>
    static class Parole
    {
        static class Nvda64
        {
            [DllImport("nvdaControllerClient64.dll", EntryPoint = "nvdaController_testIfRunning")] public static extern int Tester();
            [DllImport("nvdaControllerClient64.dll", EntryPoint = "nvdaController_speakText", CharSet = CharSet.Unicode)] public static extern int Dire(string t);
            [DllImport("nvdaControllerClient64.dll", EntryPoint = "nvdaController_cancelSpeech")] public static extern int Couper();
            [DllImport("nvdaControllerClient64.dll", EntryPoint = "nvdaController_brailleMessage", CharSet = CharSet.Unicode)] public static extern int Braille(string t);
        }
        static class Nvda32
        {
            [DllImport("nvdaControllerClient32.dll", EntryPoint = "nvdaController_testIfRunning")] public static extern int Tester();
            [DllImport("nvdaControllerClient32.dll", EntryPoint = "nvdaController_speakText", CharSet = CharSet.Unicode)] public static extern int Dire(string t);
            [DllImport("nvdaControllerClient32.dll", EntryPoint = "nvdaController_cancelSpeech")] public static extern int Couper();
            [DllImport("nvdaControllerClient32.dll", EntryPoint = "nvdaController_brailleMessage", CharSet = CharSet.Unicode)] public static extern int Braille(string t);
        }

        static bool nvdaAbsent;
        static object jaws;
        static object sapi;
        static DateTime dernierTestJaws = DateTime.MinValue;
        static bool jawsActif;

        public static string Derniere { get; private set; }

        public static void Dire(string texte, bool interrompre = true)
        {
            if (string.IsNullOrEmpty(texte)) return;
            Derniere = texte;
            if (Environment.GetEnvironmentVariable("SCRABBLE_JOURNAL") != null) Console.WriteLine("[parole] " + texte);
            if (EssayerNvda(texte, interrompre)) return;
            if (EssayerJaws(texte, interrompre)) return;
            EssayerSapi(texte, interrompre);
        }

        public static void Couper()
        {
            try { if (!nvdaAbsent) { if (Environment.Is64BitProcess) Nvda64.Couper(); else Nvda32.Couper(); } } catch { }
            try { if (sapi != null) sapi.GetType().InvokeMember("Speak", BindingFlags.InvokeMethod, null, sapi, new object[] { "", 2 }); } catch { }
        }

        static bool EssayerNvda(string texte, bool interrompre)
        {
            if (nvdaAbsent) return false;
            try
            {
                if (Environment.Is64BitProcess)
                {
                    if (Nvda64.Tester() != 0) return false;
                    if (interrompre) Nvda64.Couper();
                    Nvda64.Dire(texte);
                    Nvda64.Braille(texte);
                }
                else
                {
                    if (Nvda32.Tester() != 0) return false;
                    if (interrompre) Nvda32.Couper();
                    Nvda32.Dire(texte);
                    Nvda32.Braille(texte);
                }
                return true;
            }
            catch (DllNotFoundException) { nvdaAbsent = true; return false; }
            catch (EntryPointNotFoundException) { nvdaAbsent = true; return false; }
            catch { return false; }
        }

        static bool EssayerJaws(string texte, bool interrompre)
        {
            try
            {
                if ((DateTime.Now - dernierTestJaws).TotalSeconds > 5)
                {
                    jawsActif = Process.GetProcessesByName("jfw").Length > 0;
                    dernierTestJaws = DateTime.Now;
                }
                if (!jawsActif) return false;
                if (jaws == null)
                {
                    var t = Type.GetTypeFromProgID("FreedomSci.JawsApi");
                    if (t == null) return false;
                    jaws = Activator.CreateInstance(t);
                }
                object r = jaws.GetType().InvokeMember("SayString", BindingFlags.InvokeMethod, null, jaws, new object[] { texte, interrompre });
                return r is bool ? (bool)r : true;
            }
            catch { jaws = null; return false; }
        }

        static void EssayerSapi(string texte, bool interrompre)
        {
            try
            {
                if (sapi == null)
                {
                    var t = Type.GetTypeFromProgID("SAPI.SpVoice");
                    if (t == null) return;
                    sapi = Activator.CreateInstance(t);
                }
                // 1 = asynchrone, 2 = interrompre ce qui est en cours
                sapi.GetType().InvokeMember("Speak", BindingFlags.InvokeMethod, null, sapi, new object[] { texte, interrompre ? 3 : 1 });
            }
            catch { sapi = null; }
        }
    }
}
