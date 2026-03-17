using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using autofact.Models;
using autofact.ViewModels;

namespace autofact
{
    internal class FormArticleAdd : Form
    {
        // ?? Dependencies ???????????????????????????????????????????????????????
        private readonly PrestationViewModel _vm;
        private readonly int?                _editId;   // null = add mode, non-null = edit mode

        // ?? Palette ????????????????????????????????????????????????????????????
        private readonly Color cBlue      = Color.FromArgb(59, 130, 246);
        private readonly Color cBlueHover = Color.FromArgb(37,  99, 235);
        private readonly Color cBlueDeep  = Color.FromArgb(29,  78, 216);
        private readonly Color cBlueFocus = Color.FromArgb(66, 153, 225);
        private readonly Color cFormBg    = Color.FromArgb(244, 246, 250);
        private readonly Color cWhite     = Color.White;
        private readonly Color cText      = Color.FromArgb(17,  24,  39);
        private readonly Color cLabel     = Color.FromArgb(55,  65,  81);
        private readonly Color cBorder    = Color.FromArgb(209, 213, 219);
        private readonly Color cInputBg   = Color.FromArgb(249, 250, 251);
        private readonly Color cErrorText = Color.FromArgb(185,  28,  28);
        private readonly Color cErrorBg   = Color.FromArgb(254, 242, 242);
        private readonly Color cErrorBdr  = Color.FromArgb(254, 202, 202);

        // ?? Controls ???????????????????????????????????????????????????????????
        private Panel   card        = null!;
        private TextBox txtNom      = null!;
        private TextBox txtType     = null!;
        private TextBox txtPrix     = null!;
        private Button  btnSave     = null!;
        private Button  btnCancel   = null!;
        private Panel   pnlError    = null!;
        private Label   lblErrMsg   = null!;

        // ?? Result ?????????????????????????????????????????????????????????????
        public bool ArticleSaved { get; private set; }

        // ??????????????????????????????????????????????????????????????????????
        public FormArticleAdd(Bdd bdd, int? editId = null,
            string? initialNom = null, string? initialType = null, decimal? initialPrix = null)
        {
            _editId = editId;
            _vm     = new PrestationViewModel(bdd);

            // Pre-load into ViewModel in edit mode
            if (editId.HasValue)
            {
                _vm.Charger(new Prestation
                {
                    Id           = editId.Value,
                    Nom          = initialNom  ?? string.Empty,
                    Type         = initialType ?? string.Empty,
                    PrixUnitaire = initialPrix ?? 0m
                });
            }

            _vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(_vm.Erreur) && !string.IsNullOrEmpty(_vm.Erreur))
                    ShowError(_vm.Erreur);
                if (e.PropertyName == nameof(_vm.IsBusy))
                    SetBusy(_vm.IsBusy);
            };

            SetupForm();
            BuildCard();

