// ParserGenerator by Malcolm Crowe August 1995, 2000, 2003
// 2003 version (4.1+ of Tools) implements F. DeRemer & T. Pennello:
// Efficient Computation of LALR(1) Look-Ahead Sets
// ACM Transactions on Programming Languages and Systems
// Vol 4 (1982) p. 615-649
// See class SymbolsGen in parser.cs

using System;
using System.Collections;
using System.IO;
using System.Text;
using Tools;

public class PrecReference : TOKEN
{
	public CSymbol precref;
	public PrecReference(Lexer lx,TOKEN t) : base(lx)
	{
		precref = ((CSymbol)t).Resolve();
		new SymbolType(((ptokens)lx).m_sgen,t.yytext,true);
	}
}

// This is an implementation of the Digraph algorithm from
// F. DeRemer and T. Pennello "Efficient Computation of LALR(1)
// Look-Ahead Sets" ACM Transactions on Programming Languages
// and Systems 4 (1982) p.625
public class Digraph
{
	public SymbolsGen sg;
	public Relation R; // defines the Digraph on the set of transitions
	public Func F1,F2; // we will compute F2 from F1
	// such that F2(x) = F1(x) union Union{ F2(y) | xRy }
	// by adding elements to the F2 sets using
	public AddToFunc DF2;
	public Digraph(SymbolsGen s,Relation r,Func f1,Func f2,AddToFunc d)
	{
		sg=s; R=r; F1=f1; F2=f2; DF2=d;
		N = new int[sg.m_trans];
		for (int j=0;j<sg.m_trans;j++)
			N[j]=0;
	}
	public ObjectList S = new ObjectList();
	public int[] N;
	public void Compute()
	{
		foreach (ParseState ps in sg.m_symbols.m_states.Values)
			foreach (Transition t in ps.m_transitions.Values)
				if (N[t.m_tno]==0)
					Traverse(t);
	}
	void Traverse(Transition x)
	{
		S.Push(x);
		int d = S.Count;
		N[x.m_tno] = d;
		DF2(x,F1(x));
		foreach (Transition y in R(x).Keys)
		{
			if (N[y.m_tno]==0)
				Traverse(y);
			if (N[y.m_tno]<N[x.m_tno])
				N[x.m_tno] = N[y.m_tno];
			DF2(x,F2(y));
		}
		if (N[x.m_tno]==d)
			for (;;)
			{
				Transition t = (Transition)S.Top;
				N[t.m_tno] = int.MaxValue;
				DF2(t,F2(x));
				if (S.Pop()==x)
					break;
			}
	}
}

