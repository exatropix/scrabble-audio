using System;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("Scrabble audio")]
[assembly: System.Reflection.AssemblyProduct("Scrabble audio")]
[assembly: System.Reflection.AssemblyVersion("2.3.0.0")]

namespace ScrabbleAudio
{
    static class Programme
    {
        [STAThread]
        static void Main()
        {
            // Toute erreur imprévue est enregistrée dans erreur.txt et expliquée, au lieu de la fenêtre d'erreur de Windows.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => Signaler(e.Exception, true);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Signaler(e.ExceptionObject as Exception, false);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FenetreJeu());
        }

        static bool dejaSignale;

        static void Signaler(Exception ex, bool peutContinuer)
        {
            if (ex == null) return;
            string dossier = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScrabbleAudio");
            string texte = "Erreur imprévue :\n" + Diagnostic.Decrire(ex, null);
            string journal = Diagnostic.Enregistrer(dossier, texte);
            if (dejaSignale && peutContinuer) return; // une seule fenêtre par session ; les suivantes vont dans le fichier
            dejaSignale = true;
            try
            {
                MessageBox.Show(texte + (journal != null ? "\n\nCe texte est enregistré dans le fichier :\n" + journal : "")
                    + (peutContinuer ? "\n\nLa partie a été sauvegardée après le dernier coup ; vous pouvez continuer à jouer." : ""),
                    "Scrabble audio", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
        }
    }
}
