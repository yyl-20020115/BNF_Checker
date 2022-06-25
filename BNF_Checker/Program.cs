//bnf_chk - EBNF syntax checker and formatter
//Copyright 2006 by icosaedro.it di Umberto Salsi <salsi @icosaedro.it>
//Info: http://www.icosaedro.it/bnf_chk/index.html

using System.Linq;

var VERSION = "1.4";
var DEBUG = false;

var FileName = "";
//Preferences:
var DoReport = false;
var DoPrintText = false;
var DoPrintHtml = false;
var DoSort = false;
var DoPrintIndex = false;
var TabSize = 4;
var PrintFileName = false;
var PrintSource = false;
var PrintContext = false;
var WarnInvalidID = false; //using <ID> in place of ID
var WarnInvalidRuleTerminator = false; //using . in place of ;
var WarnInvalidEq = false; // # using := or ::= in place of =
var WarnUnquatedLitString = false;

var IdsList = new List<Identifier>();
var DeclarationsList = new List<Declaration>();

var TextReader = default(StreamReader);
var Line = ""; //current line being parsed
var Line_N = 0; //current line no.First line is the no. 1 
var Line_Index = 0; //offset of the current char in line
var Line_Pos = 0; //text editor idea of the position of the current char *
var CurrentChar = '\0'; //current char to be parsed, or NIL if end of the file 
var CurrentSymbol = Symbol.EOF; //current symbol
var CurrentString = ""; //ID or literal string, depending on the value of 's'

string String2HTML(string s)
    => s.Replace("&", "&amp;")
    .Replace("<", "&lt;")
    .Replace(">", "&gt;")
    .Replace("\n", "<br>\n")
    ;