public class ParserGenerate : SymbolsGen
{
	public bool m_showParser = false;
	public bool m_parserseen = false;
	public bool m_concrete { get { return m_symbols.m_concrete; } set { m_symbols.m_concrete=value; }}
	public ParserGenerate(ErrorHandler eh):base(eh) {}
	public CsReader m_inFile;
	public string m_actions;
	public string m_lexerClass;
	string m_actvars = "";
	public bool m_namespace = false;
	public void Create(string infname)
	{
		try
		{
			m_inFile = new CsReader(infname,m_scriptEncoding);
		}
		catch(Exception ex)
		{
			erh.Error(new CSToolsFatalException(28,0,0,"","Could not open input file "+infname+" ("+ex.Message+")."));
			return;
		}
		if (infname.EndsWith(".txt"))
			infname = infname.Substring(0,infname.Length-4);
		try
		{
			m_outFile = new StreamWriter(infname+".cs",false);
		}
		catch (Exception ex)
		{
			erh.Error(new CSToolsFatalException(29,0,0,"","Could not create output file "+infname+".cs ("+ex.Message+")."));
			return;
		}
		_Create();
		m_outFile.Close();
	}
	void _Create()
	{
		ptokens tks = new ptokens(erh);
		m_outname = "syntax";
		tks.m_sgen = this;
		m_lexer = tks;
		m_symbols.erh = erh;
		m_symbols.ClassInit(this);
		m_outname = "syntax";
		m_outFile.WriteLine("using System;using Tools;");
		Production special = new Production(this,m_symbols.Special);
		m_lexer.yytext = "error";
		CSymbol e = (new CSymbol(this)).Resolve();
		e.m_symtype = CSymbol.SymType.nonterminal;
		e.m_defined=true;
		// 1: INPUT
		// top-down parsing of script
		m_lexer.Start(m_inFile);
		m_tok = (TOKEN)m_lexer.Next();
		//Console.WriteLine("Token <{0}> {1}",m_tok.yytext,m_tok.GetType().Name);
		while (m_tok!=null)
			ParseProduction();
		// that's the end of the script
		if (!m_parserseen)
			Error(30,0,"no parser directive detected - possibly incorrect text encoding?");
		m_outFile.WriteLine(m_actions); // output the action function
		m_outFile.WriteLine("}  return null; }");
		special.AddToRhs(m_symbols.m_startSymbol);
		special.AddToRhs(m_symbols.EOFSymbol);
		// 2: PROCESSING
		Console.WriteLine("First");
		DoFirst();
		Console.WriteLine("Follow");
		DoFollow();
		Console.WriteLine("Parse Table");
		ParseState start = new ParseState(this,null);
		m_symbols.m_states[0] = start;
		start.MaybeAdd(new ProdItem(special,0));
		start.Closure();
		start.AddEntries();
		Transition tfinal = (Transition)start.m_transitions[m_symbols.m_startSymbol.yytext];
		ParserShift pe = tfinal.m_next;
		m_symbols.m_accept = pe.m_next;
		if (m_symbols.m_accept==null)
			m_symbols.erh.Error(new CSToolsFatalException(43,0,0,"","No accept state. ParserGenerator cannot continue."));
		// 2A: Reduce States for the LR(0) parser
		foreach (ParseState ps in m_symbols.m_states.Values)
			ps.ReduceStates();
		/*	if (m_showParser)
			{
				foreach (ParseState ps in m_symbols.m_states.Values)
				{
					ps.Print0();
					if (ps==m_symbols.m_accept)
						Console.WriteLine("    EOF  accept");
				}
			} */
		// 3: Compute Look-ahead sets
		if (m_lalrParser)
		{
			m_symbols.Transitions(new Builder(Transition.BuildDR));
			/*	if (m_showParser)
					m_symbols.PrintTransitions(new Func(Transition.DR),"DR"); */
			m_symbols.Transitions(new Builder(Transition.BuildReads));
			new Digraph(this,
				new Relation(Transition.reads),
				new Func(Transition.DR),
				new Func(Transition.Read),
				new AddToFunc(Transition.AddToRead)).Compute();
			// detect cycles in Read TBD
			/*	if (m_showParser)
					m_symbols.PrintTransitions(new Func(Transition.Read),"Read"); */
			m_symbols.Transitions(new Builder(Transition.BuildIncludes));
			m_symbols.Transitions(new Builder(Transition.BuildLookback));
			new Digraph(this,
				new Relation(Transition.includes),
				new Func(Transition.Read),
				new Func(Transition.Follow),
				new AddToFunc(Transition.AddToFollow)).Compute();
			// detect cycles for which Read is non empty TBD
			/*	if (m_showParser)
					m_symbols.PrintTransitions(new Func(Transition.Follow),"Follow"); */
			m_symbols.Transitions(new Builder(Transition.BuildLA));
		}
		// 5: OUTPUT
		// output the run-time ParsingInfo table
		Console.WriteLine("Building parse table");
		m_symbols.Transitions(new Builder(Transition.BuildParseTable));
		foreach (CSymbol v in m_symbols.symbols.Values)
		{
			if (v.m_symtype!=CSymbol.SymType.nodesymbol)
				continue;
			ParsingInfo pi = new ParsingInfo(v.yytext);
			CSymbol r = v;
			while (r.m_symtype==CSymbol.SymType.nodesymbol)
				r = r.m_refSymbol;
			if (m_symbols.symbolInfo[v.yytext]!=null)
				m_symbols.erh.Error(new CSToolsException(45,"Bad %node/%symbol hierarchy"));
			pi.m_parsetable = m_symbols.GetSymbolInfo(r.yytext).m_parsetable;
			m_symbols.symbolInfo[v.yytext] = pi;
		}
		Console.WriteLine("Writing the output file");
		m_outFile.WriteLine(@"/// <summary/>");
		m_outFile.WriteLine("public yy"+m_outname+"():base() { arr = new int[] { ");
		m_symbols.Emit(m_outFile);
		// output the class factories
		CSymbol s;
		Console.WriteLine("Class factories");
		IDictionaryEnumerator de = m_symbols.symbols.GetEnumerator();
		for (int pos = 0; pos<m_symbols.symbols.Count; pos++)
		{
			de.MoveNext();
			string str = (string)de.Key;
			s = (CSymbol)de.Value;
			if ((s==null) // might happen because of error recovery
				|| (s.m_symtype!=CSymbol.SymType.nonterminal && s.m_symtype!=CSymbol.SymType.nodesymbol))
				continue;
			//Console.WriteLine("{0} {1}",s.yytext,s.m_symtype);
			m_outFile.WriteLine("new Sfactory(this,\"{0}\",new SCreator({0}_factory));",str);
		}
		m_outFile.WriteLine("}");
		de.Reset();
		for (int pos = 0; pos<m_symbols.symbols.Count; pos++)
		{
			de.MoveNext();
			string str = (string)de.Key;
			s = (CSymbol)de.Value;
			if ((s==null) // might happen because of error recovery
				|| (s.m_symtype!=CSymbol.SymType.nonterminal && s.m_symtype!=CSymbol.SymType.nodesymbol))
				continue;
			//Console.WriteLine("{0} {1}",s.yytext,s.m_symtype);
			m_outFile.WriteLine(@"/// <summary/>");
			m_outFile.WriteLine("public static object "+str+"_factory(Parser yyp) { return new "+str+"(yyp); }");
		}
		m_outFile.WriteLine("}");
		m_outFile.WriteLine(@"/// <summary/>");
		m_outFile.WriteLine("public class "+m_outname+": Parser {");
		m_outFile.WriteLine(@"/// <summary/>");
		m_outFile.WriteLine("public "+m_outname+"():base(new yy"+m_outname+"(),new "+m_lexerClass+"()) {}");
		m_outFile.WriteLine(@"/// <summary/>");
		m_outFile.WriteLine("public "+m_outname+"(Symbols syms):base(syms,new "+m_lexerClass+"()) {}");
		m_outFile.WriteLine(@"/// <summary/>");
		m_outFile.WriteLine("public "+m_outname+"(Symbols syms,ErrorHandler erh):base(syms,new "+m_lexerClass+"(erh)) {}");
		m_outFile.WriteLine(m_actvars);
		m_outFile.WriteLine(" }");
		if (m_namespace)
			m_outFile.WriteLine("}");
		Console.WriteLine("Done");
		if (m_showParser)
		{
			foreach (ParseState ps in m_symbols.m_states.Values)
			{
				ps.Print();
				if (ps==m_symbols.m_accept)
					Console.WriteLine("    EOF  accept");
			}
		}
	}
	public void GetDefs(string fname)
	{
		try
		{
			bool gotit = false;
			StreamReader sr = new StreamReader(fname);
			for (string buf=sr.ReadLine(); buf!=null; buf=sr.ReadLine())
				if (buf.Length>3 && buf.Substring(0,3)=="//%")
				{
					gotit = true;
					if (buf.Substring(3,1)=="+")
					{
						m_lexer.yytext = buf.Substring(4);
						new SymbolType(this,m_lexer.yytext,true);
					}
					if (buf.Substring(3,1)=="|")
					{
						m_lexerClass = buf.Substring(4);
					}
					else
					{
						m_lexer.yytext = buf.Substring(3);
						new SymbolType(this,m_lexer.yytext,false);
					}
				}
			sr.Close();
			if (!gotit)
				erh.Error(new CSToolsFatalException(27,1,0,"","%parser directive did not give a tokens file"));
		}
		catch (Exception e)
		{
			erh.Error(new CSToolsFatalException(27,1,0,fname,fname+": "+e.Message));
		}
	}

