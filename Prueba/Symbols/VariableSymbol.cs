public class VariableSymbol : Symbol
{
    public string Type { get; set; } = null!;
    public bool Nullable { get; set; }
    public bool IsArray { get; set; }
    public int? ArraySize { get; set; }
}