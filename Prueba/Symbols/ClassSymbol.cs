public class ClassSymbol : Symbol
    {
        public SymbolTable ClassScope { get; set; } = null!;
        public Dictionary<string, VariableSymbol> Fields { get; set; } = [];
        public Dictionary<string, FunctionSymbol> Methods { get; set; } = [];
    }