# PPE - User Management

Desktop application for user management with secure authentication and two-factor authentication (2FA), built with C#, Avalonia UI and PostgreSQL.

## Features

- Authentication with secure password validation
- Two-factor authentication (2FA) with TOTP and QR Code
- Recovery codes for 2FA
- Real-time password strength indicator
- Last 3 passwords history (non-reusable)
- Role management (administrator / user)
- Full CRUD on users
- Search with filters (name, city, postal code, address)
- Modern interface with FluentAvalonia
- Logout confirmation dialogs

## Technologies

| Component | Version | Description |
|-----------|---------|-------------|
| .NET | 8.0 | Main framework |
| Avalonia UI | 11.3.9 | Cross-platform UI |
| Avalonia.Desktop | 11.3.9 | Desktop support |
| Avalonia.Controls.DataGrid | 11.3.9 | Data grid |
| Avalonia.Fonts.Inter | 11.3.9 | Inter font |
| Avalonia.Diagnostics | 11.3.9 | Diagnostic tools |
| FluentAvaloniaUI | 2.4.1 | UI components and theming |
| Npgsql | 10.0.0 | PostgreSQL driver |
| DotNetEnv | 3.1.1 | Environment variables |
| Otp.NET | 1.4.0 | TOTP generation/validation |
| QRCoder | 1.6.0 | QR code generation |

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL with configured database

### Database Configuration

1. Copy `.env.example` to `.env` and configure your credentials:

```bash
cp .env.example .env
```

2. Run the [schema.sql](schema.sql) script to create the database, table and stored procedures:

```bash
psql -U postgres -f schema.sql
```

## Installation

```bash
# Clone the project
git clone <repo-url>
cd PPE

# Restore dependencies
dotnet restore
```

## Usage

```bash
# Build
dotnet build

# Run
dotnet run
```

## Project Structure

```
PPE/
├── Main.cs                     # Entry point and configuration
├── PPE.csproj                  # Project configuration
├── schema.sql                  # Database creation script
├── .env.example                # Configuration template
│
├── Model/                      # Data layer
│   ├── Connection.cs           # Database connection (singleton)
│   ├── User.cs                 # User entity and CRUD
│   └── TotpService.cs          # TOTP service (2FA, QR codes)
│
├── Controller/                 # Application logic
│   ├── App.axaml               # Application entry and themes
│   └── Controllers.cs          # All controllers (Login, Home, Admin, Settings, Auth, etc.)
│
├── Utility/                    # Utility classes
│   ├── Hashing.cs              # SHA-512 + salt hashing
│   └── Crypto.cs               # 3DES encryption
│
└── View/                       # AXAML interfaces
    ├── Login.axaml             # Login and registration
    ├── Home.axaml              # User dashboard
    ├── Admin.axaml             # Admin dashboard with NavigationView
    ├── Settings.axaml          # User settings with SettingsExpander
    ├── Add.axaml               # Add user dialog
    ├── Edit.axaml              # Edit user dialog
    ├── Password.axaml          # Change password dialog
    ├── Auth.axaml              # 2FA configuration
    └── AuthVerify.axaml        # 2FA verification at login
```

## Architecture

The application follows an **MVC** (Model-View-Controller) architecture:

- **Model/**: Entities, data access, TOTP service
- **View/**: AXAML interfaces with FluentAvalonia
- **Controller/**: Business logic and event handling
- **Utility/**: Security classes (hashing, encryption)

### Security

| Feature | Implementation |
|---------|----------------|
| Password hashing | SHA-512 + 32-byte random salt |
| Password validation | Min 8 chars, uppercase, lowercase, digit, 2 special chars, no 3 consecutive identical chars |
| Password history | Last 3 passwords non-reusable |
| Email validation | RFC compliant pattern |
| Data encryption | Triple DES (3DES) 192 bits |
| Two-factor authentication | TOTP (RFC 6238) with Google Authenticator |
| Recovery codes | 8 single-use codes |

### Application Flow

1. **Login**: User logs in or creates an account
2. **2FA Verification**: If enabled, TOTP code is required
3. **Redirect**: Admin → Admin dashboard, User → Home dashboard
4. **Management**: CRUD on users with filters and search

---

*Project developed for BTSSIO SLAM - 2nd year*