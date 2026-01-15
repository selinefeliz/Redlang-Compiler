public class SetNode : StatementNode
{
    public AssignTargetNode Target = null!;
    public ExpressionNode Value = null!;
}