using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace PPE
{
    public class App : Application
    {
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = new MainWindow();
            base.OnFrameworkInitializationCompleted();
        }
    }

    public class MainWindow : Window
    {
        private DataGrid _grid = null!;
        private TextBox _search = null!;
        private ComboBox _filter = null!;
        private TextBlock _count = null!;
        private Border _gridBorder = null!;
        private StackPanel _emptyState = null!;
        private List<Client> _all = [];
        private readonly ObservableCollection<Client> _data = [];

        public MainWindow()
        {
            Title = "Gestion Clients";
            Width = 900;
            Height = 600;
            MinWidth = 600;
            MinHeight = 400;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(Color.Parse("#1E1E1E"));

            Build();

            // Charger quand la fenêtre est prête
            Opened += (s, e) => Load();
        }

        private void Build()
        {
            var root = new DockPanel();

            // Toolbar
            var bar = new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#3C3C3C")),
                Background = new SolidColorBrush(Color.Parse("#252526")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(16, 12)
            };

            var toolbar = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };

            // Gauche: titre
            var title = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };
            title.Children.Add(new TextBlock
            {
                Text = "Clients",
                Foreground = new SolidColorBrush(Color.Parse("#E0E0E0")),
                FontWeight = FontWeight.SemiBold,
                FontSize = 16
            });
            _count = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.Parse("#858585")),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13
            };
            title.Children.Add(_count);
            Grid.SetColumn(title, 0);
            toolbar.Children.Add(title);

            // Droite: actions
            var actions = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };

            _filter = new ComboBox
            {
                ItemsSource = new[] { "Tous", "Nom", "Ville", "Code postal" },
                BorderBrush = new SolidColorBrush(Color.Parse("#555555")),
                Background = new SolidColorBrush(Color.Parse("#3C3C3C")),
                Foreground = new SolidColorBrush(Color.Parse("#E0E0E0")),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                CornerRadius = new CornerRadius(6),
                SelectedIndex = 0,
                MinWidth = 120,
                FontSize = 13
            };
            _filter.SelectionChanged += (s, e) => Filter();
            actions.Children.Add(_filter);

            _search = new TextBox
            {
                Watermark = "Rechercher...",
                BorderBrush = new SolidColorBrush(Color.Parse("#555555")),
                Background = new SolidColorBrush(Color.Parse("#3C3C3C")),
                Foreground = new SolidColorBrush(Color.Parse("#E0E0E0")),
                VerticalAlignment = VerticalAlignment.Center,
                CornerRadius = new CornerRadius(6),
                FontSize = 13,
                Width = 200
            };
            _search.TextChanged += (s, e) => Filter();
            actions.Children.Add(_search);

            var btnDel = new Button
            {
                Content = "Supprimer",
                Background = new SolidColorBrush(Color.Parse("#5A2D2D")),
                Foreground = new SolidColorBrush(Color.Parse("#F87171")),
                VerticalAlignment = VerticalAlignment.Center,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(14, 8),
                FontSize = 13
            };
            btnDel.Click += (s, e) => Delete();
            actions.Children.Add(btnDel);

            var btnAdd = new Button
            {
                Content = "+ Ajouter",
                Background = new SolidColorBrush(Color.Parse("#0D6EFD")),
                VerticalAlignment = VerticalAlignment.Center,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(14, 8),
                Foreground = Brushes.White,
                FontSize = 13
            };
            btnAdd.Click += (s, e) => Add();
            actions.Children.Add(btnAdd);

            Grid.SetColumn(actions, 2);
            toolbar.Children.Add(actions);

            bar.Child = toolbar;
            DockPanel.SetDock(bar, Dock.Top);
            root.Children.Add(bar);

            // Grille des clients
            _grid = new DataGrid
            {
                ItemsSource = _data,
                RowBackground = new SolidColorBrush(Color.Parse("#252526")),
                Background = new SolidColorBrush(Color.Parse("#252526")),
                Foreground = new SolidColorBrush(Color.Parse("#E0E0E0")),
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                SelectionMode = DataGridSelectionMode.Single,
                BorderThickness = new Thickness(0),
                AutoGenerateColumns = false,
                CanUserResizeColumns = true,
                CanUserSortColumns = true,
                IsReadOnly = true,
                RowHeight = 44,
            };

            _grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Nom",
                Binding = new Binding("Nom"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
            _grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Adresse",
                Binding = new Binding("Adresse"),
                Width = new DataGridLength(2, DataGridLengthUnitType.Star)
            });
            _grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Ville",
                Binding = new Binding("Ville"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
            _grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Code postal",
                Binding = new Binding("Code"),
                Width = new DataGridLength(100)
            });

            _grid.DoubleTapped += (s, e) =>
            {
                if (
                    e.Source is Control ctrl &&
                    ctrl.FindAncestorOfType<DataGridRow>() != null
                ) {
                    Edit();
                }
            };

            _gridBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#3C3C3C")),
                Background = new SolidColorBrush(Color.Parse("#252526")),
                BorderThickness = new Thickness(1, 1, 1, 0),
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                Margin = new Thickness(16, 12, 16, 16),
                ClipToBounds = true,
                Child = _grid
            };

            // État vide
            var btnAddEmpty = new Button
            {
                Content = "+ Ajouter un client",
                Background = new SolidColorBrush(Color.Parse("#0D6EFD")),
                HorizontalAlignment = HorizontalAlignment.Center,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(16, 10),
                Foreground = Brushes.White,
                FontSize = 13
            };
            btnAddEmpty.Click += (s, e) => Add();

            _emptyState = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Aucun client",
                        Foreground = new SolidColorBrush(Color.Parse("#858585")),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontWeight = FontWeight.SemiBold,
                        FontSize = 18
                    },
                    new TextBlock
                    {
                        Text = "Commencez par ajouter votre premier client",
                        Foreground = new SolidColorBrush(Color.Parse("#666666")),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontSize = 13
                    },
                    btnAddEmpty
                },
                IsVisible = false
            };

            root.Children.Add(_gridBorder);
            root.Children.Add(_emptyState);
            Content = root;
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

        private void UpdateEmptyState()
        {
            bool hasData = _data.Count > 0;
            _gridBorder.IsVisible = hasData;
            _emptyState.IsVisible = !hasData;
        }

        private void Filter()
        {
            var q = _search.Text?.Trim() ?? "";
            var f = _filter.SelectedItem?.ToString() ?? "Tous";

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

            UpdateEmptyState();
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