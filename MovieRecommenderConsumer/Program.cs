using System;
using MovieRecommender.Services;

namespace MovieRecommenderConsumer
{
    class Program
    {
        static void Main()
        {
            var recommenderService = new RecommenderService();
            
            Console.WriteLine("Enter your name: ");
            var userName = Console.ReadLine();
            
            recommenderService.Login(userName);

            while (true)
            {
                Console.WriteLine("Get recommendations - 1");
                Console.WriteLine("Exit - 2");
                var choice = int.Parse(Console.ReadLine());
                if (choice == 2)
                {
                    break;
                }
                recommenderService.GetRecommendations(userName);
            }
        }
    }
}
