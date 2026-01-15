//abstracto para poner dos tipos de funciones
//aqui defino si es una declaracion de variable o de funcion
public abstract class DeclarationNode : StatementNode  //ASTNODE
{
    //lo que tiene en comun ambas
    public string Name = null!;
    public TypeNode Type = null!;

}