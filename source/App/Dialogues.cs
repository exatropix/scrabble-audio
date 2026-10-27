using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ScrabbleAudio.Core;

namespace ScrabbleAudio
{
    /// <summary>
    /// Petites fenêtres standard de Windows : NVDA et JAWS les lisent directement
    /// (titre, étiquette, liste, cases à cocher, boutons).
    /// </summary>
    static class Dialogues
    {
        /// <summary>Rend la main à la fenêtre du jeu après une boîte de dialogue.</summary>
        public static void Revenir(IWin32Window parent)
        {
            var f = parent as Form;
            if (f != null && !f.IsDisposed) f.Activate();
        }

        static Form Fenetre(string titre, int largeur, int hauteur)
        {
            var form = new Form
            {
                Text = titre,
                ClientSize = new Size(largeur, hauteur),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                KeyPreview = true,
                Font = new Font("Segoe UI", 11f)
            };
            form.FormClosed += (s, e) => Revenir(form.Owner);
            return form;
        }

        static void Boutons(Form f, int y, string ok, string annuler)
        {
            var bOk = new Button { Text = ok, DialogResult = DialogResult.OK, Location = new Point(f.ClientSize.Width - 250, y), Size = new Size(110, 34) };
            f.Controls.Add(bOk);
            f.AcceptButton = bOk;
            if (annuler != null)
            {
                var bAn = new Button { Text = annuler, DialogResult = DialogResult.Cancel, Location = new Point(f.ClientSize.Width - 130, y), Size = new Size(110, 34) };
                f.Controls.Add(bAn);
                f.CancelButton = bAn;
            }
        }

        /// <summary>Choix du niveau de l'ordinateur. Renvoie null si le joueur annule.</summary>
        public static Niveau? ChoisirNiveau(IWin32Window parent, Niveau defaut)
        {
            using (var f = Fenetre(L.T("Niveau de jeu"), 420, 230))
            {
                var lab = new Label { Text = L.T("&Niveau de l'ordinateur :"), Location = new Point(16, 14), AutoSize = true };
                var liste = new ListBox { Location = new Point(16, 44), Size = new Size(388, 100) };
                liste.Items.AddRange(new object[] { L.T("Débutant"), L.T("Moyen"), L.T("Expert") });
                liste.SelectedIndex = (int)defaut;
                liste.AccessibleName = L.T("Niveau de l'ordinateur");
                liste.DoubleClick += (s, e) => f.DialogResult = DialogResult.OK;
                f.Controls.Add(lab); f.Controls.Add(liste);
                Boutons(f, 180, "OK", L.T("Annuler"));
                f.Shown += (s, e) => liste.Focus();
                if (f.ShowDialog(parent) != DialogResult.OK) return null;
                return (Niveau)liste.SelectedIndex;
            }
        }

        /// <summary>Demande la lettre jouée par un joker. Renvoie '\0' si le joueur annule.</summary>
        public static char LettreJoker(IWin32Window parent, int numero)
        {
            while (true)
            {
                using (var f = Fenetre(L.T("Joker"), 420, 150))
                {
                    var lab = new Label { Text = (L.Pl(numero) ? L.T("Second joker : ") : "") + L.T("&quelle lettre doit remplacer le joker ?"), Location = new Point(16, 14), AutoSize = true };
                    var zone = new TextBox { Location = new Point(16, 50), Size = new Size(80, 30), MaxLength = 2, CharacterCasing = CharacterCasing.Upper };
                    zone.AccessibleName = L.T("Lettre du joker");
                    f.Controls.Add(lab); f.Controls.Add(zone);
                    Boutons(f, 100, "OK", L.T("Annuler"));
                    f.Shown += (s, e) => zone.Focus();
                    if (f.ShowDialog(parent) != DialogResult.OK) return '\0';
                    char c = Lettres.Normaliser(zone.Text);
                    if (c != '\0') return c;
                    MessageBox.Show(parent, Lettres.Langue == "es" ? L.T("Tapez une seule lettre, de A à Z, ou Ñ, CH, LL, RR.") : L.T("Tapez une seule lettre, de A à Z."), L.T("Joker"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        /// <summary>Choix des lettres à échanger. Renvoie null si le joueur annule.</summary>
        public static List<char> LettresAEchanger(IWin32Window parent, List<char> chevalet)
        {
            using (var f = Fenetre(L.T("Échanger des lettres"), 420, 330))
            {
                var lab = new Label { Text = L.T("Cochez avec Espace les &lettres à remettre dans le sac :"), Location = new Point(16, 14), Size = new Size(388, 44) };
                var liste = new CheckedListBox { Location = new Point(16, 62), Size = new Size(388, 200), CheckOnClick = true };
                liste.AccessibleName = L.T("Lettres à échanger");
                for (int i = 0; i < chevalet.Count; i++) liste.Items.Add((i + 1) + " : " + Lettres.Decrire(chevalet[i]));
                if (liste.Items.Count > 0) liste.SelectedIndex = 0;
                f.Controls.Add(lab); f.Controls.Add(liste);
                Boutons(f, 280, L.T("Échanger"), L.T("Annuler"));
                f.Shown += (s, e) => liste.Focus();
                if (f.ShowDialog(parent) != DialogResult.OK) return null;
                return liste.CheckedIndices.Cast<int>().Select(i => chevalet[i]).ToList();
            }
        }

        /// <summary>Texte long en lecture seule (aide), parcourable avec les flèches.</summary>
        public static void Texte(IWin32Window parent, string titre, string texte)
        {
            using (var f = Fenetre(titre, 720, 520))
            {
                f.FormBorderStyle = FormBorderStyle.Sizable;
                var zone = new TextBox
                {
                    Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Text = texte.Replace("\n", "\r\n"),
                    Location = new Point(12, 12), Size = new Size(696, 450), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                    BackColor = SystemColors.Window
                };
                zone.AccessibleName = titre;
                var fermer = new Button { Text = L.T("Fermer"), DialogResult = DialogResult.Cancel, Location = new Point(598, 474), Size = new Size(110, 34), Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
                f.Controls.Add(zone); f.Controls.Add(fermer);
                f.CancelButton = fermer;
                f.Shown += (s, e) => { zone.Focus(); zone.SelectionStart = 0; zone.SelectionLength = 0; };
                f.ShowDialog(parent);
            }
        }
    }
}
