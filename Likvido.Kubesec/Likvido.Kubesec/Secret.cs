namespace Likvido.Kubesec;

public class Secret(string name, string value)
{
    public string Name { get; } = name;

    public string Value { get; set; } = value;
}
