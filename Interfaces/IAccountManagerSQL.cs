using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IAccountManagerSQL
    {
        public bool InsertUser(
            string userId,
            string roleName,
            string username,
            string email,
            string passwordHash,
            out string errorMessage
        );
    }
}
