using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySqlConnector;

namespace autofact
{
    internal class FormAuth : Form
    {
        // ?? Dependencies ??????????????????????????????????????????????????????
        private readonly Bdd _bdd;

        // ?? Controls ??????????????????????????????????????????????????????????
        private TextBox  txtEmail      = null!;
        private TextBox  txtPassword   = null!;
        private Button   btnEye        = null!;   // show/hide password
        private Button   btnLogin      = null!;
        private Button   btnRegister   = null!;
        private Panel    pnlError      = null!;   // error banner
        private Label    lblErrorMsg   = null!;
        private Panel    topBanner     = null!;
        private Panel    card          = null!;
        private CheckBox chkRememberMe = null!;

        // ?? Palette ???????????????????????????????????????????????????????????
        // Blues
        private readonly Color cBlue        = Color.FromArgb(43,  108, 176);  // brand blue
        private readonly Color cBlueDeep    = Color.FromArgb(26,   86, 150);  // header gradient end
        private readonly Color cBlueHover   = Color.FromArgb(26,   86, 150);  // primary btn hover
        private readonly Color cBlueFocus   = Color.FromArgb(66,  153, 225);  // input focus ring
        private readonly Color cBlueTint    = Color.FromArgb(235, 244, 255);  // outline btn hover bg
        private readonly Color cBlueSub     = Color.FromArgb(190, 214, 248);  // header subtitle
        private readonly Color cBlueBadgeBg = Color.FromArgb(30, 255, 255, 255); // header badge bg
        // Neutrals
        private readonly Color cFormBg      = Color.FromArgb(235, 240, 248);  // page bg
        private readonly Color cText        = Color.FromArgb(26,   32,  44);  // body text
        private readonly Color cLabel       = Color.FromArgb(45,   55,  72);  // field label
        private readonly Color cBorder      = Color.FromArgb(203, 213, 224);  // input border
        private readonly Color cInputBg     = Color.FromArgb(247, 250, 252);  // input fill
        private readonly Color cWhite       = Color.White;
        // Error
        private readonly Color cErrorBg     = Color.FromArgb(255, 245, 245);  // error banner bg
        private readonly Color cErrorBorder = Color.FromArgb(254, 202, 202);  // error banner border
        private readonly Color cErrorText   = Color.FromArgb(185,  28,  28);  // red-700

        // ?? Dimensions ????????????????????????????????????????????????????????
        private const int CardW   = 460;
        private const int HeaderH = 156;
        private const int PadH    = 36;
        private const int PadV    = 32;
        private const int InputH  = 48;
        private const int BtnH    = 48;
        private const int Radius  = 20;

        // ?? State ?????????????????????????????????????????????????????????????
        private bool _busy;
        private bool _showPassword;

        // ?? Result ????????????????????????????????????????????????????????????
        public int?    AuthenticatedUserId { get; private set; }
        public string? AuthenticatedEmail  { get; private set; }

        // ??????????????????????????????????????????????????????????????????????
        public FormAuth(Bdd bdd)
        {
            _bdd = bdd;
            SetupForm();
            BuildCard();
        }

        // ??????????????????????????????????????????????????????????????????????
        // FORM SHELL
        // ??????????????????????????????????????????????????????????????????????
        private void SetupForm()
        {
            Text            = string.Empty;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition   = FormStartPosition.CenterScreen;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = cFormBg;
            Font            = new Font("Segoe UI", 10F);
            DoubleBuffered  = true;

            // Decorative background circles painted on the form itself
            Paint += PaintFormBackground;

            // Drag the borderless window
            MouseDown += OnFormDrag;
        }

