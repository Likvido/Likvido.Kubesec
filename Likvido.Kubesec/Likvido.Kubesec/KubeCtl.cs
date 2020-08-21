namespace Likvido.Kubesec
{
    using Likvido.Kubesec.Exceptions;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Threading.Tasks;

    public class KubeCtl
    {
        private readonly string context;

        public KubeCtl(string context)
        {
            this.context = context;
        }

        public IReadOnlyList<Secret> GetSecrets(string secretsName)
        {
            var result = ExecuteCommand($"get secret {secretsName} -o json");
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

        private string ExecuteCommand(string command)
        {
            using var kubectl = new Process();
            kubectl.StartInfo.FileName = "kubectl";
            kubectl.StartInfo.Arguments = string.IsNullOrWhiteSpace(context) ? command : $"{command} --context={context}";

            kubectl.StartInfo.UseShellExecute = false;
            kubectl.StartInfo.CreateNoWindow = true;
            kubectl.StartInfo.RedirectStandardOutput = true;
            kubectl.StartInfo.RedirectStandardError = true;

            kubectl.Start();

            string output = null;
            var outputReadingTask = Task.Run(() => output = kubectl.StandardOutput.ReadToEnd());

            kubectl.WaitForExit((int)TimeSpan.FromSeconds(10).TotalMilliseconds);
            outputReadingTask.Wait((int)TimeSpan.FromSeconds(10).TotalMilliseconds);

            if (kubectl.HasExited && kubectl.ExitCode != 0)
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
