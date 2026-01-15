public class SymbolTable
{
    // Constructor
    public SymbolTable(SymbolTable? parent = null)
    {
        Parent = parent;
    }

    // Table tree
    // public Dictionary<string, Symbol> Symbols { get; } = [];
    public Dictionary<string, Symbol> Symbols { get; } = new Dictionary<string, Symbol>();
    public SymbolTable? Parent { get; }

    // Methods

    public void Add(Symbol symbol)
    {
        if (!Symbols.TryAdd(symbol.Name, symbol))
            throw new Exception($"{symbol.Name} is already defined in this scope");
    }

    public Symbol? Lookup(string name) => Symbols.TryGetValue(name, out Symbol? value) ? value : Parent?.Lookup(name);

    // Imprimir tabla de símbolos de forma recursiva
    public void Print(int indent = 0)
    {
        string padding = new string(' ', indent * 2);
        foreach (var sym in Symbols.Values)
        {
            Console.WriteLine($"{padding}{sym.Name} : {sym.GetType().Name}");
            if (sym is ClassSymbol cls)
                cls.ClassScope.Print(indent + 1);
        }
     }
}



