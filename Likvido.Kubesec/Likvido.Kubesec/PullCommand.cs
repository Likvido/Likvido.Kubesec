using Spectre.Console;

namespace Likvido.Kubesec;

public static class PullCommand
{
    public static int Run(string secretsName, string? file = null, string? context = null, string? @namespace = null)
    {
        var kubeCtl = new KubeCtl(context);

        if (string.IsNullOrEmpty(@namespace))
        {
            @namespace = "default";
        }
        else
        {
            if (!kubeCtl.CheckIfNamespaceExists(@namespace))
            {
                return 1;
            }
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
            Utils.WriteToFile(file, secrets, secretsName, context, @namespace);
        }

        return 0;
    }

    private static void RenderToConsole(IReadOnlyList<Secret> secrets)
    {
        var table = new Table();
        table.AddColumn("Name");
        table.AddColumn("Value");

        foreach (var secret in secrets)
        {
            table.AddRow(secret.Name, secret.Value);
        }

        AnsiConsole.Write(table);
    }
}