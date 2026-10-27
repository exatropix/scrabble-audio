using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Threading;

namespace ScrabbleAudio
{
    /// <summary>
    /// Joue les fichiers WAV du dossier "sons". Chaque son peut être remplacé
    /// par un fichier du même nom (distribution.wav, pose.wav, erreur.wav, ordinateur.wav, fin.wav, bord.wav, selection.wav).
    /// </summary>
    static class Sons
    {
        static readonly Dictionary<string, SoundPlayer> cache = new Dictionary<string, SoundPlayer>();
        static string dossier;
        static int sequence;

        public static void Initialiser(string dossierSons)
        {
            dossier = dossierSons;
            foreach (var n in new[] { "distribution", "pose", "erreur", "ordinateur", "fin", "bord", "selection" }) Lecteur(n);
        }

        static SoundPlayer Lecteur(string nom)
        {
            SoundPlayer sp;
            if (cache.TryGetValue(nom, out sp)) return sp;
            string f = Path.Combine(dossier ?? "", nom + ".wav");
            sp = null;
            if (File.Exists(f))
            {
                try { sp = new SoundPlayer(f); sp.Load(); } catch { sp = null; }
            }
            cache[nom] = sp;
            return sp;
        }

        public static void Jouer(string nom)
        {
            try { var sp = Lecteur(nom); if (sp != null) sp.Play(); } catch { }
        }

        /// <summary>Joue un son plusieurs fois de suite (une fois par lettre distribuée).</summary>
        public static void Repeter(string nom, int fois, int intervalleMs)
        {
            if (fois <= 0) return;
            int ma = Interlocked.Increment(ref sequence);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                for (int i = 0; i < fois && ma == sequence; i++)
                {
                    try { var sp = Lecteur(nom); if (sp != null) sp.Play(); } catch { }
                    Thread.Sleep(intervalleMs);
                }
            });
        }
    }
}
