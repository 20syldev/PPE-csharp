using Avalonia;
using DotNetEnv;
using PPE.Controller;
using PPE.Model;

// Load .env file
Env.Load();

// Database configuration
var db = PPE.Model.Connection.Instance();
db.Server = Env.GetString("PPE_DB_HOST", "localhost");
db.Port = Env.GetInt("PPE_DB_PORT", 5432);
db.DatabaseName = Env.GetString("PPE_DB_NAME", "ppe");
db.UserName = Env.GetString("PPE_DB_USER", "ppe");
db.Password = Env.GetString("PPE_DB_PASSWORD", "");

// Connect to database
try {
    db.IsConnect();
}
catch (Exception ex) {
    Console.WriteLine("Connection error: " + ex.Message);
}

// Launch Avalonia GUI
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .WithInterFont()
    .AfterSetup(builder =>
    {
        // DataGrid theme for FluentAvalonia
        builder.Instance!.Styles.Add(new Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("avares://PPE"))
        {
            Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml")
        });
    })
    .StartWithClassicDesktopLifetime(args);
