using System.Collections.Generic;
using MovieRecommender.DataModels;

namespace MovieRecommender.Services
{
    public interface IRecommendationPrinter
    {
        void PrintRecommendations(IEnumerable<Recommendation> recommendations);
    }
}