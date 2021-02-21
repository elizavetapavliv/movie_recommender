using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MovieRecommenderBot.Options;

namespace MovieRecommenderBot.Services
{
    public class MoviePosterService : IMoviePosterService
    {
        private readonly HttpClient _httpClient;

        private readonly string _movieUri;
        private readonly string _posterUri;
        private readonly string _loginUri;

        private readonly LoginOptions _loginOptions;

        public MoviePosterService(IHttpClientFactory clientFactory,
            IOptions<UriOptions> uriOptions,
            IOptions<LoginOptions> loginOptions)
        {
            _httpClient = clientFactory.CreateClient();

            _movieUri = uriOptions.Value.MovieUri;
            _posterUri = uriOptions.Value.PosterUri;
            _loginUri = uriOptions.Value.LoginUri;

            _loginOptions = loginOptions.Value;
        }

        public async Task<string> GetPosterLinkAsync(int movieId)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("RecommenderBot", "0.1"));

            await _httpClient.PostAsync(_loginUri,
                new StringContent(
                    JsonSerializer.Serialize(_loginOptions, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }), Encoding.UTF8, "application/json"));

            string json = await _httpClient.GetStringAsync(new Uri($"{_movieUri}/{movieId}"));

            return _posterUri + JsonDocument.Parse(json)
                .RootElement
                .GetProperty("data")
                .GetProperty("movieDetails")
                .GetProperty("movie")
                .GetProperty("posterPath");
        }
    }
}