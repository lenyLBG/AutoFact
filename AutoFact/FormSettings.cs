using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace autofact
{
    internal class FormSettings : Form
    {
        // Colors
        private readonly Color clrBg = Color.FromArgb(244, 246, 250);
        private readonly Color clrWhite = Color.White;
        private readonly Color clrBorder = Color.FromArgb(229, 231, 235);
        private readonly Color clrTextDark = Color.FromArgb(17, 24, 39);
        private readonly Color clrTextMid = Color.FromArgb(75, 85, 101);
        private readonly Color clrTextLight = Color.FromArgb(156, 163, 175);
        private readonly Color primaryColor = Color.FromArgb(59, 130, 246);
        private readonly Color primaryHover = Color.FromArgb(37, 99, 235);
        private readonly Color clrGreen = Color.FromArgb(16, 185, 129);
        private readonly Color clrRed = Color.FromArgb(239, 68, 68);

        private readonly Bdd _bdd;
        private TabControl _tabControl = null!;

        // Tab: Compte
        private TextBox _txtEmail = null!;
        private Label _lblEmailInfo = null!;

        // Tab: Base de données
        private TextBox _txtConn = null!;
        private Button _btnTestConnection = null!;
        private Label _lblConnectionStatus = null!;

        // Tab: Préférences
        private CheckBox _chkThemeDark = null!;

        public bool SettingsUpdated { get; private set; }

        public FormSettings(Bdd bdd)
        {
            _bdd = bdd;
            SetupForm();
            BuildLayout();
            _ = LoadSettingsAsync();
        }

        private void SetupForm()
        {
            Text = "Paramètres";
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(800, 600);
            MinimumSize = new Size(700, 500);
            Font = new Font("Segoe UI", 10F);
            BackColor = clrBg;
        }

        private void BuildLayout()
        {
            // Header
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = clrWhite
            };
            header.Paint += (s, e) =>
            {
                using var pen = new Pen(clrBorder);
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };
            Controls.Add(header);

            header.Controls.Add(new Label
            {
                Text = "?  Paramètres",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = clrTextDark,
                AutoSize = true,
                Location = new Point(24, 18),
                BackColor = Color.Transparent
            });

            // Tabs
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                BackColor = clrBg,
                ItemSize = new Size(120, 32),
                Appearance = TabAppearance.Normal,
                SizeMode = TabSizeMode.Fixed,
                Padding = new Point(8, 4)
            };

            // Tab: Compte
            var tabAccount = CreateAccountTab();
            _tabControl.TabPages.Add(tabAccount);

            // Tab: Base de données
            var tabDatabase = CreateDatabaseTab();
            _tabControl.TabPages.Add(tabDatabase);

            // Tab: Préférences
            var tabPreferences = CreatePreferencesTab();
            _tabControl.TabPages.Add(tabPreferences);

            // Tab: À propos
            var tabAbout = CreateAboutTab();
            _tabControl.TabPages.Add(tabAbout);

            Controls.Add(_tabControl);

            // Footer buttons
            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = clrWhite
            };
            footer.Paint += (s, e) =>
            {
                using var pen = new Pen(clrBorder);
                e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
            };
            Controls.Add(footer);

            var btnSave = new Button
            {
                Text = "Enregistrer",
                Width = 100,
                Height = 38,
                BackColor = primaryColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Location = new Point(footer.Width - 220, 11);
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
            btnSave.MouseEnter += (s, e) => { btnSave.BackColor = primaryHover; btnSave.Invalidate(); };
            btnSave.MouseLeave += (s, e) => { btnSave.BackColor = primaryColor; btnSave.Invalidate(); };
            btnSave.Click += async (s, e) => await SaveSettingsAsync();
            footer.Controls.Add(btnSave);

            var btnCancel = new Button
            {
                Text = "Fermer",
                Width = 100,
                Height = 38,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = clrTextMid,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = clrBorder;
            btnCancel.Location = new Point(footer.Width - 112, 11);
            btnCancel.Click += (s, e) => Close();
            footer.Controls.Add(btnCancel);
        }

        private TabPage CreateAccountTab()
        {
            var tab = new TabPage { Text = "  ?? Compte", BackColor = clrBg };

            var panel = new Panel { Dock = DockStyle.Fill, BackColor = clrBg, AutoScroll = true };

            // Section: Informations personnelles
            panel.Controls.Add(CreateSectionHeader("Informations personnelles", 24, 24));

            var cardUser = CreateCard(24, 68, 752);
            _txtEmail = new TextBox
            {
                Text = "utilisateur@example.com",
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = clrTextMid,
                Font = new Font("Segoe UI", 10F),
                Location = new Point(16, 46),
                Width = 360,
                Height = 24
            };
            cardUser.Controls.Add(new Label
            {
                Text = "Email",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = clrTextMid,
                AutoSize = true,
                Location = new Point(16, 16),
                BackColor = Color.Transparent
            });
            cardUser.Controls.Add(_txtEmail);

            var btnChangePassword = new Button
            {
                Text = "Changer le mot de passe",
                Width = 180,
                Height = 36,
                BackColor = primaryColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(400, 46),
                Cursor = Cursors.Hand
            };
            btnChangePassword.FlatAppearance.BorderSize = 0;
            btnChangePassword.Click += (s, e) => MessageBox.Show("Fonctionnalité à implémenter", "Info",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            cardUser.Controls.Add(btnChangePassword);

            panel.Controls.Add(cardUser);

            _lblEmailInfo = new Label
            {
                Text = "Votre adresse email est utilisée pour vous identifier dans l'application.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = clrTextLight,
                AutoSize = true,
                Location = new Point(24, 150),
                BackColor = Color.Transparent
            };
            panel.Controls.Add(_lblEmailInfo);

            tab.Controls.Add(panel);
            return tab;
        }

        private TabPage CreateDatabaseTab()
        {
            var tab = new TabPage { Text = "  ??  Base de données", BackColor = clrBg };
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = clrBg, AutoScroll = true };

            // Section: Connexion
            panel.Controls.Add(CreateSectionHeader("Connexion à la base de données", 24, 24));

            var cardConn = CreateCard(24, 68, 752);
            cardConn.Height = 200;

            cardConn.Controls.Add(new Label
            {
                Text = "Chaîne de connexion",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = clrTextMid,
                AutoSize = true,
                Location = new Point(16, 16),
                BackColor = Color.Transparent
            });

            _txtConn = new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = clrWhite,
                ForeColor = clrTextDark,
                Font = new Font("Segoe UI", 9F),
                Location = new Point(16, 40),
                Width = 720,
                Height = 80,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            cardConn.Controls.Add(_txtConn);

            var helpText = new Label
            {
                Text = "Format: server=HOST;user id=USERNAME;password=PASSWORD;database=DBNAME",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = clrTextLight,
                AutoSize = true,
                Location = new Point(16, 128),
                BackColor = Color.Transparent
            };
            cardConn.Controls.Add(helpText);

            panel.Controls.Add(cardConn);

            // Status et boutons
            var statusPanel = new Panel
            {
                Location = new Point(24, 276),
                Size = new Size(752, 100),
                BackColor = Color.Transparent
            };

            _lblConnectionStatus = new Label
            {
                Text = "État: Vérification...",
                Font = new Font("Segoe UI", 10F),
                ForeColor = clrTextMid,
                AutoSize = true,
                Location = new Point(0, 0),
                BackColor = Color.Transparent
            };
            statusPanel.Controls.Add(_lblConnectionStatus);

            _btnTestConnection = new Button
            {
                Text = "Tester la connexion",
                Width = 140,
                Height = 36,
                BackColor = clrGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(0, 32),
                Cursor = Cursors.Hand
            };
            _btnTestConnection.FlatAppearance.BorderSize = 0;
            _btnTestConnection.Click += async (s, e) => await TestConnectionAsync();
            statusPanel.Controls.Add(_btnTestConnection);

            var btnReset = new Button
            {
                Text = "Réinitialiser",
                Width = 100,
                Height = 36,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = clrTextMid,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F),
                Location = new Point(148, 32),
                Cursor = Cursors.Hand
            };
            btnReset.FlatAppearance.BorderColor = clrBorder;
            btnReset.Click += (s, e) =>
            {
                _txtConn.Text = "server=192.168.56.200;user id=app;password=Demaindeslaube;database=AutoFact";
                MessageBox.Show("Chaîne de connexion réinitialisée.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            statusPanel.Controls.Add(btnReset);

            panel.Controls.Add(statusPanel);

            tab.Controls.Add(panel);
            return tab;
        }

        private TabPage CreatePreferencesTab()
        {
            var tab = new TabPage { Text = "  ? Préférences", BackColor = clrBg };
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = clrBg, AutoScroll = true };

            // Section: Apparence
            panel.Controls.Add(CreateSectionHeader("Apparence", 24, 24));

            var cardTheme = CreateCard(24, 68, 752);
            cardTheme.Height = 80;

            _chkThemeDark = new CheckBox
            {
                Text = "Mode sombre",
                Font = new Font("Segoe UI", 10F),
                ForeColor = clrTextDark,
                Location = new Point(16, 20),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            cardTheme.Controls.Add(_chkThemeDark);

            panel.Controls.Add(cardTheme);

            tab.Controls.Add(panel);
            return tab;
        }

        private TabPage CreateAboutTab()
        {
            var tab = new TabPage { Text = "  ?  À propos", BackColor = clrBg };
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = clrBg, AutoScroll = true };

            var cardAbout = CreateCard(24, 24, 752);
            cardAbout.Height = 250;

            cardAbout.Controls.Add(new Label
            {
                Text = "AutoFact",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = clrTextDark,
                AutoSize = true,
                Location = new Point(16, 16),
                BackColor = Color.Transparent
            });

            cardAbout.Controls.Add(new Label
            {
                Text = "Version 1.0.0",
                Font = new Font("Segoe UI", 10F),
                ForeColor = clrTextMid,
                AutoSize = true,
                Location = new Point(16, 50),
                BackColor = Color.Transparent
            });

            cardAbout.Controls.Add(new Label
            {
                Text = "Application de gestion commerciale pour autoentrepreneurs",
                Font = new Font("Segoe UI", 10F),
                ForeColor = clrTextLight,
                AutoSize = true,
                Location = new Point(16, 80),
                BackColor = Color.Transparent
            });

            cardAbout.Controls.Add(new Label
            {
                Text = "© 2024 AutoFact. Tous droits réservés.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = clrTextLight,
                AutoSize = true,
                Location = new Point(16, 210),
                BackColor = Color.Transparent
            });

            panel.Controls.Add(cardAbout);
            tab.Controls.Add(panel);
            return tab;
        }

        private Label CreateSectionHeader(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = clrTextDark,
                AutoSize = true,
                Location = new Point(x, y),
                BackColor = Color.Transparent
            };
        }

        private Panel CreateCard(int x, int y, int w)
        {
            var card = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, 100),
                BackColor = clrWhite
            };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
                using var br = new SolidBrush(clrWhite);
                e.Graphics.FillPath(br, path);
                using var pen = new Pen(clrBorder);
                e.Graphics.DrawPath(pen, path);
            };
            return card;
        }

        private async Task TestConnectionAsync()
        {
            try
            {
                _btnTestConnection.Enabled = false;
                _lblConnectionStatus.Text = "État: Test en cours...";
                _lblConnectionStatus.ForeColor = clrTextMid;
                Refresh();

                bool ok = await _bdd.TestConnectionAsync();
                if (ok)
                {
                    _lblConnectionStatus.Text = "? État: Connecté avec succès";
                    _lblConnectionStatus.ForeColor = clrGreen;
                }
                else
                {
                    _lblConnectionStatus.Text = "? État: Échec de la connexion";
                    _lblConnectionStatus.ForeColor = clrRed;
                }
            }
            catch (Exception ex)
            {
                _lblConnectionStatus.Text = $"? État: Erreur - {ex.Message}";
                _lblConnectionStatus.ForeColor = clrRed;
            }
            finally
            {
                _btnTestConnection.Enabled = true;
            }
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                var conn = _txtConn.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(conn))
                {
                    MessageBox.Show("La chaîne de connexion ne peut pas être vide.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                var obj = new { ConnectionStrings = new { Default = conn } };
                var opts = new JsonSerializerOptions { WriteIndented = true };
                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(obj, opts));

                SettingsUpdated = true;
                MessageBox.Show("Paramètres enregistrés avec succès.", "Succès",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : " + ex.Message, "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private GraphicsPath RoundedRect(Rectangle r, int radius)
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

        public async Task LoadSettingsAsync()
        {
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                if (!File.Exists(path))
                {
                    _txtConn.Text = "server=192.168.56.200;user id=app;password=Demaindeslaube;database=AutoFact";
                    return;
                }

                var json = await File.ReadAllTextAsync(path);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) &&
                    cs.TryGetProperty("Default", out var def))
                {
                    _txtConn.Text = def.GetString() ?? "";
                    await TestConnectionAsync();
                }
            }
            catch
            {
                _txtConn.Text = "server=192.168.56.200;user id=app;password=Demaindeslaube;database=AutoFact";
            }
        }
    }
}
