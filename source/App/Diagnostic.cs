using System;
using System.IO;
using System.Text;

namespace ScrabbleAudio
{
    /// <summary>Décrit une erreur en détail (cause réelle, système, fichier) pour pouvoir la corriger.</summary>
    static class Diagnostic
    {
        public static string Decrire(Exception ex, string fichier)
        {
            var sb = new StringBuilder();
            int niveau = 0;
            for (var e = ex; e != null && niveau < 6; e = e.InnerException, niveau++)
            {
                sb.Append(niveau == 0 ? "" : "Cause : ").Append(e.GetType().Name).Append(" : ").Append(e.Message).Append("\n");
                if (e.InnerException == null && e.StackTrace != null)
                {
                    var lignes = e.StackTrace.Split('\n');
                    for (int i = 0; i < lignes.Length && i < 12; i++) sb.Append("  ").Append(lignes[i].Trim()).Append("\n");
                }
            }
            sb.Append("\nVersion du jeu : ").Append(Core.MiseAJour.VersionActuelle);
            sb.Append("\nWindows : ").Append(Environment.OSVersion.VersionString).Append(Environment.Is64BitOperatingSystem ? ", 64 bits" : ", 32 bits");
            sb.Append("\n.NET : ").Append(Environment.Version);
            sb.Append("\nLangue de Windows : ").Append(System.Globalization.CultureInfo.CurrentCulture.Name)
              .Append(" / ").Append(System.Globalization.CultureInfo.CurrentUICulture.Name);
            sb.Append("\nDossier du jeu : ").Append(AppDomain.CurrentDomain.BaseDirectory);
            if (fichier != null) try
            {
                sb.Append("\nDictionnaire : ").Append(fichier ?? "?");
                if (fichier != null) sb.Append(File.Exists(fichier) ? " (présent, " + new FileInfo(fichier).Length + " octets)" : " (absent)");
            }
            catch { }
            return sb.ToString();
        }

        /// <summary>Écrit le texte dans erreur.txt ; renvoie le chemin, ou null en cas d'échec.</summary>
        public static string Enregistrer(string dossier, string texte)
        {
            try
            {
                Directory.CreateDirectory(dossier);
                string f = Path.Combine(dossier, "erreur.txt");
                if (File.Exists(f) && new FileInfo(f).Length > 200000) File.Delete(f);
                File.AppendAllText(f, "==== " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n" + texte.Replace("\n", "\r\n") + "\r\n\r\n", Encoding.UTF8);
                return f;
            }
            catch { return null; }
        }
    }
}
