using Microsoft.ML.Data;

namespace MovieRecommender
{
    class MovieRecommend
    {
        [ColumnName("Label")]
        public bool IsRecommended { get; set; }
    }
}