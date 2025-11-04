using System.Collections.Generic;
using System.Threading.Tasks;

namespace GamerLinkApp.Services
{
    public interface IRagService
    {
        Task InitializeAsync();
        Task<RagResponse> AskAsync(string question);
        Task<IReadOnlyList<string>> GetPopularQuestionsAsync(int maxQuestions = 6);
    }
}
