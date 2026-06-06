using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.RegistryItems;
using Outlet.Core.Infrastructure.DependencyInjection;
using Outlet.Kernel.Shared.Mediator;

var services = new ServiceCollection();
// A CLI speaks through stdout/stderr, not framework log lines — keep the pipeline quiet.
services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.None));
services.AddMediator(options => options.SlowExecutionThreshold = TimeSpan.FromSeconds(5));
services.AddHandlersFromAssembly(typeof(Outlet.Core.Application.AssemblyReference).Assembly);
services.AddOutletCoreInfrastructure();

await using var provider = services.BuildServiceProvider();

// Runs a command body inside a scope, turning any infrastructure fault into a clean
// one-line error + non-zero exit code (never a stack-trace dump).
async Task<int> RunAsync(Func<IMediator, CancellationToken, Task<int>> action, CancellationToken cancellationToken)
{
    try
    {
        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await action(mediator, cancellationToken);
    }
    catch (OperationCanceledException)
    {
        Console.Error.WriteLine("cancelled.");
        return 130;
    }
    catch (HttpRequestException ex)
    {
        Console.Error.WriteLine($"error: could not reach a configured registry ({ex.Message}).");
        return 1;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"error: {ex.Message}");
        return 1;
    }
}

var rootCommand = new RootCommand(
    "Outlet — copy-paste registry for .NET backend infrastructure. " +
    "Generic ports, swappable adapters, code you own.");

// outlet init
var initCommand = new Command("init", "Initialize outlet.json in the current project.");
initCommand.SetAction((_, cancellationToken) => RunAsync(async (mediator, token) =>
{
    var result = await mediator.ExecuteAsync<InitProjectCommand, OutletConfig>(
        new InitProjectCommand(Environment.CurrentDirectory), token);

    return result.Match(
        onSuccess: config =>
        {
            Console.WriteLine(
                $"Initialized outlet.json (target project '{config.Targets.Adapter.Project}', " +
                $"namespace '{config.Targets.Adapter.Namespace}').");
            return 0;
        },
        onFailure: error =>
        {
            Console.Error.WriteLine($"error: {error}");
            return 1;
        });
}, cancellationToken));

// outlet add <item>
var itemArgument = new Argument<string>("item")
{
    Description = "Registry item to copy into the project (e.g. email-smtp).",
};
var addCommand = new Command("add", "Copy a registry item (and its dependencies) into the project.");
addCommand.Arguments.Add(itemArgument);
addCommand.SetAction((parseResult, cancellationToken) => RunAsync(async (mediator, token) =>
{
    var command = new AddItemCommand(Environment.CurrentDirectory, parseResult.GetValue(itemArgument)!);
    var result = await mediator.ExecuteAsync<AddItemCommand, InstallationReport>(command, token);

    return result.Match(
        onSuccess: report =>
        {
            foreach (var name in report.InstalledItems)
                Console.WriteLine($"installed {name}");
            foreach (var file in report.WrittenFiles)
                Console.WriteLine($"  + {file}");
            foreach (var warning in report.Warnings)
                Console.Error.WriteLine($"warning: {warning}");

            if (report.InstalledItems.Count == 0)
                Console.WriteLine("Nothing to install (already up to date).");
            return 0;
        },
        onFailure: error =>
        {
            Console.Error.WriteLine($"error: {error}");
            return 1;
        });
}, cancellationToken));

// outlet list [--concern <name>]
var concernOption = new Option<string?>("--concern")
{
    Description = "Only show items of this concern (e.g. email).",
};
var listCommand = new Command("list", "List the items available across the configured registries.");
listCommand.Options.Add(concernOption);
listCommand.SetAction((parseResult, cancellationToken) => RunAsync(async (mediator, token) =>
{
    var query = new ListRegistryItemsQuery(parseResult.GetValue(concernOption));
    var result = await mediator
        .ExecuteAsync<ListRegistryItemsQuery, IReadOnlyList<RegistryItemSummary>>(query, token);

    return result.Match(
        onSuccess: items =>
        {
            if (items.Count == 0)
            {
                Console.WriteLine("No registry items available (no registry configured, or none reachable).");
                return 0;
            }

            foreach (var item in items)
                Console.WriteLine($"{item.Name,-30} {item.Concern,-10} {item.Type,-16} {item.FileCount} file(s)");
            return 0;
        },
        onFailure: error =>
        {
            Console.Error.WriteLine($"error: {error}");
            return 1;
        });
}, cancellationToken));

// outlet remove <item>
var removeItemArgument = new Argument<string>("item")
{
    Description = "Installed registry item to remove (e.g. email-smtp).",
};
var removeCommand = new Command("remove", "Remove an installed item: delete its files and clean unused NuGet packages.");
removeCommand.Arguments.Add(removeItemArgument);
removeCommand.SetAction((parseResult, cancellationToken) => RunAsync(async (mediator, token) =>
{
    var command = new RemoveItemCommand(Environment.CurrentDirectory, parseResult.GetValue(removeItemArgument)!);
    var result = await mediator.ExecuteAsync<RemoveItemCommand, RemovalReport>(command, token);

    return result.Match(
        onSuccess: report =>
        {
            Console.WriteLine($"removed {report.RemovedItem}");
            foreach (var file in report.DeletedFiles)
                Console.WriteLine($"  - {file}");
            foreach (var package in report.RemovedPackages)
                Console.WriteLine($"  cleaned PackageReference {package}");
            foreach (var warning in report.Warnings)
                Console.Error.WriteLine($"warning: {warning}");
            return 0;
        },
        onFailure: error =>
        {
            Console.Error.WriteLine($"error: {error}");
            return 1;
        });
}, cancellationToken));

rootCommand.Subcommands.Add(initCommand);
rootCommand.Subcommands.Add(addCommand);
rootCommand.Subcommands.Add(removeCommand);
rootCommand.Subcommands.Add(listCommand);

return await rootCommand.Parse(args).InvokeAsync();
