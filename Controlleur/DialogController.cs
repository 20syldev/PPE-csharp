using Avalonia.Controls;
using Avalonia.Input;
using PPE.Modele;

namespace PPE.Controlleur
{
    public partial class AddDialog : Window
    {
        public AddDialog()
        {
            InitializeComponent();

            AddHandler(KeyDownEvent, (s, e) =>
            {
                if (e.Key == Key.Escape) Close(null);
            }, Avalonia.Interactivity.RoutingStrategies.Tunnel, true);

            _cancel.Click += (s, e) => Close(null);
            _save.Click += (s, e) =>
            {
                // Valider le code postal
                var codeValidation = Utilisateur.ValidateCodePostal(_code.Text ?? "");
                if (!codeValidation.IsValid)
                {
                    _codeError.Text = codeValidation.Error;
                    _codeError.IsVisible = true;
                    return;
                }
                _codeError.IsVisible = false;

                Close(new Utilisateur(
                    _nom.Text ?? "",
                    _adr.Text ?? "",
                    _ville.Text ?? "",
                    _code.Text ?? ""
                ));
            };

            // Cacher l'erreur quand l'utilisateur tape
            _code.TextChanged += (s, e) => _codeError.IsVisible = false;
        }
    }

    public partial class EditDialog : Window
    {
        private readonly Guid _id;

        public EditDialog() => InitializeComponent();

        public EditDialog(Utilisateur utilisateur) : this()
        {
            _id = utilisateur.Id ?? Guid.Empty;
            var userName = utilisateur.Login ?? utilisateur.Nom ?? "inconnu";
            Title = $"Modifier l'utilisateur \"{userName}\"";
            _title.Text = $"Modifier l'utilisateur \"{userName}\"";
            _nom.Text = utilisateur.Nom;
            _adr.Text = utilisateur.Adresse;
            _ville.Text = utilisateur.Ville;
            _code.Text = utilisateur.Code;

            AddHandler(KeyDownEvent, (s, e) =>
            {
                if (e.Key == Key.Escape) Close(null);
            }, Avalonia.Interactivity.RoutingStrategies.Tunnel, true);

            _cancel.Click += (s, e) => Close(null);
            _save.Click += (s, e) =>
            {
                // Valider le code postal
                var codeValidation = Utilisateur.ValidateCodePostal(_code.Text ?? "");
                if (!codeValidation.IsValid)
                {
                    _codeError.Text = codeValidation.Error;
                    _codeError.IsVisible = true;
                    return;
                }
                _codeError.IsVisible = false;

                Close(new Utilisateur(
                    _nom.Text ?? "",
                    _adr.Text ?? "",
                    _ville.Text ?? "",
                    _code.Text ?? ""
                ) { Id = _id });
            };

            // Cacher l'erreur quand l'utilisateur tape
            _code.TextChanged += (s, e) => _codeError.IsVisible = false;
        }
    }

    public partial class ConfirmDialog : Window
    {
        public ConfirmDialog() => InitializeComponent();

        public ConfirmDialog(string msg) : this()
        {
            _msg.Text = msg;

            AddHandler(KeyDownEvent, (s, e) =>
            {
                if (e.Key == Key.Escape) Close(false);
            }, Avalonia.Interactivity.RoutingStrategies.Tunnel, true);

            _cancel.Click += (s, e) => Close(false);
            _confirm.Click += (s, e) => Close(true);
        }
    }

    public partial class SettingsDialog : Window
    {
        public SettingsDialog() => InitializeComponent();

        public SettingsDialog(Utilisateur utilisateur) : this()
        {
            _nom.Text = utilisateur.Nom;
            _adr.Text = utilisateur.Adresse;
            _ville.Text = utilisateur.Ville;
            _code.Text = utilisateur.Code;

            AddHandler(KeyDownEvent, (s, e) =>
            {
                if (e.Key == Key.Escape) Close(null);
            }, Avalonia.Interactivity.RoutingStrategies.Tunnel, true);

            _cancel.Click += (s, e) => Close(null);
            _save.Click += (s, e) =>
            {
                // Valider le code postal
                var codeValidation = Utilisateur.ValidateCodePostal(_code.Text ?? "");
                if (!codeValidation.IsValid)
                {
                    _codeError.Text = codeValidation.Error;
                    _codeError.IsVisible = true;
                    return;
                }
                _codeError.IsVisible = false;

                Close(new Utilisateur(
                    _nom.Text ?? "",
                    _adr.Text ?? "",
                    _ville.Text ?? "",
                    _code.Text ?? ""
                ));
            };

            // Cacher l'erreur quand l'utilisateur tape
            _code.TextChanged += (s, e) => _codeError.IsVisible = false;
        }
    }

    public partial class InfoDialog : Window
    {
        public InfoDialog() => InitializeComponent();

        public InfoDialog(string title, string content) : this()
        {
            Title = title;
            _title.Text = title;
            _content.Text = content;

            AddHandler(KeyDownEvent, (s, e) =>
            {
                if (e.Key == Key.Escape) Close();
            }, Avalonia.Interactivity.RoutingStrategies.Tunnel, true);

            _close.Click += (s, e) => Close();
        }
    }
}
