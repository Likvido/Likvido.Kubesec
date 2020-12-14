namespace Likvido.Kubesec
{
    using System;
    using System.IO;

    public static class BackupCommand
    {
        public static int Run(string context, string namespaceContains)
        {
            var backupFolder = $"Kubesec_Backup_{context}_{DateTime.Now:yyyyMMddTHHmmss}";
            Directory.CreateDirectory(backupFolder);

            var kubeCtl = new KubeCtl(context);
            var allSecrets = kubeCtl.GetNamespacesWithSecrets(namespaceContains);

            foreach (var secret in allSecrets)
            {
                var @namespace = secret.Key.Namespace;
                var secretsName = secret.Key.Name;
                var namespaceSubDirectory = $"{backupFolder}/{@namespace}";
                //create subfolders for namespaces
                if (!Directory.Exists($"{namespaceSubDirectory}"))
                {
                    Directory.CreateDirectory($"{namespaceSubDirectory}");
                }

                Utils.WriteToFile($"{namespaceSubDirectory}/{secretsName}.yaml", secret.Value, context, secretsName, @namespace);
            }

            return 0;
        }
    }
}
