using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.RegistryItems;
using Outlet.Core.Infrastructure.DependencyInjection;
using Outlet.Kernel.Shared;
using Outlet.Kernel.Shared.Mediator;

var services = new ServiceCollection();
services.AddLogging(builder => builder
    .AddConsole()
    .SetMinimumLevel(LogLevel.Warning));
services.AddMediator();
services.AddHandlersFromAssembly(typeof(Outlet.Core.Application.AssemblyReference).Assembly);
services.AddOutletCoreInfrastructure();

await using var provider = services.BuildServiceProvider();

var rootCommand = new RootCommand(
    "Outlet — copy-paste registry for .NET backend infrastructure. " +
    "Generic ports, swappable adapters, code you own.");

// outlet init
var initCommand = new Command("init", "Initialize outlet.json in the current project.");
initCommand.SetAction(async (_, cancellationToken) =>
{
    await using var scope = provider.CreateAsyncScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

    var result = await mediator.ExecuteAsync<InitProjectCommand, OutletConfig>(
        new InitProjectCommand(Environment.CurrentDirectory), cancellationToken);

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
});

// outlet add <item>
var itemArgument = new Argument<string>("item")
{
    Description = "Registry item to copy into the project (e.g. email-smtp).",
};
var addCommand = new Command("add", "Copy a registry item (and its dependencies) into the project.");
addCommand.Arguments.Add(itemArgument);
addCommand.SetAction(async (parseResult, cancellationToken) =>
{
    await using var scope = provider.CreateAsyncScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

    var command = new AddItemCommand(Environment.CurrentDirectory, parseResult.GetValue(itemArgument)!);
    var result = await mediator.ExecuteAsync<AddItemCommand, InstallationReport>(command, cancellationToken);

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
});

// outlet list [--concern <name>]
var concernOption = new Option<string?>("--concern")
{
    Description = "Only show items of this concern (e.g. email).",
};
var listCommand = new Command("list", "List the items available across the configured registries.");
listCommand.Options.Add(concernOption);
listCommand.SetAction(async (parseResult, cancellationToken) =>
{
    await using var scope = provider.CreateAsyncScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

    var query = new ListRegistryItemsQuery(parseResult.GetValue(concernOption));
    var result = await mediator
        .ExecuteAsync<ListRegistryItemsQuery, IReadOnlyList<RegistryItemSummary>>(query, cancellationToken);

    return result.Match(
        onSuccess: items =>
        {
            if (items.Count == 0)
            {
                Console.WriteLine("No registry items available (no registry source configured yet).");
                return 0;
            }

            foreach (var item in items)
            {
                Console.WriteLine($"{item.Name,-30} {item.Concern,-10} {item.Type,-16} {item.FileCount} file(s)");
            }
            return 0;
        },
        onFailure: error =>
        {
            Console.Error.WriteLine($"error: {error}");
            return 1;
        });
});

rootCommand.Subcommands.Add(initCommand);
rootCommand.Subcommands.Add(addCommand);
rootCommand.Subcommands.Add(listCommand);

return await rootCommand.Parse(args).InvokeAsync();
