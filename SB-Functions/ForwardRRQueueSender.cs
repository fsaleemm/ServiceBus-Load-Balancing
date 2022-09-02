using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

using Azure.Messaging.ServiceBus;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using System.Linq;
using Microsoft.Extensions.Azure;
using System.Collections;
using System.Collections.Generic;

namespace SB_Functions
{
    public class ForwardRRQueueSender
    {
        private readonly IConfiguration _configuration;
        private readonly IConfigurationRefresher _configurationRefresher;

        public ForwardRRQueueSender(IConfiguration configuration, IConfigurationRefresherProvider refresherProvider)
        {
            _configuration = configuration;
            _configurationRefresher = refresherProvider.Refreshers.First();
        }

        [FunctionName("ForwardRRQueueSender")]
        public void Run([ServiceBusTrigger("sourcemessages", Connection = "sourceSB")]string[] Messages, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processing messages");

            _configurationRefresher.TryRefreshAsync();

            string resp = SendToSB(Messages);


            log.LogInformation(resp);
        }

        private string SendToSB(string[] Messages)
        {

            string fullyQualifiedNamespaces = _configuration["SB-Function:ForwardRRQueueSender:SERVICE_BUS_FULLY_QUALIFIED_NAMESPACE"];

            string[] SBnamespaces = fullyQualifiedNamespaces.Split(";");

            string queueName = _configuration["SB-Function:ForwardRRQueueSender:SERVICE_BUS_QUEUE_NAME"];

            ServiceBusSender[] SBsenderClients = new ServiceBusSender[SBnamespaces.Length];



            try
            {
                for (int i = 0; i < SBnamespaces.Length; i++)
                {
                    ServiceBusClient clientFactory = new ServiceBusClient(SBnamespaces[i], new DefaultAzureCredential(),
                        new ServiceBusClientOptions
                        {
                            RetryOptions = new ServiceBusRetryOptions
                            {
                                TryTimeout = TimeSpan.FromSeconds(60),
                                MaxRetries = 3,
                                Delay = TimeSpan.FromSeconds(.8),
                                Mode = ServiceBusRetryMode.Exponential
                            }
                        });

                    SBsenderClients[i] = clientFactory.CreateSender(queueName);
                }

                for (int i = 0; i < Messages.Length; i++)
                {
                    ServiceBusSender sender = SBsenderClients[i % SBsenderClients.Length];

                    ServiceBusMessage message = new ServiceBusMessage(Messages[i]);

                    sender.SendMessageAsync(message);

                }
            }
            catch (Exception e)
            {
                return e.Message + " : " + e.InnerException;
            }

            return "Messages Sent to Service Bus namespaces " + String.Join(", ", SBnamespaces);

        }
    }
}
