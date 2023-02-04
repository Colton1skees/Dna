grammar KGram;

expression: BinaryComplement expression #BinaryComplementExpression
            | 'ExprOp' LPARAM '"-"' COMMA expression RPARAM # NegateExpression
            | 'ExprOp' LPARAM '"*"' COMMA expression COMMA expression RPARAM # MulExpression
            | 'ExprOp' LPARAM '"+"' COMMA expression COMMA expression RPARAM # AddExpression
            | 'ExprOp' LPARAM '"&"' COMMA expression COMMA expression RPARAM # AndExpression
            | 'ExprOp' LPARAM '"|"' COMMA expression COMMA expression RPARAM # OrExpression
            | 'ExprOp' LPARAM '"^"' COMMA expression COMMA expression RPARAM # XorExpression
            | 'ExprOp' LPARAM '"<<"' COMMA expression COMMA expression RPARAM # LeftShiftExpression
            | 'ExprSlice' LPARAM expression COMMA NUMBER COMMA NUMBER RPARAM #SliceExpression
            | 'ExprId' LPARAM STRING COMMA NUMBER RPARAM #IdExpression
            | 'ExprInt' LPARAM NUMBER COMMA NUMBER RPARAM #IntExpression;


// Expression constructs
LPARAM      : '(';
RPARAM      : ')';
COMMA       : ',';
BinaryComplement: '~';

STRING      : ('"' ~["]* '"') | '%' STRING;
NUMBER      : [0-9]+;
WS          : [ \t\r\n]+ -> skip ;
root: expression;