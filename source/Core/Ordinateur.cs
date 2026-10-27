using System;
using System.Collections.Generic;
using System.Linq;

namespace ScrabbleAudio.Core
{
    public enum Niveau { Debutant, Moyen, Expert }

    /// <summary>
    /// Recherche de tous les coups possibles (algorithme d'Appel et Jacobson)
    /// puis choix d'un coup selon le niveau.
    /// </summary>
    public class Generateur
    {
        readonly Plateau p;
        readonly Lexique lex;
        readonly int[] chevalet;               // une case par lettre de l'alphabet, la dernière pour le joker
        readonly int J;                          // case du joker
        bool horiz;
        int ligne;
        int[] controle = new int[Plateau.Taille];   // lettres autorisées par case (masque de bits)
        bool[] ancre = new bool[Plateau.Taille];
        readonly List<Pose> enCours = new List<Pose>();
        readonly Dictionary<string, List<Pose>> trouves = new Dictionary<string, List<Pose>>();
        readonly int Toutes;
        int ancreCourante;

        public Generateur(Plateau p, Lexique lex, IEnumerable<char> lettres)
        {
            this.p = p; this.lex = lex;
            J = Lettres.Alphabet.Length;
            chevalet = new int[J + 1];
            Toutes = J >= 31 ? -1 : (1 << J) - 1;
            foreach (char c in lettres)
            {
                int i = c == Lettres.Joker ? J : Lettres.Index(c);
                if (i >= 0) chevalet[i]++;
            }
        }

        char Case(int pos) { return horiz ? p.Lettre(ligne, pos) : p.Lettre(pos, ligne); }
        bool Vide(int pos) { return Case(pos) == '\0'; }
        char Voisin(int pos, int decalage)
        {
            int l = horiz ? ligne + decalage : pos, c = horiz ? pos : ligne + decalage;
            return Plateau.DansPlateau(l, c) ? p.Lettre(l, c) : '\0';
        }

        public List<ResultatCoup> Generer()
        {
            for (int d = 0; d < 2; d++)
            {
                horiz = d == 0;
                for (ligne = 0; ligne < Plateau.Taille; ligne++)
                {
                    PreparerLigne();
                    for (int a = 0; a < Plateau.Taille; a++)
                    {
                        if (!ancre[a]) continue;
                        ancreCourante = a;
                        if (a > 0 && !Vide(a - 1))
                        {
                            int debut = a - 1;
                            while (debut > 0 && !Vide(debut - 1)) debut--;
                            int n = Lexique.Racine;
                            for (int i = debut; i < a && n >= 0; i++) n = lex.Enfant(n, Case(i));
                            if (n >= 0) EtendreDroite(n, a);
                        }
                        else
                        {
                            int limite = 0;
                            for (int i = a - 1; i >= 0 && Vide(i) && !ancre[i]; i--) limite++;
                            PartieGauche(Lexique.Racine, limite, 0);
                        }
                    }
                }
            }
            var res = new List<ResultatCoup>();
            foreach (var poses in trouves.Values)
            {
                var r = Arbitre.Evaluer(p, poses, lex, poses.Count > 1 ? poses[0].Ligne == poses[1].Ligne : true);
                if (r.Valide) res.Add(r);
            }
            return res;
        }

        void PreparerLigne()
        {
            for (int pos = 0; pos < Plateau.Taille; pos++)
            {
                ancre[pos] = false; controle[pos] = Toutes;
                if (!Vide(pos)) continue;
                if (p.EstVierge)
                {
                    ancre[pos] = ligne == Plateau.Centre && pos == Plateau.Centre;
                    continue;
                }
                bool haut = Voisin(pos, -1) != '\0', bas = Voisin(pos, 1) != '\0';
                bool gauche = pos > 0 && !Vide(pos - 1), droite = pos < Plateau.Taille - 1 && !Vide(pos + 1);
                ancre[pos] = haut || bas || gauche || droite;
                if (haut || bas)
                {
                    string avant = "", apres = "";
                    for (int k = -1; Voisin(pos, k) != '\0'; k--) avant = Voisin(pos, k) + avant;
                    for (int k = 1; Voisin(pos, k) != '\0'; k++) apres += Voisin(pos, k);
                    int masque = 0;
                    for (int i = 0; i < J; i++)
                        if (lex.Contient(avant + Lettres.Alphabet[i] + apres)) masque |= 1 << i;
                    controle[pos] = masque;
                }
            }
        }

