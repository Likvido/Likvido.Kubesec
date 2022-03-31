using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Rendering;

namespace Likvido.Kubesec;

public static class Program
{
    public static InvocationContext? InvocationContext;
    public static ConsoleRenderer? ConsoleRenderer;

    // ReSharper disable once UnusedMember.Global
    public static async Task<int> Main(InvocationContext invocationContext, string[] args)
    {
        InvocationContext = invocationContext;
        ConsoleRenderer = new ConsoleRenderer(
            invocationContext.Console,
            invocationContext.BindingContext.OutputMode(),
            true);

        var rootCommand = new RootCommand("Kubernetes secret configuration helper")
        {
            CreatePullCommand(),
            CreatePushCommand(),
            CreateBackupCommand(),
            CreateRestoreCommand()
        };

        return await rootCommand.InvokeAsync(args);
    }

    private static Command CreatePullCommand()
    {
        var argumentSecret = new Argument<string>("secret", "The name of the secret in kubernetes");
        var optionContext = new Option<string>(new[] { "--context", "-c" }, "The kubectl config context to use");
        var optionNamespace =
            new Option<string>(new[] { "--namespace", "-n" }, "The namespace of services in kubernetes");
        var optionOutput = new Option<string>(new[] { "--output", "-o" }, "The file to write to");
        var cmd = new Command("pull", "Pulls secrets from kubernetes to a local file")
        {
            argumentSecret,
            optionContext,
            optionNamespace,
            optionOutput
        };

        cmd.SetHandler(
            (string secret, string output, string context, string @namespace) =>
            {
                InvocationContext!.ExitCode = TryCommand(() => PullCommand.Run(output, secret, context, @namespace));
            },
            argumentSecret, optionOutput, optionContext, optionNamespace);

        return cmd;
    }

    private static Command CreatePushCommand()
    {
        var argumentFile = new Argument<string>("file", "The file to read from");
        var optionContext = new Option<string>(new[] { "--context", "-c" }, "The kubectl config context to use");
        var optionSecret = new Option<string>(new[] { "--secret", "-s" }, "The name of the secret in kubernetes");
        var optionNamespace =
            new Option<string>(new[] { "--namespace", "-n" }, "The namespace of services in kubernetes");
        var cmd = new Command("push", "Pushes secrets from a local file into kubernetes")
        {
            argumentFile,
            optionContext,
            optionSecret,
            optionNamespace
        };

        cmd.SetHandler(
            (string file, string context, string secret, string @namespace) =>
            {
                InvocationContext!.ExitCode = TryCommand(() => PushCommand.Run(file, context, secret, @namespace));
            },
            argumentFile, optionContext, optionSecret, optionNamespace);

        return cmd;
    }

    private static Command CreateBackupCommand()
    {
        var optionContext = new Option<string>(new[] { "--context", "-c" }, "The kubectl config context to use");
        var optionNamespace =
            new Option<string>(new[] { "--namespace", "-n" }, "The namespace of services in kubernetes");
        var optionNamespaceIncludes = new Option<string>(new[] { "--namespace-includes", "-i" },
            "The namespace keyword of services in kubernetes");
        var optionNamespaceRegex = new Option<string>(new[] { "--namespace-regex", "-rgx" },
            "The namespace regex to search for services in kubernetes");
        var cmd = new Command("backup", "Pulls all secrets in the cluster")
        {
            optionContext,
            optionNamespace,
            optionNamespaceIncludes,
            optionNamespaceRegex
        };

        cmd.SetHandler(
            (string context, string @namespace, string namespaceIncludes, string namespaceRegex) =>
            {
                InvocationContext!.ExitCode = TryCommand(() =>
                    BackupCommand.Run(context, @namespace, namespaceIncludes, namespaceRegex));
            },
            optionContext, optionNamespace, optionNamespaceIncludes, optionNamespaceRegex);

        return cmd;
    }

    private static Command CreateRestoreCommand()
    {
        var argumentFolder = new Argument<string>("folder", "The folder containing all the secret files to push");
        var optionContext = new Option<string>(new[] { "--context", "-c" }, "The kubectl config context to use");
        var cmd = new Command("restore", "Pushes all secrets in a given folder")
        {
            argumentFolder,
            optionContext
        };

        cmd.SetHandler(
            (string folder, string context) =>
            {
                InvocationContext!.ExitCode = TryCommand(() => RestoreCommand.Run(folder, context));
            },
            argumentFolder, optionContext);

        return cmd;
    }

    private static int TryCommand(Func<int> command)
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
}