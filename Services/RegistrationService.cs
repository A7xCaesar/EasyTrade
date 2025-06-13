using DTO;
using Interfaces;
using Microsoft.Extensions.Logging;
using EasyTrade_Crypto.Interfaces;

namespace EasyTrade_Crypto.Services
{
    public class RegistrationService : IRegistrationService
    {
        private readonly IAccountManager _accountManager;
        private readonly ILogger<RegistrationService> _logger;

       
        public RegistrationService(IAccountManager accountManager, ILogger<RegistrationService> logger)
        {
            _accountManager = accountManager ?? throw new ArgumentNullException(nameof(accountManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

      
        public bool RegisterUser(RegisterInputModel input, out string errorMessage)
        {
            _logger.LogInformation("Registration attempt started for user: {Username}", input?.Username);

            // 1. Expanded Validation Logic
            if (!IsInputModelValid(input, out errorMessage))
            {
                // The validation method already logged the specific warning.
                return false;
            }

            try
            {
                var registerDto = new RegisterDTO
                {
                    Username = input.Username,
                    Email = input.Email,
                    Password = input.Password,
                    ConfirmPassword = input.ConfirmPassword
                };

                // 2. Call the underlying manager
                bool isSuccess = _accountManager.RegisterUser(registerDto, out errorMessage);

                if (isSuccess)
                {
                    _logger.LogInformation("Successfully registered user: {Username}", input.Username);
                    return true;
                }
                else
                {
                    // This is a known business logic failure (e.g., user exists)
                    _logger.LogWarning("Account manager failed to register user {Username}. Reason: {Reason}", input.Username, errorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                // 3. Expanded Error Handling Strategy
                _logger.LogError(ex, "An unexpected error occurred during registration for user: {Username}", input.Username);
                errorMessage = "An unexpected server error occurred. Please try again later.";
                return false;
            }
        }

      
        private bool IsInputModelValid(RegisterInputModel input, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (input == null)
            {
                errorMessage = "Registration data cannot be null.";
                _logger.LogWarning(errorMessage);
                return false;
            }

            if (string.IsNullOrWhiteSpace(input.Username))
            {
                errorMessage = "Username is required.";
                _logger.LogWarning("Registration failed: {Reason}", errorMessage);
                return false;
            }

            if (string.IsNullOrWhiteSpace(input.Email))
            {
                errorMessage = "Email is required.";
                _logger.LogWarning("Registration failed for user '{Username}': {Reason}", input.Username, errorMessage);
                return false;
            }

            if (string.IsNullOrWhiteSpace(input.Password))
            {
                errorMessage = "Password is required.";
                _logger.LogWarning("Registration failed for user '{Username}': {Reason}", input.Username, errorMessage);
                return false;
            }

            if (input.Password != input.ConfirmPassword)
            {
                errorMessage = "Passwords do not match.";
                _logger.LogWarning("Registration failed for user '{Username}': {Reason}", input.Username, errorMessage);
                return false;
            }

            return true;
        }
    }
}