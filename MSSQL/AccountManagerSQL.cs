using System;
using Microsoft.Data.SqlClient;
using Interfaces;
using EasyTrade_Crypto.Interfaces;
namespace EasyTrade_Crypto.MSSQL
{
    public class AccountManagerSQL : IAccountManagerSQL
    {
        private readonly IDbConnectionStringProvider _connectionProvider;

        public AccountManagerSQL(IDbConnectionStringProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

      
        public bool InsertUser(string userId, string roleName, string username, string email, string passwordHash, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionProvider.ConnectionString))
                {
                    conn.Open();

                    string sql = @"
                        INSERT INTO users (userId, roleId, username, email, passwordHash, registrationDate)
                        VALUES (@userId, @roleId, @username, @email, @passwordHash, @registrationDate)";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.Parameters.AddWithValue("@roleName", roleName);
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@passwordHash", passwordHash);
                        cmd.Parameters.AddWithValue("@registrationDate", DateTime.UtcNow);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"SQL insertion error: {ex.Message}";
                return false;
            }

            return true;
        }
    }
}