	public TOKEN m_tok; // current input token as returned by Parser.Next
	public void Advance()
	{
		m_tok = (TOKEN)m_lexer.Next();
	}
	public Production m_prod; // current production being parsed

	internal void DoFirst()
	{
		// Rule 1: terminals only
		Production p;
		foreach (CSymbol v in m_symbols.symbols.Values)
		{
			if (v.m_symtype==CSymbol.SymType.unknown)
				v.m_symtype = CSymbol.SymType.terminal;
			if (v.IsTerminal())
			{
				v.m_first.CheckIn(v);
				if (!Find(v))
					erh.Error(new CSToolsException(31,"Lexer script should define symbol "+ v.yytext));
			}
		}
		foreach (CSymbol s in m_symbols.literals.Values)
			s.m_first.CheckIn(s);
		m_symbols.EOFSymbol.m_first.CheckIn(m_symbols.EOFSymbol);
		// Rule 2: Nonterminals with the rhs consisting only of actions
		int j,k;
		for (k=1;k<prods.Count;k++)
		{
			p = (Production)prods[k];
			if (p.m_actionsOnly)
				p.m_lhs.m_first.CheckIn(m_symbols.EmptySequence);
		}

		// Rule 3: The real work begins
		bool donesome = true;
		while (donesome)
		{
			donesome = false;
			for (k=1;k<prods.Count;k++)
			{
				p = (Production)prods[k];
				int n = p.m_rhs.Count;
				for (j=0;j<n;j++)
				{
					CSymbol t = (CSymbol)p.m_rhs[j];
					if (t.IsAction())
						donesome |= t.m_first.CheckIn(m_symbols.EmptySequence);
					int pos = 0;
					foreach (CSymbol a in t.m_first.Keys)
						if ((a!=m_symbols.EmptySequence || pos++==t.m_first.Count-1))
							donesome |= p.m_lhs.m_first.CheckIn(a);
					if (!t.m_first.Contains(m_symbols.EmptySequence))
						break;
				}
			}
		}
	}

