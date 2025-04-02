using System.Diagnostics;
using Elastic.Clients.Elasticsearch;
namespace API.Services
{
    public class ElasticsearchService
    {
        private readonly ElasticsearchClient _client;
        private readonly string _taskIndex;

        public ElasticsearchService(IConfiguration configuration, ElasticsearchClient client)
        {
            _taskIndex = configuration["Elasticsearch:TaskIndex"] ?? "tasks";  // Ensure consistency
            _client = client;
        }

        #region Index Operations
        public async System.Threading.Tasks.Task CreateIndexAsync()
        {
            var existsResponse = await _client.Indices.ExistsAsync(_taskIndex);
            if (!existsResponse.Exists)
            {
                var createResponse = await _client.Indices.CreateAsync(_taskIndex, c => c
                    .Settings(s => s.NumberOfShards(1).NumberOfReplicas(1))
                    .Mappings(m => m
                        .Properties<Repositories.Models.Task>(p => p
                            .Text(t => t.Title, t => t.Analyzer("standard"))
                            .Text(t => t.Description ?? string.Empty, t => t.Analyzer("standard"))
                            .Date(d => d.StartDate)
                            .Date(d => d.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow))
                            .IntegerNumber(n => n.EstimatedDays ?? 0)  // Default to 0 if null
                            .Keyword(k => (object?)k.TaskId ?? string.Empty)
                            .Keyword(k => k.UserId)
                        )
                    )
                );

                if (!createResponse.ApiCallDetails.HasSuccessfulStatusCode)
                {
                    // Console.WriteLine($"Failed to create index: {createResponse.DebugInformation}");
                }
                else
                {
                    Console.WriteLine($"Index '{_taskIndex}' created successfully.");
                }
            }
            else
            {
                Console.WriteLine($"Index '{_taskIndex}' already exists.");
            }
        }

        #endregion

        #region Document Operations
        public async System.Threading.Tasks.Task IndexTaskAsync(Repositories.Models.Task task)
        {
            int successCount = 0;
            var response = await _client.IndexAsync(task, idx => idx.Index(_taskIndex));
            if (!response.IsValidResponse)
            {
                Console.WriteLine($"❌ Error indexing task.");
                // successCount++;
            }
            if (successCount > 0)
            {
                Console.WriteLine($"✅ {successCount} Tasks indexed successfully in Elasticsearch.");
            }

        }
        #endregion
        #region Search Method
        public async Task<List<Repositories.Models.Task>> SearchTasksAsync(string searchTerm)
        {
            var response = await _client.SearchAsync<Repositories.Models.Task>(s => s
                .Index(_taskIndex)
                .Query(q => q
                    .Bool(b => b
                        .Should(
                            m => m.Match(mq => mq.Field(f => f.Title).Query(searchTerm)),
                            m => m.Match(mq => mq.Field(f => f.Description).Query(searchTerm)),
                            m => m.Match(mq => mq.Field(f => f.Status).Query(searchTerm))
                        )
                    )
                )
            );

            if (!response.IsValidResponse)
            {
                Console.WriteLine($"❌ Elasticsearch query failed: {response.DebugInformation}");
                return new List<Repositories.Models.Task>();
            }

            if (response.Documents == null || !response.Documents.Any())
            {
                Console.WriteLine("Elasticsearch query returned no results.");
                return new List<Repositories.Models.Task>();
            }

            return response.Documents.ToList();
        }
        #endregion
    }
}