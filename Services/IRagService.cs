using System.Threading.Tasks;

namespace GamerLinkApp.Services
{
    public interface IRagService
    {
        Task InitializeAsync();
        Task<string> AskAsync(string question);
    }
}