	internal void DoFollow() // NB: this is for the LR(0) parser construction
	{
		// Rule 1:
		m_symbols.m_startSymbol.m_follow.CheckIn(m_symbols.EOFSymbol);
		// Rule 2 & 3:
		bool donesome = true;
		while (donesome)
		{
			donesome = false;
			for (int k=1; k<prods.Count; k++)
			{
				Production p = (Production)prods[k];
				int n = p.m_rhs.Count;
				for (int j=0; j<n; j++)
				{
					CSymbol b = (CSymbol)p.m_rhs[j];
					// Rule 2
					p.AddFirst(b,j+1);
					// Rule 3
					if (p.CouldBeEmpty(j+1))
						donesome |= b.AddFollow(p.m_lhs.m_follow);
				}
			}
		}
	}

	// routines called by Lexer actions
	public override void CopySegment()
	{
		string str="";
		int ch;
		for (;(ch=m_lexer.GetChar())!=0;)
		{
			str += (char)ch;
			if (ch=='\n')
			{
				if (str.Equals("%}\n"))
					return;
				m_outFile.WriteLine(str);
				str ="";
			}
		}
		m_outFile.Write(str);
	}
	public override void ParserDirective()
	{
		m_parserseen = true;
		m_lexer.yy_begin("parser");
		m_tok = m_lexer.Next(); // collect token file name from %parser directive
		m_lexer.yy_begin("YYINITIAL");
		string tokfile = m_tok.yytext;
		if (!tokfile.EndsWith(".cs"))
			tokfile += ".cs";
		GetDefs(tokfile);
		int ch;
		m_outname = "syntax";
		do						// look to see if the parser class name is given
		{
			ch = m_lexer.GetChar();
		} while (ch==' '||ch=='\t');
		if (ch!='\n')
		{
			string clsname = new string((char)ch,1);
			while (ch!=' ' && ch!='\t' && ch!='\n' && ch!=0)
			{
				ch = m_lexer.GetChar();
				clsname += new string((char)ch,1);
			}
			if (clsname!="")
				m_outname = clsname;
		}
		while (ch!='\n' && ch!=0) // ignore anything else on the line
		{
			ch = m_lexer.GetChar();
		}
		m_actions = "/// <summary/>\n" +
			"public class yy"+m_outname+": Symbols {\n" +
			"/// <summary/>\n" +
			"/// <param name='yyq'></param>\n" +
			"/// <param name='yysym'></param>\n" +
			"/// <param name='yyact'></param>\n" +
			"  public override object Action(Parser yyq,SYMBOL yysym, int yyact) {\n" +
			"    switch(yyact) {\n" +
			"	 case -1: break; //// keep compiler happy";
	}
	public override void Declare()
	{
		int rv = 1;
		int quote = 0;
		int ch;
		for(;;)
		{
			ch = m_lexer.GetChar();
			if (ch==0)
			{
				Error(32,m_lexer.m_pch,"EOF in Declare?");
				return;
			}
			if (ch=='\\')
			{
				m_actvars += ch;
				ch = (char)m_lexer.GetChar();
			}
			else if (quote==0 && ch=='{')
				 rv++;
			else if (quote==0 && ch=='}')
			{
				if (--rv==0)
					break;
			}
			else if (ch==quote)
				quote = 0;
			else if (ch=='\''||ch=='"')
				quote = ch;
			m_actvars += (char)ch;
		}
	}
	public override void ClassDefinition(string bas)
	{
		string line, name;
		int n = m_lexer.yytext.Length;
		line = m_lexer.yytext;
		EmitClassDefin(m_lexer.m_buf,ref m_lexer.m_pch,m_lexer.m_buf.Length,null,bas,out bas,out name,false);
		m_lexer.Advance();
		m_lexer.yytext = name;
		CSymbol s = (new CSymbol(this)).Resolve();
		s.m_defined = true;
		s.m_emitted = true;
		if (line[1]=='n')
		{
			s.m_symtype = CSymbol.SymType.nodesymbol;
			if (m_symbols.symbols.Contains(bas))
				s.m_refSymbol = (CSymbol)m_symbols.symbols[bas];
			else
				m_symbols.erh.Error(new CSToolsFatalException(44,s,"Unknown base type "+bas+": reorder declarations?"));
		}
	}

