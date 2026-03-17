namespace autofact.Models
{
    public class CodePromo
    {
        public int       Id             { get; set; }
        public string    Code           { get; set; } = string.Empty;
        /// <summary>Taux de remise en pourcentage (ex : 10 = 10 %).</summary>
        public decimal   TauxRemise     { get; set; }
        public DateTime? DateExpiration { get; set; }
        public bool      Actif          { get; set; } = true;
    }
}
