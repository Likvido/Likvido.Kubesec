using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Likvido.Kubesec
{
    public static class KubeCtl
    {
        public static IReadOnlyList<Secret> GetSecrets(string secretsName)
        {
            var secrets = new List<Secret>();
            var result = ExecuteCommand($"get secret {secretsName} -o json");

            if (result.ToLowerInvariant().StartsWith("error"))
            {
                return secrets;
            }

            dynamic deserialized = JsonConvert.DeserializeObject(result);

            foreach (var secret in deserialized.data.Children())
            {
                secrets.Add(new Secret(secret.Name, Encoding.UTF8.GetString(Convert.FromBase64String(secret.Value.Value))));
            }

            return secrets;
        }

        public static bool ApplyFile(string file)
        {
            var result = ExecuteCommand($"apply -f {file}");

            if (result.ToLowerInvariant().StartsWith("error"))
            {
                Console.Error.WriteLine(result);

                return false;
            }

            return true;
        }

        public static string GetCurrentContext()
        {
            return ExecuteCommand("config current-context").TrimEnd('\n');
        }

        public static bool TrySetContext(string context)
        {
            var result = ExecuteCommand($"config use-context {context}");

            return !result.ToLowerInvariant().StartsWith("error");
        }

        private static string ExecuteCommand(string command)
        {
            using var kubectl = new Process();
            kubectl.StartInfo.FileName = "kubectl";
            kubectl.StartInfo.Arguments = command;

            kubectl.StartInfo.UseShellExecute = false;
            kubectl.StartInfo.CreateNoWindow = true;
            kubectl.StartInfo.RedirectStandardOutput = true;
            kubectl.StartInfo.RedirectStandardError = true;

            kubectl.Start();
            kubectl.WaitForExit();

            return kubectl.StandardOutput.ReadToEnd()
                + kubectl.StandardError.ReadToEnd();
        }
    }
}