void Help()
{
    Prints(
        "bnf_chk " + VERSION + " - EBNF syntax checker\n" +
        "Usage: bnf_chk {OPTION} file\n" +
        "\n" +
        "Available options:\n" +
        "\t--report       Report unused/undeclared ids\n" +
        "\t--sort         Sort declarations\n" +
        "\t--print-text   Print decls in ASCII\n" +
        "\t--print-html   Print decls in HTML\n" +
        "\t--print-index  Print index numbers of decls\n" +
        "\t--no-print-file-name Do not print file name in error msgs\n" +
        "\t--print-source Print source with line numbers\n" +
        "\t--print-context  Print error context\n" +
        "\t--help         Displays this help\n" +
        "\n");
}
void Version()
{
    PrintText("bnf_chk " + VERSION + " - EBNF syntax checker\n");
    PrintText("Copyright 2006 by icosaedro.it di Umberto Salsi <salsi@icosaedro.it>\n");
    PrintText("More info: http://www.icosaedro.it/bnf_chk/index.html\n");
}
void ReportIds()
{
    PrintText("IDs report:\n");
    for (var i = 0; i < IdsList!.Count; i++)
    {
        var id = IdsList[i];
        //if (!id.IsDefined || id.Used == 0)
        {
            PrintText(id.Name + ": ");
            if (id.IsDefined)
            {
                PrintText("defined in line " + id.IsInlineDefined + ", ");
            }
            else
            {
                PrintText("NOT DEFINED, ");
            }
            if (id.Used > 0)
            {
                PrintText("used " + (id.Used) + " times\n");
            }
            else
            {
                PrintText("NOT USED\n");
            }
        }
    }
}
int Print_(char c)
{
    Console.Write(c);
    return 0;
}
int Prints(string s)
{
    Console.Write(s);
    return 0;
}
int Message(string message_type, string s)
{
    if (PrintContext)
    {
        PrintText("\n\t" + Line + "\n\t" + new string(' ', Line_Pos - 1) + "\\_ HERE\n");
    }
    if (!string.IsNullOrEmpty(FileName) && PrintFileName)
    {
        PrintText(FileName + ": ");
        PrintText("" + Line_N + ":" + Line_Pos + ": ");
    }
    if (DoPrintHtml)
    {
        Prints("<b>");
    }
    PrintText(message_type + ": " + s + "\n");
    if (DoPrintHtml)
    {
        Prints("</b>");
    }
    return 0;
}
int Exit(int n)
{
    Environment.Exit(n);
    return n;
}
int Warning(string text) => Message("Warning", text);
int Error(string text) => Message("ERROR", text);
int Fatal(string text)
{
    var r = Message("FATAL", text);
    return Exit(1);
}
void PrintText(string text)
{
    if (DoPrintHtml)
    {
        text = String2HTML(text)
            .Replace("\t", "    ")
            .Replace(" ", "&nbsp;")
            ;
        text = "<code>" + text + "</code>";
    }
    Console.Write(text);
}
void PrintLineSource()
{
    PrintText(Line_N.ToString());
    PrintText(":\t");
    PrintText(Line??String.Empty);
    PrintText("\n");
}
char ReadChar()
{
    //update line_n, line_pos:
    if (CurrentChar == '\0')
    {

    }
    else if (CurrentChar == '\t')
    {
        Line_Pos += TabSize;
    }
    else if (CurrentChar == '\n')
    {
        Line_N++;
        Line_Pos = 1;
    }
    else
    {
        Line_Pos++;
    }

    //get next char 'c' and update line_idx:
    if (Line == null)
    {
        CurrentChar = '\0';
    }
    else if (Line_Index < Line.Length)
    {
        CurrentChar = Line[Line_Index];
        Line_Index++;
    }
    else if (Line_Index == Line.Length)
    {
        CurrentChar = '\n';
        Line_Index++;
    }
    else
    {
        Line = TextReader?.ReadLine();
        if (Line == null)
        {
            CurrentChar = '\0';
        }
        else if (Line.Length == 0)
        {
            CurrentChar = '\n';
        }
        else
        {
            CurrentChar = Line[0];
        }
        Line_Index = 1;
        if (PrintSource && (!string.IsNullOrEmpty(Line)))
        {
            PrintLineSource();
        }
    }
    if (DEBUG)
    {
        Print_(CurrentChar);
    }
    return CurrentChar;
}
Symbol ReadSymbol()
{
    while ((CurrentChar == ' ') || (CurrentChar == '\t') || (CurrentChar == '#') || (CurrentChar == '\n') || (CurrentChar == '\r'))
    {
        if (CurrentChar == '#')
        {
            while (true)
            {
                ReadChar();
                if (CurrentChar == '\n' || CurrentChar == '\0')
                {
                    break;
                }
            }
        }
        else
        {
            ReadChar();
        }
    }
    if (CurrentChar == '\0')
    {
        CurrentSymbol = Symbol.EOF;
    }
    else if (IsIdStartLetter(CurrentChar))
    {
        CurrentString = CurrentChar.ToString();
        while (true)
        {
            ReadChar();
            if (IsIdLetter(CurrentChar))
            {
                CurrentString += CurrentChar;
            }
            else
            {
                break;
            }
        }
        if (WarnInvalidID)
        {
            if (!WarnUnquatedLitString)
            {
                WarnUnquatedLitString = true;
                Warning("guessed usage of the syntax style <ID> for IDs and STRING for literal strings -- trying to continue anyway");
            }
            CurrentString = "\"" + CurrentString + "\"";
            CurrentSymbol = Symbol.String;
        }
        else
        {
            CurrentSymbol = Symbol.Name;
        }
    }
    else if (CurrentChar == '<')
    {
        CurrentString = "";
        while (true)
        {
            ReadChar();
            if (!IsIdLetter(CurrentChar) || CurrentChar == '-' || CurrentChar == ' ')
            {
                if (CurrentChar == ' ' || CurrentChar == '-') CurrentChar = '_';
                CurrentString += CurrentChar;
            }
            else if (CurrentChar == '>')
            {
                if (!WarnInvalidID)
                {
                    WarnInvalidID = true;
                    Warning("invalid identifier syntax `<SOME ID>' -- please use `SOME_ID' instead");
                }
                ReadChar();
                break;
            }
            else
            {
                Fatal("invalid char in <identifier>");
            }
        }
        CurrentSymbol = Symbol.Name;
    }
    else if (CurrentChar == '=')
    {
        CurrentSymbol = Symbol.Equal;
        ReadChar();
    }
    else if (CurrentChar == ':')
    {
        ReadChar();
        if (CurrentChar == '=')
        {
            WarnInvalidEqFunction(":=");
            ReadChar();
            CurrentSymbol = Symbol.Equal;
        }
        else if (CurrentChar == ':')
        {
            ReadChar();
            if (CurrentChar == '=')
            {
                WarnInvalidEqFunction("::=");
                ReadChar();
                CurrentSymbol = Symbol.Equal;
            }
            else
            {
                Fatal("invalid character or symbol");
            }
        }
        else
            Fatal("invalid character or symbol");
    }
    else if (CurrentChar == '|')
    {
        CurrentSymbol = Symbol.Vbar;
        ReadChar();
    }
    else if (CurrentChar == '{')
    {
        CurrentSymbol = Symbol.LBrace;
        ReadChar();
    }
    else if (CurrentChar == '}')
    {
        CurrentSymbol = Symbol.RBrace;
        ReadChar();
    }
    else if (CurrentChar == '[')
    {
        CurrentSymbol = Symbol.LSquare;
        ReadChar();
    }
    else if (CurrentChar == ']')
    {
        CurrentSymbol = Symbol.RSquare;
        ReadChar();
    }
    else if (CurrentChar == '(')
    {
        CurrentSymbol = Symbol.LRound;
        ReadChar();
    }
    else if (CurrentChar == ')')
    {
        CurrentSymbol = Symbol.RRound;
        ReadChar();
    }
    else if (CurrentChar == ';')
    {
        CurrentSymbol = Symbol.Semicolon;
        ReadChar();
    }
    else if (CurrentChar == '\"')
    {
        CurrentString = ReadLitString();
        CurrentSymbol = Symbol.String;
    }
    else if (CurrentChar == '.')
    {
        ReadChar();
        if (CurrentChar == '.')
        {
            ReadChar();
            CurrentSymbol = Symbol.Range;
        }
        else
        {
            if (!WarnInvalidRuleTerminator)
            {
                WarnInvalidRuleTerminator = true;
                Warning("invalid rule terminator `.' -- please use `;' instead");
            }
            CurrentSymbol = Symbol.Semicolon;
            ReadChar();
        }
    }
    else if (CurrentChar > ' ')
    {
        Error("unexpected character `" + CurrentChar + "' -- trying to continue anyway");
        CurrentString = CurrentChar.ToString();
        ReadChar();

        while (CurrentChar > ' ')
        {
            CurrentString += CurrentChar;

            ReadChar();

        }
        CurrentString = "\"" + CurrentString + "\"";//# FIXME: special chars must be escaped
        CurrentSymbol = Symbol.String;
    }
    else
    {
        Error("unexpected control char -- trying to continue anyway");
        CurrentSymbol = Symbol.String;
    }
    return CurrentSymbol;
}
Identifier AddId(string? name, bool decl)
{
    name ??= "";
    var id = IdsList!.FirstOrDefault(i => i.Name == name);
    if (id == null)
    {
        id = new Identifier
        {
            Name = name
        };
        if (decl)
        {
            id.IsDefined = true;
            id.IsInlineDefined = Line_N;
            id.Used = 0;
        }
        else
        {
            id.IsDefined = false;
            id.IsInlineDefined = 0;
            id.Used++;
        }
        IdsList!.Add(id);
    }
    else
    {
        if (decl)
        {
            if (id.IsDefined)
            {
                Fatal("id `" + name + "' already defined in line "
                    + id.IsInlineDefined);
            }
            else
            {
                //ichiarazione di un ID gia' usato prima: 
                id.IsDefined = true;
                id.IsInlineDefined = Line_N;
            }
        }
        else
        {
            id.Used++;
        }
    }
    return id;
}
int GetHexValue(char c)
    => c >= '0' && c <= '9'
    ? c - '0' : c >= 'A' && c <= 'F'
    ? c - 'A' + 10 : c >= 'a' && c <= 'f' ? c - 'a' + 10
    : Fatal("invalid hexadecimal digit `" + c + "'");
