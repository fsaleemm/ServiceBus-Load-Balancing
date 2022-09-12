# Azure ServiceBus Load Balancing
This demonstration show how to load balancing messages across Azure Service Bus namespaces in different regions.The use case is to be able to have active/active downstream message processing components for high avaialbility. The concepts in this demo are based on the [Azure Service Bus Cross Region Federation recommendations](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-federation-overview). Instead of replicating the messages to different regions, this demo routes the messages in a round robin fashion. This demo assumes that the ordering of the incoming messages is not relevant for our use case. 

## Requirements and Assumptions
1. Ordering of the incoming messages is not relevant.
1. The source metadata such as sequence number, enqueued time ans so on are not important for downstream consumers. If you need the metadata then leverage the [utility library provided](https://github.com/Azure-Samples/azure-messaging-replication-dotnet/tree/main/src/Azure.Messaging.Replication).

## Architecture

Below is a sketch of the proposed solution for load balancing messages across Azure Service bus namespaces in different regions.

![](/images/s1.png)

## Deployment Steps
TBD

## Disclaimer
The code and deployment biceps are to be used for demonstration pusposes only. The code is not enterprise ready code and is not intended for production use.

