namespace Likvido.Kubesec
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine.Rendering.Views;
    using System.Linq;

    public static class PullCommand
    {
        public static int Run(string file, string secretsName, string context, string @namespace)
        {
            var kubeCtl = new KubeCtl(context);

            var existingNamespaces = kubeCtl.GetExistingNamespaces();
            if (!existingNamespaces.Any(a => a.Equals(@namespace)))
            {
                Console.WriteLine($"Error from server (NotFound): namespaces '{@namespace}' not found");
                return 1;
            }

            var secrets = kubeCtl.GetSecrets(secretsName, @namespace);

            if (!secrets.Any())
            {
                return 0;
            }

            if (string.IsNullOrWhiteSpace(file))
            {
                RenderToConsole(secrets);
            }
            else
            {
                Utils.WriteToFile(file, secrets, context, secretsName, @namespace);
            }

            return 0;
        }

        private static void RenderToConsole(IReadOnlyList<Secret> secrets)
        {
            var table = new TableView<Secret> { Items = secrets };
            table.AddColumn(secret => secret.Name, "Name");
            table.AddColumn(secret => secret.Value, "Value");

            var screen = new ScreenView(Program.consoleRenderer, Program.invocationContext.Console)
            {
                Child = table
            };

            screen.Render();
        }
    }
}
