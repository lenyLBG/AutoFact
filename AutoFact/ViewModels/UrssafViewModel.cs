using autofact.Data;
using autofact.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace autofact.ViewModels
{
    /// <summary>
    /// ViewModel pour le tableau de bord URSSAF.
    /// Expose le CA mensuel, trimestriel, annuel, les cotisations et le net perçu.
    /// </summary>
    internal sealed class UrssafViewModel : INotifyPropertyChanged
    {
        private readonly UrssafRepository _repo;

        private int      _anneeSelectionnee = DateTime.Today.Year;
        private decimal  _caAnnuel;
        private decimal  _caMoisCourant;
        private decimal  _cotisationAnnuelle;
        private decimal  _netAnnuel;
        private string   _erreur   = string.Empty;
        private bool     _isBusy;

        public UrssafViewModel(Bdd db) => _repo = new UrssafRepository(db);

        // ?? Propriétés bindables ??????????????????????????????????????????????
        public int AnneeSelectionnee
        {
            get => _anneeSelectionnee;
            set { _anneeSelectionnee = value; OnPropertyChanged(); }
        }

        public decimal CaAnnuel
        {
            get => _caAnnuel;
            private set { _caAnnuel = value; OnPropertyChanged(); }
        }

        public decimal CaMoisCourant
        {
            get => _caMoisCourant;
            private set { _caMoisCourant = value; OnPropertyChanged(); }
        }

        public decimal CotisationAnnuelle
        {
            get => _cotisationAnnuelle;
            private set { _cotisationAnnuelle = value; OnPropertyChanged(); }
        }

        public decimal NetAnnuel
        {
            get => _netAnnuel;
            private set { _netAnnuel = value; OnPropertyChanged(); }
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

        /// <summary>Déclarations des 4 trimestres de l'année sélectionnée.</summary>
        public DeclarationTrimestrielle[] Trimestres { get; private set; } = [];

        /// <summary>CA par client pour l'année sélectionnée.</summary>
        public List<(int ClientId, string ClientNom, decimal Ca)> CaParClient { get; private set; } = [];

        /// <summary>CA mensuel (index 0 = janvier, 11 = décembre).</summary>
        public decimal[] CaParMois { get; private set; } = new decimal[12];

        // ?? Chargement ????????????????????????????????????????????????????????
        public async Task ChargerAsync()
        {
            IsBusy = true;
            Erreur = string.Empty;
            try
            {
                var resume = await _repo.ResumeDashboardAsync();

                CaAnnuel           = resume.CaAnnuel;
                CaMoisCourant      = resume.CaMoisCourant;
                CotisationAnnuelle = resume.CotisationAnnuelle;
                NetAnnuel          = resume.NetAnnuelApresUrssaf;
                Trimestres         = await _repo.TousTrimestreAsync(_anneeSelectionnee);
                CaParClient        = resume.CaParClient.ToList();
                CaParMois          = await _repo.CaParMoisAsync(_anneeSelectionnee);

                OnPropertyChanged(nameof(Trimestres));
                OnPropertyChanged(nameof(CaParClient));
                OnPropertyChanged(nameof(CaParMois));
            }
            catch (Exception ex)
            {
                Erreur = $"Erreur chargement URSSAF : {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Recharge les données pour une autre année.</summary>
        public async Task ChangerAnneeAsync(int annee)
        {
            AnneeSelectionnee = annee;
            await ChargerAsync();
        }

        // ?? Taux URSSAF configurable ??????????????????????????????????????????
        public decimal TauxCotisation
        {
            get => UrssafService.TauxCotisation * 100;
            set
            {
                UrssafService.TauxCotisation = value / 100;
                OnPropertyChanged();
            }
        }

        // ?? INotifyPropertyChanged ????????????????????????????????????????????
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
