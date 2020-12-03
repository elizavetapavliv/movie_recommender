using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MovieRecommender
{
    class Program
    {
        public static List<Movie> movies;
        static void Main()
        {
            MLContext mlContext = new MLContext();
            (IDataView trainingDataView, IDataView testDataView) = LoadData(mlContext);

            var model = BuildAndTrainModel(mlContext, trainingDataView);
            EvaluateModel(mlContext, testDataView, model);
            SaveModel(mlContext, trainingDataView, model);
        }

        public static List<Movie> LoadMovies()
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

        public static (IDataView training, IDataView test) LoadData(MLContext mlContext)
        {
            var dataPath = Path.Combine(Environment.CurrentDirectory, "Data", "ratings.csv");
            var ratingsData = mlContext.Data.LoadFromTextFile<Rating>(dataPath, hasHeader: true, separatorChar: ',');

            var ratings = mlContext.Data.CreateEnumerable<Rating>(ratingsData, false);
            movies = LoadMovies();

            var movieRatings = ratings.Join(movies, rating => rating.MovieId, movie => movie.Id,
             (rating, movie) => movie.Genres.Select(genre => new MovieRating
             {
                 UserId = rating.UserId,
                 MovieId = movie.Id,
                 MovieTitle = movie.Title,
                 MovieGenre = genre,
                 Rating = rating.RatingValue
             })).SelectMany(movieRating => movieRating);


            var data = mlContext.Data.LoadFromEnumerable(movieRatings);

            var split = mlContext.Data.TrainTestSplit(data, testFraction: 0.2);

            var training = split.TrainSet;
            training = mlContext.Data.Cache(training);

            return (training, split.TestSet);
        }
        public static ITransformer BuildAndTrainModel(MLContext mlContext, IDataView trainingDataView)
        {
            var featureColumnNames = trainingDataView.Schema.Select(column => column.Name).Where(columnName => columnName != "Rating").ToArray();

            var dataProcessPipeline = mlContext.Transforms.CustomMapping(new IsRecommendedCustomAction().GetMapping(), contractName: "IsRecommended")
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("UserId"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("MovieId"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("MovieTitle"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("MovieGenre"))
                .Append(mlContext.Transforms.Concatenate("Features", featureColumnNames));

            var trainingPipeLine = dataProcessPipeline.Append(
                mlContext.BinaryClassification.Trainers.FieldAwareFactorizationMachine());

            var model = trainingPipeLine.Fit(trainingDataView);

            return model;
        }

        public static void EvaluateModel(MLContext mlContext, IDataView testDataView, ITransformer model)
        {
            var prediction = model.Transform(testDataView);

            var metrics = mlContext.BinaryClassification.Evaluate(prediction);
            File.WriteAllLines(Path.Combine(@"../../../", "Model", "evaluation_metrics.txt"), new string[]
            {
                "Evaluation Metrics",
                $"Accuracy: {Math.Round(metrics.Accuracy, 2)}",
                $"Area Under Roc Curve: {Math.Round(metrics.AreaUnderRocCurve, 2)}",
                $"Area Under Precision Recall Curve: {Math.Round(metrics.AreaUnderPrecisionRecallCurve, 2)}", 
                $"F1 Score: {Math.Round(metrics.F1Score, 2)}",
                $"Entropy: {Math.Round(metrics.Entropy, 2)}",
                $"Log-loss: {Math.Round(metrics.LogLoss, 2)}",
                $"Log-loss Reduction: {Math.Round(metrics.LogLossReduction, 2)}",
                $"Negative Precision: {Math.Round(metrics.NegativePrecision, 2)}",
                $"Negative Recall: {Math.Round(metrics.NegativeRecall, 2)}",
                $"Positive Precision: {Math.Round(metrics.PositivePrecision, 2)}",
                $"Positive Recall: {Math.Round(metrics.PositiveRecall, 2)}",
                $"Confusion matrix: {metrics.ConfusionMatrix.GetFormattedConfusionTable()}"
            });
        }

        public static void SaveModel(MLContext mlContext, IDataView trainingDataView, ITransformer model)
        {
            mlContext.ComponentCatalog.RegisterAssembly(typeof(IsRecommendedCustomAction).Assembly);
            var modelPath = Path.Combine(@"../../../", "Model", "MovieRecommenderModel.zip");
            mlContext.Model.Save(model, trainingDataView.Schema, modelPath);
        }
    }
}
