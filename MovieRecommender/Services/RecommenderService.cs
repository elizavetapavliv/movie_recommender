using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.ML;
using MovieRecommender.DataModels;

namespace MovieRecommender.Services
{
    public class RecommenderService
    {
        private readonly Predictor _predictor;
        private readonly UserProfile _userProfile;
        private readonly DataProcessor _dataProcessor;

        public RecommenderService()
        {
            var mlContext = new MLContext();
            var trainedModel =
                mlContext.Model.Load(Path.Combine("Model", "MovieRecommenderModel.zip"), out _);

            _dataProcessor = new DataProcessor(mlContext);
            _predictor = new Predictor(mlContext, trainedModel, _dataProcessor);
            _userProfile = new UserProfile(new UserStorage());
        }

        public void Login(string userName)
        {
            if (!_userProfile.UserExists(userName))
            {
                Console.WriteLine("Hello! Here are your initial recommendations. Please, rate them from 1 to 5, so we can " +
                                  "complete more appropriate recommendations for you.");
                PrintHeader();
                PrintMovies(_dataProcessor.BestMovies);
                _userProfile.CreateNewUserRatings(userName, GetRatingsFromUser());
            }
        }

        public void GetRecommendations(string userName)
        {
            Console.WriteLine("Loading recommendations for you... " +
                              "Please, rate movies you liked from 1 to 5 for further recommendations.");

            var recommendedMovies = _predictor.PredictTop5(_userProfile.GetUserRatings(userName));
            PrintHeader();
            foreach (var recommended in recommendedMovies)
            {
                Console.WriteLine(
                    $"{recommended.Movie.Id}. {_dataProcessor.GetMovieTitle(recommended.Movie.Title)}, " +
                    $"{string.Join("|", recommended.Movie.Genres)} " +
                    $"(Probability {recommended.Prediction.Probability * 100 : 0.##}%)");
            }
            _userProfile.UpdateUserRatings(userName, GetRatingsFromUser());
        }

        private IList<Rating> GetRatingsFromUser()
        {
            var ratings = new List<Rating>();

            Console.WriteLine("\nEnter your ratings in format <movieId rating>. To finish process, type 'exit'.");
            while (true)
            {
                var input = Console.ReadLine();
                if (input == "exit")
                {
                    break;
                }

                var split = input.Split();
                ratings.Add(new Rating
                {
                    MovieId = int.Parse(split[0]),
                    RatingValue = float.Parse(split[1])
                });
            }
            return ratings;
        }

        private void PrintHeader()
        {
            Console.WriteLine("MovieId. Title, Genres");
        }

        private void PrintMovies(IEnumerable<Movie> movies)
        {
            foreach (var movie in movies)
            {
                Console.WriteLine(
                    $"{movie.Id}. {_dataProcessor.GetMovieTitle(movie.Title)}, {string.Join("|", movie.Genres)}");
            }
        }

    }
}