using DTO;

namespace EasyTrade_Crypto.Interfaces
{
    public interface IRegistrationService
    {
        bool RegisterUser(RegisterInputModel input, out string errorMessage);
    }
} 