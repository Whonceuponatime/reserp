# SEACURE(CARE) - Installer Guide

## Creating the Installer

### Prerequisites
1. **Inno Setup** - Download and install from https://jrsoftware.org/isinfo.php
2. **.NET 8.0 SDK** - For building the application
3. **Add Inno Setup to PATH** - Add the Inno Setup installation directory to your system PATH

### Building the Installer

1. **Automatic Build** (Recommended):
   ```bash
   build_installer.bat
   ```
   This will:
   - Clean previous builds
   - Build the application in Release mode
   - Create the installer using Inno Setup
   - Output the installer to `Installer/SEACURE_CARE_Setup_v1.0.0.exe`

2. **Manual Build**:
   ```bash
   # Build the application
   cd src/MaritimeERP.Desktop
   dotnet build --configuration Release
   cd ../..
   
   # Create installer
   iscc MaritimeERP_Setup.iss
   ```

## Installation Behavior

### File Structure After Installation
```
Program Files/SEACURE(CARE)/                <- Application files
├── MaritimeERP.Desktop.exe                 <- Main executable
├── *.dll                                   <- Dependencies
├── runtimes/                               <- Runtime libraries
└── Database/maritime_erp.db                <- Template database

%APPDATA%/SEACURE(CARE)/                    <- User data (auto-created)
├── Database/                               <- User's database
│   └── maritime_erp.db                     <- Active database
├── Documents/                              <- Document storage
├── Logs/                                   <- Application logs
└── appsettings.json                        <- User configuration
```

### Configuration Priority
The application looks for configuration in this order:
1. **User Config** - `%APPDATA%/SEACURE(CARE)/appsettings.json` (highest priority)
2. **App Directory** - `Program Files/SEACURE(CARE)/appsettings.json`
3. **Development** - `src/MaritimeERP.Desktop/appsettings.json`

## Database Management

### Moving Databases

1. **Copy Database File**:
   - Source: `%APPDATA%/SEACURE(CARE)/Database/maritime_erp.db`
   - Destination: Any location you prefer

2. **Update Configuration**:
   Edit `%APPDATA%/SEACURE(CARE)/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Data Source=C:\\Path\\To\\Your\\Database\\maritime_erp.db"
     }
   }
   ```

3. **Using Network Drives**:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Data Source=\\\\ServerName\\Share\\Database\\maritime_erp.db"
     }
   }
   ```

### Backup and Restore

**Backup**:
1. Close the SEACURE(CARE) application
2. Copy `%APPDATA%/SEACURE(CARE)/Database/maritime_erp.db` to your backup location

**Restore**:
1. Close the SEACURE(CARE) application
2. Replace `%APPDATA%/SEACURE(CARE)/Database/maritime_erp.db` with your backup
3. Or update the configuration to point to the backup location

### Multiple Database Support

You can maintain multiple databases by switching the configuration:

1. **Create separate config files**:
   - `appsettings.production.json`
   - `appsettings.testing.json`

2. **Copy the desired config** to `appsettings.json` when needed

3. **Or use batch files** to launch with different configs:
   ```batch
   @echo off
   copy "appsettings.production.json" "appsettings.json"
   MaritimeERP.Desktop.exe
   ```

## Customization

### Document Storage Location
Edit the configuration to change where documents are stored:
   ```json
   {
     "Application": {
       "DocumentStoragePath": "D:\\Documents\\SEACURE CARE\\Files"
     }
   }
   ```

### Database Settings
Advanced SQLite options:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=database.db;Cache=Shared;Mode=ReadWriteCreate;Pooling=true"
  }
}
```

## Troubleshooting

### Database Issues
- **File not found**: Check the path in appsettings.json
- **Access denied**: Ensure the user has write permissions to the database directory
- **Database locked**: Close all instances of the application

### Installation Issues
- **Missing dependencies**: Install .NET 8.0 Desktop Runtime
- **Permission errors**: Run installer as administrator
- **Path issues**: Ensure Inno Setup is in your PATH

### Configuration Issues
- **Settings not loading**: Check JSON syntax in appsettings.json
- **Default config**: Delete appsettings.json to regenerate defaults

## Support

For technical support or questions about the installer:
- Check the application logs in `%APPDATA%/SEACURE(CARE)/Logs/`
- Verify database file permissions and accessibility
- Ensure .NET 8.0 Desktop Runtime is installed on target machines 