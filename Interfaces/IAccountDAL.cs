namespace EasyTrade_Crypto.Interfaces
{
    public interface IAccountDAL
    {
        bool GetUserCredentialsByEmail(string email, out string? userId, out string? username, out string? storedPasswordHash, out string? roleName, out string errorMessage); // <-- Add out string? roleName
        bool InsertUser(string userId, string roleName, string username, string email, string passwordHash, out string errorMessage);
    }
}