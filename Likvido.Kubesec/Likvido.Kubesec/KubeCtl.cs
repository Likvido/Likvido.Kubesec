namespace Likvido.Kubesec
{
    using Likvido.Kubesec.Exceptions;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    public class KubeCtl
    {
        private readonly string context;

        public KubeCtl(string context)
        {
            this.context = context;
        }

        public IReadOnlyList<Secret> GetSecrets(string secretsName, string @namespace)
        {
            var result = ExecuteCommand($"get secret {secretsName} -n {@namespace} -o json");
            dynamic deserialized = JsonConvert.DeserializeObject(result);

            var secrets = new List<Secret>();
            foreach (var secret in deserialized.data.Children())
            {
                secrets.Add(new Secret(secret.Name, Encoding.UTF8.GetString(Convert.FromBase64String(secret.Value.Value))));
            }

            return secrets;
        }

        public List<string> GetExistingNamespaces()
        {
            var allNamespaces = ExecuteCommand($"get namespaces -o custom-columns=:metadata.name");
            var existingNamespaces = allNamespaces.Split(new string[] { "\n" }, StringSplitOptions.None);
            return existingNamespaces.Where(n => !string.IsNullOrEmpty(n)).ToList();
        }

        public Dictionary<Tuple<string, string>, IReadOnlyList<Secret>> GetNamespacesWithSecrets(string namespaceKeyword)
        {
            var allSecretsDictionary = new Dictionary<Tuple<string, string>, IReadOnlyList<Secret>>();
            var filteredNamespaces = GetExistingNamespaces().Where(n => !string.IsNullOrEmpty(namespaceKeyword) && n.Contains(namespaceKeyword));

            foreach (var @namespace in filteredNamespaces)
            {
                var result = ExecuteCommand($"get secrets -o json -n={@namespace}");
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

                    allSecretsDictionary.Add(new Tuple<string, string>(@namespace, (string)item.metadata.name), secrets);
                }
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
