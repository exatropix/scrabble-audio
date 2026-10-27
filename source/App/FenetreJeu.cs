using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ScrabbleAudio.Core;

namespace ScrabbleAudio
{
    /// <summary>Zone qui reçoit le clavier. Elle dessine aussi le jeu pour une personne voyante à côté.</summary>
    class ZoneJeu : Control
    {
        public Action<Graphics> Dessin;
        public ZoneJeu()
        {
            SetStyle(ControlStyles.Selectable | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            TabStop = true;
            AccessibleName = "Scrabble audio";
            AccessibleDescription = L.T("Ctrl+T chevalet, Ctrl+P plateau, F1 aide");
            AccessibleRole = AccessibleRole.Client;
        }
        protected override bool IsInputKey(Keys k) { return true; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Dessin != null) Dessin(e.Graphics);
        }
    }

    enum Zone { Chevalet, Plateau }

    class FenetreJeu : Form
    {
        readonly ZoneJeu zone = new ZoneJeu();
        readonly MenuStrip menu = new MenuStrip();
        readonly Random rnd = new Random();
        readonly string dossierJeu;
        readonly string cheminSauvegarde;
        readonly string dossierDonnees;
        string langueJeu = "fr";                       // langue choisie pour les prochaines parties
        static readonly string[] Langues = { "fr", "en", "es" };
        bool majAuto = true;
        string majDate;

        Lexique lex;
        Partie partie;
        Zone zoneCourante = Zone.Chevalet;

        // Chevalet
        int curseur;                                   // 0..n, n = L.T("fin du chevalet")
        readonly HashSet<int> selection = new HashSet<int>();
        readonly List<char> main = new List<char>();   // lettres coupées en attente de collage
        List<char> pressePapiers = new List<char>();   // mot copié pour le plateau

        // Plateau
        int ligne = Plateau.Centre, colonne = Plateau.Centre;
        ResultatCoup enAttente;

        bool occupe;      // l'ordinateur joue ou les lettres sont distribuées
        bool menuActif;
        bool fermetureAutorisee;
        int indexMemoire = -1;
        DateTime dernierMemoire = DateTime.MinValue;

        public FenetreJeu()
        {
            dossierJeu = AppDomain.CurrentDomain.BaseDirectory;
            dossierDonnees = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScrabbleAudio");
            cheminSauvegarde = Path.Combine(dossierDonnees, "partie.sav");
            L.Initialiser(LireOption("langue"));
            langueJeu = LireOption("jeu") ?? L.Code;
            majAuto = LireOption("maj") != "0";
            majDate = LireOption("majdate");

            Text = "Scrabble audio";
            ClientSize = new Size(900, 680);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10f);
            BackColor = Color.FromArgb(32, 64, 48);

            zone.Dock = DockStyle.Fill;
            zone.Dessin = Dessiner;
            Controls.Add(zone);
            ConstruireMenu();
            Controls.Add(menu);
            MainMenuStrip = menu;
            ActiveControl = zone;

            Sons.Initialiser(Path.Combine(dossierJeu, "sons"));
            Activated += (s, e) => { if (!menuActif) BeginInvoke(new Action(() => zone.Focus())); };
        }

        // ================= Démarrage =================

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            zone.Focus();
            Parole.Dire(L.T("Scrabble audio. Chargement du dictionnaire."));
            if (!DicoInstalle(langueJeu)) langueJeu = Langues.FirstOrDefault(DicoInstalle);
            if (langueJeu == null)
            {
                MessageBox.Show(this, L.T("Aucun dictionnaire n'est installé. Téléchargez-en un sur la page du jeu, puis décompressez-le dans le dossier du jeu."), "Scrabble audio", MessageBoxButtons.OK, MessageBoxIcon.Error);
                fermetureAutorisee = true; Close(); return;
            }
            // Une partie sauvegardée se reprend dans sa langue
            string langueSauvee = LangueSauvegarde();
            if (!ChargerLexique(langueSauvee != null && DicoInstalle(langueSauvee) ? langueSauvee : langueJeu)) { fermetureAutorisee = true; Close(); return; }

