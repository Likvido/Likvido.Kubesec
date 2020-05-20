namespace Likvido.Confocto
{
    using System;

    public static class PullCommand
    {
        public static void Run(string context, string file)
        {
            Console.WriteLine($"Pull called with context '{context}' and file '{file}'");
            Console.WriteLine("kthxbye");
        }
    }
}
