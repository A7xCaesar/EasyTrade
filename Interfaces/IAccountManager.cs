using DTO;


namespace Interfaces 
{
    public interface IAccountManager
    {
        
        bool RegisterUser(RegisterDTO dto, out string errorMessage);

        bool ValidateLogin(string email, string password, out UserLoginInfoDTO? userInfo, out string errorMessage);
    }
}