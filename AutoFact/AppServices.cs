using autofact.Data;
using autofact.ViewModels;

namespace autofact
{
    /// <summary>
    /// Point d'accès centralisé aux repositories et ViewModels.
    /// Instancier une seule fois depuis <see cref="Program"/> et passer à chaque Form.
    /// Évite de faire circuler <see cref="Bdd"/> dans tous les constructeurs.
    /// </summary>
    internal sealed class AppServices
    {
        private readonly Bdd _db;

        // ?? Repositories (singleton par session) ??????????????????????????????
        public ClientRepository    Clients    { get; }
        public PrestationRepository Prestations { get; }
        public CodePromoRepository  CodePromos  { get; }
        public DocumentRepository   Documents   { get; }
        public UrssafRepository     Urssaf      { get; }

        public AppServices(Bdd db)
        {
            _db         = db;
            Clients     = new ClientRepository(db);
            Prestations = new PrestationRepository(db);
            CodePromos  = new CodePromoRepository(db);
            Documents   = new DocumentRepository(db);
            Urssaf      = new UrssafRepository(db);
        }

        // ?? Factories de ViewModels (nouveaux à chaque appel) ?????????????????
        public ClientViewModel     NewClientVM()     => new(_db);
        public PrestationViewModel NewPrestationVM() => new(_db);
        public DocumentViewModel   NewFactureVM()    => new(_db, Models.TypeDocument.Facture);
        public DocumentViewModel   NewDevisVM()      => new(_db, Models.TypeDocument.Devis);
        public AvoirViewModel      NewAvoirVM()      => new(_db);
        public UrssafViewModel     NewUrssafVM()     => new(_db);
    }
}
