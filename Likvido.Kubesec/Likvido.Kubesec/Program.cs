using System.CommandLine;
using Likvido.Kubesec;

var rootCommand = new RootCommand("Kubernetes secret configuration helper")
{
    CreatePullCommand(),
    CreatePushCommand(),
    CreateBackupCommand(),
    CreateRestoreCommand()
};

return await rootCommand.Parse(args).InvokeAsync();

static Command CreatePullCommand()
{
    var argumentSecret = new Argument<string>("secret") { Description = "The name of the secret in kubernetes" };
    var optionContext = new Option<string>("--context", "-c") { Description = "The kubectl config context to use" };
    var optionNamespace = new Option<string>("--namespace", "-n") { Description = "The namespace of services in kubernetes" };
    var optionOutput = new Option<string>("--output", "-o") { Description = "The file to write to" };
    var optionUnwrapKey = new Option<string>("--unwrap-key", "-u") { Description = "Unwrap a specific key (so we only output the value of this key)" };
    var optionPortForward = new Option<bool>("--port-forward", "-p") { Description = "Will run port-forwards and replace any Kubernetes service URLs in the config (requires the unwrap-key option as well)" };
    var optionRemoveJsonFields = new Option<List<string>>("--remove-json-field", "-r") { Description = "Will delete the listed fields from the unwrapped json secret value (requires the unwrap-key option as well)" };
    var cmd = new Command("pull", "Pulls secrets from kubernetes to a local file")
    {
        argumentSecret,
        optionContext,
        optionNamespace,
        optionOutput,
        optionUnwrapKey,
        optionPortForward,
        optionRemoveJsonFields
    };

    cmd.SetAction(async (parseResult, cancellationToken) =>
    {
        var secret = parseResult.GetValue(argumentSecret)!;
        var output = parseResult.GetValue(optionOutput);
        var context = parseResult.GetValue(optionContext);
        var @namespace = parseResult.GetValue(optionNamespace);
        var unwrapKeyName = parseResult.GetValue(optionUnwrapKey);
        var configurePortForwarding = parseResult.GetValue(optionPortForward);
        var jsonFieldsToDelete = parseResult.GetValue(optionRemoveJsonFields);

        if (configurePortForwarding && string.IsNullOrWhiteSpace(unwrapKeyName))
        {
            Console.WriteLine("When using the port-forward flag, you also have to specify the unwrap-key option");
            return await Task.FromResult(0);
        }

        if (jsonFieldsToDelete != null && string.IsNullOrWhiteSpace(unwrapKeyName))
        {
            Console.WriteLine("When using the remove-json-fields option, you also have to specify the unwrap-key option");
            return await Task.FromResult(0);
        }

        return await TryCommandAsync(() => PullCommand.Run(secret, configurePortForwarding, output, context, @namespace, unwrapKeyName, jsonFieldsToDelete));
    });

    return cmd;
}

static Command CreatePushCommand()
{
    var argumentFile = new Argument<string>("file") { Description = "The file to read from" };
    var optionContext = new Option<string>("--context", "-c") { Description = "The kubectl config context to use" };
    var optionSecret = new Option<string>("--secret", "-s") { Description = "The name of the secret in kubernetes" };
    var optionNamespace = new Option<string>("--namespace", "-n") { Description = "The namespace of services in kubernetes" };
    var optionSkipPrompts = new Option<bool>("--skip-prompts", "-sp") { Description = "Will not ask for confirmation before pushing secret" };
    var optionAutoCreateMissingNamespace = new Option<bool>("--auto-create-missing-namespace", "-acmn") { Description = "Will automatically create the namespace if it does not exist" };
    var cmd = new Command("push", "Pushes secrets from a local file into kubernetes")
    {
        argumentFile,
        optionContext,
        optionSecret,
        optionNamespace,
        optionSkipPrompts,
        optionAutoCreateMissingNamespace
    };

    cmd.SetAction(async (parseResult, cancellationToken) =>
    {
        var file = parseResult.GetValue(argumentFile)!;
        var context = parseResult.GetValue(optionContext);
        var secret = parseResult.GetValue(optionSecret);
        var @namespace = parseResult.GetValue(optionNamespace);
        var skipPrompts = parseResult.GetValue(optionSkipPrompts);
        var autoCreateMissingNamespace = parseResult.GetValue(optionAutoCreateMissingNamespace);

        return await Task.FromResult(TryCommand(() => PushCommand.Run(file, context, secret, @namespace, skipPrompts, autoCreateMissingNamespace)));
    });

    return cmd;
}

