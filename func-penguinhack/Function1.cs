using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using TableAttribute = Microsoft.Azure.WebJobs.TableAttribute;

namespace func_penguinhack
{

    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> log)
        {
            _logger = log;
        }

        [FunctionName("Function1")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }

        [OpenApiOperation(operationId: "Run", tags: new[] { "Todo" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(TodoPayload), Description = "The **Json** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        [FunctionName("TodoPost")]
        public async Task<IActionResult> TodoPostRun(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [Table("Todos", Connection = "MyStorage")] TableClient tableClient,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            TodoPayload payload = TodoPayload.ConvertTo(requestBody);
            Console.WriteLine(payload);

            string key = payload.UserId;
            Console.WriteLine(key);
            AsyncPageable<TodoEntity> queryResults = tableClient.QueryAsync<TodoEntity>(filter: $"PartitionKey eq '{key}'");
            Console.WriteLine(queryResults);
            var todos = new List<TodoEntity>();
            await foreach (TodoEntity entity in queryResults)
            {
                log.LogInformation($"{entity.PartitionKey}\t{entity.RowKey}\t{entity.Content}\t");
                todos.Add(entity);
            }

            var todoEntity = new TodoEntity()
            {
                PartitionKey = payload.UserId,
                RowKey = payload.UserId + "-" + $"{todos.Count}",
                UserId = payload.UserId,
                Difficulty = payload.Difficulty,
                Motivation = payload.Motivation,
                Category = payload.Category,
                Display = payload.Display,
                Content = payload.Content,
                TaskNumber = todos.Count,
            };

            await tableClient.AddEntityAsync(todoEntity);
            return new OkObjectResult(todoEntity);
        }

        [OpenApiOperation(operationId: "Run", tags: new[] { "Todo" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "userId", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **userId** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        [FunctionName("TodoGet")]
        public async Task<IActionResult> TodoGetRun(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            [Table("Todos", Connection = "MyStorage")] TableClient tableClient,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string key = req.Query["userId"];
            Console.WriteLine(key);
            AsyncPageable<TodoEntity> queryResults = tableClient.QueryAsync<TodoEntity>(filter: $"PartitionKey eq '{key}'");
            Console.WriteLine(queryResults);
            var todos = new List<TodoEntity>();
            await foreach (TodoEntity entity in queryResults)
            {
                log.LogInformation($"{entity.PartitionKey}\t{entity.RowKey}\t{entity.Content}\t");
                todos.Add(entity);
            }

            var responseData = new Todo()
            {
                Todos = todos
            };

            return new OkObjectResult(responseData);
        }

        [OpenApiOperation(operationId: "Run", tags: new[] { "Todo" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(TodoPayload), Description = "The **Json** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        [FunctionName("TodoPut")]
        public async Task<IActionResult> TodoPutRun(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest req,
            [Table("Todos", Connection = "MyStorage")] TableClient tableClient,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            TodoPayload payload = TodoPayload.ConvertTo(requestBody);

            string words = payload.UserId + "-" + $"{payload.TaskNumber}";
            TodoEntity entity = tableClient.GetEntity<TodoEntity>(payload.UserId, words);
            
            entity.Display = payload.Display;

            await tableClient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace);
            return new OkObjectResult(entity);
        }

        [OpenApiOperation(operationId: "Run", tags: new[] { "Todo" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(TodoPayload), Description = "The **Json** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        [FunctionName("TodoDelete")]
        public static async Task<IActionResult> TodoDeleteRun(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req,
            [Table("Todos", Connection = "MyStorage")] TableClient tableClient,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            TodoPayload payload = TodoPayload.ConvertTo(requestBody);

            string words = payload.UserId + "-" + $"{payload.TaskNumber}";
            await tableClient.DeleteEntityAsync(payload.UserId, words, ETag.All);
            return new OkObjectResult(payload);
        }
    }
}