        // ??????????????????????????????????????????????????????????????????????
        // CARD
        // ??????????????????????????????????????????????????????????????????????
        private void BuildCard()
        {
            // ?? Height calculation ????????????????????????????????????????????
            const int labelH   = 18;
            const int groupGap = 22;
            const int errorH   = 36;
            const int btnGap   = 20;

            int bodyInner = labelH + 4 + InputH
                          + groupGap
                          + labelH + 4 + InputH
                          + 10 + errorH
                          + 10 + 22       // "se souvenir de moi" checkbox
                          + 10 + BtnH;

            int cardH = HeaderH + PadV + bodyInner + PadV;

            // ?? Form is larger than the card so it shows centred on the bg ???
            const int margin = 60;  // space around the card on every side
            ClientSize = new Size(CardW + margin * 2, cardH + margin * 2);

            // ?? Card panel ????????????????????????????????????????????????????
            card = new Panel
            {
                Size      = new Size(CardW, cardH),
                Location  = new Point(margin, margin),   // centred by margin
                BackColor = cWhite
            };
            card.Paint     += PaintCard;
            card.MouseDown += OnFormDrag;
            Controls.Add(card);

            // Keep card centred when the form is resized
            Resize += (s, e) =>
                card.Location = new Point(
                    (ClientSize.Width  - card.Width)  / 2,
                    (ClientSize.Height - card.Height) / 2);

            // ?? Header ????????????????????????????????????????????????????????
            topBanner = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = HeaderH,
                BackColor = Color.Transparent
            };
            topBanner.Paint += PaintHeader;
            card.Controls.Add(topBanner);

            // Brand chip (pill badge top-left)
            var chip = new Panel
            {
                Size      = new Size(84, 26),
                Location  = new Point(PadH, 18),
                BackColor = Color.Transparent
            };
            chip.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = Pill(chip.ClientRectangle);
                using var br   = new SolidBrush(Color.FromArgb(45, 255, 255, 255));
                e.Graphics.FillPath(br, path);
                using var pen  = new Pen(Color.FromArgb(60, 255, 255, 255));
                e.Graphics.DrawPath(pen, path);
                using var f    = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                using var tb   = new SolidBrush(Color.White);
                TextRenderer.DrawText(e.Graphics, "AutoFact", f, chip.ClientRectangle,
                    Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            topBanner.Controls.Add(chip);

            // Title
            var lblTitle = new Label
            {
                Text      = "Bienvenue",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 24F, FontStyle.Bold),
                AutoSize  = true,
                Location  = new Point(PadH, 54),
                BackColor = Color.Transparent
            };
            topBanner.Controls.Add(lblTitle);

            // Subtitle
            var lblSub = new Label
            {
                Text      = "Connectez-vous pour continuer",
                ForeColor = cBlueSub,
                Font      = new Font("Segoe UI", 10F),
                AutoSize  = true,
                Location  = new Point(PadH, 96),
                BackColor = Color.Transparent
            };
            topBanner.Controls.Add(lblSub);

