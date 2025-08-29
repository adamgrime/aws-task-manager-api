using Amazon.DynamoDBv2.DataModel;

namespace TaskManager.Core;

[DynamoDBTable("Tasks")]
public class TaskItem
{
    [DynamoDBHashKey]
    public string TaskId { get; set; }
    public string Title { get; set; }
    public bool IsComplete { get; set; }
}

