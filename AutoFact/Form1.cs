using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using MySqlConnector;

namespace autofact
{
    public partial class Form1 : Form
    {
        // ── Palette ────────────────────────────────────────────────────────────
        private readonly Color clrSidebarStart = Color.FromArgb(17, 24, 39);   // gray-900
        private readonly Color clrSidebarEnd = Color.FromArgb(9, 14, 26);   // near-black
        private readonly Color clrAccentBar = Color.FromArgb(59, 130, 246);   // blue-500
        private readonly Color clrMainBg = Color.FromArgb(244, 246, 250);  // cool gray
        private readonly Color clrWhite = Color.White;
        private readonly Color clrTextDark = Color.FromArgb(17, 24, 39);   // gray-900
        private readonly Color clrTextMid = Color.FromArgb(75, 85, 101);   // gray-600
        private readonly Color clrTextLight = Color.FromArgb(156, 163, 175);  // gray-400
        private readonly Color clrBorder = Color.FromArgb(229, 231, 235);  // gray-200
        private readonly Color primaryColor = Color.FromArgb(59, 130, 246);   // blue-500
        private readonly Color primaryLight = Color.FromArgb(239, 246, 255);  // blue-50
        private readonly Color clrGreen = Color.FromArgb(16, 185, 129);   // emerald-500
        private readonly Color clrOrange = Color.FromArgb(245, 158, 11);  // amber-500
        private readonly Color clrPurple = Color.FromArgb(139, 92, 246);  // violet-500

        // ── Layout panels ──────────────────────────────────────────────────────
        private Panel panelSidebar = null!;
        private Panel panelMain = null!;
        private Panel panelTopbar = null!;
        private Panel panelContent = null!;
        private Label lblPageTitle = null!;

        // ── State ──────────────────────────────────────────────────────────────
        private Panel? activeMenuPanel;
        private Bdd db = null!;
        private AppServices _services = null!;
        private int? currentUserId;
        private string? currentUserEmail;
        private string _currentViewType = "dashboard";  // Track which view is displayed

        // ── Client view controls ───────────────────────────────────────────────
        private ListView lvClients = null!;
        private Button btnAddClient = null!;

        // ── Articles view controls ─────────────────────────────────────────────
        private ListView lvArticles = null!;
        private Button btnAddArticle = null!;

        private ToolTip tooltip = null!;

        // ══════════════════════════════════════════════════════════════════════
        internal Form1(Bdd bdd, int? authenticatedUserId = null,
                       string? authenticatedUserEmail = null,
                       AppServices? services = null)
        {
            InitializeComponent();

            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.AllPaintingInWmPaint, true);

            tooltip = new ToolTip();
            db = bdd ?? new Bdd();
            _services = services ?? new AppServices(db);
            currentUserId = authenticatedUserId;
            currentUserEmail = authenticatedUserEmail;

            SetupForm();
            BuildLayout();

            Load += Form1_Load;
        }

