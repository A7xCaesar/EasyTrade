using System;
using System.Data;
using Microsoft.Data.SqlClient;
using BCrypt.Net;
using DTO;
using EasyTrade_Crypto.Interfaces;
using EasyTrade_Crypto.Managers;
using Interfaces;

namespace EasyTrade_Crypto.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountManager _accountManager;

        public AccountService(IAccountManager accountManager)
        {
            _accountManager = accountManager ?? throw new ArgumentNullException(nameof(accountManager));
        }

        public bool ValidateUserLogin(string email, string password, out UserLoginInfoDTO? userInfo, out string errorMessage)
        {
            // Delegate to the AccountManager
            return _accountManager.ValidateLogin(email, password, out userInfo, out errorMessage);
        }

        public bool RegisterUser(RegisterDTO registerInfo, out string errorMessage)
        {
            
            errorMessage = "Not implemented";
            return false;
        }

       
    }

  
} 