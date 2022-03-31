﻿using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Likvido.Kubesec.Exceptions;
using Newtonsoft.Json;

namespace Likvido.Kubesec;

public class KubeCtl
{
    private readonly string context;

    public KubeCtl(string context)
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
        string @namespace, string namespaceIncludes, string namespaceRegex)
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

        if (filter != null) filteredNamespaces = filteredNamespaces.Where(filter).ToList();

        foreach (var namespaceItem in filteredNamespaces)
        {
            var command = $"get secrets -o json -n={namespaceItem}";
            var result = ExecuteCommand(command);
            dynamic deserialized =
                JsonConvert.DeserializeObject(result ??
                                              throw new InvalidOperationException(
                                                  $"Command '{command}' gave no result!")) ??
                throw new InvalidOperationException($"Command '{command}' gave no result!");

            foreach (var item in deserialized.items)
            {
                if (item.type != "Opaque")
                {
                    Console.WriteLine(
                        $"Skipping secret '{item.metadata.name}', because it is not Opaque type, but '{item.type}'");
                    continue;
                }

                if (item.data == null)
                {
                    Console.WriteLine(
                        $"Skipping secret '{item.metadata.name}', because it does not have any data property");
                    continue;
                }

                var secrets = new List<Secret>();

                foreach (var secret in item.data.Children())
                    secrets.Add(new Secret(secret.Name,
                        Encoding.UTF8.GetString(Convert.FromBase64String(secret.Value.Value))));

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

    private List<string> GetExistingNamespaces()
    {
        var allNamespaces = ExecuteCommand("get namespaces -o custom-columns=:metadata.name");
        var existingNamespaces = allNamespaces?.Split(new[] { "\n" }, StringSplitOptions.None);
        return existingNamespaces?.Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>();
    }

    private string? ExecuteCommand(string command)
    {
        using var kubectl = new Process();
        kubectl.StartInfo.FileName = "kubectl";
        kubectl.StartInfo.Arguments = string.IsNullOrWhiteSpace(context) ? command : $"{command} --context={context}";

        kubectl.StartInfo.UseShellExecute = false;
        kubectl.StartInfo.CreateNoWindow = true;
        kubectl.StartInfo.RedirectStandardOutput = true;
        kubectl.StartInfo.RedirectStandardError = true;

        kubectl.Start();

        string? output = null;
        // ReSharper disable once AccessToDisposedClosure
        var outputReadingTask = Task.Run(() => output = kubectl.StandardOutput.ReadToEnd());

        kubectl.WaitForExit((int)TimeSpan.FromSeconds(10).TotalMilliseconds);
        outputReadingTask.Wait((int)TimeSpan.FromSeconds(10).TotalMilliseconds);

        if (kubectl.HasExited && kubectl.ExitCode != 0)
        {
            var error = kubectl.StandardError.ReadToEnd();

            if (!string.IsNullOrWhiteSpace(error)) throw new KubeCtlException(error);
        }

        return output;
    }
}