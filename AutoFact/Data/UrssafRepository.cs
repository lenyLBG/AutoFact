using autofact.Models;
using autofact.Services;

namespace autofact.Data
{
    /// <summary>
    /// Charge les factures payées depuis la base et délègue les calculs à <see cref="UrssafService"/>.
    /// Point d'entrée unique pour tous les tableaux de bord URSSAF / statistiques.
    /// </summary>
    internal class UrssafRepository(Bdd db)
    {
        private readonly DocumentRepository _docRepo = new(db);

        // ?? Données brutes ????????????????????????????????????????????????????

        /// <summary>
        /// Charge toutes les factures payées d'une année depuis la DB,
        /// avec leurs lignes (nécessaires pour TotalHT).
        /// </summary>
        public async Task<List<DocumentFacturation>> FacturesPayeesAsync(int annee)
        {
            var factures = await _docRepo.ChargerDocumentsAsync(
                filtre: TypeDocument.Facture, annee: annee);

            // Filtrer côté C# pour les payées, puis charger les lignes
            var payees = factures
                .Where(f => f.Statut == StatutDocument.Paye)
                .ToList();

            foreach (var f in payees)
                f.Lignes = await _docRepo.ChargerLignesAsync(f.Id);

            return payees;
        }

        // ?? CA mensuel ????????????????????????????????????????????????????????

        public async Task<decimal> CaMensuelAsync(int annee, int mois)
        {
            var factures = await FacturesPayeesAsync(annee);
            return UrssafService.CaMensuel(factures, annee, mois);
        }

        /// <summary>Retourne le CA de chaque mois de l'année (index 0 = janvier).</summary>
        public async Task<decimal[]> CaParMoisAsync(int annee)
        {
            var factures = await FacturesPayeesAsync(annee);
            var result   = new decimal[12];
            for (int m = 1; m <= 12; m++)
                result[m - 1] = UrssafService.CaMensuel(factures, annee, m);
            return result;
        }

        // ?? CA trimestriel ????????????????????????????????????????????????????

        public async Task<decimal> CaTrimestrielAsync(int annee, int trimestre)
        {
            var factures = await FacturesPayeesAsync(annee);
            return UrssafService.CaTrimestriel(factures, annee, trimestre);
        }

        /// <summary>Retourne les 4 déclarations trimestrielles d'une année.</summary>
        public async Task<DeclarationTrimestrielle[]> TousTrimestreAsync(int annee)
        {
            var factures = await FacturesPayeesAsync(annee);
            return Enumerable.Range(1, 4)
                .Select(t => UrssafService.ResumeTrimestriel(factures, annee, t))
                .ToArray();
        }

        // ?? CA annuel ?????????????????????????????????????????????????????????

        public async Task<decimal> CaAnnuelAsync(int annee)
        {
            var factures = await FacturesPayeesAsync(annee);
            return UrssafService.CaAnnuel(factures, annee);
        }

        // ?? CA par client ?????????????????????????????????????????????????????

        public async Task<IEnumerable<(int ClientId, string ClientNom, decimal Ca)>>
            CaParClientAsync(int annee)
        {
            var factures = await FacturesPayeesAsync(annee);
            return UrssafService.CaParClient(factures, annee);
        }

        // ?? Résumé dashboard ??????????????????????????????????????????????????

        /// <summary>
        /// Résumé complet pour le tableau de bord :
        /// CA annuel, CA mensuel courant, déclaration du trimestre courant.
        /// </summary>
        public async Task<ResumeDashboard> ResumeDashboardAsync()
        {
            int annee     = DateTime.Today.Year;
            int mois      = DateTime.Today.Month;
            int trimestre = (mois - 1) / 3 + 1;

            var factures  = await FacturesPayeesAsync(annee);

            decimal caAnnuel      = UrssafService.CaAnnuel(factures, annee);
            decimal caMois        = UrssafService.CaMensuel(factures, annee, mois);
            var     declaration   = UrssafService.ResumeTrimestriel(factures, annee, trimestre);
            var     parClient     = UrssafService.CaParClient(factures, annee).ToList();

            return new ResumeDashboard
            {
                Annee          = annee,
                CaAnnuel       = caAnnuel,
                CaMoisCourant  = caMois,
                Declaration    = declaration,
                CaParClient    = parClient
            };
        }
    }

    /// <summary>DTO agrégé pour le tableau de bord principal.</summary>
    internal sealed class ResumeDashboard
    {
        public int                                             Annee         { get; init; }
        public decimal                                         CaAnnuel      { get; init; }
        public decimal                                         CaMoisCourant { get; init; }
        public DeclarationTrimestrielle                        Declaration   { get; init; } = null!;
        public List<(int ClientId, string ClientNom, decimal Ca)> CaParClient { get; init; } = [];

        public decimal CotisationAnnuelle   => UrssafService.Cotisation(CaAnnuel);
        public decimal NetAnnuelApresUrssaf => UrssafService.NetApresUrssaf(CaAnnuel);
    }
}
