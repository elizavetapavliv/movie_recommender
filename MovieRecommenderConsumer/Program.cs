using System;
using System.Collections.Generic;
using Microsoft.ML;
using MovieRecommender;
using MovieRecommender.DataModels;
using MovieRecommender.Services;

namespace MovieRecommenderConsumer
{
    class Program
    {
        private static ConsolePrinter _printer;

        static void Main()
        {
            var mlContext = new MLContext();
            var dataProcessor = new DataProcessor(mlContext);
            var predictor = new Predictor(mlContext, dataProcessor);
            _printer = new ConsolePrinter(dataProcessor);
            var userProfile = new UserProfile(new UserStorage());

            var recommenderService = new RecommenderService(predictor, userProfile);
            
            Console.WriteLine("Enter your name: ");
            var userName = Console.ReadLine();

            if (!recommenderService.Login(userName))
            {
                _printer.PrintMessage(
                    "Hello! Here are your initial recommendations. Please, rate them from 1 to 5, so we can " +
                    "complete more appropriate recommendations for you.");

                _printer.PrintMovies(dataProcessor.BestMovies);
                recommenderService.CreateUserRatings(userName, GetRatingsFromUser());

            }

            while (true)
            {
                Console.WriteLine("Get recommendations - 1");
                Console.WriteLine("Exit - 2");
                var choice = int.Parse(Console.ReadLine());
                if (choice == 2)
                {
                    break;
                }

                _printer.PrintMessage("Loading recommendations for you... " +
                                      "Please, rate movies you liked from 1 to 5 for further recommendations.");

                var recommendedMovies = recommenderService.GetRecommendations(userName);
                _printer.PrintRecommendations(recommendedMovies);
                recommenderService.UpdateUserRatings(userName, GetRatingsFromUser());
            }
        }

        private static IList<Rating> GetRatingsFromUser()
        {
            var ratings = new List<Rating>();

            _printer.PrintMessage("Enter your ratings in format <movieId rating>. To finish process, type 'exit'.");
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
    }
}
