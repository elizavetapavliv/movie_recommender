namespace MovieRecommender
{
    public class MovieRatingPrediction
    {
        public bool PredictedLabel { get; set; }
        public float Score { get; set; }
        public float Probability { get; set; }
    }
}