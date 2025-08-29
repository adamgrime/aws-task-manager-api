using System.Net;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using TaskManager.Core;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace TaskManager.API;

public class Function
{
    private readonly IDynamoDBContext _context;

    /// <summary>
    /// This constructor is used by Lambda to instantiate the function.
    /// </summary>
    public Function()
    {
        IAmazonDynamoDB client = new AmazonDynamoDBClient();
        _context = new DynamoDBContext(client);
    }

    /// <summary>
    /// This constructor is used by unit tests.
    /// </summary>
    public Function(IDynamoDBContext context)
    {
        _context = context;
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        context.Logger.LogLine($"Received API Gateway request with HTTP Method: {request.HttpMethod}");

        var taskId = request.PathParameters != null && request.PathParameters.ContainsKey("id") ? request.PathParameters["id"] : null;

        if (request.HttpMethod == "GET" && taskId == null)
        {
            return await ListTasks();
        }
        else if (request.HttpMethod == "GET" && taskId != null)
        {
            return await GetTaskById(taskId);
        }
        else if (request.HttpMethod == "POST")
        {
            return await CreateTask(request);
        }

        // Default response for unhandled methods
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.MethodNotAllowed,
            Body = "{\"message\":\"Method not allowed.\"}"
        };
    }

    private async Task<APIGatewayProxyResponse> CreateTask(APIGatewayProxyRequest request)
    {
        var requestBody = JsonSerializer.Deserialize<Dictionary<string, string>>(request.Body);

        if (!requestBody.TryGetValue("title", out string title))
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Body = "{\"message\":\"Missing 'title' in request body.\"}"
            };
        }

        var taskItem = new TaskItem
        {
            TaskId = Guid.NewGuid().ToString(),
            Title = title,
            IsComplete = false
        };

        await _context.SaveAsync(taskItem);

        return new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.Created,
            Body = JsonSerializer.Serialize(taskItem),
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            }
        };
    }

    private async Task<APIGatewayProxyResponse> GetTaskById(string taskId)
    {
        var task = await _context.LoadAsync<TaskItem>(taskId);

        if (task == null)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Body = "{\"message\":\"Task not found.\"}"
            };
        }

        return new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.OK,
            Body = JsonSerializer.Serialize(task),
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            }
        };
    }

    private async Task<APIGatewayProxyResponse> ListTasks()
    {
        var scanResult = await _context.ScanAsync<TaskItem>((IEnumerable<ScanCondition>)null).GetRemainingAsync();

        return new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.OK,
            Body = JsonSerializer.Serialize(scanResult),
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            }
        };
    }
}