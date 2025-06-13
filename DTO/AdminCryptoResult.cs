namespace DTO
{
    public class AdminCryptoResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        
        public static AdminCryptoResult CreateSuccess()
        {
            return new AdminCryptoResult { Success = true };
        }
        
        public static AdminCryptoResult CreateError(string errorMessage)
        {
            return new AdminCryptoResult { Success = false, ErrorMessage = errorMessage };
        }
    }
} 