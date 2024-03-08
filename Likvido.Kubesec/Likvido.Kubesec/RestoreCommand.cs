namespace Likvido.Kubesec;

public static class RestoreCommand
{
    public static int Run(string folder, string? context, bool recursive, bool skipPrompts = false)
    {
        RestoreFiles(folder, context, skipPrompts);

        if (recursive)
        {
            RecursiveRestore(folder, context, skipPrompts);
        }

        return 0;
    }

    private static void RecursiveRestore(string folder, string? context, bool skipPrompts)
    {
        foreach (var directory in Directory.EnumerateDirectories(folder))
        {
            RestoreFiles(directory, context, skipPrompts);
            RecursiveRestore(directory, context, skipPrompts);
        }
    }

    private static void RestoreFiles(string folder, string? context, bool skipPrompts)
    {
        foreach (var file in Directory.EnumerateFiles(folder))
        {
            Console.WriteLine($"Processing '{file}'");
            PushCommand.Run(file, context, skipPrompts: skipPrompts);
        }
    }
}