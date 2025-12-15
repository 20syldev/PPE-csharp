using Avalonia;
using Avalonia.Themes.Fluent;
using DotNetEnv;
using PPE.Controlleur;
using PPE.Modele;

// Charger le fichier .env
Env.Load();

// Configuration de la base de données
var db = Connect.Instance();
db.Server = Env.GetString("PPE_DB_HOST", "localhost");
db.Port = Env.GetInt("PPE_DB_PORT", 5432);
db.DatabaseName = Env.GetString("PPE_DB_NAME", "ppe");
db.UserName = Env.GetString("PPE_DB_USER", "ppe");
db.Password = Env.GetString("PPE_DB_PASSWORD", "");

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
        builder.Instance!.Styles.Add(new Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("avares://PPE"))
        {
            Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml")
        });
    })
    .StartWithClassicDesktopLifetime(args);
