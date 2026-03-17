using autofact.Data;
using autofact.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace autofact.ViewModels
{
    /// <summary>
    /// ViewModel pour la création / édition d'un client.
    /// Implémente <see cref="INotifyPropertyChanged"/> pour le binding WinForms.
    /// </summary>
    internal sealed class ClientViewModel : INotifyPropertyChanged
    {
        private readonly ClientRepository _repo;

        // ?? Champs liés ????????????????????????????????????????????????????????
        private string _nom       = string.Empty;
        private string _email     = string.Empty;
        private string _telephone = string.Empty;
        private string _adresse   = string.Empty;
        private string _erreur    = string.Empty;
        private bool   _isBusy;

        public ClientViewModel(Bdd db)
        {
            _repo = new ClientRepository(db);
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
        /// Insère le client en base via le repository.
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
                    Nom       = Nom.Trim(),
                    Email     = Email.Trim(),
                    Telephone = Telephone.Trim(),
                    Adresse   = Adresse.Trim()
                };

                int newId = await _repo.CreerAsync(client);
                ResetChamps();
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

        // ?? Charger tous les clients ????????????????????????????????????????
        /// <summary>Retourne la liste des clients depuis le repository.</summary>
        public async Task<List<Client>> ChargerClientsAsync()
        {
            Erreur = string.Empty;
            try
            {
                return await _repo.TousAsync();
            }
            catch (Exception ex)
            {
                Erreur = $"Erreur chargement clients : {ex.Message}";
                return [];
            }
        }

        // ?? Helpers ???????????????????????????????????????????????????????????
        private void Validate() =>
            Erreur = string.IsNullOrWhiteSpace(Nom) ? "Le nom est obligatoire." : string.Empty;

        private void ResetChamps()
        {
            Nom       = string.Empty;
            Email     = string.Empty;
            Telephone = string.Empty;
            Adresse   = string.Empty;
        }

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
