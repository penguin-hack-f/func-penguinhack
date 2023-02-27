using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Resolvers;
using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using Newtonsoft.Json.Serialization;

namespace func_penguinhack
{
    [OpenApiExample(typeof(TodoPayloadExample))]
    public class TodoPayload
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; }
        
        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; }

        [JsonPropertyName("motivation")]
        public string Motivation { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("display")]
        public bool Display { get; set; }

        [JsonPropertyName("task")]
        public string Content { get; set; }

        [JsonPropertyName("taskNumber")]
        public int TaskNumber { get; set; }

        public static TodoPayload ConvertTo(string value) => JsonSerializer.Deserialize<TodoPayload>(value);

        public override string ToString()
        {
            var options = new JsonSerializerOptions { WriteIndented = false };
            return JsonSerializer.Serialize(this, options);
        }
    }

    public class TodoPayloadExample : OpenApiExample<TodoPayload>
    {
        public override IOpenApiExample<TodoPayload> Build(NamingStrategy namingStrategy = null)
        {
            this.Examples.Add(
                 OpenApiExampleResolver.Resolve(
                     "BookRequestExample",
                     new TodoPayload()
                     {
                         UserId = "123",
                         Difficulty = "Easy",
                         Motivation = "Fun",
                         Category = "Fun",
                         Display = true,
                         Content = "Do something fun",
                         TaskNumber = 0
                     },
                     namingStrategy
                 ));
            return this;
        }
    }
}