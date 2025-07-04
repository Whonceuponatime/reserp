# 🚢 Maritime ERP System

A comprehensive **Enterprise Resource Planning (ERP)** system designed specifically for maritime operations, fleet management, and ship administration.

![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue.svg)
![WPF](https://img.shields.io/badge/WPF-Framework-lightblue.svg)
![Entity Framework](https://img.shields.io/badge/Entity%20Framework-Core-green.svg)
![SQLite](https://img.shields.io/badge/SQLite-Database-orange.svg)
![License](https://img.shields.io/badge/License-MIT-yellow.svg)

## 🌊 Overview

Maritime ERP is a modern, desktop-based enterprise resource planning system tailored for maritime companies, shipping operators, and fleet managers. Built with .NET 8.0 and WPF, it provides a comprehensive solution for managing ships, systems, documents, and maritime operations.

## ✨ Features

### 🎯 Core Modules
- **🏠 Dashboard**: Real-time maritime statistics and fleet overview
- **⚓ Fleet Management**: Comprehensive ship registry and management
- **⚙️ Systems Management**: Ship systems and equipment tracking
- **🔧 Components Management**: Parts and component inventory
- **💻 Software Management**: Maritime software inventory and licensing
- **📋 Change Requests**: Workflow management for system changes
- **📄 Document Management**: Maritime document storage and organization
- **📊 Reports & Analytics**: Data visualization and business intelligence
- **👥 User Management**: Role-based access control and user administration

### 🔐 Security & Authentication
- Secure login system with role-based access
- Data encryption and secure database connections
- Audit trails for all system changes

### 📊 Data Management
- SQLite database with Entity Framework Core
- Clean Architecture with separation of concerns
- Comprehensive data validation and integrity checks

## 🏗️ Architecture

The system follows **Clean Architecture** principles with clear separation of concerns:

```
📦 Maritime ERP
├── 🎨 MaritimeERP.Desktop (WPF UI Layer)
│   ├── Views/ (XAML UI Components)
│   ├── ViewModels/ (MVVM ViewModels)
│   ├── Services/ (UI Services)
│   └── Resources/ (Styles & Templates)
├── 🔧 MaritimeERP.Services (Business Logic Layer)
│   ├── Services/ (Business Services)
│   ├── Interfaces/ (Service Contracts)
│   └── DTOs/ (Data Transfer Objects)
├── 🗃️ MaritimeERP.Data (Data Access Layer)
│   ├── Context/ (Entity Framework Context)
│   ├── Repositories/ (Data Repositories)
│   └── Configurations/ (Entity Configurations)
└── 🏢 MaritimeERP.Core (Domain Layer)
    ├── Entities/ (Domain Models)
    ├── Enums/ (Domain Enumerations)
    └── Interfaces/ (Domain Contracts)
```

## 🚀 Getting Started

### 📋 Prerequisites

- **Windows 10/11** (64-bit)
- **.NET 8.0 Runtime** or later
- **Visual Studio 2022** (for development)
- **SQLite** (embedded, no separate installation needed)

### 🔧 Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/whonceuponatime/reserp.git
   cd reserp
   ```

2. **Restore NuGet packages:**
   ```bash
   dotnet restore
   ```

3. **Build the solution:**
   ```bash
   dotnet build --configuration Release
   ```

4. **Run the application:**
   ```bash
   dotnet run --project src/MaritimeERP.Desktop
   ```

### 🔑 Default Login

- **Username:** `admin`
- **Password:** `admin123`

## 💻 Development

### 🛠️ Development Setup

1. **Install Visual Studio 2022** with:
   - .NET desktop development workload
   - WPF development tools

2. **Clone and open the solution:**
   ```bash
   git clone https://github.com/whonceuponatime/reserp.git
   cd reserp
   start MaritimeERP.sln
   ```

3. **Set MaritimeERP.Desktop as startup project**

4. **Run the application** (F5 or Ctrl+F5)

### 📁 Project Structure

```
src/
├── MaritimeERP.Core/           # Domain models and interfaces
├── MaritimeERP.Data/           # Data access and Entity Framework
├── MaritimeERP.Services/       # Business logic and services
└── MaritimeERP.Desktop/        # WPF user interface
    ├── Views/                  # XAML views
    ├── ViewModels/             # MVVM view models
    ├── Services/               # UI services
    └── Resources/              # Styles and templates
```

### 🔄 Database Migrations

The system uses Entity Framework Core with SQLite:

```bash
# Add a new migration
dotnet ef migrations add MigrationName --project src/MaritimeERP.Data

# Update database
dotnet ef database update --project src/MaritimeERP.Data
```

## 🎨 Screenshots

### Dashboard
![Dashboard](https://via.placeholder.com/800x500/1976D2/FFFFFF?text=Maritime+ERP+Dashboard)

### Fleet Management
![Fleet Management](https://via.placeholder.com/800x500/FF9800/FFFFFF?text=Fleet+Management)

### Systems Management
![Systems Management](https://via.placeholder.com/800x500/4CAF50/FFFFFF?text=Systems+Management)

## 🔧 Configuration

### Database Configuration
The system uses SQLite by default. Database settings can be configured in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=maritime_erp.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

### Application Settings
Additional configuration options are available in the application settings:

- **Theme**: Light/Dark mode support
- **Language**: Multi-language support (English default)
- **Logging**: Configurable logging levels
- **Security**: Password policies and session timeouts

## 📊 Data Model

### Core Entities

- **Ship**: Vessel information (IMO, flag, type, tonnage, etc.)
- **System**: Ship systems and equipment
- **Component**: Parts and components
- **Software**: Maritime software and licenses
- **Document**: Maritime documents and certificates
- **User**: System users and roles
- **ChangeRequest**: Change management workflow

### Database Schema
The system uses Entity Framework Core with a code-first approach. The database schema includes:

- Ships and fleet information
- Systems and equipment tracking
- Component inventory
- Software license management
- Document management
- User authentication and authorization
- Audit trails and logging

## 🤝 Contributing

1. **Fork the repository**
2. **Create a feature branch** (`git checkout -b feature/AmazingFeature`)
3. **Commit your changes** (`git commit -m 'Add some AmazingFeature'`)
4. **Push to the branch** (`git push origin feature/AmazingFeature`)
5. **Open a Pull Request**

### 🎯 Development Guidelines

- Follow **Clean Architecture** principles
- Use **MVVM pattern** for UI components
- Write **unit tests** for business logic
- Follow **C# coding standards**
- Document public APIs and complex logic

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🏆 Acknowledgments

- Built with ❤️ for the maritime industry
- Powered by .NET 8.0 and WPF
- Uses Entity Framework Core for data access
- Inspired by modern maritime operations

## 📞 Support

For support, please:
1. Check the **Issues** section for known problems
2. Create a **new issue** with detailed description
3. Contact the development team

---

## 🔄 Version History

### v1.0.0 (Current)
- ✅ Initial release with core functionality
- ✅ Dashboard with real-time statistics
- ✅ Fleet management system
- ✅ Systems management module
- ✅ User authentication and authorization
- ✅ Clean Architecture implementation
- ✅ SQLite database integration

### Future Releases
- 🔄 Components management (v1.1.0)
- 🔄 Software management (v1.2.0)
- 🔄 Document management (v1.3.0)
- 🔄 Reports and analytics (v1.4.0)
- 🔄 Advanced user management (v1.5.0)

---

**Maritime ERP System** - *Professional Maritime Enterprise Resource Planning Solution*

🚢 *"Navigate your maritime operations with confidence"* 