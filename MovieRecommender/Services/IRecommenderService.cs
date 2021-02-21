using System.Collections.Generic;
using MovieRecommender.DataModels;

namespace MovieRecommender.Services
{
    public interface IRecommenderService
    {
        bool Login(string userName);

        IEnumerable<Recommendation> GetRecommendations(string userName);

        void UpdateUserRatings(string userName, IList<Rating> ratings);

        void CreateUserRatings(string userName, IList<Rating> ratings);
    }
}