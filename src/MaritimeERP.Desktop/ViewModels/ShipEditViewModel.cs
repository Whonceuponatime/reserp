using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MaritimeERP.Core.Entities;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Services.Interfaces;

namespace MaritimeERP.Desktop.ViewModels
{
    public class ShipEditViewModel : INotifyPropertyChanged
    {
        private readonly IShipService _shipService;
        private readonly IServiceProvider _serviceProvider;
        private Ship? _originalShip;
        private bool _isEditing;
        private bool _isLoading;
        private string _statusMessage = string.Empty;

        public ShipEditViewModel(IShipService shipService, IServiceProvider serviceProvider, Ship? ship = null)
        {
            _shipService = shipService;
            _serviceProvider = serviceProvider;
            _originalShip = ship;
            _isEditing = ship != null;

            InitializeCommands();
            InitializeData();
            
            if (_isEditing && _originalShip != null)
            {
                PopulateFromShip(_originalShip);
            }
            else
            {
                // Set default values for new ship
                IsActive = true;
                BuildYear = (short)DateTime.Now.Year;
            }
        }

        #region Properties

        // Core Properties
        private int _id;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _shipName = string.Empty;
        public string ShipName
        {
            get => _shipName;
            set => SetProperty(ref _shipName, value);
        }

        private string _imoNumber = string.Empty;
        public string ImoNumber
        {
            get => _imoNumber;
            set => SetProperty(ref _imoNumber, value);
        }

        private string _shipType = string.Empty;
        public string ShipType
        {
            get => _shipType;
            set => SetProperty(ref _shipType, value);
        }

        private string _flag = string.Empty;
        public string Flag
        {
            get => _flag;
            set => SetProperty(ref _flag, value);
        }

        private string _portOfRegistry = string.Empty;
        public string PortOfRegistry
        {
            get => _portOfRegistry;
            set => SetProperty(ref _portOfRegistry, value);
        }

        private string _class = string.Empty;
        public string Class
        {
            get => _class;
            set
            {
                if (SetProperty(ref _class, value))
                {
                    UpdateClassNotationOptions();
                    // Clear current notation when class changes
                    ClassNotation = string.Empty;
                }
            }
        }

        private string _classNotation = string.Empty;
        public string ClassNotation
        {
            get => _classNotation;
            set => SetProperty(ref _classNotation, value);
        }

        private short? _buildYear;
        public short? BuildYear
        {
            get => _buildYear;
            set => SetProperty(ref _buildYear, value);
        }

        // Tonnage Properties
        private decimal? _grossTonnage;
        public decimal? GrossTonnage
        {
            get => _grossTonnage;
            set => SetProperty(ref _grossTonnage, value);
        }

        private decimal? _netTonnage;
        public decimal? NetTonnage
        {
            get => _netTonnage;
            set => SetProperty(ref _netTonnage, value);
        }

        private decimal? _deadweightTonnage;
        public decimal? DeadweightTonnage
        {
            get => _deadweightTonnage;
            set => SetProperty(ref _deadweightTonnage, value);
        }

        // Management Properties
        private string _owner = string.Empty;
        public string Owner
        {
            get => _owner;
            set => SetProperty(ref _owner, value);
        }

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        // Status Properties
        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // Collections for dropdowns
        public ObservableCollection<string> ShipTypes { get; set; } = new();
        public ObservableCollection<string> Flags { get; set; } = new();
        public ObservableCollection<string> Classes { get; set; } = new();
        public ObservableCollection<string> ClassNotations { get; set; } = new();

        // Display Properties
        public string DialogTitle => IsEditing ? "Edit Ship" : "Add New Ship";
        public string SaveButtonText => IsEditing ? "Update Ship" : "Add Ship";

        // Dialog Result Property
        private bool? _dialogResult;
        public bool? DialogResult
        {
            get => _dialogResult;
            set => SetProperty(ref _dialogResult, value);
        }

        #endregion

        #region Commands

        public ICommand SaveCommand { get; private set; } = null!;
        public ICommand CancelCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
            CancelCommand = new RelayCommand(Cancel);
        }

        #endregion

        #region Methods

        private void InitializeData()
        {
            LoadDropdownData();
        }

