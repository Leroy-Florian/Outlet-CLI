using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Outlet.Registry.Email;

// Outlet playground API — executes registry adapters through their generic port,
// exactly like a real app would (AddXxx → resolve IEmailSender → send).

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

// The catalogue the UI renders: concerns, their port, and each adapter's option
// schema (reflected from the Options class — secrets flagged for password fields).
var catalog = new
{
    concerns = new object[]
    {
        new
        {
            id = "email",
            name = "Email",
            port = "IEmailSender",
            enabled = true,
            adapters = new object[]
            {
                new { id = "smtp", name = "SMTP (MailKit)", registration = "AddSmtpEmail", options = OptionsSchema(typeof(SmtpEmailOptions)) },
                new { id = "sendgrid", name = "SendGrid", registration = "AddSendGridEmail", options = OptionsSchema(typeof(SendGridEmailOptions)) },
            },
        },
        Disabled("cache", "Cache", "ICache"),
        Disabled("resilience", "Resilience", "IResiliencePipeline"),
        Disabled("storage", "Storage", "IBlobStore"),
    },
};

app.MapGet("/api/catalog", () => Results.Ok(catalog));

app.MapPost("/api/email/send", async (SendRequest request) =>
{
    var services = new ServiceCollection();

    // THE SWAP: the only line that differs between providers.
    switch (request.Adapter)
    {
        case "smtp":
            services.AddSmtpEmail(options => Populate(options, request.Options));
            break;
        case "sendgrid":
            services.AddSendGridEmail(options => Populate(options, request.Options));
            break;
        default:
            return Results.BadRequest(new { error = $"Unknown email adapter '{request.Adapter}'." });
    }

    using var provider = services.BuildServiceProvider();
    var sender = provider.GetRequiredService<IEmailSender>();

    var message = new EmailMessage
    {
        From = new EmailAddress(request.Message.From),
        To = [.. request.Message.To.Select(address => new EmailAddress(address))],
        Subject = request.Message.Subject,
        TextBody = request.Message.Text,
        HtmlBody = request.Message.Html,
    };

    var stopwatch = Stopwatch.StartNew();
    var result = await sender.SendAsync(message);
    stopwatch.Stop();

    return Results.Ok(new
    {
        success = result.IsSuccess,
        messageId = result.MessageId,
        error = result.Error,
        elapsedMs = stopwatch.ElapsedMilliseconds,
        adapterType = sender.GetType().Name,
        port = "IEmailSender",
    });
});

app.Run();

static object Disabled(string id, string name, string port)
    => new { id, name, port, enabled = false, adapters = Array.Empty<object>() };

static List<OptionField> OptionsSchema(Type optionsType) =>
[
    .. optionsType
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(property => property.CanWrite)
        .Select(property => new OptionField(
            Camel(property.Name),
            SchemaType(property.PropertyType),
            IsSecret(property.Name))),
];

static string SchemaType(Type type)
{
    var underlying = Nullable.GetUnderlyingType(type) ?? type;
    if (underlying == typeof(bool)) return "bool";
    if (underlying == typeof(int) || underlying == typeof(long)) return "int";
    return "string";
}

static bool IsSecret(string name)
{
    string[] markers = ["password", "apikey", "secret", "token", "key"];
    return markers.Any(marker => name.Contains(marker, StringComparison.OrdinalIgnoreCase));
}

static string Camel(string name) => name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];

static void Populate(object target, Dictionary<string, JsonElement> values)
{
    foreach (var property in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
    {
        if (!property.CanWrite)
            continue;

        var match = values.FirstOrDefault(pair => string.Equals(pair.Key, property.Name, StringComparison.OrdinalIgnoreCase));
        if (match.Key is null)
            continue;

        var converted = Coerce(match.Value, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
        if (converted is not null)
            property.SetValue(target, converted);
    }
}

static object? Coerce(JsonElement element, Type targetType)
{
    if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        return null;

    if (targetType == typeof(bool))
        return element.ValueKind == JsonValueKind.String ? bool.TryParse(element.GetString(), out var b) && b : element.GetBoolean();

    if (targetType == typeof(int))
        return element.ValueKind == JsonValueKind.Number ? element.GetInt32() : int.TryParse(element.GetString(), out var i) ? i : 0;

    return element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
}

internal sealed record OptionField(string Name, string Type, bool Secret);

internal sealed record SendRequest(string Adapter, Dictionary<string, JsonElement> Options, MessageDto Message);

internal sealed record MessageDto(string From, string[] To, string Subject, string? Text, string? Html);
