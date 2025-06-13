using System;
using BCrypt.Net;
using DTO;
using EasyTrade_Crypto.Interfaces;
using EasyTrade_Crypto.MSSQL; 
using EasyTrade_Crypto.Utilities;
using Interfaces;

namespace EasyTrade_Crypto.Managers
{

    public class AccountManager : IAccountManager
    {
        private readonly IAccountDAL _accountDAL;
        private readonly string _normalRoleName = "Normal"; 

        public AccountManager(IAccountDAL accountDAL)
        {
            _accountDAL = accountDAL ?? throw new ArgumentNullException(nameof(accountDAL));
        }


        public bool RegisterUser(RegisterDTO dto, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (dto == null)
            {
                errorMessage = "Registration data is missing.";
                return false;
            }

            // Basic input validations.
            if (string.IsNullOrWhiteSpace(dto.Username))
            {
                errorMessage = "Username is required.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                errorMessage = "Email is required.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(dto.Password))
            {
                errorMessage = "Password is required.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(dto.ConfirmPassword))
            {
                errorMessage = "Please confirm your password.";
                return false;
            }
            if (dto.Password != dto.ConfirmPassword)
            {
                errorMessage = "Passwords do not match.";
                return false;
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            string userId = IdGenerator.GenerateShortUniqueId();


            if (!_accountDAL.InsertUser(userId, _normalRoleName, dto.Username, dto.Email, hashedPassword, out string dalError))
            {
                errorMessage = $"Registration failed: {dalError}";
                return false;
            }
            return true;
        }

        // login validation
        public bool ValidateLogin(string email, string password, out UserLoginInfoDTO? userInfo, out string errorMessage)
        {
            userInfo = null;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Email and password are required.";
                return false;
            }

            bool userExists = _accountDAL.GetUserCredentialsByEmail(email, out string? userId, out string? username, out string? storedPasswordHash, out string? roleName, out string dalError);

            if (!userExists)
            {
                errorMessage = dalError;
                if (string.IsNullOrWhiteSpace(errorMessage))
                {
                    errorMessage = "Invalid email or password.";
                }
                return false;
            }

            if (string.IsNullOrEmpty(storedPasswordHash) || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(roleName)) // Also check roleName
            {
                errorMessage = "User account data is incomplete. Please contact support.";
                return false;
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, storedPasswordHash);

            if (isPasswordValid)
            {
                userInfo = new UserLoginInfoDTO
                {
                    UserId = userId,
                    Username = username,
                    Email = email,
                    Role = roleName 
                };
                return true;
            }
            else
            {
                errorMessage = "Invalid email or password.";
                return false;
            }
        }
    }
}