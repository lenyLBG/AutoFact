using autofact.Models;

namespace autofact.Services
{
    /// <summary>
    /// Génère des numéros de documents séquentiels et horodatés.
    /// Format : DEV-2024-0001 / FAC-2024-0001 / AVO-2024-0001
    /// Le <paramref name="dernierIndex"/> est le dernier index utilisé dans l'année.
    /// </summary>
    public static class NumerotationService
    {
        public static string Generer(TypeDocument type, int annee, int dernierIndex)
        {
            string prefixe = type switch
            {
                TypeDocument.Devis   => "DEV",
                TypeDocument.Facture => "FAC",
                TypeDocument.Avoir   => "AVO",
                _                    => "DOC"
            };
            return $"{prefixe}-{annee}-{(dernierIndex + 1):D4}";
        }
    }
}
