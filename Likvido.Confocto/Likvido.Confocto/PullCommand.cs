namespace Likvido.Confocto
{
    using System.IO;

    public static class PullCommand
    {
        public static void Run(string file, string context, string secretsName)
        {
            var secrets = KubeCtl.GetSecrets(context, secretsName);

            using var fileStream = File.OpenWrite(file);
            using var streamWriter = new StreamWriter(fileStream);

            foreach (var secret in secrets)
            {
                streamWriter.WriteLine($"{secret.Key}={secret.Value}");
            }
        }
    }
}
