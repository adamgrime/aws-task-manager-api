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

        var taskId = request.PathParameters != null && request.PathParameters.TryGetValue("id", out var id) ? id : null;

        return (request.HttpMethod, taskId) switch
        {
            ("GET", null) => await ListTasks(),
            ("GET", { }) => await GetTaskById(taskId),
            ("POST", null) => await CreateTask(request),
            ("PUT", { }) => await UpdateTask(request),
            ("DELETE", { }) => await DeleteTask(request),
            _ => new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.MethodNotAllowed, Body = "{\"message\":\"Method not allowed.\"}" },
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

    private async Task<APIGatewayProxyResponse> UpdateTask(APIGatewayProxyRequest request)
    {
        var taskId = request.PathParameters["id"];
        var taskToUpdate = await _context.LoadAsync<TaskItem>(taskId);

        if (taskToUpdate == null)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Body = "{\"message\":\"Task not found.\"}"
            };
        }

        try
        {
            var requestBody = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(request.Body);

            if (requestBody.TryGetValue("title", out JsonElement titleElement))
            {
                taskToUpdate.Title = titleElement.GetString();
            }
            if (requestBody.TryGetValue("isComplete", out JsonElement isCompleteElement))
            {
                taskToUpdate.IsComplete = isCompleteElement.GetBoolean();
            }

            await _context.SaveAsync(taskToUpdate);

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = "{\"message\":\"Task updated successfully.\"}"
            };
        }
        catch
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Body = "{\"message\":\"Invalid request body.\"}"
            };
        }
    }
    private async Task<APIGatewayProxyResponse> DeleteTask(APIGatewayProxyRequest request)
    {
        var taskId = request.PathParameters["id"];
        var taskToDelete = await _context.LoadAsync<TaskItem>(taskId);

        if (taskToDelete == null)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Body = "{\"message\":\"Task not found.\"}"
            };
        }

        await _context.DeleteAsync(taskToDelete);

        return new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.NoContent,
            Body = ""
        };
    }
}