static Command CreateBackupCommand()
{
    var optionContext = new Option<string>("--context", "-c") { Description = "The kubectl config context to use" };
    var optionNamespace = new Option<string>("--namespace", "-n") { Description = "The namespace of services in kubernetes" };
    var optionNamespaceIncludes = new Option<string>("--namespace-includes", "-i") { Description = "The namespace keyword of services in kubernetes" };
    var optionNamespaceRegex = new Option<string>("--namespace-regex", "-rgx") { Description = "The namespace regex to search for services in kubernetes" };
    var optionExcludedNamespaces = new Option<string[]>("--excluded-namespace", "-e") { Description = "A list of namespaces that should not be included in the backup" };
    var cmd = new Command("backup", "Pulls all secrets in the cluster")
    {
        optionContext,
        optionNamespace,
        optionNamespaceIncludes,
        optionNamespaceRegex,
        optionExcludedNamespaces
    };

    cmd.SetAction(async (parseResult, cancellationToken) =>
    {
        var context = parseResult.GetValue(optionContext);
        var @namespace = parseResult.GetValue(optionNamespace);
        var namespaceIncludes = parseResult.GetValue(optionNamespaceIncludes);
        var namespaceRegex = parseResult.GetValue(optionNamespaceRegex);
        var excludedNamespaces = parseResult.GetValue(optionExcludedNamespaces);

        return await Task.FromResult(TryCommand(() => BackupCommand.Run(context, @namespace, namespaceIncludes, namespaceRegex, excludedNamespaces)));
    });

    return cmd;
}

static Command CreateRestoreCommand()
{
    var argumentFolder = new Argument<string>("folder") { Description = "The folder containing all the secret files to push" };
    var optionContext = new Option<string>("--context", "-c") { Description = "The kubectl config context to use" };
    var optionRecursive = new Option<bool>("--recursive", "-r") { Description = "Will recursively push secrets" };
    var optionSkipPrompts = new Option<bool>("--skip-prompts", "-sp") { Description = "Will not ask for confirmation before pushing secrets" };
    var optionAutoCreateMissingNamespaces = new Option<bool>("--auto-create-missing-namespaces", "-acmn") { Description = "Will automatically create missing namespaces when pushing secrets" };
    var cmd = new Command("restore", "Pushes all secrets in a given folder")
    {
        argumentFolder,
        optionContext,
        optionRecursive,
        optionSkipPrompts,
        optionAutoCreateMissingNamespaces
    };

    cmd.SetAction(async (parseResult, cancellationToken) =>
    {
        var folder = parseResult.GetValue(argumentFolder)!;
        var context = parseResult.GetValue(optionContext);
        var recursive = parseResult.GetValue(optionRecursive);
        var skipPrompts = parseResult.GetValue(optionSkipPrompts);
        var autoCreateMissingNamespaces = parseResult.GetValue(optionAutoCreateMissingNamespaces);

        return await Task.FromResult(TryCommand(() => RestoreCommand.Run(folder, context, recursive, skipPrompts, autoCreateMissingNamespaces)));
    });

    return cmd;
}

static int TryCommand(Func<int> command)
{
    try
    {
        return command();
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(exception.Message);

        return 1;
    }
}

static async Task<int> TryCommandAsync(Func<Task<int>> command)
{
    try
    {
        return await command();
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(exception.Message);

        return 1;
    }
}
