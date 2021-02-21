using System.Collections.Generic;
using Microsoft.Azure.Cosmos.Table;
using MovieRecommender.DataModels;
using Newtonsoft.Json;

namespace MovieRecommender
{
    public class UserStorage
    {
        private const string ConnectionString =
            "DefaultEndpointsProtocol=https;AccountName=rgcinemabotdiag;AccountKey=DCuqicvptPG1pYRZL+YYhZwcrg1LgSAKYXZtKczjkBXAvM1Z5+y1Hn261h6qMAbujfuNd69lKkaBXE57msHSoQ==;EndpointSuffix=core.windows.net";

        private const string TableName = "Ratings";
        private const string PartitionKey = "Users";

        private readonly CloudTable _tableClient;

        public UserStorage()
        {
            var storageAccount = CloudStorageAccount.Parse(ConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            _tableClient = tableClient.GetTableReference(TableName);
            _tableClient.CreateIfNotExists();
        }

        public void Create(string userId, IList<Rating> ratings)
        {
            var entity = new UserRatingEntity(userId)
            {
                Ratings = ratings
            };
            _tableClient.Execute(TableOperation.InsertOrReplace(entity));
        }

        public void Update(string userId, IList<Rating> ratings)
        {
            var retrieveOperation =
                TableOperation.Retrieve<UserRatingEntity>(PartitionKey, userId);

            var result = _tableClient.Execute(retrieveOperation);
            if (result.Result is UserRatingEntity entity)
            {
                var currentRatings = (List<Rating>)entity.Ratings;
                currentRatings.AddRange(ratings);
                entity.Ratings = currentRatings;
                _tableClient.Execute(TableOperation.Replace(entity));
            }
        }

        public bool UserExists(string userId)
        {
            var retrieveOperation =
                TableOperation.Retrieve<UserRatingEntity>(PartitionKey, userId);

            var result = _tableClient.Execute(retrieveOperation);

            return result.Result is UserRatingEntity;
        }

        public IEnumerable<Rating> GetUserRatings(string userId)
        {
            var retrieveOperation =
                TableOperation.Retrieve<UserRatingEntity>(PartitionKey, userId);

            var result = _tableClient.Execute(retrieveOperation);
            return (result.Result as UserRatingEntity)?.Ratings;
        }

        private class UserRatingEntity : TableEntity
        {
            public UserRatingEntity()
            {
            }

            public UserRatingEntity(string userId)
                :base(UserStorage.PartitionKey, userId)
            {
            }
            public string RatingsInternal { get; set; }

            [IgnoreProperty]
            public IList<Rating> Ratings
            {
                get => JsonConvert.DeserializeObject<IList<Rating>>(RatingsInternal);
                set => RatingsInternal = JsonConvert.SerializeObject(value);
            }
        }
    }
}