namespace Likvido.Kubesec
{
    using System;
    using System.Collections.Generic;
    using System.IO;

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

            foreach (var secret in secrets)
            {
                streamWriter.WriteLine($"{secret.Name}={secret.Value}");
            }
        }
    }
}
