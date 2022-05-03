namespace Likvido.Kubesec;

public static class RestoreCommand
{
    public static int Run(string folder, string? context, bool recursive)
    {
        RestoreFiles(folder, context);

        if (recursive)
        {
            RecursiveRestore(folder, context);
        }

        return 0;
    }

    private static void RecursiveRestore(string folder, string? context)
    {
        foreach (var directory in Directory.EnumerateDirectories(folder))
        {
            RestoreFiles(directory, context);
            RecursiveRestore(directory, context);
        }
    }

    private static void RestoreFiles(string folder, string? context)
    {
        foreach (var file in Directory.EnumerateFiles(folder))
        {
            Console.WriteLine($"Processing '{file}'");
            PushCommand.Run(file, context, null, null);
        }
    }
}