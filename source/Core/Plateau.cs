using System;
using System.Text;

namespace ScrabbleAudio.Core
{
    public enum Bonus { Aucun, LettreDouble, LettreTriple, MotDouble, MotTriple }

    /// <summary>Une lettre posée (ou à poser) sur le plateau.</summary>
    public struct Pose
    {
        public int Ligne, Colonne;
        public char Lettre;   // lettre affichée, A à Z
        public bool Joker;    // vrai si c'est un joker qui joue cette lettre

        public Pose(int ligne, int colonne, char lettre, bool joker)
        {
            Ligne = ligne; Colonne = colonne; Lettre = lettre; Joker = joker;
        }
    }

    /// <summary>
    /// Plateau classique de 15 x 15 cases. Les lignes sont nommées A à O (de haut en bas),
    /// les colonnes 1 à 15 (de gauche à droite) : la case centrale est H8.
    /// </summary>
    public class Plateau
    {
        public const int Taille = 15;
        public const int Centre = 7;

        readonly char[,] lettres = new char[Taille, Taille];
        readonly bool[,] jokers = new bool[Taille, Taille];
        public int NombreTuiles { get; private set; }

        static readonly Bonus[,] bonus = CreerBonus();

        static Bonus[,] CreerBonus()
        {
            var b = new Bonus[Taille, Taille];
            int[,] mt = { { 0, 0 }, { 0, 7 }, { 0, 14 }, { 7, 0 }, { 7, 14 }, { 14, 0 }, { 14, 7 }, { 14, 14 } };
            for (int i = 0; i < mt.GetLength(0); i++) b[mt[i, 0], mt[i, 1]] = Bonus.MotTriple;
            for (int i = 1; i <= 4; i++)
            {
                b[i, i] = Bonus.MotDouble; b[i, 14 - i] = Bonus.MotDouble;
                b[14 - i, i] = Bonus.MotDouble; b[14 - i, 14 - i] = Bonus.MotDouble;
            }
            b[7, 7] = Bonus.MotDouble;
            int[,] lt = { { 1, 5 }, { 1, 9 }, { 5, 1 }, { 5, 5 }, { 5, 9 }, { 5, 13 }, { 9, 1 }, { 9, 5 }, { 9, 9 }, { 9, 13 }, { 13, 5 }, { 13, 9 } };
            for (int i = 0; i < lt.GetLength(0); i++) b[lt[i, 0], lt[i, 1]] = Bonus.LettreTriple;
            int[,] ld = { { 0, 3 }, { 0, 11 }, { 2, 6 }, { 2, 8 }, { 3, 0 }, { 3, 7 }, { 3, 14 }, { 6, 2 }, { 6, 6 }, { 6, 8 }, { 6, 12 },
                          { 7, 3 }, { 7, 11 }, { 8, 2 }, { 8, 6 }, { 8, 8 }, { 8, 12 }, { 11, 0 }, { 11, 7 }, { 11, 14 },
                          { 12, 6 }, { 12, 8 }, { 14, 3 }, { 14, 11 } };
            for (int i = 0; i < ld.GetLength(0); i++) b[ld[i, 0], ld[i, 1]] = Bonus.LettreDouble;
            return b;
        }

        public static bool DansPlateau(int l, int c)
        {
            return l >= 0 && l < Taille && c >= 0 && c < Taille;
        }

        public static Bonus BonusDe(int l, int c) { return bonus[l, c]; }

        public char Lettre(int l, int c) { return lettres[l, c]; }
        public bool EstJoker(int l, int c) { return jokers[l, c]; }
        public bool EstVide(int l, int c) { return lettres[l, c] == '\0'; }
        public bool EstOccupee(int l, int c) { return DansPlateau(l, c) && lettres[l, c] != '\0'; }
        public bool EstVierge { get { return NombreTuiles == 0; } }

        public void Poser(Pose p)
        {
            if (lettres[p.Ligne, p.Colonne] == '\0') NombreTuiles++;
            lettres[p.Ligne, p.Colonne] = p.Lettre;
            jokers[p.Ligne, p.Colonne] = p.Joker;
        }

        public static string Coord(int l, int c)
        {
            return ((char)('A' + l)).ToString() + (c + 1);
        }

        public static string NomBonus(Bonus b)
        {
            switch (b)
            {
                case Bonus.LettreDouble: return L.T("lettre compte double");
                case Bonus.LettreTriple: return L.T("lettre compte triple");
                case Bonus.MotDouble: return L.T("mot compte double");
                case Bonus.MotTriple: return L.T("mot compte triple");
                default: return "";
            }
        }

        /// <summary>Description parlée d'une case : "H8, vide, mot compte double" ou "H9, A".</summary>
        public string DecrireCase(int l, int c)
        {
            var sb = new StringBuilder(Coord(l, c));
            if (EstVide(l, c))
            {
                sb.Append(L.T(", vide"));
                Bonus b = BonusDe(l, c);
                if (b != Bonus.Aucun) sb.Append(", ").Append(NomBonus(b));
                if (l == Centre && c == Centre) sb.Append(L.T(", case centrale"));
            }
            else
            {
                sb.Append(", ").Append(Lettres.Nom(Lettre(l, c)));
                if (EstJoker(l, c)) sb.Append(L.T(", joker"));
            }
            return sb.ToString();
        }

        /// <summary>Lit une ligne ou une colonne : seulement les lettres posées et les blancs entre elles.</summary>
        public string DecrireLigne(int index, bool horizontal)
        {
            var sb = new StringBuilder();
            int vides = 0;
            bool rien = true;
            for (int i = 0; i < Taille; i++)
            {
                int l = horizontal ? index : i, c = horizontal ? i : index;
                if (EstVide(l, c)) { vides++; continue; }
                if (!rien && vides > 0) sb.Append(vides).Append(L.Pl(vides) ? L.T(" vides, ") : L.T(" vide, "));
                sb.Append(Coord(l, c)).Append(" ").Append(Lettres.Nom(Lettre(l, c))).Append(", ");
                rien = false; vides = 0;
            }
            string nom = horizontal ? L.T("Ligne ") + (char)('A' + index) : L.T("Colonne ") + (index + 1);
            if (rien) return nom + L.T(" : vide.");
            return nom + " : " + sb.ToString().TrimEnd(' ', ',') + ".";
        }

        // ---- Sauvegarde : 15 lignes, "." vide, majuscule = lettre, minuscule = joker
        public string[] Exporter()
        {
            var lignes = new string[Taille];
            for (int l = 0; l < Taille; l++)
            {
                var sb = new StringBuilder();
                for (int c = 0; c < Taille; c++)
                {
                    char x = lettres[l, c];
                    sb.Append(x == '\0' ? '.' : (jokers[l, c] ? char.ToLowerInvariant(x) : x));
                }
                lignes[l] = sb.ToString();
            }
            return lignes;
        }

        public void Importer(string[] lignes)
        {
            for (int l = 0; l < Taille; l++)
                for (int c = 0; c < Taille; c++)
                {
                    char x = lignes[l][c];
                    if (x == '.') continue;
                    Poser(new Pose(l, c, char.ToUpperInvariant(x), char.IsLower(x)));
                }
        }

        public Plateau Copier()
        {
            var p = new Plateau();
            p.Importer(Exporter());
            return p;
        }
    }
}
