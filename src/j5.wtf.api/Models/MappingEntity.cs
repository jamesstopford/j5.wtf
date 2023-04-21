using Azure;
using Azure.Data.Tables;

public class MappingEntity : ITableEntity
{
    public string? Destination { get; set; }
    public string? PartitionKey { get; set; }
    public string? RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}