namespace KBMS.Parser;

/// <summary>
/// All token types for KBQL (KBDDL + KBDML)
/// </summary>
public enum TokenType
{
    // End of file
    EOF,

    // Literals
    IDENTIFIER,     // concept_name, kb_name, variable, etc.
    NUMBER,         // 123, 3.14, -5
    STRING,         // 'hello', "description"
    BOOLEAN,        // true, false

    // Keywords - DDL
    CREATE,         // CREATE
    DROP,           // DROP
    USE,            // USE
    ADD,            // ADD
    REMOVE,         // REMOVE

    // Keywords - Objects
    KNOWLEDGE,      // KNOWLEDGE
    BASE,           // BASE
    CONCEPT,        // CONCEPT
    RULE,           // RULE
    RELATION,       // RELATION
    OPERATOR,       // OPERATOR
    FUNCTION,       // FUNCTION
    USER,           // USER

    // Keywords - Plural objects (for SHOW commands)
    CONCEPTS,       // CONCEPTS
    RULES,          // RULES
    RELATIONS,      // RELATIONS
    OPERATORS,      // OPERATORS
    FUNCTIONS,      // FUNCTIONS
    USERS,          // USERS

    // Keywords - DML
    SELECT,         // SELECT
    INSERT,         // INSERT
    UPDATE,         // UPDATE
    DELETE,         // DELETE
    SOLVE,          // SOLVE
    SHOW,           // SHOW

    // Keywords - Clauses
    WHERE,          // WHERE
    FROM,           // FROM
    INTO,           // INTO
    TO,             // TO
    OF,             // OF
    VALUES,         // VALUES
    SET,            // SET
    JOIN,           // JOIN
    ON,             // ON
    ORDER,          // ORDER
    BY,             // BY
    GROUP,          // GROUP
    HAVING,         // HAVING
    LIMIT,          // LIMIT
    OFFSET,         // OFFSET
    AS,             // AS

    // Keywords - Concept definition
    VARIABLES,      // VARIABLES
    ALIASES,        // ALIASES
    BASE_OBJECTS,   // BASE_OBJECTS
    CONSTRAINTS,    // CONSTRAINTS
    SAME_VARIABLES, // SAME_VARIABLES
    VARIABLE,       // VARIABLE (singular)

    // Keywords - Hierarchy
    IS_A,           // IS_A
    PART_OF,        // PART_OF
    HIERARCHY,      // HIERARCHY

    // Keywords - Relation/Function/Operator
    PARAMS,         // PARAMS
    RETURNS,        // RETURNS
    BODY,           // BODY
    PROPERTIES,     // PROPERTIES

    // Keywords - Computation
    COMPUTATION,    // COMPUTATION
    FORMULA,        // FORMULA
    COST,           // COST

    // Keywords - Rule
    TYPE,           // TYPE
    SCOPE,          // SCOPE
    IF,             // IF
    THEN,           // THEN

    // Keywords - User/Privilege
    PASSWORD,       // PASSWORD
    ROLE,           // ROLE
    SYSTEM_ADMIN,   // SYSTEM_ADMIN
    GRANT,          // GRANT
    REVOKE,         // REVOKE
    PRIVILEGES,     // PRIVILEGES
    IN,             // IN

    // Keywords - Solve
    FOR,            // FOR
    GIVEN,          // GIVEN
    USING,          // USING

    // Keywords - Aggregation
    COUNT,          // COUNT
    SUM,            // SUM
    AVG,            // AVG
    MAX,            // MAX
    MIN,            // MIN

    // Keywords - Logical
    AND,            // AND
    OR,             // OR
    NOT,            // NOT

    // Keywords - Other
    DESCRIPTION,    // DESCRIPTION
    ASC,            // ASC
    DESC,           // DESC

    // Data Types - Numeric
    TINYINT,
    SMALLINT,
    INT,
    BIGINT,
    FLOAT,
    DOUBLE,
    DECIMAL,

    // Data Types - String
    VARCHAR,
    CHAR,
    TEXT,

    // Data Types - Boolean
    BOOLEAN_TYPE,   // BOOLEAN (type)

    // Data Types - Date/Time
    DATE,
    DATETIME,
    TIMESTAMP,

    // Data Types - Reference
    OBJECT_TYPE,    // object (type)

    // Operators - Arithmetic
    PLUS,           // +
    MINUS,          // -
    STAR,           // *
    SLASH,          // /
    CARET,          // ^
    PERCENT,        // %

    // Operators - Comparison
    EQUALS,         // =
    NOT_EQUALS,     // <> or !=
    GREATER,        // >
    LESS,           // <
    GREATER_EQUAL,  // >=
    LESS_EQUAL,     // <=

    // Punctuation
    LPAREN,         // (
    RPAREN,         // )
    LBRACKET,       // [
    RBRACKET,       // ]
    LBRACE,         // {
    RBRACE,         // }
    COMMA,          // ,
    SEMICOLON,      // ;
    COLON,          // :
    DOT,            // .

    // Special
    NULL_TOKEN,     // null
    COMMENT,        // -- comment
    UNKNOWN         // Unknown token
}
