using Repositories.Models;

namespace Repositories.Interfaces
{
    public interface IChatInterface
    {
        Task<int> SaveChat(Chat chat);
        Task<List<Chat>?> GetChatHistory(Guid senderId, Guid receiverId);
        Task<int> MarkChatAsRead(int chatId);
        Task<List<Chat>?> GetUnreadChats(Guid userId);
        Task<List<Chat>?> GetUnreadChatsAll();
        Task<bool> MarkAllChatsAsRead(Guid senderId, Guid receiverId);
    }
}