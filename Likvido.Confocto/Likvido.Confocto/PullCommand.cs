namespace Likvido.Confocto
{
    using System.Collections.Generic;
    using System.CommandLine.Rendering.Views;
    using System.IO;

    public static class PullCommand
    {
        public static void Run(string file, string context, string secretsName)
        {
            var secrets = KubeCtl.GetSecrets(context, secretsName);

            if (string.IsNullOrWhiteSpace(file))
            {
                RenderToConsole(secrets);
            }
            else
            {
                WriteToFile(file, secrets);
            }
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

        private static void WriteToFile(string file, IReadOnlyList<Secret> secrets)
        {
            using var fileStream = File.OpenWrite(file);
            using var streamWriter = new StreamWriter(fileStream);

            foreach (var secret in secrets)
            {
                streamWriter.WriteLine($"{secret.Name}={secret.Value}");
            }
        }
    }
}
