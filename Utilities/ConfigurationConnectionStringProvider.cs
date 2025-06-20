using Microsoft.Extensions.Configuration;
using EasyTrade_Crypto.Interfaces;
namespace EasyTrade_Crypto.Utilities
{
    public class ConfigurationConnectionStringProvider : IDbConnectionStringProvider
    {
        public string ConnectionString { get; }
        public ConfigurationConnectionStringProvider(IConfiguration configuration)
        {
            ConnectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }
    }
} 