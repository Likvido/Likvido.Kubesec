using System.CommandLine.Rendering.Views;

namespace Likvido.Kubesec;

public static class PullCommand
{
    public static int Run(string file, string secretsName, string context, string @namespace)
    {
        var kubeCtl = new KubeCtl(context);


        if (string.IsNullOrEmpty(@namespace))
        {
            @namespace = "default";
        }
        else
        {
            if (!kubeCtl.CheckIfNamespaceExists(@namespace)) return 1;
        }

        var secrets = kubeCtl.GetSecrets(secretsName, @namespace);

        if (!secrets.Any()) return 0;

        if (string.IsNullOrWhiteSpace(file))
            RenderToConsole(secrets);
        else
            Utils.WriteToFile(file, secrets, context, secretsName, @namespace);

        return 0;
    }

    private static void RenderToConsole(IReadOnlyList<Secret> secrets)
    {
        var table = new TableView<Secret> { Items = secrets };
        table.AddColumn(secret => secret.Name, "Name");
        table.AddColumn(secret => secret.Value, "Value");

        if (Program.InvocationContext != null)
        {
            var screen = new ScreenView(Program.ConsoleRenderer, Program.InvocationContext.Console)
            {
                Child = table
            };

            screen.Render();
        }
    }
}