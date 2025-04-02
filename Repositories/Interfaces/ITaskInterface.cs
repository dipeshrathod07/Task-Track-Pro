using Repositories.Models;

namespace Repositories.Interfaces
{
    public interface ITaskInterface
    {
        Task<List<Models.Task>> GetAll();
        Task<List<Models.Task>> GetAllByUser(string id);
        Task<Models.Task?> GetOne(string id);
        Task<int> Add(Models.Task model);
        Task<int> Update(Models.Task model);
        Task<int> Delete(string id);
    }
}