        private void LoadDropdownData()
        {
            // Ship Types
            ShipTypes.Clear();
            ShipTypes.Add("Container Ship");
            ShipTypes.Add("Bulk Carrier");
            ShipTypes.Add("Tanker");
            ShipTypes.Add("Cargo Ship");
            ShipTypes.Add("Passenger Ship");
            ShipTypes.Add("Ferry");
            ShipTypes.Add("Cruise Ship");
            ShipTypes.Add("Fishing Vessel");
            ShipTypes.Add("Offshore Supply");
            ShipTypes.Add("Tugboat");
            ShipTypes.Add("Research Vessel");
            ShipTypes.Add("Naval Vessel");
            ShipTypes.Add("Other");

            // Flags (common maritime flags)
            Flags.Clear();
            Flags.Add("Panama");
            Flags.Add("Liberia");
            Flags.Add("Marshall Islands");
            Flags.Add("Hong Kong");
            Flags.Add("Singapore");
            Flags.Add("Bahamas");
            Flags.Add("Malta");
            Flags.Add("Cyprus");
            Flags.Add("Isle of Man");
            Flags.Add("Madeira");
            Flags.Add("Norway");
            Flags.Add("Germany");
            Flags.Add("United Kingdom");
            Flags.Add("Netherlands");
            Flags.Add("Denmark");
            Flags.Add("Other");

            // Classification Societies
            Classes.Clear();
            Classes.Add("DNV");
            Classes.Add("Lloyd's Register");
            Classes.Add("American Bureau of Shipping");
            Classes.Add("Bureau Veritas");
            Classes.Add("Class NK");
            Classes.Add("Korean Register");
            Classes.Add("China Classification Society");
            Classes.Add("Russian Maritime Register");
            Classes.Add("RINA");
            Classes.Add("Other");
        }

        private void UpdateClassNotationOptions()
        {
            ClassNotations.Clear();

            switch (Class)
            {
                case "DNV":
                    ClassNotations.Add("Cyber Secure");
                    ClassNotations.Add("Cyber Secure (Essential)");
                    ClassNotations.Add("Cyber Secure (Advanced)");
                    ClassNotations.Add("Cyber Secure (Essential) +");
                    ClassNotations.Add("Cyber Secure (Advanced) +");
                    break;

                case "Lloyd's Register":
                    ClassNotations.Add("ShipRight");
                    ClassNotations.Add("ShipRight (CCS)");
                    ClassNotations.Add("ShipRight (FDA)");
                    ClassNotations.Add("ShipRight (CM)");
                    ClassNotations.Add("ShipRight (SDA)");
                    break;

                case "American Bureau of Shipping":
                    ClassNotations.Add("A1");
                    ClassNotations.Add("A1 (E)");
                    ClassNotations.Add("A1 (CCS)");
                    ClassNotations.Add("A1 (HSE)");
                    ClassNotations.Add("A1 (ACCU)");
                    break;

                case "Bureau Veritas":
                    ClassNotations.Add("I");
                    ClassNotations.Add("I (CCS)");
                    ClassNotations.Add("I (CYBER)");
                    ClassNotations.Add("I (GREEN)");
                    ClassNotations.Add("I (SMART)");
                    break;

                case "Class NK":
                    ClassNotations.Add("NS");
                    ClassNotations.Add("NS (CCS)");
                    ClassNotations.Add("NS (CYBER)");
                    ClassNotations.Add("NS (ENVIRO)");
                    ClassNotations.Add("NS (ECO)");
                    break;

                default:
                    ClassNotations.Add("Standard");
                    ClassNotations.Add("Enhanced");
                    ClassNotations.Add("Premium");
                    break;
            }
        }

        private void PopulateFromShip(Ship ship)
        {
            Id = ship.Id;
            ShipName = ship.ShipName;
            ImoNumber = ship.ImoNumber;
            ShipType = ship.ShipType;
            Flag = ship.Flag;
            PortOfRegistry = ship.PortOfRegistry;
            Class = ship.Class;
            ClassNotation = ship.ClassNotation;
            BuildYear = ship.BuildYear;
            GrossTonnage = ship.GrossTonnage;
            NetTonnage = ship.NetTonnage;
            DeadweightTonnage = ship.DeadweightTonnage;
            Owner = ship.Owner;
            IsActive = ship.IsActive;

            // Update class notation options after setting class
            UpdateClassNotationOptions();
        }

        private Ship CreateShipFromForm()
        {
            return new Ship
            {
                Id = Id,
                ShipName = ShipName,
                ImoNumber = ImoNumber,
                ShipType = ShipType,
                Flag = Flag,
                PortOfRegistry = PortOfRegistry,
                Class = Class,
                ClassNotation = ClassNotation,
                BuildYear = BuildYear,
                GrossTonnage = GrossTonnage,
                NetTonnage = NetTonnage,
                DeadweightTonnage = DeadweightTonnage,
                Owner = Owner,
                IsActive = IsActive,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private bool CanSave()
        {
            return !IsLoading && 
                   !string.IsNullOrWhiteSpace(ShipName) && 
                   !string.IsNullOrWhiteSpace(ImoNumber) && 
                   ImoNumber.Length == 7;
        }

        private async Task SaveAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = IsEditing ? "Updating ship..." : "Adding ship...";

                var ship = CreateShipFromForm();

                if (IsEditing)
                {
                    await _shipService.UpdateShipAsync(ship);
                    StatusMessage = "Ship updated successfully!";
                }
                else
                {
                    await _shipService.CreateShipAsync(ship);
                    StatusMessage = "Ship added successfully!";
                }

                // Set dialog result to true
                DialogResult = true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Cancel()
        {
            DialogResult = false;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
} 