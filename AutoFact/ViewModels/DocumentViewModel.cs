using autofact.Data;
using autofact.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace autofact.ViewModels
{
    /// <summary>
    /// ViewModel pour la création / édition d'un devis ou d'une facture.
    /// Gère les lignes, les codes promo, la numérotation et la persistance.
    /// </summary>
    internal sealed class DocumentViewModel : INotifyPropertyChanged
    {
        private readonly DocumentRepository _repo;

        // ?? État du document ??????????????????????????????????????????????????
        private int           _id;
        private string        _numero        = string.Empty;
        private TypeDocument  _type          = TypeDocument.Facture;
        private StatutDocument _statut       = StatutDocument.Brouillon;
        private DateTime      _dateEmission  = DateTime.Today;
        private DateTime?     _dateEcheance;
        private int           _clientId;
        private string        _clientNom     = string.Empty;
        private string        _erreur        = string.Empty;
        private bool          _isBusy;

        public DocumentViewModel(Bdd db, TypeDocument type = TypeDocument.Facture)
        {
            _repo = new DocumentRepository(db);
            _type = type;
        }

        // ?? Propriétés bindables ??????????????????????????????????????????????
        public int Id => _id;

        public string Numero
        {
            get => _numero;
            private set { _numero = value; OnPropertyChanged(); }
        }

        public TypeDocument Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); }
        }

        public StatutDocument Statut
        {
            get => _statut;
            set { _statut = value; OnPropertyChanged(); }
        }

        public DateTime DateEmission
        {
            get => _dateEmission;
            set { _dateEmission = value; OnPropertyChanged(); }
        }

        public DateTime? DateEcheance
        {
            get => _dateEcheance;
            set { _dateEcheance = value; OnPropertyChanged(); }
        }

        public int ClientId
        {
            get => _clientId;
            set { _clientId = value; OnPropertyChanged(); Validate(); }
        }

        public string ClientNom
        {
            get => _clientNom;
            set { _clientNom = value; OnPropertyChanged(); }
        }

        public string Erreur
        {
            get => _erreur;
            private set { _erreur = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set { _isBusy = value; OnPropertyChanged(); }
        }

        // ?? Lignes ????????????????????????????????????????????????????????????
        public List<LigneDocument> Lignes { get; } = [];

        /// <summary>Ajoute une ligne initialisée depuis une prestation.</summary>
        public void AjouterLigne(Prestation prestation, int quantite = 1)
        {
            Lignes.Add(new LigneDocument
            {
                PrestationId = prestation.Id,
                Designation  = prestation.Nom,
                PrixUnitaire = prestation.PrixUnitaire,
                Quantite     = quantite
            });
            OnPropertyChanged(nameof(TotalHT));
        }

        public void SupprimerLigne(LigneDocument ligne)
        {
            Lignes.Remove(ligne);
            OnPropertyChanged(nameof(TotalHT));
        }

        /// <summary>
        /// Applique un code promo à une ligne.
        /// Valide le code en base avant application.
        /// </summary>
        public async Task<bool> AppliquerCodePromoAsync(LigneDocument ligne, string code)
        {
            var promo = await _repo.TrouverCodePromoAsync(code);
            if (promo is null)
            {
                Erreur = $"Code promo « {code} » invalide ou expiré.";
                return false;
            }
            ligne.CodePromoId   = promo.Id;
            ligne.CodePromoCode = promo.Code;
            ligne.TauxRemise    = promo.TauxRemise;
            OnPropertyChanged(nameof(TotalHT));
            return true;
        }

        // ?? Totaux ????????????????????????????????????????????????????????????
        public decimal TotalHT  => Lignes.Sum(l => l.MontantHT);
        public decimal TotalNet => TotalHT;

        public bool PeutSauvegarder => ClientId > 0 && Lignes.Count > 0 && !IsBusy;

        // ?? Initialiser un nouveau numéro ?????????????????????????????????????
        public async Task InitialiserNumeroAsync()
        {
            Numero = await _repo.ProchainNumeroAsync(_type);
        }

        // ?? Charger un document existant ??????????????????????????????????????
        public async Task ChargerAsync(int documentId)
        {
            IsBusy = true;
            try
            {
                var docs = await _repo.ChargerDocumentsAsync();
                var doc  = docs.FirstOrDefault(d => d.Id == documentId)
                           ?? throw new InvalidOperationException("Document introuvable.");

                _id          = doc.Id;
                Numero       = doc.Numero;
                Type         = doc.Type;
                Statut       = doc.Statut;
                DateEmission = doc.DateEmission;
                DateEcheance = doc.DateEcheance;
                ClientId     = doc.ClientId;
                ClientNom    = doc.ClientNom;

                Lignes.Clear();
                var lignes = await _repo.ChargerLignesAsync(documentId);
                Lignes.AddRange(lignes);
                OnPropertyChanged(nameof(TotalHT));
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ?? Sauvegarder ???????????????????????????????????????????????????????
        public async Task<bool> SauvegarderAsync()
        {
            Validate();
            if (!PeutSauvegarder) return false;

            if (string.IsNullOrWhiteSpace(Numero))
                await InitialiserNumeroAsync();

            IsBusy = true;
            Erreur = string.Empty;
            try
            {
                var doc = new DocumentFacturation
                {
                    Id           = _id,
                    Numero       = Numero,
                    Type         = Type,
                    Statut       = Statut,
                    DateEmission = DateEmission,
                    DateEcheance = DateEcheance,
                    ClientId     = ClientId,
                    ClientNom    = ClientNom,
                    Lignes       = [.. Lignes]
                };

                int newId = await _repo.CreerDocumentAsync(doc);
                _id = newId;
                return true;
            }
            catch (Exception ex)
            {
                Erreur = $"Erreur sauvegarde : {ex.Message}";
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ?? Changer le statut ?????????????????????????????????????????????????
        public async Task<bool> ChangerStatutAsync(StatutDocument nouveauStatut)
        {
            if (_id == 0) { Erreur = "Document non encore sauvegardé."; return false; }

            IsBusy = true;
            Erreur = string.Empty;
            try
            {
                await _repo.MettreAJourStatutAsync(_id, nouveauStatut);
                Statut = nouveauStatut;
                return true;
            }
            catch (Exception ex)
            {
                Erreur = $"Erreur : {ex.Message}";
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ?? Helpers ???????????????????????????????????????????????????????????
        private void Validate()
        {
            if (ClientId <= 0)         { Erreur = "Veuillez sélectionner un client.";   return; }
            if (Lignes.Count == 0)     { Erreur = "Ajoutez au moins une ligne.";         return; }
            Erreur = string.Empty;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            if (name is nameof(ClientId) or nameof(IsBusy))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PeutSauvegarder)));
        }
    }
}
