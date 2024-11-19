using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
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
            [ServiceBusTrigger("%QueueName%", Connection = "ServiceBusConnectionString")] string message,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing message: {Message}", message);

                var api_version = Environment.GetEnvironmentVariable("API_VERSION") ?? throw new ArgumentNullException("API_VERSION");
                var sp = Environment.GetEnvironmentVariable("SP") ?? throw new ArgumentNullException("SP");
                var sv = Environment.GetEnvironmentVariable("SV") ?? throw new ArgumentNullException("SV");
                var sig = Environment.GetEnvironmentVariable("SIG") ?? throw new ArgumentNullException("SIG");

                var url = $"https://prod-238.westeurope.logic.azure.com:443/workflows/b14e904cbfdb424cb056e59d052aaa59/triggers/When_a_HTTP_request_is_received/paths/invoke?api-version={api_version}&sp={sp}&sv={sv}&sig={sig}";

                var response = await _httpClient.GetAsync(url, cancellationToken);

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
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request failed while processing the message.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing the message.");
                throw;
            }
        }
    }
}