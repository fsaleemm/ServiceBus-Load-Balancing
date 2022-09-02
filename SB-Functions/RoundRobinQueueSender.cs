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
using Microsoft.Extensions.Azure;

namespace SB_Functions
{
    public class RoundRobinQueueSender
    {
        private readonly IConfiguration _configuration;
        private readonly IConfigurationRefresher _configurationRefresher;

        public RoundRobinQueueSender(IConfiguration configuration, IConfigurationRefresherProvider refresherProvider)
        {
            _configuration = configuration;
            _configurationRefresher = refresherProvider.Refreshers.First();
        }

        [FunctionName("RoundRobinQueueSender")]
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
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : await SendToSB(numOfMessages);

            return new OkObjectResult(responseMessage);
        }

        private async Task<string> SendToSB(int numOfMessages)
        {

            string fullyQualifiedNamespaces = _configuration["SB-Function:RoundRobinQueueSender:SERVICE_BUS_FULLY_QUALIFIED_NAMESPACE"];

            string[] SBnamespaceList= fullyQualifiedNamespaces.Split(";");

            string queueName = _configuration["SB-Function:RoundRobinQueueSender:SERVICE_BUS_QUEUE_NAME"];

            ServiceBusClient[] SBclientList  = new ServiceBusClient[SBnamespaceList.Length];

            try
            {
                for( int i=0; i< SBnamespaceList.Length; i++)
                {
                    ServiceBusClient cl = new ServiceBusClient(SBnamespaceList[i], new DefaultAzureCredential());
                    SBclientList[i]= cl;
                }

                for (int i=0; i < numOfMessages; i++)
                {
                    ServiceBusClient client = (ServiceBusClient) SBclientList[i % SBnamespaceList.Length];

                    ServiceBusSender sender = client.CreateSender(queueName);
                    ServiceBusMessage message = new ServiceBusMessage("Message Id: # " + i);
                    await sender.SendMessageAsync(message);

                }
            }
            catch (Exception e)
            {
                return e.Message + " : " + e.InnerException;
            }

            return "Messages Sent to Service Bus namespaces";

        }
    }
}
