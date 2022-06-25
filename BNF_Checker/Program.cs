//bnf_chk - EBNF syntax checker and formatter
//Copyright 2006 by icosaedro.it di Umberto Salsi <salsi @icosaedro.it>
//Info: http://www.icosaedro.it/bnf_chk/index.html

var VERSION = "1.4";
var DEBUG = false;

var fn = "";
//Preferences:
var do_report = false;
var do_print_text = false;
var do_print_html = false;
var do_sort = false;
var do_print_index = false;
var tab_size = 4;
var print_file_name = false;
var print_source = false;
var print_context = false;
var warn_invalid_id = false; //using <ID> in place of ID
var warn_invalid_rule_terminator = false; //using . in place of ;
var warn_invalid_eq = false; // # using := or ::= in place of =
var warn_invalid_unquoted_lit_str = false;

var ids = new List<Identifier>();
var decls = new List<Decl>();

StreamReader fd = null;
var line = ""; //current line being parsed
var line_n = 0; //current line no.First line is the no. 1 
var line_idx = 0; //offset of the current char in line
var line_pos = 0; //text editor idea of the position of the current char *
var c = '\0'; //current char to be parsed, or NIL if end of the file 
var sym = Symbol.sym_eof; //current symbol
var s = ""; //ID or literal string, depending on the value of 's'

