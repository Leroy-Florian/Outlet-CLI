using System.CommandLine;
using System.Reflection;
using System.Text.Json;
using Outlet.Cli;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Outlet.Core.Application.Cli;
using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.RegistryItems;
using Outlet.Core.Domain.Cli;
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
    "Generic ports, swappable adapters, code you own. " +
    "Exit codes: 0 success · 1 error · 130 cancelled.");

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
var noRestoreOption = new Option<bool>("--no-restore")
{
    Description = "Skip 'dotnet restore' after adding packages (transitive deps are not materialized yet).",
};
var dryRunOption = new Option<bool>("--dry-run")
{
    Description = "Preview the install: print the files and packages that would be written, change nothing.",
};
var addCommand = new Command("add", "Copy a registry item (and its dependencies) into the project.");
addCommand.Arguments.Add(itemArgument);
addCommand.Options.Add(noRestoreOption);
addCommand.Options.Add(dryRunOption);
addCommand.SetAction((parseResult, cancellationToken) => RunAsync(async (mediator, token) =>
{
    var dryRun = parseResult.GetValue(dryRunOption);
    var command = new AddItemCommand(
        Environment.CurrentDirectory,
        parseResult.GetValue(itemArgument)!,
        Restore: !parseResult.GetValue(noRestoreOption),
        DryRun: dryRun);
    var result = await mediator.ExecuteAsync<AddItemCommand, InstallationReport>(command, token);

    return result.Match(
        onSuccess: report =>
        {
            var prefix = report.DryRun ? "would install" : "installed";
            foreach (var name in report.InstalledItems)
                Console.WriteLine($"{prefix} {name}");
            foreach (var file in report.WrittenFiles)
                Console.WriteLine($"  + {file}");
            foreach (var warning in report.Warnings)
                Console.Error.WriteLine($"warning: {warning}");

            if (report.InstalledItems.Count == 0)
                Console.WriteLine("Nothing to install (already up to date).");
            else if (report.DryRun)
                Console.WriteLine("dry run — no changes written. Re-run without --dry-run to apply.");
            else if (report.Restored)
                Console.WriteLine("restored packages (transitive dependencies resolved).");
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
var jsonOption = new Option<bool>("--json")
{
    Description = "Emit the catalogue as JSON (for scripting / CI) instead of the aligned table.",
};
var listCommand = new Command("list", "List the items available across the configured registries.");
listCommand.Options.Add(concernOption);
listCommand.Options.Add(jsonOption);
listCommand.SetAction((parseResult, cancellationToken) => RunAsync(async (mediator, token) =>
{
    var asJson = parseResult.GetValue(jsonOption);
    var query = new ListRegistryItemsQuery(parseResult.GetValue(concernOption));
    var result = await mediator
        .ExecuteAsync<ListRegistryItemsQuery, IReadOnlyList<RegistryItemSummary>>(query, token);

    return result.Match(
        onSuccess: items =>
        {
            if (asJson)
            {
                Console.WriteLine(JsonSerializer.Serialize(items, CliJson.Options));
                return 0;
            }

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

// outlet diff <item>
var diffItemArgument = new Argument<string>("item")
{
    Description = "Installed item to compare with its current registry version.",
};
var diffCommand = new Command("diff", "Show how an installed item differs from its current registry version.");
diffCommand.Arguments.Add(diffItemArgument);
diffCommand.SetAction((parseResult, cancellationToken) => RunAsync(async (mediator, token) =>
{
    var command = new DiffItemCommand(Environment.CurrentDirectory, parseResult.GetValue(diffItemArgument)!);
    var result = await mediator.ExecuteAsync<DiffItemCommand, ItemDiffReport>(command, token);

    return result.Match(
        onSuccess: report =>
        {
            if (!report.HasChanges)
            {
                Console.WriteLine($"{report.ItemName} is up to date.");
                return 0;
            }

            Console.WriteLine($"{report.ItemName}:");
            foreach (var file in report.Files.Where(f => f.Status != nameof(FileChangeStatus.Unchanged)))
                Console.WriteLine($"  {file.Status,-16} {file.Path}");
            return 0;
        },
        onFailure: error =>
        {
            Console.Error.WriteLine($"error: {error}");
            return 1;
        });
}, cancellationToken));

// outlet update <item>
var updateItemArgument = new Argument<string>("item")
{
    Description = "Installed item to update to its current registry version.",
};
var updateCommand = new Command("update", "Update an installed item, preserving local edits (conflicts written as <file>.outlet-new).");
updateCommand.Arguments.Add(updateItemArgument);
updateCommand.SetAction((parseResult, cancellationToken) => RunAsync(async (mediator, token) =>
{
    var command = new UpdateItemCommand(Environment.CurrentDirectory, parseResult.GetValue(updateItemArgument)!);
    var result = await mediator.ExecuteAsync<UpdateItemCommand, UpdateReport>(command, token);

    return result.Match(
        onSuccess: report =>
        {
            foreach (var file in report.Updated)
                Console.WriteLine($"  updated {file}");
            foreach (var conflict in report.Conflicts)
                Console.WriteLine($"  conflict {conflict} (see {conflict}.outlet-new)");
            foreach (var warning in report.Warnings)
                Console.Error.WriteLine($"warning: {warning}");

            if (report.Updated.Count == 0 && report.Conflicts.Count == 0)
                Console.WriteLine($"{report.ItemName} is already up to date.");
            return 0;
        },
        onFailure: error =>
        {
            Console.Error.WriteLine($"error: {error}");
            return 1;
        });
}, cancellationToken));

// outlet self-update
var selfUpdateCommand = new Command(
    "self-update",
    "Update the Outlet CLI itself to the latest published version (runs 'dotnet tool update').");
selfUpdateCommand.SetAction((_, cancellationToken) => RunAsync(async (mediator, token) =>
{
    var result = await mediator.ExecuteAsync<SelfUpdateCliCommand, string>(new SelfUpdateCliCommand(), token);

    return result.Match(
        onSuccess: detail =>
        {
            Console.WriteLine(detail);
            return 0;
        },
        onFailure: error =>
        {
            Console.Error.WriteLine($"error: {error}");
            return 1;
        });
}, cancellationToken));

// outlet publish [--root <dir>] [--out <dir>] [--dry-run]
var publishRootOption = new Option<string>("--root")
{
    Description = "Registry source tree to publish (the directory holding the *.registry.json items).",
    DefaultValueFactory = _ => "registry",
};
var publishOutOption = new Option<string>("--out")
{
    Description = "Directory to write the publishable artifact (index + item files) into.",
    DefaultValueFactory = _ => Path.Combine("dist", "registry"),
};
var publishDryRunOption = new Option<bool>("--dry-run")
{
    Description = "Validate the registry and report what would be published, without writing the artifact.",
};
var publishCommand = new Command(
    "publish",
    "Build the publishable registry artifact (index + files) from a registry source tree — for authoring your own registry.");
publishCommand.Options.Add(publishRootOption);
publishCommand.Options.Add(publishOutOption);
publishCommand.Options.Add(publishDryRunOption);
publishCommand.SetAction((parseResult, cancellationToken) => RunAsync(async (mediator, token) =>
{
    var command = new PublishRegistryCommand(
        parseResult.GetValue(publishRootOption)!,
        parseResult.GetValue(publishOutOption)!,
        DryRun: parseResult.GetValue(publishDryRunOption));
    var result = await mediator.ExecuteAsync<PublishRegistryCommand, RegistryPublicationReport>(command, token);

    return result.Match(
        onSuccess: report =>
        {
            var prefix = report.DryRun ? "would publish" : "published";
            Console.WriteLine($"{prefix} {report.Items.Count} item(s)" +
                (report.DryRun ? "." : $" to '{report.OutputDirectory}'."));
            foreach (var item in report.Items)
                Console.WriteLine($"  - {item.Name} ({item.FileCount} file(s))");
            if (report.DryRun)
                Console.WriteLine("dry run — nothing written. Re-run without --dry-run to write the artifact.");
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
rootCommand.Subcommands.Add(diffCommand);
rootCommand.Subcommands.Add(updateCommand);
rootCommand.Subcommands.Add(selfUpdateCommand);
rootCommand.Subcommands.Add(listCommand);
rootCommand.Subcommands.Add(publishCommand);

// Level 2: fire a throttled, best-effort "newer version available" check in the
// background so it overlaps the command instead of delaying it. We print the notice
// (to stderr, keeping stdout pipe-clean) only if it finishes in a short grace window.
var updateNotice = StartUpdateCheck(provider, args);

var exitCode = await rootCommand.Parse(args).InvokeAsync();

await EmitUpdateNoticeIfReady(updateNotice);
return exitCode;

// Kicks off the background check. Returns the notice text (or null) once known.
// Honours the OUTLET_NO_UPDATE_CHECK opt-out and never throws.
Task<string?> StartUpdateCheck(IServiceProvider serviceProvider, string[] commandArgs)
{
    if (Environment.GetEnvironmentVariable("OUTLET_NO_UPDATE_CHECK") is { Length: > 0 })
        return Task.FromResult<string?>(null);

    // Don't nag during the explicit self-update itself.
    if (commandArgs is ["self-update", ..])
        return Task.FromResult<string?>(null);

    var current = CliVersion.TryParse(Assembly.GetExecutingAssembly().GetName().Version?.ToString());
    if (current is null)
        return Task.FromResult<string?>(null);

    return Task.Run(async () =>
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var result = await mediator.ExecuteAsync<CheckForCliUpdateQuery, CliUpdateStatus>(
                new CheckForCliUpdateQuery(current));

            if (result.IsSuccess && result.Value!.UpdateAvailable)
                return $"A new Outlet version is available ({result.Value.LatestVersion}, you have " +
                       $"{result.Value.CurrentVersion}). Run 'outlet self-update'.";
        }
        catch
        {
            // Best-effort: a failed background check is never the user's problem.
        }

        return null;
    });
}

async Task EmitUpdateNoticeIfReady(Task<string?> notice)
{
    try
    {
        // Most runs are throttled and complete instantly; cap the wait so a slow
        // once-a-day lookup never noticeably delays exit (next run will catch it).
        var finished = await Task.WhenAny(notice, Task.Delay(TimeSpan.FromMilliseconds(800)));
        if (finished == notice && notice.Result is { } message)
            Console.Error.WriteLine(message);
    }
    catch
    {
        // Never let the notice path change the command's outcome.
    }
}
