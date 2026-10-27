using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ScrabbleAudio.Core;

static class Tests
{
    static int echecs = 0;
    static void Verifier(bool ok, string nom)
    {
        Console.WriteLine((ok ? "OK    " : "ECHEC ") + nom);
        if (!ok) echecs++;
    }

    static int Main(string[] args)
    {
        var sw = Stopwatch.StartNew();
        var lex = Lexique.Charger(args[0]);
        Console.WriteLine("Dictionnaire : " + lex.NombreMots + " mots en " + sw.ElapsedMilliseconds + " ms");
        Verifier(lex.Contient("MAISON") && lex.Contient("QI") && !lex.Contient("MAISONX"), "dictionnaire");
        Verifier(Lettres.SacComplet().Count == 102, "sac de 102 lettres");

        // Premier coup
        var p = new Plateau();
        var poses = new List<Pose>();
        string mot = "MAISON";
        for (int i = 0; i < mot.Length; i++) poses.Add(new Pose(7, 4 + i, mot[i], false));
        var r = Arbitre.Evaluer(p, poses, lex, true);
        // H5..H10 : M A I S O N ; H8 = S est mot compte double ; pas de lettre double sur la ligne H dans cette zone
        Verifier(r.Valide && r.Points == (2 + 1 + 1 + 1 + 1 + 1) * 2, "premier coup MAISON = 14 points (" + r.Points + ")");
        var hors = new List<Pose> { new Pose(0, 0, 'L', false), new Pose(0, 1, 'A', false) };
        r = Arbitre.Evaluer(p, hors, lex, true);
        Verifier(!r.Valide && r.Erreur.Contains("H8"), "premier coup hors du centre refusé : " + r.Erreur);

        foreach (var x in poses) p.Poser(x);

        // Croisement : SOT vertical sur le S de MAISON en H8 -> G8 ? On place O en I8 et T en J8
        var croise = new List<Pose> { new Pose(8, 7, 'O', false), new Pose(9, 7, 'T', false) };
        r = Arbitre.Evaluer(p, croise, lex, false);
        Verifier(r.Valide && r.Principal.Mot == "SOT", "mot vertical SOT utilisant le S : " + (r.Valide ? r.DecrirePlacement() + " " + r.Points : r.Erreur));

        var faux = new List<Pose> { new Pose(8, 7, 'X', false), new Pose(9, 7, 'Z', false) };
        r = Arbitre.Evaluer(p, faux, lex, false);
        Verifier(!r.Valide && r.Erreur.Contains("sxz"), "mot inexistant refusé : " + r.Erreur);

        var isole = new List<Pose> { new Pose(0, 0, 'L', false), new Pose(0, 1, 'A', false) };
        r = Arbitre.Evaluer(p, isole, lex, true);
        Verifier(!r.Valide && r.Erreur.Contains("toucher"), "mot isolé refusé : " + r.Erreur);

        // Mot secondaire invalide : poser "AS" sous "MA" de MAISON en I5-I6 forme MA/A? -> "MA" vertical "MA"... on teste un cas faux
        var second = new List<Pose> { new Pose(8, 3, 'E', false), new Pose(8, 4, 'T', false) };
        r = Arbitre.Evaluer(p, second, lex, true);
        Verifier(!r.Valide && r.Erreur.Contains("formé"), "mot croisé invalide signalé : " + r.Erreur);

        // Ordinateur : coups trouvés
        var lettres = "AERSTL?".ToList();
        sw.Restart();
        var coups = new Generateur(p, lex, lettres).Generer();
        Console.WriteLine("Coups possibles avec AERSTL? : " + coups.Count + " en " + sw.ElapsedMilliseconds + " ms, meilleur : "
            + coups.OrderByDescending(c => c.Points).First().DecrirePlacement() + " " + coups.Max(c => c.Points));
        Verifier(coups.Count > 50 && coups.All(c => c.Valide), "générateur de coups");

        // Parties complètes ordinateur contre ordinateur
        foreach (Niveau n in Enum.GetValues(typeof(Niveau)))
        {
            var rnd = new Random(42 + (int)n);
            var pt = Partie.Nouvelle(n, rnd);
            int tours = 0; long maxMs = 0;
            while (!pt.Terminee && tours < 200)
            {
                var j = pt.JoueurCourant;
                sw.Restart();
                var coup = Ordinateur.Choisir(pt.Plateau, lex, j.Chevalet, n, rnd);
                maxMs = Math.Max(maxMs, sw.ElapsedMilliseconds);
                if (coup != null)
                {
                    var check = Arbitre.Evaluer(pt.Plateau, coup.Poses, lex, coup.Horizontal);
                    if (!check.Valide || check.Points != coup.Points) { Verifier(false, "coup incohérent " + coup.DecrirePlacement()); break; }
                    pt.Appliquer(coup);
                }
                else if (pt.PeutEchanger) pt.Echanger(new List<char>(j.Chevalet));
                else pt.Passer();
                tours++;
            }
            int total = pt.Plateau.NombreTuiles + pt.Sac.Count + pt.Joueurs.Sum(x => x.Chevalet.Count);
            Verifier(pt.Terminee && total == 102, "partie " + n + " terminée en " + tours + " tours, " + pt.Humain.Score + " à " + pt.Machine.Score
                + ", coup le plus long " + maxMs + " ms");
            if (n == Niveau.Expert)
            {
                Console.WriteLine("  " + string.Join("\n  ", pt.Historique.Skip(Math.Max(0, pt.Historique.Count - 4))));
                foreach (var l in pt.Plateau.Exporter()) Console.WriteLine("    " + l);
            }
        }

        // Sauvegarde et reprise
        {
            var rnd = new Random(7);
            var pt = Partie.Nouvelle(Niveau.Moyen, rnd);
            for (int i = 0; i < 6; i++)
            {
                var coup = Ordinateur.Choisir(pt.Plateau, lex, pt.JoueurCourant.Chevalet, Niveau.Moyen, rnd);
                if (coup != null) pt.Appliquer(coup); else pt.Passer();
            }
            string f = Path.Combine(Path.GetTempPath(), "scrabble_test", "partie.sav");
            pt.Sauvegarder(f);
            var pt2 = Partie.Charger(f, rnd);
            bool ok = pt2.Plateau.Exporter().SequenceEqual(pt.Plateau.Exporter()) && pt2.Humain.Score == pt.Humain.Score
                      && new string(pt2.Machine.Chevalet.ToArray()) == new string(pt.Machine.Chevalet.ToArray())
                      && pt2.Sac.Count == pt.Sac.Count && pt2.Courant == pt.Courant && pt2.Historique.Count == pt.Historique.Count;
            Verifier(ok, "sauvegarde puis reprise identiques");
        }

        Console.WriteLine(echecs == 0 ? "TOUS LES TESTS SONT PASSÉS" : echecs + " ÉCHEC(S)");
        return echecs;
    }
}
