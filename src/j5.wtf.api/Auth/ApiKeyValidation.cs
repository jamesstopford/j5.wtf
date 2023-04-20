namespace j5.wtf.api.Auth;

public class ApiKeyValidation
{
    public static Func<HttpContext, Func<Task>, Task> ApiKeyValidationMiddleware(string validApiKey)
    {
        return async (context, next) =>
        {
            if (!HttpMethods.IsGet(context.Request.Method))
            {
                if (!context.Request.Headers.TryGetValue("X-Api-Key", out var apiKey) || apiKey != validApiKey)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid API key.");
                    return;
                }
            }

            await next.Invoke();
        };
    }
}