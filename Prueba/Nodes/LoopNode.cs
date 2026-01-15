public class LoopNode : StatementNode
{
    public AstNode? Init; // VariableNode o SetNode, LoopInit
    public ExpressionNode Condition = null!;
    public SetNode Iteration = null!; //accionLoop
    public BlockNode Body = null!;
}