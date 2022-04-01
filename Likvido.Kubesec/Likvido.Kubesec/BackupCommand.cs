namespace Likvido.Kubesec;

public static class BackupCommand
{
    public static int Run(string? context = null, string? @namespace = null, string? namespaceIncludes = null, string? namespaceRegex = null)
    {
        var backupFolder = $"Kubesec_Backup_{context}_{DateTime.Now:yyyyMMddTHHmmss}";
        Directory.CreateDirectory(backupFolder);

        var kubeCtl = new KubeCtl(context);
        var allSecrets = kubeCtl.GetNamespacesWithSecrets(@namespace, namespaceIncludes, namespaceRegex);

        foreach (var secret in allSecrets)
        {
            var namespaceItem = secret.Key.Namespace;
            var secretsName = secret.Key.Name;
            var namespaceSubDirectory = $"{backupFolder}/{namespaceItem}";
            //create subfolders for namespaces
            if (!Directory.Exists($"{namespaceSubDirectory}"))
            {
                Directory.CreateDirectory($"{namespaceSubDirectory}");
            }

            Utils.WriteToFile($"{namespaceSubDirectory}/{secretsName}.yaml", secret.Value, secretsName, context, namespaceItem);
        }

        return 0;
    }
}