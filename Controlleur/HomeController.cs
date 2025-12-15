using Avalonia.Controls;
using PPE.Modele;

namespace PPE.Controlleur
{
    public partial class HomeWindow : Window
    {
        public HomeWindow()
        {
            InitializeComponent();

            _settings.Click += async (s, e) =>
            {
                if (Utilisateur.Current == null) return;

                var result = await new SettingsDialog(Utilisateur.Current).ShowDialog<Utilisateur?>(this);
                if (result != null)
                {
                    Utilisateur.Current.Nom = result.Nom;
                    Utilisateur.Current.Adresse = result.Adresse;
                    Utilisateur.Current.Ville = result.Ville;
                    Utilisateur.Current.Code = result.Code;
                    Utilisateur.Current.Modifier();
                }
            };

            // Déconnexion
            _logout.Click += (s, e) =>
            {
                Utilisateur.Current = null;
                new LoginWindow().Show();
                Close();
            };
        }
    }
}
