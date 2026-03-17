using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace autofact
{
    // Formulaire des paramètres avec amélioration
    internal class FormSettings : Form
    {
        private readonly Bdd _bdd;
        private TextBox _txtConn;
        private Button _btnSave;
        private Button _btnCancel;
        private Button _btnReset;
        private Label _lblHelp;

        public bool SettingsUpdated { get; private set; }

        public FormSettings(Bdd bdd)
        {
            _bdd = bdd;
            InitializeComponents();
            _ = LoadSettingsAsync();
        }

        private void InitializeComponents()
        {
            this.Text = "Paramètres";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new System.Drawing.Size(700, 240);
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lbl = new Label 
            { 
                Text = "Chaîne de connexion MySQL/MariaDB", 
                AutoSize = true, 
                Left = 12, 
                Top = 12,
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold)
            };

            _txtConn = new TextBox 
            { 
                Left = 12, 
                Top = 36, 
                Width = 660,
                Height = 80,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            _lblHelp = new Label
            {
                Text = "Format: server=HOST;user id=USERNAME;password=PASSWORD;database=DBNAME",
                AutoSize = false,
                Left = 12,
                Top = 120,
                Width = 660,
                Height = 40,
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font("Segoe UI", 9F)
            };

            _btnReset = new Button 
            { 
                Text = "Réinitialiser", 
                Left = 12, 
                Width = 100, 
                Top = 165,
                Cursor = System.Windows.Forms.Cursors.Hand
            };

            _btnSave = new Button 
            { 
                Text = "Enregistrer", 
                Left = 500, 
                Width = 80, 
                Top = 165, 
                DialogResult = DialogResult.OK 
            };

            _btnCancel = new Button 
            { 
                Text = "Annuler", 
                Left = 592, 
                Width = 80, 
                Top = 165, 
                DialogResult = DialogResult.Cancel 
            };

            _btnSave.Click += BtnSave_Click;
            _btnCancel.Click += (s, e) => this.Close();
            _btnReset.Click += BtnReset_Click;

            this.Controls.Add(lbl);
            this.Controls.Add(_txtConn);
            this.Controls.Add(_lblHelp);
            this.Controls.Add(_btnReset);
            this.Controls.Add(_btnSave);
            this.Controls.Add(_btnCancel);
        }

        private void BtnReset_Click(object? sender, EventArgs e)
        {
            // Réinitialise la chaîne de connexion par défaut
            _txtConn.Text = "server=192.168.56.200;user id=app;password=Demaindeslaube;database=AutoFact";
            MessageBox.Show("Chaîne de connexion réinitialisée par défaut.", "Réinitialisation", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
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

                // Écrit dans appsettings.json
                var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                var obj = new { ConnectionStrings = new { Default = conn } };
                var opts = new JsonSerializerOptions { WriteIndented = true };
                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(obj, opts));

                SettingsUpdated = true;
                MessageBox.Show("Paramètres enregistrés. Redémarrage recommandé.", "Succès", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'enregistrement : " + ex.Message, "Erreur", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                }
            }
            catch
            {
                _txtConn.Text = "server=192.168.56.200;user id=app;password=Demaindeslaube;database=AutoFact";
            }
        }
    }
}
