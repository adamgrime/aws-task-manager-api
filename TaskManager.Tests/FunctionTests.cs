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


}
