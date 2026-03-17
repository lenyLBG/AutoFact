using autofact.Data;
using autofact.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace autofact.ViewModels
{
    /// <summary>
    /// ViewModel pour l'édition d'un client existant.
    /// </summary>
    internal sealed class ClientEditViewModel : INotifyPropertyChanged
    {
        private readonly ClientRepository _repo;
        private readonly int _clientId;

        // ?? Champs liés ????????????????????????????????????????????????????????
        private string _nom       = string.Empty;
        private string _email     = string.Empty;
        private string _telephone = string.Empty;
        private string _adresse   = string.Empty;
        private string _erreur    = string.Empty;
        private bool   _isBusy;

        public ClientEditViewModel(Bdd db, int clientId)
        {
            _repo      = new ClientRepository(db);
            _clientId  = clientId;
        }

        // ?? Propriétés bindables ??????????????????????????????????????????????
        public string Nom
        {
            get => _nom;
            set { _nom = value; OnPropertyChanged(); Validate(); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public string Telephone
        {
            get => _telephone;
            set { _telephone = value; OnPropertyChanged(); }
        }

        public string Adresse
        {
            get => _adresse;
            set { _adresse = value; OnPropertyChanged(); }
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

        public bool PeutSauvegarder => !string.IsNullOrWhiteSpace(Nom) && !IsBusy;

        // ?? Sauvegarder ???????????????????????????????????????????????????????
        /// <summary>
        /// Met à jour le client en base via le repository.
        /// Retourne true en cas de succès, false si validation ou erreur BDD.
        /// </summary>
        public async Task<bool> SauvegarderAsync()
        {
            Validate();
            if (!PeutSauvegarder) return false;

            IsBusy = true;
            Erreur = string.Empty;
            try
            {
                var client = new Client
                {
                    Id        = _clientId,
                    Nom       = Nom.Trim(),
                    Email     = Email.Trim(),
                    Telephone = Telephone.Trim(),
                    Adresse   = Adresse.Trim()
                };

                await _repo.ModifierAsync(client);
                return true;
            }
            catch (Exception ex)
            {
                Erreur = $"Erreur lors de l'enregistrement : {ex.Message}";
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ?? Helpers ???????????????????????????????????????????????????????????
        private void Validate() =>
            Erreur = string.IsNullOrWhiteSpace(Nom) ? "Le nom est obligatoire." : string.Empty;

        // ?? INotifyPropertyChanged ????????????????????????????????????????????
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            if (name is nameof(Nom) or nameof(IsBusy))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PeutSauvegarder)));
        }
    }
}
