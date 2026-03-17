using autofact.Data;
using autofact.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace autofact.ViewModels
{
    /// <summary>
    /// ViewModel pour la création d'un avoir à partir d'une facture existante.
    /// Un avoir annule totalement ou partiellement une facture payée.
    /// </summary>
    internal sealed class AvoirViewModel : INotifyPropertyChanged
    {
        private readonly DocumentRepository _repo;

        private int    _factureSourceId;
        private string _factureSourceNumero  = string.Empty;
        private string _motif                = string.Empty;
        private string _erreur               = string.Empty;
        private bool   _isBusy;

        public AvoirViewModel(Bdd db) => _repo = new DocumentRepository(db);

        // ?? Propriétés bindables ??????????????????????????????????????????????
        public string FactureSourceNumero
        {
            get => _factureSourceNumero;
            private set { _factureSourceNumero = value; OnPropertyChanged(); }
        }

        public string Motif
        {
            get => _motif;
            set { _motif = value; OnPropertyChanged(); }
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

        /// <summary>Document d'avoir en cours de construction (lignes éditables).</summary>
        public DocumentFacturation? Avoir { get; private set; }

        public bool PeutCreer => Avoir?.Lignes.Count > 0 && !IsBusy;

        // ?? Initialiser depuis une facture ????????????????????????????????????
        /// <summary>
        /// Prépare l'avoir en copiant toutes les lignes de la facture source.
        /// L'appelant peut ensuite retirer des lignes avant de confirmer.
        /// </summary>
        public async Task<bool> InitialiserDepuisFactureAsync(int factureId)
        {
            IsBusy = true;
            Erreur = string.Empty;
            try
            {
                var docs = await _repo.ChargerDocumentsAsync(filtre: TypeDocument.Facture);
                var source = docs.FirstOrDefault(d => d.Id == factureId);
                if (source is null)
                {
                    Erreur = "Facture source introuvable.";
                    return false;
                }
                if (source.Statut != StatutDocument.Paye)
                {
                    Erreur = "Seules les factures payées peuvent faire l'objet d'un avoir.";
                    return false;
                }

                _factureSourceId   = factureId;
                FactureSourceNumero = source.Numero;

                var lignes = await _repo.ChargerLignesAsync(factureId);
                // Copie des lignes (nouveaux objets sans id)
                var lignesCopie = lignes.Select(l => new LigneDocument
                {
                    PrestationId  = l.PrestationId,
                    Designation   = l.Designation,
                    PrixUnitaire  = l.PrixUnitaire,
                    Quantite      = l.Quantite,
                    TauxRemise    = l.TauxRemise,
                    CodePromoId   = l.CodePromoId,
                    CodePromoCode = l.CodePromoCode
                }).ToList();

                string numeroAvoir = await _repo.ProchainNumeroAsync(TypeDocument.Avoir);

                Avoir = new DocumentFacturation
                {
                    Numero               = numeroAvoir,
                    Type                 = TypeDocument.Avoir,
                    Statut               = StatutDocument.Brouillon,
                    DateEmission         = DateTime.Today,
                    ClientId             = source.ClientId,
                    ClientNom            = source.ClientNom,
                    NumeroDocumentParent = source.Numero,
                    Lignes               = lignesCopie
                };

                OnPropertyChanged(nameof(Avoir));
                OnPropertyChanged(nameof(PeutCreer));
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

        // ?? Confirmer l'avoir ?????????????????????????????????????????????????
        public async Task<bool> ConfirmerAsync()
        {
            if (Avoir is null || !PeutCreer) return false;

            IsBusy = true;
            Erreur = string.Empty;
            try
            {
                int id = await _repo.CreerDocumentAsync(Avoir);
                Avoir.Id = id;

                // Marquer la facture source comme annulée
                await _repo.MettreAJourStatutAsync(_factureSourceId, StatutDocument.Annule);

                OnPropertyChanged(nameof(PeutCreer));
                return true;
            }
            catch (Exception ex)
            {
                Erreur = $"Erreur création avoir : {ex.Message}";
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ?? INotifyPropertyChanged ????????????????????????????????????????????
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            if (name is nameof(IsBusy))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PeutCreer)));
        }
    }
}
