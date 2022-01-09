using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
//using System.Threading.Tasks;
using System.Threading;
using System;

namespace TrainingDurableFA
{
    public static class Function1
    {
        //[FunctionName("Function1")]
        //public static async Task<bool> RunOrchestrator(
        //    [OrchestrationTrigger] IDurableOrchestrationContext context)
        //{
        //    TimeSpan timeout = TimeSpan.FromSeconds(20);
        //    DateTime deadline = context.CurrentUtcDateTime.Add(timeout);

        //    using (var cts = new CancellationTokenSource())
        //    {
        //        Task activityTask = context.CallActivityAsync("Function1_Hello", "Tokyo");
        //        Task timeoutTask = context.CreateTimer(deadline, cts.Token);

        //        Task winner = await Task.WhenAny(activityTask, timeoutTask);
        //        if (winner == activityTask)
        //        {
        //            // success case
        //            cts.Cancel();
        //            return true;
        //        }
        //        else
        //        {
        //            // timeout case
        //            return false;
        //        }
        //    }

        //    //var outputs = new List<string>();

        //    //DateTime deadline = context.CurrentUtcDateTime.Add(TimeSpan.FromSeconds(10));
        //    //await context.CreateTimer(deadline, CancellationToken.None);
        //    //await context.CallActivityAsync("Function1_Hello", "Tokyo");

        //    //await context.CreateTimer(context.CurrentUtcDateTime.Add(TimeSpan.FromSeconds(10)), CancellationToken.None);

        //    //outputs.Add(await context.CallActivityAsync<string>("Function1_Hello", "Tokyo"));

        //    //return outputs;
        //}

        [FunctionName("Function1_Hello")]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");

            System.Threading.Thread.Sleep(10000);

            return $"Hello {name}!";
        }

        [FunctionName("Function1_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
             [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
             [DurableClient(ConnectionName = "orchclieconn", ExternalClient = true, TaskHub = "LogTestHubName")] IDurableOrchestrationClient starter,
             ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("Function1", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            var client = new HttpClient();

            var msg = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new System.Uri(@"http://localhost:7071/api/Function2_HttpStart")
            };

            HttpResponseMessage response = new HttpResponseMessage();

            await Task.Factory.StartNew(async () =>
            {
                response = await client.SendAsync(msg);
            });

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("Function2_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart2(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("Function", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            log.LogInformation("Calling second http starter");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("Function")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            Thread.Sleep(1000);
            Console.WriteLine("DONE!");
        }
    }
}