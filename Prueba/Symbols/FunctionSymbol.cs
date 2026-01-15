public class FunctionSymbol : Symbol
    {
        public List<VariableSymbol> Parameters { get; set; } = [];
        public string ReturnType { get; set; } = null!;
        public bool NullableType { get; set; }
        public bool IsEntry { get; set; }
        public ClassSymbol? ParentClass { get; set; }
    }