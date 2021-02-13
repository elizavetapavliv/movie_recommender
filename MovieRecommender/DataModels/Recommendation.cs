namespace MovieRecommender.DataModels
{
    public class Recommendation
    {
        public Movie Movie { get; set; }

        public MovieRatingPrediction Prediction { get; set; }
    }
}