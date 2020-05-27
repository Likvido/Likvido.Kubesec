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
                new Option<string>(new string[] { "--output", "-o" }, "The file to write to")
            };

            cmd.Handler = CommandHandler.Create(
                (string output, string context, string secret) =>
                {
                    return SetContextAndRunCommand(context, (c) => PullCommand.Run(output, secret, c));
                });

            return cmd;
        }

        private static Command CreatePushCommand()
        {
            var cmd = new Command("push", "Pushes secrets from a local file into kubernetes")
            {
                new Argument<string>("file", "The file to read from"),
                new Option<string>(new string[] { "--context", "-c" }, "The kubectl config context to use"),
                new Option<string>(new string[] { "--secret", "-s" }, "The name of the secret in kubernetes")
            };

            cmd.Handler = CommandHandler.Create(
                (string file, string context, string secret) =>
                {
                    return SetContextAndRunCommand(context, (c) => PushCommand.Run(file, c, secret));
                });

            return cmd;
        }

        private static Command CreateBackupCommand()
        {
            var cmd = new Command("backup", "Pulls all secrets in the cluster")
            {
                new Option<string>(new string[] { "--context", "-c" }, "The kubectl config context to use")
            };

            cmd.Handler = CommandHandler.Create(
                (string context) =>
                {
                    return SetContextAndRunCommand(context, (c) => BackupCommand.Run(c));
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
                    return SetContextAndRunCommand(context, (c) => RestoreCommand.Run(folder, c));
                });

            return cmd;
        }

        private static int SetContextAndRunCommand(string context, Func<string, int> command)
        {
            var previousContext = KubeCtl.GetCurrentContext();

            if (string.IsNullOrWhiteSpace(context))
            {
                Console.WriteLine($"Using context: {previousContext}");
                context = previousContext;
            }
            else if (previousContext != context && !KubeCtl.TrySetContext(context))
            {
                Console.WriteLine("The given context does not exist");

                return 1;
            }

            try
            {
                return command(context);
            }
            finally
            {
                if (previousContext != context)
                {
                    KubeCtl.TrySetContext(previousContext);
                }
            }
        }
    }
}
