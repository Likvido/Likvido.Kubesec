using YamlDotNet.Serialization;

namespace Likvido.Kubesec;

public static class Utils
{
    public static void WriteToFile(string file, IReadOnlyList<Secret> secrets, string secretsName, string? context = null, string? @namespace = null)
    {
        if (File.Exists(file))
        {
            File.Delete(file);
        }

        Console.WriteLine($"Writing file '{file}'");

        using var fileStream = File.OpenWrite(file);
        using var streamWriter = new StreamWriter(fileStream);

        streamWriter.WriteLine("#######################################");
        streamWriter.WriteLine($"# Context: {context}");
        streamWriter.WriteLine($"# Secret: {secretsName}");
        streamWriter.WriteLine($"# Namespace: {@namespace}");
        streamWriter.WriteLine("#######################################");

        var serializer = new Serializer();

        //hack due to https://github.com/aaubry/YamlDotNet/issues/361
        // trying to keep "\n" in kubernetes but Environment.NewLine locally
        var content = serializer
            .Serialize(secrets.ToDictionary(s => s.Name, s => s.Value.Replace(Environment.NewLine, "\n")))
            .Replace(Environment.NewLine, "\n")
            .Replace("\n", Environment.NewLine);

        streamWriter.WriteLine(content);
    }

    public static void WriteUnwrappedSecretToFile(string file, Secret secret)
    {
        Console.WriteLine($"Writing the value of the key '{secret.Name}' to the file '{file}'");

        if (File.Exists(file))
        {
            File.Delete(file);
        }

        using var fileStream = File.OpenWrite(file);
        using var streamWriter = new StreamWriter(fileStream);

        streamWriter.Write(secret.Value);
    }
}
