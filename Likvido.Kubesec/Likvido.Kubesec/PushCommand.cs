using System.Text;
using System.Text.RegularExpressions;
using Likvido.Kubesec.Exceptions;
using YamlDotNet.Serialization;

namespace Likvido.Kubesec;

public static class PushCommand
{
    public static int Run(string file, string? context = null, string? secretsName = null, string? @namespace = null, bool skipPrompts = false)
    {
        var kubeCtl = new KubeCtl(context);

        if (!File.Exists(file))
        {
            Console.WriteLine($"This file does not exist: {file}");

            return 1;
        }

        var secretsFile = ReadSecretsFile(file);

        if (string.IsNullOrWhiteSpace(secretsFile.ContextFromHeader))
        {
            if (!skipPrompts && !PromptContinue(
                    "The file does not appear to be written by Kubesec (missing context header). Are you sure you wish to continue?"))
            {
                return 0;
            }
        }
        else if (context != secretsFile.ContextFromHeader)
        {
            if (!skipPrompts && !PromptContinue(
                    $"The given context '{context}' does not match the context in the file '{secretsFile.ContextFromHeader}'. Are you sure you wish to continue?"))
            {
                return 0;
            }
        }

        if (string.IsNullOrWhiteSpace(secretsName))
        {
            if (string.IsNullOrWhiteSpace(secretsFile.SecretsNameFromHeader))
            {
                Console.WriteLine("Providing a secret name is required when the file does not contain a secret name");

                return 1;
            }

            Console.WriteLine($"Using secret: {secretsFile.SecretsNameFromHeader}");
            secretsName = secretsFile.SecretsNameFromHeader;
        }
        else if (secretsName != secretsFile.SecretsNameFromHeader)
        {
            if (!skipPrompts && !PromptContinue(
                    $"The given secret name '{secretsName}' does not match the secret name in the file '{secretsFile.SecretsNameFromHeader}'. Are you sure you wish to continue?"))
            {
                return 0;
            }
        }

        if (string.IsNullOrEmpty(@namespace))
        {
            @namespace = "default";
            if (!string.IsNullOrWhiteSpace(secretsFile.NamespaceFromHeader))
            {
                @namespace = secretsFile.NamespaceFromHeader;
                if (!kubeCtl.CheckIfNamespaceExists(@namespace))
                {
                    return 1;
                }
            }
        }
        else
        {
            if (!kubeCtl.CheckIfNamespaceExists(@namespace))
            {
                return 1;
            }
        }

        Console.WriteLine($"Using namespace: {@namespace}");

        if (!DisplayChanges(secretsName, @namespace, secretsFile, kubeCtl))
        {
            Console.WriteLine("No changes detected ... skipping");

            return 0;
        }

        if (!skipPrompts && !PromptContinue("Are you sure you wish to continue?"))
        {
            return 0;
        }

        var secretsTempFile = CreateSecretsTempFile(secretsName, @namespace, secretsFile.Secrets);

        kubeCtl.ApplyFile(secretsTempFile);
        Console.WriteLine("All done ... great success");
        File.Delete(secretsTempFile);

        return 0;
    }

    private static string CreateSecretsTempFile(string secretName, string @namespace, IReadOnlyList<Secret> secrets)
    {
        var contentBuilder = new StringBuilder();

        contentBuilder.AppendLine("apiVersion: v1");
        contentBuilder.AppendLine("kind: Secret");
        contentBuilder.AppendLine("metadata:");
        contentBuilder.AppendLine($"  name: {secretName}");
        contentBuilder.AppendLine($"  namespace: {@namespace}");
        contentBuilder.AppendLine("type: Opaque");
        contentBuilder.AppendLine("data:");

        foreach (var secret in secrets)
        {
            contentBuilder.AppendLine($"  {secret.Name}: {Convert.ToBase64String(Encoding.UTF8.GetBytes(secret.Value))}");
        }

        var filePath = $".secrets-upload-{DateTime.Now:yyyyMMddTHHmmss}.yaml";
        File.WriteAllText(filePath, contentBuilder.ToString());

        return filePath;
    }

    private static bool DisplayChanges(string secretsName, string @namespace, SecretsFile secretsFile, KubeCtl kubeCtl)
    {
        IReadOnlyList<Secret> currentKubernetesSecrets;
        try
        {
            currentKubernetesSecrets = kubeCtl.GetSecrets(secretsName, @namespace);
        }
        catch (KubeCtlException)
        {
            currentKubernetesSecrets = new List<Secret>();
        }

        var changesDetected = false;
        Console.WriteLine("Changes:");
        foreach (var removedSecret in currentKubernetesSecrets.Where(x => secretsFile.Secrets.All(y => x.Name != y.Name)))
        {
            Console.WriteLine($"- {removedSecret.Name}");
            changesDetected = true;
        }

        foreach (var addedSecret in secretsFile.Secrets.Where(x => currentKubernetesSecrets.All(y => x.Name != y.Name)))
        {
            Console.WriteLine($"+ {addedSecret.Name}");
            changesDetected = true;
        }

        foreach (var modifiedSecret in secretsFile.Secrets.Where(x => currentKubernetesSecrets.Any(y => x.Name == y.Name && x.Value != y.Value)))
        {
            var currentKubernetesSecret = currentKubernetesSecrets.First(x => x.Name == modifiedSecret.Name);
            Console.WriteLine(modifiedSecret.Name);
            Console.WriteLine($"FROM: {currentKubernetesSecret.Value}");
            Console.WriteLine($"TO  : {modifiedSecret.Value}");
            changesDetected = true;
        }

        return changesDetected;
    }

    private static bool PromptContinue(string question)
    {
        ConsoleKey response;
        do
        {
            Console.Write($"{question} [y/n] ");
            response = Console.ReadKey(false).Key;
            if (response != ConsoleKey.Enter)
            {
                Console.WriteLine();
            }
        } while (response != ConsoleKey.Y && response != ConsoleKey.N);

        return response == ConsoleKey.Y;
    }

    private static SecretsFile ReadSecretsFile(string file)
    {
        var secretsFile = new SecretsFile();
        var fileContents = File.ReadAllText(file);

        var contextMatch = Regex.Match(fileContents, "# Context: ([^\n\r]*)");
        var secretMatch = Regex.Match(fileContents, "# Secret: ([^\n\r]*)");
        var namespaceMatch = Regex.Match(fileContents, "# Namespace: ([^\n\r]*)");

        if (contextMatch.Success)
        {
            secretsFile.ContextFromHeader = contextMatch.Groups[1].Value;
        }

        if (secretMatch.Success)
        {
            secretsFile.SecretsNameFromHeader = secretMatch.Groups[1].Value;
        }

        if (namespaceMatch.Success)
        {
            secretsFile.NamespaceFromHeader = namespaceMatch.Groups[1].Value;
        }

        var deserializer = new Deserializer();
        foreach (var item in deserializer.Deserialize<Dictionary<string, string>>(fileContents))
        {
            // trying to keep "\n" in kubernetes but Environment.NewLine locally
            secretsFile.Secrets.Add(new Secret(item.Key, item.Value.Replace(Environment.NewLine, "\n")));
        }

        return secretsFile;
    }

    private class SecretsFile
    {
        public string? ContextFromHeader { get; set; }

        public string? SecretsNameFromHeader { get; set; }

        public string? NamespaceFromHeader { get; set; }

        public List<Secret> Secrets { get; } = new();
    }
}