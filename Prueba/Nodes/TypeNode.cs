public class TypeNode : AstNode
{
    public string Name = null!; // "i", "f", "b", "s" o identificador
    //public TypeNode Type = null!;
    public bool IsArray = false;          // si tiene []
    public ExpressionNode? ArraySize = null; // si el array tiene tamaño (ej: [5])
    public bool IsNullable = false; // si terminó en '?'
    
}
