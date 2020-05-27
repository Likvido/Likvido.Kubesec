namespace Likvido.Kubesec
{
    using System.Collections.Generic;
    using System.CommandLine.Rendering.Views;
    using System.Linq;

    public static class PullCommand
    {
        public static int Run(string file, string secretsName, string context)
        {
            var secrets = KubeCtl.GetSecrets(secretsName);

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
                Utils.WriteToFile(file, secrets, context, secretsName);
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
