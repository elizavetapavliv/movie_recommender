using Microsoft.ML.Data;

namespace MovieRecommender.DataModels
{
    public class UserRating
    {
        [LoadColumn(0)]
        public float UserId { get; set; }
        [LoadColumn(1)]
        public float MovieId { get; set; }
        [LoadColumn(2)]
        public float RatingValue { get; set; }
    }
}