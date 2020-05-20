using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Likvido.Confocto
{
    public static class KubeCtl
    {
        public static Dictionary<string, string> GetSecrets(string context, string secretsName)
        {
            var rawSecrets = ExecuteCommand($"get secret {secretsName} -o json");
            dynamic deserialized = JsonConvert.DeserializeObject(rawSecrets);

            var secrets = new Dictionary<string, string>();

            foreach (var secret in deserialized.data.Children())
            {
                secrets.Add(secret.Name, Encoding.UTF8.GetString(Convert.FromBase64String(secret.Value.Value)));
            }

            return secrets;
        }

        private static string ExecuteCommand(string command)
        {
            using var kubectl = new Process();
            kubectl.StartInfo.FileName = "kubectl";
            kubectl.StartInfo.Arguments = command;

            kubectl.StartInfo.UseShellExecute = false;
            kubectl.StartInfo.CreateNoWindow = true;
            kubectl.StartInfo.RedirectStandardOutput = true;

            kubectl.Start();
            kubectl.WaitForExit();

            return kubectl.StandardOutput.ReadToEnd();
        }
    }
}
