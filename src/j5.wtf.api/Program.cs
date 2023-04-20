using System.Text.Json;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Diagnostics;
using j5.wtf.api.Validators;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("AzureTableStorage");
var tableName = configuration["TableName"];
var defaultDestination = configuration["DefaultDestination"];

var app = builder.Build();

var idValidator = new IdValidator();
var tableClient = new TableClient(connectionString, tableName);

// app.UseExceptionHandler(errorApp =>
// {
//     errorApp.Run(async context =>
//     {
//         var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
//         var exception = exceptionHandlerPathFeature?.Error;
//
//         // Log the exception here
//
//         context.Response.StatusCode = StatusCodes.Status500InternalServerError;
//         context.Response.ContentType = "application/json";
//
//         var errorResponse = new
//         {
//             ErrorMessage = "An unexpected error occurred. Please try again later.",
//             // Add more details about the error if needed
//         };
//
//         var errorJson = JsonSerializer.Serialize(errorResponse);
//         await context.Response.WriteAsync(errorJson);
//     });
// });


app.MapGet("/{id?}", async(string? id) =>
{
    if (string.IsNullOrEmpty(id) && app.Environment.IsProduction())
    {
        return Results.Redirect(defaultDestination!);
    }

    if (id == null) return Results.NotFound($"No mapping found for the ID '{id}'");
    
    var validationResult = idValidator.Validate(id);
    if (!validationResult.IsValid)
    {
        var errorMessage = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
        return Results.BadRequest($"Invalid ID format: {errorMessage}");
    }

    var mapping = await tableClient.GetEntityIfExistsAsync<MappingEntity>(id, id);

    return mapping.HasValue ? Results.Redirect(mapping.Value.Destination) : Results.NotFound($"No mapping found for the ID '{id}'");
});

app.MapPost("/shorten/", async (UrlInput input) =>
{
    var shortId = generateShortID(input.DestinationUrl);

    var mappingEntity = new MappingEntity
    {
        PartitionKey = shortId,
        RowKey = shortId,
        Destination = input.DestinationUrl
    };

    await tableClient.AddEntityAsync(mappingEntity);

    return Results.Ok(new { shortId });
});


app.Run();

string generateShortID(string url)
{
    var guid = Guid.NewGuid();
    var base64Guid = Convert.ToBase64String(guid.ToByteArray());
    return base64Guid.Substring(0, 8);
}

public class UrlInput
{
    public string DestinationUrl { get; set; }
}