using System;
using System.IO;
using System.Text;

namespace ScrabbleAudio
{
    /// <summary>
    /// Journal de démarrage : chaque étape est écrite aussitôt dans demarrage.txt, dans le dossier du jeu
    /// (ou, si c'est impossible, dans AppData\Roaming\ScrabbleAudio). Si le jeu se ferme sans rien dire,
    /// la dernière ligne indique où il s'est arrêté. Toutes les erreurs, même rattrapées, y sont notées.
    /// </summary>
    static class Journal
    {
        static string fichier;
        static int erreurs;
        static readonly object verrou = new object();

        public static string Fichier { get { return fichier; } }

        public static void Demarrer()
        {
            foreach (var dossier in new[] { AppDomain.CurrentDomain.BaseDirectory,
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScrabbleAudio") })
            {
                try
                {
                    Directory.CreateDirectory(dossier);
                    string f = Path.Combine(dossier, "demarrage.txt");
                    File.WriteAllText(f, "Scrabble audio " + Core.MiseAJour.VersionActuelle + ", démarrage le " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n"
                        + "Windows " + Environment.OSVersion.VersionString + (Environment.Is64BitProcess ? ", programme 64 bits" : ", programme 32 bits")
                        + ", .NET " + Environment.Version + ", langue " + System.Globalization.CultureInfo.CurrentCulture.Name + "\r\n"
                        + "Dossier du jeu : " + AppDomain.CurrentDomain.BaseDirectory + "\r\n", Encoding.UTF8);
                    fichier = f;
                    break;
                }
                catch { }
            }
            try
            {
                AppDomain.CurrentDomain.FirstChanceException += (s, e) =>
                {
                    if (erreurs++ < 40) Etape("  ! " + e.Exception.GetType().Name + " : " + e.Exception.Message);
                };
            }
            catch { }
        }

        public static void Etape(string texte)
        {
            if (fichier == null) return;
            lock (verrou)
            {
                try { File.AppendAllText(fichier, DateTime.Now.ToString("HH:mm:ss.fff") + "  " + texte + "\r\n", Encoding.UTF8); }
                catch { }
            }
        }
    }
}
