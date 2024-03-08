namespace Likvido.Kubesec;

public static class RestoreCommand
{
    public static int Run(string folder, string? context, bool recursive, bool skipPrompts = false, bool autoCreateMissingNamespaces = false)
    {
        RestoreFiles(folder, context, skipPrompts, autoCreateMissingNamespaces);

        if (recursive)
        {
            RecursiveRestore(folder, context, skipPrompts, autoCreateMissingNamespaces);
        }

        return 0;
    }

    private static void RecursiveRestore(string folder, string? context, bool skipPrompts, bool autoCreateMissingNamespaces)
    {
        foreach (var directory in Directory.EnumerateDirectories(folder))
        {
            RestoreFiles(directory, context, skipPrompts, autoCreateMissingNamespaces);
            RecursiveRestore(directory, context, skipPrompts, autoCreateMissingNamespaces);
        }
    }

    private static void RestoreFiles(string folder, string? context, bool skipPrompts, bool autoCreateMissingNamespaces)
    {
        foreach (var file in Directory.EnumerateFiles(folder))
        {
            Console.WriteLine($"Processing '{file}'");
            PushCommand.Run(file, context, skipPrompts: skipPrompts, autoCreateMissingNamespace: autoCreateMissingNamespaces);
        }
    }
}
