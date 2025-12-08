using Avalonia.Controls;
using Avalonia.Input;

namespace PPE
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
            _save.Click += (s, e) => Close(new Client(
                _nom.Text ?? "",
                _adr.Text ?? "",
                _ville.Text ?? "",
                _code.Text ?? ""
            ));
        }
    }

    public partial class EditDialog : Window
    {
        private readonly Guid _id;

        public EditDialog() => InitializeComponent();

        public EditDialog(Client client) : this()
        {
            _id = client.Id ?? Guid.Empty;
            _nom.Text = client.Nom;
            _adr.Text = client.Adresse;
            _ville.Text = client.Ville;
            _code.Text = client.Code;

            AddHandler(KeyDownEvent, (s, e) =>
            {
                if (e.Key == Key.Escape) Close(null);
            }, Avalonia.Interactivity.RoutingStrategies.Tunnel, true);

            _cancel.Click += (s, e) => Close(null);
            _save.Click += (s, e) => Close(new Client(
                _nom.Text ?? "",
                _adr.Text ?? "",
                _ville.Text ?? "",
                _code.Text ?? ""
            ) { Id = _id });
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
}
