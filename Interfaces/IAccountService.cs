using System.Threading.Tasks;
using DTO;

namespace EasyTrade_Crypto.Interfaces
{

   


    public interface IAccountService
    {

        bool ValidateUserLogin(string email, string password, out UserLoginInfoDTO? userInfo, out string errorMessage);


        bool RegisterUser(RegisterDTO registerInfo, out string errorMessage);
    }
} 