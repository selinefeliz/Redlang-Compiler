public class FunctionNode : DeclarationNode
{
    public bool IsEntry; //si es una funcion de entrada o no
    public List<ParameterNode>? Parameters = [];
    public BlockNode Body = null!;
    
}