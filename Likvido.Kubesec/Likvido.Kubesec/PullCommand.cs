using System.Text.RegularExpressions;
using Spectre.Console;

namespace Likvido.Kubesec;

public static class PullCommand
{
    public static async Task<int> Run(string secretsName, bool configurePortForwarding, string? file = null, string? context = null,
        string? @namespace = null, string? unwrapKeyName = null)
    {
        var portForwardingStarted = false;
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

            if (configurePortForwarding)
            {
                portForwardingStarted = await SetUpPortForwardingAndMakeConfigReplacements(kubeCtl, secret);
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

        if (portForwardingStarted)
        {
            try
            {
                Console.WriteLine("Port forwarding configured. To stop the port forwarding, kill kubesec");
                await Task.Delay(Timeout.Infinite).ConfigureAwait(false);
            }
            catch(TaskCanceledException) { /* Ignored */ }
        }

        return 0;
    }

    private static async Task<bool> SetUpPortForwardingAndMakeConfigReplacements(KubeCtl kubeCtl, Secret secret)
    {
        var portForwardingStarted = false;
        var kubernetesUrlRegex = new Regex("(?<baseUrl>https?:\\/\\/(?<serviceName>[a-zA-Z0-9-]*).(?<namespace>[a-zA-Z0-9-]*).svc.cluster.local:?(?<port>\\d{1,5})?)");

        var portForwardTasks = new List<(string BaseUrl, Task<string?> Task)>();
        foreach (Match match in kubernetesUrlRegex.Matches(secret.Value))
        {
            var baseUrl = match.Groups["baseUrl"].Value;
            var serviceName = match.Groups["serviceName"].Value;
            var @namespace = match.Groups["namespace"].Value;
            var port = match.Groups["port"].Value;

            if (baseUrl.ToLowerInvariant().StartsWith("https") && string.IsNullOrWhiteSpace(port))
            {
                port = "443";
            }

            // Only run one port-forward for each base url
            if (portForwardTasks.Any(x => string.Equals(x.BaseUrl, baseUrl, StringComparison.InvariantCultureIgnoreCase)))
            {
                continue;
            }

            portForwardTasks.Add((baseUrl, Task.Run(() => kubeCtl.RunPortForward(serviceName, @namespace, port))));
        }

        foreach (var portForwardTask in portForwardTasks)
        {
            var localPort = await portForwardTask.Task;

            if (string.IsNullOrWhiteSpace(localPort))
            {
                continue;
            }

            secret.Value = secret.Value.Replace(portForwardTask.BaseUrl, $"http://localhost:{localPort}");
            portForwardingStarted = true;
        }

        return portForwardingStarted;
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