        void PartieGauche(int noeud, int limite, int longueur)
        {
            EtendreDroite(noeud, ancreCourante);
            if (limite <= 0) return;
            foreach (var e in lex.Enfants(noeud))
            {
                int i = Lettres.Index(e.Key);
                for (int essai = 0; essai < 2; essai++)
                {
                    bool joker = essai == 1;
                    if (joker ? chevalet[J] == 0 : chevalet[i] == 0) continue;
                    if (joker) chevalet[J]--; else chevalet[i]--;
                    // les lettres de gauche sont décalées vers la gauche à chaque ajout
                    for (int k = 0; k < enCours.Count; k++) enCours[k] = Decaler(enCours[k], -1);
                    enCours.Add(PoseA(ancreCourante - 1, e.Key, joker));
                    PartieGauche(e.Value, limite - 1, longueur + 1);
                    enCours.RemoveAt(enCours.Count - 1);
                    for (int k = 0; k < enCours.Count; k++) enCours[k] = Decaler(enCours[k], 1);
                    if (joker) chevalet[J]++; else chevalet[i]++;
                }
            }
        }

        Pose Decaler(Pose x, int d)
        {
            if (horiz) x.Colonne += d; else x.Ligne += d;
            return x;
        }

        Pose PoseA(int pos, char c, bool joker)
        {
            return horiz ? new Pose(ligne, pos, c, joker) : new Pose(pos, ligne, c, joker);
        }

        void EtendreDroite(int noeud, int pos)
        {
            if (pos >= Plateau.Taille || Vide(pos))
            {
                if (pos > ancreCourante && lex.EstTerminal(noeud) && enCours.Count > 0) Noter();
                if (pos >= Plateau.Taille) return;
                foreach (var e in lex.Enfants(noeud))
                {
                    int i = Lettres.Index(e.Key);
                    if ((controle[pos] & (1 << i)) == 0) continue;
                    if (chevalet[i] > 0)
                    {
                        chevalet[i]--; enCours.Add(PoseA(pos, e.Key, false));
                        EtendreDroite(e.Value, pos + 1);
                        enCours.RemoveAt(enCours.Count - 1); chevalet[i]++;
                    }
                    if (chevalet[J] > 0)
                    {
                        chevalet[J]--; enCours.Add(PoseA(pos, e.Key, true));
                        EtendreDroite(e.Value, pos + 1);
                        enCours.RemoveAt(enCours.Count - 1); chevalet[J]++;
                    }
                }
            }
            else
            {
                int n = lex.Enfant(noeud, Case(pos));
                if (n >= 0) EtendreDroite(n, pos + 1);
            }
        }

        void Noter()
        {
            var poses = enCours.OrderBy(x => x.Ligne).ThenBy(x => x.Colonne).ToList();
            string cle = string.Join(";", poses.Select(x => x.Ligne + "," + x.Colonne + x.Lettre + (x.Joker ? "*" : "")));
            if (!trouves.ContainsKey(cle)) trouves[cle] = poses;
        }
    }

    public static class Ordinateur
    {
        /// <summary>Choisit un coup, ou null si aucun coup n'est possible.</summary>
        public static ResultatCoup Choisir(Plateau p, Lexique lex, List<char> chevalet, Niveau niveau, Random rnd)
        {
            var coups = new Generateur(p, lex, chevalet).Generer();
            if (coups.Count == 0) return null;
            coups.Sort((a, b) => b.Points.CompareTo(a.Points));
            int meilleur = coups[0].Points;

            switch (niveau)
            {
                case Niveau.Expert:
                    {
                        // Le meilleur score ; à égalité, on préfère garder ses jokers
                        var top = coups.Where(c => c.Points == meilleur).ToList();
                        int minJokers = top.Min(c => c.Poses.Count(x => x.Joker));
                        top = top.Where(c => c.Poses.Count(x => x.Joker) == minJokers).ToList();
                        return top[rnd.Next(top.Count)];
                    }
                case Niveau.Moyen:
                    {
                        // Un bon coup, mais pas toujours le meilleur ; garde ses jokers pour les gros coups
                        var choix = coups.Where(c => c.Points >= meilleur * 0.55 && c.Points <= meilleur * 0.9
                                                     && (c.Points >= 40 || !c.Poses.Any(x => x.Joker))).ToList();
                        if (choix.Count == 0) choix = coups.Take(5).ToList();
                        return choix[rnd.Next(choix.Count)];
                    }
                default:
                    {
                        // Des mots courts et des coups modestes
                        var choix = coups.Where(c => c.Principal.Mot.Length <= 5 && !c.Poses.Any(x => x.Joker)
                                                     && c.Points <= Math.Max(12, meilleur * 0.5)).ToList();
                        if (choix.Count == 0) choix = coups.Skip(coups.Count / 2).ToList();
                        if (choix.Count == 0) choix = coups;
                        return choix[rnd.Next(choix.Count)];
                    }
            }
        }
    }
}
