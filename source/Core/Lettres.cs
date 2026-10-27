using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScrabbleAudio.Core
{
    /// <summary>
    /// Valeurs et répartition des lettres selon la langue du jeu : français (102 jetons), anglais (100), espagnol (100).
    /// En espagnol, CH, LL et RR sont des jetons à part entière ; dans le programme, ils sont notés par un seul caractère :
    /// Ç pour CH, Ł pour LL, Ř pour RR. Le Ñ est une lettre normale.
    /// </summary>
    public static class Lettres
    {
        public const char Joker = '?';
        public const char CH = 'Ç', LL = 'Ł', RR = 'Ř';
        public const int NombreJokers = 2;

        class Jeton { public char C; public int Valeur, Nombre; public Jeton(char c, int v, int n) { C = c; Valeur = v; Nombre = n; } }

        static Jeton[] J(string def)
        {
            // "A1x9 B3x2 ..." : lettre, valeur, nombre
            return def.Split(' ').Select(t =>
            {
                int x = t.IndexOf('x');
                return new Jeton(t[0], int.Parse(t.Substring(1, x - 1)), int.Parse(t.Substring(x + 1)));
            }).ToArray();
        }

        static readonly Jeton[] Fr = J("A1x9 B3x2 C3x2 D2x3 E1x15 F4x2 G2x2 H4x2 I1x8 J8x1 K10x1 L1x5 M2x3 N1x6 O1x6 P3x2 Q8x1 R1x6 S1x6 T1x6 U1x6 V4x2 W10x1 X10x1 Y10x1 Z10x1");
        static readonly Jeton[] En = J("A1x9 B3x2 C3x2 D2x4 E1x12 F4x2 G2x3 H4x2 I1x9 J8x1 K5x1 L1x4 M3x2 N1x6 O1x8 P3x2 Q10x1 R1x6 S1x4 T1x6 U1x4 V4x2 W4x2 X8x1 Y4x2 Z10x1");
        static readonly Jeton[] Es = J("A1x12 B3x2 C3x4 Ç5x1 D2x5 E1x12 F4x1 G2x2 H4x2 I1x6 J8x1 L1x4 Ł8x1 M3x2 N1x5 Ñ8x1 O1x9 P3x2 Q5x1 R1x5 Ř8x1 S1x6 T1x4 U1x5 V4x1 X8x1 Y4x1 Z10x1");

        static string langue = "fr";
        static Jeton[] jeu = Fr;
        static Dictionary<char, int> index;

        /// <summary>Toutes les lettres que le dictionnaire de cette langue peut contenir, dans l'ordre.</summary>
        public static char[] Alphabet { get; private set; }

        static Lettres() { Langue = "fr"; }

        public static string Langue
        {
            get { return langue; }
            set
            {
                langue = value == "en" || value == "es" ? value : "fr";
                jeu = langue == "en" ? En : langue == "es" ? Es : Fr;
                var alpha = new List<char>();
                for (char c = 'A'; c <= 'Z'; c++) alpha.Add(c);   // K et W n'ont pas de jeton en espagnol, mais un joker peut les jouer
                if (langue == "es") alpha.AddRange(new[] { 'Ñ', CH, LL, RR });
                Alphabet = alpha.ToArray();
                index = new Dictionary<char, int>();
                for (int i = 0; i < Alphabet.Length; i++) index[Alphabet[i]] = i;
            }
        }

        /// <summary>Position de la lettre dans l'alphabet de la langue, ou -1.</summary>
        public static int Index(char c) { int i; return index.TryGetValue(c, out i) ? i : -1; }
        public static bool EstLettre(char c) { return index.ContainsKey(c); }

        public static int Valeur(char c)
        {
            if (c == Joker) return 0;
            foreach (var j in jeu) if (j.C == c) return j.Valeur;
            return 0;
        }

        public static List<char> SacComplet()
        {
            var sac = new List<char>();
            foreach (var j in jeu) for (int n = 0; n < j.Nombre; n++) sac.Add(j.C);
            for (int n = 0; n < NombreJokers; n++) sac.Add(Joker);
            return sac;
        }

        /// <summary>Nom affiché ou parlé d'une lettre : "E", "CH", ou "joker".</summary>
        public static string Nom(char c)
        {
            if (c == Joker) return L.T("joker");
            if (c == CH) return "CH";
            if (c == LL) return "LL";
            if (c == RR) return "RR";
            return c.ToString();
        }

        /// <summary>Remet un mot interne en lettres ordinaires : PEŘO devient PERRO.</summary>
        public static string Lisible(string mot)
        {
            if (mot == null) return "";
            var sb = new StringBuilder();
            foreach (char c in mot) sb.Append(c == Joker ? "?" : Nom(c));
            return sb.ToString();
        }

        /// <summary>Découpe un mot du dictionnaire en jetons : en espagnol, PERRO devient PEŘO.</summary>
        public static string EnJetons(string mot)
        {
            if (langue != "es") return mot;
            return mot.Replace("CH", CH.ToString()).Replace("LL", LL.ToString()).Replace("RR", RR.ToString());
        }

        public static string Points(int n)
        {
            return n + (L.Pl(n) ? L.T(" points") : L.T(" point"));
        }

        /// <summary>Description parlée d'un jeton : "E, 1 point".</summary>
        public static string Decrire(char c)
        {
            return Nom(c) + ", " + Points(Valeur(c));
        }

        /// <summary>Lettre tapée par le joueur pour un joker : "e", "é", "ñ", ou en espagnol "ch", "ll", "rr". Renvoie '\0' si ce n'est pas une lettre du jeu.</summary>
        public static char Normaliser(string saisie)
        {
            if (string.IsNullOrEmpty(saisie)) return '\0';
            string s = saisie.Trim().ToUpperInvariant();
            if (langue == "es")
            {
                if (s == "CH") return CH;
                if (s == "LL") return LL;
                if (s == "RR") return RR;
                if (s == "Ñ") return 'Ñ';
            }
            if (s.Length != 1) return '\0';
            if (s == "Œ") return 'O';
            string d = s.Normalize(NormalizationForm.FormD);
            foreach (char x in d)
            {
                char u = char.ToUpperInvariant(x);
                if (u >= 'A' && u <= 'Z') return u;
            }
            return '\0';
        }

        /// <summary>Valeurs des lettres pour l'aide : "1 point : A, E, I…"</summary>
        public static string Bareme()
        {
            var groupes = jeu.GroupBy(j => j.Valeur).OrderBy(g => g.Key);
            return string.Join(" ", groupes.Select(g => Points(g.Key) + " : " + string.Join(", ", g.Select(j => Nom(j.C))) + "."))
                   + " " + L.T("Le joker vaut 0 point.");
        }

        public static int NombreJetons { get { return jeu.Sum(j => j.Nombre) + NombreJokers; } }
    }
}
