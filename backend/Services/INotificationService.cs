using backend.Data;
using backend.Models.Chat;
using System.Threading.Tasks;

namespace backend.Services
{
    public interface INotificationService
    {
        string GetNotificationMessage(MessageModel privateChat);
        Task SendNotificationAsync(string receiverId, string payloadJSON);
    }
}