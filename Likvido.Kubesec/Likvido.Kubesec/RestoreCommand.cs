namespace Likvido.Kubesec;

public static class RestoreCommand
{
    public static int Run(string folder, string context)
    {
        var files = Directory.GetFiles(folder);

        foreach (var file in files)
        {
            Console.WriteLine($"Processing '{file}'");
            PushCommand.Run(file, context, null, null);
        }

        return 0;
    }
}