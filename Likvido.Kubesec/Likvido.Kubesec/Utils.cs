namespace Likvido.Kubesec
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public static class Utils
    {
        public static void WriteToFile(string file, IReadOnlyList<Secret> secrets, string context, string secretsName)
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
            streamWriter.WriteLine("#######################################");

            var serializer = new YamlDotNet.Serialization.Serializer();

            //hack due to https://github.com/aaubry/YamlDotNet/issues/361
            // trying to keep "\n" in kubernetes but Environment.NewLine locally
            var content = serializer
                .Serialize(secrets.ToDictionary(s => s.Name, s => s.Value.Replace(Environment.NewLine, "\n")))
                .Replace(Environment.NewLine, "\n")
                .Replace("\n", Environment.NewLine);

            streamWriter.WriteLine(content);
        }
    }
}