	internal int prec = 0;

	public override void AssocType(Precedence.PrecType pt, int p)
	{
		string line;
		int len,action=0;
		CSymbol s;
		line = m_lexer.yytext;
		prec += 10;
		if (line[p]!=' '&&line[p]!='\t')
			Error(33,m_lexer.m_pch,"Expected white space after precedence directive");
		for (p++;p<line.Length && (line[p]==' '||line[p]=='\t');p++)
			;
		while (p<line.Length)
		{
			len = m_lexer.m_start.Match(line,p,ref action);
			if (len<0)
			{
				Console.WriteLine(line.Substring(p));
				Error(34,m_lexer.m_pch,"Expected token");
				break;
			}
			m_lexer.yytext = line.Substring(p,len);
			bool rej = false;
			s = (CSymbol)((yyptokens)(m_lexer.tokens)).OldAction(m_lexer,m_lexer.yytext,action,ref rej);
			s = s.Resolve();
			s.m_prec = new Precedence(pt,prec,s.m_prec);
			for (p+=len; p<line.Length && (line[p]==' '||line[p]=='\t'); p++)
				;
		}
	}

	public override void SetNamespace()
	{
		m_tok = m_lexer.Next();
		m_namespace = true;
		m_outFile.WriteLine("namespace "+m_tok.yytext+" {");
	}

	public override void SetStartSymbol()
	{
		m_tok = m_lexer.Next(); // recursive call of lexer.Next should be okay
		m_symbols.m_startSymbol = ((CSymbol)m_tok).Resolve();
		m_symbols.m_startSymbol.m_symtype = CSymbol.SymType.nonterminal;
	}

