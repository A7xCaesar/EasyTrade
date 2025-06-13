using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Managers
{
    public abstract class UserManager
    {

        protected string userId { get; private set; }
        protected string roleId { get; private set; }
        protected string username { get; private set; }
        protected string email { get; private set; }

        protected string passwordHash { get; private set; } 

        protected DateTime registrationDate { get; private set; }


        protected UserManager(string userId, string roleId, string username, string email, string passwordHash, DateTime registrationDate)
        {
            this.userId = userId;
            this.roleId = roleId;
            this.username = username;
            this.email = email;
            this.passwordHash = passwordHash;
            this.registrationDate = registrationDate;

        }

       


    }
}
