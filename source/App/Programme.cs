using System;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("Scrabble audio")]
[assembly: System.Reflection.AssemblyProduct("Scrabble audio")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]

namespace ScrabbleAudio
{
    static class Programme
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FenetreJeu());
        }
    }
}
