namespace autofact.Models
{
    public enum TypeDocument
    {
        Devis,
        Facture,
        Avoir
    }

    public enum StatutDocument
    {
        Brouillon,
        Envoye,
        Accepte,
        Paye,
        Annule,
        Refuse
    }

    public class DocumentFacturation
    {
        public int            Id                   { get; set; }

        /// <summary>Numéro lisible : "FAC-2024-0001", "DEV-2024-0001", "AVO-2024-0001".</summary>
        public string         Numero               { get; set; } = string.Empty;

        public TypeDocument   Type                 { get; set; }
        public StatutDocument Statut               { get; set; } = StatutDocument.Brouillon;
        public DateTime       DateEmission         { get; set; } = DateTime.Today;
        public DateTime?      DateEcheance         { get; set; }

        /// <summary>Pour un avoir : numéro de la facture d'origine.</summary>
        public string?        NumeroDocumentParent { get; set; }

        public int            ClientId             { get; set; }
        /// <summary>Nom du client (dénormalisé pour l'affichage).</summary>
        public string         ClientNom            { get; set; } = string.Empty;

        public List<LigneDocument> Lignes { get; set; } = [];

        // ?? Totaux calculés ???????????????????????????????????????????????????
        /// <summary>
        /// Somme des montants HT de toutes les lignes.
        /// Pour les autoentrepreneurs, il n'y a pas de TVA ? TotalNet = TotalHT.
        /// </summary>
        public decimal TotalHT  => Lignes.Sum(l => l.MontantHT);
        public decimal TotalNet => TotalHT;
    }
}
