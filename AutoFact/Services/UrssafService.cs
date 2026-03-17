using autofact.Models;

namespace autofact.Services
{
    /// <summary>
    /// Calculs URSSAF pour autoentrepreneur.
    /// Taux 2024 par défaut : 21,2 % (BNC — professions libérales / prestations de services).
    /// Adapter <see cref="TauxCotisation"/> selon votre catégorie d'activité :
    ///   - BIC marchandises  : 12,3 %
    ///   - BIC services      : 21,2 %
    ///   - BNC               : 21,2 %
    /// </summary>
    public static class UrssafService
    {
        /// <summary>Taux de cotisation URSSAF applicable (modifiable au démarrage ou dans les paramètres).</summary>
        public static decimal TauxCotisation { get; set; } = 0.212m;

        // ?? CA mensuel ????????????????????????????????????????????????????????
        /// <summary>CA encaissé (factures payées) pour un mois donné.</summary>
        public static decimal CaMensuel(
            IEnumerable<DocumentFacturation> factures, int annee, int mois)
            => factures
                .Where(f => f.Type   == TypeDocument.Facture
                         && f.Statut == StatutDocument.Paye
                         && f.DateEmission.Year  == annee
                         && f.DateEmission.Month == mois)
                .Sum(f => f.TotalNet);

        // ?? CA trimestriel ????????????????????????????????????????????????????
        /// <summary>CA encaissé pour un trimestre (1 à 4) donné.</summary>
        public static decimal CaTrimestriel(
            IEnumerable<DocumentFacturation> factures, int annee, int trimestre)
        {
            int moisDebut = (trimestre - 1) * 3 + 1;
            return factures
                .Where(f => f.Type   == TypeDocument.Facture
                         && f.Statut == StatutDocument.Paye
                         && f.DateEmission.Year  == annee
                         && f.DateEmission.Month >= moisDebut
                         && f.DateEmission.Month <  moisDebut + 3)
                .Sum(f => f.TotalNet);
        }

        // ?? CA annuel ????????????????????????????????????????????????????????
        /// <summary>CA encaissé sur toute une année.</summary>
        public static decimal CaAnnuel(
            IEnumerable<DocumentFacturation> factures, int annee)
            => factures
                .Where(f => f.Type   == TypeDocument.Facture
                         && f.Statut == StatutDocument.Paye
                         && f.DateEmission.Year == annee)
                .Sum(f => f.TotalNet);

        // ?? CA par client et par année ????????????????????????????????????????
        /// <summary>Classement des clients par CA décroissant pour une année donnée.</summary>
        public static IEnumerable<(int ClientId, string ClientNom, decimal Ca)>
            CaParClient(IEnumerable<DocumentFacturation> factures, int annee)
            => factures
                .Where(f => f.Type   == TypeDocument.Facture
                         && f.Statut == StatutDocument.Paye
                         && f.DateEmission.Year == annee)
                .GroupBy(f => new { f.ClientId, f.ClientNom })
                .Select(g => (g.Key.ClientId, g.Key.ClientNom, Ca: g.Sum(f => f.TotalNet)))
                .OrderByDescending(x => x.Ca);

        // ?? Calculs de cotisation ?????????????????????????????????????????????
        /// <summary>Montant des cotisations URSSAF dues sur un CA donné.</summary>
        public static decimal Cotisation(decimal ca)
            => Math.Round(ca * TauxCotisation, 2);

        /// <summary>Ce que l'autoentrepreneur touche réellement après prélèvement URSSAF.</summary>
        public static decimal NetApresUrssaf(decimal ca)
            => Math.Round(ca * (1 - TauxCotisation), 2);

        // ?? Résumé trimestriel complet ????????????????????????????????????????
        /// <summary>Retourne un résumé structuré pour la déclaration trimestrielle.</summary>
        public static DeclarationTrimestrielle ResumeTrimestriel(
            IEnumerable<DocumentFacturation> factures, int annee, int trimestre)
        {
            decimal ca = CaTrimestriel(factures, annee, trimestre);
            return new DeclarationTrimestrielle
            {
                Annee       = annee,
                Trimestre   = trimestre,
                CaBrut      = ca,
                Cotisation  = Cotisation(ca),
                NetPercu    = NetApresUrssaf(ca)
            };
        }
    }

    /// <summary>DTO de résumé pour une déclaration trimestrielle URSSAF.</summary>
    public sealed class DeclarationTrimestrielle
    {
        public int     Annee      { get; init; }
        public int     Trimestre  { get; init; }
        public decimal CaBrut     { get; init; }
        public decimal Cotisation { get; init; }
        public decimal NetPercu   { get; init; }

        public string PeriodeLabel =>
            $"T{Trimestre} {Annee}";

        public override string ToString() =>
            $"{PeriodeLabel} — CA : {CaBrut:C2} | Cotisations : {Cotisation:C2} | Net : {NetPercu:C2}";
    }
}
