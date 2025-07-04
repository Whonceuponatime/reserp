using System.ComponentModel.DataAnnotations;

namespace MaritimeERP.Core.Entities
{
    public class ShipType
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public override string ToString()
        {
            return Name;
        }
        
        public override bool Equals(object? obj)
        {
            if (obj is ShipType other)
            {
                return Id == other.Id && Name == other.Name;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name);
        }
    }
} 