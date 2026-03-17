using autofact.Data;
using autofact.Models;
using MySqlConnector;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace autofact.ViewModels
{
    /// <summary>
    /// ViewModel pour la création / édition d'une prestation du catalogue.
    /// </summary>
    internal sealed class PrestationViewModel : INotifyPropertyChanged
    {
        private readonly Bdd                  _db;
        private readonly PrestationRepository _repo;

        private int     _id;
        private string  _nom          = string.Empty;
        private string  _type         = string.Empty;
        private string  _description  = string.Empty;
        private decimal _prixUnitaire;
        private string  _erreur       = string.Empty;
        private bool    _isBusy;

        public PrestationViewModel(Bdd db)
        {
            _db   = db;
            _repo = new PrestationRepository(db);
        }

        // ?? Mode édition : charge une prestation existante ????????????????????
        public void Charger(Prestation p)
        {
            _id          = p.Id;
            Nom          = p.Nom;
            Type         = p.Type;
            Description  = p.Description;
            PrixUnitaire = p.PrixUnitaire;
        }

        // ?? Propriétés bindables ??????????????????????????????????????????????
        public string Nom
        {
            get => _nom;
            set { _nom = value; OnPropertyChanged(); Validate(); }
        }

        public string Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public decimal PrixUnitaire
        {
            get => _prixUnitaire;
            set { _prixUnitaire = value; OnPropertyChanged(); Validate(); }
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

        public bool PeutSauvegarder => !string.IsNullOrWhiteSpace(Nom) && PrixUnitaire >= 0 && !IsBusy;

        // ?? Sauvegarder (création ou mise à jour) ?????????????????????????????
        public async Task<bool> SauvegarderAsync()
        {
            Validate();
            if (!PeutSauvegarder) return false;

            IsBusy = true;
            Erreur = string.Empty;
            try
            {
                var p = new Prestation
                {
                    Id           = _id,
                    Nom          = Nom.Trim(),
                    Type         = Type.Trim(),
                    Description  = Description.Trim(),
                    PrixUnitaire = PrixUnitaire
                };

                if (_id == 0)
                    _id = await _repo.CreerAsync(p);
                else
                    await _repo.ModifierAsync(p);

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

        public async Task<bool> SupprimerAsync(int id)
        {
            IsBusy = true;
            Erreur = string.Empty;
            try
            {
                await _repo.SupprimerAsync(id);
                return true;
            }
            catch (Exception ex)
            {
                Erreur = $"Impossible de supprimer : {ex.Message}";
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<List<Prestation>> ChargerPrestationsAsync(string? recherche = null)
        {
            Erreur = string.Empty;
            try
            {
                return await _repo.ToutesAsync(recherche);
            }
            catch (Exception ex)
            {
                Erreur = $"Erreur chargement : {ex.Message}";
                return [];
            }
        }

        // ?? Helpers ???????????????????????????????????????????????????????????
        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(Nom))       { Erreur = "Le nom est obligatoire.";           return; }
            if (PrixUnitaire < 0)                     { Erreur = "Le prix ne peut pas être négatif."; return; }
            Erreur = string.Empty;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            if (name is nameof(Nom) or nameof(PrixUnitaire) or nameof(IsBusy))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PeutSauvegarder)));
        }
    }
}
