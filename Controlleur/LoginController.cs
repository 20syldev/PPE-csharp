using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using PPE.Modele;

namespace PPE.Controlleur
{
    public partial class LoginWindow : Window
    {
        private bool _isRegisterMode = false;

        public LoginWindow()
        {
            InitializeComponent();

            // Toggle entre connexion et inscription
            _tabLogin.Click += (s, e) => SetMode(false);
            _tabRegister.Click += (s, e) => SetMode(true);

            // Validation mot de passe en temps réel (inscription)
            _password.TextChanged += (s, e) => UpdatePasswordStrength();

            // Soumettre
            _submit.Click += (s, e) => Submit();

            // Échap pour fermer
            AddHandler(KeyDownEvent, (s, e) =>
            {
                if (e.Key == Key.Escape) Close();
                if (e.Key == Key.Enter) Submit();
            }, Avalonia.Interactivity.RoutingStrategies.Tunnel, true);

            // Mode initial
            SetMode(false);
        }

        private void SetMode(bool register)
        {
            _isRegisterMode = register;

            // Mettre à jour les tabs
            _tabLogin.Classes.Clear();
            _tabRegister.Classes.Clear();
            _tabLogin.Classes.Add(register ? "secondary" : "primary");
            _tabRegister.Classes.Add(register ? "primary" : "secondary");

            // Afficher/masquer les éléments d'inscription
            _confirmPanel.IsVisible = register;
            _strengthPanel.IsVisible = register;

            // Mettre à jour le bouton
            _submit.Content = register ? "Créer le compte" : "Se connecter";

            // Reset erreurs
            _error.IsVisible = false;
            _loginError.IsVisible = false;

            // Mettre à jour la jauge si en mode inscription
            if (register) UpdatePasswordStrength();
        }

        private void UpdatePasswordStrength()
        {
            if (!_isRegisterMode) return;

            var password = _password.Text ?? "";
            var validation = Utilisateur.ValidatePassword(password);
            var percentage = Utilisateur.GetPasswordStrengthPercentage(password);

            // Mettre à jour la barre (340px = largeur fenêtre 400 - marges 30*2)
            _strengthBar.Width = Math.Max(0, 340 * percentage / 100);
            _strengthBar.Background = new SolidColorBrush(Color.Parse(validation.Color));

            // Mettre à jour le texte
            _strengthText.Text = validation.Strength;
            _strengthText.Foreground = new SolidColorBrush(Color.Parse(validation.Color));

            // Mettre à jour les critères
            UpdateCriterion(_critMin, validation.HasMinLength);
            UpdateCriterion(_critUpper, validation.HasUppercase);
            UpdateCriterion(_critLower, validation.HasLowercase);
            UpdateCriterion(_critDigit, validation.HasDigit);
            UpdateCriterion(_critSpecial, validation.HasSpecialChars);
            UpdateCriterion(_critRepeat, validation.NoConsecutiveRepeat);
        }

        private static void UpdateCriterion(TextBlock criterion, bool isValid)
        {
            criterion.Foreground = new SolidColorBrush(Color.Parse(isValid ? "#34D399" : "#858585"));
        }

        private void Submit()
        {
            _error.IsVisible = false;
            _loginError.IsVisible = false;

            var login = _login.Text?.Trim() ?? "";
            var password = _password.Text ?? "";

            if (string.IsNullOrEmpty(login))
            {
                ShowError("Veuillez entrer un email");
                return;
            }

            var emailValidation = Utilisateur.ValidateEmail(login);
            if (!emailValidation.IsValid)
            {
                _loginError.Text = emailValidation.Error;
                _loginError.IsVisible = true;
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Veuillez entrer un mot de passe");
                return;
            }

            if (_isRegisterMode)
            {
                Register(login, password);
            }
            else
            {
                Login(login, password);
            }
        }

        private void Login(string login, string password)
        {
            var utilisateur = Utilisateur.Authentifier(login, password);

            if (utilisateur != null)
            {
                Console.WriteLine($"[LOGIN] Connexion réussie: {utilisateur.Login}");
                OpenMainWindow();
            }
            else
            {
                ShowError("Email ou mot de passe incorrect");
            }
        }

        private void Register(string login, string password)
        {
            var confirm = _confirmPassword.Text ?? "";

            if (password != confirm)
            {
                ShowError("Les mots de passe ne correspondent pas");
                return;
            }

            var validation = Utilisateur.ValidatePassword(password);
            if (!validation.IsValid)
            {
                ShowError("Le mot de passe ne respecte pas les critères");
                return;
            }

            if (Utilisateur.LoginExists(login))
            {
                _loginError.Text = "Cet email est déjà utilisé";
                _loginError.IsVisible = true;
                return;
            }

            var utilisateur = new Utilisateur(login);
            if (utilisateur.CreerCompte(password))
            {
                Console.WriteLine($"[REGISTER] Compte créé: {utilisateur.Login} (IdCode: {utilisateur.IdCode})");
                Utilisateur.Current = utilisateur;
                OpenMainWindow();
            }
            else
            {
                ShowError("Erreur lors de la création du compte");
            }
        }

        private void OpenMainWindow()
        {
            Window mainWindow;

            // Ouvrir la fenêtre selon le rôle
            if (Utilisateur.Current?.Admin == true)
            {
                Console.WriteLine("[LOGIN] Ouverture de l'interface Admin");
                mainWindow = new AdminWindow();
            }
            else
            {
                Console.WriteLine("[LOGIN] Ouverture de l'interface Utilisateur");
                mainWindow = new HomeWindow();
            }

            mainWindow.Show();

            // Remplacer la MainWindow de l'application
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = mainWindow;
            }

            // Fermer la fenêtre de connexion
            Close();
        }

        private void ShowError(string message)
        {
            _error.Text = message;
            _error.IsVisible = true;
        }
    }
}
