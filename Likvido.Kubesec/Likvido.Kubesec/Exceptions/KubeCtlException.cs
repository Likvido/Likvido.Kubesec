namespace Likvido.Kubesec.Exceptions;

public class KubeCtlException : Exception
{
    public KubeCtlException(string message)
        : base(message)
    {
    }
}