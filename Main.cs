using Avalonia;
using Avalonia.Themes.Fluent;
using PPE;

// Configuration de la base de données
var db = Connect.Instance();
db.Server = "localhost";
db.Port = 5432;
db.DatabaseName = "ppe";
db.UserName = "ppe";
db.Password = Crypto.Decrypt("W6dGsBJka5FDcr+/EZQy99hvx/xIF7hAOXaDXI+S2PU=");

// Connexion à la BDD
try {
    db.IsConnect();
}
catch (Exception ex) {
    Console.WriteLine("Erreur de connexion: " + ex.Message);
}

// Lancement de l'interface graphique Avalonia
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .WithInterFont()
    .AfterSetup(builder =>
    {
        builder.Instance!.Styles.Add(new FluentTheme());
        builder.Instance!.Styles.Add(new Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("avares://Hashage"))
        {
            Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml")
        });
    })
    .StartWithClassicDesktopLifetime(args);