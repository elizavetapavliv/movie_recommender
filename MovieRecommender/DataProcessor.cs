using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Accord.Math;
using Microsoft.ML;
using MovieRecommender.DataModels;

namespace MovieRecommender
{
    public class DataProcessor
    {
        public readonly string UserRatingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            @"../../../../", "MovieRecommender", "Data", "user_ratings.csv");
        private readonly string _bestMoviesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            @"../../../../", "MovieRecommender", "Data", "best_movies.csv");
        private readonly string _moviesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            @"../../../../", "MovieRecommender", "Data", "movies.csv");

        private List<Movie> _movies;
        private List<UserRating> _ratings;
        private Dictionary<float, string[]> _movieGenres;
        private Dictionary<string, int> _genreIndex;
        private List<Movie> _bestMovies;
        private readonly List<string> _genres = new List<string>
        {
            "Action",
            "Adventure",
            "Animation",
            "Children",
            "Comedy",
            "Crime",
            "Documentary",
            "Drama",
            "Fantasy",
            "Film-Noir",
            "Horror",
            "Musical",
            "Mystery",
            "Romance",
            "Sci-Fi",
            "Thriller",
            "War",
            "Western",
            "IMAX",
            "(no genres listed)"
        };

        private readonly MLContext _mlContext;

        public DataProcessor(MLContext mlContext)
        {
            _mlContext = mlContext;
        }

        public List<Movie> Movies => _movies ??= LoadMovies(_moviesPath, true);

        public List<UserRating> Ratings
        {
            get
            {
                if (_ratings == null)
                {
                    LoadRatings();
                }

                return _ratings;
            }
        }

        public Dictionary<float, string[]> MovieGenres
        {
            get
            {
                return _movieGenres ??= Movies
                    .Select(movie => new {movie.Id, movie.Genres})
                    .ToDictionary(x => x.Id, x => x.Genres);
            }
        }

        public Dictionary<string, int> GenreIndex
        {
            get
            {
                return _genreIndex ??= _genres
                    .Select((genre, i) => new { genre, Index = i })
                    .ToDictionary(x => x.genre, x => x.Index);

            }
        }

        public List<Movie> BestMovies => _bestMovies ??= LoadMovies(_bestMoviesPath, false);
        
        public void InitUserRatingsData()
        {
            if (!File.Exists(UserRatingsPath))
            {
                var users = Ratings.GroupBy(r => r.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        MoviesRatings = g.Select(r => new Rating
                        {
                            MovieId = (int)r.MovieId,
                            RatingValue = r.RatingValue
                        })
                    })
                    .OrderBy(u => u.UserId)
                    .ToList();

                var usersData = new List<string>();
                var usersCount = users.Count;
                int k = 0;

                foreach (var user in users)
                {
                    var userRatings = GetUserAllMoviesRatings(user.MoviesRatings);
                    usersData.Add($"{user.UserId} {string.Join(" ", userRatings)}");

                    Console.WriteLine($"Processed user {++k} from {usersCount}");
                    if (k % 10000 == 0)
                    {
                        File.AppendAllLines(UserRatingsPath, usersData);
                        usersData.Clear();
                    }
                }

                File.AppendAllLines(UserRatingsPath, usersData);
            }
        }

        public double[] GetUserAllMoviesRatings(IEnumerable<Rating> ratings)
        {
            var userRatings = Enumerable.Repeat(0.0, _genres.Count).ToArray();

            foreach (var rating in ratings)
            {
                var genres = MovieGenres[rating.MovieId];

                foreach (var genre in genres)
                {
                    userRatings[GenreIndex[genre]] += rating.RatingValue;
                }
            }

            var maxGenreRatingSum = 5.0 * Movies.Count;

            for (var i = 0; i < _genres.Count; i++)
            {
                userRatings[i] /= maxGenreRatingSum;
            }

            return userRatings;
        }

        public void InitBestMoviesData()
        {
            if (!File.Exists(_bestMoviesPath))
            {
                _bestMovies = Movies.GroupJoin(
                        Ratings,
                        m => m.Id,
                        r => r.MovieId,
                        (movie, ratings) => (movie, sumRating: ratings.Sum(ur => ur.RatingValue)))
                    .OrderByDescending(mr => mr.sumRating)
                    .Select(mr => mr.movie).Take(20).ToList();

                Console.WriteLine("Got best");
                int k = 0;
                var bestMoviesCount = _bestMovies.Count;
                var bestMoviesData = new List<string>();

                foreach (var movie in _bestMovies)
                {
                    bestMoviesData.Add($"{movie.Id},{GetMovieTitle(movie.Title)},{string.Join('|', movie.Genres)}");

                    Console.WriteLine($"Processed movie {++k} from {bestMoviesCount}");
                }

                File.WriteAllLines(_bestMoviesPath, bestMoviesData);
            }
        }

        public string GetMovieTitle(string movieTitle)
        {
            return movieTitle.Contains(',') ? $"\"{movieTitle}\"" : movieTitle;
        }

        private List<Movie> LoadMovies(string filePath, bool hasHeader)
        {
            var data = File.ReadAllLines(filePath);
            List<Movie> movies;
            if (hasHeader)
            {
                movies = new List<Movie>(data.Length - 1);
                data = data.RemoveAt(0);
            }
            else
            {
                movies = new List<Movie>(data.Length);
            }

            foreach (var line in data)
            {
                var split = line.Split('"');
                if (split.Length == 1)
                {
                    split = split[0].Split(',');
                }
                else
                {
                    if (split.Length > 3)
                    {
                        split[1] = line.Substring(split[0].Length + 1,
                            line.Length - split[0].Length - split[^1].Length - 1);
                    }
                    split[0] = split[0].Substring(0, split[0].Length - 1);
                    split[2] = split[^1].Substring(1);
                }
                movies.Add(new Movie { Id = float.Parse(split[0]), Title = split[1], Genres = split[2].Split('|') });
            }
            return movies;
        }

        private void LoadRatings()
        {
            var dataPath = Path.Combine(Environment.CurrentDirectory, "Data", "ratings.csv");
            var ratingsData = _mlContext.Data.LoadFromTextFile<UserRating>(dataPath, hasHeader: true, separatorChar: ',');
            _ratings = _mlContext.Data.CreateEnumerable<UserRating>(ratingsData, false).ToList();
        }

        public (IDataView training, IDataView test) LoadData()
        {
            var movieRatings = Ratings.Join(Movies, rating => rating.MovieId, movie => movie.Id,
             (rating, movie) => movie.Genres.Select(genre => new MovieRating
             {
                 UserId = rating.UserId,
                 MovieId = movie.Id,
                 MovieTitle = movie.Title,
                 MovieGenre = genre,
                 Rating = rating.RatingValue
             })).SelectMany(movieRating => movieRating);

            var data = _mlContext.Data.LoadFromEnumerable(movieRatings);
            var split = _mlContext.Data.TrainTestSplit(data, 0.2);

            var training = split.TrainSet;
            training = _mlContext.Data.Cache(training);

            return (training, split.TestSet);
        }
    }
}