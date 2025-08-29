using System.Text.Json;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Moq;
using TaskManager.API;
using TaskManager.Core;

namespace TaskManager.Tests;

public class FunctionTests
{
    [Fact]
    public async Task GivenValidRequest_WhenCreateTaskIsCalled_ThenItSavesToDynamoDBAndReturnsCreatedResponse()
    {
        var mockContext = new Mock<IDynamoDBContext>();
        var mockLambdaContext = new Mock<ILambdaContext>();
        var mockLogger = new Mock<ILambdaLogger>();

        mockLambdaContext.Setup(c => c.Logger).Returns(mockLogger.Object);

        var function = new Function(mockContext.Object);

        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "POST",
            Body = JsonSerializer.Serialize(new { title = "Test Task" })
        };

        var response = await function.FunctionHandler(request, mockLambdaContext.Object);

        Assert.Equal(201, response.StatusCode);
        mockContext.Verify(x => x.SaveAsync(It.IsAny<TaskItem>(), default), Times.Once);
    }

    [Fact]
    public async Task GivenInvalidPostRequest_WhenTitleMissing_ThenReturnsBadRequest()
    {
        var mockContext = new Mock<IDynamoDBContext>();
        var mockLambdaContext = new Mock<ILambdaContext>();
        mockLambdaContext.Setup(c => c.Logger).Returns(Mock.Of<ILambdaLogger>());
        var function = new Function(mockContext.Object);

        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "POST",
            Body = JsonSerializer.Serialize(new { notTitle = "oops" })
        };

        var response = await function.FunctionHandler(request, mockLambdaContext.Object);

        Assert.Equal(400, response.StatusCode);
        mockContext.Verify(x => x.SaveAsync(It.IsAny<TaskItem>(), default), Times.Never);
    }

    [Fact]
    public async Task GivenUnsupportedMethod_WhenCalled_ThenReturnsMethodNotAllowed()
    {
        var mockContext = new Mock<IDynamoDBContext>();
        var mockLambdaContext = new Mock<ILambdaContext>();
        mockLambdaContext.Setup(c => c.Logger).Returns(Mock.Of<ILambdaLogger>());

        var function = new Function(mockContext.Object);

        var request = new APIGatewayProxyRequest { HttpMethod = "PUT" };

        var response = await function.FunctionHandler(request, mockLambdaContext.Object);

        Assert.Equal(405, response.StatusCode);
    }

    [Fact]
    public async Task GivenValidPostRequest_WhenCalled_ThenSavesTaskWithCorrectProperties()
    {
        var mockContext = new Mock<IDynamoDBContext>();
        var mockLambdaContext = new Mock<ILambdaContext>();
        mockLambdaContext.Setup(c => c.Logger).Returns(Mock.Of<ILambdaLogger>());

        TaskItem? savedTask = null;
        mockContext.Setup(c => c.SaveAsync(It.IsAny<TaskItem>(), default))
            .Callback((TaskItem item, CancellationToken _) => savedTask = item);

        var function = new Function(mockContext.Object);
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "POST",
            Body = JsonSerializer.Serialize(new { title = "New Task" })
        };

        var response = await function.FunctionHandler(request, mockLambdaContext.Object);

        Assert.Equal(201, response.StatusCode);
        Assert.NotNull(savedTask);
        Assert.Equal("New Task", savedTask.Title);
        Assert.False(savedTask.IsComplete);
        Assert.False(string.IsNullOrEmpty(savedTask.TaskId));
    }

    [Fact]
    public async Task GivenValidGetRequestWithId_WhenGetTaskByIdIsCalled_ThenItReturnsASingleTask()
    {
        var mockContext = new Mock<IDynamoDBContext>();
        var mockLambdaContext = new Mock<ILambdaContext>(); 
        mockLambdaContext.Setup(c => c.Logger).Returns(Mock.Of<ILambdaLogger>());

        var taskId = "123-456";
        var singleTask = new TaskItem { TaskId = taskId, Title = "Single Task", IsComplete = false };

        mockContext.Setup(c => c.LoadAsync<TaskItem>(taskId, default))
            .ReturnsAsync(singleTask);

        var function = new Function(mockContext.Object);

        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "GET",
            PathParameters = new Dictionary<string, string>
            {
                { "id", taskId }
            }
        };

        var response = await function.FunctionHandler(request, mockLambdaContext.Object);

        Assert.Equal(200, response.StatusCode);
        Assert.Contains(JsonSerializer.Serialize(singleTask), response.Body);
        mockContext.Verify(c => c.LoadAsync<TaskItem>(taskId, default), Times.Once);
    }

    [Fact]
    public async Task GivenValidPutRequest_WhenUpdateTaskIsCalled_ThenItUpdatesTheTaskAndReturnsOkResponse()
    {
        // Given an existing task in the database
        var mockContext = new Mock<IDynamoDBContext>();
        var mockLambdaContext = new Mock<ILambdaContext>();
        mockLambdaContext.Setup(c => c.Logger).Returns(Mock.Of<ILambdaLogger>());

        var taskId = "existing-task-id";
        var existingTask = new TaskItem { TaskId = taskId, Title = "Old Title", IsComplete = false };

        mockContext.Setup(c => c.LoadAsync<TaskItem>(taskId, default))
            .ReturnsAsync(existingTask);

        var function = new Function(mockContext.Object);

        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "PUT",
            PathParameters = new Dictionary<string, string> { { "id", taskId } },
            Body = JsonSerializer.Serialize(new { title = "New Title", isComplete = true })
        };

        var response = await function.FunctionHandler(request, mockLambdaContext.Object);

        Assert.Equal(200, response.StatusCode);
        mockContext.Verify(c => c.SaveAsync(It.Is<TaskItem>(
            t => t.TaskId == taskId && t.Title == "New Title" && t.IsComplete == true), default), Times.Once);
    }


}
