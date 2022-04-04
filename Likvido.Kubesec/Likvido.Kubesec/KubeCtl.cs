using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Likvido.Kubesec.Exceptions;
using Newtonsoft.Json;

namespace Likvido.Kubesec;

public class KubeCtl
{
    private readonly string? context;

    public KubeCtl(string? context)
    {
        this.context = context;
    }

    public IReadOnlyList<Secret> GetSecrets(string secretsName, string @namespace)
    {
        var command = $"get secret {secretsName} -n {@namespace} -o json";
        var result = ExecuteCommand(command);
        dynamic deserialized =
            JsonConvert.DeserializeObject(result ??
                                          throw new InvalidOperationException(
                                              $"Command '{command}' gave no result!")) ??
            throw new InvalidOperationException($"Command '{command}' gave no result!");

        var secrets = new List<Secret>();
        foreach (var secret in deserialized.data.Children())
            secrets.Add(new Secret(secret.Name, Encoding.UTF8.GetString(Convert.FromBase64String(secret.Value.Value))));

        return secrets;
    }

    public Dictionary<(string Namespace, string Name), IReadOnlyList<Secret>> GetNamespacesWithSecrets(
        string? @namespace = null, string? namespaceIncludes = null, string? namespaceRegex = null)
    {
        var allSecretsDictionary = new Dictionary<(string, string), IReadOnlyList<Secret>>();
        Func<string, bool>? filter = null;

        if (!string.IsNullOrEmpty(@namespace))
        {
            filter = f => f.Equals(@namespace);
        }
        else if (!string.IsNullOrEmpty(namespaceIncludes))
        {
            filter = f => f.Contains(namespaceIncludes);
        }
        else if (!string.IsNullOrEmpty(namespaceRegex))
        {
            var regexPattern = new Regex(namespaceRegex);
            filter = f => regexPattern.IsMatch(f);
        }

        var filteredNamespaces = GetExistingNamespaces();

        if (filter != null)
        {
            filteredNamespaces = filteredNamespaces.Where(filter).ToList();
        }

        foreach (var namespaceItem in filteredNamespaces)
        {
            var command = $"get secrets -o json -n={namespaceItem}";
            var result = ExecuteCommand(command, waitingTimeSeconds: 5);
            dynamic deserialized =
                JsonConvert.DeserializeObject(result ??
                                              throw new InvalidOperationException(
                                                  $"Command '{command}' gave no result!")) ??
                throw new InvalidOperationException($"Command '{command}' gave no result!");

            foreach (var item in deserialized.items)
            {
                if (item.type != "Opaque")
                {
                    Console.WriteLine($"Skipping secret '{item.metadata.name}', because it is not Opaque type, but '{item.type}'");
                    continue;
                }

                if (item.data == null)
                {
                    Console.WriteLine($"Skipping secret '{item.metadata.name}', because it does not have any data property");
                    continue;
                }

                var secrets = new List<Secret>();

                foreach (var secret in item.data.Children())
                {
                    secrets.Add(new Secret(secret.Name, Encoding.UTF8.GetString(Convert.FromBase64String(secret.Value.Value))));
                }

                allSecretsDictionary.Add((namespaceItem, (string)item.metadata.name), secrets);
            }
        }

        return allSecretsDictionary;
    }

    public void ApplyFile(string file)
    {
        ExecuteCommand($"apply -f {file}");
    }

    public bool CheckIfNamespaceExists(string @namespace)
    {
        var existingNamespaces = GetExistingNamespaces();
        if (!existingNamespaces.Any(a => a.Equals(@namespace)))
        {
            Console.WriteLine($"Error from server (NotFound): namespaces '{@namespace}' not found");
            return false;
        }

        return true;
    }

    public string? RunPortForward(string service, string @namespace, string port)
    {
        var servicePort = string.IsNullOrWhiteSpace(port) ? "80" : port;
        var output = ExecuteCommand($"port-forward service/{service} -n {@namespace} :{servicePort}", false, 2);

        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        var portForwardOutputRegex = new Regex("^Forwarding from 127\\.0\\.0\\.1:(?<localPort>\\d{2,5})");
        var match = portForwardOutputRegex.Match(output);

        return !match.Success ? null : match.Groups["localPort"].Value;
    }

    private List<string> GetExistingNamespaces()
    {
        var allNamespaces = ExecuteCommand("get namespaces -o custom-columns=:metadata.name");
        var existingNamespaces = allNamespaces?.Split(new[] { "\n" }, StringSplitOptions.None);
        return existingNamespaces?.Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>();
    }

    private string? ExecuteCommand(string command, bool dispose = true, int waitingTimeSeconds = 10)
    {
        var kubectl = new Process();
        kubectl.StartInfo.FileName = "kubectl";
        kubectl.StartInfo.Arguments = string.IsNullOrWhiteSpace(context) ? command : $"{command} --context={context}";
        kubectl.StartInfo.UseShellExecute = false;
        kubectl.StartInfo.CreateNoWindow = true;
        kubectl.StartInfo.RedirectStandardOutput = true;
        kubectl.StartInfo.RedirectStandardError = true;
        kubectl.Start();

        kubectl.WaitForExit((int)TimeSpan.FromSeconds(waitingTimeSeconds).TotalMilliseconds);

        var output = dispose
            ? kubectl.StandardOutput.ReadToEnd()
            : kubectl.StandardOutput.ReadLine();

        if (kubectl.HasExited && kubectl.ExitCode != 0)
        {
            var error = kubectl.StandardError.ReadToEnd();

            if (!string.IsNullOrWhiteSpace(error))
            {
                throw new KubeCtlException(error);
            }
        }

        if (dispose)
        {
            kubectl.Kill(true);
            kubectl.WaitForExit();
            kubectl.Dispose();
        }

        return output;
    }
}