void ids_report()
{
    print_text("IDs report:\n");
    for (var i = 0; i < ids.Count; i++)
    {
        var id = ids[i];
        if (!id.defined || id.used == 0)
        {
            print_text(id.name + ": ");
            if (id.defined)
            {
                print_text("defined in line " + id.defined_in_line + ", ");
            }
            else
            {
                print_text("NOT DEFINED, ");
            }
            if (id.used > 0)
            {
                print_text("used " + (id.used) + " times\n");
            }
            else
            {
                print_text("NOT USED\n");
            }
        }
    }
}
void Help()
{
    prints(
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
    print_text("bnf_chk " + VERSION + " - EBNF syntax checker\n");
    print_text("Copyright 2006 by icosaedro.it di Umberto Salsi <salsi@icosaedro.it>\n");
    print_text("More info: http://www.icosaedro.it/bnf_chk/index.html\n");
}
string String2HTML(string s)
    => s.Replace("&", "&amp;")
    .Replace("<", "&lt;")
    .Replace(">", "&gt;")
    .Replace("\n", "<br>\n")
    ;
int print(char c)
{
    Console.Write(c);
    return 0;
}
int prints(string s)
{
    Console.Write(s);
    return 0;
}
int message(string msg_type, string s)
{
    if (print_context)
    {
        print_text("\n\t" + line + "\n\t" + new string(' ', line_pos - 1) + "\\_ HERE\n");
    }
    if (print_file_name)
    {
        print_text(fn + ": ");

    }
    print_text("" + line_n + ":" + line_pos + ": ");
    if (do_print_html)
    {
        prints("<b>");
    }
    print_text(msg_type + ": " + s + "\n");
    if (do_print_html)
    {
        prints("</b>");
    }
    return 0;
}
int warning(string s)
{
    return message("Warning", s);
}
int exit(int n)
{
    Environment.Exit(n);
    return n;
}
int err(string s)
{
    return message("ERROR", s);
}
int error(string s)
{
    return err(s);
}
int fatal(string s)
{
    var r = message("FATAL", s);
    exit(1);
    return r;
}
void print_text(string s)
{
    if (do_print_html)
    {
        s = String2HTML(s)
            .Replace("\t", "    ")
            .Replace(" ", "&nbsp;")
            ;
        s = "<code>" + s + "</code>";
    }
    Console.WriteLine(s);
}
void PrintLineSource()
{
    print_text(line_n.ToString());
    print_text(":\t");
    print_text(line);
    print_text("\n");
}
char read_ch()
{
    //update line_n, line_pos:
    if (c == '\0')
    {

    }
    else if (c == '\t')
    {
        line_pos += tab_size;
    }
    else if (c == '\n')
    {
        line_n++;
        line_pos = 1;
    }
    else
    {
        line_pos++;
    }

    //get next char 'c' and update line_idx:
    if (string.IsNullOrEmpty(line))
    {
        c = '\0';
    }
    else if (line_idx < line.Length)
    {
        c = line[line_idx];
        line_idx++;
    }
    else if (line_idx == line.Length)
    {
        c = '\n';
        line_idx++;
    }
    line = fd?.ReadLine();
    if (line == null)
    {
        c = '\0';
    }
    else if (line.Length == 0)
    {
        c = '\n';
    }
    else
    {
        c = line[0];
    }
    line_idx = 1;
    if (print_source && (!string.IsNullOrEmpty(line)))
    {
        PrintLineSource();
    }


    if (DEBUG)
    {
        print(c);
    }
    return c;
}
Identifier? add_id(string name, bool decl)
{
    Identifier? id = null;
    for (var i = 0; i < ids.Count; i++)
    {
        if (ids[i].name == name)
        {
            id = ids[i];
        }
    }
    if (id == null)
    {
        id = new Identifier();
        if (decl)
        {
            id.name = name;
            id.defined = true;
            id.defined_in_line = line_n;
            id.used = 0;
        }
        else
        {
            id.name = name;
            id.defined = false;
            id.defined_in_line = 0;
            id.used++;
        }
        ids.Add(id);
    }
    else
    {
        if (decl)
        {
            if (id.defined)
            {
                fatal("id `" + name + "' already defined in line "
                    + id.defined_in_line);
            }
            else
            {
                //ichiarazione di un ID gia' usato prima: 
                id.defined = true;
                id.defined_in_line = line_n;
            }
        }
        else
        {
            id.used++;
        }
    }
    return id;
}
int hex(char c)
    => c >= '0' && c <= '9'
    ? c - '0' : c >= 'A' && c <= 'F'
    ? c - 'A' + 10 : c >= 'a' && c <= 'f' ? c - 'a' + 10
    : fatal("invalid hexadecimal digit `" + c + "'");
void WarnInvalidEq(string s)
{
    if (!warn_invalid_eq)
    {
        warn_invalid_eq = true;
        warning("invalid `" + s + "' symbol -- please use `=' instead");
    }
}
void print_as_html(List<Decl> decls)
{
    for (int i = 0; i < decls.Count; i++)
    {
        if (do_print_index)
        {
            prints(i + 1 + ". ");
        }
        var d = decls[i];
        prints(d.id.name);
        prints(" = ");
        print_expr(d.n);
        prints(" ;<br>\n");
    }
}
void print_as_text(List<Decl> decls)
{
    for (int i = 0; i < decls.Count; i++)
    {
        if (do_print_index)
        {
            prints(i + 1 + ". ");
        }
        var d = decls[i];
        prints(d.id.name);
        prints(" = ");
        print_expr(d.n);
        prints(" ;\n");
    }
}
int DeclComparison(Decl x, Decl y)
{
    return string.Compare(x.id.name, y.id.name);
}
void sort_decls(List<Decl> decls)
{
    decls.Sort(DeclComparison);
    for (int i = 0; i < decls.Count; i++)
    {
        decls[i].id.index = i;
    }
}
string read_lit_string()
{
    var s = "\"";
    var d1 = 0;
    var d2 = 0;
    read_ch();
    while (true)
    {
        switch (c)
        {
            case '\0':
                fatal("unclosed literal string");
                break;
            case '\"':
                {
                    read_ch();
                    if (s.Length == 1)
                    {
                        fatal("invalid empty string");
                        return s + "\"";
                    }
                }
                break;
            case '\\':
                {
                    s += "\\";
                    read_ch();
                    if (c == '\"' | c == '\\')
                    {
                        s += c;
                        read_ch();
                    }
                    else if (c == 'a' || c == 'b' || c == 'n' || c == 'r' || c == 't')
                    {
                        s += c;
                        read_ch();
                    }
                    else if (c == 'x')
                    {
                        s += "x";
                        read_ch();
                        d1 = hex(c);
                        s += c;
                        read_ch();
                        d2 = hex(c);
                        s += c;
                        read_ch();
                        s += (char)(d1 << 4 + d2);
                        //s = s + CHR(16*d1+d2)
                    }
                    else
                    {
                        fatal("invalid escape sequence \"\\" + c + "\"");
                    }
                }
                break;
            default:
                if (c >= ' ' && c != 127)
                {
                    s += c;
                    read_ch();
                }
                else
                {
                    err("invalid control character code " + c + " in literal string -- possible unclosed string");
                    read_ch();
                    return s + "\"";

                }
                break;
        }
    }
}
int Main(string[] args)
{
    //Default preferences:
    tab_size = 8;
    do_sort = false;
    do_report = false;
    do_print_text = false;
    do_print_html = false;
    print_file_name = true;
    print_source = false;
    print_context = false;

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
                do_sort = true;
                break;
            case "--report":
                do_report = true;
                break;
            case "--print-text":
                do_print_text = true;
                break;
            case "--print-html":
                do_print_html = true;
                break;
            case "--print-index":
                do_print_index = true;
                break;
            case "--no-print-file-name":
                print_file_name = false;
                break;
            case "--print-source":
                print_source = true;
                break;
            case "--print-context":
                print_context = true;
                break;
            default:
                if (args[i].Length > 0 && args[i][0] == '-')
                {
                    error("ERROR: unknown option " + args[i] + "\n");
                    exit(1);
                }
                else
                {
                    if (fn != null)
                    {
                        error("ERROR: parameter " + args[i]
                        + ": file name already set to " + fn + "\n");
                        error("Please type \"bnf_chk --help\" for a complete list of the available options\n");
                        exit(1);
                    }
                    fn = args[i];
                }
                break;
        }

    }
    if (fn == null)
    {
        error("ERROR: required file name\n");
        exit(1);

    }
    ids.Clear();
    decls.Clear();
    read_decls(fn);
    if (do_report)
    {
        ids_report();
    }
    if (do_sort)
    {
        sort_decls(decls);
    }
    if (do_print_text)
    {
        print_as_text(decls);
    }
    if (do_print_html)
    {
        print_as_html(decls);
    }
    return 0;
}
Decl read_decl()
{
    Decl d = new();

    if (sym != Symbol.sym_name)
        fatal("expected rule name");

    d.id = add_id(s, true);
    read_sym();
    if (sym != Symbol.sym_eq)
        fatal("expected `='");

    read_sym();
    d.n = read_expr();
    if (sym != Symbol.sym_semicolon)
        err("unexpected symbol -- check possible missing `;' in the rule above");
    read_sym();
    return d;
}
void read_decls(string fn)
{
    int index = 0;
    Decl decl;

    fd = File.OpenText(fn);
    line_n = 1;
    line_pos = 0;
    line = fd.ReadLine();

    c = '\0';
    if (print_source)
        PrintLineSource();

    read_ch();
    read_sym();
    while (sym != Symbol.sym_eof)
    {
        decl = read_decl();
        index++;
        decl.id.index = index;
        decls.Add(decl);
    }
}
Node read_expr()
{
    Node e, s = new();
    e = read_termin();
    if (sym == Symbol.sym_vbar)
    {
        s.type = NodeType.or;
        s.a.Add(e);
        e = s;
    }
    while (sym == Symbol.sym_vbar)
    {
        read_sym();
        e.a.Add(read_termin());
    }
    return e;
}
Node read_factor()
{
    Node f = null;
    Symbol sym = Symbol.sym_eof;
    if (sym == Symbol.sym_lround)
    {
        read_sym();
        f = read_expr();
        if (sym != Symbol.sym_rround)
        {
            fatal("expected `)'");
        }
        read_sym();
    }
    else if (sym == Symbol.sym_lsquare)
    {
        f.type = NodeType.opt;
        read_sym();
        f.n = read_expr();
        if (sym != Symbol.sym_rsquare)
            fatal("expected `]'");
        read_sym();
    }
    else if (sym == Symbol.sym_lbrace)
    {
        f.type = NodeType.repeat;
        read_sym();
        f.n = read_expr();
        if (sym != Symbol.sym_rbrace)
            fatal("expected `}'");
        read_sym();
    }
    else if (sym == Symbol.sym_name)
    {
        f.type = NodeType.box;
        f.id = add_id(s, false);
        read_sym();
    }
    else if (sym == Symbol.sym_string)
    {
        f.type = NodeType.roundbox;
        //f[s] = StringToLiteral(s)
        f.s = s;
        read_sym();
        if (sym == Symbol.sym_range)
        {
            read_sym();
            if (sym != Symbol.sym_string)
                fatal("expected string after range separator");
            //##f[s] = f[s] + ".." + StringToLiteral(s)
            f.s += (".." + s);
            read_sym();
        }
    }
    else
        return null;
    return f;
}
Node read_termin()
{
    Node f = new(), p2 = new(), p = new();

    //Il primo fattore e' obbligatorio: 
    p = read_factor();
    if (p == null)
        fatal("expected factor");
    //Se c'e' un secondo fattore, allora e' un prodotto: 
    f = read_factor();
    if (f == null)
        return p;

    p2.type = NodeType.prod;
    p2.a[0] = p;
    p2.a[1] = f;

    p = p2;
    //Vedi se ci sono altri fattori: 
    while (true)
    {
        f = read_factor();
        if (f == null)
            return p;
        p.a.Add(f);

    }
}
void print_expr(Node d)
{
    if (d.type == NodeType.box)
    {
        print_text(" " + d.id.name);
        if (do_print_index)
        {
            int index = d.id.index;
            if (index == 0)
                prints("_?");
            else
                prints("_" + (index));
        }
    }
    else if (d.type == NodeType.roundbox)
        prints(" " + d.s);
    else if (d.type == NodeType.repeat)
    {
        prints(" {");
        print_expr(d.n);
        prints(" }");
    }
    else if (d.type == NodeType.opt)
    {
        prints(" [");
        print_expr(d.n);
        prints(" ]");
    }
    else if (d.type == NodeType.prod)
    {
        for (int i = 0; i < d.a.Count; i++)
        {
            if (d.a[i].type == NodeType.or)
            {
                prints(" (");
                print_expr(d.a[i]);
                prints(" )");
            }
            else
            {
                print_expr(d.a[i]);
            }
        }
    }
    else if (d.type == NodeType.or)
    {
        for (int i = 0; i < d.a.Count; i++)
        {
            if (i > 0)
                prints(" |");
            print_expr(d.a[i]);
        }
    }
    else
    {
        error("internal error (1)\n");
        exit(1);
    }
}
void print_expr_html(Node d)
{
    if (d.type == NodeType.box)
    {
        prints(" <i>" + d.id.name + "</i>");
        if (do_print_index)
        {
            int index = d.id.index;
            if (index == 0)
                prints("<sub>?</sub>");
            else
                prints("<sub>" + (index) + "</sub>");
        }
    }
    else if (d.type == NodeType.roundbox)
        prints(" <code><b>" + String2HTML(d.s) + "</b></code>");
    else if (d.type == NodeType.repeat)
    {
        prints(" {");
        print_expr(d.n);
        prints(" }");
    }
    if (d.type == NodeType.opt)
    {
        prints(" [");
        print_expr(d.n);
        prints(" ]");
    }
    if (d.type == NodeType.prod)
    {
        for (int i = 0; i < d.a.Count; i++)
        {
            if (d.a[i].type == NodeType.or)
            {
                prints(" (");
                print_expr(d.a[i]);
                prints(" )");
            }
            else
            {
                print_expr(d.a[i]);
            }
        }
    }
    if (d.type == NodeType.or)
    {
        for (int i = 0; i < d.a.Count; i++)
        {
            if (i > 0)
                prints(" |");
            print_expr(d.a[i]);
        }
    }
    else
    {
        error("internal error (1)\n");
        exit(1);
    }
}

