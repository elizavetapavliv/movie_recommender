using System.Collections.Generic;
using System.IO;
using System.Linq;
using Accord.Math.Distances;
using Microsoft.ML;
using MovieRecommender.DataModels;

namespace MovieRecommender.Services
{
    public class Predictor
    {
        private readonly string _userRatingsPath;
        private Dictionary<int, double[]> _userRatings;
        private readonly DataProcessor _dataProcessor;
        private readonly MLContext _mlContext;
        private readonly ITransformer _model;

       public Predictor(MLContext mlContext, DataProcessor dataProcessor)
        {
            _mlContext = mlContext;
            _model = mlContext.Model.Load(Path.Combine("Model", "MovieRecommenderModel.zip"), out _);
            _dataProcessor = dataProcessor;
            _userRatingsPath = _dataProcessor.UserRatingsPath;
        }

       public Dictionary<int, double[]> UserRatings
       {
           get
           {
               if (_userRatings == null)
               {
                   _userRatings = new Dictionary<int, double[]>();
                   var data = File.ReadAllLines(_userRatingsPath);
                   foreach (var str in data)
                   {
                       var split = str.Split();
                       _userRatings[int.Parse(split[0])] = split.Skip(1).Select(double.Parse).ToArray();
                   }
               }

               return _userRatings;
           }
       }

       private int FindSimilarUserId(double[] currentUserRatings)
        {
            double maxSimilarity = 0.0;
            int userId = 0;
            var cosine = new Cosine();

            foreach (var userRating in UserRatings)
            {
                var similarity = cosine.Similarity(userRating.Value, currentUserRatings);
                if (similarity > maxSimilarity)
                {
                    maxSimilarity = similarity;
                    userId = userRating.Key;
                }
            }
            return userId;
        }

        public IEnumerable<Recommendation> PredictTop5(IEnumerable<Rating> ratings)
        {
            ratings = ratings.ToList();
            var similarUserId = FindSimilarUserId(_dataProcessor.GetUserAllMoviesRatings(ratings));
            var predictionEngine = _mlContext.Model
                .CreatePredictionEngine<MovieRating, MovieRatingPrediction>(_model);

            var watchedMoviesIds = ratings.Select(r => r.MovieId).ToList();

            return _dataProcessor.Movies.Select(movie => new Recommendation
                {
                    Movie = movie,
                    Prediction = predictionEngine.Predict(
                        new MovieRating
                        {
                            UserId = similarUserId,
                            MovieId = movie.Id
                        })
                })
                .Where(moviePrediction => moviePrediction.Prediction.PredictedLabel
                                          && !watchedMoviesIds.Contains((int)moviePrediction.Movie.Id))
                .OrderByDescending(moviePrediction => moviePrediction.Prediction.Score)
                .Take(5);
        }

    }
}