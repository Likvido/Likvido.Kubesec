namespace Likvido.Confocto
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
                CreatePushCommand()
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
                    var previousContext = KubeCtl.GetCurrentContext();

                    if (string.IsNullOrWhiteSpace(context))
                    {
                        Console.WriteLine($"No context specified, using current context: {context}");
                        context = previousContext;
                    }
                    else if (previousContext != context && !KubeCtl.TrySetContext(context))
                    {
                        Console.WriteLine("The given context does not exist");

                        return 1;
                    }

                    PullCommand.Run(output, secret, context);

                    if (previousContext != context)
                    {
                        KubeCtl.TrySetContext(previousContext);
                    }

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
