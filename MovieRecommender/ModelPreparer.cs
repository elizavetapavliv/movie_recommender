using System;
using System.IO;
using System.Linq;
using Microsoft.ML;

namespace MovieRecommender
{
    public class ModelPreparer
    {
        public ITransformer BuildAndTrainModel(MLContext mlContext, IDataView trainingDataView)
        {
            var featureColumnNames = trainingDataView.Schema.Select(column => column.Name).Where(columnName => columnName != "UserRating").ToArray();

            var dataProcessPipeline = mlContext.Transforms.CustomMapping(new IsRecommendedCustomAction().GetMapping(), "IsRecommended")
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

        public void EvaluateModel(MLContext mlContext, IDataView testDataView, ITransformer model)
        {
            var prediction = model.Transform(testDataView);

            var metrics = mlContext.BinaryClassification.Evaluate(prediction);
            File.WriteAllLines(Path.Combine(@"../../../", "Model", "evaluation_metrics.txt"), new[]
            {
                "Evaluation Metrics",
                $"Accuracy: {Math.Round(metrics.Accuracy, 2)}",
                $"Area Under Roc Curve: {Math.Round(metrics.AreaUnderRocCurve, 2)}",
                $"Area Under Precision Recall Curve: {Math.Round(metrics.AreaUnderPrecisionRecallCurve, 2)}",
                $"F1 Score: {Math.Round(metrics.F1Score, 2)}",
                $"Negative Precision: {Math.Round(metrics.NegativePrecision, 2)}",
                $"Negative Recall: {Math.Round(metrics.NegativeRecall, 2)}",
                $"Positive Precision: {Math.Round(metrics.PositivePrecision, 2)}",
                $"Positive Recall: {Math.Round(metrics.PositiveRecall, 2)}",
                $"Confusion matrix: {metrics.ConfusionMatrix.GetFormattedConfusionTable()}"
            });
        }

        public void SaveModel(MLContext mlContext, IDataView trainingDataView, ITransformer model)
        {
            mlContext.ComponentCatalog.RegisterAssembly(typeof(IsRecommendedCustomAction).Assembly);
            var modelPath = Path.Combine(@"../../../", "Model", "MovieRecommenderModel.zip");
            mlContext.Model.Save(model, trainingDataView.Schema, modelPath);
        }
    }
}