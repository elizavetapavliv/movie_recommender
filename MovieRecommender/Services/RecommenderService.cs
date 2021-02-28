using System.Collections.Generic;
using System.Linq;
using MovieRecommender.DataModels;

namespace MovieRecommender.Services
{
    public class RecommenderService : IRecommenderService
    {
        private readonly Predictor _predictor;
        private readonly UserProfile _userProfile;

        public RecommenderService(
            Predictor predictor,
            UserProfile profile)
        {
            _predictor = predictor;
            _userProfile = profile;
        }

        public bool Login(string userName)
        {
            return _userProfile.UserExists(userName);
        }

        public IEnumerable<Recommendation> GetRecommendations(string userName)
        {
            return _predictor.PredictTop5(_userProfile.GetUserRatings(userName));
        }

        public void UpdateUserRatings(string userName, IList<Rating> ratings)
        {
            ratings = ratings.GroupBy(r => r.MovieId)
                .Select(g => g.Last())
                .ToList();

            _userProfile.UpdateUserRatings(userName, ratings);
        }

        public void CreateUserRatings(string userName, IList<Rating> ratings)
        {
            ratings = ratings.GroupBy(r => r.MovieId)
                .Select(g => g.Last())
                .ToList();

            _userProfile.CreateNewUserRatings(userName, ratings);
        }
    }
}