        // ══════════════════════════════════════════════════════════════════════
        // FORM SETUP
        // ══════════════════════════════════════════════════════════════════════
        private void SetupForm()
        {
            Text = "AutoFact";
            ClientSize = new Size(1280, 720);   // set ClientSize, not Size, to match the designer
            MinimumSize = new Size(980, 640);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10F);
            BackColor = clrMainBg;
        }

        // ══════════════════════════════════════════════════════════════════════
        // TOP-LEVEL LAYOUT
        // ══════════════════════════════════════════════════════════════════════
        private void BuildLayout()
        {
            // DockStyle.Fill must be added BEFORE DockStyle.Left — WinForms docks
            // in reverse Controls order, so Fill is processed last and correctly
            // fills only the space left after the Left panel is reserved.
            panelMain = new Panel { Dock = DockStyle.Fill, BackColor = clrMainBg };
            Controls.Add(panelMain);

            panelSidebar = new Panel { Dock = DockStyle.Left, Width = 260, BackColor = Color.Transparent };
            panelSidebar.Paint += PaintSidebar;
            Controls.Add(panelSidebar);

            BuildSidebar();
            BuildMain();
        }

        // ══════════════════════════════════════════════════════════════════════
        // SIDEBAR
        // ══════════════════════════════════════════════════════════════════════
        private void PaintSidebar(object? sender, PaintEventArgs e)
        {
            if (panelSidebar.Width <= 0 || panelSidebar.Height <= 0) return;

            using var br = new LinearGradientBrush(panelSidebar.ClientRectangle,
                clrSidebarStart, clrSidebarEnd, LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(br, panelSidebar.ClientRectangle);

            using var pen = new Pen(Color.FromArgb(30, 255, 255, 255), 1f);
            e.Graphics.DrawLine(pen, panelSidebar.Width - 1, 0,
                                     panelSidebar.Width - 1, panelSidebar.Height);
        }

        private void BuildSidebar()
        {
            // WinForms docks in reverse Controls.Add order.
            // Correct order to add: Fill first, then Bottom, then Top (last-added = first-docked).

            // ── Nav section (Fill) — added FIRST so it's docked LAST ──────────
            var navArea = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            panelSidebar.Controls.Add(navArea);

            // ── Footer (Bottom) ───────────────────────────────────────────────
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 116, BackColor = Color.Transparent };
            panelSidebar.Controls.Add(footer);

            // ── Separator under header (Top) ──────────────────────────────────
            var sepTop = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(35, 255, 255, 255) };
            panelSidebar.Controls.Add(sepTop);

            // ── Logo header (Top) — added LAST so it's docked FIRST ───────────
            var header = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Color.Transparent };
            panelSidebar.Controls.Add(header);

            // ── Header contents ───────────────────────────────────────────────
            var logo = new Panel { Size = new Size(36, 36), Location = new Point(20, 18), BackColor = Color.Transparent, Cursor = Cursors.Hand };
            logo.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var br = new LinearGradientBrush(logo.ClientRectangle,
                    Color.FromArgb(96, 165, 250), primaryColor, LinearGradientMode.ForwardDiagonal);
                e.Graphics.FillEllipse(br, 0, 0, 35, 35);
                using var f = new Font("Segoe UI", 10F, FontStyle.Bold);
                using var tb = new SolidBrush(Color.White);
                SizeF sz = e.Graphics.MeasureString("AF", f);
                e.Graphics.DrawString("AF", f, tb,
                    (36 - sz.Width) / 2f, (36 - sz.Height) / 2f);
            };
            header.Controls.Add(logo);
            var lblTitleAutoFact = new Label
            {
                Text = "AutoFact",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(64, 15),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            header.Controls.Add(lblTitleAutoFact);
            header.Controls.Add(new Label
            {
                Text = "Gestion commerciale",
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 8F),
                AutoSize = true,
                Location = new Point(65, 38),
                BackColor = Color.Transparent
            });

            // ── Nav section contents ──────────────────────────────────────────
            navArea.Controls.Add(new Label
            {
                Text = "NAVIGATION",
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 18),
                BackColor = Color.Transparent
            });

            var flow = new FlowLayoutPanel
            {
                Left = 0,
                Top = 42,
                Width = 260,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            navArea.Controls.Add(flow);

            var itemDashboard = CreateNavItem("Tableau de bord", null, "home");
            var itemDevis = CreateNavItem("Devis", null, "document");
            var itemFacture = CreateNavItem("Facturation", null, "clipboard");
            var itemClients = CreateNavItem("Clients", null, "user");
            var itemArticles = CreateNavItem("Articles", "Produits/Services", "cube");
            var itemUrssaf = CreateNavItem("URSSAF", "Cotisations", "chart");

            flow.Controls.Add(itemDashboard);
            flow.Controls.Add(itemDevis);
            flow.Controls.Add(itemFacture);
            flow.Controls.Add(itemClients);
            flow.Controls.Add(itemArticles);
            flow.Controls.Add(itemUrssaf);

            SetActiveMenu(itemDashboard);

            logo.Click += (s, e) => NavItemClicked(itemDashboard, "Tableau de bord");
            lblTitleAutoFact.Click += (s, e) => NavItemClicked(itemDashboard, "Tableau de bord");

            // ── Footer contents ───────────────────────────────────────────────
            var sepBot = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(35, 255, 255, 255) };
            footer.Controls.Add(sepBot);

            var userRow = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent };
            var userAvatar = new Panel { Size = new Size(32, 32), Location = new Point(16, 6), BackColor = Color.Transparent };
            userAvatar.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var br = new SolidBrush(primaryColor);
                e.Graphics.FillEllipse(br, 0, 0, 31, 31);
                using var f = new Font("Segoe UI", 9F, FontStyle.Bold);
                string initials = currentUserEmail?.Length > 0 ? currentUserEmail[..1].ToUpper() : "U";
                SizeF sz = e.Graphics.MeasureString(initials, f);
                using var tb = new SolidBrush(Color.White);
                e.Graphics.DrawString(initials, f, tb,
                    (32 - sz.Width) / 2f, (32 - sz.Height) / 2f);
            };
            userRow.Controls.Add(userAvatar);
            userRow.Controls.Add(new Label
            {
                Text = currentUserEmail ?? "Utilisateur",
                ForeColor = Color.FromArgb(203, 213, 225),
                Font = new Font("Segoe UI", 8.5F),
                AutoSize = false,
                Width = 180,
                Height = 20,
                Location = new Point(56, 12),
                BackColor = Color.Transparent
            });
            footer.Controls.Add(userRow);

            var itemSettings = CreateNavItem("Paramètres", null, "cog");
            var itemLogout = CreateNavItem("Déconnexion", null, "logout");
            var footerFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = itemSettings.Height + itemLogout.Height,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            footerFlow.Controls.Add(itemSettings);
            footerFlow.Controls.Add(itemLogout);
            footer.Controls.Add(footerFlow);

            // Recalculate footer height to fit all content exactly
            footer.Height = sepBot.Height + userRow.Height + footerFlow.Height + 4;
        }

        private Panel CreateNavItem(string title, string? subtitle, string iconKey)
        {
            int h = subtitle != null ? 52 : 44;
            var p = new Panel
            {
                Width = 260,
                Height = h,
                Margin = new Padding(0),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };

            var accent = new Panel { Width = 3, Dock = DockStyle.Left, BackColor = clrAccentBar, Visible = false };
            p.Controls.Add(accent);

            // Icon background circle
            var iconCircle = new Panel
            {
                Size = new Size(28, 28),
                Location = new Point(18, (h - 28) / 2),
                BackColor = Color.Transparent
            };
            iconCircle.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool active = p == activeMenuPanel;
                if (active)
                {
                    using var br = new SolidBrush(Color.FromArgb(40, 96, 165, 250));
                    e.Graphics.FillEllipse(br, 0, 0, 27, 27);
                }
                Color iconColor = active ? Color.White : Color.FromArgb(148, 163, 184);
                DrawNavIcon(e.Graphics, iconKey, iconColor, new Rectangle(0, 0, 28, 28));
            };
            p.Controls.Add(iconCircle);

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                Location = new Point(54, subtitle != null ? 8 : (h - 16) / 2),
                BackColor = Color.Transparent
            };
            p.Controls.Add(lblTitle);

            if (subtitle != null)
            {
                var lblSub = new Label
                {
                    Text = subtitle,
                    Font = new Font("Segoe UI", 7.5F),
                    ForeColor = Color.FromArgb(71, 85, 105),
                    AutoSize = true,
                    Location = new Point(54, 26),
                    BackColor = Color.Transparent
                };
                p.Controls.Add(lblSub);
                lblSub.Click += (s, e) => NavItemClicked(p, title);
            }

            p.Tag = (accent, lblTitle, iconCircle);

            void activate(object? s, EventArgs e) => NavItemClicked(p, title);
            p.Click += activate;
            iconCircle.Click += activate;
            lblTitle.Click += activate;

            p.MouseEnter += (s, e) =>
            {
                if (p != activeMenuPanel)
                {
                    p.BackColor = Color.FromArgb(18, 255, 255, 255);
                    lblTitle.ForeColor = Color.FromArgb(209, 213, 219);
                }
            };
            p.MouseLeave += (s, e) =>
            {
                if (p != activeMenuPanel)
                {
                    p.BackColor = Color.Transparent;
                    lblTitle.ForeColor = Color.FromArgb(148, 163, 184);
                }
            };

            return p;
        }

        private void DrawNavIcon(Graphics g, string key, Color color, Rectangle bounds)
        {
            float centerX = bounds.Left + bounds.Width / 2f;
            float centerY = bounds.Top + bounds.Height / 2f;

            using var pen = new Pen(color, 1.5f) { LineJoin = System.Drawing.Drawing2D.LineJoin.Round };

            switch (key)
            {
                case "home": // Graphique à barres (dashboard)
                    // 3 barres ascendantes
                    g.DrawLine(pen, centerX - 6, centerY + 4, centerX - 6, centerY - 2);
                    g.DrawLine(pen, centerX, centerY + 4, centerX, centerY - 4);
                    g.DrawLine(pen, centerX + 6, centerY + 4, centerX + 6, centerY - 6);
                    // Base
                    g.DrawLine(pen, centerX - 8, centerY + 5, centerX + 8, centerY + 5);
                    break;

                case "document": // Document avec coin plié
                    // Corps du document
                    g.DrawRectangle(pen, centerX - 5, centerY - 6, 10, 12);
                    // Coin plié en haut à droite
                    g.DrawLine(pen, centerX + 5, centerY - 6, centerX + 2, centerY - 6);
                    g.DrawLine(pen, centerX + 5, centerY - 6, centerX + 5, centerY - 3);
                    g.DrawLine(pen, centerX + 2, centerY - 6, centerX + 5, centerY - 3);
                    // Lignes de texte
                    g.DrawLine(pen, centerX - 3, centerY - 2, centerX + 3, centerY - 2);
                    g.DrawLine(pen, centerX - 3, centerY + 1, centerX + 3, centerY + 1);
                    g.DrawLine(pen, centerX - 3, centerY + 4, centerX + 3, centerY + 4);
                    break;

                case "clipboard": // Presse-papiers avec tableau
                    // Pince en haut
                    g.DrawRectangle(pen, centerX - 2, centerY - 7, 4, 2);
                    // Corps
                    g.DrawRectangle(pen, centerX - 5, centerY - 5, 10, 11);
                    // Lignes du tableau
                    g.DrawLine(pen, centerX - 3, centerY - 2, centerX + 3, centerY - 2);
                    g.DrawLine(pen, centerX - 3, centerY + 1, centerX + 3, centerY + 1);
                    g.DrawLine(pen, centerX - 3, centerY + 4, centerX + 3, centerY + 4);
                    break;

                case "user": // Personne / profil
                    // Tête
                    g.DrawEllipse(pen, centerX - 3, centerY - 5, 6, 6);
                    // Corps
                    g.DrawPath(pen, CreateRoundedPath(centerX - 5, centerY + 1, 10, 6, 2));
                    break;

                case "cube": // Boîte / paquet
                    // Face avant
                    g.DrawLine(pen, centerX - 4, centerY - 2, centerX - 4, centerY + 4);
                    g.DrawLine(pen, centerX - 4, centerY + 4, centerX + 4, centerY + 4);
                    g.DrawLine(pen, centerX + 4, centerY + 4, centerX + 4, centerY - 2);
                    g.DrawLine(pen, centerX + 4, centerY - 2, centerX - 4, centerY - 2);
                    // Face supérieure
                    g.DrawLine(pen, centerX - 4, centerY - 2, centerX - 1, centerY - 4);
                    g.DrawLine(pen, centerX - 1, centerY - 4, centerX + 5, centerY - 4);
                    g.DrawLine(pen, centerX + 5, centerY - 4, centerX + 4, centerY - 2);
                    break;

                case "cog": // Engrenage / Paramètres
                    // Cercle central
                    g.DrawEllipse(pen, centerX - 2, centerY - 2, 4, 4);
                    // Dents (8 dents)
                    float radius = 5f;
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = (float)(i * Math.PI / 4);
                        float x1 = centerX + (float)Math.Cos(angle) * radius;
                        float y1 = centerY + (float)Math.Sin(angle) * radius;
                        float x2 = centerX + (float)Math.Cos(angle) * (radius + 2);
                        float y2 = centerY + (float)Math.Sin(angle) * (radius + 2);
                        g.DrawLine(pen, x1, y1, x2, y2);
                    }
                    break;

                case "logout": // Sortie / Porte
                    // Porte
                    g.DrawRectangle(pen, centerX - 3, centerY - 4, 4, 8);
                    // Poignée
                    g.DrawEllipse(pen, centerX + 1, centerY + 1, 2, 2);
                    // Flèche de sortie
                    g.DrawLine(pen, centerX + 3, centerY - 2, centerX + 6, centerY - 2);
                    g.DrawLine(pen, centerX + 6, centerY - 2, centerX + 5, centerY - 3);
                    g.DrawLine(pen, centerX + 6, centerY - 2, centerX + 5, centerY - 1);
                    break;

                case "chart": // Graphique / Tableau URSSAF
                    // Axes
                    g.DrawLine(pen, centerX - 5, centerY + 4, centerX - 5, centerY - 4);
                    g.DrawLine(pen, centerX - 5, centerY + 4, centerX + 5, centerY + 4);
                    // Points/barres du graphique
                    g.DrawEllipse(pen, centerX - 4, centerY + 1, 2, 2);
                    g.DrawEllipse(pen, centerX - 1, centerY - 1, 2, 2);
                    g.DrawEllipse(pen, centerX + 2, centerY - 3, 2, 2);
                    // Lignes de connexion
                    g.DrawLine(pen, centerX - 3, centerY + 2, centerX, centerY);
                    g.DrawLine(pen, centerX, centerY, centerX + 3, centerY - 2);
                    break;

                default:
                    // Point par défaut
                    g.FillEllipse(new SolidBrush(color), centerX - 1, centerY - 1, 2, 2);
                    break;
            }
        }

        private System.Drawing.Drawing2D.GraphicsPath CreateRoundedPath(float x, float y, float width, float height, float radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddRectangle(new RectangleF(x, y, width, height));
            return path;
        }

        private static string GetNavIcon(string key) => key switch
        {
            "home" => "🏠",
            "document" => "📄",
            "clipboard" => "📋",
            "user" => "👤",
            "cube" => "📦",
            "cog" => "⚙",
            "logout" => "🚪",
            "chart" => "📊",
            _ => "•"
        };

        private void SetActiveMenu(Panel? p)
        {
            if (activeMenuPanel?.Tag is (Panel oa, Label ol, Panel oc))
            {
                oa.Visible = false;
                ol.ForeColor = Color.FromArgb(148, 163, 184);
                ol.Font = new Font("Segoe UI", 10F);
                activeMenuPanel.BackColor = Color.Transparent;
                oc.Invalidate();
            }

            activeMenuPanel = p;

            if (activeMenuPanel?.Tag is (Panel na, Label nl, Panel nc))
            {
                activeMenuPanel.BackColor = Color.FromArgb(22, 255, 255, 255);
                na.Visible = true;
                nl.ForeColor = Color.White;
                nl.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                nc.Invalidate();
            }
        }

        private async void NavItemClicked(Panel p, string title)
        {
            SetActiveMenu(p);
            lblPageTitle.Text = title;

            switch (title)
            {
                case "Paramètres":
                    try
                    {
                        using var dlg = new FormSettings(db);
                        dlg.ShowDialog(this);
                        if (dlg.SettingsUpdated)
                        {
                            bool ok = await db.TestConnectionAsync();
                            if (!ok)
                                MessageBox.Show("Échec de la connexion après modification.", "Erreur",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Erreur paramètres : " + ex.Message, "Erreur",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    break;

                case "Déconnexion":
                    if (MessageBox.Show("Voulez-vous vous déconnecter ?", "Déconnexion",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        currentUserId = null;
                        currentUserEmail = null;
                        Application.Exit();
                    }
                    break;

                case "Clients":
                    ShowClientsView();
                    await LoadClientsAsync();
                    break;

                case "Articles":
                    ShowArticlesView();
                    await LoadArticlesAsync();
                    break;

                case "Devis":
                    ShowDocumentsView(autofact.Models.TypeDocument.Devis, "Devis", "document", clrPurple);
                    await LoadDocumentsAsync();
                    break;

                case "Facturation":
                    ShowDocumentsView(autofact.Models.TypeDocument.Facture, "Facturation", "clipboard", primaryColor);
                    await LoadDocumentsAsync();
                    break;

                case "URSSAF":
                    ShowUrssafView();
                    break;

                default:
                    ShowDashboard();
                    break;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // TOPBAR
        // ══════════════════════════════════════════════════════════════════════
        private void BuildMain()
        {
            panelTopbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = clrWhite
            };
            panelTopbar.Paint += (s, e) =>
            {
                if (panelTopbar.Width <= 0 || panelTopbar.Height <= 0) return;

                using var pen = new Pen(clrBorder, 1f);
                e.Graphics.DrawLine(pen, 0, panelTopbar.Height - 1,
                                         panelTopbar.Width, panelTopbar.Height - 1);

                var glowRect = new Rectangle(0, panelTopbar.Height - 4, panelTopbar.Width, 4);
                if (glowRect.Width > 0 && glowRect.Height > 0)
                {
                    using var br = new LinearGradientBrush(
                        glowRect,
                        Color.FromArgb(8, 0, 0, 0), Color.Transparent, LinearGradientMode.Vertical);
                    e.Graphics.FillRectangle(br, glowRect);
                }
            };
            panelMain.Controls.Add(panelTopbar);

            // Page title
            lblPageTitle = new Label
            {
                Text = "Dashboard",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = clrTextDark,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(28, 20)
            };
            panelTopbar.Controls.Add(lblPageTitle);
            panelTopbar.Resize += (s, e) =>
                lblPageTitle.Location = new Point(28, (panelTopbar.Height - lblPageTitle.Height) / 2);

            // Right-side controls container
            var rightBar = new Panel
            {
                Height = 64,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            panelTopbar.Controls.Add(rightBar);

            // Search box
            var searchWrap = new Panel
            {
                Size = new Size(260, 36),
                Left = 0,
                Top = 14,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            searchWrap.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRectStatic(new Rectangle(0, 0, searchWrap.Width - 1, searchWrap.Height - 1), 8);
                using var br = new SolidBrush(searchWrap.BackColor);
                e.Graphics.FillPath(br, path);
                using var pen = new Pen(clrBorder, 1f);
                e.Graphics.DrawPath(pen, path);
                // search icon
                using var ipen = new Pen(Color.FromArgb(156, 163, 175), 1.5f);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawEllipse(ipen, 10, 10, 14, 14);
                e.Graphics.DrawLine(ipen, 22, 22, 28, 28);
            };
            var txtSearch = new TextBox
            {
                BorderStyle = BorderStyle.None,
                PlaceholderText = "Rechercher…",
                Font = new Font("Segoe UI", 9.5F),
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = clrTextMid,
                Location = new Point(36, 9),
                Width = searchWrap.Width - 48
            };
            searchWrap.Controls.Add(txtSearch);
            rightBar.Controls.Add(searchWrap);

            // Notification bell
            var bell = new Panel
            {
                Size = new Size(36, 36),
                Left = 272,
                Top = 14,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            bell.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.FromArgb(107, 114, 128), 1.6f);
                e.Graphics.DrawArc(pen, 8, 6, 20, 18, 180, 180);
                e.Graphics.DrawLine(pen, 8, 15, 8, 22);
                e.Graphics.DrawLine(pen, 28, 15, 28, 22);
                e.Graphics.DrawArc(pen, 12, 20, 12, 8, 0, 180);
                // dot
                using var dotBr = new SolidBrush(Color.FromArgb(239, 68, 68));
                e.Graphics.FillEllipse(dotBr, 22, 6, 8, 8);
            };
            rightBar.Controls.Add(bell);

            // User avatar pill
            var userPill = new Panel
            {
                Size = new Size(130, 36),
                Left = 318,
                Top = 14,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            userPill.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRectStatic(new Rectangle(0, 0, userPill.Width - 1, userPill.Height - 1), 18);
                using var bg = new SolidBrush(Color.FromArgb(248, 250, 252));
                e.Graphics.FillPath(bg, path);
                using var bdr = new Pen(clrBorder, 1f);
                e.Graphics.DrawPath(bdr, path);
                // Avatar circle
                using var avBr = new SolidBrush(primaryColor);
                e.Graphics.FillEllipse(avBr, 5, 4, 27, 27);
                string initial = currentUserEmail?.Length > 0 ? currentUserEmail[..1].ToUpper() : "U";
                using var f = new Font("Segoe UI", 9F, FontStyle.Bold);
                using var tb = new SolidBrush(Color.White);
                SizeF sz = e.Graphics.MeasureString(initial, f);
                e.Graphics.DrawString(initial, f, tb, 5 + (27 - sz.Width) / 2f, 4 + (27 - sz.Height) / 2f);
                // Name
                string name = currentUserEmail ?? "User";
                if (name.Length > 10) name = name[..10] + "…";
                TextRenderer.DrawText(e.Graphics, name,
                    new Font("Segoe UI", 8.5F), new Rectangle(38, 0, 80, 36),
                    clrTextDark, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };
            rightBar.Controls.Add(userPill);

            // Size and position rightBar
            int rbW = 460;
            rightBar.Size = new Size(rbW, 64);
            panelTopbar.Resize += (s, e) =>
                rightBar.Location = new Point(panelTopbar.ClientSize.Width - rbW - 12,
                                              (panelTopbar.Height - rightBar.Height) / 2);
            rightBar.Location = new Point(panelTopbar.Width - rbW - 12, 0);

            // ── Content area ──────────────────────────────────────────────────
            panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = clrMainBg,
                AutoScroll = false,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            panelMain.Controls.Add(panelContent);
            panelContent.BringToFront();

            ShowDashboard();
        }

        // ══════════════════════════════════════════════════════════════════════
        // DASHBOARD
        // ══════════════════════════════════════════════════════════════════════
        private void ShowDashboard()
        {
            _currentViewType = "dashboard";  // Track view type
            panelContent.Controls.Clear();
            panelContent.AutoScroll = true;

            const int padH = 32, padV = 28;

            var inner = new Panel { AutoSize = false, BackColor = clrMainBg, Location = new Point(0, 0) };
            panelContent.Controls.Add(inner);

            void ResizeInner()
            {
                inner.Width = Math.Max(panelContent.ClientSize.Width, 900);
                const int contentH = 28 + 34 + 14 + 78 + 96 + 32 + 22 + 30 + 180 + 40;
                inner.Height = Math.Max(panelContent.ClientSize.Height, contentH);
            }
            panelContent.Resize += (s, e) => ResizeInner();
            ResizeInner();

            // ── Page heading ──────────────────────────────────────────────────
            inner.Controls.Add(new Label
            {
                Text = "Tableau de bord",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = clrTextDark,
                AutoSize = true,
                Location = new Point(padH, padV)
            });
            inner.Controls.Add(new Label
            {
                Text = "Vue d'ensemble de votre activité",
                Font = new Font("Segoe UI", 10F),
                ForeColor = clrTextMid,
                AutoSize = true,
                Location = new Point(padH, padV + 34)
            });

            // ── Stat cards (placeholders refreshed after DB load) ─────────────
            int statTop = padV + 78;
            const int statW = 190, statH = 96, statGap = 20;

            // Create cards with placeholder text — updated once DB data arrives
            var cardCaMois = CreateStatCard("…", "CA du mois", "…", primaryColor, statW, statH);
            var cardDevis = CreateStatCard("…", "Devis en cours", "…", clrOrange, statW, statH);
            var cardClients = CreateStatCard("…", "Clients actifs", "…", clrGreen, statW, statH);
            var cardImpayees = CreateStatCard("…", "Factures impayées", "…", Color.FromArgb(239, 68, 68), statW, statH);

            Panel[] statCards = { cardCaMois, cardDevis, cardClients, cardImpayees };
            for (int i = 0; i < statCards.Length; i++)
            {
                statCards[i].Location = new Point(padH + i * (statW + statGap), statTop);
                inner.Controls.Add(statCards[i]);
            }

            // Helper: update a stat card's value + trend labels
            static void SetCard(Panel card, string value, string trend)
            {
                var valLbl = card.Controls.Find("lblValue", false).FirstOrDefault() as Label;
                var trendLbl = card.Controls.Find("lblTrend", false).FirstOrDefault() as Label;
                if (valLbl != null) valLbl.Text = value;
                if (trendLbl != null) trendLbl.Text = trend;
                card.Invalidate(true);
            }

            // Load real dashboard data asynchronously
            _ = Task.Run(async () =>
            {
                string ca = "0 €";
                string err = "";

                try
                {
                    var vm = _services.NewUrssafVM();
                    await vm.ChargerAsync();
                    ca = vm.CaMoisCourant.ToString("N0") + " €";
                }
                catch (Exception ex) { err += ex.Message + "\n"; }

                int devis = 0, impayees = 0;
                try
                {
                    var docs = await _services.Documents.ChargerDocumentsAsync();
                    devis = docs.Count(d => d.Type == autofact.Models.TypeDocument.Devis && d.Statut == autofact.Models.StatutDocument.Brouillon);
                    impayees = docs.Count(d => d.Type == autofact.Models.TypeDocument.Facture && d.Statut == autofact.Models.StatutDocument.Envoye);
                }
                catch (Exception ex) { err += ex.Message + "\n"; }

                int nbClients = 0;
                try
                {
                    var clients = await _services.Clients.TousAsync();
                    nbClients = clients.Count;
                }
                catch (Exception ex) { err += ex.Message + "\n"; }

                // Force init DB if schema was missing
                if (err.Contains("doesn't exist"))
                {
                    try { await db.InitializeDatabaseAsync(); } catch { }
                }

                Invoke(() =>
                {
                    if (cardCaMois.IsDisposed) return;
                    SetCard(cardCaMois, ca, "↑ vs mois préc.");
                    SetCard(cardDevis, devis.ToString(), "→ en attente");
                    SetCard(cardClients, nbClients.ToString(), "↑ total");
                    SetCard(cardImpayees, impayees.ToString(), "↓ à relancer");
                });
            });

            // ── Quick Actions heading ─────────────────────────────────────────
            int qaTop = statTop + statH + 32;
            inner.Controls.Add(new Label
            {
                Text = "Actions rapides",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = clrTextDark,
                AutoSize = true,
                Location = new Point(padH, qaTop)
            });

            // ── Quick-action cards — now wired to real actions ─────────────────
            int cardTop = qaTop + 30;
            const int cardW = 170, cardH = 180, cardGap = 18;

            var qa0 = CreateQuickActionCard("Nouveau\nDevis", "document", primaryColor, cardW, cardH);
            var qa1 = CreateQuickActionCard("Nouvelle\nFacture", "clipboard", clrPurple, cardW, cardH);
            var qa2 = CreateQuickActionCard("Ajouter\nClient", "user", clrGreen, cardW, cardH);
            var qa3 = CreateQuickActionCard("Nouvel\nArticle", "cube", clrOrange, cardW, cardH);

            qa0.Click += (s, e) => NavItemClicked(qa0, "Devis");
            qa1.Click += (s, e) => NavItemClicked(qa1, "Facturation");
            qa2.Click += async (s, e) =>
            {
                using var dlg = new FormClientAdd(db);
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ClientAdded)
                    ShowDashboard();
            };
            qa3.Click += async (s, e) =>
            {
                using var dlg = new FormArticleAdd(db);
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ArticleSaved)
                    ShowDashboard();
            };

            Panel[] qaCards = { qa0, qa1, qa2, qa3 };
            for (int i = 0; i < qaCards.Length; i++)
            {
                qaCards[i].Location = new Point(padH + i * (cardW + cardGap), cardTop);
                inner.Controls.Add(qaCards[i]);
            }

            // ── Aperçu rapide card ────────────────────────────────────────────
            const int apercuW = 380, apercuH = cardH + 34 + 30;
            //var apercu = CreateApercuCard(apercuW, apercuH);

            void PlaceApercu()
            {
                int cardsEnd = padH + qaCards.Length * (cardW + cardGap);
                int rightPos = inner.Width - apercuW - padH;
                //apercu.Location = new Point(Math.Max(cardsEnd + 24, rightPos), qaTop);
            }
            inner.Resize += (s, e) => PlaceApercu();
            PlaceApercu();
            //inner.Controls.Add(apercu);
        }

        // ── Stat card ─────────────────────────────────────────────────────────
        private Panel CreateStatCard(string value, string label, string trend, Color accent, int w, int h)
        {
            var card = new Panel { Size = new Size(w, h), BackColor = clrWhite, Cursor = Cursors.Default };

            bool hovered = false;
            card.MouseEnter += (s, e) => { hovered = true; card.Invalidate(); };
            card.MouseLeave += (s, e) => { hovered = false; card.Invalidate(); };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                if (hovered)
                {
                    using var shBr = new SolidBrush(Color.FromArgb(16, 0, 0, 0));
                    using var sp = RoundedRectStatic(new Rectangle(2, 4, w - 4, h - 4), 12);
                    g.FillPath(shBr, sp);
                }

                using var path = RoundedRectStatic(new Rectangle(0, 0, w - 1, h - 1), 12);
                using var bg = new SolidBrush(clrWhite);
                g.FillPath(bg, path);
                using var bdr = new Pen(clrBorder, 1f);
                g.DrawPath(bdr, path);

                // Left accent strip
                using var acBr = new SolidBrush(accent);
                using var acPath = RoundedRectStatic(new Rectangle(0, 0, 4, h - 1), 2);
                g.FillPath(acBr, acPath);
            };

            // Accent icon circle
            var dot = new Panel { Size = new Size(36, 36), Location = new Point(w - 48, 16), BackColor = Color.Transparent };
            dot.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var br = new SolidBrush(Color.FromArgb(25, accent.R, accent.G, accent.B));
                e.Graphics.FillEllipse(br, 0, 0, 35, 35);
                using var pen = new Pen(accent, 1.5f);
                e.Graphics.DrawEllipse(pen, 3, 3, 29, 29);
            };
            card.Controls.Add(dot);

            card.Controls.Add(new Label
            {
                Name = "lblTitle",
                Text = label,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = clrTextLight,
                AutoSize = true,
                Location = new Point(14, 14),
                BackColor = Color.Transparent
            });
            card.Controls.Add(new Label
            {
                Name = "lblValue",
                Text = value,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = clrTextDark,
                AutoSize = true,
                Location = new Point(12, 34),
                BackColor = Color.Transparent
            });
            card.Controls.Add(new Label
            {
                Name = "lblTrend",
                Text = trend,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = trend.StartsWith('↑') ? clrGreen : Color.FromArgb(239, 68, 68),
                AutoSize = true,
                Location = new Point(14, h - 24),
                BackColor = Color.Transparent
            });

            return card;
        }

        // ── Quick-action card ─────────────────────────────────────────────────
        private Panel CreateQuickActionCard(string label, string iconKey, Color accent, int w, int h)
        {
            var card = new Panel { Size = new Size(w, h), BackColor = clrWhite, Cursor = Cursors.Hand };

            bool hovered = false;
            card.MouseEnter += (s, e) => { hovered = true; card.Invalidate(); };
            card.MouseLeave += (s, e) => { hovered = false; card.Invalidate(); };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                if (hovered)
                {
                    using var shBr = new SolidBrush(Color.FromArgb(18, 0, 0, 0));
                    using var sp = RoundedRectStatic(new Rectangle(2, 4, w - 4, h - 4), 14);
                    g.FillPath(shBr, sp);
                }

                using var path = RoundedRectStatic(new Rectangle(0, 0, w - 1, h - 1), 14);
                Color fill = hovered ? Color.FromArgb(252, 252, 255) : clrWhite;
                using var bg = new SolidBrush(fill);
                g.FillPath(bg, path);
                using var bdr = new Pen(hovered ? Color.FromArgb(180, accent.R, accent.G, accent.B) : clrBorder, 1f);
                g.DrawPath(bdr, path);
            };

            // Icon circle
            var iconArea = new Panel { Size = new Size(52, 52), BackColor = Color.Transparent };
            iconArea.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var br = new SolidBrush(Color.FromArgb(20, accent.R, accent.G, accent.B));
                e.Graphics.FillEllipse(br, 0, 0, 51, 51);
                using var pen = new Pen(Color.FromArgb(60, accent.R, accent.G, accent.B), 1.5f);
                e.Graphics.DrawEllipse(pen, 1, 1, 49, 49);
                using var f = new Font("Segoe UI", 20F);
                string icon = GetNavIcon(iconKey);
                SizeF sz = e.Graphics.MeasureString(icon, f);
                using var tb = new SolidBrush(accent);
                e.Graphics.DrawString(icon, f, tb,
                    (52 - sz.Width) / 2f, (52 - sz.Height) / 2f);
            };
            card.Controls.Add(iconArea);

            var lbl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = clrTextDark,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lbl);

            // "+" badge
            var plus = new Panel { Size = new Size(26, 26), BackColor = Color.Transparent };
            plus.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var br = new SolidBrush(accent);
                e.Graphics.FillEllipse(br, 0, 0, 25, 25);
                using var f = new Font("Segoe UI", 13F, FontStyle.Bold);
                using var tb = new SolidBrush(Color.White);
                SizeF sz = e.Graphics.MeasureString("+", f);
                e.Graphics.DrawString("+", f, tb,
                    (26 - sz.Width) / 2f, (26 - sz.Height) / 2f - 1);
            };
            card.Controls.Add(plus);

            card.Resize += (s, e) =>
            {
                iconArea.Location = new Point((w - 52) / 2, 28);
                lbl.Size = new Size(w - 16, 36);
                lbl.Location = new Point(8, 88);
                plus.Location = new Point(w - 34, h - 34);
            };
            iconArea.Location = new Point((w - 52) / 2, 28);
            lbl.Size = new Size(w - 16, 36);
            lbl.Location = new Point(8, 88);
            plus.Location = new Point(w - 34, h - 34);

            return card;
        }

        // ── Aperçu rapide card ────────────────────────────────────────────────
        /*private Panel CreateApercuCard(int w, int h)
        {
            var card = new Panel { Size = new Size(w, h), BackColor = clrWhite };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRectStatic(new Rectangle(0, 0, w - 1, h - 1), 14);
                using var bg   = new SolidBrush(clrWhite);
                g.FillPath(bg, path);
                using var bdr  = new Pen(clrBorder, 1f);
                g.DrawPath(bdr, path);
            };

            card.Controls.Add(new Label
            {
                Text      = "Aperçu rapide",
                Font      = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = clrTextDark,
                AutoSize  = true,
                Location  = new Point(20, 16),
                BackColor = Color.Transparent
            });
            card.Controls.Add(new Label
            {
                Text      = "Activité récente",
                Font      = new Font("Segoe UI", 8.5F),
                ForeColor = clrTextLight,
                AutoSize  = true,
                Location  = new Point(20, 36),
                BackColor = Color.Transparent
            });
            card.Controls.Add(new Label
            {
                Text      = "201 943 €",
                Font      = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = clrTextDark,
                AutoSize  = true,
                Location  = new Point(20, 56),
                BackColor = Color.Transparent
            });

            var btnFilter = new Button
            {
                Text      = "Ce mois ▾",
                Font      = new Font("Segoe UI", 8.5F),
                ForeColor = clrTextMid,
                BackColor = clrWhite,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(90, 26),
                Location  = new Point(w - 104, 16),
                Cursor    = Cursors.Hand
            };
            btnFilter.FlatAppearance.BorderColor = clrBorder;
            card.Controls.Add(btnFilter);

            var chartArea = new Panel
            {
                Location  = new Point(12, 90),
                Size      = new Size(w - 24, h - 116),
                BackColor = Color.Transparent
            };
            chartArea.Paint += (s, e) => DrawApercuChart(e.Graphics, chartArea.ClientRectangle);
            card.Controls.Add(chartArea);

            return card;
        }

        private void DrawApercuChart(Graphics g, Rectangle r)
        {
            if (r.Width <= 0 || r.Height <= 0) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Background
            using var bgBr   = new SolidBrush(Color.FromArgb(250, 251, 252));
            using var bgPath = RoundedRectStatic(r, 8);
            g.FillPath(bgBr, bgPath);

            // Horizontal grid lines
            using var gridPen = new Pen(Color.FromArgb(235, 237, 240), 1f) { DashStyle = DashStyle.Dash };
            for (int i = 1; i <= 3; i++)
            {
                int gy = r.Top + (int)(r.Height * i / 4f);
                g.DrawLine(gridPen, r.Left + 8, gy, r.Right - 8, gy);
            }

            float[] ys    = { 0.70f, 0.42f, 0.58f, 0.28f, 0.52f, 0.18f, 0.35f };
            int     n     = ys.Length;
            float   xStep = (r.Width - 20f) / (n - 1);

            var pts = new PointF[n];
            for (int i = 0; i < n; i++)
                pts[i] = new PointF(r.Left + 10 + i * xStep,
                                    r.Top + 8 + ys[i] * (r.Height - 30));

            // Area fill — close the polygon down to the baseline then use a cardinal spline
            using var fillBr = new LinearGradientBrush(
                new PointF(0, r.Top), new PointF(0, r.Bottom),
                Color.FromArgb(55, primaryColor), Color.FromArgb(4, primaryColor));

            using var fillPath = new GraphicsPath();
            fillPath.AddLine(pts[0].X, r.Bottom - 14, pts[0].X, pts[0].Y);
            fillPath.AddCurve(pts, 0.4f);
            fillPath.AddLine(pts[n - 1].X, pts[n - 1].Y, pts[n - 1].X, r.Bottom - 14);
            fillPath.CloseFigure();
            g.FillPath(fillBr, fillPath);

            // Smooth line
            using var linePen = new Pen(primaryColor, 2.2f);
            g.DrawCurve(linePen, pts, 0.4f);

            // Dots
            foreach (var pt in pts)
            {
                g.FillEllipse(Brushes.White, pt.X - 4, pt.Y - 4, 8, 8);
                using var dotPen = new Pen(primaryColor, 2f);
                g.DrawEllipse(dotPen, pt.X - 4, pt.Y - 4, 8, 8);
            }

            // X-axis month labels
            string[] months = { "Jan", "Fév", "Mar", "Avr", "Mai", "Jun", "Jul" };
            using var lf = new Font("Segoe UI", 7.5F);
            using var lb = new SolidBrush(clrTextLight);
            for (int i = 0; i < n; i++)
            {
                var lr = new RectangleF(pts[i].X - 16, r.Bottom - 14, 32, 14);
                g.DrawString(months[i], lf, lb, lr,
                    new StringFormat { Alignment = StringAlignment.Center });
            }
        }*/

        // ══════════════════════════════════════════════════════════════════════
        // CLIENTS VIEW
        // ══════════════════════════════════════════════════════════════════════
        private void ShowClientsView()
        {
            _currentViewType = "clients";  // Track view type
            panelContent.Controls.Clear();
            panelContent.AutoScroll = true;
            panelContent.Invalidate();

            // Header band - docked at top
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 88,
                BackColor = clrWhite,
                Padding = new Padding(0)
            };
            header.Paint += (s, e) =>
            {
                using var pen = new Pen(clrBorder);
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };
            panelContent.Controls.Add(header);

            header.Controls.Add(new Label
            {
                Text = "Gestion des clients",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = clrTextDark,
                AutoSize = true,
                Location = new Point(28, 16)
            });

            // Client count badge
            var badge = new Panel { Size = new Size(60, 22), Location = new Point(246, 20), BackColor = Color.Transparent };
            badge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var br = new SolidBrush(primaryLight);
                using var path = RoundedRectStatic(new Rectangle(0, 0, badge.Width - 1, badge.Height - 1), 11);
                e.Graphics.FillPath(br, path);
                int count = lvClients?.Items.Count ?? 0;
                TextRenderer.DrawText(e.Graphics, count.ToString(), new Font("Segoe UI", 8F, FontStyle.Bold),
                    badge.ClientRectangle, primaryColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            header.Controls.Add(badge);

            header.Controls.Add(new Label
            {
                Text = "Liste et gestion de vos clients enregistrés",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = clrTextMid,
                AutoSize = true,
                Location = new Point(28, 50)
            });

            // Body - fills remaining space
            var body = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = clrMainBg,
                Padding = new Padding(24, 16, 24, 16),
                AutoScroll = false
            };
            panelContent.Controls.Add(body);
            body.BringToFront();

            // Actions bar
            var actions = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent };
            body.Controls.Add(actions);

            // "Ajouter client" button
            btnAddClient = new Button
            {
                Text = "＋  Ajouter un client",
                Height = 38,
                Width = 168,
                Left = 0,
                Top = 7,
                BackColor = primaryColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddClient.FlatAppearance.BorderSize = 0;
            btnAddClient.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRectStatic(
                    new Rectangle(0, 0, btnAddClient.Width - 1, btnAddClient.Height - 1), 19);
                using var br = new SolidBrush(btnAddClient.BackColor);
                g.FillPath(br, path);
                TextRenderer.DrawText(g, btnAddClient.Text, btnAddClient.Font,
                    btnAddClient.ClientRectangle, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnAddClient.MouseEnter += (s, e) => { btnAddClient.BackColor = Color.FromArgb(37, 99, 235); btnAddClient.Invalidate(); };
            btnAddClient.MouseLeave += (s, e) => { btnAddClient.BackColor = primaryColor; btnAddClient.Invalidate(); };
            btnAddClient.Click += async (s, e) =>
            {
                using var dlg = new FormClientAdd(db);
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ClientAdded)
                    await LoadClientsAsync();
            };
            actions.Controls.Add(btnAddClient);

            // ListView wrapped in rounded panel
            var listWrap = new Panel { Dock = DockStyle.Fill, BackColor = clrWhite };
            listWrap.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRectStatic(
                    new Rectangle(0, 0, listWrap.Width - 1, listWrap.Height - 1), 12);
                using var bg = new SolidBrush(clrWhite);
                e.Graphics.FillPath(bg, path);
                using var bdr = new Pen(clrBorder);
                e.Graphics.DrawPath(bdr, path);
            };
            body.Controls.Add(listWrap);
            listWrap.BringToFront();

            lvClients = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BorderStyle = BorderStyle.None,
                OwnerDraw = true,
                Font = new Font("Segoe UI", 9.5F),
                BackColor = clrWhite,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            lvClients.Columns.Add("ID", 55);
            lvClients.Columns.Add("Nom", 200);
            lvClients.Columns.Add("Email", 200);
            lvClients.Columns.Add("Téléphone", 130);
            lvClients.Columns.Add("Adresse", 300);

            // Owner-draw header
            lvClients.DrawColumnHeader += (s, e) =>
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), e.Bounds);
                using var pen = new Pen(clrBorder);
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1,
                                         e.Bounds.Right, e.Bounds.Bottom - 1);
                TextRenderer.DrawText(e.Graphics, e.Header.Text,
                    new Font("Segoe UI", 9F, FontStyle.Bold),
                    new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 10, e.Bounds.Height),
                    clrTextMid,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };

            // Use default WinForms rendering - it's stable and flicker-free
            lvClients.OwnerDraw = false;

            // Double-click to edit
            lvClients.DoubleClick += async (s, e) =>
            {
                if (lvClients.SelectedItems.Count == 0) return;
                var item = lvClients.SelectedItems[0];
                if (!int.TryParse(item.Text, out int id)) return;

                using var dlg = new FormClientEdit(db, id, item.SubItems[1].Text,
                                                                 item.SubItems[2].Text,
                                                                 item.SubItems[3].Text,
                                                                 item.SubItems[4].Text);
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ClientModified)
                    await LoadClientsAsync();
            };

            // Context menu
            var ctxMenu = new ContextMenuStrip();
            var miEdit = new ToolStripMenuItem("✏  Modifier");
            var miDelete = new ToolStripMenuItem("🗑  Supprimer");
            ctxMenu.Items.Add(miEdit);
            ctxMenu.Items.Add(new ToolStripSeparator());
            ctxMenu.Items.Add(miDelete);

            miEdit.Click += async (s, e) =>
            {
                if (lvClients.SelectedItems.Count == 0) return;
                var item = lvClients.SelectedItems[0];
                if (!int.TryParse(item.Text, out int id)) return;
                using var dlg = new FormClientEdit(db, id, item.SubItems[1].Text,
                                                                 item.SubItems[2].Text,
                                                                 item.SubItems[3].Text,
                                                                 item.SubItems[4].Text);
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ClientModified)
                    await LoadClientsAsync();
            };

            miDelete.Click += async (s, e) =>
            {
                if (lvClients.SelectedItems.Count == 0) return;
                var item = lvClients.SelectedItems[0];
                if (!int.TryParse(item.Text, out int id)) return;
                if (MessageBox.Show(
                        $"Supprimer le client « {item.SubItems[1].Text} » ?",
                        "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                    != DialogResult.Yes) return;
                try
                {
                    await _services.Clients.SupprimerAsync(id);
                    await LoadClientsAsync();
                    badge.Invalidate();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur suppression : " + ex.Message,
                        "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            lvClients.ContextMenuStrip = ctxMenu;
            lvClients.ItemSelectionChanged += (s, e) => { lvClients.Invalidate(); badge.Invalidate(); };
            listWrap.Controls.Add(lvClients);
        }

        private async Task LoadClientsAsync()
        {
            if (lvClients == null) return;
            lvClients.Items.Clear();

            try
            {
                var clients = await _services.Clients.TousAsync();
                foreach (var c in clients)
                {
                    var lvi = new ListViewItem(c.Id.ToString());
                    lvi.SubItems.Add(c.Nom);
                    lvi.SubItems.Add(c.Email ?? "");
                    lvi.SubItems.Add(c.Telephone ?? "");
                    lvi.SubItems.Add(c.Adresse ?? "");
                    lvClients.Items.Add(lvi);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement clients : " + ex.Message,
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // ARTICLES VIEW
        // ══════════════════════════════════════════════════════════════════════
        private void ShowArticlesView()
        {
            _currentViewType = "articles";  // Track view type
            panelContent.Controls.Clear();
            panelContent.AutoScroll = true;

            // Header band
            var header = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = clrWhite };
            header.Paint += (s, e) =>
            {
                using var pen = new Pen(clrBorder);
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };
            panelContent.Controls.Add(header);

            header.Controls.Add(new Label
            {
                Text = "Catalogue articles",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = clrTextDark,
                AutoSize = true,
                Location = new Point(28, 16)
            });

            var badge = new Panel { Size = new Size(62, 22), Location = new Point(254, 20), BackColor = Color.Transparent };
            badge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var br = new SolidBrush(Color.FromArgb(255, 247, 237));
                using var path = RoundedRectStatic(new Rectangle(0, 0, badge.Width - 1, badge.Height - 1), 11);
                e.Graphics.FillPath(br, path);
                TextRenderer.DrawText(e.Graphics, "articles", new Font("Segoe UI", 8F, FontStyle.Bold),
                    badge.ClientRectangle, clrOrange,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            header.Controls.Add(badge);

            header.Controls.Add(new Label
            {
                Text = "Produits et services de votre catalogue tarifaire",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = clrTextMid,
                AutoSize = true,
                Location = new Point(28, 50)
            });

            // Body
            var body = new Panel { Dock = DockStyle.Fill, BackColor = clrMainBg, Padding = new Padding(24, 16, 24, 16) };
            panelContent.Controls.Add(body);
            body.BringToFront();

            // Actions bar
            var actions = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent };
            body.Controls.Add(actions);

            // "Ajouter article" pill button
            btnAddArticle = new Button
            {
                Text = "＋  Ajouter un article",
                Height = 38,
                Width = 172,
                Left = 0,
                Top = 7,
                BackColor = clrOrange,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddArticle.FlatAppearance.BorderSize = 0;
            btnAddArticle.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRectStatic(
                    new Rectangle(0, 0, btnAddArticle.Width - 1, btnAddArticle.Height - 1), 19);
                using var br = new SolidBrush(btnAddArticle.BackColor);
                g.FillPath(br, path);
                TextRenderer.DrawText(g, btnAddArticle.Text, btnAddArticle.Font,
                    btnAddArticle.ClientRectangle, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            var orangeHover = Color.FromArgb(217, 119, 6);
            btnAddArticle.MouseEnter += (s, e) => { btnAddArticle.BackColor = orangeHover; btnAddArticle.Invalidate(); };
            btnAddArticle.MouseLeave += (s, e) => { btnAddArticle.BackColor = clrOrange; btnAddArticle.Invalidate(); };
            btnAddArticle.Click += async (s, e) =>
            {
                using var dlg = new FormArticleAdd(db);
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ArticleSaved)
                    await LoadArticlesAsync();
            };
            actions.Controls.Add(btnAddArticle);

            // ListView wrapper
            var listWrap = new Panel { Dock = DockStyle.Fill, BackColor = clrWhite };
            listWrap.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRectStatic(
                    new Rectangle(0, 0, listWrap.Width - 1, listWrap.Height - 1), 12);
                using var bg = new SolidBrush(clrWhite);
                e.Graphics.FillPath(bg, path);
                using var bdr = new Pen(clrBorder);
                e.Graphics.DrawPath(bdr, path);
            };
            body.Controls.Add(listWrap);
            listWrap.BringToFront();

            lvArticles = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BorderStyle = BorderStyle.None,
                OwnerDraw = true,
                Font = new Font("Segoe UI", 9.5F),
                BackColor = clrWhite,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            lvArticles.Columns.Add("ID", 55);
            lvArticles.Columns.Add("Nom", 240);
            lvArticles.Columns.Add("Type", 180);
            lvArticles.Columns.Add("Prix unitaire", 130);

            lvArticles.DrawColumnHeader += (s, e) =>
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), e.Bounds);
                using var pen = new Pen(clrBorder);
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                TextRenderer.DrawText(e.Graphics, e.Header.Text,
                    new Font("Segoe UI", 9F, FontStyle.Bold),
                    new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 10, e.Bounds.Height),
                    clrTextMid,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };

            lvArticles.DrawItem += (s, e) =>
            {
                e.DrawDefault = true;  // Let WinForms draw everything - it's stable
            };

            lvArticles.DrawSubItem += (s, e) =>
            {
                e.DrawDefault = true;  // Let WinForms draw everything - it's stable
            };

            // Double-click to edit
            lvArticles.DoubleClick += async (s, e) =>
            {
                if (lvArticles.SelectedItems.Count == 0) return;
                var item = lvArticles.SelectedItems[0];
                if (!int.TryParse(item.Text, out int id)) return;

                string nom = item.SubItems[1].Text;
                string type = item.SubItems[2].Text;
                string prixS = item.SubItems[3].Text.Replace(" €", "").Replace(',', '.');
                decimal.TryParse(prixS,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal prix);

                using var dlg = new FormArticleAdd(db, id, nom, type, prix);
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ArticleSaved)
                    await LoadArticlesAsync();
            };

            // Context menu: Edit + Delete
            var ctxMenu = new ContextMenuStrip();
            var miEdit = new ToolStripMenuItem("✏  Modifier");
            var miDelete = new ToolStripMenuItem("🗑  Supprimer");
            ctxMenu.Items.Add(miEdit);
            ctxMenu.Items.Add(new ToolStripSeparator());
            ctxMenu.Items.Add(miDelete);

            miEdit.Click += async (s, e) =>
            {
                if (lvArticles.SelectedItems.Count == 0) return;
                var item = lvArticles.SelectedItems[0];
                if (!int.TryParse(item.Text, out int id)) return;
                string nom = item.SubItems[1].Text;
                string type = item.SubItems[2].Text;
                string prixS = item.SubItems[3].Text.Replace(" €", "").Replace(',', '.');
                decimal.TryParse(prixS,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal prix);
                using var dlg = new FormArticleAdd(db, id, nom, type, prix);
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ArticleSaved)
                    await LoadArticlesAsync();
            };

            miDelete.Click += async (s, e) =>
            {
                if (lvArticles.SelectedItems.Count == 0) return;
                var item = lvArticles.SelectedItems[0];
                if (!int.TryParse(item.Text, out int id)) return;
                if (MessageBox.Show(
                        $"Supprimer l'article « {item.SubItems[1].Text} » ?",
                        "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                    != DialogResult.Yes) return;
                try
                {
                    await _services.Prestations.SupprimerAsync(id);
                    await LoadArticlesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur suppression : " + ex.Message,
                        "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            lvArticles.ContextMenuStrip = ctxMenu;
            lvArticles.ItemSelectionChanged += (s, e) => lvArticles.Invalidate();

            listWrap.Controls.Add(lvArticles);
        }

        private async Task LoadArticlesAsync()
        {
            if (lvArticles == null) return;
            lvArticles.Items.Clear();

            try
            {
                var prestations = await _services.Prestations.ToutesAsync();
                foreach (var p in prestations)
                {
                    var lvi = new ListViewItem(p.Id.ToString());
                    lvi.SubItems.Add(p.Nom);
                    lvi.SubItems.Add(p.Type);
                    lvi.SubItems.Add(p.PrixUnitaire.ToString("F2",
                        System.Globalization.CultureInfo.InvariantCulture) + " €");
                    lvArticles.Items.Add(lvi);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement articles : " + ex.Message,
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // DOCUMENTS VIEW  (Devis / Factures)
        // ══════════════════════════════════════════════════════════════════════
        private ListView lvDocuments = null!;
        private autofact.Models.TypeDocument _currentDocType;

        private void ShowDocumentsView(
            autofact.Models.TypeDocument type, string titre, string iconKey, Color accent)
        {
            _currentViewType = "documents";  // Track view type
            _currentDocType = type;
            panelContent.Controls.Clear();
            panelContent.AutoScroll = true;

            // ── Header ────────────────────────────────────────────────────────
            var header = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = clrWhite };
            header.Paint += (s, e) =>
            {
                using var pen = new Pen(clrBorder);
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };
            panelContent.Controls.Add(header);

            header.Controls.Add(new Label
            {
                Text = titre,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = clrTextDark,
                AutoSize = true,
                Location = new Point(28, 16)
            });
            header.Controls.Add(new Label
            {
                Text = type == autofact.Models.TypeDocument.Devis
                    ? "Création et suivi de vos devis"
                    : "Création et suivi de vos factures",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = clrTextMid,
                AutoSize = true,
                Location = new Point(28, 50)
            });

            // ── Body ──────────────────────────────────────────────────────────
            var body = new Panel { Dock = DockStyle.Fill, BackColor = clrMainBg, Padding = new Padding(24, 16, 24, 16) };
            panelContent.Controls.Add(body);
            body.BringToFront();

            // ── Actions bar ───────────────────────────────────────────────────
            var actBar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent };
            body.Controls.Add(actBar);

            string btnLabel = type == autofact.Models.TypeDocument.Devis
                ? "＋  Nouveau devis" : "＋  Nouvelle facture";

            var btnNew = new Button
            {
                Text = btnLabel,
                Height = 38,
                Width = 180,
                Left = 0,
                BackColor = accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNew.FlatAppearance.BorderSize = 0;
            btnNew.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var path = RoundedRectStatic(new Rectangle(0, 0, btnNew.Width - 1, btnNew.Height - 1), 19);
                using var br = new SolidBrush(btnNew.BackColor);
                e.Graphics.FillPath(br, path);
                TextRenderer.DrawText(e.Graphics, btnNew.Text, btnNew.Font,
                    btnNew.ClientRectangle, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnNew.MouseEnter += (s, e) => { btnNew.BackColor = ControlPaint.Dark(accent, 0.08f); btnNew.Invalidate(); };
            btnNew.MouseLeave += (s, e) => { btnNew.BackColor = accent; btnNew.Invalidate(); };
            btnNew.Click += async (s, e) =>
            {
                using var dlg = new FormDocumentAdd(db, type);
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.DocumentSaved)
                {
                    await LoadDocumentsAsync();
                    // Refresh dashboard stats after document creation
                    _ = RefreshDashboardStatsAsync();
                }
            };
            actBar.Controls.Add(btnNew);

            // Filtre URSSAF → ouvre la vue URSSAF
            var btnUrssaf = new Button
            {
                Text = "📊  Tableau URSSAF",
                Height = 38,
                Width = 168,
                Left = 192,
                Top = 7,
                BackColor = clrWhite,
                ForeColor = clrTextMid,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F),
                Cursor = Cursors.Hand
            };
            btnUrssaf.FlatAppearance.BorderColor = clrBorder;
            btnUrssaf.Click += (s, e) => ShowUrssafView();
            actBar.Controls.Add(btnUrssaf);

            // ── ListView ──────────────────────────────────────────────────────
            var listWrap = new Panel { Dock = DockStyle.Fill, BackColor = clrWhite };
            listWrap.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var path = RoundedRectStatic(new Rectangle(0, 0, listWrap.Width - 1, listWrap.Height - 1), 12);
                using var bg = new SolidBrush(clrWhite);
                e.Graphics.FillPath(bg, path);
                using var bdr = new Pen(clrBorder);
                e.Graphics.DrawPath(bdr, path);
            };
            body.Controls.Add(listWrap);
            listWrap.BringToFront();

            lvDocuments = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BorderStyle = BorderStyle.None,
                OwnerDraw = true,
                Font = new Font("Segoe UI", 9.5F),
                BackColor = clrWhite,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            lvDocuments.Columns.Add("Numéro", 130);
            lvDocuments.Columns.Add("Client", 200);
            lvDocuments.Columns.Add("Date", 90);
            lvDocuments.Columns.Add("Échéance", 90);
            lvDocuments.Columns.Add("Total HT", 110);
            lvDocuments.Columns.Add("Statut", 110);

            lvDocuments.DrawColumnHeader += (s, e) =>
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), e.Bounds);
                using var pen = new Pen(clrBorder);
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                TextRenderer.DrawText(e.Graphics, e.Header.Text,
                    new Font("Segoe UI", 9F, FontStyle.Bold),
                    new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 10, e.Bounds.Height),
                    clrTextMid, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };

            // Use default WinForms rendering - it's stable and flicker-free
            lvDocuments.OwnerDraw = false;

            // Context menu: Marquer payé / Créer avoir / Exporter PDF
            var ctx = new ContextMenuStrip();
            var miModifier = new ToolStripMenuItem("✏️  Modifier");
            var miPaye = new ToolStripMenuItem("✅  Marquer comme payée");
            var miAvoir = new ToolStripMenuItem("🔄  Créer un avoir");
            var miPdf = new ToolStripMenuItem("📄  Exporter en PDF");
            var miSupprimer = new ToolStripMenuItem("🗑️  Supprimer");
            ctx.Items.AddRange(new ToolStripItem[] { miModifier, new ToolStripSeparator(), miPaye, miAvoir, new ToolStripSeparator(), miPdf, new ToolStripSeparator(), miSupprimer });
            lvDocuments.ContextMenuStrip = ctx;

            miModifier.Click += async (s, e) =>
            {
                if (lvDocuments.SelectedItems.Count == 0) return;
                if (!int.TryParse(lvDocuments.SelectedItems[0].Tag?.ToString(), out int docId)) return;

                try
                {
                    using var dlg = new FormDocumentAdd(db, _currentDocType, docId);
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        await LoadDocumentsAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur : " + ex.Message, "Erreur",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            miPaye.Click += async (s, e) =>
            {
                if (lvDocuments.SelectedItems.Count == 0) return;
                if (!int.TryParse(lvDocuments.SelectedItems[0].Tag?.ToString(), out int docId)) return;
                await _services.Documents.MettreAJourStatutAsync(docId, autofact.Models.StatutDocument.Paye);
                await LoadDocumentsAsync();
            };

            miAvoir.Click += async (s, e) =>
            {
                if (lvDocuments.SelectedItems.Count == 0) return;
                if (!int.TryParse(lvDocuments.SelectedItems[0].Tag?.ToString(), out int docId)) return;
                var avoirVm = _services.NewAvoirVM();
                bool ok = await avoirVm.InitialiserDepuisFactureAsync(docId);
                if (!ok) { MessageBox.Show(avoirVm.Erreur, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (MessageBox.Show(
                        $"Créer un avoir {avoirVm.Avoir?.Numero} pour {avoirVm.FactureSourceNumero} ?",
                        "Confirmer l'avoir", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                    == DialogResult.Yes)
                {
                    bool confirmed = await avoirVm.ConfirmerAsync();
                    if (!confirmed) MessageBox.Show(avoirVm.Erreur, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else await LoadDocumentsAsync();
                }
            };

            miPdf.Click += async (s, e) =>
            {
                if (lvDocuments.SelectedItems.Count == 0) return;
                if (!int.TryParse(lvDocuments.SelectedItems[0].Tag?.ToString(), out int docId)) return;

                // Load document with lines
                var docs = await _services.Documents.ChargerDocumentsAsync();
                var doc = docs.FirstOrDefault(d => d.Id == docId);
                if (doc == null) return;
                doc.Lignes = await _services.Documents.ChargerLignesAsync(docId);

                using var sfd = new SaveFileDialog
                {
                    Title = "Exporter la facture en PDF",
                    Filter = "PDF|*.pdf",
                    FileName = doc.Numero + ".pdf",
                    DefaultExt = "pdf"
                };
                if (sfd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    var entreprise = new autofact.Services.InfosEntreprise
                    {
                        Nom = "Mon Entreprise",
                        Adresse = "1 rue de l'Exemple, 75000 Paris",
                        Telephone = "06 00 00 00 00",
                        Email = currentUserEmail ?? string.Empty
                    };
                    autofact.Services.PdfService.GenererPdf(doc, entreprise, sfd.FileName);
                    MessageBox.Show("PDF généré : " + sfd.FileName, "Succès",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur PDF : " + ex.Message, "Erreur",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            miSupprimer.Click += async (s, e) =>
            {
                if (lvDocuments.SelectedItems.Count == 0) return;
                if (!int.TryParse(lvDocuments.SelectedItems[0].Tag?.ToString(), out int docId)) return;

                var docs = await _services.Documents.ChargerDocumentsAsync();
                var doc = docs.FirstOrDefault(d => d.Id == docId);
                if (doc == null) return;

                var result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer le document {doc.Numero} ?\n\nCette action est irréversible.",
                    "Confirmer la suppression",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        await _services.Documents.SupprimerDocumentAsync(docId);
                        MessageBox.Show("Document supprimé avec succès.", "Succès",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await LoadDocumentsAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Erreur lors de la suppression : " + ex.Message, "Erreur",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            lvDocuments.ItemSelectionChanged += (s, e) => lvDocuments.Invalidate();
            listWrap.Controls.Add(lvDocuments);

            // Trigger initial load
            _ = LoadDocumentsAsync();
        }

        private async Task LoadDocumentsAsync()
        {
            if (lvDocuments == null) return;
            lvDocuments.Items.Clear();
            try
            {
                var docs = await _services.Documents.ChargerDocumentsAsync(filtre: _currentDocType);
                foreach (var d in docs)
                {
                    // Load lignes to compute TotalHT
                    d.Lignes = await _services.Documents.ChargerLignesAsync(d.Id);

                    var lvi = new ListViewItem(d.Numero)
                    {
                        Tag = d.Id.ToString()
                    };
                    lvi.SubItems.Add(d.ClientNom);
                    lvi.SubItems.Add(d.DateEmission.ToString("dd/MM/yyyy"));
                    lvi.SubItems.Add(d.DateEcheance.HasValue ? d.DateEcheance.Value.ToString("dd/MM/yyyy") : "—");
                    lvi.SubItems.Add(d.TotalHT.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + " €");
                    lvi.SubItems.Add(d.Statut.ToString());
                    lvDocuments.Items.Add(lvi);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement documents : " + ex.Message,
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // URSSAF VIEW
        // ══════════════════════════════════════════════════════════════════════
        private void ShowUrssafView()
        {
            _currentViewType = "urssaf";  // Track view type
            panelContent.Controls.Clear();
            panelContent.AutoScroll = false;
            lblPageTitle.Text = "Tableau URSSAF";

            // ── Header ────────────────────────────────────────────────────────
            var header = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = clrWhite };
            header.Paint += (s, e) =>
            {
                using var pen = new Pen(clrBorder);
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };
            panelContent.Controls.Add(header);
            header.Controls.Add(new Label
            {
                Text = "Tableau de bord URSSAF",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = clrTextDark,
                AutoSize = true,
                Location = new Point(28, 16)
            });
            header.Controls.Add(new Label
            {
                Text = "Cumul CA, cotisations et net perçu pour l'année en cours",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = clrTextMid,
                AutoSize = true,
                Location = new Point(28, 50)
            });

            // ── Body (scrollable) ─────────────────────────────────────────────
            var body = new Panel { Dock = DockStyle.Fill, BackColor = clrMainBg, AutoScroll = true };
            panelContent.Controls.Add(body);
            body.BringToFront();

            // Placeholder labels — updated once data loads
            var lblCaAnnuel = MakeUrssafKpi("CA annuel", "…", primaryColor);
            var lblCotisation = MakeUrssafKpi("Cotisations URSSAF", "…", Color.FromArgb(239, 68, 68));
            var lblNet = MakeUrssafKpi("Net perçu", "…", clrGreen);
            var lblCaMois = MakeUrssafKpi("CA mois courant", "…", clrOrange);

            const int kpiW = 210, kpiH = 90, kpiGap = 20, kpiTop = 24, kpiLeft = 24;
            Panel[] kpis = { lblCaAnnuel, lblCotisation, lblNet, lblCaMois };
            for (int i = 0; i < kpis.Length; i++)
            {
                kpis[i].Size = new Size(kpiW, kpiH);
                kpis[i].Location = new Point(kpiLeft + i * (kpiW + kpiGap), kpiTop);
                body.Controls.Add(kpis[i]);
            }

            // Trimestre cards (4 panels)
            int trimTop = kpiTop + kpiH + 24;
            var trimPanels = new Panel[4];
            for (int t = 0; t < 4; t++)
            {
                var tp = new Panel
                {
                    Size = new Size(kpiW, 110),
                    BackColor = clrWhite,
                    Location = new Point(kpiLeft + t * (kpiW + kpiGap), trimTop)
                };
                tp.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using var path = RoundedRectStatic(new Rectangle(0, 0, tp.Width - 1, tp.Height - 1), 12);
                    using var bg = new SolidBrush(clrWhite);
                    e.Graphics.FillPath(bg, path);
                    using var bdr = new Pen(clrBorder);
                    e.Graphics.DrawPath(bdr, path);
                };
                tp.Controls.Add(new Label
                {
                    Text = $"T{t + 1}",
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = clrTextLight,
                    AutoSize = true,
                    Location = new Point(14, 10),
                    BackColor = Color.Transparent
                });
                tp.Controls.Add(new Label
                {
                    Name = "caVal",
                    Text = "…",
                    Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                    ForeColor = clrTextDark,
                    AutoSize = true,
                    Location = new Point(14, 28),
                    BackColor = Color.Transparent
                });
                tp.Controls.Add(new Label
                {
                    Name = "cotVal",
                    Text = "Cot. …",
                    Font = new Font("Segoe UI", 8.5F),
                    ForeColor = Color.FromArgb(239, 68, 68),
                    AutoSize = true,
                    Location = new Point(14, 60),
                    BackColor = Color.Transparent
                });
                tp.Controls.Add(new Label
                {
                    Name = "netVal",
                    Text = "Net …",
                    Font = new Font("Segoe UI", 8.5F),
                    ForeColor = clrGreen,
                    AutoSize = true,
                    Location = new Point(14, 80),
                    BackColor = Color.Transparent
                });
                trimPanels[t] = tp;
                body.Controls.Add(tp);
            }

            // CA par client list
            int caTop = trimTop + 110 + 24;
            body.Controls.Add(new Label
            {
                Text = "CA par client",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = clrTextDark,
                AutoSize = true,
                Location = new Point(kpiLeft, caTop)
            });

            var lvCaClient = new ListView
            {
                Location = new Point(kpiLeft, caTop + 28),
                Size = new Size(kpiW * 4 + kpiGap * 3, 180),
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5F),
                BackColor = clrWhite,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            lvCaClient.Columns.Add("Client", 280);
            lvCaClient.Columns.Add("CA HT", 150);
            lvCaClient.Columns.Add("Cotisations", 150);
            lvCaClient.Columns.Add("Net perçu", 150);
            body.Controls.Add(lvCaClient);

            // Load real data
            _ = Task.Run(async () =>
            {
                try
                {
                    var vm = _services.NewUrssafVM();
                    await vm.ChargerAsync();

                    Invoke(() =>
                    {
                        // Update KPI cards
                        SetUrssafKpi(lblCaAnnuel, vm.CaAnnuel.ToString("N2") + " €");
                        SetUrssafKpi(lblCotisation, vm.CotisationAnnuelle.ToString("N2") + " €");
                        SetUrssafKpi(lblNet, vm.NetAnnuel.ToString("N2") + " €");
                        SetUrssafKpi(lblCaMois, vm.CaMoisCourant.ToString("N2") + " €");

                        // Update trimestre panels
                        for (int t = 0; t < 4 && t < vm.Trimestres.Length; t++)
                        {
                            var tr = vm.Trimestres[t];
                            var labels = trimPanels[t].Controls.OfType<Label>().ToArray();
                            if (labels.Length >= 4)
                            {
                                labels[1].Text = tr.CaBrut.ToString("N2") + " €";
                                labels[2].Text = "Cot. " + tr.Cotisation.ToString("N2") + " €";
                                labels[3].Text = "Net " + tr.NetPercu.ToString("N2") + " €";
                            }
                        }

                        // Update CA par client
                        lvCaClient.Items.Clear();
                        foreach (var (_, nom, ca) in vm.CaParClient)
                        {
                            var lvi = new ListViewItem(nom);
                            lvi.SubItems.Add(ca.ToString("N2") + " €");
                            lvi.SubItems.Add(autofact.Services.UrssafService.Cotisation(ca).ToString("N2") + " €");
                            lvi.SubItems.Add(autofact.Services.UrssafService.NetApresUrssaf(ca).ToString("N2") + " €");
                            lvCaClient.Items.Add(lvi);
                        }
                    });
                }
                catch { /* DB unavailable — keep placeholders */ }
            });
        }

        private Panel MakeUrssafKpi(string label, string value, Color accent)
        {
            var p = new Panel { BackColor = clrWhite };
            p.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var path = RoundedRectStatic(new Rectangle(0, 0, p.Width - 1, p.Height - 1), 12);
                using var bg = new SolidBrush(clrWhite);
                e.Graphics.FillPath(bg, path);
                using var bdr = new Pen(clrBorder);
                e.Graphics.DrawPath(bdr, path);
                using var acc = new SolidBrush(accent);
                using var acP = RoundedRectStatic(new Rectangle(0, 0, 4, p.Height - 1), 2);
                e.Graphics.FillPath(acc, acP);
            };
            p.Controls.Add(new Label
            {
                Name = "lbl",
                Text = label,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = clrTextLight,
                AutoSize = true,
                Location = new Point(14, 12),
                BackColor = Color.Transparent
            });
            p.Controls.Add(new Label
            {
                Name = "val",
                Text = value,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = clrTextDark,
                AutoSize = true,
                Location = new Point(12, 30),
                BackColor = Color.Transparent
            });
            return p;
        }

        private static void SetUrssafKpi(Panel kpi, string value)
        {
            var val = kpi.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "val");
            if (val != null) { val.Text = value; kpi.Invalidate(true); }
        }

        // ══════════════════════════════════════════════════════════════════════
        // FORM LOAD
        // ══════════════════════════════════════════════════════════════════════
        private async void Form1_Load(object sender, EventArgs e)
        {
            // Auto-update schema to ensure new tables exist
            try { await db.InitializeDatabaseAsync(); } catch { }

            bool ok = await db.TestConnectionAsync();
            if (!ok)
            {
                var res = MessageBox.Show(
                    "Connexion à la base impossible. Voulez-vous initialiser la base ?",
                    "Base introuvable", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    try
                    {
                        await db.InitializeDatabaseAsync();
                        MessageBox.Show("Initialisation terminée.", "Succès",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Échec : " + ex.Message, "Erreur",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // REFRESH STATS
        // ══════════════════════════════════════════════════════════════════════
        /// <summary>Rafraîchit les statistiques selon la vue actuellement affichée.</summary>
        private async Task RefreshDashboardStatsAsync()
        {
            // Charge les stats asynchronement et met à jour la vue active
            _ = Task.Run(async () =>
            {
                try
                {
                    // Selon la vue active, rafraîchir les données appropriées
                    if (_currentViewType == "urssaf")
                    {
                        Invoke(() => ShowUrssafView());
                    }
                    else if (_currentViewType == "dashboard")
                    {
                        Invoke(() => ShowDashboard());
                    }
                }
                catch { }
            });
        }

        // ══════════════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════════════
        private static GraphicsPath RoundedRect(Rectangle r, int radius)
            => RoundedRectStatic(r, radius);

        private static GraphicsPath RoundedRectStatic(Rectangle r, int radius)
        {
            // Clamp radius so the arcs never exceed half the rect's smallest side
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

        private void Form1_Load_1(object sender, EventArgs e)
        {

        }
    }
}
