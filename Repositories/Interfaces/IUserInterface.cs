using Repositories.Models;

namespace Repositories.Interfaces
{
    public interface IUserInterface
    {
        Task<List<User>> GetAll();
        Task<User?> GetOne(string id);
        Task<int> Add(User model);
        Task<int> Update(User model);
        Task<int> Delete(string userid);
        Task<User?> Login(LoginVM model);
        Task<int> UpdatePassword(ChangePasswordVM model);
    }
}