	// proxies for constructors
	internal void NewConstructor(ref CSymbol s,string str)
	{ // may update s to a new node type
		// we have just seen a new initialiser for s
		if (str.Length==0)
			return;
		CSymbol bas = s;
		string newname;
		for (int variant=1;;variant++)
		{ // get a genuinely new identifier
			newname = String.Format("{0}_{1}", bas.yytext, variant);
			s = (CSymbol)m_symbols.symbols[newname];
			if (s==null)
				break;
		}
		m_lexer.yytext = newname;
		s = (new CSymbol(this)).Resolve();
		s.m_symtype = CSymbol.SymType.nodesymbol;
		s.m_refSymbol = bas;
		s.m_emitted = true;
		m_outFile.WriteLine();
		m_outFile.WriteLine(@"/// <summary/>");
		m_outFile.WriteLine("public class "+newname+" : "+bas.yytext+" {");
		m_outFile.WriteLine(@"/// <summary/>");
		m_outFile.WriteLine(@"/// <param name='yyq'></param>");
		m_outFile.WriteLine("  public "+newname+"(Parser yyq):base(yyq"+str+"}");
	}

	void NextNonWhite(out int ch)
	{
		while ((ch=m_lexer.PeekChar())==' '||ch=='\t'||ch=='\n')
			m_lexer.GetChar();
	}

	public override void SimpleAction(ParserSimpleAction a)
	{
		string str = a.yytext.Substring(1, a.yytext.Length-1);
		if (str=="null")
		{
			m_lexer.yytext = "Null";
			a.m_sym = (new CSymbol(this)).Resolve();
			NewConstructor(ref a.m_sym,",\""+m_prod.m_lhs.yytext+"\"){}");
			return;
		}
		if (str.Length==0)
			str = m_prod.m_lhs.yytext;
		CSymbol s = (CSymbol)m_symbols.symbols[str];
		if (s==null)
		{ // not there: define a new node type
			//Console.WriteLine("Symbol {0} needs defining",str);
			m_lexer.yytext = str;
			s = new CSymbol(this);
			m_symbols.symbols[str] = s;
			s.m_symtype = CSymbol.SymType.nodesymbol;
			s.m_refSymbol = (CSymbol)m_prod.m_lhs;
			s.m_emitted = true;
			m_outFile.WriteLine(@"/// <summary/>");
			m_outFile.WriteLine("public class "+str+" : "+s.m_refSymbol.yytext+" { ");
			m_outFile.WriteLine(@"/// <summary/>");
			m_outFile.WriteLine(@"/// <param name='yyq'></param>");
			m_outFile.WriteLine("  public "+str+"(Parser yyq):base(yyq) { }}");
		}
		a.m_sym = s;
		int ch;
		str = ")";
		NextNonWhite(out ch);
		if (ch=='(')
		{
			ch = m_lexer.GetChar();
			str = ","+GetBracketedSeq(ref ch,')').Substring(1);
			NextNonWhite(out ch);
		}
		if (str==",)") // 4.0d
			str=")";
		string init = "{}";
		if (ch=='{')
		{
			ch = m_lexer.GetChar();
			init = "yyp=("+m_outname+")yyq;\n"+ GetBracketedSeq(ref ch,'}');
			NextNonWhite(out ch);
		}
		init = str + init;
		NewConstructor(ref a.m_sym,init);
	}

	string FixDollars(ref int ch)
	{
		bool minus = false;
		int ix;
		int ln = m_prod.m_rhs.Count+1;
		string type="int";
		string str = "";
		ch = m_lexer.GetChar();
		if (ch=='<')
		{
			type = ((TOKEN)m_lexer.Next()).yytext;
			ch = m_lexer.GetChar();
			if (ch!='>')
				Error(35,m_lexer.m_pch,"Expected >");
			ch = m_lexer.GetChar();
		}
		if (ch=='$')
		{
			//		ch = m_lexer.GetChar();
			str += "yylval";
			if (m_prod.m_lhs.m_defined)
				Error(36,m_lexer.m_pch,String.Format("Production {0}: symbol {1} has been typed, and should not use $$",m_prod.m_pno, m_prod.m_lhs.yytext));
			return str;
		}
		if (ch=='-')
		{
			minus = true;
			//		ch = m_lexer.GetChar();
		}
		ix =1;
		if (ch<'0' || ch>'9')
			Error(37,m_lexer.m_pch,String.Format("expected $ or digit, got {0}",(char)ch));
		else
			ix = minus?('0'-ch):(ch-'0');
		for (ch = m_lexer.PeekChar(); ch>='0' && ch<='9'; m_lexer.Advance())
			ix = ix*10+ (minus?('0'-ch):(ch-'0'));
		if (ix<=0)
		{
			str += String.Format("\n\t({0})(yyps.StackAt({0}).m_value)\n\t",type,ln-ix);
			if ((char)ch!='.')
				str += ".m_dollar";
		}
		else
			m_prod.StackRef(ref str, ch, ix);
		return str;
	}