            // Fill form controls from ViewModel after they are created
            if (editId.HasValue)
            {
                txtNom.Text  = _vm.Nom;
                txtType.Text = _vm.Type;
                txtPrix.Text = _vm.PrixUnitaire > 0
                    ? _vm.PrixUnitaire.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                    : string.Empty;
            }
        }

        // ??????????????????????????????????????????????????????????????????????
        // FORM SHELL
        // ??????????????????????????????????????????????????????????????????????
        private void SetupForm()
        {
            Text            = string.Empty;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition   = FormStartPosition.CenterParent;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = cFormBg;
            Font            = new Font("Segoe UI", 10F);
            DoubleBuffered  = true;
            Paint           += PaintBackground;
            MouseDown       += OnDrag;
        }

        // ??????????????????????????????????????????????????????????????????????
        // CARD
        // ??????????????????????????????????????????????????????????????????????
        private void BuildCard()
        {
            const int cardW  = 460;
            const int padH   = 32;
            const int padV   = 28;
            const int labelH = 18;
            const int inputH = 44;
            const int gap    = 20;
            const int errorH = 34;
            const int btnH   = 44;

            int bodyH = (labelH + 6 + inputH) * 3 + gap * 2   // Nom, Type, Prix
                      + gap + errorH + gap
                      + btnH + padV;
            int cardH  = 56 + padV + bodyH;
            int margin = 48;
            ClientSize = new Size(cardW + margin * 2, cardH + margin * 2);

            card = new Panel { Size = new Size(cardW, cardH), Location = new Point(margin, margin), BackColor = cWhite };
            card.Paint     += PaintCard;
            card.MouseDown += OnDrag;
            Controls.Add(card);

            Resize += (s, e) => card.Location = new Point(
                (ClientSize.Width  - card.Width)  / 2,
                (ClientSize.Height - card.Height) / 2);

            // Header
            var header = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.Transparent };
            header.Paint    += PaintHeader;
            header.MouseDown += OnDrag;
            card.Controls.Add(header);

            string title = _editId.HasValue ? "Modifier l'article" : "Ajouter un article";
            header.Controls.Add(new Label
            {
                Text      = title,
                ForeColor = cWhite,
                Font      = new Font("Segoe UI", 13F, FontStyle.Bold),
                AutoSize  = true,
                Location  = new Point(padH, 16),
                BackColor = Color.Transparent
            });

            var btnX = new Button
            {
                Size      = new Size(32, 32),
                Location  = new Point(cardW - 44, 12),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                TabStop   = false,
                Cursor    = Cursors.Hand
            };
            btnX.FlatAppearance.BorderSize         = 0;
            btnX.FlatAppearance.MouseOverBackColor = Color.FromArgb(25, 255, 255, 255);
            btnX.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var p = new Pen(Color.White, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                e.Graphics.DrawLine(p, 9, 9, 23, 23);
                e.Graphics.DrawLine(p, 23, 9, 9, 23);
            };
            btnX.Click += (s, e) => Close();
            header.Controls.Add(btnX);

            // Body
            var body = new Panel { Dock = DockStyle.Fill, BackColor = cWhite };
            card.Controls.Add(body);
            body.BringToFront();

            int xOff = padH;
            int cxW  = cardW - padH * 2;
            int y    = padV;

            // Nom *
            body.Controls.Add(MakeLabel("Nom *", xOff, y));
            y += labelH + 6;
            (var wNom, txtNom) = MakeInput("ex. Consultation horaire", false, cxW, xOff, y);
            body.Controls.Add(wNom);
            y += inputH + gap;

            // Type
            body.Controls.Add(MakeLabel("Type (produit / service)", xOff, y));
            y += labelH + 6;
            (var wType, txtType) = MakeInput("ex. Service, Produit physique…", false, cxW, xOff, y);
            body.Controls.Add(wType);
            y += inputH + gap;

            // Prix unitaire *
            body.Controls.Add(MakeLabel("Prix unitaire HT (€) *", xOff, y));
            y += labelH + 6;
            (var wPrix, txtPrix) = MakeInput("ex. 120.00", false, cxW, xOff, y);
            body.Controls.Add(wPrix);
            y += inputH + gap;

            // Error banner
            pnlError = new Panel
            {
                Left      = xOff,
                Top       = y,
                Width     = cxW,
                Height    = errorH,
                BackColor = cErrorBg,
                Visible   = false
            };
            pnlError.Paint += PaintError;
            lblErrMsg = new Label
            {
                Left      = 30,
                Top       = 0,
                Width     = cxW - 38,
                Height    = errorH,
                ForeColor = cErrorText,
                Font      = new Font("Segoe UI", 9F),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlError.Controls.Add(lblErrMsg);
            body.Controls.Add(pnlError);
            y += errorH + gap;

            // Buttons
            int halfW = (cxW - 12) / 2;
            string saveLabel = _editId.HasValue ? "Enregistrer" : "Ajouter";
            btnCancel = MakeButton("Annuler",   xOff,              y, halfW, btnH, filled: false);
            btnSave   = MakeButton(saveLabel,   xOff + halfW + 12, y, halfW, btnH, filled: true);

            body.Controls.Add(btnCancel);
            body.Controls.Add(btnSave);

            btnCancel.Click += (s, e) => Close();
            btnSave.Click   += BtnSave_Click;
            AcceptButton     = btnSave;

            txtNom.TabIndex  = 0;
            txtType.TabIndex = 1;
            txtPrix.TabIndex = 2;
            btnSave.TabIndex = 3;
            btnCancel.TabIndex = 4;
        }

        // ??????????????????????????????????????????????????????????????????????
        // CONTROL FACTORIES
        // ??????????????????????????????????????????????????????????????????????
        private Label MakeLabel(string text, int x, int y) => new Label
        {
            Text      = text,
            Left      = x,
            Top       = y,
            AutoSize  = true,
            ForeColor = cLabel,
            Font      = new Font("Segoe UI", 9F, FontStyle.Bold),
            BackColor = Color.Transparent
        };

        private (Panel wrap, TextBox tb) MakeInput(
            string placeholder, bool multiline,
            int width, int x, int y, int height = 44)
        {
            var tb = new TextBox
            {
                PlaceholderText = placeholder,
                BorderStyle     = BorderStyle.None,
                BackColor       = cInputBg,
                ForeColor       = cText,
                Font            = new Font("Segoe UI", 10F),
                Multiline       = multiline,
                ScrollBars      = multiline ? ScrollBars.Vertical : ScrollBars.None
            };

            var wrap = new Panel
            {
                Left      = x,
                Top       = y,
                Width     = width,
                Height    = height,
                BackColor = cInputBg,
                Cursor    = Cursors.IBeam
            };

            void Layout()
            {
                tb.Width    = wrap.Width - 20;
                tb.Location = new Point(10, (wrap.Height - tb.PreferredHeight) / 2);
                tb.BackColor = wrap.BackColor;
            }

            wrap.Controls.Add(tb);
            wrap.Resize  += (s, e) => Layout();
            Layout();

            tb.GotFocus  += (s, e) => { wrap.BackColor = cWhite;   wrap.Invalidate(); tb.BackColor = cWhite; };
            tb.LostFocus += (s, e) => { wrap.BackColor = cInputBg; wrap.Invalidate(); tb.BackColor = cInputBg; };
            wrap.Click   += (s, e) => tb.Focus();

            wrap.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, wrap.Width - 1, wrap.Height - 1);
                using var path = RoundedPath(rect, 8);
                using var br   = new SolidBrush(wrap.BackColor);
                g.FillPath(br, path);
                if (tb.Focused)
                {
                    var glow = new Rectangle(-3, -3, wrap.Width + 5, wrap.Height + 5);
                    using var gp   = RoundedPath(glow, 11);
                    using var gPen = new Pen(Color.FromArgb(45, cBlueFocus), 4f);
                    g.DrawPath(gPen, gp);
                    using var bPen = new Pen(cBlueFocus, 1.5f);
                    g.DrawPath(bPen, path);
                }
                else
                {
                    using var pen = new Pen(cBorder, 1f);
                    g.DrawPath(pen, path);
                }
            };

            return (wrap, tb);
        }

        private Button MakeButton(string text, int x, int y, int w, int h, bool filled)
        {
            var btn = new Button
            {
                Text      = text,
                Left      = x,
                Top       = y,
                Size      = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                BackColor = filled ? cBlue : cWhite,
                ForeColor = filled ? cWhite : cBlue
            };
            btn.FlatAppearance.BorderSize = 0;

            btn.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, btn.Width - 1, btn.Height - 1);
                using var path = RoundedPath(r, h / 2);
                if (filled)
                {
                    using var br = new LinearGradientBrush(r,
                        Lighten(btn.BackColor, 12), btn.BackColor, LinearGradientMode.Vertical);
                    g.FillPath(br, path);
                }
                else
                {
                    using var br  = new SolidBrush(btn.BackColor);
                    g.FillPath(br, path);
                    using var pen = new Pen(cBlue, 1.5f);
                    g.DrawPath(pen, path);
                }
                TextRenderer.DrawText(g, btn.Text, btn.Font, r, btn.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            if (filled)
            {
                btn.MouseEnter += (s, e) => { btn.BackColor = cBlueHover; btn.Invalidate(); };
                btn.MouseLeave += (s, e) => { btn.BackColor = cBlue;      btn.Invalidate(); };
                btn.MouseDown  += (s, e) => { btn.BackColor = cBlueDeep;  btn.Invalidate(); };
                btn.MouseUp    += (s, e) => { btn.BackColor = cBlueHover; btn.Invalidate(); };
            }
            else
            {
                btn.MouseEnter += (s, e) => { btn.BackColor = Color.FromArgb(239, 246, 255); btn.Invalidate(); };
                btn.MouseLeave += (s, e) => { btn.BackColor = cWhite;                        btn.Invalidate(); };
            }

            return btn;
        }

        // ??????????????????????????????????????????????????????????????????????
        // PAINT HANDLERS
        // ??????????????????????????????????????????????????????????????????????
        private void PaintBackground(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var br1 = new SolidBrush(Color.FromArgb(18, 59, 130, 246));
            g.FillEllipse(br1, -60, -60, 260, 260);
            using var br2 = new SolidBrush(Color.FromArgb(12, 59, 130, 246));
            g.FillEllipse(br2, ClientSize.Width - 80, ClientSize.Height - 80, 180, 180);
        }

        private void PaintCard(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            foreach (var (off, alpha) in new[] { (2, 5), (4, 8), (7, 10), (11, 7), (15, 4) })
            {
                var sr = new Rectangle(off / 2, off, card.Width - off, card.Height - off);
                using var shBr   = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0));
                using var shPath = RoundedPath(sr, 16);
                g.FillPath(shBr, shPath);
            }
            using var cardPath = RoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 16);
            using var cardBr   = new SolidBrush(cWhite);
            g.FillPath(cardBr, cardPath);
            using var bdr = new Pen(Color.FromArgb(15, 0, 0, 0), 1f);
            g.DrawPath(bdr, cardPath);
        }

        private void PaintHeader(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, card.Width, 56 + 16);
            using var path = RoundedPath(rect, 16, topOnly: true);
            using var br   = new LinearGradientBrush(
                new Point(0, 0), new Point(card.Width, 56),
                Color.FromArgb(79, 130, 220), Color.FromArgb(37, 99, 235));
            g.FillPath(br, path);
        }

        private void PaintError(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedPath(new Rectangle(0, 0, pnlError.Width - 1, pnlError.Height - 1), 7);
            using var br   = new SolidBrush(cErrorBg);
            g.FillPath(br, path);
            using var pen  = new Pen(cErrorBdr, 1f);
            g.DrawPath(pen, path);
            using var ipen = new Pen(cErrorText, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            int cy = pnlError.Height / 2;
            g.DrawLines(ipen, new[] { new Point(10, cy + 6), new Point(16, cy - 6), new Point(22, cy + 6), new Point(10, cy + 6) });
            using var dotBr = new SolidBrush(cErrorText);
            g.FillEllipse(dotBr, 15, cy + 1, 3, 3);
        }

        // ??????????????????????????????????????????????????????????????????????
        // BUSY STATE
        // ??????????????????????????????????????????????????????????????????????
        private void SetBusy(bool busy)
        {
            btnSave.Enabled   = !busy;
            btnCancel.Enabled = !busy;
            btnSave.BackColor = busy ? cBlueDeep : cBlue;
            btnSave.Invalidate();
        }

        // ??????????????????????????????????????????????????????????????????????
        // VALIDATION & SAVE
        // ??????????????????????????????????????????????????????????????????????
        private void ShowError(string msg)
        {
            lblErrMsg.Text   = msg;
            pnlError.Visible = true;
            pnlError.Invalidate();
        }

        private void HideError() => pnlError.Visible = false;

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            HideError();

            // Push field values into ViewModel
            _vm.Nom          = txtNom.Text?.Trim()  ?? string.Empty;
            _vm.Type         = txtType.Text?.Trim() ?? string.Empty;

            var prixRaw = txtPrix.Text?.Trim().Replace(',', '.') ?? string.Empty;
            if (!decimal.TryParse(prixRaw,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal prix) || prix < 0)
            {
                ShowError("Le prix unitaire doit être un nombre positif.");
                txtPrix.Focus();
                return;
            }
            _vm.PrixUnitaire = prix;

            bool ok = await _vm.SauvegarderAsync();
            if (ok)
            {
                ArticleSaved = true;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        // ??????????????????????????????????????????????????????????????????????
        // HELPERS
        // ??????????????????????????????????????????????????????????????????????
        private static GraphicsPath RoundedPath(Rectangle r, int radius, bool topOnly = false)
        {
            var path = new GraphicsPath();
            int d    = radius * 2;
            if (topOnly)
            {
                path.AddArc(r.Left,      r.Top, d, d, 180, 90);
                path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
                path.AddLine(r.Right, r.Top + radius, r.Right, r.Bottom);
                path.AddLine(r.Right, r.Bottom, r.Left, r.Bottom);
                path.AddLine(r.Left,  r.Bottom, r.Left, r.Top + radius);
                path.CloseFigure();
                return path;
            }
            path.AddArc(r.Left,      r.Top,        d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top,        d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
            path.AddArc(r.Left,      r.Bottom - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }

        private static Color Lighten(Color c, int by) =>
            Color.FromArgb(c.A,
                Math.Min(255, c.R + by),
                Math.Min(255, c.G + by),
                Math.Min(255, c.B + by));

        private void OnDrag(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(Handle, 0xA1, 0x2, 0);
            }
        }
    }
}
