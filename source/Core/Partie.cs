using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ScrabbleAudio.Core
{
    public class Joueur
    {
        public string Nom;
        public bool EstOrdinateur;
        public int Score;
        public List<char> Chevalet = new List<char>();
    }

    /// <summary>État complet d'une partie : plateau, sac, joueurs, tours, historique.</summary>
    public class Partie
    {
        public const int TailleChevalet = 7;
        public const int ToursBlancsMax = 6; // fin de partie après 6 tours sans point d'affilée

        public Plateau Plateau = new Plateau();
        public List<char> Sac = new List<char>();
        public Joueur[] Joueurs;
        public int Courant;               // index du joueur qui doit jouer
        public Niveau Niveau;
        public string Langue = "fr";      // langue du jeu : lettres, points et dictionnaire
        public int ToursBlancs;
        public bool Terminee;
        public string Bilan = "";
        public List<string> Historique = new List<string>();
        readonly Random rnd;

        public Partie(Random rnd) { this.rnd = rnd ?? new Random(); }

        public Joueur Humain { get { return Joueurs[0]; } }
        public Joueur Machine { get { return Joueurs[1]; } }
        public Joueur JoueurCourant { get { return Joueurs[Courant]; } }

        /// <summary>Nouvelle partie contre l'ordinateur. Le premier joueur est tiré au sort.</summary>
        public static Partie Nouvelle(Niveau niveau, Random rnd)
        {
            var pt = new Partie(rnd) { Niveau = niveau, Langue = Lettres.Langue };
            pt.Joueurs = new[] {
                new Joueur { Nom = "Vous" },
                new Joueur { Nom = "L'ordinateur", EstOrdinateur = true }
            };
            pt.Sac = Lettres.SacComplet();
            pt.Melanger(pt.Sac);
            pt.Courant = pt.rnd.Next(2);
            foreach (var j in pt.Joueurs) pt.Tirer(j);
            return pt;
        }

        void Melanger(List<char> l)
        {
            for (int i = l.Count - 1; i > 0; i--)
            {
                int k = rnd.Next(i + 1);
                char t = l[i]; l[i] = l[k]; l[k] = t;
            }
        }

        public void MelangerChevalet(Joueur j) { Melanger(j.Chevalet); }

        /// <summary>Complète le chevalet à 7 lettres. Renvoie le nombre de lettres tirées.</summary>
        public int Tirer(Joueur j)
        {
            int n = 0;
            while (j.Chevalet.Count < TailleChevalet && Sac.Count > 0)
            {
                j.Chevalet.Add(Sac[Sac.Count - 1]);
                Sac.RemoveAt(Sac.Count - 1);
                n++;
            }
            return n;
        }

        /// <summary>Vrai si le chevalet contient toutes ces lettres (jokers compris).</summary>
        public static bool Contient(List<char> chevalet, IEnumerable<char> lettres)
        {
            var reste = new List<char>(chevalet);
            foreach (char c in lettres)
                if (!reste.Remove(c)) return false;
            return true;
        }

        /// <summary>Joue un coup déjà évalué et valide. Renvoie le nombre de lettres tirées.</summary>
        public int Appliquer(ResultatCoup coup)
        {
            var j = JoueurCourant;
            foreach (var x in coup.Poses)
            {
                Plateau.Poser(x);
                j.Chevalet.Remove(x.Joker ? Lettres.Joker : x.Lettre);
            }
            j.Score += coup.Points;
            ToursBlancs = 0;
            Historique.Add(L.T(j.Nom) + (j.EstOrdinateur ? L.T(" a joué ") : L.T(" avez joué ")) + coup.DecrirePlacement() + ", " + Lettres.Points(coup.Points)
                           + "." + (coup.Scrabble ? " Scrabble !" : ""));
            int tirees = Tirer(j);
            if (j.Chevalet.Count == 0 && Sac.Count == 0) Terminer(j);
            else Suivant();
            return tirees;
        }

        public bool PeutEchanger { get { return Sac.Count >= TailleChevalet; } }

        public bool Echanger(List<char> lettres)
        {
            var j = JoueurCourant;
            if (!PeutEchanger || lettres.Count == 0 || !Contient(j.Chevalet, lettres)) return false;
            foreach (char c in lettres) j.Chevalet.Remove(c);
            Tirer(j);
            Sac.AddRange(lettres);
            Melanger(Sac);
            Historique.Add(L.T(j.Nom) + (j.EstOrdinateur ? L.T(" a échangé ") : L.T(" avez échangé ")) + lettres.Count + (L.Pl(lettres.Count) ? L.T(" lettres.") : L.T(" lettre.")));
            TourBlanc();
            return true;
        }

        public void Passer()
        {
            var j = JoueurCourant;
            Historique.Add(L.T(j.Nom) + (j.EstOrdinateur ? L.T(" a passé son tour.") : L.T(" avez passé votre tour.")));
            TourBlanc();
        }

        void TourBlanc()
        {
            ToursBlancs++;
            if (ToursBlancs >= ToursBlancsMax) Terminer(null);
            else Suivant();
        }

        void Suivant() { Courant = (Courant + 1) % Joueurs.Length; }

        void Terminer(Joueur videur)
        {
            Terminee = true;
            var sb = new StringBuilder(L.T("Partie terminée. "));
            int bonus = 0;
            foreach (var j in Joueurs)
            {
                if (j == videur) continue;
                int reste = j.Chevalet.Sum(c => Lettres.Valeur(c));
                j.Score -= reste;
                bonus += reste;
                if (reste > 0) sb.Append(L.T(j.Nom) + (j.EstOrdinateur ? L.T(" perd ") : L.T(" perdez ")) + Lettres.Points(reste) + L.T(" de lettres restantes. "));
            }
            if (videur != null)
            {
                videur.Score += bonus;
                sb.Append(L.T(videur.Nom) + (videur.EstOrdinateur ? L.T(" a posé toutes ses lettres") : L.T(" avez posé toutes vos lettres"))
                          + (bonus > 0 ? L.T(" et gagne ") + Lettres.Points(bonus) : "") + ". ");
            }
            sb.Append(DecrireScores()).Append(" ");
            int h = Humain.Score, m = Machine.Score;
            if (h > m) sb.Append(L.T("Bravo, vous avez gagné !"));
            else if (h < m) sb.Append(L.T("L'ordinateur a gagné."));
            else sb.Append(L.T("Égalité parfaite !"));
            Bilan = sb.ToString();
            Historique.Add(Bilan);
        }

        public string DecrireScores()
        {
            int h = Humain.Score, m = Machine.Score;
            string s = L.T("Vous : ") + Lettres.Points(h) + L.T(". Ordinateur : ") + Lettres.Points(m) + ".";
            if (h > m) s += L.T(" Vous menez de ") + Lettres.Points(h - m) + ".";
            else if (m > h) s += L.T(" L'ordinateur mène de ") + Lettres.Points(m - h) + ".";
            else s += L.T(" Égalité.");
            return s;
        }

        // ---------------- Sauvegarde ----------------

        public void Sauvegarder(string chemin)
        {
            var sb = new StringBuilder();
            sb.AppendLine("version=1");
            sb.AppendLine("niveau=" + Niveau);
            sb.AppendLine("langue=" + Langue);
            sb.AppendLine("courant=" + Courant);
            sb.AppendLine("toursblancs=" + ToursBlancs);
            sb.AppendLine("sac=" + new string(Sac.ToArray()));
            for (int i = 0; i < Joueurs.Length; i++)
                sb.AppendLine("joueur=" + Joueurs[i].Nom + "|" + (Joueurs[i].EstOrdinateur ? 1 : 0) + "|" + Joueurs[i].Score + "|" + new string(Joueurs[i].Chevalet.ToArray()));
            foreach (var l in Plateau.Exporter()) sb.AppendLine("plateau=" + l);
            foreach (var h in Historique) sb.AppendLine("historique=" + h.Replace("\n", " "));
            Directory.CreateDirectory(Path.GetDirectoryName(chemin));
            File.WriteAllText(chemin, sb.ToString(), Encoding.UTF8);
        }

        public static Partie Charger(string chemin, Random rnd)
        {
            var pt = new Partie(rnd);
            var joueurs = new List<Joueur>();
            var plateau = new List<string>();
            foreach (var ligne in File.ReadAllLines(chemin, Encoding.UTF8))
            {
                int k = ligne.IndexOf('=');
                if (k < 0) continue;
                string cle = ligne.Substring(0, k), v = ligne.Substring(k + 1);
                switch (cle)
                {
                    case "niveau": pt.Niveau = (Niveau)Enum.Parse(typeof(Niveau), v); break;
                    case "courant": pt.Courant = int.Parse(v); break;
                    case "langue": pt.Langue = v; break;
                    case "toursblancs": pt.ToursBlancs = int.Parse(v); break;
                    case "sac": pt.Sac = v.ToList(); break;
                    case "joueur":
                        var t = v.Split('|');
                        joueurs.Add(new Joueur { Nom = t[0], EstOrdinateur = t[1] == "1", Score = int.Parse(t[2]), Chevalet = t[3].ToList() });
                        break;
                    case "plateau": plateau.Add(v); break;
                    case "historique": pt.Historique.Add(v); break;
                }
            }
            if (joueurs.Count != 2 || plateau.Count != Plateau.Taille) throw new InvalidDataException(L.T("Sauvegarde incomplète."));
            pt.Joueurs = joueurs.ToArray();
            pt.Plateau.Importer(plateau.ToArray());
            return pt;
        }
    }
}
