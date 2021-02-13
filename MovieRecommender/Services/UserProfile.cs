using System.Collections.Generic;
using MovieRecommender.DataModels;

namespace MovieRecommender.Services
{
    public class UserProfile
    {
        private readonly UserStorage _storage;

        public UserProfile(UserStorage storage)
        {
            _storage = storage;
        }

        public void UpdateUserRatings(string userId, IList<Rating> ratings)
        {
            _storage.Update(userId, ratings);
        }

        public IEnumerable<Rating> GetUserRatings(string userId)
        {
            return _storage.GetUserRatings(userId);
        }

        public void CreateNewUserRatings(string userId, IList<Rating> ratings)
        {
            _storage.Create(userId, ratings);
        }

        public bool UserExists(string userId)
        {
            return _storage.UserExists(userId);
        }

    }
}