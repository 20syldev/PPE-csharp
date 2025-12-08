# PPE - Gestion Clients

> **Work In Progress**

Application de bureau pour la gestion de clients, développée en C# avec Avalonia UI et PostgreSQL.

## Fonctionnalités

- Affichage des clients dans une grille de données
- Ajout, modification et suppression de clients
- Recherche avec filtres (nom, ville, code postal)
- Interface moderne avec thème sombre Fluent

## Technologies

| Composant | Version |
|-----------|---------|
| .NET | 8.0 |
| Avalonia UI | 11.3.9 |
| PostgreSQL | via Npgsql 10.0.0 |

## Prérequis

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL avec une base de données configurée

### Configuration de la base de données

Exécutez le script [schema.sql](schema.sql) pour créer la base de données, la table et les procédures stockées :

```bash
psql -U postgres -f schema.sql
```

## Installation

```bash
# Cloner le projet
git clone <url-du-repo>
cd PPE

# Mettre à jour les dépendances
dotnet add package Npgsql
dotnet add package Avalonia
dotnet add package Avalonia.Desktop
dotnet add package Avalonia.Fonts.Inter
dotnet add package Avalonia.Themes.Fluent
dotnet add package Avalonia.Controls.DataGrid

# Ou restorer les dépendances
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
├── Main.cs        # Point d'entrée et configuration
├── Main.axaml     # Interface graphique (XAML)
├── Interface.cs   # Logique de l'interface (code-behind)
├── Client.cs      # Modèle et opérations CRUD
├── Connect.cs     # Connexion à la base de données
├── Crypto.cs      # Utilitaires de chiffrement
└── Popup.cs       # Dialogues (ajout, modification, confirmation)
```

## Architecture

L'interface utilise le pattern XAML/code-behind d'Avalonia :

- **Window.axaml** : Définit la structure de l'UI et les styles en XML
- **Interface.cs** : Contient la logique (événements, filtrage, CRUD)

### Styles disponibles (Window.axaml)

| Classe | Usage |
|--------|-------|
| `Button.primary` | Boutons d'action principaux (bleu) |
| `Button.danger` | Boutons de suppression (rouge) |

## Apercu

L'application affiche une interface avec :
- Une barre d'outils avec recherche et boutons d'action
- Une grille de données listant les clients
- Des dialogues modaux pour l'ajout et la modification

---

*Projet réalisé dans le cadre du BTSSIO SLAM - 2ème année*
