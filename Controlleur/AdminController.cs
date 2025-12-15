using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.VisualTree;
using PPE.Modele;

namespace PPE.Controlleur
{
    public partial class AdminWindow : Window
    {
        private List<Utilisateur> _all = [];
        private readonly ObservableCollection<Utilisateur> _data = [];

        public AdminWindow()
        {
            InitializeComponent();

            // Mettre à jour le message de bienvenue
            if (Utilisateur.Current != null)
            {
                _welcomeUser.Text = $"Connecté en tant que {Utilisateur.Current.Login}";
            }

            // Événements du menu
            _menuAccueil.Click += (s, e) => ShowPage("accueil");
            _menuClients.Click += (s, e) => ShowPage("utilisateurs");

            // Déconnexion
            _logout.Click += (s, e) =>
            {
                Utilisateur.Current = null;
                new LoginWindow().Show();
                Close();
            };

            // Configurer le DataGrid
            _grid.ItemsSource = _data;
            _grid.SelectionChanged += (s, e) => _btnDel.IsVisible = _grid.SelectedItem != null;
            _grid.DoubleTapped += (s, e) =>
            {
                if (e.Source is Control ctrl && ctrl.FindAncestorOfType<DataGridRow>() != null)
                    Edit();
            };

            // Événements de filtrage
            _search.TextChanged += (s, e) => Filter();
            _filter.SelectionChanged += (s, e) => Filter();

            // Désélectionner au clic en dehors
            PointerPressed += (s, e) =>
            {
                if (e.Source is Control ctrl && ctrl.FindAncestorOfType<DataGridRow>() == null)
                    _grid.SelectedItem = null;
            };

            // Supprimer
            _btnDel.Click += (s, e) => Delete();

            // Actualiser
            _btnRefresh.Click += (s, e) => LoadUtilisateurs();

            // Menu contextuel - données sensibles
            _menuShowPassword.Click += async (s, e) =>
            {
                if (_grid.SelectedItem is Utilisateur u)
                    await new InfoDialog("Mot de passe", u.Password ?? "(non disponible)").ShowDialog(this);
            };
            _menuShowCode.Click += async (s, e) =>
            {
                if (_grid.SelectedItem is Utilisateur u)
                    await new InfoDialog("Code utilisateur", u.IdCode?.ToString() ?? "(non disponible)").ShowDialog(this);
            };

            // Page initiale
            ShowPage("accueil");
        }

        private void ShowPage(string page)
        {
            // Masquer toutes les pages
            _pageAccueil.IsVisible = false;
            _pageClients.IsVisible = false;

            // Mettre à jour les classes des boutons de menu
            _menuAccueil.Classes.Clear();
            _menuClients.Classes.Clear();
            _menuAccueil.Classes.Add("menuItem");
            _menuClients.Classes.Add("menuItem");

            switch (page)
            {
                case "accueil":
                    _pageAccueil.IsVisible = true;
                    _menuAccueil.Classes.Add("active");
                    break;
                case "utilisateurs":
                    _pageClients.IsVisible = true;
                    _menuClients.Classes.Add("active");
                    LoadUtilisateurs();
                    break;
            }
        }

        private void LoadUtilisateurs()
        {
            Console.WriteLine("[ADMIN] Chargement des utilisateurs...");
            try
            {
                if (!Connect.Instance().IsConnect())
                {
                    Console.WriteLine("[ADMIN] Erreur: connexion à la base échouée");
                    _count.Text = "(erreur connexion)";
                    return;
                }
                _all = Utilisateur.ListerTous();
                _data.Clear();
                foreach (var u in _all)
                    _data.Add(u);
                _count.Text = $"({_all.Count})";
                Console.WriteLine($"[ADMIN] {_all.Count} utilisateur(s) chargé(s)");
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ADMIN] Erreur: {ex.Message}");
                _count.Text = $"({ex.Message})";
            }
        }

        private void UpdateEmptyState(bool isSearching = false)
        {
            bool hasData = _data.Count > 0;
            _gridBorder.IsVisible = hasData;
            _emptyState.IsVisible = !hasData && !isSearching && _all.Count == 0;
            _noResults.IsVisible = !hasData && isSearching;
        }

        private void Filter()
        {
            var q = _search.Text?.Trim() ?? "";
            var selected = _filter.SelectedItem;
            var f = selected is ComboBoxItem item ? item.Content?.ToString() : "Tous";

            var res = string.IsNullOrEmpty(q) ? _all : f switch
            {
                "Nom" => _all.Where(c => c.Nom?.Contains(q, StringComparison.OrdinalIgnoreCase) == true),
                "Ville" => _all.Where(c => c.Ville?.Contains(q, StringComparison.OrdinalIgnoreCase) == true),
                "Code postal" => _all.Where(c => c.Code?.StartsWith(q) == true),
                _ => _all.Where(c =>
                    c.Nom?.Contains(q, StringComparison.OrdinalIgnoreCase) == true ||
                    c.Ville?.Contains(q, StringComparison.OrdinalIgnoreCase) == true ||
                    c.Code?.Contains(q) == true ||
                    c.Adresse?.Contains(q, StringComparison.OrdinalIgnoreCase) == true)
            };

            _data.Clear();
            foreach (var c in res) _data.Add(c);

            _count.Text = string.IsNullOrEmpty(q)
                ? $"({_data.Count})"
                : $"({_data.Count}/{_all.Count})";

            UpdateEmptyState(isSearching: !string.IsNullOrEmpty(q));
        }

        private async void Delete()
        {
            if (_grid.SelectedItem is not Utilisateur u || u.Id == null) return;
            Console.WriteLine($"[ADMIN DELETE] Demande de suppression: {u.Nom} (ID: {u.Id})");
            if (await new ConfirmDialog($"Supprimer {u.Nom} ?").ShowDialog<bool>(this))
            {
                Utilisateur.Supprimer(u.Id.Value);
                Console.WriteLine($"[ADMIN DELETE] Utilisateur supprimé: {u.Nom}");
                LoadUtilisateurs();
            }
            else
            {
                Console.WriteLine("[ADMIN DELETE] Suppression annulée");
            }
        }

        private async void Edit()
        {
            if (_grid.SelectedItem is not Utilisateur u || u.Id == null) return;
            Console.WriteLine($"[ADMIN EDIT] Ouverture du dialogue de modification: {u.Nom} (ID: {u.Id})");
            var r = await new EditDialog(u).ShowDialog<Utilisateur?>(this);
            if (r != null)
            {
                r.Id = u.Id;
                if (r.Modifier())
                {
                    Console.WriteLine($"[ADMIN EDIT] Utilisateur modifié: {r.Nom} ({r.Ville})");
                    LoadUtilisateurs();
                }
            }
            else
            {
                Console.WriteLine("[ADMIN EDIT] Modification annulée");
            }
        }
    }
}
