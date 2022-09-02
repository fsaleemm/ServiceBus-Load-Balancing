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
using System.Collections.Generic;

namespace SB_Functions
{
    public class BatchQueueSender
    {
        private readonly IConfiguration _configuration;
        private readonly IConfigurationRefresher _configurationRefresher;

        public BatchQueueSender(IConfiguration configuration, IConfigurationRefresherProvider refresherProvider)
        {
            _configuration = configuration;
            _configurationRefresher = refresherProvider.Refreshers.First();
        }

        [FunctionName("BatchQueueSender")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            await _configurationRefresher.TryRefreshAsync();

            int numOfMessages = 0;
            string count = req.Query["count"];

            int.TryParse(count, out numOfMessages);

            string responseMessage = string.IsNullOrEmpty(count)
                ? "This HTTP triggered function executed successfully. Pass a count in the query string or in the request body for a personalized response."
                : await SendToSB(numOfMessages);

            return new OkObjectResult(responseMessage);
        }

        private async Task<string> SendToSB(int numOfMessages)
        {

            string fullyQualifiedNamespace = _configuration["SB-Function:BatchQueueSender:SERVICE_BUS_FULLY_QUALIFIED_NAMESPACE"];
            string queueName = _configuration["SB-Function:BatchQueueSender:SERVICE_BUS_QUEUE_NAME"];
            IList<ServiceBusMessage> messages = new List<ServiceBusMessage>();

            try
            {
                ServiceBusClient client = new ServiceBusClient(fullyQualifiedNamespace, new DefaultAzureCredential(),
                    new ServiceBusClientOptions
                    {
                        TransportType = ServiceBusTransportType.AmqpTcp
                    });


                ServiceBusSender sender = client.CreateSender(queueName);

                for (int i = 0; i < numOfMessages; i++)
                {
                    messages.Add(new ServiceBusMessage("Message: # " + i));
                }

                await sender.SendMessagesAsync(messages);
            }
            catch (Exception e)
            {
                return e.Message + " : " + e.InnerException;
            }

            return "Message Sent to Service Bus";

        }
    }
}
