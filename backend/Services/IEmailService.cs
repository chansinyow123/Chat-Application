using backend.Data;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace backend.Services
{
    public interface IEmailService
    {
        Task SendForgotPasswordEmailAsync(ApplicationUser user, string path);
        Task SendSignUpEmailAsync(ApplicationUser user, string path);
    }
}