void WarnInvalidEqFunction(string text)
{
    if (!WarnInvalidEq)
    {
        WarnInvalidEq = true;
        Warning("invalid `" + text + "' symbol -- please use `=' instead");
    }
}
void PrintExprAsText(Node node)
{
    if (node.Type == NodeType.Name)
    {
        PrintText(" " + node.Id.Name);
        if (DoPrintIndex)
        {
            int index = node.Id.Index;
            if (index == 0)
                Prints("_?");
            else
                Prints("_" + (index));
        }
    }
    else if (node.Type == NodeType.QuotedName)
        Prints(" " + node.Text);
    else if (node.Type == NodeType.Repeats)
    {
        Prints(" {");
        PrintExprAsText(node.NextNode);
        Prints(" }");
    }
    else if (node.Type == NodeType.Optional)
    {
        Prints(" [");
        PrintExprAsText(node.NextNode);
        Prints(" ]");
    }
    else if (node.Type == NodeType.Product)
    {
        for (int i = 0; i < node.SubNodes.Count; i++)
        {
            if (node.SubNodes[i].Type == NodeType.Alternatives)
            {
                Prints(" (");
                PrintExprAsText(node.SubNodes[i]);
                Prints(" )");
            }
            else
            {
                PrintExprAsText(node.SubNodes[i]);
            }
        }
    }
    else if (node.Type == NodeType.Alternatives)
    {
        for (int i = 0; i < node.SubNodes.Count; i++)
        {
            if (i > 0)
                Prints(" |");
            PrintExprAsText(node.SubNodes[i]);
        }
    }
    else
    {
        Error("internal error (1)\n");
        Exit(1);
    }
}
void PrintExprAsHtml(Node d)
{
    if (d.Type == NodeType.Name)
    {
        Prints(" <i>" + d.Id.Name + "</i>");
        if (DoPrintIndex)
        {
            int index = d.Id.Index;
            if (index == 0)
                Prints("<sub>?</sub>");
            else
                Prints("<sub>" + (index) + "</sub>");
        }
    }
    else if (d.Type == NodeType.QuotedName)
        Prints(" <code><b>" + String2HTML(d.Text) + "</b></code>");
    else if (d.Type == NodeType.Repeats)
    {
        Prints(" {");
        PrintExprAsText(d.NextNode);
        Prints(" }");
    }
    else if (d.Type == NodeType.Optional)
    {
        Prints(" [");
        PrintExprAsText(d.NextNode);
        Prints(" ]");
    }
    else if (d.Type == NodeType.Product)
    {
        for (int i = 0; i < d.SubNodes.Count; i++)
        {
            if (d.SubNodes[i].Type == NodeType.Alternatives)
            {
                Prints(" (");
                PrintExprAsText(d.SubNodes[i]);
                Prints(" )");
            }
            else
            {
                PrintExprAsText(d.SubNodes[i]);
            }
        }
    }
    else if (d.Type == NodeType.Alternatives)
    {
        for (int i = 0; i < d.SubNodes.Count; i++)
        {
            if (i > 0)
                Prints(" |");
            PrintExprAsText(d.SubNodes[i]);
        }
    }
    else
    {
        Error("internal error (1)\n");
        Exit(1);
    }
}
void PrintAsHtml(List<Declaration> declarationsList)
{
    for (int i = 0; i < declarationsList.Count; i++)
    {
        if (DoPrintIndex)
        {
            Prints(i + 1 + ". ");
        }
        var d = declarationsList[i];
        Prints(d.Id.Name);
        Prints(" =");
        PrintExprAsHtml(d.Node);
        Prints(";<br>\n");
    }
}
void PrintAsText(List<Declaration> declarationsList)
{
    for (int i = 0; i < declarationsList.Count; i++)
    {
        if (DoPrintIndex)
        {
            Prints(i + 1 + ". ");
        }
        var d = declarationsList[i];
        Prints(d.Id.Name);
        Prints(" =");
        PrintExprAsText(d.Node);
        Prints(";\n");
    }
}
int DeclComparison(Declaration x, Declaration y) => string.Compare(x.Id.Name, y.Id.Name);
void SortDeclarations(List<Declaration> declarationsList)
{
    declarationsList.Sort(DeclComparison);
    for (int i = 0; i < declarationsList.Count; i++)
    {
        declarationsList[i].Id.Index = i;
    }
}
string ReadLitString()
{
    var s = "\"";
    var d1 = 0;
    var d2 = 0;
    ReadChar();
    while (true)
    {
        switch (CurrentChar)
        {
            case '\0':
                Fatal("unclosed literal string");
                break;
            case '\"':
                {
                    ReadChar();
                    if (s.Length == 1)
                    {
                        Fatal("invalid empty string");
                    }
                    return s + "\"";
                }
            case '\\':
                {
                    s += "\\";
                    ReadChar();
                    if (CurrentChar == '\"' | CurrentChar == '\\')
                    {
                        s += CurrentChar;
                        ReadChar();
                    }
                    else if (CurrentChar == 'a' || CurrentChar == 'b' || CurrentChar == 'n' || CurrentChar == 'r' || CurrentChar == 't')
                    {
                        s += CurrentChar;
                        ReadChar();
                    }
                    else if (CurrentChar == 'x')
                    {
                        s += "x";
                        ReadChar();
                        d1 = GetHexValue(CurrentChar);
                        s += CurrentChar;
                        ReadChar();
                        d2 = GetHexValue(CurrentChar);
                        s += CurrentChar;
                        ReadChar();
                        //s += (char)(d1 << 4 + d2);
                        //s = s + CHR(16*d1+d2)
                    }
                    else
                    {
                        Fatal("invalid escape sequence \"\\" + CurrentChar + "\"");
                    }
                }
                break;
            default:
                if (CurrentChar >= ' ' && CurrentChar != 127)
                {
                    s += CurrentChar;
                    ReadChar();
                }
                else
                {
                    Error("invalid control character code " + CurrentChar + " in literal string -- possible unclosed string");
                    ReadChar();
                    return s + "\"";

                }
                break;
        }
    }
}
Declaration ReadDeclaration()
{
    var declaration = new Declaration();

    if (CurrentSymbol != Symbol.Name)
        Fatal("expected rule name");

    declaration.Id = AddId(CurrentString, true);
    ReadSymbol();
    if (CurrentSymbol != Symbol.Equal)
        Fatal("expected `='");

    ReadSymbol();
    declaration.Node = ReadExpr();
    if (CurrentSymbol != Symbol.Semicolon)
        Error("unexpected symbol -- check possible missing `;' in the rule above");
    ReadSymbol();
    return declaration;
}
List<Declaration> ReadDeclarations()
{
    Declaration declaration = new();

    Line_N = 1;
    Line_Pos = 0;
    Line = TextReader.ReadLine();

    CurrentChar = '\0';
    if (PrintSource)
        PrintLineSource();

    ReadChar();
    ReadSymbol();
    int index = 0;
    while (CurrentSymbol != Symbol.EOF)
    {
        declaration = ReadDeclaration();
        index++;
        declaration.Id.Index = index;
        DeclarationsList!.Add(declaration);
    }
    return DeclarationsList!;
}
Node? ReadExpr()
{
    var s = new Node();
    var e = ReadTerminal();
    if (CurrentSymbol == Symbol.Vbar)
    {
        s.Type = NodeType.Alternatives;
        s.SubNodes.Add(e);
        e = s;
    }
    while (CurrentSymbol == Symbol.Vbar)
    {
        ReadSymbol();
        e.SubNodes.Add(ReadTerminal());
    }
    return e;
}
Node? ReadFactor()
{
    var f = new Node();
    if (CurrentSymbol == Symbol.LRound)
    {
        ReadSymbol();
        f = ReadExpr();
        if (CurrentSymbol != Symbol.RRound)
        {
            Fatal("expected `)'");
        }
        ReadSymbol();
    }
    else if (CurrentSymbol == Symbol.LSquare)
    {
        f.Type = NodeType.Optional;
        ReadSymbol();
        f.NextNode = ReadExpr();
        if (CurrentSymbol != Symbol.RSquare)
            Fatal("expected `]'");
        ReadSymbol();
    }
    else if (CurrentSymbol == Symbol.LBrace)
    {
        f.Type = NodeType.Repeats;
        ReadSymbol();
        f.NextNode = ReadExpr();
        if (CurrentSymbol != Symbol.RBrace)
            Fatal("expected `}'");
        ReadSymbol();
    }
    else if (CurrentSymbol == Symbol.Name)
    {
        f.Type = NodeType.Name;
        f.Id = AddId(CurrentString, false);
        ReadSymbol();
    }
    else if (CurrentSymbol == Symbol.String)
    {
        f.Type = NodeType.QuotedName;
        //f[s] = StringToLiteral(s)
        f.Text = CurrentString??"";
        ReadSymbol();
        if (CurrentSymbol == Symbol.Range)
        {
            ReadSymbol();
            if (CurrentSymbol != Symbol.String)
                Fatal("expected string after range separator");
            //##f[s] = f[s] + ".." + StringToLiteral(s)
            f.Text += (".." + CurrentString);
            ReadSymbol();
        }
    }
    else
        return null;
    return f;
}
Node ReadTerminal()
{
    Node? f = null;
    Node? p1 = null;
    Node? p2 = new();

    //Il primo fattore e' obbligatorio: 
    p1 = ReadFactor();
    if (p1 == null)
        Fatal("expected factor");
    //Se c'e' un secondo fattore, allora e' un prodotto: 
    f = ReadFactor();
    if (f == null)
        return p1!;

    p2.Type = NodeType.Product;
    p2.SubNodes.Add(p1!);
    p2.SubNodes.Add(f!);

    p1 = p2;
    //Vedi se ci sono altri fattori: 
    while (true)
    {
        f = ReadFactor();
        if (f == null)
            return p1;
        p1.SubNodes.Add(f);

    }
}
bool IsIdLetter(char c)
    => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_' || c >= '0' && c <= '9';
