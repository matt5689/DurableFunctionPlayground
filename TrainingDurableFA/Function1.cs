using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Threading;
using System;

namespace TrainingDurableFA
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            // Replace "hello" with the name of your Durable Activity Function.
            outputs.Add(await context.CallActivityAsync<string>("Function1_Hello", "Tokyo"));
            outputs.Add(await context.CallActivityAsync<string>("Function1_Hello", "Seattle"));
            outputs.Add(await context.CallActivityAsync<string>("Function1_Hello", "London"));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("Function1_Hello")]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("Function1_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("Function1", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("Select")]
        public static async Task Run(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var event1 = context.WaitForExternalEvent<float>("Event1");
            var event2 = context.WaitForExternalEvent<bool>("Event2");
            var event3 = context.WaitForExternalEvent<int>("Event3");

            var winner = await Task.WhenAny(event1, event2, event3);
            if (winner == event1)
            {
                Console.WriteLine("Event1");
            }
            else if (winner == event2)
            {
                Console.WriteLine("Event2");
            }
            else if (winner == event3)
            {
                Console.WriteLine("Event3");
            }
        }

        [FunctionName("ApprovalQueueProcessor")]
        public static async Task Run(
            [QueueTrigger("approval-queue")] string instanceId,
            [DurableClient] IDurableOrchestrationClient client)
        {
            await client.RaiseEventAsync(instanceId, "Approval", true);
        }
    }
}