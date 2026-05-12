using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using autofact.Models;
using autofact.ViewModels;

namespace autofact
{
    /// <summary>Formulaire d'affichage des détails d'une facture ou devis.</summary>
    internal partial class FormDocumentDetail : Form
    {
        // ── Services et données ────────────────────────────────────────────
        private readonly Bdd _db;
        private readonly AppServices _services;
        private readonly int _documentId;
        private DocumentViewModel? _viewModel;

        // ── UI Controls ────────────────────────────────────────────────────
        private Label lblNumero = null!;
        private Label lblClient = null!;
        private Label lblDateEmission = null!;
        private Label lblDateEcheance = null!;
        private Label lblStatut = null!;
        private ListView lvLignes = null!;
        private Label lblTotalValue = null!;

        // ══════════════════════════════════════════════════════════════════════
        // CONSTRUCTEUR
        // ══════════════════════════════════════════════════════════════════════

        public FormDocumentDetail(Bdd db, int documentId)
        {
            InitializeComponent();

            _db = db ?? throw new ArgumentNullException(nameof(db));
            _services = new AppServices(_db);
            _documentId = documentId;

            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.AllPaintingInWmPaint, true);

            Text = "Détails du document";
            ClientSize = new Size(1000, 700);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.FromArgb(244, 246, 250);
            StartPosition = FormStartPosition.CenterParent;

            BuildUI();
        }

        // ══════════════════════════════════════════════════════════════════════
        // INTERFACE UTILISATEUR
        // ══════════════════════════════════════════════════════════════════════

        private void BuildUI()
        {
            BackColor = Color.FromArgb(244, 246, 250);

            // ── En-tête du formulaire (docked top) ──────────────────────────
            // NOTE: Controls are added in REVERSE docking order for WinForms:
            // 1st add Fill, then Bottom, then Top (last-added = first-docked)

            // STEP 1: Add ListView (Fill) first
            lvLignes = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5F),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(17, 24, 39),
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Padding = new Padding(0)
            };
            // Colonnes du ListView
            lvLignes.Columns.Add("Article/Service", 350);
            lvLignes.Columns.Add("Qté", 70, HorizontalAlignment.Right);
            lvLignes.Columns.Add("Prix unitaire", 120, HorizontalAlignment.Right);
            lvLignes.Columns.Add("Remise %", 80, HorizontalAlignment.Right);
            lvLignes.Columns.Add("Montant HT", 120, HorizontalAlignment.Right);
            Controls.Add(lvLignes);

            // STEP 2: Add Footer (Bottom)
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = Color.White };
            footer.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(229, 231, 235));
                e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
            };
            Controls.Add(footer);

            footer.Controls.Add(new Label
            {
                Text = "Total HT :",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(17, 24, 39),
                AutoSize = true,
                Location = new Point(24, 20),
                BackColor = Color.Transparent
            });

            lblTotalValue = new Label
            {
                Text = "0,00 €",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(59, 130, 246),
                AutoSize = true,
                Location = new Point(24, 45),
                BackColor = Color.Transparent
            };
            footer.Controls.Add(lblTotalValue);

            var btnClose = new Button
            {
                Text = "Fermer",
                Location = new Point(ClientSize.Width - 160, 20),
                Width = 120,
                Height = 40,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(75, 85, 101),
                Font = new Font("Segoe UI", 10F),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(229, 231, 235);
            btnClose.Click += (s, e) => Close();
            footer.Controls.Add(btnClose);

            // STEP 3: Add TitleBar (Top)
            var titleBar = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.Transparent };
            Controls.Add(titleBar);

            var lblArticles = new Label
            {
                Text = "Articles/Services",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(17, 24, 39),
                AutoSize = true,
                Location = new Point(24, 14),
                BackColor = Color.Transparent
            };
            titleBar.Controls.Add(lblArticles);

            // STEP 4: Add Header (Top) last so it docks FIRST
            var header = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.White };
            header.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(229, 231, 235));
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };
            Controls.Add(header);

            lblNumero = new Label
            {
                Text = "—",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(59, 130, 246),
                AutoSize = true,
                Location = new Point(24, 16),
                BackColor = Color.Transparent
            };
            header.Controls.Add(lblNumero);

            lblStatut = new Label
            {
                Text = "—",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129),
                AutoSize = true,
                Location = new Point(24, 44),
                BackColor = Color.Transparent
            };
            header.Controls.Add(lblStatut);

            lblClient = new Label
            {
                Text = "—",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(75, 85, 101),
                AutoSize = true,
                Location = new Point(24, 68),
                BackColor = Color.Transparent
            };
            header.Controls.Add(lblClient);

            lblDateEmission = new Label
            {
                Text = "Émis le —",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(156, 163, 175),
                AutoSize = true,
                Location = new Point(400, 44),
                BackColor = Color.Transparent
            };
            header.Controls.Add(lblDateEmission);

            lblDateEcheance = new Label
            {
                Text = "Échéance —",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(156, 163, 175),
                AutoSize = true,
                Location = new Point(400, 68),
                BackColor = Color.Transparent
            };
            header.Controls.Add(lblDateEcheance);
        }

        // ══════════════════════════════════════════════════════════════════════
        // CHARGEMENT DU FORMULAIRE
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>Initialise le formulaire et charge les données du document.</summary>
        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            try
            {
                // Créer et charger le ViewModel
                _viewModel = _services.NewFactureVM();
                await _viewModel.ChargerAsync(_documentId);

                // Remplir les champs d'en-tête
                lblNumero.Text = _viewModel.Numero;
                lblClient.Text = $"Client : {_viewModel.ClientNom}";
                lblDateEmission.Text = $"Émis le {_viewModel.DateEmission:dd/MM/yyyy}";
                lblDateEcheance.Text = _viewModel.DateEcheance.HasValue
                    ? $"Échéance {_viewModel.DateEcheance:dd/MM/yyyy}"
                    : "Pas d'échéance";

                // Déterminer et afficher le statut
                var statut = _viewModel.Statut;
                lblStatut.Text = statut switch
                {
                    StatutDocument.Brouillon => "BROUILLON",
                    StatutDocument.Envoye => "ENVOYÉ",
                    StatutDocument.Paye => "PAYÉ",
                    _ => "INCONNU"
                };

                lblStatut.ForeColor = statut switch
                {
                    StatutDocument.Brouillon => Color.FromArgb(107, 114, 128),
                    StatutDocument.Envoye => Color.FromArgb(245, 158, 11),
                    StatutDocument.Paye => Color.FromArgb(16, 185, 129),
                    _ => Color.FromArgb(75, 85, 101)
                };

                // Remplir le tableau des lignes
                foreach (var ligne in _viewModel.Lignes)
                {
                    var lvi = new ListViewItem(ligne.Designation);
                    lvi.SubItems.Add(ligne.Quantite.ToString("F0"));
                    lvi.SubItems.Add(ligne.PrixUnitaire.ToString("F2") + " €");
                    lvi.SubItems.Add(ligne.TauxRemise.ToString("F0") + " %");

                    // Calcul du montant HT
                    decimal montant = ligne.Quantite * ligne.PrixUnitaire * (1 - ligne.TauxRemise / 100m);
                    lvi.SubItems.Add(montant.ToString("F2") + " €");

                    lvLignes.Items.Add(lvi);
                }

                // Afficher le total
                lblTotalValue.Text = _viewModel.TotalHT.ToString("F2") + " €";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement : " + ex.Message, "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // INITIALISATION
        // ══════════════════════════════════════════════════════════════════════

        private void InitializeComponent()
        {
            SuspendLayout();
            ResumeLayout(false);
        }
    }
}
