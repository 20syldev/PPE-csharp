# PPE - Gestion Utilisateurs

Application de bureau pour la gestion d'utilisateurs avec authentification, développée en C# avec Avalonia UI et PostgreSQL.

## Fonctionnalités

- Authentification avec validation de mot de passe sécurisé
- Indicateur de force du mot de passe en temps réel
- Gestion des rôles (administrateur / utilisateur)
- CRUD complet sur les utilisateurs
- Recherche avec filtres (nom, ville, code postal, adresse)
- Interface moderne avec thème sombre Fluent

## Technologies

| Composant | Version | Description |
|-----------|---------|-------------|
| .NET | 8.0 | Framework principal |
| Avalonia UI | 11.3.9 | Interface graphique cross-platform |
| Avalonia.Desktop | 11.3.9 | Support desktop |
| Avalonia.Controls.DataGrid | 11.3.9 | Grille de données |
| Avalonia.Themes.Fluent | 11.3.9 | Thème Fluent Design |
| Avalonia.Fonts.Inter | 11.3.9 | Police Inter |
| Avalonia.Diagnostics | 11.3.9 | Outils de diagnostic |
| Npgsql | 10.0.0 | Driver PostgreSQL |
| DotNetEnv | 3.1.1 | Gestion des variables d'environnement |

## Prérequis

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL avec une base de données configurée

### Configuration de la base de données

1. Copiez le fichier `.env.example` en `.env` et configurez vos identifiants :

```bash
cp .env.example .env
```

2. Exécutez le script [schema.sql](schema.sql) pour créer la base de données, la table et les procédures stockées :

```bash
psql -U postgres -f schema.sql
```

## Installation

```bash
# Cloner le projet
git clone <url-du-repo>
cd PPE

# Restaurer les dépendances
dotnet restore
```

## Utilisation

```bash
# Compiler
dotnet build

# Exécuter
dotnet run
```

## Structure du projet

```
PPE/
├── Main.cs                 # Point d'entrée et configuration
├── PPE.csproj              # Configuration du projet
├── schema.sql              # Script de création BDD
├── .env.example            # Template de configuration
│
├── Modele/                 # Couche données
│   ├── Connect.cs          # Connexion BDD (singleton)
│   ├── Utilisateur.cs      # Entité utilisateur et CRUD
│   ├── Hashage.cs          # Hachage SHA-512 + sel
│   └── Crypto.cs           # Chiffrement 3DES
│
├── Controlleur/            # Logique applicative
│   ├── LoginController.cs  # Authentification et inscription
│   ├── MainController.cs   # Liste des utilisateurs
│   ├── AdminController.cs  # Interface administrateur
│   ├── HomeController.cs   # Tableau de bord utilisateur
│   └── DialogController.cs # Dialogues modaux
│
└── Vue/                    # Interfaces XAML
    ├── App.axaml           # Styles globaux
    ├── LoginWindow.axaml   # Écran de connexion
    ├── MainWindow.axaml    # Liste utilisateurs
    ├── AdminWindow.axaml   # Dashboard admin
    ├── HomeWindow.axaml    # Dashboard utilisateur
    └── *Dialog.axaml       # Dialogues (Add, Edit, Confirm, Settings, Info)
```

## Architecture

L'application suit une architecture **MVC** (Modèle-Vue-Contrôleur) :

- **Modele/** : Entités, accès aux données, utilitaires de sécurité
- **Vue/** : Interfaces XAML avec thème Fluent sombre
- **Controlleur/** : Logique métier et gestion des événements

### Sécurité

| Fonctionnalité | Implémentation |
|----------------|----------------|
| Hachage mot de passe | SHA-512 + sel aléatoire 32 octets |
| Validation mot de passe | Min 8 car., majuscule, minuscule, chiffre, 2 caractères spéciaux |
| Validation email | Pattern RFC compliant |
| Chiffrement données | Triple DES (3DES) 192 bits |

### Flux applicatif

1. **Connexion** : L'utilisateur se connecte ou crée un compte
2. **Redirection** : Admin → AdminWindow, Utilisateur → HomeWindow
3. **Gestion** : CRUD sur les utilisateurs avec filtres et recherche

---

*Projet réalisé dans le cadre du BTSSIO SLAM - 2ème année*