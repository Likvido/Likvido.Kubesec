namespace Likvido.Kubesec
{
    using Likvido.Kubesec.Exceptions;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

    public class KubeCtl
    {
        private readonly string context;

        public KubeCtl(string context)
        {
            this.context = context;
        }

        public IReadOnlyList<Secret> GetSecrets(string secretsName)
        {
            var result = ExecuteCommand($"get secret {secretsName} -o json", 5);
            dynamic deserialized = JsonConvert.DeserializeObject(result);

            var secrets = new List<Secret>();
            foreach (var secret in deserialized.data.Children())
            {
                secrets.Add(new Secret(secret.Name, Encoding.UTF8.GetString(Convert.FromBase64String(secret.Value.Value))));
            }

            return secrets;
        }

        public Dictionary<string, IReadOnlyList<Secret>> GetAllSecrets()
        {
            var allSecretsDictionary = new Dictionary<string, IReadOnlyList<Secret>>();
            var result = ExecuteCommand($"get secrets -o json");

            dynamic deserialized = JsonConvert.DeserializeObject(result);

            foreach (var item in deserialized.items)
            {
                if (item.type != "Opaque")
                {
                    Console.WriteLine($"Skipping secret '{item.metadata.name}', because it is not Opaque type, but '{item.type}'");
                    continue;
                }

                var secrets = new List<Secret>();

                foreach (var secret in item.data.Children())
                {
                    secrets.Add(new Secret(secret.Name, Encoding.UTF8.GetString(Convert.FromBase64String(secret.Value.Value))));
                }

                allSecretsDictionary.Add((string)item.metadata.name, secrets);
            }

            return allSecretsDictionary;
        }

        public void ApplyFile(string file)
        {
            ExecuteCommand($"apply -f {file}");
        }

        private string ExecuteCommand(string command, int secondsTimeout = 10)
        {
            using var kubectl = new Process();
            kubectl.StartInfo.FileName = "kubectl";
            kubectl.StartInfo.Arguments = string.IsNullOrWhiteSpace(context) ? command : $"{command} --context={context}";

            kubectl.StartInfo.UseShellExecute = false;
            kubectl.StartInfo.CreateNoWindow = true;
            kubectl.StartInfo.RedirectStandardInput = true;
            kubectl.StartInfo.RedirectStandardOutput = true;
            kubectl.StartInfo.RedirectStandardError = true;

            kubectl.Start();
            kubectl.WaitForExit((int)TimeSpan.FromSeconds(secondsTimeout).TotalMilliseconds);

            var output = "";
            if (!kubectl.HasExited)
            {
                output = kubectl.StandardOutput.ReadToEnd();

                //sometimes it doesn't stop at all< so we send crtl+c
                kubectl.StandardInput.WriteLine("\x3");
                kubectl.StandardInput.Flush();
                kubectl.WaitForExit((int)TimeSpan.FromSeconds(10).TotalMilliseconds);
            }

            if (kubectl.ExitCode != 0)
            {
                var error = kubectl.StandardError.ReadToEnd();

                if (!string.IsNullOrWhiteSpace(error))
                {
                    throw new KubeCtlException(error);
                }
            }

            return output;
        }
    }
}
