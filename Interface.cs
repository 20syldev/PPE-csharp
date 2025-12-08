using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;

namespace PPE
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
                desktop.MainWindow = new MainWindow();
            base.OnFrameworkInitializationCompleted();
        }
    }

    public partial class MainWindow : Window
    {
        private List<Client> _all = [];
        private readonly ObservableCollection<Client> _data = [];

        public MainWindow()
        {
            InitializeComponent();

            // Événements des boutons
            _btnDel.Click += (s, e) => Delete();
            _btnAdd.Click += (s, e) => Add();
            _btnAddEmpty.Click += (s, e) => Add();

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

            // Charger quand la fenêtre est prête
            Opened += (s, e) => Load();
        }

        private void Load()
        {
            Console.WriteLine("[LOAD] Chargement des clients...");
            try
            {
                if (!Connect.Instance().IsConnect())
                {
                    Console.WriteLine("[LOAD] Erreur: connexion à la base échouée");
                    _count.Text = "(erreur connexion)";
                    return;
                }
                _all = Client.ListerTous();
                _data.Clear();
                foreach (var c in _all)
                    _data.Add(c);
                _count.Text = $"({_all.Count})";
                Console.WriteLine($"[LOAD] {_all.Count} client(s) chargé(s)");
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

        private async void Add()
        {
            Console.WriteLine("[ADD] Ouverture du dialogue d'ajout");
            var r = await new AddDialog().ShowDialog<Client?>(this);
            if (r?.Enregistrer() == true)
            {
                Console.WriteLine($"[ADD] Client ajouté: {r.Nom} ({r.Ville})");
                Load();
            }
            else
            {
                Console.WriteLine("[ADD] Ajout annulé");
            }
        }

        private async void Delete()
        {
            if (_grid.SelectedItem is not Client c || c.Id == null) return;
            Console.WriteLine($"[DELETE] Demande de suppression: {c.Nom} (ID: {c.Id})");
            if (await new ConfirmDialog($"Supprimer {c.Nom} ?").ShowDialog<bool>(this))
            {
                Client.Supprimer(c.Id.Value);
                Console.WriteLine($"[DELETE] Client supprimé: {c.Nom}");
                Load();
            }
            else
            {
                Console.WriteLine("[DELETE] Suppression annulée");
            }
        }

        private async void Edit()
        {
            if (_grid.SelectedItem is not Client c || c.Id == null) return;
            Console.WriteLine($"[EDIT] Ouverture du dialogue de modification: {c.Nom} (ID: {c.Id})");
            var r = await new EditDialog(c).ShowDialog<Client?>(this);
            if (r?.Modifier() == true)
            {
                Console.WriteLine($"[EDIT] Client modifié: {r.Nom} ({r.Ville})");
                Load();
            }
            else
            {
                Console.WriteLine("[EDIT] Modification annulée");
            }
        }
    }
}
