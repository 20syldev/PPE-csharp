using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls;
using PPE.Model;
using PPE.Utility;

namespace PPE.Controller
{
    public partial class Login : Window
    {
        private bool _isRegisterMode = false;

        public Login()
        {
            InitializeComponent();

            _tabLogin.Click += (s, e) => SetMode(false);
            _tabRegister.Click += (s, e) => SetMode(true);
            _password.TextChanged += (s, e) => UpdatePasswordStrength();
            _submit.Click += (s, e) => Submit();

            AddHandler(KeyDownEvent, (s, e) =>
            {
                if (e.Key == Key.Escape) Close();
                if (e.Key == Key.Enter) Submit();
            }, Avalonia.Interactivity.RoutingStrategies.Tunnel, true);

            Opened += (s, e) => _login.Focus();

            SetMode(false);
        }

        private void SetMode(bool register)
        {
            _isRegisterMode = register;

            _tabLogin.Classes.Clear();
            _tabRegister.Classes.Clear();
            if (register) _tabRegister.Classes.Add("accent");
            else _tabLogin.Classes.Add("accent");

            _confirmPanel.IsVisible = register;
            _strengthPanel.IsVisible = register;
            _submit.Content = register ? "Create Account" : "Sign In";
            _error.IsOpen = false;
            _loginError.IsOpen = false;

            if (register) UpdatePasswordStrength();
        }

        private void UpdatePasswordStrength()
        {
            if (!_isRegisterMode) return;

            var password = _password.Text ?? "";
            var validation = User.ValidatePassword(password);
            var percentage = User.GetPasswordStrengthPercentage(password);

            _strengthBar.Value = percentage;
            _strengthText.Text = validation.Strength;

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
            _error.IsOpen = false;
            _loginError.IsOpen = false;

            var login = _login.Text?.Trim() ?? "";
            var password = _password.Text ?? "";

            if (string.IsNullOrEmpty(login))
            {
                ShowError("Please enter an email");
                return;
            }

            var emailValidation = User.ValidateEmail(login);
            if (!emailValidation.IsValid)
            {
                _loginError.Message = emailValidation.Error;
                _loginError.IsOpen = true;
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Please enter a password");
                return;
            }

            if (_isRegisterMode)
                Register(login, password);
            else
                _ = LoginAsync(login, password);
        }

        private async Task LoginAsync(string login, string password)
        {
            var user = User.Authenticate(login, password);

            if (user != null)
            {
                if (user.TotpEnabled)
                {
                    var verifyDialog = new AuthVerify(user);
                    await verifyDialog.ShowDialog(this);

                    if (!verifyDialog.IsVerified)
                    {
                        User.Current = null;
                        return;
                    }
                }

                Console.WriteLine($"[LOGIN] Login successful: {user.Login}");
                OpenMainWindow();
            }
            else
            {
                ShowError("Incorrect email or password");
            }
        }

        private void Register(string login, string password)
        {
            var confirm = _confirmPassword.Text ?? "";

            if (password != confirm)
            {
                ShowError("Passwords do not match");
                return;
            }

            var validation = User.ValidatePassword(password);
            if (!validation.IsValid)
            {
                ShowError("Password does not meet the criteria");
                return;
            }

            if (User.LoginExists(login))
            {
                _loginError.Message = "This email is already in use";
                _loginError.IsOpen = true;
                return;
            }

            var user = new User(login);
            if (user.CreateAccount(password))
            {
                Console.WriteLine($"[REGISTER] Account created: {user.Login} (IdCode: {user.IdCode})");
                User.Current = user;
                OpenMainWindow();
            }
            else
            {
                ShowError("Error creating account");
            }
        }

        private void OpenMainWindow()
        {
            Window mainWindow;

            if (User.Current?.Admin == true)
            {
                Console.WriteLine("[LOGIN] Opening Admin interface");
                mainWindow = new Admin();
            }
            else
            {
                Console.WriteLine("[LOGIN] Opening User interface");
                mainWindow = new Home();
            }

            mainWindow.WindowState = WindowState.Maximized;
            mainWindow.Show();

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = mainWindow;
            }

