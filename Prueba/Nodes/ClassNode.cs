public class ClassNode: AstNode
{
    public string ClassName = null!; //variable

    //declaraxcion de variables y //declaracion de funciones
    public List<DeclarationNode> Members{get;} = []; //miembros de la declaracion de la clase classMember
    
    
}