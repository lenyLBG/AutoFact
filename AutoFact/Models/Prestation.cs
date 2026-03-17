namespace autofact.Models
{
    public class Prestation
    {
        public int     Id           { get; set; }
        public string  Nom          { get; set; } = string.Empty;
        /// <summary>"Produit" ou "Service".</summary>
        public string  Type         { get; set; } = string.Empty;
        public string  Description  { get; set; } = string.Empty;
        public decimal PrixUnitaire { get; set; }
    }
}
