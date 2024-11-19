using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace MondayPOCFA
{
    public class ServiceBusQueueTrigger
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ServiceBusQueueTrigger> _logger;

        public ServiceBusQueueTrigger(IHttpClientFactory httpClientFactory, ILogger<ServiceBusQueueTrigger> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        [Function("ServiceBusQueueTrigger")]
        public async Task Run(
            [ServiceBusTrigger("validated-queue", Connection = "ServiceBusConnectionString")] string message,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing message: {Message}", message);

                var apiUrl = Environment.GetEnvironmentVariable("RestApiUrl")
                    ?? throw new InvalidOperationException("RestApiUrl is not configured");
                var apiKey = Environment.GetEnvironmentVariable("RestApiKey")
                    ?? throw new InvalidOperationException("RestApiKey is not configured");

                var content = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("api_dev_key", apiKey),
                new KeyValuePair<string, string>("api_option", "paste"),
                new KeyValuePair<string, string>("api_paste_code", message)
            });

                using var response = await _httpClient.PostAsync(apiUrl, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Successfully sent to REST API: {result}");
                }
                else
                {
                    _logger.LogWarning($"Failed to send data to REST API: {response.StatusCode}");
                }

                _logger.LogInformation("Message processed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing the message");
                throw;
            }
        }
    }
}
