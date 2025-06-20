namespace EasyTrade_Crypto.Interfaces
{
    /// <summary>
    /// Provides database connection string; allows high-level modules and DALs to depend on this abstraction instead of IConfiguration.
    /// </summary>
    public interface IDbConnectionStringProvider
    {
        string ConnectionString { get; }
    }
} 