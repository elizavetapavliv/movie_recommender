using Microsoft.ML.Data;

namespace MovieRecommender.DataModels
{
    class MovieRecommend
    {
        [ColumnName("Label")]
        public bool IsRecommended { get; set; }
    }
}