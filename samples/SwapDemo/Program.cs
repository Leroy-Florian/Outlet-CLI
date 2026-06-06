using Microsoft.Extensions.DependencyInjection;
using Outlet.Registry.Email;

// Outlet swap demo — same code, swappable provider behind the generic IEmailSender port.
// Run:  dotnet run --project samples/SwapDemo -- smtp
//       dotnet run --project samples/SwapDemo -- sendgrid

var provider = args.FirstOrDefault() ?? "smtp";

var services = new ServiceCollection();

// ── THE SWAP IS THIS ONE LINE ───────────────────────────────────────────────
// Switch the provider by changing the single AddXxxEmail(...) registration.
// Everything below (resolving IEmailSender, building and sending the message)
// stays identical.
if (provider == "sendgrid")
{
    services.AddSendGridEmail(options =>
        options.ApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY") ?? "SG.demo-key");
}
else
{
    services.AddSmtpEmail(options =>
    {
        options.Host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "localhost";
        options.Port = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 1025;
        options.UseStartTls = false;
    });
}
// ────────────────────────────────────────────────────────────────────────────

using var serviceProvider = services.BuildServiceProvider();
var emailSender = serviceProvider.GetRequiredService<IEmailSender>();

Console.WriteLine($"Provider     : {provider}");
Console.WriteLine($"Active adapter: {emailSender.GetType().Name}  (behind IEmailSender)");

var message = new EmailMessage
{
    From = new EmailAddress("demo@outlet.dev", "Outlet Demo"),
    To = [new EmailAddress("dev@example.com")],
    Subject = "Hello from Outlet",
    TextBody = "Swapping email providers is a one-line change.",
    HtmlBody = "<p>Swapping email providers is a <strong>one-line</strong> change.</p>",
};

var result = await emailSender.SendAsync(message);

if (result.IsSuccess)
{
    Console.WriteLine($"Sent ✅  (message id: {result.MessageId ?? "n/a"})");
}
else
{
    Console.WriteLine($"Not delivered (expected without a real server/credentials):");
    Console.WriteLine($"  {result.Error}");
    Console.WriteLine("Set SMTP_HOST/SMTP_PORT (e.g. a local smtp4dev) or SENDGRID_API_KEY to deliver for real.");
}

return 0;
