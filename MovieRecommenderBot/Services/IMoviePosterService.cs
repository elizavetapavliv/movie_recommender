using System.Threading.Tasks;

namespace MovieRecommenderBot.Services
{
    public interface IMoviePosterService
    {
        Task<string> GetPosterLinkAsync(int movieId);
    }
}