using System;
using System.Windows.Forms;
using System.Drawing;

namespace autofact
{
    public partial class Form1 : Form
    {
        private Color clrBackground = Color.FromArgb(225, 225, 225); // Gris fond général
        private Color clrWhite = Color.White;
        private Color clrTextDark = Color.FromArgb(50, 50, 50);      // Titres
        private Color clrTextLight = Color.FromArgb(150, 150, 150);  // Descriptions
        private Color clrBlackBtn = Color.FromArgb(30, 30, 30);      // Bouton Register

        // Conteneurs principaux
        private Panel panelHeader;
        private Panel panelSidebar;
        private Panel panelContent;

        public Form1()
        {
            InitializeComponent();

            // Initialisation de base
            this.DoubleBuffered = true; // Évite le scintillement
            SetupForm();
            BuildHeader();
            BuildSidebar();
            BuildContentArea();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Chargement initial (si nécessaire)
        }

        private void SetupForm()
        {
            this.Text = "AutoFact";
            this.Size = new Size(1280, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.BackColor = clrBackground;
        }

        private void BuildHeader()
        {
            panelHeader = new Panel();
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Height = 80;
            panelHeader.BackColor = clrWhite;
            panelHeader.Padding = new Padding(20, 10, 20, 10);
            this.Controls.Add(panelHeader);

            // -- Partie Gauche (Avatar + Nom) --
            PictureBox avatar = new PictureBox();
            avatar.Size = new Size(50, 50);
            avatar.BackColor = Color.LightBlue; // Placeholder pour l'image
            avatar.Location = new Point(20, 15);
            // Pour faire un rond parfait (cercle), on utilise une astuce de dessin, 
            // mais restons simple pour l'instant : carré
            panelHeader.Controls.Add(avatar);

            Label lblName = new Label();
            lblName.Text = "Nom du compte";
            lblName.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblName.AutoSize = true;
            lblName.Location = new Point(80, 18);
            panelHeader.Controls.Add(lblName);

            Label lblDesc = new Label();
            lblDesc.Text = "Description du rôle";
            lblDesc.ForeColor = clrTextLight;
            lblDesc.AutoSize = true;
            lblDesc.Font = new Font("Segoe UI", 9F);
            lblDesc.Location = new Point(80, 40);
            panelHeader.Controls.Add(lblDesc);

            // -- Partie Centre (Dashboard) --
            Label lblDash = new Label();
            lblDash.Text = "Dashboard";
            lblDash.Font = new Font("Segoe UI", 14F, FontStyle.Regular);
            lblDash.AutoSize = true;
            // On le centre dynamiquement plus tard, ou on utilise l'ancrage
            lblDash.Location = new Point((this.Width / 2) - 50, 25);
            lblDash.Anchor = AnchorStyles.Top; // Reste au milieu
            panelHeader.Controls.Add(lblDash);

            // -- Partie Droite (Boutons Sign in / Register) --
            Button btnRegister = new Button();
            btnRegister.Text = "Register";
            btnRegister.Size = new Size(100, 40);
            btnRegister.BackColor = clrBlackBtn;
            btnRegister.ForeColor = Color.White;
            btnRegister.FlatStyle = FlatStyle.Flat;
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.Location = new Point(this.ClientSize.Width - 140, 20);
            btnRegister.Anchor = AnchorStyles.Top | AnchorStyles.Right; // Collé à droite
            panelHeader.Controls.Add(btnRegister);

            Button btnSignIn = new Button();
            btnSignIn.Text = "Sign in";
            btnSignIn.Size = new Size(100, 40);
            btnSignIn.BackColor = Color.White; // Fond blanc
            btnSignIn.ForeColor = Color.Black;
            btnSignIn.FlatStyle = FlatStyle.Flat;
            // Bordure grise
            btnSignIn.FlatAppearance.BorderColor = Color.Gray;
            btnSignIn.FlatAppearance.BorderSize = 1;
            btnSignIn.Location = new Point(this.ClientSize.Width - 250, 20);
            btnSignIn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            panelHeader.Controls.Add(btnSignIn);
        }

        private void BuildSidebar()
        {
            // Panel conteneur
            panelSidebar = new Panel();
            panelSidebar.Dock = DockStyle.Left;
            panelSidebar.Width = 300; // Largeur du menu
            panelSidebar.BackColor = clrWhite;
            // Petite marge à droite pour voir le gris du fond (comme sur le figma)
            // En Winforms, c'est plus simple de mettre le padding sur le container parent
            // Mais ici, on va tricher en ajoutant une bordure vide
            panelSidebar.Padding = new Padding(20);
            this.Controls.Add(panelSidebar);
            panelSidebar.BringToFront(); // S'assure qu'il est au dessus si besoin

            // Titre "Menu"
            Label lblMenu = new Label();
            lblMenu.Text = "Menu";
            lblMenu.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblMenu.Dock = DockStyle.Top;
            lblMenu.Height = 40;
            panelSidebar.Controls.Add(lblMenu);

            // Ligne de séparation
            Panel line = new Panel();
            line.Height = 1;
            line.BackColor = Color.LightGray;
            line.Dock = DockStyle.Top;
            panelSidebar.Controls.Add(line);

            // Espace
            Panel spacer = new Panel();
            spacer.Height = 20;
            spacer.Dock = DockStyle.Top;
            panelSidebar.Controls.Add(spacer);


            FlowLayoutPanel menuContainer = new FlowLayoutPanel();
            menuContainer.Dock = DockStyle.Fill;
            menuContainer.FlowDirection = FlowDirection.TopDown;
            menuContainer.WrapContents = false;
            menuContainer.AutoScroll = true; // Si l'écran est trop petit
            panelSidebar.Controls.Add(menuContainer);
            menuContainer.BringToFront(); // Doit être après le spacer

            // Ajout des boutons "Custom"
            menuContainer.Controls.Add(CreateMenuItem("Devis", "Menu description.", "📄"));
            menuContainer.Controls.Add(CreateMenuItem("Facturation", "Menu description.", "📄"));
            menuContainer.Controls.Add(CreateMenuItem("Gestion clients", "Menu description.", "👤"));
            menuContainer.Controls.Add(CreateMenuItem("Article Produits/Services", "Menu description.", "📚"));
            menuContainer.Controls.Add(CreateMenuItem("(...)", "Menu description.", "⭐"));

            // Bouton déconnexion (Tout en bas)
            // On le met directement dans panelSidebar, Dock Bottom
            Panel panelDisconnect = CreateMenuItem("Déconnection", "", "🚪");
            panelDisconnect.Dock = DockStyle.Bottom;
            panelSidebar.Controls.Add(panelDisconnect);
        }

        private void BuildContentArea()
        {
            panelContent = new Panel();
            panelContent.Dock = DockStyle.Fill;
            panelContent.BackColor = clrBackground; // Le gris clair
            this.Controls.Add(panelContent);
            panelContent.BringToFront(); // Pour être sûr de l'ordre
        }

        private Panel CreateMenuItem(string title, string desc, string iconSymbol)
        {
            // Le conteneur (ressemble à un bouton)
            Panel p = new Panel();
            p.Size = new Size(260, 70); // Largeur et hauteur fixe
            p.Margin = new Padding(0, 0, 0, 15); // Espace entre les items
            p.Cursor = Cursors.Hand;
            p.BackColor = Color.Transparent;

            // L'icône (Label simulé)
            Label lblIcon = new Label();
            lblIcon.Text = iconSymbol; // Emoji ou charactère
            lblIcon.Font = new Font("Segoe UI Emoji", 16F);
            lblIcon.Location = new Point(0, 5); // En haut à gauche
            lblIcon.AutoSize = true;
            p.Controls.Add(lblIcon);

            // Le Titre
            Label lblTitle = new Label();
            lblTitle.Text = title;
            lblTitle.Font = new Font("Segoe UI", 11F, FontStyle.Regular); // Ou Bold selon l'image
            lblTitle.ForeColor = clrTextDark;
            lblTitle.Location = new Point(35, 8); // Décalé à droite de l'icône
            lblTitle.AutoSize = true;
            p.Controls.Add(lblTitle);

            // La Description (si elle existe)
            if (!string.IsNullOrEmpty(desc))
            {
                Label lblDesc = new Label();
                lblDesc.Text = desc;
                lblDesc.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                lblDesc.ForeColor = clrTextLight;
                lblDesc.Location = new Point(35, 32); // En dessous du titre
                lblDesc.AutoSize = true;
                p.Controls.Add(lblDesc);

                // Pour que le clic sur le texte active aussi le panel
                lblDesc.Click += (s, e) => { OnMenuClick(title); };
            }

            // Gestion du Clic (Un peu partout pour que ce soit fluide)
            p.Click += (s, e) => { OnMenuClick(title); };
            lblIcon.Click += (s, e) => { OnMenuClick(title); };
            lblTitle.Click += (s, e) => { OnMenuClick(title); };

            // Petit effet Hover (survol)
            p.MouseEnter += (s, e) => { p.BackColor = Color.WhiteSmoke; };
            p.MouseLeave += (s, e) => { p.BackColor = Color.Transparent; };

            return p;
        }

        // Action quand on clique sur un menu
        private void OnMenuClick(string menuName)
        {
            MessageBox.Show("Vous avez cliqué sur : " + menuName);
            // Ici, vous chargerez vos différentes vues (UserControl) dans panelContent
        }
    }
}