bool IsIdStartLetter(char c)
    => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_';
int Main(string[] args)
{
    //Default preferences:
    TabSize = 8;
    DoSort = false;
    DoReport = false;
    DoPrintText = false;
    DoPrintHtml = false;
    PrintFileName = true;
    PrintSource = false;
    PrintContext = false;

    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--help":
                Help();
                break;
            case "--version":
                Version();
                break;
            case "--sort":
                DoSort = true;
                break;
            case "--report":
                DoReport = true;
                break;
            case "--print-text":
                DoPrintText = true;
                break;
            case "--print-html":
                DoPrintHtml = true;
                break;
            case "--print-index":
                DoPrintIndex = true;
                break;
            case "--no-print-file-name":
                PrintFileName = false;
                break;
            case "--print-source":
                PrintSource = true;
                break;
            case "--print-context":
                PrintContext = true;
                break;
            default:
                if (args[i].Length > 0 && args[i][0] == '-')
                {
                    Error("unknown option " + args[i] + "\n");
                    Exit(1);
                }
                else
                {
                    if (!string.IsNullOrEmpty(FileName))
                    {
                        Error("parameter " + args[i]
                        + ": file name already set to " + FileName + "\n");
                        Error("Please type \"bnf_chk --help\" for a complete list of the available options\n");
                        Exit(1);
                    }
                    FileName = args[i];
                }
                break;
        }

    }
    if (string.IsNullOrEmpty(FileName))
    {
        //Error("required file name\n");
        Help();
        Exit(1);
    }
    IdsList.Clear();
    DeclarationsList.Clear();

    using (TextReader = File.OpenText(FileName))
    {
        ReadDeclarations();
    }
    if (DoReport)
    {
        ReportIds();
    }
    if (DoSort)
    {
        SortDeclarations(DeclarationsList);
    }
    if (DoPrintText)
    {
        PrintAsText(DeclarationsList);
    }
    if (DoPrintHtml)
    {
        PrintAsHtml(DeclarationsList);
    }
    return 0;
}
//Entry Point
return Main(args);
public class Identifier
{
    public string Name = "";
    public bool IsDefined = false;
    public int IsInlineDefined = 0;
    public int Index = 0;
    public int Used = 0;
    public override string ToString() => $"{Name}({IsInlineDefined},{Index},{Used})";
}
public enum NodeType : uint
{
    Name = 0,//rule name - ex. A
    QuotedName = 1,//literal string - ex. "PRINT"
    Product = 2,//product of factors - ex. A B C
    Alternatives = 3,//alternatives terms - ex. A|B|C
    Repeats = 4,//zero or more times - ex. {A}
    Optional = 5,//optional - ex. [A]
}
public class Node
{
    public Identifier Id = new();
    public NodeType Type = NodeType.Name;
    public string Text = "";
    public Node? NextNode =null;
    public List<Node> SubNodes = new();
    public string SubNodeTexts => string.Join(',', (IEnumerable<Node>)this.SubNodes.ToArray());
    public override string ToString() => $"{Id}-{Text}({Type})[{this.SubNodeTexts}]";
}
public class Declaration
{
    public Identifier Id = new();
    public Node Node = new();
    public override string ToString() => $"{this.Id} : {this.Node}";
}
public enum Symbol : uint
{
    EOF = 0,
    Semicolon = 1,
    Name = 2,
    Equal = 3,
    LBrace = 4,
    RBrace = 5,
    LSquare = 6,
    RSquare = 7,
    LRound = 8,
    RRound = 9,
    Vbar = 10,
    String = 11,
    Range = 12,
}
