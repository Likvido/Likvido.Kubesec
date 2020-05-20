namespace Likvido.Confocto
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.Threading.Tasks;

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("Kubernetes secret configuration helper")
            {
                CreatePullCommand(),
                CreatePushCommand()
            };

            return await rootCommand.InvokeAsync(args);
        }

        private static Command CreatePullCommand()
        {
            var cmd = new Command("pull", "Pulls secrets from kubernetes to a local file")
            {
                new Argument<string>("file", "The file to write to"),
                new Option<string>(new string[] { "--context", "-c" }, "The kubectl config context to use"),
                new Option<string>(new string[] { "--secret", "-s" }, "The name of the secret in kubernetes")
            };

            cmd.Handler = CommandHandler.Create(
                (string file, string context, string secret) =>
                {
                    if (string.IsNullOrWhiteSpace(file))
                    {
                        Console.WriteLine("file is required");

                        return 1;
                    }

                    PullCommand.Run(file, context, secret);

                    return 0;
                });

            return cmd;
        }

        private static Command CreatePushCommand()
        {
            var cmd = new Command("push", "Pushes secrets from a local file into kubernetes")
            {
                new Option<string>(new string[] { "--context", "-c" }, "The kubectl config context to use"),
                new Option<string>(new string[] { "--file", "-f" }, "The file to read from")
            };

            cmd.Handler = CommandHandler.Create<string, string>(PushCommand.Run);

            return cmd;
        }
    }
}
