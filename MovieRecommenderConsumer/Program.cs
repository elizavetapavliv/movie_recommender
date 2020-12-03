using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MovieRecommender
{
    class Program
    {
        static int userId = 1;
        static void Main()
        {
            Console.WriteLine($"Calculating the top 5 movies for user {userId}...");
            var mlContext = new MLContext();
            var trainedModel = mlContext.Model.Load(Path.Combine(@"../../../", "Model", "MovieRecommenderModel.zip"),  out _);         
            PredictTop5(mlContext, trainedModel);
        }

        private static List<Movie> LoadMovies()
        {
            var data = File.ReadAllLines(Path.Combine(Environment.CurrentDirectory, "Data", "movies.csv"));
            var movies = new List<Movie>(data.Length - 1);

            for (int i = 1; i < data.Length; i++)
            {
                var split = data[i].Split('"');
                if (split.Length == 1)
                {
                    split = split[0].Split(',');
                }
                else
                {
                    if (split.Length > 3)
                    {
                        split[1] = data[i].Substring(split[0].Length + 1,
                            data[i].Length - split[0].Length - split[split.Length - 1].Length - 1);
                    }
                    split[0] = split[0].Substring(0, split[0].Length - 1);
                    split[2] = split[split.Length - 1].Substring(1);
                }
                movies.Add(new Movie { Id = float.Parse(split[0]), Title = split[1], Genres = split[2].Split('|') });
            }
            return movies;
        }

        public static void PredictTop5(MLContext mlContext, ITransformer model)
        {
            var predictionEngine = mlContext.Model.CreatePredictionEngine<MovieRating, MovieRatingPrediction>(model);
       
            var movies = LoadMovies();
            var recommendedMovies = movies.Select(movie => new
            {
                movie,
                prediction = predictionEngine.Predict(
                   new MovieRating
                   {
                       UserId = userId,
                       MovieId = movie.Id
                   })
            })
                .Where(moviePrediction => moviePrediction.prediction.PredictedLabel)
                .OrderByDescending(moviePrediction => moviePrediction.prediction.Score)
                .Take(5);

            var number = 1;
            foreach (var recommended in recommendedMovies)
            {
                Console.WriteLine($"{number++}. {recommended.movie.Title}, { string.Join("|", recommended.movie.Genres)} " +
                    $"(Probability {recommended.prediction.Probability})");

            }
        }
    }
}
