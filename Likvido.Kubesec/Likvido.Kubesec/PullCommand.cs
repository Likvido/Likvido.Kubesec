using Spectre.Console;

namespace Likvido.Kubesec;

public static class PullCommand
{
    public static int Run(string secretsName, string? file = null, string? context = null, string? @namespace = null, string? unwrapKeyName = null)
    {
        var kubeCtl = new KubeCtl(context);

        if (string.IsNullOrEmpty(@namespace))
        {
            @namespace = "default";
        }
        else if (!kubeCtl.CheckIfNamespaceExists(@namespace))
        {
            return 1;
        }

        var secrets = kubeCtl.GetSecrets(secretsName, @namespace);

        if (!secrets.Any())
        {
            Console.WriteLine("No secrets found");
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(unwrapKeyName))
        {
            var secret = secrets.FirstOrDefault(x => x.Name.ToLowerInvariant() == unwrapKeyName.ToLowerInvariant());

            if (secret == null)
            {
                Console.WriteLine($"The key '{unwrapKeyName}' was not found");
                return 0;
            }

            if (string.IsNullOrWhiteSpace(file))
            {
                RenderUnwrappedSecretToConsole(secret);
            }
            else
            {
                Utils.WriteUnwrappedSecretToFile(file, secret);
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                RenderToConsole(secrets);
            }
            else
            {
                Utils.WriteToFile(file, secrets, secretsName, context, @namespace);
            }
        }

        return 0;
    }

    private static void RenderUnwrappedSecretToConsole(Secret secret)
    {
        Console.WriteLine($"Value of the key '{secret.Name}':");
        Console.WriteLine(secret.Value);
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