            Close();
        }

        private void ShowError(string message)
        {
            _error.Message = message;
            _error.IsOpen = true;
        }
    }

    public partial class Home : Window
    {
        public Home()
        {
            InitializeComponent();

            UpdateWelcome();

            _settings.Click += async (s, e) =>
            {
                if (User.Current == null) return;
                var result = await new Settings(User.Current).ShowDialog<User?>(this);
                if (result != null)
                {
                    User.Current.Nom = result.Nom;
                    User.Current.Adresse = result.Adresse;
                    User.Current.Ville = result.Ville;
                    User.Current.Code = result.Code;
                    User.Current.Update();
                    UpdateWelcome();
                }
            };

            _logout.Click += async (s, e) =>
            {
                var result = await Dialogs.ShowConfirm(this, "Are you sure you want to logout?");
                if (result == ContentDialogResult.Primary)
                {
                    User.Current = null;
                    new Login().Show();
                    Close();
                }
            };
        }

        private void UpdateWelcome()
        {
            if (User.Current == null) return;
            var name = string.IsNullOrWhiteSpace(User.Current.Nom) ? User.Current.Login : User.Current.Nom;
            _welcomeText.Text = $"Welcome, {name}!";
        }
    }

    public partial class Admin : Window
    {
        private List<User> _all = [];
        private readonly ObservableCollection<User> _data = [];
        private NavigationViewItem? _homeItem;
        private NavigationViewItem? _usersItem;

        public Admin()
        {
            InitializeComponent();

            if (User.Current != null)
                _welcomeUser.Text = $"Logged in as {User.Current.Login}";

            _grid.ItemsSource = _data;
            _grid.SelectionChanged += (s, e) => _btnDel.IsVisible = _grid.SelectedItem != null;
            _grid.DoubleTapped += (s, e) =>
            {
                if (e.Source is Control ctrl && ctrl.FindAncestorOfType<DataGridRow>() != null)
                    EditUser();
            };
            _grid.PointerPressed += (s, e) =>
            {
                if (e.GetCurrentPoint(_grid).Properties.IsRightButtonPressed &&
                    e.Source is Control ctrl)
                {
                    var row = ctrl.FindAncestorOfType<DataGridRow>();
                    if (row?.DataContext is User u)
                        _grid.SelectedItem = u;
                }
            };

            _search.TextChanged += (s, e) => Filter();
            _filter.SelectionChanged += (s, e) => Filter();

            PointerPressed += (s, e) =>
            {
                if (e.Source is Control ctrl && ctrl.FindAncestorOfType<DataGridRow>() == null)
                    _grid.SelectedItem = null;
            };

            _btnDel.Click += (s, e) => DeleteUser();
            _btnRefresh.Click += (s, e) => LoadUsers();

            _menuShowPassword.Click += async (s, e) =>
            {
                if (_grid.SelectedItem is User u)
                    await Dialogs.ShowInfo(this, "Password", u.Password ?? "(not available)");
            };
            _menuShowCode.Click += async (s, e) =>
            {
                if (_grid.SelectedItem is User u)
                    await Dialogs.ShowInfo(this, "User Code", u.IdCode?.ToString() ?? "(not available)");
            };

            foreach (var item in _nav.MenuItems)
            {
                if (item is NavigationViewItem nvi)
                {
                    if (nvi.Tag?.ToString() == "home") _homeItem = nvi;
                    else if (nvi.Tag?.ToString() == "users") _usersItem = nvi;
                }
            }

            _nav.SelectionChanged += OnNavSelectionChanged;
            ShowPage("home");
        }

        private async void OnNavSelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
        {
            if (e.SelectedItem is NavigationViewItem item)
            {
                var tag = item.Tag?.ToString();
                switch (tag)
                {
                    case "home":
                    case "users":
                        ShowPage(tag);
                        break;
                    case "settings":
                        if (User.Current != null)
                        {
                            var result = await new Settings(User.Current).ShowDialog<User?>(this);
                            if (result != null)
                            {
                                User.Current.Nom = result.Nom;
                                User.Current.Adresse = result.Adresse;
                                User.Current.Ville = result.Ville;
                                User.Current.Code = result.Code;
                                User.Current.Update();
                            }
                        }
                        if (_pageHome.IsVisible) _nav.SelectedItem = _homeItem;
                        else _nav.SelectedItem = _usersItem;
                        break;
                    case "logout":
                        var confirmResult = await Dialogs.ShowConfirm(this, "Are you sure you want to logout?");
                        if (confirmResult == ContentDialogResult.Primary)
                        {
                            User.Current = null;
                            new Login().Show();
                            Close();
                        }
                        else
                        {
                            if (_pageHome.IsVisible) _nav.SelectedItem = _homeItem;
                            else _nav.SelectedItem = _usersItem;
                        }
                        break;
                }
            }
        }

        private void ShowPage(string page)
        {
            _pageHome.IsVisible = page == "home";
            _pageUsers.IsVisible = page == "users";
            if (page == "users") LoadUsers();
        }

        private void LoadUsers()
        {
            try
            {
                if (!Model.Connection.Instance().IsConnect())
                {
                    _count.Text = "(connection error)";
                    return;
                }
                _all = User.ListAll();
                _data.Clear();
                foreach (var u in _all) _data.Add(u);
                _count.Text = $"({_all.Count})";
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                _count.Text = $"({ex.Message})";
            }
        }

        private void UpdateEmptyState(bool isSearching = false)
        {
            bool hasData = _data.Count > 0;
            _emptyState.IsVisible = !hasData && !isSearching && _all.Count == 0;
            _noResults.IsVisible = !hasData && isSearching;
        }

        private void Filter()
        {
            var q = _search.Text?.Trim() ?? "";
            var selected = _filter.SelectedItem;
            var f = selected is ComboBoxItem item ? item.Content?.ToString() : "All";

            var res = string.IsNullOrEmpty(q) ? _all : f switch
            {
                "Name" => _all.Where(c => c.Nom?.Contains(q, StringComparison.OrdinalIgnoreCase) == true),
                "City" => _all.Where(c => c.Ville?.Contains(q, StringComparison.OrdinalIgnoreCase) == true),
                "Postal Code" => _all.Where(c => c.Code?.StartsWith(q) == true),
                _ => _all.Where(c =>
                    c.Nom?.Contains(q, StringComparison.OrdinalIgnoreCase) == true ||
                    c.Ville?.Contains(q, StringComparison.OrdinalIgnoreCase) == true ||
                    c.Code?.Contains(q) == true ||
                    c.Adresse?.Contains(q, StringComparison.OrdinalIgnoreCase) == true)
            };

            _data.Clear();
            foreach (var c in res) _data.Add(c);

            _count.Text = string.IsNullOrEmpty(q) ? $"({_data.Count})" : $"({_data.Count}/{_all.Count})";
            UpdateEmptyState(isSearching: !string.IsNullOrEmpty(q));
        }

        private async void DeleteUser()
        {
            if (_grid.SelectedItem is not User u || u.Id == null) return;
            var result = await Dialogs.ShowConfirm(this, $"Delete {u.Nom}?");
            if (result == ContentDialogResult.Primary)
            {
                User.Delete(u.Id.Value);
                LoadUsers();
            }
        }

        private async void EditUser()
        {
            if (_grid.SelectedItem is not User u || u.Id == null) return;
            var r = await new Edit(u).ShowDialog<User?>(this);
            if (r != null)
            {
                r.Id = u.Id;
                if (r.Update()) LoadUsers();
            }
        }
    }

    public partial class Settings : Window
    {
        private readonly User _user;

        public Settings() : this(new User()) { }

        public Settings(User user)
        {
            InitializeComponent();
            _user = user;

            _nom.Text = user.Nom;
            _adr.Text = user.Adresse;
            _ville.Text = user.Ville;
            _code.Text = user.Code;

            UpdateLastChanged();

            _success.Closed += (s, e) => _success.IsVisible = false;

            _changePassword.Click += async (s, e) =>
            {
                var result = await new Password().ShowDialog<bool?>(this);
                if (result == true)
                {
                    _success.Message = "Password changed successfully";
                    _success.IsVisible = true;
                    _success.IsOpen = true;
                    UpdateLastChanged();
                }
            };

            _twoFA.Click += async (s, e) =>
            {
                var auth = new Auth(_user);
                await auth.ShowDialog(this);
                _twoFA.Content = _user.TotpEnabled ? "Manage" : "Configure";
            };

            _twoFA.Content = user.TotpEnabled ? "Manage" : "Configure";

            _code.TextChanged += (s, e) =>
            {
                var code = _code.Text ?? "";
                if (string.IsNullOrEmpty(code)) { _codeError.IsOpen = false; return; }
                var validation = User.ValidatePostalCode(code);
                _codeError.IsOpen = !validation.IsValid;
                _codeError.Message = validation.Error ?? "";
            };

            _cancel.Click += (s, e) => Close(null);
            _save.Click += (s, e) =>
            {
                var code = _code.Text ?? "";
                if (!string.IsNullOrEmpty(code))
                {
                    var validation = User.ValidatePostalCode(code);
                    if (!validation.IsValid)
                    {
                        _codeError.IsOpen = true;
                        _codeError.Message = validation.Error ?? "";
                        return;
                    }
                }

                Close(new User
                {
                    Nom = _nom.Text,
                    Adresse = _adr.Text,
                    Ville = _ville.Text,
                    Code = _code.Text
                });
            };
        }

        private void UpdateLastChanged()
        {
            var lastChanged = _user.GetLastPasswordChange();
            _lastChanged.Text = lastChanged.HasValue
                ? $"Last changed: {lastChanged.Value:MMM d, yyyy}"
                : "Never changed";
        }
    }

    public partial class Auth : Window
    {
        private readonly User _user;
        private string? _pendingSecret;
        private List<string>? _recoveryCodes;
        private string? _pendingAction;

        public Auth() : this(new User()) { }

        public Auth(User user)
        {
            InitializeComponent();
            _user = user;

            _btnClose.Click += (s, e) => Close();
            _btnEnable.Click += (s, e) => ShowSetupPanel();
            _btnVerify.Click += (s, e) => VerifyAndEnable();
            _btnSaveCodes.Click += async (s, e) => await SaveCodesToFile();
            _btnContinue.Click += (s, e) => { SaveSetup(); UpdateView(); };
            _btnRegenerate.Click += (s, e) => StartRegenerate();
            _btnDisable.Click += (s, e) => StartDisable();
            _btnPasswordCancel.Click += (s, e) => CancelPasswordPrompt();
            _btnPasswordConfirm.Click += (s, e) => ConfirmPassword();

            UpdateView();
        }

        private void UpdateView()
        {
            _panelDisabled.IsVisible = false;
            _panelSetup.IsVisible = false;
            _panelRecoveryCodes.IsVisible = false;
            _panelEnabled.IsVisible = false;
            _panelPassword.IsVisible = false;
            _btnClose.IsVisible = true;

            if (_user.TotpEnabled)
            {
                _panelEnabled.IsVisible = true;
                var codes = TotpService.DecryptRecoveryCodes(_user.RecoveryCodes);
                _remainingCodes.Text = $"Recovery codes remaining: {codes.Count}/8";
            }
            else
            {
                _panelDisabled.IsVisible = true;
            }
        }

        private void ShowSetupPanel()
        {
            _panelDisabled.IsVisible = false;
            _panelSetup.IsVisible = true;
            _btnClose.IsVisible = false;

            _pendingSecret = TotpService.GenerateSecret();
            _secretKey.Text = _pendingSecret;

            var uri = TotpService.GenerateOtpAuthUri(_pendingSecret, _user.Login ?? "user");
            var qrBytes = TotpService.GenerateQrCode(uri);
            using var ms = new System.IO.MemoryStream(qrBytes);
            _qrCodeImage.Source = new Avalonia.Media.Imaging.Bitmap(ms);
        }

        private void VerifyAndEnable()
        {
            var code = _verifyCode.Text?.Trim() ?? "";
            if (_pendingSecret == null) return;

            if (code.Length != 6 || !code.All(char.IsDigit))
            {
                _setupError.Message = "Code must contain 6 digits";
                _setupError.IsOpen = true;
                return;
            }

            if (TotpService.ValidateCode(_pendingSecret, code))
            {
                _recoveryCodes = TotpService.GenerateRecoveryCodes();
                ShowRecoveryCodes();
            }
            else
            {
                _setupError.Message = "Invalid code. Check your device time.";
                _setupError.IsOpen = true;
            }
        }

        private void SaveSetup()
        {
            if (_pendingSecret == null || _recoveryCodes == null) return;

            // Encrypt before saving
            _user.TotpSecret = TotpService.EncryptSecret(_pendingSecret);
            _user.TotpEnabled = true;
            _user.RecoveryCodes = TotpService.EncryptRecoveryCodes(_recoveryCodes);
            _user.Update2FA();

            _pendingSecret = null;
            _recoveryCodes = null;
        }

        private void ShowRecoveryCodes()
        {
            _panelSetup.IsVisible = false;
            _panelRecoveryCodes.IsVisible = true;

            _recoveryCodesList.Children.Clear();
            if (_recoveryCodes != null)
            {
                foreach (var code in _recoveryCodes)
                {
                    _recoveryCodesList.Children.Add(new SelectableTextBlock
                    {
                        Text = code,
                        FontFamily = new FontFamily("Consolas, monospace"),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    });
                }
            }
        }

        private async Task SaveCodesToFile()
        {
            if (_recoveryCodes == null) return;

            var file = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Save Recovery Codes",
                DefaultExtension = "txt",
                FileTypeChoices = [new Avalonia.Platform.Storage.FilePickerFileType("Text Files") { Patterns = ["*.txt"] }]
            });

            if (file != null)
            {
                await using var stream = await file.OpenWriteAsync();
                await using var writer = new System.IO.StreamWriter(stream);
                await writer.WriteAsync(string.Join("\n", _recoveryCodes));
            }
        }

        private void StartRegenerate()
        {
            _pendingAction = "regenerate";
            _passwordPrompt.Text = "Enter your password to regenerate recovery codes:";
            ShowPasswordPanel();
        }

        private void StartDisable()
        {
            _pendingAction = "disable";
            _passwordPrompt.Text = "Enter your password to disable 2FA:";
            ShowPasswordPanel();
        }

        private void ShowPasswordPanel()
        {
            _panelEnabled.IsVisible = false;
            _panelPassword.IsVisible = true;
            _btnClose.IsVisible = false;
            _passwordInput.Text = "";
            _passwordError.IsOpen = false;
        }

        private void CancelPasswordPrompt()
        {
            _panelPassword.IsVisible = false;
            _panelEnabled.IsVisible = true;
            _btnClose.IsVisible = true;
        }

        private void ConfirmPassword()
        {
            var password = _passwordInput.Text ?? "";
            if (!Hashing.VerifyPassword(password, _user.Password ?? ""))
            {
                _passwordError.Message = "Invalid password";
                _passwordError.IsOpen = true;
                return;
            }

            if (_pendingAction == "regenerate")
            {
                _recoveryCodes = TotpService.GenerateRecoveryCodes();
                _user.RecoveryCodes = TotpService.EncryptRecoveryCodes(_recoveryCodes);
                _user.Update2FA();
                _panelPassword.IsVisible = false;
                ShowRecoveryCodes();
            }
            else if (_pendingAction == "disable")
            {
                _user.TotpSecret = null;
                _user.TotpEnabled = false;
                _user.RecoveryCodes = null;
                _user.Update2FA();
                _panelPassword.IsVisible = false;
                UpdateView();
            }
        }
    }

    public partial class AuthVerify : Window
    {
        public bool IsVerified { get; private set; }
        private readonly User _user;

        public AuthVerify() : this(User.Current ?? new User()) { }

        public AuthVerify(User user)
        {
            InitializeComponent();
            _user = user;

            _cancel.Click += (s, e) => Close();
            _verify.Click += (s, e) => Verify();

            AddHandler(KeyDownEvent, (s, e) =>
            {
                if (e.Key == Key.Escape) Close();
                if (e.Key == Key.Enter) Verify();
            }, Avalonia.Interactivity.RoutingStrategies.Tunnel, true);

            Opened += (s, e) => _codeInput.Focus();
        }

        private void Verify()
        {
            var code = _codeInput.Text?.Trim().Replace(" ", "").Replace("-", "") ?? "";
            _error.IsOpen = false;

            // Try TOTP code first (6 digits)
            if (code.Length == 6 && code.All(char.IsDigit))
            {
                var secret = TotpService.DecryptSecret(_user.TotpSecret);
                if (TotpService.ValidateCode(secret, code))
                {
                    IsVerified = true;
                    Close();
                    return;
                }
            }

            // Try recovery code
            var recoveryCodes = TotpService.DecryptRecoveryCodes(_user.RecoveryCodes);
            if (TotpService.ValidateAndConsumeRecoveryCode(code, ref recoveryCodes))
            {
                _user.RecoveryCodes = TotpService.EncryptRecoveryCodes(recoveryCodes);
                _user.Update2FA();
                IsVerified = true;
                Close();
                return;
            }

            _error.Message = "Invalid code";
            _error.IsOpen = true;
        }
    }

    public partial class Password : Window
    {
        public Password()
        {
            InitializeComponent();

            _newPassword.TextChanged += (s, e) => UpdateStrength();

            _cancel.Click += (s, e) => Close();
            _save.Click += (s, e) => ChangePassword();

            Opened += (s, e) => _currentPassword.Focus();
        }

        private void UpdateStrength()
        {
            var pwd = _newPassword.Text ?? "";
            var validation = User.ValidatePassword(pwd);
            var percentage = User.GetPasswordStrengthPercentage(pwd);

            _strengthBar.Value = percentage;
            _strengthText.Text = validation.Strength;

            UpdateCriterion(_iconMinLength, _textMinLength, validation.HasMinLength);
            UpdateCriterion(_iconUppercase, _textUppercase, validation.HasUppercase);
            UpdateCriterion(_iconLowercase, _textLowercase, validation.HasLowercase);
            UpdateCriterion(_iconDigit, _textDigit, validation.HasDigit);
            UpdateCriterion(_iconSpecial, _textSpecial, validation.HasSpecialChars);
            UpdateCriterion(_iconNoRepeat, _textNoRepeat, validation.NoConsecutiveRepeat);
        }

        private static void UpdateCriterion(FluentAvalonia.UI.Controls.SymbolIcon icon, TextBlock text, bool isValid)
        {
            var color = new SolidColorBrush(Color.Parse(isValid ? "#10B981" : "#F87171"));
            icon.Symbol = isValid ? FluentAvalonia.UI.Controls.Symbol.Accept : FluentAvalonia.UI.Controls.Symbol.Dismiss;
            icon.Foreground = color;
            text.Foreground = color;
        }

        private void ShowError(string message)
        {
            _error.Message = message;
            _error.IsOpen = true;
            _warning.IsOpen = false;
        }

        private void ChangePassword()
        {
            if (User.Current == null) return;

            var current = _currentPassword.Text ?? "";
            var newPwd = _newPassword.Text ?? "";
            var confirm = _confirmPassword.Text ?? "";

            if (!Hashing.VerifyPassword(current, User.Current.Password ?? ""))
            {
                ShowError("Current password is incorrect");
                return;
            }

            // 1. Validate password criteria first
            var validation = User.ValidatePassword(newPwd);
            if (!validation.IsValid)
            {
                ShowError("Password does not meet the criteria");
                return;
            }

            // 2. Check passwords match
            if (newPwd != confirm)
            {
                ShowError("Passwords do not match");
                return;
            }

            // 3. Check not reusing current or recent passwords
            if (User.Current.IsPasswordInHistory(newPwd))
            {
                ShowError("Cannot reuse your last 3 passwords");
                return;
            }

            var result = User.Current.ChangePassword(newPwd);
            if (result == 0)
            {
                Close(true);
            }
            else if (result == 1)
            {
                ShowError("Cannot reuse recent passwords");
            }
            else
            {
                ShowError("Error changing password");
            }
        }
    }

    public partial class Add : Window
    {
        public Add()
        {
            InitializeComponent();

            _code.TextChanged += (s, e) =>
            {
                var code = _code.Text ?? "";
                if (string.IsNullOrEmpty(code)) { _codeError.IsOpen = false; return; }
                var validation = User.ValidatePostalCode(code);
                _codeError.IsOpen = !validation.IsValid;
                _codeError.Message = validation.Error ?? "";
            };

            _cancel.Click += (s, e) => Close(null);
            _save.Click += (s, e) =>
            {
                var code = _code.Text ?? "";
                if (!string.IsNullOrEmpty(code))
                {
                    var validation = User.ValidatePostalCode(code);
                    if (!validation.IsValid)
                    {
                        _codeError.IsOpen = true;
                        return;
                    }
                }

                Close(new User
                {
                    Nom = _nom.Text,
                    Adresse = _adr.Text,
                    Ville = _ville.Text,
                    Code = _code.Text
                });
            };
        }
    }

    public partial class Edit : Window
    {
        public Edit() : this(new User()) { }

        public Edit(User user)
        {
            InitializeComponent();

            _title.Text = $"Edit: {user.Nom ?? user.Login}";
            _nom.Text = user.Nom;
            _adr.Text = user.Adresse;
            _ville.Text = user.Ville;
            _code.Text = user.Code;

            _code.TextChanged += (s, e) =>
            {
                var code = _code.Text ?? "";
                if (string.IsNullOrEmpty(code)) { _codeError.IsOpen = false; return; }
                var validation = User.ValidatePostalCode(code);
                _codeError.IsOpen = !validation.IsValid;
                _codeError.Message = validation.Error ?? "";
            };

            _cancel.Click += (s, e) => Close(null);
            _save.Click += (s, e) =>
            {
                var code = _code.Text ?? "";
                if (!string.IsNullOrEmpty(code))
                {
                    var validation = User.ValidatePostalCode(code);
                    if (!validation.IsValid)
                    {
                        _codeError.IsOpen = true;
                        return;
                    }
                }

                Close(new User
                {
                    Id = user.Id,
                    Nom = _nom.Text,
                    Adresse = _adr.Text,
                    Ville = _ville.Text,
                    Code = _code.Text
                });
            };
        }
    }

    public static class Dialogs
    {
        public static async Task<ContentDialogResult> ShowConfirm(Window parent, string message, string title = "Confirmation")
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                PrimaryButtonText = "Confirm",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close
            };
            return await dialog.ShowAsync(parent);
        }

        public static async Task ShowInfo(Window parent, string title, string content)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = new TextBlock { Text = content, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close
            };
            await dialog.ShowAsync(parent);
        }
    }
}
