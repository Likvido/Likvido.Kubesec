namespace Likvido.Confocto
{
    using System;

    public static class PushCommand
    {
        public static void Run(string file, string context, string secret)
        {
            Console.WriteLine($"Push called with context '{context}' and file '{file}'");
            Console.WriteLine("kthxbye");
        }
    }
}
