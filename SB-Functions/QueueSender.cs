using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Messaging.ServiceBus;
using Azure.Identity;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using System.Linq;

namespace SB_Functions
{
    public class QueueSender
    {
        private readonly IConfiguration _configuration;
        private readonly IConfigurationRefresher _configurationRefresher;

        public QueueSender(IConfiguration configuration, IConfigurationRefresherProvider refresherProvider)
        {
            _configuration = configuration;
            _configurationRefresher = refresherProvider.Refreshers.First();
        }

        [FunctionName("QueueSender")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            await _configurationRefresher.TryRefreshAsync();

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                //: $"Hello, {name}. This HTTP triggered function executed successfully.";
                : await SendToSB(name);

            return new OkObjectResult(responseMessage);
        }

        private async Task<string> SendToSB(string messageStr)
        {

            string fullyQualifiedNamespace = _configuration["SB-Function:QueueSender:SERVICE_BUS_FULLY_QUALIFIED_NAMESPACE"];
            string queueName = _configuration["SB-Function:QueueSender:SERVICE_BUS_QUEUE_NAME"];

            try
            {
                ServiceBusClient client = new ServiceBusClient(fullyQualifiedNamespace, new DefaultAzureCredential(), 
                    new ServiceBusClientOptions { 
                        TransportType = ServiceBusTransportType.AmqpTcp 
                    });


                ServiceBusSender sender = client.CreateSender(queueName);
                ServiceBusMessage message = new ServiceBusMessage(messageStr);
                await sender.SendMessageAsync(message);
            }
            catch (Exception e)
            {
                return e.Message + " : " + e.InnerException;
            }

            return "Message Sent to Service Bus";

        }
    }
}
