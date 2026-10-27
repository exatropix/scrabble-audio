using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace ScrabbleAudio.Core
{
    /// <summary>
    /// Recherche d'une nouvelle version publiée sur GitHub (page « Releases » du dépôt).
    /// Le jeu ne télécharge rien lui-même : il prévient le joueur et propose d'ouvrir la page de téléchargement.
    /// </summary>
    public static class MiseAJour
    {
        public const string Depot = "exatropix/scrabble-audio";            // compte/dépôt sur GitHub
        public const string VersionActuelle = "2.0";
        public static string PageDepot { get { return "https://github.com/" + Depot + "/releases/latest"; } }

        public class Resultat
        {
            public bool Reussi;             // la recherche a pu aboutir
            public bool NouvelleVersion;
            public string Version;          // numéro publié, par exemple 4.1
            public string Page;             // page de téléchargement
            public string Erreur;
        }

        /// <summary>Lance la recherche en arrière-plan ; le rappel reçoit le résultat (sur un autre fil d'exécution).</summary>
        public static void Rechercher(Action<Resultat> rappel)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Resultat r;
                try { r = RechercherMaintenant(); }
                catch (Exception ex) { r = new Resultat { Reussi = false, Erreur = ex.Message }; }
                rappel(r);
            });
        }

        static Resultat RechercherMaintenant()
        {
            // GitHub exige TLS 1.2, que .NET Framework n'active pas toujours de lui-même.
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
            try { return ParLaPage(); }
            catch { return ParLApi(); }
        }

        /// <summary>
        /// La page « latest » de GitHub renvoie vers la dernière version : …/releases/tag/v4.1.
        /// Cette méthode n'est pas soumise à la limite de requêtes de l'API.
        /// </summary>
        static Resultat ParLaPage()
        {
            var req = (HttpWebRequest)WebRequest.Create(PageDepot);
            req.UserAgent = "ScrabbleAudio/" + VersionActuelle;
            req.Method = "HEAD";
            req.AllowAutoRedirect = false;
            req.Timeout = 10000;
            using (var rep = (HttpWebResponse)req.GetResponse())
            {
                string cible = rep.Headers["Location"] ?? "";
                var m = Regex.Match(cible, "/releases/tag/([^/?#]+)");
                if (!m.Success)
                {
                    // Pas encore de version publiée : GitHub renvoie vers la liste des versions.
                    if ((int)rep.StatusCode >= 300 && (int)rep.StatusCode < 400) return new Resultat { Reussi = true, NouvelleVersion = false, Version = VersionActuelle };
                    throw new WebException("Réponse inattendue");
                }
                string publiee = Nettoyer(Uri.UnescapeDataString(m.Groups[1].Value));
                return new Resultat { Reussi = true, Version = publiee, Page = cible.StartsWith("http") ? cible : "https://github.com" + cible, NouvelleVersion = PlusRecente(publiee, VersionActuelle) };
            }
        }

        static Resultat ParLApi()
        {
            var req = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/" + Depot + "/releases/latest");
            req.UserAgent = "ScrabbleAudio/" + VersionActuelle;
            req.Accept = "application/vnd.github+json";
            req.Timeout = 10000;
            string json;
            try
            {
                using (var rep = (HttpWebResponse)req.GetResponse())
                using (var lecteur = new StreamReader(rep.GetResponseStream()))
                    json = lecteur.ReadToEnd();
            }
            catch (WebException ex)
            {
                var rep = ex.Response as HttpWebResponse;
                // Pas encore de version publiée : ce n'est pas une erreur
                if (rep != null && rep.StatusCode == HttpStatusCode.NotFound) return new Resultat { Reussi = true, NouvelleVersion = false, Version = VersionActuelle };
                throw;
            }
            var tag = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]*)\"").Groups[1].Value;
            var page = Regex.Match(json, "\"html_url\"\\s*:\\s*\"([^\"]*/releases/[^\"]*)\"").Groups[1].Value;
            string publiee = Nettoyer(tag);
            return new Resultat
            {
                Reussi = true,
                Version = publiee,
                Page = string.IsNullOrEmpty(page) ? PageDepot : page,
                NouvelleVersion = PlusRecente(publiee, VersionActuelle)
            };
        }

        /// <summary>« v4.1 », « Version 4.1 » ou « 4.1.0 » deviennent « 4.1 ».</summary>
        static string Nettoyer(string tag)
        {
            var m = Regex.Match(tag ?? "", @"\d+(\.\d+){0,3}");
            return m.Success ? m.Value : "";
        }

        public static bool PlusRecente(string publiee, string actuelle)
        {
            Version a, b;
            if (!Version.TryParse(Completer(publiee), out a) || !Version.TryParse(Completer(actuelle), out b)) return false;
            return a > b;
        }

        static string Completer(string v) { return v != null && v.IndexOf('.') < 0 ? v + ".0" : v; }

        public static void OuvrirPage(string page)
        {
            try { System.Diagnostics.Process.Start(string.IsNullOrEmpty(page) ? PageDepot : page); } catch { }
        }
    }
}
