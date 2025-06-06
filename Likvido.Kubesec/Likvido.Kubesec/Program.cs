﻿using System.CommandLine;
using Likvido.Kubesec;

var rootCommand = new RootCommand("Kubernetes secret configuration helper")
{
    CreatePullCommand(),
    CreatePushCommand(),
    CreateBackupCommand(),
    CreateRestoreCommand()
};

return await rootCommand.InvokeAsync(args);

static Command CreatePullCommand()
{
    var argumentSecret = new Argument<string>("secret", "The name of the secret in kubernetes");
    var optionContext = new Option<string>(new[] { "--context", "-c" }, "The kubectl config context to use");
    var optionNamespace =
        new Option<string>(new[] { "--namespace", "-n" }, "The namespace of services in kubernetes");
    var optionOutput = new Option<string>(new[] { "--output", "-o" }, "The file to write to");
    var optionUnwrapKey = new Option<string>(new[] { "--unwrap-key", "-u" }, "Unwrap a specific key (so we only output the value of this key)");
    var optionPortForward = new Option<bool>(new[] { "--port-forward", "-p" }, "Will run port-forwards and replace any Kubernetes service URLs in the config (requires the unwrap-key option as well)");
    var optionRemoveJsonFields = new Option<List<string>>(new[] { "--remove-json-field", "-r" }, "Will delete the listed fields from the unwrapped json secret value (requires the unwrap-key option as well)");
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

    cmd.SetHandler(
        (string secret, string? output, string? context, string? @namespace, string? unwrapKeyName, bool configurePortForwarding, List<string>? jsonFieldsToDelete) =>
        {
            if (configurePortForwarding && string.IsNullOrWhiteSpace(unwrapKeyName))
            {
                Console.WriteLine("When using the port-forward flag, you also have to specify the unwrap-key option");
                return Task.FromResult(0);
            }

            if (jsonFieldsToDelete != null && string.IsNullOrWhiteSpace(unwrapKeyName))
            {
                Console.WriteLine("When using the remove-json-fields option, you also have to specify the unwrap-key option");
                return Task.FromResult(0);
            }

            return TryCommandAsync(() => PullCommand.Run(secret, configurePortForwarding, output, context, @namespace, unwrapKeyName, jsonFieldsToDelete));
        },
        argumentSecret, optionOutput, optionContext, optionNamespace, optionUnwrapKey, optionPortForward, optionRemoveJsonFields);

    return cmd;
}

static Command CreatePushCommand()
{
    var argumentFile = new Argument<string>("file", "The file to read from");
    var optionContext = new Option<string>(new[] { "--context", "-c" }, "The kubectl config context to use");
    var optionSecret = new Option<string>(new[] { "--secret", "-s" }, "The name of the secret in kubernetes");
    var optionNamespace = new Option<string>(new[] { "--namespace", "-n" }, "The namespace of services in kubernetes");
    var optionSkipPrompts = new Option<bool>(new[] { "--skip-prompts", "-sp" }, "Will not ask for confirmation before pushing secret");
    var optionAutoCreateMissingNamespace = new Option<bool>(new[] { "--auto-create-missing-namespace", "-acmn" }, "Will automatically create the namespace if it does not exist");
    var cmd = new Command("push", "Pushes secrets from a local file into kubernetes")
    {
        argumentFile,
        optionContext,
        optionSecret,
        optionNamespace,
        optionSkipPrompts,
        optionAutoCreateMissingNamespace
    };

    cmd.SetHandler(
        (string file, string? context, string? secret, string? @namespace, bool skipPrompts, bool autoCreateMissingNamespace) =>
        {
            return Task.FromResult(TryCommand(() => PushCommand.Run(file, context, secret, @namespace, skipPrompts, autoCreateMissingNamespace)));
        },
        argumentFile, optionContext, optionSecret, optionNamespace, optionSkipPrompts, optionAutoCreateMissingNamespace);

    return cmd;
}

static Command CreateBackupCommand()
{
    var optionContext = new Option<string>(new[] { "--context", "-c" }, "The kubectl config context to use");
    var optionNamespace =
        new Option<string>(new[] { "--namespace", "-n" }, "The namespace of services in kubernetes");
    var optionNamespaceIncludes = new Option<string>(new[] { "--namespace-includes", "-i" },
        "The namespace keyword of services in kubernetes");
    var optionNamespaceRegex = new Option<string>(new[] { "--namespace-regex", "-rgx" },
        "The namespace regex to search for services in kubernetes");
    var optionExcludedNamespaces = new Option<string[]>(new[] { "--excluded-namespace", "-e" },
        "A list of namespaces that should not be included in the backup");
    var cmd = new Command("backup", "Pulls all secrets in the cluster")
    {
        optionContext,
        optionNamespace,
        optionNamespaceIncludes,
        optionNamespaceRegex,
        optionExcludedNamespaces
    };

    cmd.SetHandler(
        (string? context, string? @namespace, string? namespaceIncludes, string? namespaceRegex, string[]? excludedNamespaces) =>
        {
            return Task.FromResult(TryCommand(() =>
                BackupCommand.Run(context, @namespace, namespaceIncludes, namespaceRegex, excludedNamespaces)));
        },
        optionContext, optionNamespace, optionNamespaceIncludes, optionNamespaceRegex, optionExcludedNamespaces);

    return cmd;
}

static Command CreateRestoreCommand()
{
    var argumentFolder = new Argument<string>("folder", "The folder containing all the secret files to push");
    var optionContext = new Option<string>(new[] { "--context", "-c" }, "The kubectl config context to use");
    var optionRecursive = new Option<bool>(new[] { "--recursive", "-r" }, "Will recursively push secrets");
    var optionSkipPrompts = new Option<bool>(new[] { "--skip-prompts", "-sp" }, "Will not ask for confirmation before pushing secrets");
    var optionAutoCreateMissingNamespaces = new Option<bool>(new[] { "--auto-create-missing-namespaces", "-acmn" }, "Will automatically create missing namespaces when pushing secrets");
    var cmd = new Command("restore", "Pushes all secrets in a given folder")
    {
        argumentFolder,
        optionContext,
        optionRecursive,
        optionSkipPrompts,
        optionAutoCreateMissingNamespaces
    };

    cmd.SetHandler(
        (string folder, string? context, bool recursive, bool skipPrompts, bool autoCreateMissingNamespaces) =>
        {
            return Task.FromResult(TryCommand(() => RestoreCommand.Run(folder, context, recursive, skipPrompts, autoCreateMissingNamespaces)));
        },
        argumentFolder, optionContext, optionRecursive, optionSkipPrompts, optionAutoCreateMissingNamespaces);

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
