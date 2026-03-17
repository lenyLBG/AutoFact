namespace autofact.Models
{
    public class LigneDocument
    {
        public int     Id            { get; set; }
        public int     DocumentId    { get; set; }
        public int     PrestationId  { get; set; }

        /// <summary>Libellé saisi ou copié depuis la prestation.</summary>
        public string  Designation   { get; set; } = string.Empty;
        public int     Quantite      { get; set; } = 1;
        public decimal PrixUnitaire  { get; set; }

        // ?? Code promo ????????????????????????????????????????????????????????
        /// <summary>Id du code promo appliqué (null = aucun).</summary>
        public int?    CodePromoId   { get; set; }
        /// <summary>Code promo en clair (lecture seule, renseigné au chargement).</summary>
        public string? CodePromoCode { get; set; }

        /// <summary>Taux de remise effectif en % (0-100), issu du code promo ou saisi manuellement.</summary>
        public decimal TauxRemise    { get; set; }

        // ?? Calcul ????????????????????????????????????????????????????????????
        /// <summary>Montant HT après remise, arrondi à 2 décimales.</summary>
        public decimal MontantHT =>
            Math.Round(PrixUnitaire * Quantite * (1 - TauxRemise / 100m), 2);
    }
}
