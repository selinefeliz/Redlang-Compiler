grammar Redlang;

// ============================================================================
// REGLAS DEL PARSER (SINTAXIS)
// ============================================================================

/* Regla principal del programa.*/
program : (use_stmt | clase_decl)* EOF;
// ESTRUCTURA DE CLASES
use_stmt : USE IDENT SEMI_COLON;

/*Declaracion de una clase */
clase_decl : OBJECT IDENT O_BRACES classMember* C_BRACES;

classMember : declare_stmt | func_decl | entry_func_decl;

// FUNCIONES
func_decl : FUNC IDENT O_PAREN param_list? C_PAREN COLON data_type block;

entry_func_decl : ENTRY func_decl;

param_list : param (COMMA param)*;

param : IDENT COLON data_type;

// BLOQUES Y statementS
block : O_BRACES statement* C_BRACES;

statement : declare_stmt
    | set_stmt
    | return_stmt
    | stmt_control
    | func_call SEMI_COLON
    | member_access SEMI_COLON;

// DECLARACIONES Y ASIGNACIONES
declare_stmt : DECLARE IDENT COLON (data_type | IDENT?) (EQUAL expression)? SEMI_COLON ;

set_stmt : SET assign_target EQUAL expression SEMI_COLON;

assign_target : IDENT | array_access | member_access ;

return_stmt : GIVES expression? SEMI_COLON;

// ESTRUCTURAS DE CONTROL (check, loop, repeat) 
stmt_control : check_stmt | loop_stmt | repeat_stmt;
    
check_stmt : CHECK O_PAREN expression C_PAREN block otherwiseOpcional?;

otherwiseOpcional : OTHERWISE block ;

/* Bucle tipo "for"*/
loop_stmt : LOOP O_PAREN loopInit SEMI_COLON expression SEMI_COLON accionLoop C_PAREN block;

/*Inicializacion del bucle "loop"*/
loopInit : decl_head (EQUAL expression)? | accionLoop;

/* Accion final de cada iteracion del bucle*/
accionLoop : SET assign_target EQUAL expression;

/*Bucle tipo "repeat" (while-like)*/
repeat_stmt : REPEAT O_PAREN expression C_PAREN block;

decl_head : DECLARE IDENT COLON (data_type | IDENT?);

// TIPOS DE DATOS E INICIALIZADORES
data_type : type_base array_specifier? QUESTION? ;

type_base
    : TYPE_I | TYPE_F | TYPE_B | TYPE_S | IDENT ;

array_specifier : O_BRACKETS expression? C_BRACKETS;

// EXPRESIONES Y OPERADORES

expression
    : expression OR expression          # LogicalOr
    | expression AND expression         # LogicalAnd
    | NOT expression                    # LogicalNot
    | expression comparator expression  # Relational
    | expression (PLUS | MINUS) expression # AddSub
    | expression (MULTIPLY | DIVIDE | MODULO) expression # MulDiv
    | MINUS expression                  # UnaryMinus
    | factor                            # Atom
    ;

comparator : EQ | NEQ | GTE | LTE | GTHAN | LTHAN;

factor
    : IDENT
    | literal
    | array_access
    | member_access
    | func_call
    | O_PAREN expression C_PAREN
    ;
// LITERALES Y ARREGLOS
literal : BOOL | FLOAT | INT | STRING | NULL | array_literal ;

array_literal : O_BRACKETS (arg_list)? C_BRACKETS;

// ACCESO A ARREGLOS, MIEMBROS Y LLAMADAS A FUNCIONES
array_access : IDENT O_BRACKETS expression C_BRACKETS;

member_access : IDENT DOT IDENT | IDENT DOT func_call;

func_call
    : (ASK | SHOW | LEN | FILE_OP | CONVERT_OP) O_PAREN arg_list? C_PAREN
    | IDENT O_PAREN arg_list? C_PAREN;

arg_list : expression (COMMA expression)*;

// ============================================================================
// REGLAS DEL LEXER (TOKENS)
// ============================================================================

/* Palabras reservadas del lenguaje */
FUNC: 'func';
ENTRY: 'entry';
DECLARE: 'declare';
SET: 'set';
OBJECT: 'object';
USE: 'use';
CHECK: 'check';
OTHERWISE: 'otherwise';
LOOP: 'loop';
REPEAT: 'repeat';
GIVES: 'gives';
AND: 'and';
OR: 'or';
NOT: 'not';
NULL: 'null';

/* Tipos primitivos soportados*/
TYPE_I: 'i';
TYPE_F: 'f';
TYPE_B: 'b';
TYPE_S: 's';

/* Funciones integradas (built-in)*/
ASK: 'ask';
SHOW: 'show';
LEN: 'len';
CONVERT_OP: 'convertToInt' | 'convertToFloat' | 'convertToBoolean';
FILE_OP: 'readfile' | 'writefile';

/*Literales: booleanos, numÃ©ricos y cadenas*/
BOOL: 'true' | 'false';
FLOAT: '-'? [0-9]+ '.' [0-9]+;
INT: '-'? [0-9]+;
STRING: '"' (ESCAPE_SEQ | ~('\\'|'"'))* '"';
fragment ESCAPE_SEQ: '\\' [btn"\\'];

/*Identificadores de nombres (variables, funciones, clases)*/
IDENT: ID;
ID: [a-zA-Z_] [a-zA-Z0-9_]*;

/*Signos, operadores y puntuaciÃ³n del lenguaje*/
COMMA: ',';
DOT: '.';
COLON: ':';
SEMI_COLON: ';';
EQUAL: '=';
QUESTION: '?';
UNDERSCORE: '_';

O_PAREN: '(';
C_PAREN: ')';
O_BRACKETS: '[';
C_BRACKETS: ']';
O_BRACES: '{';
C_BRACES: '}';

PLUS: '+';
MINUS: '-';
MULTIPLY: '*';
DIVIDE: '/';
MODULO: '%';

LTHAN: '<';
GTHAN: '>';
LTE: '<=';
GTE: '>=';
EQ: '==';
NEQ: '!=';

/* Espacios en blanco (ignorados por el analizador lexico).*/
WS: [ \t\r\n]+ -> skip;