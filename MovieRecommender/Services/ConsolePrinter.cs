using System;
using System.Collections.Generic;
using MovieRecommender.DataModels;

namespace MovieRecommender.Services
{
    public class ConsolePrinter : IRecommendationPrinter
    {
        private readonly DataProcessor _dataProcessor;

        public ConsolePrinter(DataProcessor dataProcessor)
        {
            _dataProcessor = dataProcessor;
        }

        public void PrintMessage(string message)
        {
            Console.WriteLine(message);
        }

        public void PrintMovies(IEnumerable<Movie> movies)
        {
            PrintHeader();

            foreach (var movie in movies)
            {
                Console.WriteLine(
                    $"{movie.Id}. {_dataProcessor.GetMovieTitle(movie.Title)}, {string.Join("|", movie.Genres)}");
            }
        }

        public void PrintRecommendations(IEnumerable<Recommendation> recommendations)
        {
            PrintHeader();

            foreach (var recommendation in recommendations)
            {
                Console.WriteLine($"{recommendation.Movie.Id}. {_dataProcessor.GetMovieTitle(recommendation.Movie.Title)}, " +
                                  $"{string.Join("|", recommendation.Movie.Genres)} " +
                                  $"(Probability {recommendation.Prediction.Probability * 100: 0.##}%)");
            }
        }

        private void PrintHeader()
        {
            Console.WriteLine("MovieId. Title, Genres");
        }
    }
}