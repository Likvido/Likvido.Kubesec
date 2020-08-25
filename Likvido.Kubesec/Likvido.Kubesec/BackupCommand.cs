namespace Likvido.Kubesec
{
    using System;
    using System.IO;

    public static class BackupCommand
    {
        public static int Run(string context)
        {
            var backupFolder = $"Kubesec_Backup_{context}_{DateTime.Now:yyyyMMddTHHmmss}";
            Directory.CreateDirectory(backupFolder);

            var kubeCtl = new KubeCtl(context);
            var allSecrets = kubeCtl.GetAllSecrets();

            foreach (var secret in allSecrets)
            {
                Utils.WriteToFile($"{backupFolder}/{secret.Key}.yaml", secret.Value, context, secret.Key);
            }

            return 0;
        }
    }
}