            // Close × button
            var btnClose = new Button
            {
                Size      = new Size(34, 34),
                Location  = new Point(CardW - 48, 12),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
                TabStop   = false
            };
            btnClose.FlatAppearance.BorderSize         = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(25, 255, 255, 255);
            btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(50, 255, 255, 255);
            btnClose.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.White, 1.8f)
                    { StartCap = LineCap.Round, EndCap = LineCap.Round };
                const int m = 10;
                e.Graphics.DrawLine(pen, m, m, btnClose.Width - m, btnClose.Height - m);
                e.Graphics.DrawLine(pen, btnClose.Width - m, m, m, btnClose.Height - m);
            };
            btnClose.Click += (s, e) => Close();
            topBanner.Controls.Add(btnClose);

            // ?? Body ??????????????????????????????????????????????????????????
            var body = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = cWhite,
                Padding   = new Padding(0)   // no padding — we position everything manually
            };
            card.Controls.Add(body);
            body.BringToFront();

            // Content area is horizontally centred inside the card
            int cx   = CardW - PadH * 2;   // content width
            int xOff = PadH;               // left offset (= right offset, so content is centred)
            int y    = PadV;               // start below the top vertical padding

            // ?? Identifiant ???????????????????????????????????????????????????
            var lblEmail = MakeLabel("Identifiant", y);
            lblEmail.Left = xOff;
            body.Controls.Add(lblEmail);
            y += labelH + 4;

            var (wrapEmail, tbEmail) = MakeInput("Votre identifiant", false, cx, y);
            wrapEmail.Left = xOff;
            txtEmail = tbEmail;
            body.Controls.Add(wrapEmail);
            y += InputH + groupGap;

            // ?? Mot de passe ??????????????????????????????????????????????????
            var lblPwd = MakeLabel("Mot de passe", y);
            lblPwd.Left = xOff;
            body.Controls.Add(lblPwd);
            y += labelH + 4;

            var (wrapPwd, tbPwd) = MakeInput("Votre mot de passe", true, cx, y);
            wrapPwd.Left = xOff;
            txtPassword = tbPwd;

            // Eye toggle inside the password wrapper
            btnEye = new Button
            {
                Size      = new Size(30, 30),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
                TabStop   = false
            };
            btnEye.FlatAppearance.BorderSize         = 0;
            btnEye.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnEye.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnEye.Paint += PaintEyeIcon;
            btnEye.Click += (s, e) =>
            {
                _showPassword                     = !_showPassword;
                txtPassword.UseSystemPasswordChar = !_showPassword;
                btnEye.Invalidate();
                txtPassword.Focus();
            };
            wrapPwd.Controls.Add(btnEye);
            wrapPwd.Resize += (s, e) =>
                btnEye.Location = new Point(wrapPwd.Width - btnEye.Width - 8,
                                            (wrapPwd.Height - btnEye.Height) / 2);
            btnEye.Location = new Point(cx - btnEye.Width - 8, (InputH - 30) / 2);
            tbPwd.Width     = cx - 28 - btnEye.Width - 8;

            body.Controls.Add(wrapPwd);
            y += InputH + 10;

            // ?? Error banner ??????????????????????????????????????????????????
            pnlError = new Panel
            {
                Left      = xOff,
                Top       = y,
                Width     = cx,
                Height    = errorH,
                BackColor = cErrorBg,
                Visible   = false
            };
            pnlError.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedPath(new Rectangle(0, 0, pnlError.Width - 1, pnlError.Height - 1), 8);
                using var br   = new SolidBrush(cErrorBg);
                e.Graphics.FillPath(br, path);
                using var pen  = new Pen(cErrorBorder);
                e.Graphics.DrawPath(pen, path);
                using var dotBr = new SolidBrush(cErrorText);
                e.Graphics.FillEllipse(dotBr, 12, 12, 12, 12);
                using var f = new Font("Segoe UI", 7.5F, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, "!", f,
                    new Rectangle(12, 12, 12, 12), cWhite,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            lblErrorMsg = new Label
            {
                Left      = 32,
                Top       = 0,
                Width     = cx - 40,
                Height    = errorH,
                ForeColor = cErrorText,
                Font      = new Font("Segoe UI", 9F),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Text      = string.Empty
            };
            pnlError.Controls.Add(lblErrorMsg);
            body.Controls.Add(pnlError);
            y += errorH + 10;   // reduced gap — checkbox sits between error and buttons

            // ?? Se souvenir de moi ????????????????????????????????????????????????
            chkRememberMe = new CheckBox
            {
                Text      = "Se souvenir de moi",
                Left      = xOff,
                Top       = y,
                Width     = cx,
                Height    = 22,
                ForeColor = cLabel,
                Font      = new Font("Segoe UI", 9F),
                BackColor = Color.Transparent,
                Cursor    = Cursors.Hand,
                TabIndex  = 2
            };
            body.Controls.Add(chkRememberMe);
            y += chkRememberMe.Height + 10;

            // ?? Buttons ???????????????????????????????????????????????????????
            int btnW = (cx - 12) / 2;

            btnLogin = new Button
            {
                Text = "Se connecter",
                Size = new Size(btnW, BtnH),
                Left = xOff,
                Top  = y
            };
            btnRegister = new Button
            {
                Text = "S'inscrire",
                Size = new Size(btnW, BtnH),
                Left = xOff + btnW + 12,
                Top  = y
            };

            StylePrimary(btnLogin);
            StyleOutline(btnRegister);

            body.Controls.Add(btnLogin);
            body.Controls.Add(btnRegister);

            btnLogin.Click    += BtnLogin_Click;
            btnRegister.Click += BtnRegister_Click;
            AcceptButton       = btnLogin;

            // Tab order
            txtEmail.TabIndex      = 0;
            txtPassword.TabIndex   = 1;
            chkRememberMe.TabIndex = 2;
            btnLogin.TabIndex      = 3;
            btnRegister.TabIndex   = 4;

            // ?? Pre-fill from saved credentials ?????????????????????????????????
            var saved = RememberMe.Load();
            if (saved is not null)
            {
                txtEmail.Text              = saved.Value.Email;
                txtPassword.Text           = saved.Value.Password;
                chkRememberMe.Checked      = true;
            }
        }

        // ??????????????????????????????????????????????????????????????????????
        // CONTROL FACTORIES
        // ??????????????????????????????????????????????????????????????????????
        private Label MakeLabel(string text, int top) => new Label
        {
            Text      = text,
            Left      = 0,
            Top       = top,
            AutoSize  = true,
            ForeColor = cLabel,
            Font      = new Font("Segoe UI", 9F, FontStyle.Bold),
            BackColor = Color.Transparent
        };

        private (Panel wrap, TextBox tb) MakeInput(
            string placeholder, bool isPassword, int width, int top)
        {
            var tb = new TextBox
            {
                PlaceholderText       = placeholder,
                UseSystemPasswordChar = isPassword,
                BorderStyle           = BorderStyle.None,
                BackColor             = cInputBg,
                ForeColor             = cText,
                Font                  = new Font("Segoe UI", 10.5F),
                Multiline             = false
            };

            var wrap = new Panel
            {
                Left      = 0,
                Top       = top,
                Width     = width,
                Height    = InputH,
                BackColor = cInputBg,
                Cursor    = Cursors.IBeam
            };

            void Layout()
            {
                tb.Width    = wrap.Width - 28;
                tb.Location = new Point(14, (wrap.Height - tb.PreferredHeight) / 2);
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
                using var path   = RoundedPath(rect, 10);
                using var fillBr = new SolidBrush(wrap.BackColor);
                g.FillPath(fillBr, path);

                if (tb.Focused)
                {
                    var glow = new Rectangle(-3, -3, wrap.Width + 5, wrap.Height + 5);
                    using var gPath = RoundedPath(glow, 13);
                    using var gPen  = new Pen(Color.FromArgb(45, cBlueFocus), 4f);
                    g.DrawPath(gPen, gPath);
                    using var bPen  = new Pen(cBlueFocus, 1.5f);
                    g.DrawPath(bPen, path);
                }
                else
                {
                    using var bPen = new Pen(cBorder, 1f);
                    g.DrawPath(bPen, path);
                }
            };

            return (wrap, tb);
        }

        // ??????????????????????????????????????????????????????????????????????
        // BUTTON STYLES
        // ??????????????????????????????????????????????????????????????????????
        private void StylePrimary(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = cBlue;
            btn.ForeColor = cWhite;
            btn.Font      = new Font("Segoe UI", 10F, FontStyle.Bold);
            btn.Cursor    = Cursors.Hand;

            btn.Paint      += (s, e) => PaintPill(e, btn, filled: true);
            btn.MouseEnter += (s, e) => { if (!_busy) { btn.BackColor = cBlueHover;  btn.Invalidate(); } };
            btn.MouseLeave += (s, e) => { if (!_busy) { btn.BackColor = cBlue;       btn.Invalidate(); } };
            btn.MouseDown  += (s, e) => { if (!_busy) { btn.BackColor = cBlueDeep;   btn.Invalidate(); } };
            btn.MouseUp    += (s, e) => { if (!_busy) { btn.BackColor = cBlueHover;  btn.Invalidate(); } };
        }

        private void StyleOutline(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = cWhite;
            btn.ForeColor = cBlue;
            btn.Font      = new Font("Segoe UI", 10F, FontStyle.Bold);
            btn.Cursor    = Cursors.Hand;

            btn.Paint      += (s, e) => PaintPill(e, btn, filled: false);
            btn.MouseEnter += (s, e) => { btn.BackColor = cBlueTint; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { btn.BackColor = cWhite;    btn.Invalidate(); };
            btn.MouseDown  += (s, e) => { btn.BackColor = Color.FromArgb(219, 234, 254); btn.Invalidate(); };
            btn.MouseUp    += (s, e) => { btn.BackColor = cBlueTint; btn.Invalidate(); };
        }

        private void PaintPill(PaintEventArgs e, Button btn, bool filled)
        {
            var g    = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, btn.Width - 1, btn.Height - 1);
            using var path = RoundedPath(rect, btn.Height / 2);

            if (filled)
            {
                // Top-to-bottom gradient on primary button
                using var br = new LinearGradientBrush(rect,
                    Lighten(btn.BackColor, 14), btn.BackColor,
                    LinearGradientMode.Vertical);
                g.FillPath(br, path);
            }
            else
            {
                using var br  = new SolidBrush(btn.BackColor);
                g.FillPath(br, path);
                using var pen = new Pen(cBlue, 1.5f);
                g.DrawPath(pen, path);
            }

            string label = _busy && filled ? "Connexion…" : btn.Text;
            TextRenderer.DrawText(g, label, btn.Font, rect, btn.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine);
        }

        // ??????????????????????????????????????????????????????????????????????
        // PAINT HANDLERS
        // ??????????????????????????????????????????????????????????????????????
        private void PaintFormBackground(object? sender, PaintEventArgs e)
        {
            // Soft decorative circles in page bg (gives a modern SaaS look)
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            DrawDecorCircle(g, -80, -80,  320, Color.FromArgb(22, 43, 108, 176));
            DrawDecorCircle(g, ClientSize.Width - 100, ClientSize.Height - 100, 260, Color.FromArgb(18, 43, 108, 176));
            DrawDecorCircle(g, ClientSize.Width - 40,  10, 140, Color.FromArgb(12, 66, 153, 225));
        }

        private static void DrawDecorCircle(Graphics g, int x, int y, int d, Color c)
        {
            using var br = new SolidBrush(c);
            g.FillEllipse(br, x, y, d, d);
        }

        private void PaintCard(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Multi-layer soft shadow
            (int off, int alpha)[] layers =
            {
                (2, 5), (4, 8), (6, 11), (9, 9), (13, 6), (18, 4)
            };
            foreach (var (off, alpha) in layers)
            {
                var sr = new Rectangle(off / 2, off, card.Width - off, card.Height - off);
                using var shBr   = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0));
                using var shPath = RoundedPath(sr, Radius);
                g.FillPath(shBr, shPath);
            }

            // Card surface
            using var cardPath = RoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), Radius);
            using var cardBr   = new SolidBrush(cWhite);
            g.FillPath(cardBr, cardPath);

            // Very subtle outer border for definition
            using var borderPen = new Pen(Color.FromArgb(18, 0, 0, 0), 1f);
            g.DrawPath(borderPen, cardPath);
        }

        private void PaintHeader(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Extend rect below so lower corners are straight (hidden by body)
            var rect = new Rectangle(0, 0, topBanner.Width, topBanner.Height + Radius);
            using var path = RoundedPath(rect, Radius, topOnly: true);

            // Diagonal gradient for depth
            using var br = new LinearGradientBrush(
                new Point(0, 0), new Point(topBanner.Width, topBanner.Height),
                Color.FromArgb(55, 100, 180),
                cBlueDeep);
            g.FillPath(br, path);

            // Subtle highlight line at top edge
            using var hlPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f);
            g.DrawLine(hlPen, Radius, 0, topBanner.Width - Radius, 0);
        }

        private void PaintEyeIcon(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = btnEye.ClientRectangle;
            int cx = r.Width / 2, cy = r.Height / 2;

            using var pen = new Pen(Color.FromArgb(160, 174, 192), 1.6f)
                { StartCap = LineCap.Round, EndCap = LineCap.Round };

            if (!_showPassword)
            {
                // Eye outline (arc)
                g.DrawArc(pen, cx - 8, cy - 5, 16, 10, 0, 180);
                g.DrawArc(pen, cx - 8, cy - 5, 16, 10, 180, 180);
                // Pupil
                using var dotBr = new SolidBrush(Color.FromArgb(160, 174, 192));
                g.FillEllipse(dotBr, cx - 3, cy - 3, 6, 6);
            }
            else
            {
                // Eye-off: same eye with a diagonal slash
                g.DrawArc(pen, cx - 8, cy - 5, 16, 10, 0, 180);
                g.DrawArc(pen, cx - 8, cy - 5, 16, 10, 180, 180);
                using var slashPen = new Pen(Color.FromArgb(160, 174, 192), 1.6f)
                    { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawLine(slashPen, cx - 9, cy + 7, cx + 9, cy - 7);
            }
        }

        // ??????????????????????????????????????????????????????????????????????
        // GEOMETRY HELPERS
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

        private static GraphicsPath Pill(Rectangle r)
            => RoundedPath(r, Math.Min(r.Width, r.Height) / 2);

        private static Color Lighten(Color c, int by) =>
            Color.FromArgb(c.A,
                Math.Min(255, c.R + by),
                Math.Min(255, c.G + by),
                Math.Min(255, c.B + by));

        private void OnFormDrag(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(Handle, 0xA1, 0x2, 0);
            }
        }

        // ??????????????????????????????????????????????????????????????????????
        // STATE HELPERS
        // ??????????????????????????????????????????????????????????????????????
        private void ShowError(string message)
        {
            lblErrorMsg.Text = message;
            pnlError.Visible = true;
            pnlError.Invalidate();
        }

        private void HideError()
        {
            pnlError.Visible = false;
        }

        private void SetBusy(bool busy)
        {
            _busy               = busy;
            btnLogin.Enabled    = !busy;
            btnRegister.Enabled = !busy;
            btnLogin.BackColor  = busy ? cBlueDeep : cBlue;
            btnLogin.Invalidate();
        }

        // ??????????????????????????????????????????????????????????????????????
        // BUTTON HANDLERS
        // ??????????????????????????????????????????????????????????????????????
        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            HideError();
            var email    = txtEmail.Text?.Trim()    ?? string.Empty;
            var password = txtPassword.Text         ?? string.Empty;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowError("Identifiant et mot de passe requis.");
                return;
            }

            SetBusy(true);
            try
            {
                await using var conn = _bdd.CreateConnection();
                await conn.OpenAsync();
                await using var cmd  = conn.CreateCommand();
                cmd.CommandText =
                    "SELECT id, mot_de_passe, actif FROM utilisateur WHERE email = @email LIMIT 1";
                cmd.Parameters.Add(new MySqlParameter("@email", email));

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var id     = reader.GetInt32(0);
                    var stored = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                    var actif  = !reader.IsDBNull(2) && reader.GetBoolean(2);

                    if (!actif) { ShowError("Ce compte est désactivé."); return; }

                    if (Auth.VerifyPassword(stored, password))
                    {
                        // Save or clear remember-me credentials
                        if (chkRememberMe.Checked)
                            RememberMe.Save(email, password);
                        else
                            RememberMe.Clear();

                        AuthenticatedUserId = id;
                        AuthenticatedEmail  = email;
                        DialogResult        = DialogResult.OK;
                        Close();
                        return;
                    }
                }

                ShowError("Identifiant ou mot de passe incorrect.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur connexion : " + ex.Message, "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { SetBusy(false); }
        }

        private async void BtnRegister_Click(object? sender, EventArgs e)
        {
            HideError();
            var email    = txtEmail.Text?.Trim()    ?? string.Empty;
            var password = txtPassword.Text         ?? string.Empty;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowError("Identifiant et mot de passe requis.");
                return;
            }

            SetBusy(true);
            try
            {
                await using var conn = _bdd.CreateConnection();
                await conn.OpenAsync();

                await using (var check = conn.CreateCommand())
                {
                    check.CommandText = "SELECT COUNT(1) FROM utilisateur WHERE email = @email";
                    check.Parameters.Add(new MySqlParameter("@email", email));
                    if (Convert.ToInt32(await check.ExecuteScalarAsync()) > 0)
                    {
                        ShowError("Un compte avec cet identifiant existe déjà.");
                        return;
                    }
                }

                var hash = Auth.HashPassword(password);
                await using var cmd = conn.CreateCommand();
                cmd.CommandText =
                    "INSERT INTO utilisateur (email, mot_de_passe, date_inscription, actif) VALUES (@email, @pwd, @date, 1)";
                cmd.Parameters.Add(new MySqlParameter("@email", email));
                cmd.Parameters.Add(new MySqlParameter("@pwd",   hash));
                cmd.Parameters.Add(new MySqlParameter("@date",  DateTime.UtcNow.Date));
                await cmd.ExecuteNonQueryAsync();

                MessageBox.Show(
                    "Inscription réussie. Vous pouvez maintenant vous connecter.",
                    "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur inscription : " + ex.Message, "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { SetBusy(false); }
        }
    }

    // ?? P/Invoke for borderless drag ??????????????????????????????????????????
    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
    }
}
