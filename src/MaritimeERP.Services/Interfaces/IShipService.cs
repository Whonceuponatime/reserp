using MaritimeERP.Core.Entities;

namespace MaritimeERP.Services.Interfaces
{
    public interface IShipService
    {
        Task<IEnumerable<Ship>> GetAllShipsAsync();
        Task<Ship?> GetShipByIdAsync(int id);
        Task<Ship?> GetShipByImoAsync(string imoNumber);
        Task<IEnumerable<Ship>> SearchShipsAsync(string searchTerm);
        Task<IEnumerable<Ship>> GetShipsByTypeAsync(string shipType);
        Task<IEnumerable<Ship>> GetShipsByFlagAsync(string flag);
        Task<Ship> CreateShipAsync(Ship ship);
        Task<Ship> UpdateShipAsync(Ship ship);
        Task<bool> DeleteShipAsync(int id);
        Task<bool> ActivateShipAsync(int id);
        Task<bool> DeactivateShipAsync(int id);
        Task<bool> ImoExistsAsync(string imoNumber, int? excludeShipId = null);
        Task<int> GetShipCountAsync();
        Task<int> GetActiveShipCountAsync();
        Task<IEnumerable<string>> GetShipTypesAsync();
        Task<IEnumerable<ShipType>> GetShipTypeEntitiesAsync();
        Task<IEnumerable<string>> GetShipFlagsAsync();
        Task<ShipStatistics> GetShipStatisticsAsync();
    }

    public class ShipStatistics
    {
        public int TotalShips { get; set; }
        public decimal TotalGrossTonnage { get; set; }
        public int ShipsThisYear { get; set; }
        public Dictionary<string, int> ShipsByFlag { get; set; } = new();
        public Dictionary<short, int> ShipsByYear { get; set; } = new();
    }
} 