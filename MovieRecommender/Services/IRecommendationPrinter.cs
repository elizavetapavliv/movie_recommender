using System.Collections.Generic;
using MovieRecommender.DataModels;

namespace MovieRecommender.Services
{
    public interface IRecommendationPrinter
    {
        void PrintMessage(string message);

        void PrintMovies(IEnumerable<Movie> movies);

        void PrintRecommendations(IEnumerable<Recommendation> recommendations);
    }
}