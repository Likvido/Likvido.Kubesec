namespace Likvido.Kubesec.Exceptions
{
    using System;

    public class KubeCtlException : Exception
    {
        public KubeCtlException(string message)
            : base(message)
        {
        }
    }
}
