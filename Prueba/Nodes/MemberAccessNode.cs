public class MemberAccessNode : ExpressionNode
{
    public string ObjectName = null!;
    public string? MemberName;      // si es IDENT . IDENT
    public FuncCallNode? Call;     // si es IDENT . func_call
}