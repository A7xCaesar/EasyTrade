using DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IRegisterDTO
    {
        bool RegisterUser(RegisterDTO dto, out string errorMessage);
    }
}