            if (File.Exists(cheminSauvegarde))
            {
                var r = MessageBox.Show(this, L.T("Une partie sauvegardée existe. Voulez-vous la reprendre ?"), L.T("Reprendre la partie"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r == DialogResult.Yes)
                {
                    try
                    {
                        partie = Partie.Charger(cheminSauvegarde, rnd);
                        if (partie.Langue != Lettres.Langue) throw new InvalidDataException();
                        ReinitialiserEtat();
                        string msg = L.T("Partie reprise, niveau ") + NomNiveau(partie.Niveau) + ". " + partie.DecrireScores();
                        if (partie.Courant == 0) PlusTard(400, () => Parole.Dire(msg + L.T(" À vous de jouer. Vous êtes sur votre chevalet. ") + DecrireCurseur()));
                        else { occupe = true; PlusTard(400, () => Parole.Dire(msg + L.T(" L'ordinateur va jouer."))); PlusTard(1500, TourOrdinateur); }
                        zone.Invalidate();
                        PlusTard(15000, MajAuDemarrage);
                        return;
                    }
                    catch
                    {
                        MessageBox.Show(this, L.T("La sauvegarde est illisible. Une nouvelle partie va commencer."), "Scrabble audio", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            NouvellePartie(true);
        }

        void NouvellePartie(bool auDemarrage)
        {
            if (auDemarrage) PlusTard(15000, MajAuDemarrage);
            if (Lettres.Langue != langueJeu || lex == null)
            {
                if (!DicoInstalle(langueJeu)) { Parole.Dire(L.F("Le dictionnaire {0} n'est pas installé. Téléchargez-le sur la page du jeu, puis décompressez-le dans le dossier du jeu.", NomLangue(langueJeu))); return; }
                Parole.Dire(L.T("Scrabble audio. Chargement du dictionnaire."));
                if (!ChargerLexique(langueJeu)) return;
            }
            var niveau = Dialogues.ChoisirNiveau(this, partie != null ? partie.Niveau : Niveau.Moyen);
            if (niveau == null)
            {
                if (auDemarrage) { fermetureAutorisee = true; Close(); }
                else PlusTard(300, () => Parole.Dire(L.T("Nouvelle partie annulée.")));
                return;
            }
            partie = Partie.Nouvelle(niveau.Value, rnd);
            SupprimerSauvegarde();
            ReinitialiserEtat();
            occupe = true;
            zone.Invalidate();

            string tirage = (Lettres.Langue != L.Code ? L.F("Partie en {0}.", NomLangue(Lettres.Langue)) + " " : "") + L.T("Niveau ") + NomNiveau(niveau.Value) + L.T(". Tirage au sort : ") + (partie.Courant == 0 ? L.T("vous commencez.") : L.T("l'ordinateur commence."));
            PlusTard(300, () => Parole.Dire(tirage + L.T(" Distribution des lettres.")));
            PlusTard(2600, () => Sons.Repeter("distribution", Partie.TailleChevalet, 170));
            PlusTard(2600 + Partie.TailleChevalet * 170 + 300, () =>
            {
                if (partie.Courant == 0)
                {
                    occupe = false;
                    Parole.Dire(L.T("Vous avez reçu 7 lettres. À vous de jouer. Vous êtes sur votre chevalet. ") + DecrireCurseur());
                }
                else
                {
                    Parole.Dire(L.T("Vous avez reçu 7 lettres."));
                    TourOrdinateur();
                }
            });
        }

        // ================= Recherche de mise à jour =================

        void RechercherMaj(bool manuel)
        {
            if (manuel) Parole.Dire(L.T("Recherche d'une mise à jour…"));
            MiseAJour.Rechercher(r =>
            {
                try { BeginInvoke(new Action(() => ResultatMaj(r, manuel))); } catch { }
            });
        }

        void ResultatMaj(MiseAJour.Resultat r, bool manuel)
        {
            if (r.Reussi) { majDate = DateTime.Today.ToString("yyyy-MM-dd"); EcrireOptions(); }
            if (r.Reussi && r.NouvelleVersion)
            {
                string msg = L.F("Une nouvelle version est disponible : {0}. Vous avez la version {1}.", r.Version, MiseAJour.VersionActuelle);
                if (!manuel)
                {
                    // Au démarrage, on prévient sans interrompre la partie par une fenêtre.
                    Parole.Dire(msg + " " + L.T("Pour la télécharger : menu Aide, Rechercher une mise à jour."), false);
                    return;
                }
                var rep = MessageBox.Show(this, msg + "\n\n" + L.T("Ouvrir la page de téléchargement ?"), L.T("Mise à jour"), MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (rep == DialogResult.Yes) MiseAJour.OuvrirPage(r.Page);
                return;
            }
            if (!manuel) return;
            if (r.Reussi) Parole.Dire(L.F("Vous avez la dernière version, la {0}.", MiseAJour.VersionActuelle));
            else Parole.Dire(L.T("Impossible de vérifier les mises à jour. Vérifiez votre connexion à Internet."));
        }

        void BasculerMajAuto()
        {
            majAuto = !majAuto;
            EcrireOptions();
            ReconstruireMenu();
            Parole.Dire(majAuto ? L.T("Recherche des mises à jour au démarrage activée.") : L.T("Recherche des mises à jour au démarrage désactivée."));
        }

        void MajAuDemarrage()
        {
            if (majAuto && majDate != DateTime.Today.ToString("yyyy-MM-dd")) RechercherMaj(false);
        }

        void ReinitialiserEtat()
        {
            zoneCourante = Zone.Chevalet;
            curseur = 0;
            EffacerSelection(); main.Clear(); pressePapiers.Clear();
            ligne = colonne = Plateau.Centre;
            enAttente = null; occupe = false; indexMemoire = -1;
        }

        static string NomNiveau(Niveau n)
        {
            return n == Niveau.Debutant ? L.T("débutant") : n == Niveau.Moyen ? L.T("moyen") : L.T("expert");
        }

        List<char> Chevalet { get { return partie.Humain.Chevalet; } }

        // ================= Clavier =================

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (menuActif) return base.ProcessCmdKey(ref msg, keyData);
            if (!zone.Focused) zone.Focus();
            if (Traiter(keyData)) return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }

        bool Traiter(Keys k)
        {
            Keys code = k & Keys.KeyCode;
            bool ctrl = (k & Keys.Control) != 0, shift = (k & Keys.Shift) != 0, alt = (k & Keys.Alt) != 0;
            if (alt) return false;

            // --- Commandes disponibles à tout moment
            if (code == Keys.F1 && !ctrl) { AfficherAide(); return true; }
            if (ctrl && code == Keys.Q) { Quitter(); return true; }
            if (lex == null) return false;
            if (ctrl && code == Keys.N) { DemanderNouvellePartie(); return true; }
            if (partie == null) return false;
            if (ctrl && code == Keys.R) { Parole.Dire(partie.DecrireScores() + " " + QuiJoue()); return true; }
            if (ctrl && code == Keys.S) { AnnoncerSac(); return true; }
            if (ctrl && code == Keys.M) { Memoire(); return true; }

            // --- Navigation : toujours possible, même pendant le tour de l'ordinateur
            if (ctrl && code == Keys.T) { AllerChevalet(); return true; }
            if (ctrl && code == Keys.P) { AllerPlateau(); return true; }

            if (enAttente != null)
            {
                if (code == Keys.Enter) { ValiderPose(); return true; }
                if (code == Keys.Escape) { enAttente = null; Parole.Dire(L.T("Annulé")); zone.Invalidate(); return true; }
            }

            bool navigation = !ctrl && (code == Keys.Left || code == Keys.Right || code == Keys.Up || code == Keys.Down || code == Keys.Home || code == Keys.End)
                              || ctrl && zoneCourante == Zone.Plateau && (code == Keys.Left || code == Keys.Right || code == Keys.Up || code == Keys.Down);
            bool lecture = zoneCourante == Zone.Plateau && !ctrl && (code == Keys.L || code == Keys.C || code == Keys.Space);

            if (!navigation && !lecture && EstToucheAction(code, ctrl))
            {
                if (partie.Terminee) { Parole.Dire(L.T("La partie est terminée. Ctrl+N pour une nouvelle partie.")); return true; }
                if (occupe) { Parole.Dire(L.T("Patientez, l'ordinateur joue.")); return true; }
            }

            if (ctrl && code == Keys.E) { Echanger(); return true; }
            if (ctrl && code == Keys.J) { Passer(); return true; }

            return zoneCourante == Zone.Chevalet ? ToucheChevalet(code, ctrl, shift) : TouchePlateau(code, ctrl, shift);
        }

        static bool EstToucheAction(Keys code, bool ctrl)
        {
            if (ctrl) return code == Keys.E || code == Keys.J || code == Keys.X || code == Keys.C || code == Keys.V;
            return code == Keys.Space || code == Keys.Escape || (code >= Keys.A && code <= Keys.Z) || code == Keys.Oemcomma || code == Keys.OemQuestion;
        }

        string QuiJoue()
        {
            if (partie.Terminee) return L.T("La partie est terminée.");
            return occupe ? L.T("L'ordinateur joue.") : L.T("À vous de jouer.");
        }

        // ================= Chevalet =================
        // Le chevalet se comporte comme une zone d'édition : le curseur est posé sur une lettre,
        // Maj+flèches sélectionne, Ctrl+X coupe, Ctrl+V colle. Chaque lettre est annoncée « A, 1 ».

        int ancre = -1; // début de la sélection (Maj+flèches), -1 = aucune

        void AllerChevalet()
        {
            if (enAttente != null) enAttente = null;
            zoneCourante = Zone.Chevalet;
            if (curseur > Chevalet.Count) curseur = Chevalet.Count;
            Parole.Dire(L.T("Chevalet. ") + DecrireCurseur());
            zone.Invalidate();
        }

        string DecrireLettre(int i)
        {
            if (i < 0 || i >= Chevalet.Count) return L.T("Fin");
            char c = Chevalet[i];
            return c == Lettres.Joker ? Lettres.Nom(c) : Lettres.Nom(c) + ", " + Lettres.Valeur(c);
        }

        string DecrireCurseur() { return DecrireLettre(curseur); }

        static string Lettres_(IEnumerable<char> l)
        {
            return string.Join(", ", l.Select(Lettres.Nom));
        }

        void MajSelection()
        {
            selection.Clear();
            if (ancre < 0) return;
            for (int i = Math.Min(ancre, curseur); i < Math.Max(ancre, curseur); i++)
                if (i < Chevalet.Count) selection.Add(i);
        }

        void EffacerSelection() { ancre = -1; selection.Clear(); }

        bool ToucheChevalet(Keys code, bool ctrl, bool shift)
        {
            int n = Chevalet.Count;
            if (!ctrl && (code == Keys.Left || code == Keys.Right || code == Keys.Home || code == Keys.End))
            {
                int avant = curseur;
                int cible = curseur;
                if (code == Keys.Left) cible--;
                if (code == Keys.Right) cible++;
                if (code == Keys.Home) cible = 0;
                if (code == Keys.End) cible = n;
                if (cible < 0 || cible > n) { Sons.Jouer("bord"); Parole.Dire(DecrireCurseur()); return true; }
                if (shift)
                {
                    if (ancre < 0) ancre = avant;
                    curseur = cible;
                    MajSelection();
                    // comme dans une zone d'édition : on annonce la lettre qui vient d'être (dé)sélectionnée
                    int lettre = cible > avant ? avant : cible;
                    if (code == Keys.Home || code == Keys.End)
                        Parole.Dire(selection.Count > 0 ? Lettres_(selection.OrderBy(i => i).Select(i => Chevalet[i])) + L.T(" sélectionné") : L.T("Rien de sélectionné"));
                    else
                        Parole.Dire(DecrireLettre(lettre) + (selection.Contains(lettre) ? L.T(", sélectionné") : L.T(", désélectionné")));
                }
                else
                {
                    EffacerSelection();
                    curseur = cible;
                    Parole.Dire(DecrireCurseur());
                }
                zone.Invalidate();
                return true;
            }
            if (!ctrl && (code == Keys.Up || code == Keys.Down)) { Parole.Dire(DecrireCurseur()); return true; }
            if (!ctrl && code == Keys.Escape)
            {
                if (selection.Count > 0) { EffacerSelection(); Parole.Dire(L.T("Sélection effacée")); zone.Invalidate(); }
                return true;
            }
            if (ctrl && code == Keys.A)
            {
                ancre = 0; curseur = n; MajSelection();
                Parole.Dire(L.T("Tout sélectionné"));
                zone.Invalidate();
                return true;
            }
            if (ctrl && code == Keys.X) { Couper(); return true; }
            if (ctrl && code == Keys.V) { CollerChevalet(); return true; }
            if (ctrl && code == Keys.C) { Copier(); return true; }
            return false;
        }

        List<int> IndicesChoisis()
        {
            if (selection.Count > 0) return selection.Where(i => i < Chevalet.Count).OrderBy(i => i).ToList();
            if (curseur < Chevalet.Count) return new List<int> { curseur };
            return new List<int>();
        }

        void Couper()
        {
            var ids = IndicesChoisis();
            if (ids.Count == 0) { Parole.Dire(L.T("Rien à couper")); return; }
            RendreMain();
            var lettres = ids.Select(i => Chevalet[i]).ToList();
            foreach (int i in ids.OrderByDescending(i => i)) Chevalet.RemoveAt(i);
            main.AddRange(lettres);
            curseur = Math.Min(ids[0], Chevalet.Count);
            EffacerSelection();
            Parole.Dire(Lettres_(lettres) + L.T(" coupé"));
            zone.Invalidate();
        }

        void CollerChevalet()
        {
            if (main.Count == 0) { Parole.Dire(L.T("Rien à coller")); return; }
            curseur = Math.Min(curseur, Chevalet.Count);
            Chevalet.InsertRange(curseur, main);
            string s = Lettres_(main) + L.T(" collé");
            main.Clear();
            EffacerSelection();
            Parole.Dire(s);
            zone.Invalidate();
        }

        void Copier()
        {
            RendreMain();
            var ids = IndicesChoisis();
            if (ids.Count == 0) { Parole.Dire(L.T("Rien à copier")); return; }
            pressePapiers = ids.Select(i => Chevalet[i]).ToList();
            Parole.Dire(Lettres_(pressePapiers) + L.T(" copié"));
        }

        /// <summary>Remet les lettres coupées et non collées en fin de chevalet (elles deviennent le mot à poser).</summary>
        void RendreMain()
        {
            if (main.Count == 0) return;
            Chevalet.AddRange(main);
            pressePapiers = new List<char>(main);
            main.Clear();
        }

        // ================= Plateau =================

        void AllerPlateau()
        {
            RendreMain();
            zoneCourante = Zone.Plateau;
            Parole.Dire(L.T("Plateau. ") + DecrireCase(ligne, colonne));
            zone.Invalidate();
        }

        bool TouchePlateau(Keys code, bool ctrl, bool shift)
        {
            int dl = 0, dc = 0;
            if (code == Keys.Up) dl = -1;
            else if (code == Keys.Down) dl = 1;
            else if (code == Keys.Left) dc = -1;
            else if (code == Keys.Right) dc = 1;

            if (dl != 0 || dc != 0)
            {
                if (ctrl)
                {
                    int l = ligne + dl, c = colonne + dc;
                    while (Plateau.DansPlateau(l, c) && partie.Plateau.EstVide(l, c)) { l += dl; c += dc; }
                    if (!Plateau.DansPlateau(l, c)) { Sons.Jouer("bord"); Parole.Dire(L.T("Aucune lettre plus loin. ") + DecrireCase(ligne, colonne), enAttente == null); return true; }
                    ligne = l; colonne = c;
                }
                else
                {
                    if (!Plateau.DansPlateau(ligne + dl, colonne + dc)) { Sons.Jouer("bord"); Parole.Dire(L.T("Bord du plateau. ") + DecrireCase(ligne, colonne)); return true; }
                    ligne += dl; colonne += dc;
                }
                Parole.Dire(DecrireCase(ligne, colonne), false);
                zone.Invalidate();
                return true;
            }
            if (!ctrl && code == Keys.Home) { colonne = 0; Parole.Dire(DecrireCase(ligne, colonne)); zone.Invalidate(); return true; }
            if (!ctrl && code == Keys.End) { colonne = Plateau.Taille - 1; Parole.Dire(DecrireCase(ligne, colonne)); zone.Invalidate(); return true; }
            if (!ctrl && code == Keys.Space) { Parole.Dire(enAttente != null ? EpelerAttente() : DecrireCase(ligne, colonne)); return true; }
            if (!ctrl && code == Keys.L) { Parole.Dire(partie.Plateau.DecrireLigne(ligne, true)); return true; }
            if (!ctrl && code == Keys.C) { Parole.Dire(partie.Plateau.DecrireLigne(colonne, false)); return true; }
            if (ctrl && code == Keys.V) { CollerPlateau(); return true; }
            if (ctrl && (code == Keys.X || code == Keys.C)) { Parole.Dire(L.T("Pour couper ou copier des lettres, allez sur votre chevalet avec Ctrl+T.")); return true; }
            return false;
        }

        /// <summary>Case du plateau, en tenant compte des lettres posées en attente de validation.</summary>
        string DecrireCase(int l, int c)
        {
            if (enAttente != null)
                foreach (var x in enAttente.Poses)
                    if (x.Ligne == l && x.Colonne == c)
                        return Plateau.Coord(l, c) + ", " + (x.Joker ? Lettres.Nom(x.Lettre) + L.T(", joker") : Lettres.Nom(x.Lettre)) + L.T(", posée");
            return partie.Plateau.DecrireCase(l, c);
        }

        /// <summary>Épelle le mot en attente : « adore : A, D, O, R, E. 7 points. »</summary>
        string EpelerAttente()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var m in enAttente.Mots)
                sb.Append(Lettres.Lisible(m.Mot).ToLowerInvariant()).Append(" : ").Append(string.Join(", ", m.Mot.Select(c => Lettres.Nom(c)))).Append(". ");
            return sb.ToString() + Lettres.Points(enAttente.Points) + L.T(". Entrée pour valider, Échap pour annuler.");
        }

        void CollerPlateau()
        {
            RendreMain();
            // Le mot à poser : les lettres copiées avec Ctrl+C (elles restent sur le chevalet), sinon la sélection
            if (pressePapiers.Count == 0 && selection.Count > 0) pressePapiers = selection.Where(i => i < Chevalet.Count).OrderBy(i => i).Select(i => Chevalet[i]).ToList();
            if (pressePapiers.Count == 0) { Parole.Dire(L.T("Aucun mot copié. Sur le chevalet, sélectionnez votre mot puis Ctrl+C")); return; }
            if (!Partie.Contient(Chevalet, pressePapiers)) { Parole.Dire(L.T("Ces lettres ne sont plus sur votre chevalet")); return; }

            var choix = new ContextMenuStrip();
            bool choisi = false;
            var h = new ToolStripMenuItem(L.T("&Horizontal"));
            var v = new ToolStripMenuItem(L.T("&Vertical"));
            h.Click += (s, e) => { choisi = true; PlusTard(150, () => PoserMot(true)); };
            v.Click += (s, e) => { choisi = true; PlusTard(150, () => PoserMot(false)); };
            choix.Items.Add(h); choix.Items.Add(v);
            choix.AccessibleName = L.T("Sens du mot");
            choix.Closed += (s, e) => { if (!choisi && e.CloseReason != ToolStripDropDownCloseReason.ItemClicked) PlusTard(200, () => Parole.Dire(L.T("Annulé"))); };
            choix.Show(zone, CentreCase(ligne, colonne));
            choix.Items[0].Select();
        }

        void PoserMot(bool horiz)
        {
            var lettres = new List<char>(pressePapiers);
            var jouees = new List<char>();
            int numJoker = 0;
            foreach (char c in lettres)
            {
                if (c == Lettres.Joker)
                {
                    char x = Dialogues.LettreJoker(this, ++numJoker);
                    if (x == '\0') { PlusTard(250, () => Parole.Dire(L.T("Pose annulée."))); return; }
                    jouees.Add(x);
                }
                else jouees.Add(c);
            }

            var poses = new List<Pose>();
            int l = ligne, col = colonne;
            for (int i = 0; i < lettres.Count; i++)
            {
                while (Plateau.DansPlateau(l, col) && !partie.Plateau.EstVide(l, col)) { if (horiz) col++; else l++; }
                if (!Plateau.DansPlateau(l, col)) { Refuser(L.T("Le mot dépasse du plateau.")); return; }
                poses.Add(new Pose(l, col, jouees[i], lettres[i] == Lettres.Joker));
                if (horiz) col++; else l++;
            }

            var res = Arbitre.Evaluer(partie.Plateau, poses, lex, horiz);
            if (!res.Valide) { Refuser(res.Erreur); return; }
            enAttente = res;
            zone.Invalidate();
            string s = Lettres.Lisible(res.Principal.Mot).ToLowerInvariant() + ", " + Lettres.Points(res.Points) + "." + (res.Scrabble ? " Scrabble !" : "")
                       + res.DecrireMotsSecondaires() + L.T(" Espace pour relire, Entrée pour valider.");
            PlusTard(numJoker > 0 ? 300 : 50, () => Parole.Dire(s));
        }

        void Refuser(string raison)
        {
            Sons.Jouer("erreur");
            PlusTard(250, () => Parole.Dire(L.T("Impossible. ") + raison));
        }

        void ValiderPose()
        {
            if (occupe || partie.Terminee || partie.Courant != 0) { enAttente = null; return; }
            var coup = enAttente;
            enAttente = null;
            // Le plateau a pu changer depuis : on revérifie
            var verif = Arbitre.Evaluer(partie.Plateau, coup.Poses, lex, coup.Horizontal);
            if (!verif.Valide || !Partie.Contient(Chevalet, coup.Poses.Select(x => x.Joker ? Lettres.Joker : x.Lettre))) { Refuser(verif.Erreur ?? L.T("Les lettres ne sont plus sur votre chevalet.")); return; }

            int tirees = partie.Appliquer(verif);
            pressePapiers.Clear(); EffacerSelection();
            curseur = Math.Min(curseur, Chevalet.Count);
            Sons.Jouer("pose");
            string s = L.T("Vous jouez ") + verif.DecrirePlacement() + ", " + Lettres.Points(verif.Points) + "." + (verif.Scrabble ? " Scrabble !" : "");
            if (tirees > 0) { s += L.T(" Vous piochez ") + tirees + (L.Pl(tirees) ? L.T(" lettres.") : L.T(" lettre.")); PlusTard(700, () => Sons.Repeter("distribution", tirees, 170)); }
            Parole.Dire(s);
            zone.Invalidate();
            Sauvegarder(false);
            if (partie.Terminee) FinPartie();
            else TourOrdinateur();
        }

        // ================= Autres actions =================

        void Echanger()
        {
            RendreMain();
            if (!partie.PeutEchanger) { Parole.Dire(L.T("Échange impossible : il reste moins de 7 lettres dans le sac.")); return; }
            var l = Dialogues.LettresAEchanger(this, Chevalet);
            if (l == null) { PlusTard(250, () => Parole.Dire(L.T("Échange annulé."))); return; }
            if (l.Count == 0) { PlusTard(250, () => Parole.Dire(L.T("Aucune lettre cochée. Échange annulé."))); return; }
            partie.Echanger(l);
            EffacerSelection(); pressePapiers.Clear(); curseur = 0;
            zone.Invalidate();
            PlusTard(250, () =>
            {
                Sons.Repeter("distribution", l.Count, 170);
                Parole.Dire(L.T("Vous échangez ") + l.Count + (L.Pl(l.Count) ? L.T(" lettres") : L.T(" lettre")) + L.T(" et passez votre tour."));
                Sauvegarder(false);
                if (partie.Terminee) FinPartie(); else TourOrdinateur();
            });
        }

        void Passer()
        {
            RendreMain();
            var r = MessageBox.Show(this, L.T("Voulez-vous vraiment passer votre tour ?"), L.T("Passer"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) { PlusTard(250, () => Parole.Dire(L.T("Vous continuez à jouer."))); return; }
            partie.Passer();
            enAttente = null;
            PlusTard(250, () =>
            {
                Parole.Dire(L.T("Vous passez votre tour."));
                Sauvegarder(false);
                if (partie.Terminee) FinPartie(); else TourOrdinateur();
            });
        }

        void AnnoncerSac()
        {
            int n = partie.Sac.Count;
            string s = n == 0 ? L.T("Le sac est vide.") : L.T("Il reste ") + n + (L.Pl(n) ? L.T(" lettres") : L.T(" lettre")) + L.T(" dans le sac.");
            if (n < Partie.TailleChevalet) s += L.T(" L'ordinateur a ") + partie.Machine.Chevalet.Count + L.T(" lettres sur son chevalet.");
            Parole.Dire(s);
        }

        void Memoire()
        {
            var h = partie.Historique;
            if (h.Count == 0) { Parole.Dire(L.T("Aucun coup joué pour l'instant.")); return; }
            if ((DateTime.Now - dernierMemoire).TotalSeconds < 10 && indexMemoire > 0 && indexMemoire < h.Count) indexMemoire--;
            else if ((DateTime.Now - dernierMemoire).TotalSeconds < 10 && indexMemoire == 0) { }
            else indexMemoire = h.Count - 1;
            dernierMemoire = DateTime.Now;
            Parole.Dire(h[indexMemoire] + (indexMemoire == 0 ? L.T(" C'est le premier coup de la partie.") : ""));
        }

        // ================= Tour de l'ordinateur =================

        void TourOrdinateur()
        {
            if (partie.Terminee || partie.Courant != 1) { occupe = false; return; }
            occupe = true;
            var plateau = partie.Plateau.Copier();
            var lettres = new List<char>(partie.Machine.Chevalet);
            var niveau = partie.Niveau;
            var cettePartie = partie;
            var debut = DateTime.Now;
            var hasard = new Random(rnd.Next());
            ThreadPool.QueueUserWorkItem(_ =>
            {
                ResultatCoup coup = null;
                try { coup = Ordinateur.Choisir(plateau, lex, lettres, niveau, hasard); } catch { }
                int attente = 1800 - (int)(DateTime.Now - debut).TotalMilliseconds;
                if (attente > 0) Thread.Sleep(attente);
                try { BeginInvoke(new Action(() => { if (partie == cettePartie) JouerOrdinateur(coup); })); } catch { }
            });
        }

        void JouerOrdinateur(ResultatCoup coup)
        {
            string s;
            Sons.Jouer("ordinateur");
            if (coup != null)
            {
                partie.Appliquer(coup);
                s = L.T("L'ordinateur joue ") + coup.DecrirePlacement() + ", " + Lettres.Points(coup.Points) + "." + (coup.Scrabble ? " Scrabble !" : "");
            }
            else if (partie.PeutEchanger)
            {
                int n = partie.Machine.Chevalet.Count;
                partie.Echanger(new List<char>(partie.Machine.Chevalet));
                s = L.T("L'ordinateur échange ") + n + L.T(" lettres et passe son tour.");
            }
            else
            {
                partie.Passer();
                s = L.T("L'ordinateur passe son tour.");
            }
            occupe = false;
            // Si votre mot en attente touche les nouvelles lettres, il faudra le reposer
            enAttente = null;
            zone.Invalidate();
            Sauvegarder(false);
            if (partie.Terminee) { Parole.Dire(s, false); FinPartie(); }
            else Parole.Dire(s + L.T(" À vous de jouer."), false);
        }

        void FinPartie()
        {
            occupe = false;
            SupprimerSauvegarde();
            PlusTard(1200, () =>
            {
                Sons.Jouer("fin");
                Parole.Dire(partie.Bilan + L.T(" Ctrl+N pour une nouvelle partie, Ctrl+Q pour quitter."), false);
            });
            zone.Invalidate();
        }

        // ================= Partie, sauvegarde, fermeture =================

        void DemanderNouvellePartie()
        {
            if (occupe) { Parole.Dire(L.T("Patientez, l'ordinateur joue.")); return; }
            if (partie != null && !partie.Terminee)
            {
                var r = MessageBox.Show(this, L.T("Abandonner la partie en cours et en commencer une nouvelle ?"), L.T("Nouvelle partie"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r != DialogResult.Yes) { PlusTard(250, () => Parole.Dire(L.T("La partie continue."))); return; }
            }
            NouvellePartie(false);
        }

        void Sauvegarder(bool annoncer)
        {
            if (partie == null || partie.Terminee) return;
            RendreMain();
            try
            {
                partie.Sauvegarder(cheminSauvegarde);
                if (annoncer) Parole.Dire(L.T("Partie sauvegardée."));
            }
            catch (Exception ex)
            {
                if (annoncer) Parole.Dire(L.T("Impossible de sauvegarder : ") + ex.Message);
            }
        }

        void SupprimerSauvegarde()
        {
            try { if (File.Exists(cheminSauvegarde)) File.Delete(cheminSauvegarde); } catch { }
        }

        void Quitter()
        {
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!fermetureAutorisee && partie != null && !partie.Terminee)
            {
                var r = MessageBox.Show(this, L.T("Voulez-vous sauvegarder la partie avant de quitter ?"), L.T("Arrêter le jeu"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (r == DialogResult.Cancel) { e.Cancel = true; PlusTard(250, () => Parole.Dire(L.T("La partie continue."))); return; }
                if (r == DialogResult.Yes) Sauvegarder(false); else SupprimerSauvegarde();
            }
            fermetureAutorisee = true;
            Parole.Dire(L.T("Au revoir."));
            base.OnFormClosing(e);
        }

        void AfficherAide()
        {
            Dialogues.Texte(this, L.T("Aide de Scrabble audio"), Aide.Texte);
            PlusTard(250, () => Parole.Dire(L.T("Retour au jeu. ") + (zoneCourante == Zone.Chevalet && partie != null ? DecrireCurseur() : partie != null ? DecrireCase(ligne, colonne) : "")));
        }

        // ================= Menu =================

        void ConstruireMenu()
        {
            menu.Items.Add(Sous(L.T("&Partie"),
                Element(L.T("&Nouvelle partie"), "Ctrl+N", Keys.Control | Keys.N),
                Element(L.T("&Sauvegarder maintenant"), "", Keys.None, () => { Sauvegarder(true); }),
                Element(L.T("&Arrêter le jeu"), "Ctrl+Q", Keys.Control | Keys.Q)));
            menu.Items.Add(Sous(L.T("&Jeu"),
                Element(L.T("Aller au &chevalet"), "Ctrl+T", Keys.Control | Keys.T),
                Element(L.T("Aller au &plateau"), "Ctrl+P", Keys.Control | Keys.P),
                Element(L.T("C&opier le mot"), "Ctrl+C", Keys.Control | Keys.C),
                Element(L.T("Co&ller"), "Ctrl+V", Keys.Control | Keys.V),
                Element(L.T("Co&uper"), "Ctrl+X", Keys.Control | Keys.X),
                Element(L.T("&Échanger des lettres"), "Ctrl+E", Keys.Control | Keys.E),
                Element(L.T("Passer son &tour"), "Ctrl+J", Keys.Control | Keys.J)));
            menu.Items.Add(Sous(L.T("&Informations"),
                Element(L.T("&Résultat, les scores"), "Ctrl+R", Keys.Control | Keys.R),
                Element(L.T("Lettres dans le &sac"), "Ctrl+S", Keys.Control | Keys.S),
                Element(L.T("&Mémoire, dernier coup joué"), "Ctrl+M", Keys.Control | Keys.M)));
            var inter = Sous(L.T("&Interface"),
                Coche("&Français", L.Code == "fr", () => ChangerInterface("fr")),
                Coche("&English", L.Code == "en", () => ChangerInterface("en")),
                Coche("&Español", L.Code == "es", () => ChangerInterface("es")));
            var jeu = new ToolStripMenuItem(L.T("Langue du &jeu"));
            foreach (var c in Langues)
            {
                string cc = c;
                string nom = c == "fr" ? L.T("&Français") : c == "en" ? L.T("&Anglais") : L.T("&Espagnol");
                if (!DicoInstalle(c)) nom += L.T(" (non installé)");
                jeu.DropDownItems.Add(Coche(nom, langueJeu == c, () => ChangerLangueJeu(cc)));
            }
            menu.Items.Add(Sous(L.T("&Langue"), inter, jeu));
            menu.Items.Add(Sous(L.T("&Aide"),
                Element(L.T("&Règles et commandes"), "F1", Keys.F1),
                new ToolStripSeparator(),
                Element(L.T("Rechercher une &mise à jour"), "", Keys.None, () => RechercherMaj(true)),
                Coche(L.T("Rechercher les mises à jour au &démarrage"), majAuto, BasculerMajAuto)));
            if (!menuAbonne)
            {
                menuAbonne = true;
                menu.MenuActivate += (s, e) => menuActif = true;
                menu.MenuDeactivate += (s, e) => { menuActif = false; zone.Focus(); };
            }
        }
        bool menuAbonne;

        ToolStripMenuItem Coche(string titre, bool coche, Action action)
        {
            var m = new ToolStripMenuItem(titre) { Checked = coche };
            m.Click += (s, e) => PlusTard(150, () => { zone.Focus(); action(); });
            return m;
        }

        // ================= Langues et dictionnaires =================

        string LireOption(string nom)
        {
            try
            {
                string f = Path.Combine(dossierDonnees, "options.txt");
                if (!File.Exists(f)) return null;
                foreach (var l in File.ReadAllLines(f)) if (l.StartsWith(nom + "=")) return l.Substring(nom.Length + 1);
            }
            catch { }
            return null;
        }

        void EcrireOptions()
        {
            try
            {
                Directory.CreateDirectory(dossierDonnees);
                File.WriteAllText(Path.Combine(dossierDonnees, "options.txt"), "langue=" + L.Code + "\r\njeu=" + langueJeu + "\r\nmaj=" + (majAuto ? "1" : "0") + "\r\nmajdate=" + majDate + "\r\n");
            }
            catch { }
        }

        string FichierMots(string code)
        {
            string f = Path.Combine(dossierJeu, "dictionnaires", code, "mots.txt");
            if (File.Exists(f)) return f;
            f = ChercherDico(code);
            if (f != null) return f;
            if (code == "fr" && File.Exists(Path.Combine(dossierJeu, "mots.txt"))) return Path.Combine(dossierJeu, "mots.txt");   // ancienne installation
            return null;
        }

        readonly Dictionary<string, string> dicosTrouves = new Dictionary<string, string>();

        /// <summary>
        /// Le dictionnaire a pu être décompressé un niveau trop bas (par exemple dans un dossier portant le nom du zip) :
        /// on cherche un dossier dictionnaires\code\mots.txt n'importe où dans le dossier du jeu.
        /// </summary>
        string ChercherDico(string code)
        {
            string r;
            if (dicosTrouves.TryGetValue(code, out r)) return r;
            r = null;
            try
            {
                foreach (var f in Directory.EnumerateFiles(dossierJeu, "mots.txt", SearchOption.AllDirectories))
                {
                    var dir = new DirectoryInfo(Path.GetDirectoryName(f));
                    if (dir.Name.Equals(code, StringComparison.OrdinalIgnoreCase) && dir.Parent != null && dir.Parent.Name.Equals("dictionnaires", StringComparison.OrdinalIgnoreCase)) { r = f; break; }
                }
            }
            catch { }
            if (r != null) dicosTrouves[code] = r;
            return r;
        }

        bool DicoInstalle(string code) { return FichierMots(code) != null; }

        static string NomLangue(string code) { return code == "fr" ? L.T("français") : code == "en" ? L.T("anglais") : L.T("espagnol"); }

        string LangueSauvegarde()
        {
            try
            {
                if (!File.Exists(cheminSauvegarde)) return null;
                foreach (var l in File.ReadAllLines(cheminSauvegarde)) if (l.StartsWith("langue=")) return l.Substring(7);
                return "fr";
            }
            catch { return null; }
        }

        bool ChargerLexique(string code)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                Lettres.Langue = code;
                lex = Lexique.Charger(FichierMots(code));
                Cursor = Cursors.Default;
                return true;
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                lex = null;
                MessageBox.Show(this, L.T("Le dictionnaire mots.txt est introuvable ou illisible :\n") + ex.Message, "Scrabble audio", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        void ChangerLangueJeu(string code)
        {
            if (!DicoInstalle(code)) { Parole.Dire(L.F("Le dictionnaire {0} n'est pas installé. Téléchargez-le sur la page du jeu, puis décompressez-le dans le dossier du jeu.", NomLangue(code))); return; }
            langueJeu = code;
            EcrireOptions();
            ReconstruireMenu();
            string s = L.F("Langue du jeu : {0}.", NomLangue(code));
            if (partie != null && !partie.Terminee && partie.Langue != code) s += L.T(" Elle s'appliquera à la prochaine partie, Ctrl+N.");
            Parole.Dire(s);
        }

        void ChangerInterface(string code)
        {
            L.Code = code;
            EcrireOptions();
            ReconstruireMenu();
            zone.AccessibleDescription = L.T("Ctrl+T chevalet, Ctrl+P plateau, F1 aide");
            zone.Invalidate();
            Parole.Dire(code == "fr" ? "Interface en français." : code == "en" ? "Interface in English." : "Interfaz en español.");
        }

        void ReconstruireMenu()
        {
            menu.Items.Clear();
            ConstruireMenu();
        }

        ToolStripMenuItem Sous(string titre, params ToolStripItem[] items)
        {
            var m = new ToolStripMenuItem(titre);
            m.DropDownItems.AddRange(items);
            return m;
        }

        ToolStripMenuItem Element(string titre, string raccourci, Keys touches, Action action = null)
        {
            // Le raccourci fait partie du texte de l'élément : le lecteur d'écran l'annonce, par exemple « Mémoire, dernier coup joué, Ctrl+M »
            var m = new ToolStripMenuItem(string.IsNullOrEmpty(raccourci) ? titre : titre + ", " + raccourci);
            m.Click += (s, e) => PlusTard(150, () =>
            {
                zone.Focus();
                if (action != null) action();
                else Traiter(touches);
            });
            return m;
        }

        // ================= Outils =================

        void PlusTard(int ms, Action a)
        {
            var t = new System.Windows.Forms.Timer { Interval = Math.Max(1, ms) };
            t.Tick += (s, e) => { t.Stop(); t.Dispose(); if (!IsDisposed) a(); };
            t.Start();
        }

        // ================= Dessin (pour une personne voyante) =================

        const int Marge = 16, TailleCase = 36;

        Point CentreCase(int l, int c)
        {
            return new Point(Marge + 24 + c * TailleCase + TailleCase / 2, Marge + 24 + l * TailleCase + TailleCase / 2);
        }

        void Dessiner(Graphics g)
        {
            g.Clear(Color.FromArgb(32, 64, 48));
            if (partie == null) return;
            int ox = Marge + 24, oy = Marge + 24;
            using (var petit = new Font("Segoe UI", 7f))
            using (var grand = new Font("Segoe UI", 15f, FontStyle.Bold))
            using (var petitGras = new Font("Segoe UI", 10f, FontStyle.Bold))
            using (var valeur = new Font("Segoe UI", 7f))
            using (var texte = new Font("Segoe UI", 11f))
            using (var blanc = new SolidBrush(Color.White))
            using (var noir = new SolidBrush(Color.Black))
            {
                for (int i = 0; i < Plateau.Taille; i++)
                {
                    g.DrawString(((char)('A' + i)).ToString(), texte, blanc, Marge, oy + i * TailleCase + 8);
                    g.DrawString((i + 1).ToString(), texte, blanc, ox + i * TailleCase + 8, Marge);
                }
                for (int l = 0; l < Plateau.Taille; l++)
                    for (int c = 0; c < Plateau.Taille; c++)
                    {
                        var r = new Rectangle(ox + c * TailleCase, oy + l * TailleCase, TailleCase - 2, TailleCase - 2);
                        bool posee = enAttente != null && enAttente.Poses.Any(x => x.Ligne == l && x.Colonne == c);
                        char lettre = partie.Plateau.Lettre(l, c);
                        if (posee) lettre = enAttente.Poses.First(x => x.Ligne == l && x.Colonne == c).Lettre;
                        Color fond;
                        if (lettre != '\0') fond = posee ? Color.FromArgb(255, 230, 140) : Color.FromArgb(245, 222, 179);
                        else switch (Plateau.BonusDe(l, c))
                            {
                                case Bonus.MotTriple: fond = Color.FromArgb(200, 40, 40); break;
                                case Bonus.MotDouble: fond = Color.FromArgb(240, 150, 160); break;
                                case Bonus.LettreTriple: fond = Color.FromArgb(40, 90, 200); break;
                                case Bonus.LettreDouble: fond = Color.FromArgb(150, 200, 240); break;
                                default: fond = Color.FromArgb(60, 120, 90); break;
                            }
                        using (var b = new SolidBrush(fond)) g.FillRectangle(b, r);
                        if (lettre != '\0')
                        {
                            g.DrawString(Lettres.Nom(lettre), Lettres.Nom(lettre).Length > 1 ? petitGras : grand, noir, r.X + 4, r.Y + 3);
                            if (!partie.Plateau.EstJoker(l, c)) g.DrawString(Lettres.Valeur(lettre).ToString(), valeur, noir, r.Right - 12, r.Bottom - 13);
                        }
                        else if (l == Plateau.Centre && c == Plateau.Centre) g.DrawString("★", grand, noir, r.X + 6, r.Y + 3);
                        if (zoneCourante == Zone.Plateau && l == ligne && c == colonne)
                            using (var p = new Pen(Color.Yellow, 3)) g.DrawRectangle(p, r.X - 1, r.Y - 1, r.Width + 1, r.Height + 1);
                    }

                // Chevalet
                int cy = oy + Plateau.Taille * TailleCase + 18;
                g.DrawString(L.T("Votre chevalet :"), texte, blanc, Marge, cy + 10);
                for (int i = 0; i <= Chevalet.Count; i++)
                {
                    var r = new Rectangle(ox + 120 + i * 44, cy, 40, 40);
                    if (i < Chevalet.Count)
                    {
                        using (var b = new SolidBrush(selection.Contains(i) ? Color.FromArgb(255, 210, 90) : Color.FromArgb(245, 222, 179))) g.FillRectangle(b, r);
                        char x = Chevalet[i];
                        g.DrawString(x == Lettres.Joker ? " " : Lettres.Nom(x), Lettres.Nom(x).Length > 1 ? petitGras : grand, noir, r.X + 6, r.Y + 5);
                        if (x != Lettres.Joker) g.DrawString(Lettres.Valeur(x).ToString(), valeur, noir, r.Right - 12, r.Bottom - 13);
                    }
                    if (zoneCourante == Zone.Chevalet && i == curseur)
                        using (var p = new Pen(Color.Yellow, 3)) g.DrawRectangle(p, r.X - 2, r.Y - 2, r.Width + 3, r.Height + 3);
                }

                // Informations
                int ix = ox + Plateau.Taille * TailleCase + 20;
                string info = L.T("Niveau ") + NomNiveau(partie.Niveau) + L.T("\n\nVous : ") + partie.Humain.Score + L.T("\nOrdinateur : ") + partie.Machine.Score
                              + L.T("\n\nSac : ") + partie.Sac.Count + L.T("\n\n") + (partie.Terminee ? L.T("Partie terminée") : occupe ? L.T("L'ordinateur joue…") : L.T("À vous de jouer"))
                              + L.T("\n\nF1 : aide");
                g.DrawString(info, texte, blanc, new RectangleF(ix, oy, ClientSize.Width - ix - 10, 400));
            }
        }
    }
}