	string GetBracketedSeq(ref int ch,char cend)
	{ // ch is the start bracket, already returned
		string str = "";
		int quoteseen = 0;
		int brackets = 1;
		char cstart = (char)ch;
		str += cstart;
		//Console.WriteLine("GetBracketedSeq with {0}",str);
		while ((ch=m_lexer.GetChar())!=0)
		{
			if (ch==cstart)
			{
				if (quoteseen==0)
					brackets++;
			}
			else if (ch==cend)
			{
				if (quoteseen==0 && --brackets==0)
					goto labout;
			}
			else if ((ch>='a'&&ch<='z')||(ch>='A'&&ch<='Z')||ch=='_')
			{
				m_lexer.UnGetChar();
				TOKEN a = m_lexer.Next();
				if (m_prod.m_alias.Contains(a.yytext))
				{
					int ix = (int)m_prod.m_alias[a.yytext];
					ch = m_lexer.PeekChar();
					m_prod.StackRef(ref str,ch,ix);
				}
				else
					str += a.yytext;
				continue;
			}
			else
				switch(ch)
				{
					case '$':
						str += FixDollars(ref ch);
						continue;  // don't add the ch
					case '\\':
						str += (char)ch;
						ch = m_lexer.GetChar();
						break;
					case '\n':
						break;
					case '"': case '\'':
						if (quoteseen==0)    // start of quoted string
							quoteseen = ch;
						else if (quoteseen==ch) // matching end of string
							quoteseen = 0;
						break;
				}
			str += (char)ch;
		}
		Error(38,m_lexer.m_pch,"EOF in Action: check bracket counts, quotes");
		labout:
			str += cend;
		str = str.Replace("yyp","(("+m_outname+")yyq)");
		return str;
	}

	public override void OldAction(ParserOldAction a)
	{
		int ch = '{';
		a.m_symtype = CSymbol.SymType.oldaction;
		a.m_action = ++action;
		a.m_initialisation = GetBracketedSeq(ref ch,'}');
		a.m_sym = (CSymbol)m_prod.m_lhs;

		NextNonWhite(out ch);
		if (ch==';'||ch=='|')
		{ // an old action at the end is converted into a simple action
			m_lexer.yytext = "%";
			ParserSimpleAction sa = new ParserSimpleAction(this);
			SimpleAction(sa);
			NewConstructor(ref sa.m_sym, ")"+a.m_initialisation);
			a.m_sym = (CSymbol)sa;
			sa.yytext += sa.m_sym.yytext;
			a.yytext = "#"+sa.yytext;
			a.m_action = -1; // mark the old action for deletion
		}
	}

