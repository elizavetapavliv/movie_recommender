using Microsoft.ML;

namespace MovieRecommender
{
    class Program
    {
        static void Main()
        {
            var mlContext = new MLContext();
            var dataProcessor = new DataProcessor(mlContext);
           // (IDataView trainingDataView, IDataView testDataView) = dataProcessor.LoadData();
            dataProcessor.InitUserRatingsData();
            //dataProcessor.InitBestMoviesData();

            //var modelPreparer = new ModelPreparer();
            //var model = modelPreparer.BuildAndTrainModel(mlContext, trainingDataView);
            //modelPreparer.EvaluateModel(mlContext, testDataView, model);
            //modelPreparer.SaveModel(mlContext, trainingDataView, model);
        }
    }
}
