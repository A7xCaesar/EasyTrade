using System;
using System.ComponentModel.DataAnnotations;

namespace DTO
{
    public class CryptoDTO
    {
        public string AssetId { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Symbol is required")]
        [StringLength(10, ErrorMessage = "Symbol cannot exceed 10 characters")]
        public string Symbol { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;
        
        public decimal CurrentPrice { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class AddCryptoDTO
    {
        [Required(ErrorMessage = "Symbol is required")]
        [StringLength(10, ErrorMessage = "Symbol cannot exceed 10 characters")]
        public string Symbol { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Initial price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Initial price must be greater than 0")]
        public decimal InitialPrice { get; set; }
    }
} 