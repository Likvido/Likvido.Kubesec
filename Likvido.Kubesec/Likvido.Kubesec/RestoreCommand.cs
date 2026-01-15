namespace Likvido.Kubesec;

public static class RestoreCommand
{
    public static int Run(string folder, string? context, bool recursive, bool skipPrompts = false, bool autoCreateMissingNamespaces = false, CancellationToken cancellationToken = default)
    {
        RestoreFiles(folder, context, skipPrompts, autoCreateMissingNamespaces, cancellationToken);

        if (recursive)
        {
            RecursiveRestore(folder, context, skipPrompts, autoCreateMissingNamespaces, cancellationToken);
        }

        return 0;
    }

    private static void RecursiveRestore(string folder, string? context, bool skipPrompts, bool autoCreateMissingNamespaces, CancellationToken cancellationToken)
    {
        foreach (var directory in Directory.EnumerateDirectories(folder))
        {
            cancellationToken.ThrowIfCancellationRequested();

            RestoreFiles(directory, context, skipPrompts, autoCreateMissingNamespaces, cancellationToken);
            RecursiveRestore(directory, context, skipPrompts, autoCreateMissingNamespaces, cancellationToken);
        }
    }

    private static void RestoreFiles(string folder, string? context, bool skipPrompts, bool autoCreateMissingNamespaces, CancellationToken cancellationToken)
    {
        foreach (var file in Directory.EnumerateFiles(folder))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Skip .DS_Store files
            if (Path.GetFileName(file) == ".DS_Store")
            {
                Console.WriteLine($"Skipping '.DS_Store' file: '{file}'");
                continue;
            }

            Console.WriteLine($"Processing '{file}'");
            try
            {
                PushCommand.Run(file, context, skipPrompts: skipPrompts, autoCreateMissingNamespace: autoCreateMissingNamespaces);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing '{file}': {ex.Message}");
                Console.WriteLine("Continuing with next file...");
            }
        }
    }
}
