using System.Text.Json;
using Azure.Data.Tables;
using j5.wtf.api.Auth;
using j5.wtf.api.Models;
using j5.wtf.api.Validators;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("AzureTableStorage");
var tableName = configuration["TableName"];
var defaultDestination = configuration["DefaultDestination"];
var allowedApiKey = configuration["AllowedApiKey"];

var app = builder.Build();

var idValidator = new IdValidator();
var destinationUrlValidator = new DestinationUrlValidator();
var tableClient = new TableClient(connectionString, tableName);

app.Use(ApiKeyValidation.ApiKeyValidationMiddleware(allowedApiKey!));

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            ErrorMessage = "An unexpected error occurred. Please try again later."
        };

        var errorJson = JsonSerializer.Serialize(errorResponse);
        await context.Response.WriteAsync(errorJson);
    });
});


app.MapGet("/{id?}", async (string? id) =>
{
    if (string.IsNullOrEmpty(id) && app.Environment.IsProduction()) return Results.Redirect(defaultDestination!);

    if (id == null) return Results.NotFound($"No mapping found for the ID '{id}'");

    var validationResult = idValidator.Validate(id);
    if (!validationResult.IsValid)
    {
        var errorMessage = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
        return Results.BadRequest($"Invalid ID format: {errorMessage}");
    }

    var mapping = await tableClient.GetEntityIfExistsAsync<MappingEntity>(id, id);

    return mapping.HasValue
        ? Results.Redirect(mapping.Value.Destination!)
        : Results.NotFound($"No mapping found for the ID '{id}'");
});

app.MapPost("/shorten/", async (UrlInput input) =>
{
    string id;

    if (string.IsNullOrEmpty(input.Slug))
    {
        id = GenerateShortId();
    }
    else
    {
        id = input.Slug;
        var idValidationResult = idValidator.Validate(id);
        if (!idValidationResult.IsValid)
        {
            var errorMessage = string.Join(", ", idValidationResult.Errors.Select(e => e.ErrorMessage));
            return Results.BadRequest($"Invalid ID format: {errorMessage}");
        }
    }

    var destinationUrlValidatorResult = destinationUrlValidator.Validate(input);
    if (!destinationUrlValidatorResult.IsValid)
    {
        var errorMessage = string.Join(", ", destinationUrlValidatorResult.Errors.Select(e => e.ErrorMessage));
        return Results.BadRequest($"Invalid destination URL format: {errorMessage}. Expected: https://google.com");
    }

    var mappingEntity = new MappingEntity
    {
        PartitionKey = id,
        RowKey = id,
        Destination = input.DestinationUrl
    };
    var mapping = await tableClient.GetEntityIfExistsAsync<MappingEntity>(id, id);
    if (mapping.HasValue) return Results.Conflict("Slug conflict.");
    await tableClient.AddEntityAsync(mappingEntity);

    return Results.Ok(new { shortId = id });
});


app.Run();

string GenerateShortId()
{
    var guid = Guid.NewGuid();
    var base64Guid = Convert.ToBase64String(guid.ToByteArray());
    return base64Guid[..8];
}