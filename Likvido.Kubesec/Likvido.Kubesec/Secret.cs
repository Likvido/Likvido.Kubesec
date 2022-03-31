namespace Likvido.Kubesec;

public class Secret
{
    public Secret(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public string Value { get; }
}