using System;
using BCrypt.Net;
using DTO;
using EasyTrade_Crypto.Interfaces;
using EasyTrade_Crypto.MSSQL; 
using EasyTrade_Crypto.Utilities;
using Interfaces;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace EasyTrade_Crypto.Managers
{

    public class AccountManager : IAccountManager
    {
        private readonly IAccountDAL _accountDAL;
        private readonly IPortfolioDAL _portfolioDAL;
        private readonly IDbConnectionStringProvider _connectionProvider;
        private readonly string _normalRoleName = "Normal"; 
        private const decimal STARTING_EUR_BALANCE = 10000m; // Starting balance for new users

        public AccountManager(IAccountDAL accountDAL, IPortfolioDAL portfolioDAL, IDbConnectionStringProvider connectionProvider)
        {
            _accountDAL = accountDAL ?? throw new ArgumentNullException(nameof(accountDAL));
            _portfolioDAL = portfolioDAL ?? throw new ArgumentNullException(nameof(portfolioDAL));
            _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
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

            // Insert user account
            if (!_accountDAL.InsertUser(userId, _normalRoleName, dto.Username, dto.Email, hashedPassword, out string dalError))
            {
                errorMessage = $"Registration failed: {dalError}";
                return false;
            }

            // Initialize user with starting EUR balance
            try
            {
                var success = InitializeUserWithStartingBalance(userId).Result;
                if (!success)
                {
                    errorMessage = "Account created but failed to initialize starting balance. Please contact support.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Account created but failed to initialize starting balance: {ex.Message}";
                return false;
            }

            return true;
        }

        private async Task<bool> InitializeUserWithStartingBalance(string userId)
        {
            try
            {
                // Get EUR asset ID from the database
                var eurAssetId = await GetEurAssetIdAsync();
                if (string.IsNullOrEmpty(eurAssetId))
                {
                    return false; // EUR asset not found
                }
                
                // Add starting EUR balance using the portfolio DAL
                await _portfolioDAL.UpsertBalanceAsync(userId, eurAssetId, STARTING_EUR_BALANCE);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> GetEurAssetIdAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionProvider.ConnectionString);
                await connection.OpenAsync();
                
                using var command = new SqlCommand("SELECT assetId FROM assets WHERE symbol = 'EUR'", connection);
                var result = await command.ExecuteScalarAsync();
                
                return result?.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
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