bool is_id_letter(char c)
    => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_' || c >= '0' && c <= '9';
bool is_id_start_letter(char c)
    => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_';
void read_sym()
{
    while ((c == ' ') || (c == '\t') || (c == '#') || (c == '\n') || (c == '\r'))
    {
        if (c == '#')
        {
            while (true)
            {
                read_ch();
                if (c == '\n' || c == '\0')
                {
                    break;
                }
            }
        }
        else
        {
            read_ch();
        }
    }
    if (c == '\0')
    {
        sym = Symbol.sym_eof;
    }
    else if (is_id_start_letter(c))
    {
        s = c.ToString();
        while (true)
        {
            read_ch();
            if (is_id_letter(c))
            {
                s += c;
            }
            else
            {
                break;
            }
        }
        if (warn_invalid_id)
        {
            if (!warn_invalid_unquoted_lit_str)
            {
                warn_invalid_unquoted_lit_str = true;
                warning("guessed usage of the syntax style <ID> for IDs and STRING for literal strings -- trying to continue anyway");
            }
            s = "\"" + s + "\"";
            sym = Symbol.sym_string;
        }
        else
        {
            sym = Symbol.sym_name;
        }
    }
    else if (c == '<')
    {
        s = "";
        while (true)
        {
            read_ch();
            if (!is_id_letter(c) || c == '-' || c == ' ')
            {
                if (c == ' ' || c == '-') c = '_';
                s += c;
            }
            else if (c == '>')
            {
                if (!warn_invalid_id)
                {
                    warn_invalid_id = true;
                    warning("invalid identifier syntax `<SOME ID>' -- please use `SOME_ID' instead");
                }
                read_ch();
                break;
            }
            else
            {
                fatal("invalid char in <identifier>");
            }
        }
        sym = Symbol.sym_name;
    }
    else if (c == '=')
    {
        sym = Symbol.sym_eq;
        read_ch();
    }
    else if (c == ':')
    {
        read_ch();
        if (c == '=')
        {
            WarnInvalidEq(":=");
            read_ch();
            sym = Symbol.sym_eq;
        }
        else if (c == ':')
        {
            read_ch();
            if (c == '=')
            {
                WarnInvalidEq("::=");
                read_ch();
                sym = Symbol.sym_eq;
            }
            else
            {
                fatal("invalid character or symbol");
            }
        }
        else
            fatal("invalid character or symbol");
    }
    else if (c == '|')
    {
        sym = Symbol.sym_vbar;
        read_ch();
    }
    else if (c == '{')
    {
        sym = Symbol.sym_lbrace;
        read_ch();
    }
    else if (c == '}')
    {
        sym = Symbol.sym_rbrace;
        read_ch();
    }
    else if (c == '[')
    {
        sym = Symbol.sym_lsquare;
        read_ch();
    }
    else if (c == ']')
    {
        sym = Symbol.sym_rsquare;
        read_ch();
    }
    else if (c == '(')
    {
        sym = Symbol.sym_lround;
        read_ch();
    }
    else if (c == ')')
    {
        sym = Symbol.sym_rround;
        read_ch();
    }
    else if (c == ';')
    {
        sym = Symbol.sym_semicolon;
        read_ch();
    }
    else if (c == '\"')
    {
        s = read_lit_string();
        sym = Symbol.sym_string;
    }
    else if (c == '.')
    {
        read_ch();
        if (c == '.')
        {
            read_ch();
            sym = Symbol.sym_range;
        }
        else
        {
            if (!warn_invalid_rule_terminator)
            {
                warn_invalid_rule_terminator = true;
                warning("invalid rule terminator `.' -- please use `;' instead");
            }
            sym = Symbol.sym_semicolon;
            read_ch();
        }
    }
    else if (c > ' ')
    {
        err("unexpected character `" + c + "' -- trying to continue anyway");
        s = c.ToString();
        read_ch();

        while (c > ' ')
        {
            s += c;

            read_ch();

        }
        s = "\"" + s + "\"";//# FIXME: special chars must be escaped
        sym = Symbol.sym_string;
    }
    else
    {
        err("unexpected control char -- trying to continue anyway");
        sym = Symbol.sym_string;
    }
}

public class Identifier
{
    public string name = "";
    public bool defined = false;
    public int defined_in_line = 0;
    public int index = 0;
    public int used = 0;
}
public enum NodeType : uint
{
    box = 0,//rule name - ex. A
    roundbox = 1,//literal string - ex. "PRINT"
    prod = 2,//product of factors - ex. A B C
    or = 3,//alternatives terms - ex. A|B|C
    repeat = 4,//zero or more times - ex. {A}
    opt = 5,//optional - ex. [A]
}
public class Node
{
    public Identifier id = new();
    public NodeType type = NodeType.box;
    public string s = "";
    public Node n = new();
    public List<Node> a = new();
}
public class Decl
{
    public Identifier id = new();
    public Node n = new();
}
public enum Symbol : uint
{
    sym_eof = 0,
    sym_semicolon = 1,
    sym_name = 2,
    sym_eq = 3,
    sym_lbrace = 4,
    sym_rbrace = 5,
    sym_lsquare = 6,
    sym_rsquare = 7,
    sym_lround = 8,
    sym_rround = 9,
    sym_vbar = 10,
    sym_string = 11,
    sym_range = 12,
}
