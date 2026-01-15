namespace Likvido.Kubesec;

public static class BackupCommand
{
    public static int Run(string? context = null, string? @namespace = null, string? namespaceIncludes = null,
        string? namespaceRegex = null, string[]? excludedNamespaces = null, CancellationToken cancellationToken = default)
    {
        var backupFolder = $"Kubesec_Backup_{context}_{DateTime.Now:yyyyMMddTHHmmss}";
        var kubeCtl = new KubeCtl(context);

        CreateBackup(kubeCtl, backupFolder, context, @namespace, namespaceIncludes, namespaceRegex, excludedNamespaces, cancellationToken);

        return 0;
    }

    /// <summary>
    /// Creates a backup and returns the secrets that were backed up.
    /// </summary>
    public static Dictionary<(string Namespace, string Name), IReadOnlyList<Secret>> CreateBackup(
        KubeCtl kubeCtl,
        string backupFolder,
        string? context = null,
        string? @namespace = null,
        string? namespaceIncludes = null,
        string? namespaceRegex = null,
        string[]? excludedNamespaces = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(backupFolder);

        var allSecrets = kubeCtl.GetNamespacesWithSecrets(@namespace, namespaceIncludes, namespaceRegex, excludedNamespaces, cancellationToken);

        foreach (var secret in allSecrets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var namespaceItem = secret.Key.Namespace;
            var secretsName = secret.Key.Name;
            var namespaceSubDirectory = $"{backupFolder}/{namespaceItem}";

            if (!Directory.Exists(namespaceSubDirectory))
            {
                Directory.CreateDirectory(namespaceSubDirectory);
            }

            Utils.WriteToFile($"{namespaceSubDirectory}/{secretsName}.yaml", secret.Value, secretsName, context, namespaceItem);
        }

        return allSecrets;
    }
}
