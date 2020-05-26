namespace Likvido.Kubesec
{
    using System.Collections.Generic;
    using System.CommandLine.Rendering.Views;
    using System.IO;
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
                WriteToFile(file, secrets, context, secretsName);
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

        private static void WriteToFile(string file, IReadOnlyList<Secret> secrets, string context, string secretsName)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }

            using var fileStream = File.OpenWrite(file);
            using var streamWriter = new StreamWriter(fileStream);

            streamWriter.WriteLine("#######################################");
            streamWriter.WriteLine($"# Context: {context}");
            streamWriter.WriteLine($"# Secret: {secretsName}");
            streamWriter.WriteLine("#######################################");

            foreach (var secret in secrets)
            {
                streamWriter.WriteLine($"{secret.Name}={secret.Value}");
            }
        }
    }
}
