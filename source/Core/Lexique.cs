using System;
using System.Collections.Generic;
using System.IO;

namespace ScrabbleAudio.Core
{
    /// <summary>
    /// Dictionnaire stocké sous forme d'arbre de lettres compact (trie).
    /// Sert à la fois à vérifier un mot et à guider l'ordinateur dans sa recherche de coups.
    /// </summary>
    public class Lexique
    {
        int[] premierEnfant = new int[1 << 20];
        int[] frere = new int[1 << 20];
        char[] lettre = new char[1 << 20];
        bool[] terminal = new bool[1 << 20];
        int nb = 1; // le nœud 0 est la racine

        public int NombreMots { get; private set; }
        public const int Racine = 0;

        public Lexique() { premierEnfant[0] = -1; frere[0] = -1; }

        public static Lexique Charger(string chemin)
        {
            var lex = new Lexique();
            foreach (var ligne in File.ReadLines(chemin))
            {
                // En espagnol, CH, LL et RR deviennent chacun un seul jeton (voir Lettres.EnJetons).
                string m = Lettres.EnJetons(ligne.Trim().ToUpperInvariant());
                if (m.Length >= 2 && m.Length <= 15) lex.Ajouter(m);
            }
            return lex;
        }

        int NouveauNoeud(char c)
        {
            if (nb == premierEnfant.Length)
            {
                int t = nb * 2;
                Array.Resize(ref premierEnfant, t); Array.Resize(ref frere, t);
                Array.Resize(ref lettre, t); Array.Resize(ref terminal, t);
            }
            premierEnfant[nb] = -1; frere[nb] = -1; lettre[nb] = c; terminal[nb] = false;
            return nb++;
        }

        public void Ajouter(string mot)
        {
            int n = Racine;
            foreach (char c in mot)
            {
                if (!Lettres.EstLettre(c)) return;
                int e = Enfant(n, c);
                if (e < 0)
                {
                    e = NouveauNoeud(c);
                    frere[e] = premierEnfant[n];
                    premierEnfant[n] = e;
                }
                n = e;
            }
            if (!terminal[n]) { terminal[n] = true; NombreMots++; }
        }

        public int Enfant(int noeud, char c)
        {
            for (int e = premierEnfant[noeud]; e >= 0; e = frere[e])
                if (lettre[e] == c) return e;
            return -1;
        }

        public bool EstTerminal(int noeud) { return terminal[noeud]; }

        /// <summary>Énumère les enfants d'un nœud : lettre et numéro du nœud.</summary>
        public IEnumerable<KeyValuePair<char, int>> Enfants(int noeud)
        {
            for (int e = premierEnfant[noeud]; e >= 0; e = frere[e])
                yield return new KeyValuePair<char, int>(lettre[e], e);
        }

        public bool Contient(string mot)
        {
            int n = Racine;
            foreach (char c in mot)
            {
                n = Enfant(n, c);
                if (n < 0) return false;
            }
            return terminal[n];
        }
    }
}
