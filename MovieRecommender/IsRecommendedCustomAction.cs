using Microsoft.ML.Transforms;
using System;
using MovieRecommender.DataModels;

namespace MovieRecommender
{
    [CustomMappingFactoryAttribute("IsRecommended")]
    class IsRecommendedCustomAction : CustomMappingFactory<MovieRating, MovieRecommend>
    {
        public override Action<MovieRating, MovieRecommend> GetMapping()
        {
            return (input, output) => output.IsRecommended = input.Rating > 3.5;
        }
    }

}
