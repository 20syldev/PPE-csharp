using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace PPE
{
    public class AddDialog : Window
    {
        private readonly TextBox _nom = null!, _adr = null!, _ville = null!, _code = null!;

        public AddDialog()
        {
            Title = "Nouveau client";
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(Color.Parse("#252526"));
            SizeToContent = SizeToContent.Height;
            CanResize = false;
            Width = 360;

            AddHandler(KeyDownEvent, (s, e) =>
            {
                if (e.Key == Key.Escape) Close(null);
            }, Avalonia.Interactivity.RoutingStrategies.Tunnel, true);

            var p = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 12
            };

            p.Children.Add(new TextBlock
            {
                Text = "Nouveau client",
                Foreground = new SolidColorBrush(Color.Parse("#E0E0E0")),
                Margin = new Thickness(0, 0, 0, 8),
                FontWeight = FontWeight.SemiBold,
                FontSize = 16
            });

            p.Children.Add(Input("Nom", out _nom));
            p.Children.Add(Input("Adresse", out _adr));

            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,12,Auto") };
            var v = Input("Ville", out _ville);

            Grid.SetColumn(v, 0);
            row.Children.Add(v);

            var c = Input("Code postal", out _code);
            _code.Width = 100;

            Grid.SetColumn(c, 2);
            row.Children.Add(c);
            p.Children.Add(row);

            var btns = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 12, 0, 0),
                Spacing = 8
            };

            var cancel = new Button
            {
                Content = "Annuler",
                Background = new SolidColorBrush(Color.Parse("#3C3C3C")),
                Foreground = new SolidColorBrush(Color.Parse("#E0E0E0")),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(16, 8),
                FontSize = 13
            };
            cancel.Click += (s, e) => Close(null);
            btns.Children.Add(cancel);

            var save = new Button
            {
                Content = "Ajouter",
                Background = new SolidColorBrush(Color.Parse("#0D6EFD")),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(16, 8),
                Foreground = Brushes.White,
                FontSize = 13
            };
            save.Click += (s, e) => Close(new Client(_nom.Text ?? "", _adr.Text ?? "", _ville.Text ?? "", _code.Text ?? ""));
            btns.Children.Add(save);

            p.Children.Add(btns);
            Content = p;
        }

        private static StackPanel Input(string label, out TextBox box)
        {
            var s = new StackPanel { Spacing = 4 };
            s.Children.Add(new TextBlock
            {
                Text = label,
                Foreground = new SolidColorBrush(Color.Parse("#858585")),
                FontSize = 12
            });
            box = new TextBox
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#555555")),
                Background = new SolidColorBrush(Color.Parse("#3C3C3C")),
                Foreground = new SolidColorBrush(Color.Parse("#E0E0E0")),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8),
                FontSize = 13
            };
            s.Children.Add(box);
            return s;
        }
    }

    public class EditDialog : Window
    {
        private readonly TextBox _nom = null!, _adr = null!, _ville = null!, _code = null!;
        private readonly Guid _id;

        public EditDialog(Client client)
        {
            _id = client.Id ?? Guid.Empty;
            Title = "Modifier client";
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(Color.Parse("#252526"));
            SizeToContent = SizeToContent.Height;
            CanResize = false;
            Width = 360;

            AddHandler(KeyDownEvent, (s, e) =>
            {
                if (e.Key == Key.Escape) Close(null);
            }, Avalonia.Interactivity.RoutingStrategies.Tunnel, true);

            var p = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 12
            };

            p.Children.Add(new TextBlock
            {
                Text = "Modifier client",
                Foreground = new SolidColorBrush(Color.Parse("#E0E0E0")),
                Margin = new Thickness(0, 0, 0, 8),
                FontWeight = FontWeight.SemiBold,
                FontSize = 16
            });

            p.Children.Add(Input("Nom", out _nom, client.Nom));
            p.Children.Add(Input("Adresse", out _adr, client.Adresse));

            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,12,Auto") };
            var v = Input("Ville", out _ville, client.Ville);

            Grid.SetColumn(v, 0);
            row.Children.Add(v);

            var c = Input("Code postal", out _code, client.Code);
            _code.Width = 100;

            Grid.SetColumn(c, 2);
            row.Children.Add(c);
            p.Children.Add(row);

            var btns = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 12, 0, 0),
                Spacing = 8
            };

            var cancel = new Button
            {
                Content = "Annuler",
                Background = new SolidColorBrush(Color.Parse("#3C3C3C")),
                Foreground = new SolidColorBrush(Color.Parse("#E0E0E0")),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(16, 8),
                FontSize = 13
            };
            cancel.Click += (s, e) => Close(null);
            btns.Children.Add(cancel);

            var save = new Button
            {
                Content = "Enregistrer",
                Background = new SolidColorBrush(Color.Parse("#0D6EFD")),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(16, 8),
                Foreground = Brushes.White,
                FontSize = 13
            };
            save.Click += (s, e) => Close(new Client(_nom.Text ?? "", _adr.Text ?? "", _ville.Text ?? "", _code.Text ?? "") { Id = _id });
            btns.Children.Add(save);

            p.Children.Add(btns);
            Content = p;
        }

        private static StackPanel Input(string label, out TextBox box, string? value)
        {
            var s = new StackPanel { Spacing = 4 };
            s.Children.Add(new TextBlock
            {
                Text = label,
                Foreground = new SolidColorBrush(Color.Parse("#858585")),
                FontSize = 12
            });
            box = new TextBox
            {
                Text = value ?? "",
                BorderBrush = new SolidColorBrush(Color.Parse("#555555")),
                Background = new SolidColorBrush(Color.Parse("#3C3C3C")),
                Foreground = new SolidColorBrush(Color.Parse("#E0E0E0")),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8),
                FontSize = 13
            };
            s.Children.Add(box);
            return s;
        }
    }

    public class ConfirmDialog : Window
    {
        public ConfirmDialog(string msg)
        {
            Title = "Confirmation";
            Background = new SolidColorBrush(Color.Parse("#252526"));
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            SizeToContent = SizeToContent.Height;
            CanResize = false;
            Width = 320;

            AddHandler(KeyDownEvent, (s, e) =>
            {
                if (e.Key == Key.Escape) Close(false);
            }, Avalonia.Interactivity.RoutingStrategies.Tunnel, true);

            var p = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 16
            };
            p.Children.Add(new TextBlock
            {
                Text = msg,
                Foreground = new SolidColorBrush(Color.Parse("#E0E0E0")),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14
            });

            var btns = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };

            var no = new Button
            {
                Content = "Annuler",
                Background = new SolidColorBrush(Color.Parse("#3C3C3C")),
                Foreground = new SolidColorBrush(Color.Parse("#E0E0E0")),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(16, 8),
                FontSize = 13
            };
            no.Click += (s, e) => Close(false);
            btns.Children.Add(no);

            var yes = new Button
            {
                Content = "Supprimer",
                Background = new SolidColorBrush(Color.Parse("#DC2626")),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(16, 8),
                Foreground = Brushes.White,
                FontSize = 13
            };
            yes.Click += (s, e) => Close(true);
            btns.Children.Add(yes);

            p.Children.Add(btns);
            Content = p;
        }
    }
}