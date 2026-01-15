public class FuncCallNode : ExpressionNode
{
    public string FunctionName = null!;
    public List<ExpressionNode> Arguments { get; } = [];
}