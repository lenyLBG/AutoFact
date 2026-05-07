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
    /// <summary>Formulaire de création/modification de factures et devis.</summary>
    internal partial class FormDocumentAdd : Form
    {
        // ── Services et données ────────────────────────────────────────────
        private readonly Bdd _db;
        private readonly AppServices _services;
        private readonly TypeDocument _docType;
        private DocumentViewModel? _viewModel;
        private List<Prestation> _prestations = new();
        private List<Client> _clients = new();

        // ── UI Controls ────────────────────────────────────────────────────
        private ComboBox cmbClient = null!;
        private DateTimePicker dtEmission = null!;
        private DateTimePicker dtEcheance = null!;
        private DataGridView dgvLignes = null!;
        private Label lblTotal = null!;
        private Label lblTotalValue = null!;
        private Button btnAddLine = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        // ── État ───────────────────────────────────────────────────────────
        public bool DocumentSaved { get; private set; }
        private int? _documentIdToEdit = null;
        private bool _isEditMode = false;

        // ══════════════════════════════════════════════════════════════════════
        // CONSTRUCTEURS
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>Constructeur pour créer un nouveau document.</summary>
        public FormDocumentAdd(Bdd db, TypeDocument docType)
        {
            InitializeComponent();

            _db = db;
            _services = new AppServices(db);
            _docType = docType;
            DocumentSaved = false;
            _isEditMode = false;

            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.AllPaintingInWmPaint, true);

            Text = docType == TypeDocument.Facture ? "Nouvelle Facture" : "Nouveau Devis";
            ClientSize = new Size(900, 650);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.FromArgb(244, 246, 250);
            StartPosition = FormStartPosition.CenterParent;

            BuildUI();
        }

        /// <summary>Constructeur pour modifier un document existant.</summary>
        public FormDocumentAdd(Bdd db, TypeDocument docType, int documentId)
        {
            InitializeComponent();

            _db = db;
            _services = new AppServices(db);
            _docType = docType;
            _documentIdToEdit = documentId;
            _isEditMode = true;
            DocumentSaved = false;

            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.AllPaintingInWmPaint, true);

            Text = docType == TypeDocument.Facture ? "Modifier Facture" : "Modifier Devis";
            ClientSize = new Size(900, 650);
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
            var mainPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            Controls.Add(mainPanel);

            // ── En-tête du formulaire ───────────────────────────────────────
            var header = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.White };
            header.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(229, 231, 235));
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };
            mainPanel.Controls.Add(header);

            var lblTitle = new Label
            {
                Text = _isEditMode 
                    ? (_docType == TypeDocument.Facture ? "Modifier Facture" : "Modifier Devis")
                    : (_docType == TypeDocument.Facture ? "Nouvelle Facture" : "Nouveau Devis"),
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(17, 24, 39),
                AutoSize = true,
                Location = new Point(24, 16),
                BackColor = Color.Transparent
            };
            header.Controls.Add(lblTitle);

            var lblNumero = new Label
            {
                Text = "—",
                Name = "lblNumero",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(59, 130, 246),
                AutoSize = true,
                Location = new Point(24, 44),
                BackColor = Color.Transparent
            };
            header.Controls.Add(lblNumero);

            // ── Contenu du formulaire (zone de scroll) ──────────────────────
            var content = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(244, 246, 250), AutoScroll = true };
            mainPanel.Controls.Add(content);

            // ── Bloc 1 : Informations générales ────────────────────────────
            var infoPad = 24;
            content.Controls.Add(new Label
            {
                Text = "Informations générales",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(17, 24, 39),
                AutoSize = true,
                Location = new Point(infoPad, infoPad)
            });

            // Sélection du client
            content.Controls.Add(new Label
            {
                Text = "Client *",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(75, 85, 101),
                AutoSize = true,
                Location = new Point(infoPad, 64)
            });

            cmbClient = new ComboBox
            {
                Location = new Point(infoPad, 84),
                Width = 300,
                Height = 32,
                DropDownStyle = ComboBoxStyle.DropDown,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(17, 24, 39)
            };
            cmbClient.DrawMode = DrawMode.OwnerDrawFixed;
            cmbClient.DrawItem += (s, e) =>
            {
                e.DrawBackground();
                if (e.Index >= 0 && e.Index < _clients.Count)
                    TextRenderer.DrawText(e.Graphics, _clients[e.Index].Nom,
                        cmbClient.Font, e.Bounds, Color.FromArgb(17, 24, 39),
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                e.DrawFocusRectangle();
            };
            content.Controls.Add(cmbClient);

            // Dates d'émission et d'échéance
            content.Controls.Add(new Label
            {
                Text = "Date d'émission *",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(75, 85, 101),
                AutoSize = true,
                Location = new Point(infoPad, 128)
            });

            dtEmission = new DateTimePicker
            {
                Location = new Point(infoPad, 148),
                Width = 140,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now
            };
            content.Controls.Add(dtEmission);

            content.Controls.Add(new Label
            {
                Text = "Date d'échéance",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(75, 85, 101),
                AutoSize = true,
                Location = new Point(infoPad + 180, 128)
            });

            dtEcheance = new DateTimePicker
            {
                Location = new Point(infoPad + 180, 148),
                Width = 140,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now.AddMonths(1)
            };
            content.Controls.Add(dtEcheance);

            // ── Bloc 2 : Lignes de produits/services ───────────────────────
            content.Controls.Add(new Label
            {
                Text = "Articles/Services",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(17, 24, 39),
                AutoSize = true,
                Location = new Point(infoPad, 210)
            });

            // Tableau des lignes
            dgvLignes = new DataGridView
            {
                Location = new Point(infoPad, 240),
                Width = 852,
                Height = 200,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = true,
                RowHeadersVisible = false,
                MultiSelect = false,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders
            };
            dgvLignes.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(75, 85, 101),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };
            dgvLignes.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(17, 24, 39),
                BackColor = Color.White,
                SelectionBackColor = Color.FromArgb(219, 234, 254),
                SelectionForeColor = Color.FromArgb(17, 24, 39)
            };
            dgvLignes.ColumnHeadersHeight = 36;
            dgvLignes.RowTemplate.Height = 32;

            // Colonnes : Article (ComboBox), Qté, Prix, Remise %, Total
            var colArticle = new DataGridViewComboBoxColumn
            {
                Name = "Article",
                HeaderText = "Article/Service",
                Width = 280,
                DataSource = _prestations.ToList(),
                DisplayMember = "Nom",
                ValueMember = "Id"
            };
            dgvLignes.Columns.Add(colArticle);

            var colQte = new DataGridViewTextBoxColumn
            {
                Name = "Qte",
                HeaderText = "Qté",
                Width = 70,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            };
            dgvLignes.Columns.Add(colQte);

            var colPrix = new DataGridViewTextBoxColumn
            {
                Name = "Prix",
                HeaderText = "Prix unitaire",
                Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            };
            dgvLignes.Columns.Add(colPrix);

            var colRemise = new DataGridViewTextBoxColumn
            {
                Name = "Remise",
                HeaderText = "Remise %",
                Width = 80,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            };
            dgvLignes.Columns.Add(colRemise);

            var colTotal = new DataGridViewTextBoxColumn
            {
                Name = "Total",
                HeaderText = "Montant HT",
                Width = 120,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    ForeColor = Color.FromArgb(59, 130, 246),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
                }
            };
            dgvLignes.Columns.Add(colTotal);

            // Recalcul des totaux lors de la modification
            dgvLignes.CellEndEdit += (s, e) =>
            {
                if (e.ColumnIndex == 0) // Article sélectionné
                {
                    var presId = dgvLignes.Rows[e.RowIndex].Cells[0].Value;
                    if (presId is int id && id > 0)
                    {
                        var pres = _prestations.FirstOrDefault(p => p.Id == id);
                        if (pres != null)
                        {
                            dgvLignes.Rows[e.RowIndex].Cells[2].Value = pres.PrixUnitaire.ToString("F2");
                            dgvLignes.Rows[e.RowIndex].Cells[3].Value = "0";
                        }
                    }
                }
                RecalculateLineTotal(e.RowIndex);
                RecalculateTotal();
            };

            dgvLignes.CellValueChanged += (s, e) =>
            {
                if (e.ColumnIndex >= 1 && e.ColumnIndex <= 3 && e.RowIndex >= 0)
                {
                    RecalculateLineTotal(e.RowIndex);
                    RecalculateTotal();
                }
            };

            content.Controls.Add(dgvLignes);

            // Bouton ajouter ligne
            btnAddLine = new Button
            {
                Text = "＋  Ajouter une ligne",
                Location = new Point(infoPad, 460),
                Width = 168,
                Height = 38,
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAddLine.FlatAppearance.BorderSize = 0;
            btnAddLine.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRect(new Rectangle(0, 0, btnAddLine.Width - 1, btnAddLine.Height - 1), 8);
                using var br = new SolidBrush(btnAddLine.BackColor);
                e.Graphics.FillPath(br, path);
                TextRenderer.DrawText(e.Graphics, btnAddLine.Text, btnAddLine.Font,
                    btnAddLine.ClientRectangle, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnAddLine.Click += (s, e) =>
            {
                dgvLignes.Rows.Add("", "1", "0.00", "0", "0.00");
                dgvLignes.CurrentCell = dgvLignes.Rows[dgvLignes.Rows.Count - 1].Cells[0];
                dgvLignes.BeginEdit(true);
            };
            content.Controls.Add(btnAddLine);

            // ── Bloc 3 : Total ─────────────────────────────────────────────
            lblTotal = new Label
            {
                Text = "Total HT :",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(75, 85, 101),
                AutoSize = true,
                Location = new Point(infoPad, 520)
            };
            content.Controls.Add(lblTotal);

            lblTotalValue = new Label
            {
                Text = "0.00 €",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(59, 130, 246),
                AutoSize = true,
                Location = new Point(infoPad, 546),
                BackColor = Color.Transparent
            };
            content.Controls.Add(lblTotalValue);

            // ── Pied de page : Boutons ─────────────────────────────────────
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = Color.White };
            footer.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(229, 231, 235));
                e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
            };
            mainPanel.Controls.Add(footer);

            // Bouton Enregistrer
            btnSave = new Button
            {
                Text = "Enregistrer",
                Location = new Point(ClientSize.Width - 300, 20),
                Width = 120,
                Height = 40,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRect(new Rectangle(0, 0, btnSave.Width - 1, btnSave.Height - 1), 6);
                using var br = new SolidBrush(btnSave.BackColor);
                e.Graphics.FillPath(br, path);
                TextRenderer.DrawText(e.Graphics, btnSave.Text, btnSave.Font,
                    btnSave.ClientRectangle, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnSave.Click += BtnSave_Click;
            footer.Controls.Add(btnSave);

            // Bouton Annuler
            btnCancel = new Button
            {
                Text = "Annuler",
                Location = new Point(ClientSize.Width - 160, 20),
                Width = 120,
                Height = 40,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(75, 85, 101),
                Font = new Font("Segoe UI", 10F),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(229, 231, 235);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            footer.Controls.Add(btnCancel);
        }

        // ══════════════════════════════════════════════════════════════════════
        // CALCULS
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>Recalcule le total d'une ligne (prix * qté - remise).</summary>
        private void RecalculateLineTotal(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgvLignes.Rows.Count) return;

            var row = dgvLignes.Rows[rowIndex];
            if (!decimal.TryParse(row.Cells[2].Value?.ToString() ?? "0", out decimal prix)) prix = 0;
            if (!int.TryParse(row.Cells[1].Value?.ToString() ?? "1", out int qte)) qte = 1;
            if (!decimal.TryParse(row.Cells[3].Value?.ToString() ?? "0", out decimal remise)) remise = 0;

            decimal total = prix * qte * (1 - remise / 100m);
            row.Cells[4].Value = total.ToString("F2");
        }

        /// <summary>Recalcule et affiche le total général du document.</summary>
        private void RecalculateTotal()
        {
            decimal total = 0;
            foreach (DataGridViewRow row in dgvLignes.Rows)
            {
                if (!row.IsNewRow && decimal.TryParse(row.Cells[4].Value?.ToString() ?? "0", out decimal lineTotal))
                    total += lineTotal;
            }
            lblTotalValue.Text = total.ToString("F2") + " €";
        }

        // ══════════════════════════════════════════════════════════════════════
        // SAUVEGARDE
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>Valide et enregistre le document.</summary>
        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            // Validation : client sélectionné
            if (cmbClient.SelectedIndex < 0)
            {
                MessageBox.Show("Veuillez sélectionner un client.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validation : au moins une ligne
            if (dgvLignes.Rows.Count == 0 || (dgvLignes.Rows.Count == 1 && dgvLignes.Rows[0].IsNewRow))
            {
                MessageBox.Show("Veuillez ajouter au moins une ligne.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validation : chaque ligne a un article et un prix valides
            foreach (DataGridViewRow row in dgvLignes.Rows)
            {
                if (row.IsNewRow) continue;

                var prestationIdObj = row.Cells[0].Value;
                if (!(prestationIdObj is int prestationId) || prestationId <= 0)
                {
                    MessageBox.Show($"Ligne {row.Index + 1} : veuillez sélectionner un article valide.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!decimal.TryParse(row.Cells[2].Value?.ToString() ?? "0", out _))
                {
                    MessageBox.Show($"Ligne {row.Index + 1} : prix invalide.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            try
            {
                // Préparer le ViewModel (Facture ou Devis)
                _viewModel = _services.NewFactureVM();
                if (_docType == TypeDocument.Devis)
                    _viewModel = _services.NewDevisVM();

                // Remplir les données du document
                var clientId = _clients[cmbClient.SelectedIndex].Id;
                _viewModel.ClientId = clientId;
                _viewModel.DateEmission = dtEmission.Value;
                _viewModel.DateEcheance = dtEcheance.Value;

                // Ajouter les lignes avec validation
                foreach (DataGridViewRow row in dgvLignes.Rows)
                {
                    if (row.IsNewRow) continue;

                    var prestationId = (int)row.Cells[0].Value;
                    var prestation = _prestations.FirstOrDefault(p => p.Id == prestationId);
                    if (prestation == null) continue;

                    if (!int.TryParse(row.Cells[1].Value?.ToString() ?? "1", out int qty)) qty = 1;
                    if (!decimal.TryParse(row.Cells[3].Value?.ToString() ?? "0", out decimal discount)) discount = 0;

                    _viewModel.AjouterLigne(prestation, qty);

                    // Appliquer la remise à la dernière ligne ajoutée
                    var addedLine = _viewModel.Lignes.Last();
                    addedLine.TauxRemise = discount;
                }

                // Enregistrer le document
                btnSave.Enabled = false;
                btnSave.Text = "Enregistrement...";

                bool ok = await _viewModel.SauvegarderAsync();
                if (!ok)
                {
                    MessageBox.Show("Erreur sauvegarde : " + _viewModel.Erreur, "Erreur",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnSave.Enabled = true;
                    btnSave.Text = "Enregistrer";
                    return;
                }

                DocumentSaved = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur sauvegarde : " + ex.Message, "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnSave.Enabled = true;
                btnSave.Text = "Enregistrer";
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // CHARGEMENT DU FORMULAIRE
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>Initialise le formulaire : charge les clients, articles et données existantes.</summary>
        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            try
            {
                // Charger les données de référence
                _clients = await _services.Clients.TousAsync();
                _prestations = await _services.Prestations.ToutesAsync();

                cmbClient.DataSource = _clients.ToList();
                cmbClient.DisplayMember = "Nom";
                cmbClient.ValueMember = "Id";

                // Mettre à jour la colonne article du DataGridView
                var colArticle = dgvLignes.Columns[0] as DataGridViewComboBoxColumn;
                if (colArticle != null)
                {
                    colArticle.DataSource = _prestations.ToList();
                    colArticle.DisplayMember = "Nom";
                    colArticle.ValueMember = "Id";
                }

                // Créer ou charger le ViewModel
                if (_viewModel == null)
                    _viewModel = _docType == TypeDocument.Facture ? _services.NewFactureVM() : _services.NewDevisVM();

                if (_isEditMode && _documentIdToEdit.HasValue)
                {
                    // Mode édition : charger le document existant
                    await _viewModel.ChargerAsync(_documentIdToEdit.Value);

                    // Remplir les champs avec les données du document
                    cmbClient.SelectedValue = _viewModel.ClientId;
                    dtEmission.Value = _viewModel.DateEmission;
                    if (_viewModel.DateEcheance.HasValue)
                        dtEcheance.Value = _viewModel.DateEcheance.Value;

                    // Remplir le tableau avec les lignes du document
                    dgvLignes.Rows.Clear();
                    foreach (var ligne in _viewModel.Lignes)
                    {
                        int rowIdx = dgvLignes.Rows.Add();
                        dgvLignes.Rows[rowIdx].Cells[0].Value = ligne.PrestationId;
                        dgvLignes.Rows[rowIdx].Cells[1].Value = ligne.Designation;
                        dgvLignes.Rows[rowIdx].Cells[2].Value = ligne.Quantite;
                        dgvLignes.Rows[rowIdx].Cells[3].Value = ligne.PrixUnitaire;
                        dgvLignes.Rows[rowIdx].Cells[4].Value = ligne.TauxRemise;
                        dgvLignes.Rows[rowIdx].Tag = ligne.Id;
                    }
                }
                else
                {
                    // Mode création : générer un nouveau numéro
                    await _viewModel.InitialiserNumeroAsync();
                }

                // Afficher le numéro du document
                var lblNum = Controls.Find("lblNumero", true).FirstOrDefault() as Label;
                if (lblNum != null)
                    lblNum.Text = _viewModel.Numero;

                // Afficher le total
                var lblTotal = Controls.Find("lblTotalValue", true).FirstOrDefault() as Label;
                if (lblTotal != null)
                    lblTotal.Text = _viewModel.TotalHT.ToString("F2") + " €";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement : " + ex.Message, "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // UTILITAIRES
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>Crée un rectangle arrondi pour les bordures.</summary>
        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            radius = Math.Max(1, Math.Min(radius, Math.Min(r.Width, r.Height) / 2));
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            ResumeLayout(false);
        }
    }
}