	// parsing routines
	internal void ParseProduction()
	{
		TOKEN tok = m_tok;
		CSymbol lhs = null;
		try
		{
			lhs = ((CSymbol)m_tok).Resolve();
		}
		catch(Exception e)
		{
			erh.Error(new CSToolsFatalException(45,tok,string.Format("Syntax error in Parser script - possibly extra semicolon?",e.Message)));
		}
		m_tok = lhs;
		if (m_tok.IsTerminal())
			Error(39,m_tok.pos,string.Format("Illegal left hand side <{0}> for production",m_tok.yytext));
		if (m_symbols.m_startSymbol==null)
			m_symbols.m_startSymbol = lhs;
		if (lhs.m_symtype==CSymbol.SymType.unknown)
			lhs.m_symtype = CSymbol.SymType.nonterminal;
		if ((!lhs.m_defined) && lhs.m_prods.Count==0)
		{ // lhs not defined in %symbol statement and not previously a lhs
			// so declare it as a new symbol
			m_outFile.WriteLine(@"/// <summary/>");
			m_outFile.WriteLine("public class "+lhs.yytext+" : SYMBOL {");
			m_outFile.WriteLine(@"/// <summary/>");
			m_outFile.WriteLine(@"/// <param name='yyq'></param>");
			m_outFile.WriteLine("	public "+lhs.yytext+"(Parser yyq):base(yyq) { }");
			m_outFile.WriteLine(@"/// <summary/>");
			m_outFile.WriteLine("  public override string yyname() { return \""+lhs.yytext+"\"; }}");
		}
		if (!Find(lhs))
			new SymbolType(this,lhs.yytext);
		m_prod = new Production(this,lhs);
		m_lexer.yy_begin("rhs");
		Advance();
		if (!m_tok.Matches(":"))
			Error(40,m_tok.pos,String.Format("Colon expected for production {0}",lhs.yytext));
		Advance();
		RhSide(m_prod);
		while(m_tok!=null && m_tok.Matches("|"))
		{
			Advance();
			m_prod = new Production(this,lhs);
			RhSide(m_prod);
		}
		if (m_tok==null || !m_tok.Matches(";"))
			Error(41,m_lexer.m_pch,"Semicolon expected");
		Advance();
		m_prod = null;
		m_lexer.yy_begin("YYINITIAL");
	}

	public void RhSide(Production p)
	{
		CSymbol s;
		ParserOldAction a = null; // last old action seen
		while (m_tok!=null)
		{
			if (m_tok.Matches(";"))
				break;
			if (m_tok.Matches("|"))
				break;
			if (m_tok.Matches(":"))
			{
				Advance();
				p.m_alias[m_tok.yytext] = p.m_rhs.Count;
				Advance();
			}
			else if (m_tok is PrecReference)
			{
				if (p.m_rhs.Count==0)
					erh.Error(new CSToolsException(21,"%prec cannot be at the start of a right hand side"));
				PrecReference pr = (PrecReference)m_tok;
				CSymbol r = (CSymbol)p.m_rhs[p.m_rhs.Count-1];
				r.m_prec = pr.precref.m_prec;
				Advance();
			}
			else
			{
				s = (CSymbol)m_tok;
				if (s.m_symtype==CSymbol.SymType.oldaction)
				{
					if (a!=null)
						Error(42,s.pos,"adjacent actions");
					a = (ParserOldAction)s;
					if (a.m_action<0)
					{ // an OldAction that has been converted to a SimpleAction: discard it
						s = a.m_sym; // add the SimpleAction instead
						s.m_symtype = CSymbol.SymType.simpleaction;
						ParserSimpleAction sa = (ParserSimpleAction)s;
					}
					else  // add it to the Actions function
						m_actions += String.Format("\ncase {0} : {1} break;", a.m_action, a.m_initialisation);
					a = null;
				}
				else if (s.m_symtype!=CSymbol.SymType.simpleaction)
					s = ((CSymbol)m_tok).Resolve();
				p.AddToRhs(s);
				Advance();
			}
		}
		Precedence.Check(p);
	}

	public static void Main(string[] argv)
	{
		bool Dflag = false;
		bool Kflag = false;
		bool Lflag = false;
		bool Cflag = false;
		int argc = argv.Length;
		int j;
		for (j = 0; argc>0 && argv[j][0]=='-'; j++,argc--)
			switch(argv[j][1])
			{
				case 'D': Dflag = true; Kflag = true; break; // showParser
				case 'K': Kflag = true; break; // keep going
				case 'L': Lflag = true; break; // skip LALR phase
				case 'C': Cflag = true; break; // discard the concrete syntax
			}
		ErrorHandler erh = new ErrorHandler(!Kflag); // by default reduce/reduce errors abort the parser generator
		ParserGenerate parser = new ParserGenerate(erh);
		parser.m_showParser = Dflag;
		parser.m_lalrParser = !Lflag;
		parser.m_concrete = !Cflag;
		if (argc==1)
			parser.Create(argv[j]);
		else
			parser.Create("test.parser");
		if (erh.counter==0)
			Console.WriteLine("ParserGenerate completed successfully");
		else
			Console.WriteLine("Parser error count="+erh.counter);
	}
}
