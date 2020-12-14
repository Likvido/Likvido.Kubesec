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

        public Dictionary<(string Namespace, string Name), IReadOnlyList<Secret>> GetNamespacesWithSecrets(string @namespace, string namespaceContains, string namespaceRegex)
        {
            var allSecretsDictionary = new Dictionary<(string, string), IReadOnlyList<Secret>>();
            string pattern = null;

            if (!string.IsNullOrEmpty(@namespace))
            {
                pattern = @namespace;
            }
            else if (!string.IsNullOrEmpty(namespaceContains))
            {
                pattern = namespaceContains;
            }
            else if (!string.IsNullOrEmpty(namespaceRegex))
            {
                pattern = namespaceRegex;
            }
            else
            {
                pattern = "default";
            }

            var regexPattern = new Regex(pattern);
            var filteredNamespaces = GetExistingNamespaces().Where(n => regexPattern.IsMatch(n));

            foreach (var namespaceItem in filteredNamespaces)
            {
                var result = ExecuteCommand($"get secrets -o json -n={namespaceItem}");
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
