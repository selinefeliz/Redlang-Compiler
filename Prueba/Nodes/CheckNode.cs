public class CheckNode : StatementNode
{
    public ExpressionNode Condition = null!;
    public BlockNode ThenBlock = null!;
    public BlockNode? ElseBlock;  // opcional (otherwiseOpcional)
}