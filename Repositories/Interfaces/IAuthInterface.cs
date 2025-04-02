using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repositories.Models;

namespace Repositories.Interfaces
{
    public interface IAuthInterface
    {
        Task<T?> Login<T>(T model);
        Task<int> UpdatePassword(ChangePasswordVM model);
    }
}