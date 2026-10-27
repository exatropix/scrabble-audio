using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScrabbleAudio.Core
{
    public class MotForme
    {
        public string Mot;
        public bool Horizontal;
        public int Ligne, Colonne; // première lettre
        public int Points;
        public bool Principal;
    }

    public class ResultatCoup
    {
        public bool Valide;
        public string Erreur;
        public List<MotForme> Mots = new List<MotForme>();
        public int Points;
        public bool Scrabble; // les 7 lettres posées : prime de 50 points
        public bool Horizontal;
        public List<Pose> Poses = new List<Pose>();

        public MotForme Principal { get { return Mots.FirstOrDefault(m => m.Principal) ?? Mots.FirstOrDefault(); } }

        public static ResultatCoup Refus(string erreur)
        {
            return new ResultatCoup { Valide = false, Erreur = erreur };
        }

        /// <summary>"maison en H4, horizontal"</summary>
        public string DecrirePlacement()
        {
            var p = Principal;
            if (p == null) return "";
            return Lettres.Lisible(p.Mot).ToLowerInvariant() + L.T(" en ") + Plateau.Coord(p.Ligne, p.Colonne) + ", " + (p.Horizontal ? L.T("horizontal") : L.T("vertical"));
        }

        /// <summary>"Mots formés : maison, as." si plusieurs mots.</summary>
        public string DecrireMotsSecondaires()
        {
            if (Mots.Count < 2) return "";
            return L.T(" Mots formés : ") + string.Join(", ", Mots.Select(m => Lettres.Lisible(m.Mot).ToLowerInvariant() + " " + Lettres.Points(m.Points))) + ".";
        }
    }

    /// <summary>Vérifie un coup selon les règles et en calcule les points.</summary>
    public static class Arbitre
    {
        public const int PrimeScrabble = 50;

        public static ResultatCoup Evaluer(Plateau p, IList<Pose> poses, Lexique lex, bool horizontalPrefere)
        {
            if (poses == null || poses.Count == 0) return ResultatCoup.Refus(L.T("Aucune lettre à poser."));

            var nouvelles = new Dictionary<int, Pose>();
            foreach (var x in poses)
            {
                if (!Plateau.DansPlateau(x.Ligne, x.Colonne)) return ResultatCoup.Refus(L.T("Le mot dépasse du plateau."));
                if (!p.EstVide(x.Ligne, x.Colonne)) return ResultatCoup.Refus(L.T("La case ") + Plateau.Coord(x.Ligne, x.Colonne) + L.T(" est déjà occupée."));
                int k = x.Ligne * Plateau.Taille + x.Colonne;
                if (nouvelles.ContainsKey(k)) return ResultatCoup.Refus(L.T("Deux lettres sur la même case."));
                nouvelles[k] = x;
            }

            bool memeLigne = poses.All(x => x.Ligne == poses[0].Ligne);
            bool memeColonne = poses.All(x => x.Colonne == poses[0].Colonne);
            bool horiz;
            if (poses.Count > 1)
            {
                if (memeLigne) horiz = true;
                else if (memeColonne) horiz = false;
                else return ResultatCoup.Refus(L.T("Les lettres doivent être sur une même ligne ou une même colonne."));
            }
            else
            {
                horiz = horizontalPrefere;
                if (LongueurMot(p, nouvelles, poses[0].Ligne, poses[0].Colonne, horiz) < 2
                    && LongueurMot(p, nouvelles, poses[0].Ligne, poses[0].Colonne, !horiz) >= 2)
                    horiz = !horiz;
            }

            // Pas de trou entre la première et la dernière lettre posée
            int ligne = poses[0].Ligne, col = poses[0].Colonne;
            int min = horiz ? poses.Min(x => x.Colonne) : poses.Min(x => x.Ligne);
            int max = horiz ? poses.Max(x => x.Colonne) : poses.Max(x => x.Ligne);
            for (int i = min; i <= max; i++)
            {
                int l = horiz ? ligne : i, c = horiz ? i : col;
                if (Lire(p, nouvelles, l, c) == '\0') return ResultatCoup.Refus(L.T("Il y a une case vide au milieu du mot, en ") + Plateau.Coord(l, c) + ".");
            }

            var res = new ResultatCoup { Horizontal = horiz };
            res.Poses.AddRange(poses);

            // Mot principal
            MotForme principal = LireMot(p, nouvelles, poses[0].Ligne, poses[0].Colonne, horiz);
            bool touche = false;

            if (p.EstVierge)
            {
                if (!nouvelles.ContainsKey(Plateau.Centre * Plateau.Taille + Plateau.Centre))
                    return ResultatCoup.Refus(L.T("Le premier mot doit passer par la case centrale H8."));
                if (principal.Mot.Length < 2) return ResultatCoup.Refus(L.T("Un mot doit contenir au moins deux lettres."));
                touche = true;
            }

            if (principal.Mot.Length >= 2)
            {
                principal.Principal = true;
                res.Mots.Add(principal);
                if (principal.Mot.Length > poses.Count) touche = true; // utilise une lettre déjà posée
            }

            // Mots croisés formés par chaque nouvelle lettre
            foreach (var x in poses)
            {
                MotForme croise = LireMot(p, nouvelles, x.Ligne, x.Colonne, !horiz);
                if (croise.Mot.Length >= 2)
                {
                    touche = true;
                    if (res.Mots.Count == 0) croise.Principal = true;
                    res.Mots.Add(croise);
                }
            }

            if (res.Mots.Count == 0) return ResultatCoup.Refus(L.T("Un mot doit contenir au moins deux lettres."));
            if (!touche) return ResultatCoup.Refus(L.T("Le mot doit toucher une lettre déjà posée sur le plateau."));

            foreach (var m in res.Mots)
            {
                if (!lex.Contient(m.Mot))
                {
                    string ou = m.Principal ? "" : L.T(", formé ") + (m.Horizontal ? L.T("horizontalement") : L.T("verticalement")) + L.T(" en ") + Plateau.Coord(m.Ligne, m.Colonne) + ",";
                    return ResultatCoup.Refus(L.T("Le mot ") + Lettres.Lisible(m.Mot).ToLowerInvariant() + ou + L.T(" n'existe pas dans le dictionnaire."));
                }
            }

            int total = 0;
            foreach (var m in res.Mots)
            {
                m.Points = CompterMot(p, nouvelles, m);
                total += m.Points;
            }
            if (poses.Count == 7) { total += PrimeScrabble; res.Scrabble = true; }
            res.Points = total;
            res.Valide = true;
            return res;
        }

        static char Lire(Plateau p, Dictionary<int, Pose> nouvelles, int l, int c)
        {
            if (!Plateau.DansPlateau(l, c)) return '\0';
            Pose x;
            if (nouvelles.TryGetValue(l * Plateau.Taille + c, out x)) return x.Lettre;
            return p.Lettre(l, c);
        }

        static int LongueurMot(Plateau p, Dictionary<int, Pose> nouvelles, int l, int c, bool horiz)
        {
            return LireMot(p, nouvelles, l, c, horiz).Mot.Length;
        }

        static MotForme LireMot(Plateau p, Dictionary<int, Pose> nouvelles, int l, int c, bool horiz)
        {
            int dl = horiz ? 0 : 1, dc = horiz ? 1 : 0;
            int sl = l, sc = c;
            while (Lire(p, nouvelles, sl - dl, sc - dc) != '\0') { sl -= dl; sc -= dc; }
            var sb = new StringBuilder();
            int el = sl, ec = sc;
            while (Lire(p, nouvelles, el, ec) != '\0') { sb.Append(Lire(p, nouvelles, el, ec)); el += dl; ec += dc; }
            return new MotForme { Mot = sb.ToString(), Horizontal = horiz, Ligne = sl, Colonne = sc };
        }

        static int CompterMot(Plateau p, Dictionary<int, Pose> nouvelles, MotForme m)
        {
            int dl = m.Horizontal ? 0 : 1, dc = m.Horizontal ? 1 : 0;
            int somme = 0, multiplicateur = 1;
            for (int i = 0; i < m.Mot.Length; i++)
            {
                int l = m.Ligne + dl * i, c = m.Colonne + dc * i;
                Pose x;
                if (nouvelles.TryGetValue(l * Plateau.Taille + c, out x))
                {
                    int v = x.Joker ? 0 : Lettres.Valeur(x.Lettre);
                    switch (Plateau.BonusDe(l, c))
                    {
                        case Bonus.LettreDouble: v *= 2; break;
                        case Bonus.LettreTriple: v *= 3; break;
                        case Bonus.MotDouble: multiplicateur *= 2; break;
                        case Bonus.MotTriple: multiplicateur *= 3; break;
                    }
                    somme += v;
                }
                else
                {
                    somme += p.EstJoker(l, c) ? 0 : Lettres.Valeur(p.Lettre(l, c));
                }
            }
            return somme * multiplicateur;
        }
    }
}
