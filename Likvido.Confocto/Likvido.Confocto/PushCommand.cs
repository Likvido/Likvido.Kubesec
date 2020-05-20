namespace Likvido.Confocto
{
    using System;

    public static class PushCommand
    {
        public static void Run(string context, string file)
        {
            Console.WriteLine($"Push called with context '{context}' and file '{file}'");
            Console.WriteLine("kthxbye");
        }
    }
}
