namespace MovieRecommender.DataModels
{
    public class MovieRating
    {
        public float UserId { get; set; }
        public float MovieId { get; set; }
        public string MovieTitle { get; set; }
        public string MovieGenre { get; set; }
        public float Rating { get; set; }
    }
}
