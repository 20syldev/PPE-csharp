using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;
using PPE.Modele;

namespace PPE.Controlleur
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Afficher la page de connexion d'abord
                desktop.MainWindow = new LoginWindow();
            }
            base.OnFrameworkInitializationCompleted();
        }
    }

    public partial class MainWindow : Window
    {
        private List<Utilisateur> _all = [];
        private readonly ObservableCollection<Utilisateur> _data = [];

        public MainWindow()
        {
            InitializeComponent();

            // Mettre à jour le titre avec le nom de l'utilisateur connecté
            if (Utilisateur.Current != null)
            {
                Title = $"Gestion Utilisateurs - {Utilisateur.Current.Login}";
            }

            // Événements des boutons
            _btnDel.Click += (s, e) => Delete();
            _btnAdd.Click += (s, e) => Add();
            _btnAddEmpty.Click += (s, e) => Add();

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

            // Actualiser
            _btnRefresh.Click += (s, e) => Load();

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

            // Charger quand la fenêtre est prête
            Opened += (s, e) => Load();
        }

        private void Load()
        {
            Console.WriteLine("[LOAD] Chargement des utilisateurs...");
            try
            {
                if (!Connect.Instance().IsConnect())
                {
                    Console.WriteLine("[LOAD] Erreur: connexion à la base échouée");
                    _count.Text = "(erreur connexion)";
                    return;
                }
                _all = Utilisateur.ListerTous();
                _data.Clear();
                foreach (var u in _all)
                    _data.Add(u);
                _count.Text = $"({_all.Count})";
                Console.WriteLine($"[LOAD] {_all.Count} utilisateur(s) chargé(s)");
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOAD] Erreur: {ex.Message}");
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
                "Nom" => _all.Where(u => u.Nom?.Contains(q, StringComparison.OrdinalIgnoreCase) == true),
                "Ville" => _all.Where(u => u.Ville?.Contains(q, StringComparison.OrdinalIgnoreCase) == true),
                "Code postal" => _all.Where(u => u.Code?.StartsWith(q) == true),
                _ => _all.Where(u =>
                    u.Nom?.Contains(q, StringComparison.OrdinalIgnoreCase) == true ||
                    u.Ville?.Contains(q, StringComparison.OrdinalIgnoreCase) == true ||
                    u.Code?.Contains(q) == true ||
                    u.Adresse?.Contains(q, StringComparison.OrdinalIgnoreCase) == true)
            };

            _data.Clear();
            foreach (var u in res) _data.Add(u);

            _count.Text = string.IsNullOrEmpty(q)
                ? $"({_data.Count})"
                : $"({_data.Count}/{_all.Count})";

            UpdateEmptyState(isSearching: !string.IsNullOrEmpty(q));
        }

        private async void Add()
        {
            Console.WriteLine("[ADD] Ouverture du dialogue d'ajout");
            var r = await new AddDialog().ShowDialog<Utilisateur?>(this);
            if (r != null)
            {
                // Créer un nouveau compte utilisateur
                if (r.CreerCompte("TempPass!@34")) // Mot de passe temporaire valide
                {
                    Console.WriteLine($"[ADD] Utilisateur ajouté: {r.Nom} ({r.Ville}) - IdCode: {r.IdCode}");
                    Load();
                }
            }
            else
            {
                Console.WriteLine("[ADD] Ajout annulé");
            }
        }

        private async void Delete()
        {
            if (_grid.SelectedItem is not Utilisateur u || u.Id == null) return;
            Console.WriteLine($"[DELETE] Demande de suppression: {u.Nom} (ID: {u.Id})");
            if (await new ConfirmDialog($"Supprimer {u.Nom} ?").ShowDialog<bool>(this))
            {
                Utilisateur.Supprimer(u.Id.Value);
                Console.WriteLine($"[DELETE] Utilisateur supprimé: {u.Nom}");
                Load();
            }
            else
            {
                Console.WriteLine("[DELETE] Suppression annulée");
            }
        }

        private async void Edit()
        {
            if (_grid.SelectedItem is not Utilisateur u || u.Id == null) return;
            Console.WriteLine($"[EDIT] Ouverture du dialogue de modification: {u.Nom} (ID: {u.Id})");
            var r = await new EditDialog(u).ShowDialog<Utilisateur?>(this);
            if (r?.Modifier() == true)
            {
                Console.WriteLine($"[EDIT] Utilisateur modifié: {r.Nom} ({r.Ville})");
                Load();
            }
            else
            {
                Console.WriteLine("[EDIT] Modification annulée");
            }
        }
    }
}
