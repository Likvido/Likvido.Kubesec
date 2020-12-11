namespace Likvido.Kubesec
{
    using System;
    using System.IO;

    public static class BackupCommand
    {
        public static int Run(string context, string namespaceKeyword)
        {
            var backupFolder = $"Kubesec_Backup_{context}_{DateTime.Now:yyyyMMddTHHmmss}";
            Directory.CreateDirectory(backupFolder);

            var kubeCtl = new KubeCtl(context);
            var allSecrets = kubeCtl.GetNamespacesWithSecrets(namespaceKeyword);

            foreach (var secret in allSecrets)
            {
                var @namespace = secret.Key.Item1;
                var secretsName = secret.Key.Item2;
                var namespaceSubDirectory = $"{backupFolder}/{@namespace}";
                //create subfolders for namespaces
                if (!Directory.Exists($"{namespaceSubDirectory}"))
                {
                    Directory.CreateDirectory($"{namespaceSubDirectory}");
                }

                Utils.WriteToFile($"{namespaceSubDirectory}/{secretsName}.yaml", secret.Value, context, secretsName, null);
            }

            return 0;
        }
    }
}
