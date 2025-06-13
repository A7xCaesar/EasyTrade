using DTO;

namespace EasyTrade_Crypto.Tests.PageModels.LoginTests
{
    public static class LoginModelTestDelegates
    {
        public delegate void ValidateUserLoginCallback(string email, string password, out UserLoginInfoDTO? userInfo, out string errorMessage);
    }
}