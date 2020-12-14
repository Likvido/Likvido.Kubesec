namespace Likvido.Kubesec
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.CommandLine.Rendering;
    using System.Threading.Tasks;

    public class Program
    {
        public static InvocationContext invocationContext;
        public static ConsoleRenderer consoleRenderer;

        public static async Task<int> Main(InvocationContext invocationContext, string[] args)
        {
            Program.invocationContext = invocationContext;
            consoleRenderer = new ConsoleRenderer(
              invocationContext.Console,
              mode: invocationContext.BindingContext.OutputMode(),
              resetAfterRender: true);

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
            var cmd = new Command("pull", "Pulls secrets from kubernetes to a local file")
            {
                new Argument<string>("secret", "The name of the secret in kubernetes"),
                new Option<string>(new string[] { "--context", "-c" }, "The kubectl config context to use"),
                new Option<string>(new string[] { "--namespace", "-n" }, "The namespace of services in kubernetes"),
                new Option<string>(new string[] { "--output", "-o" }, "The file to write to")
            };

            cmd.Handler = CommandHandler.Create(
                (string output, string context, string secret, string @namespace) =>
                {
                    return TryCommand(() => PullCommand.Run(output, secret, context, @namespace));
                });

            return cmd;
        }

        private static Command CreatePushCommand()
        {
            var cmd = new Command("push", "Pushes secrets from a local file into kubernetes")
            {
                new Argument<string>("file", "The file to read from"),
                new Option<string>(new string[] { "--context", "-c" }, "The kubectl config context to use"),
                new Option<string>(new string[] { "--secret", "-s" }, "The name of the secret in kubernetes"),
                new Option<string>(new string[] { "--namespace", "-n" }, "The namespace of services in kubernetes")
            };

            cmd.Handler = CommandHandler.Create(
                (string file, string context, string secret, string @namespace) =>
                {
                    return TryCommand(() => PushCommand.Run(file, context, secret, @namespace));
                });

            return cmd;
        }

        private static Command CreateBackupCommand()
        {
            var cmd = new Command("backup", "Pulls all secrets in the cluster")
            {
                new Option<string>(new string[] { "--context", "-c" }, "The kubectl config context to use"),
                new Option<string>(new string[] { "--namespace", "-n" }, "The namespace of services in kubernetes"),
                new Option<string>(new string[] { "--namespace-contains", "-nc" }, "The namespace keyword of services in kubernetes"),
                new Option<string>(new string[] { "--namespace-regex", "-nrgx" }, "The namespace regex to search for services in kubernetes")
            };

            cmd.Handler = CommandHandler.Create(
                (string context, string @namespace, string namespaceContains, string namespaceRegex) =>
                {
                    return TryCommand(() => BackupCommand.Run(context, @namespace, namespaceContains, namespaceRegex));
                });

            return cmd;
        }

        private static Command CreateRestoreCommand()
        {
            var cmd = new Command("restore", "Pushes all secrets in a given folder")
            {
                new Argument<string>("folder", "The folder containing all the secret files to push"),
                new Option<string>(new string[] { "--context", "-c" }, "The kubectl config context to use")
            };

            cmd.Handler = CommandHandler.Create(
                (string folder, string context) =>
                {
                    return TryCommand(() => RestoreCommand.Run(folder, context));
                });

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
}
