// Malcolm Crowe 1995, 2000
// a yacc-style implementation

using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;

namespace Tools
{
	public class Symbols
	{
		public ErrorHandler erh = new ErrorHandler(true); // should get overwritten by Parser constructor
		public Symbols() { }
		// symbols
		public Hashtable symbols = new Hashtable(); // string -> CSymbol
		public Hashtable literals = new Hashtable(); // string -> Literal
		// support for parsing
		public Hashtable symbolInfo = new Hashtable();  // name -> ParsingInfo
		public Hashtable literalInfo = new Hashtable(); // yytext -> ParsingInfo
		public bool m_concrete; // whether to build the concrete syntax tree
		public Hashtable m_states = new Hashtable(); // int->ParseState
		public CSymbol EOFSymbol;
		public CSymbol EmptySequence;
		public CSymbol Special;
		public CSymbol m_startSymbol;
		public string StartSymbol
		{
			get { return (m_startSymbol!=null)?m_startSymbol.yytext:"<null>"; }
			set
			{
				CSymbol s = (CSymbol)symbols[value];
				if (s==null)
					erh.Error(new CSToolsException(25,"No such symbol <"+value+">"));
				m_startSymbol = s;
			}
		}
#if (GENTIME)
		public ParsingInfo GetSymbolInfo(string name)
		{
			ParsingInfo pi = (ParsingInfo)symbolInfo[name];
			if (pi==null)
				symbolInfo[name] = pi = new ParsingInfo(name);
			return pi;
		}
		public ParsingInfo GetLiteralInfo(string spell)
		{
			ParsingInfo pi = (ParsingInfo)literalInfo[spell];
			if (pi==null)
				literalInfo[spell] = pi = new ParsingInfo(spell);
			return pi;
		}
		public void ClassInit(SymbolsGen yyp)
		{
			EmptySequence = new CSymbol(yyp); EmptySequence.yytext="e";
			Special = new CSymbol(yyp); Special.yytext="S'";
			EOFSymbol = new EOF(yyp).Resolve();
		}
		public void Transitions(Builder b)
		{
			foreach (ParseState ps in m_states.Values)
				foreach (Transition t in ps.m_transitions.Values)
					b(t);
		}
		public void PrintTransitions(Func f,string s)
		{
			foreach (ParseState ps in m_states.Values)
				foreach (Transition t in ps.m_transitions.Values)
					t.Print(f(t),s);
		}
#endif
		public ParseState m_accept;
		// support for actions
		public virtual object Action(Parser yyp,SYMBOL yysym, int yyact) { return null; } // will be generated for the generated parser
		public Hashtable types = new Hashtable(); // string->SCreator
		// support for serialization
		public int[] arr; // defined in generated subclass

		public void GetEOF(Lexer yyl)
		{
			EOFSymbol = (EOF)symbols["EOF"];
			if (EOFSymbol==null)
				EOFSymbol = new EOF(yyl);
		}
#if (GENTIME)
		public void Emit(TextWriter m_outFile)
		{
			Serialiser b = new Serialiser(m_outFile);
			Console.WriteLine("Serialising the parser");
			b.Serialise(m_startSymbol);
			b.Serialise(m_accept);
			b.Serialise(m_states);
			b.Serialise(symbolInfo);
			b.Serialise(literalInfo);
			b.Serialise(m_concrete);
			m_outFile.WriteLine("0};");
		}
#endif
		public void GetParser(Lexer m_lexer)
		{
			Serialiser b = new Serialiser(arr);
			m_startSymbol = (CSymbol)b.Deserialise();
			m_startSymbol.kids = new ObjectList(); // 4.2a
			m_accept = (ParseState)b.Deserialise();
			m_states = (Hashtable)b.Deserialise();
			symbolInfo = (Hashtable)b.Deserialise();
			literalInfo = (Hashtable)b.Deserialise();
			m_concrete = (bool)b.Deserialise();
			GetEOF(m_lexer);
		}
	}

#if (GENTIME)
	// Context free Grammar is a quadruple G=<T,N,S,P>
	// where T is a finite set of terminal symbols
	// [namely, the instances of TOKEN]
	// N is a finite set of nonterminal symbols
	// [namely, the instances of SYMBOL for which IsTerminal() is false]
	// such that T intersect N = emptyset.
	// S [= m_symbols.m_startSymbol] is the start symbol
	// P is a finite subset of N x V* where V = T union N
	// [i.e. V is the set of instances of SYMBOL]
	// and each member (A,w) of P is called a production
	// [i.e. an instance p of Production] written A->w .
	// A is called the left part [p.m_lhs]
	// and w the right part [the ObjectList p.m_rhs]
	public abstract class SymbolsGen : GenBase
	{
		protected SymbolsGen(ErrorHandler eh):base(eh) {}
		public bool m_lalrParser = true;
		public Lexer m_lexer;
		public Symbols m_symbols = new Symbols();
		// Productions
		public int pno = 0;
		public ObjectList prods = new ObjectList(); // Production
		public int m_trans = 0; // #Transitions
		// support for actions
		public int action = 0;
		internal ObjectList actions = new ObjectList();  // ParserAction
		internal int action_num = 0; // for old actions
		public SymbolType stypes = null; // the list of grammar symbols
		internal int state=0; // for parsestates
		public SymbolSet lahead = null; // support for lookahead sets
		public bool Find(CSymbol sym)
		{
			if (sym.yytext.Equals("Null")) // special case
				return true;
			if (sym.yytext[0]=='\'')
				return true;
			if (stypes==null)
				return false;
			return stypes._Find(sym.yytext);
		}
		public abstract void ParserDirective();
		public abstract void Declare();
		public abstract void SetNamespace();
		public abstract void SetStartSymbol();
		public abstract void ClassDefinition(string s);
		public abstract void AssocType(Precedence.PrecType pt,int n);
		public abstract void CopySegment();
		public abstract void SimpleAction(ParserSimpleAction a);
		public abstract void OldAction(ParserOldAction a);
	}

	public class SymbolType
	{
		string m_name;
		SymbolType m_next;
		public SymbolType(SymbolsGen yyp,string name) : this(yyp,name,false)	{}
		public SymbolType(SymbolsGen yyp,string name,bool defined)
		{
			Lexer yyl = yyp.m_lexer;
			yyl.yytext = name;
			CSymbol s = (new CSymbol(yyp)).Resolve();
			m_name = name; m_next=yyp.stypes; yyp.stypes=this;
			if (defined)
				s.m_defined = true;
		}
		public bool _Find(string name)
		{
			if (name.Equals(m_name))
				return true;
			if (m_next==null)
				return false;
			return m_next._Find(name);
		}
	}
#endif
	public class ParserAction : CSymbol
	{
		public virtual SYMBOL Action(Parser yyp)
		{
			SYMBOL s = (SYMBOL)Sfactory.create(m_sym.yytext,yyp);
			if (s.yyname()==m_sym.yytext)
			{  // provide for the default $$ = $1 action if possible
				SYMBOL t = yyp.StackAt(m_len-1).m_value;
				s.m_dollar = (m_len==0 || t==null)? null : t.m_dollar;
			}
			return s;
		}
		public override void Print() { Console.Write(m_sym.yytext); }
		public CSymbol m_sym;
		public int m_len;
		public override Boolean IsAction() { return true; }
		public virtual int ActNum() { return 0; }
#if (GENTIME)
		public ParserAction(SymbolsGen yyp) : base(yyp) {}
#endif
		protected ParserAction() {}
		public new static object Serialise(object o,Serialiser s)
		{
			ParserAction p = (ParserAction)o;
			if (s.Encode)
			{
				CSymbol.Serialise(p,s);
				s.Serialise(p.m_sym);
				s.Serialise(p.m_len);
				return null;
			}
			CSymbol.Serialise(p,s);
			p.m_sym = (CSymbol)s.Deserialise();
			p.m_len = (int)s.Deserialise();
			return p;
		}
	}

	public class ParserOldAction : ParserAction
	{
		public int m_action;
		public override SYMBOL Action(Parser yyp)
		{
			SYMBOL s = base.Action(yyp);
			object ob = yyp.m_symbols.Action(yyp,s,m_action);
			if (ob!=null)
				s.m_dollar = ob;
			return s;
		}
		public override int ActNum() { return m_action; }
#if (GENTIME)
		public ParserOldAction(SymbolsGen yyp) : base(yyp) {
			m_action =  yyp.action_num++;
			yyp.actions.Add(this); m_sym = null;
			m_symtype = CSymbol.SymType.oldaction;
			yyp.OldAction(this);
		}
#endif
		ParserOldAction() {}
		public new static object Serialise(object o,Serialiser s)
		{
			if (s==null)
				return new ParserOldAction();
			ParserOldAction p = (ParserOldAction)o;
			if (s.Encode)
			{
				ParserAction.Serialise(p,s);
				s.Serialise(p.m_action);
				return null;
			}
			ParserAction.Serialise(p,s);
			p.m_action = (int)s.Deserialise();
			return p;
		}
	}

	public class ParserSimpleAction : ParserAction
	{
#if (GENTIME)
		public override string TypeStr() { return m_sym.yytext; }
		public override void Print()
		{
			Console.Write(" %{0}", m_sym.yytext);
		}
		public ParserSimpleAction(SymbolsGen yyp) : base(yyp) {
			yyp.actions.Add(this);
			m_symtype = CSymbol.SymType.simpleaction;
			yyp.SimpleAction(this);
		}
#endif
		ParserSimpleAction() {}
		public new static object Serialise(object o,Serialiser s)
		{
			if (s==null)
				return new ParserSimpleAction();
			if (s.Encode)
			{
				ParserAction.Serialise(o,s);
				return null;
			}
			return ParserAction.Serialise(o,s);
		}
	}

	public abstract class ParserEntry
	{
		public ParserAction m_action;
		[NonSerialized] public int m_priority = 0;
		public ParserEntry() { m_action = null; }
		public ParserEntry(ParserAction action) { m_action=action; }
		public virtual void Pass(ref ParseStackEntry top) {}
		public virtual bool IsReduce() { return false; }
		public virtual string str { get { return ""; }}
		public virtual void Print(ParseState ps,CSymbol s)
		{
			Console.Write("  {0}  {1}  ",s.yytext,str);
			if (m_action!=null)
				m_action.Print();
		}
		public static object Serialise(object o, Serialiser s)
		{
			ParserEntry p = (ParserEntry)o;
			if (s.Encode)
			{
				s.Serialise(p.m_action);
				return null;
			}
			p.m_action = (ParserAction)s.Deserialise();
			return p;
		}
	}

	public class ParserShift : ParserEntry
	{
		public ParseState m_next;
		public ParserShift() {}
		public ParserShift(ParserAction action,ParseState next) : base(action) { m_next=next; }
		public override void Pass(ref ParseStackEntry top)
		{
			Parser yyp = top.yyps;
			if(m_action==null)
			{
				yyp.Push(top);
				top = new ParseStackEntry(yyp,m_next.m_state,yyp.NextSym());
			}
			else
			{
				yyp.Push(new ParseStackEntry(yyp,top.m_state,m_action.Action(yyp)));
				top.m_state = m_next.m_state;
			}
		}
		public override string str
		{
			get
			{
				if (m_next==null)
					return "?? null shift";
				return string.Format("shift {0}",m_next.m_state); }
		}
#if (GENTIME)
		public override void Print(ParseState ps,CSymbol s)
		{
			base.Print(ps,s);
			while (s.m_symtype==CSymbol.SymType.nodesymbol)
				s = s.m_refSymbol;
			Transition t = (Transition)ps.m_transitions[s.yytext];
			Transition.Follow(t).Print();
		}
#endif
		public new static object Serialise(object o,Serialiser s)
		{
			if (s==null)
				return new ParserShift();
			ParserShift p = (ParserShift)o;
			if (s.Encode)
			{
				ParserEntry.Serialise(p,s);
				s.Serialise(p.m_next);
				return null;
			}
			ParserEntry.Serialise(p,s);
			p.m_next = (ParseState)s.Deserialise();
			return p;
		}
	}

	public class ParserReduce : ParserEntry
	{
		public int m_depth;
		public Production m_prod;
		public ParserReduce(ParserAction action,int depth,Production prod) : base(action) {m_depth=depth; m_prod = prod; }
		ParserReduce() {}
#if (GENTIME)
		public SymbolSet m_lookAhead = null;
		public void BuildLookback(Transition a)
		{
			SymbolsGen sg = a.m_ps.m_sgen;
			if (m_lookAhead!=null)
				return;
			m_lookAhead = new SymbolSet(sg);
			foreach (ParseState p in sg.m_symbols.m_states.Values)
			{
				Transition b = (Transition)p.m_transitions[m_prod.m_lhs.yytext];
				if (b==null)
					continue;
				Path pa = new Path(p,m_prod.Prefix(m_prod.m_rhs.Count));
				if (pa.valid && pa.Top == a.m_ps)
					b.m_lookbackOf[this] = true;
			}
		}
#endif
		public override void Pass(ref ParseStackEntry top)
		{
			Parser yyp = top.yyps;
			SYMBOL ns = m_action.Action(yyp); // before we change the stack
			yyp.m_ungot = top.m_value;
			if (yyp.m_debug)
				Console.WriteLine("about to pop {0} count is {1}",m_depth,yyp.m_stack.Count);
			yyp.Pop(ref top,m_depth,ns);
			if (ns.pos==0)
				ns.pos = top.m_value.pos;  // Guess symbol position
			top.m_value = ns;
		}
		public override bool IsReduce() { return true; }
		public override string str
		{
			get
			{
				if (m_prod==null)
					return "?? null reduce";
				return string.Format("reduce {0}",m_prod.m_pno);
			}
		}
#if (GENTIME)
		public override void Print(ParseState ps,CSymbol s)
		{
			base.Print(ps,s);
			Console.Write("  ");
			m_lookAhead.Print();
		}
#endif
		public new static object Serialise(object o,Serialiser s)
		{
			if (s==null)
				return new ParserReduce();
			ParserReduce p = (ParserReduce)o;
			if (s.Encode)
			{
				ParserEntry.Serialise(p,s);
				s.Serialise(p.m_depth);
				s.Serialise(p.m_prod);
				return null;
			}
			ParserEntry.Serialise(p,s);
			p.m_depth = (int)s.Deserialise();
			p.m_prod = (Production)s.Deserialise();
			return p;
		}
	}
#if (GENTIME)
	public delegate Hashtable Relation(Transition a); // Transition->bool
	public delegate SymbolSet Func(Transition a);
	public delegate void AddToFunc(Transition a,SymbolSet s);
	public delegate void Builder(Transition t);
	public class Transition
	{
		public int m_tno;
		public ParseState m_ps;
		public CSymbol m_A;
		public Transition(ParseState p,CSymbol a)
		{
			m_ps=p; m_A=a;
			m_tno = p.m_sgen.m_trans++;
			p.m_transitions[a.yytext] = this;
		}
		private ParsingInfo ParsingInfo
		{
			get
			{
				Symbols syms = m_ps.m_sgen.m_symbols;
				if (m_A is Literal)
					return syms.GetLiteralInfo(m_A.yytext);
				else
					return syms.GetSymbolInfo(m_A.yytext);
			}
		}
		public ParserShift m_next = null;
		public Hashtable m_reduce = new Hashtable(); // Production->ParserReduce
		Hashtable m_reads = new Hashtable(); // Transition->bool
		Hashtable m_includes = new Hashtable(); // Transition->bool
		internal Hashtable m_lookbackOf = new Hashtable(); // ParserReduce->bool
		public static Hashtable reads(Transition a) { return a.m_reads; }
		public static Hashtable includes(Transition a) { return a.m_includes; }
		public static SymbolSet DR(Transition a) { return a.m_DR; }
		public static SymbolSet Read(Transition a) { return a.m_Read; }
		public static SymbolSet Follow(Transition a) { return a.m_Follow; }
		public static void AddToRead(Transition a,SymbolSet s) { a.m_Read.Add(s); }
		public static void AddToFollow(Transition a, SymbolSet s) { a.m_Follow.Add(s); }
		SymbolSet m_DR; // built by BuildDR, called from parserGenerator
		SymbolSet m_Read; // built using Digraph Compute called from ParserGenerator
		SymbolSet m_Follow; // ditto
		public static void BuildDR(Transition t)
		{
			SymbolsGen sg = t.m_ps.m_sgen;
			t.m_DR = new SymbolSet(sg);
			if (t.m_next==null)
				return;
			foreach (Transition u in t.m_next.m_next.m_transitions.Values)
				if (u.m_next!=null)
					if (u.m_A.m_symtype==CSymbol.SymType.terminal || u.m_A.m_symtype==CSymbol.SymType.eofsymbol)
						t.m_DR.AddIn(u.m_A);
		}
		public static void Final(Transition t)
		{
			t.m_DR.AddIn(t.m_ps.m_sgen.m_symbols.EOFSymbol);
		}
		public static void BuildReads(Transition t)
		{
			t.m_Read = new SymbolSet(t.m_ps.m_sgen);
			ParseState ps = t.m_A.Next(t.m_ps);
			if (ps==null)
				return;
			foreach (Transition b in ps.m_transitions.Values)
				if (b.m_A.CouldBeEmpty())
					t.m_reads[b] = true;
		}
		public static void BuildIncludes(Transition t)
		{
			t.m_Follow = new SymbolSet(t.m_ps.m_sgen);
			foreach (ParseState ps in t.m_ps.m_sgen.m_symbols.m_states.Values)
				foreach (Production p in t.m_ps.m_sgen.prods)
				{
					Transition b = (Transition)ps.m_transitions[p.m_lhs.yytext];
					if (b==null)
						continue;
					for (int i=p.m_rhs.Count-1;i>=0;i--)
					{
						CSymbol s = (CSymbol)p.m_rhs[i];
						if (s==t.m_A)
						{
							if (i==0 && b.m_ps==t.m_ps)
							{
								t.m_includes[b] = true;
								break;
							}
							if (new Path(b.m_ps,p.Prefix(i)).Top == t.m_ps)
							{
								t.m_includes[b] = true;
								break;
							}
						}
						if (!s.CouldBeEmpty())
							break;
					}
				}
		}
		public static void BuildLookback(Transition t)
		{
			foreach (ParserReduce pr in t.m_reduce.Values)
				pr.BuildLookback(t);
		}
		public static void BuildLA(Transition t)
		{
			foreach (ParserReduce pr in t.m_lookbackOf.Keys)
				pr.m_lookAhead.Add(t.m_Follow);
		}
		public static void BuildParseTable(Transition t)
		{
			Symbols syms = t.m_ps.m_sgen.m_symbols;
			ParsingInfo pi = t.ParsingInfo;
			ParserReduce red = null;
			foreach (ParserReduce pr in t.m_reduce.Values)
			{
					if (t.m_ps.m_sgen.m_lalrParser?
						pr.m_lookAhead.Contains(t.m_A):
						pr.m_prod.m_lhs.m_follow.Contains(t.m_A))
					{
						if (red!=null)
							syms.erh.Error(new CSToolsException(12,string.Format("reduce/reduce conflict {0} vs {1}",red.m_prod.m_pno,pr.m_prod.m_pno)+
								string.Format(" state {0} on {1}",t.m_ps.m_state,t.m_A.yytext)));
						red = pr;
					}
				//	else
				//		t.Print(pr.m_lookAhead,"discarding reduce ("+pr.m_prod.m_pno+") LA ");
			}

			if (t.m_next!=null && t.m_A!=syms.EOFSymbol)
			{
				if (red==null)
					pi.m_parsetable[t.m_ps.m_state] = t.m_next;
				else if (t.m_A.ShiftPrecedence(red.m_prod,t.m_ps))
					pi.m_parsetable[t.m_ps.m_state] = t.m_next;
				else
					pi.m_parsetable[t.m_ps.m_state] = red;
			}
			else if (red!=null)
				pi.m_parsetable[t.m_ps.m_state] = red;
		}
		public void Print0()
		{
			Console.Write("    "+m_A.yytext);
			if (m_next!=null)
				Console.Write("  shift "+m_next.m_next.m_state);
			foreach (Production p in m_reduce.Keys)
				Console.Write("  reduce ("+p.m_pno+")");
			Console.WriteLine();
		}
		public void Print(SymbolSet x,string s)
		{
			Console.Write("Transition ("+m_ps.m_state+","+m_A.yytext+") "+s+" ");
			x.Print();
		}
	}
	// The Closure and AddActions functions represent the heart of the ParserGenerator
#endif

	public class ParseState
	{
		public int m_state;
		public CSymbol m_accessingSymbol;
		bool m_changed = true;
#if (GENTIME)
		public SymbolsGen m_sgen;
		internal ProdItemList m_items; // ProdItem, in ProdItem order
		public Hashtable m_transitions = new Hashtable(); // string -> Transition
		public Transition GetTransition(CSymbol s)
		{
			Transition t = (Transition)m_transitions[s.yytext];
			if (t!=null)
				return t;
			return new Transition(this,s);
		}
		public bool Accessor(CSymbol[] x) { return new Path(x).Top == this; }
		public bool Lookback(Production pr,ParseState p) { return new Path(this,pr.Prefix(pr.m_rhs.Count)).Top==this; }
		public void MaybeAdd(ProdItem item)
		{ // called by CSymbol.AddStartItems
			if (!m_items.Add(item))
				return;
			m_changed = true;
		}
		public void Closure()
		{
			while (m_changed)
			{
				m_changed = false;
				for (ProdItemList pi = m_items; pi.m_pi!=null; pi = pi.m_next)
					CheckClosure(pi.m_pi);
			}
		}
		public void CheckClosure(ProdItem item)
		{
			CSymbol ss = item.Next();
			if (ss!=null)
			{
				ss.AddStartItems(this,item.FirstOfRest(ss.m_parser));
				if (item.IsReducingAction())
					MaybeAdd(new ProdItem(item.m_prod, item.m_pos+1));
			}
		}
		public void AddEntries()
		{
			ProdItemList pil;
			for (pil=m_items; pil.m_pi!=null; pil=pil.m_next)
			{
				ProdItem item = pil.m_pi;
				if (item.m_done)
					continue;
				CSymbol s = item.Next();
				if (s==null || item.IsReducingAction())
					continue;
				// shift/goto action
				// Build a new parse state as target: we will check later to see if we need it
				ParseState p = new ParseState(m_sgen,s);
				// the new state should have at least the successor of this item
				p.MaybeAdd(new ProdItem(item.m_prod, item.m_pos+1));

				// check the rest of the items in this ParseState (leads to m_done for them)
				// looking for other items that allow this CSymbol to pass
				for (ProdItemList pil1=pil.m_next; pil1!=null && pil1.m_pi!=null; pil1=pil1.m_next)
				{
					ProdItem another = pil1.m_pi;
					if (s==another.Next())
					{
						p.MaybeAdd(new ProdItem(another.m_prod, another.m_pos+1));
						another.m_done = true;
					}
				}

				if (!m_items.AtEnd)
				{
					if (s.IsAction())
					{
						p = p.CheckExists();
						foreach (CSymbol f in s.m_follow.Keys)
							if (f!=m_sgen.m_symbols.EOFSymbol)
							{
								Transition t = GetTransition(f);
//								if (t.m_next!=null)
//									m_sgen.Error(15,s.pos,String.Format("Action/Action or Action/Shift conflict on {0}",f.yytext));
								t.m_next = new ParserShift((ParserAction)s,p);
							}
					}
					else
					{ // we guarantee to make a nonzero entry in the parsetable
						GetTransition(s).m_next = new ParserShift(null, p.CheckExists());
					}
				}
			}
		}
		public void ReduceStates()
		{
			ProdItemList pil;
			for (pil=m_items; pil.m_pi!=null; pil=pil.m_next)
			{
				ProdItem item = pil.m_pi;
				CSymbol s = item.Next();
				if (s==null)
				{ // item is a reducing item
					Production rp = item.m_prod;
					if (rp.m_pno==0) // except for production 0: S'->S-|
						continue;
					// reduce item: deal with it
					int n = rp.m_rhs.Count;
					CSymbol a;
					ParserReduce pr;
					if (n>0 && (a=(CSymbol)rp.m_rhs[n-1])!=null && a.IsAction())
					{
						ParserAction pa = (ParserAction)a;
						pa.m_len = n;
						pr = new ParserReduce(pa,n-1,rp);
					}
					else
					{
						m_sgen.m_lexer.yytext = "%"+rp.m_lhs.yytext;
						ParserSimpleAction sa = new ParserSimpleAction(m_sgen);
						sa.m_sym = (CSymbol)rp.m_lhs;
						sa.m_len = n;
						pr = new ParserReduce(sa,n,rp);
					}
					foreach (CSymbol ss in item.m_prod.m_lhs.m_follow.Keys)
						GetTransition(ss).m_reduce[rp]=pr;
				}
			}
		}
		public bool SameAs(ParseState p)
		{
			if (m_accessingSymbol!=p.m_accessingSymbol)
				return false;
			ProdItemList pos1 = m_items;
			ProdItemList pos2 = p.m_items;
			while (!pos1.AtEnd && !pos2.AtEnd && pos1.m_pi.m_prod==pos2.m_pi.m_prod && pos1.m_pi.m_pos==pos2.m_pi.m_pos)
			{
				pos1 = pos1.m_next;
				pos2 = pos2.m_next;
			}
			return pos1.AtEnd && pos2.AtEnd;
		}
		public ParseState CheckExists()
		{
			Closure();
			foreach (ParseState p in m_sgen.m_symbols.m_states.Values)
				if (SameAs(p))
					return p;
			m_sgen.m_symbols.m_states[m_state]=this;
			AddEntries();
			return this;
		}
		~ParseState()
		{
			if (m_sgen!=null && m_state==m_sgen.state-1)
				m_sgen.state--;
		}
		public ParseState(SymbolsGen syms,CSymbol acc)
		{
			m_sgen = syms;
			m_state=syms.state++;
			m_accessingSymbol=acc;
			m_items = new ProdItemList();
		}
		public void Print0()
		{
			Console.WriteLine();
			if (m_state==0)
				Console.WriteLine("state 0");
			else
				Console.WriteLine("state {0} accessed by {1}",m_state,m_accessingSymbol.yytext);
			// first about the state itself
			if (m_items!=null)
				for (ProdItemList pil=m_items; pil.m_pi!=null; pil=pil.m_next)
				{
					pil.m_pi.Print();
					pil.m_pi.m_prod.m_lhs.m_follow.Print();
				}
			foreach (Transition t in m_transitions.Values)
				t.Print0();
		}
		public void Print()
		{
			Console.WriteLine();
			if (m_state==0)
				Console.WriteLine("state 0");
			else
				Console.WriteLine("state {0} accessed by {1}",m_state,m_accessingSymbol.yytext);
			// first about the state itself
			if (m_items!=null)
				for (ProdItemList pil=m_items; pil.m_pi!=null; pil=pil.m_next)
				{
					pil.m_pi.Print();
					Console.WriteLine();
				}
			// next about the transitions
			Console.WriteLine();
			foreach (ParsingInfo pi in m_sgen.m_symbols.symbolInfo.Values)
				PrintTransition(pi);
			foreach (ParsingInfo pi in m_sgen.m_symbols.literalInfo.Values)
				PrintTransition(pi);
		}
		void PrintTransition(ParsingInfo pi)
		{
			ParserEntry pe = (ParserEntry)pi.m_parsetable[m_state];
			if (pe!=null)
			{
				Console.Write("        {0}  {1}  ", pi.m_symName,pe.str);
				if (pe.m_action!=null)
					pe.m_action.Print();
				Console.WriteLine();
			}
		}
#endif
		ParseState() {}
		public static object Serialise(object o, Serialiser s)
		{
			if (s==null)
				return new ParseState();
			ParseState p = (ParseState)o;
			if (s.Encode)
			{
				s.Serialise(p.m_state);
				s.Serialise(p.m_accessingSymbol);
				s.Serialise(p.m_changed);
				return true;
			}
			p.m_state = (int)s.Deserialise();
			p.m_accessingSymbol = (CSymbol)s.Deserialise();
			p.m_changed = (bool)s.Deserialise();
			return p;
		}
	}

	public class ParseStackEntry
	{
		public Parser yyps;
		public int m_state;
		public SYMBOL m_value;
		public ParseStackEntry(Parser yyp) {yyps = yyp; }
		public ParseStackEntry(Parser yyp,int state,SYMBOL value)
		{
			yyps=yyp;m_state=state; m_value = value;
		}
	}

	public class ParsingInfo
	{
		public string m_symName;   // symbol name (as in GetType().Name)
		public Hashtable m_parsetable = new Hashtable(); // state:int -> ParserEntry
		public ParsingInfo(string name)
		{
			m_symName = name;
		}
		ParsingInfo() {}
		public static object Serialise(object o,Serialiser s)
		{
			if (s==null)
				return new ParsingInfo();
			ParsingInfo p = (ParsingInfo)o;
			if (s.Encode)
			{
				s.Serialise(p.m_symName);
				s.Serialise(p.m_parsetable);
				return null;
			}
			p.m_symName = (string)s.Deserialise();
			p.m_parsetable = (Hashtable)s.Deserialise();
			return p;
		}
	}

#if (GENTIME)
	public class SymbolSet
	{
		public SymbolsGen m_symbols;
		public SymbolSet m_next;
		Hashtable m_set = new Hashtable(); // CSymbol -> bool
		public SymbolSet(SymbolsGen syms) { m_symbols = syms; }
		public SymbolSet(SymbolSet s):this(s.m_symbols) { Add(s); }
		public bool Contains(CSymbol a) { return m_set.Contains(a); }
		public ICollection Keys { get { return m_set.Keys; } }
		public IDictionaryEnumerator GetEnumerator() { return m_set.GetEnumerator(); }
		public int Count { get { return m_set.Count; }}
		public bool CheckIn(CSymbol a)
		{
			if (Contains(a))
				return false;
			AddIn(a);
			return true;
		}
		public SymbolSet Resolve()
		{
			return find(m_symbols.lahead);
		}
		SymbolSet find(SymbolSet h)
		{
			if (h==null)
			{
				m_next = m_symbols.lahead;
				m_symbols.lahead = this;
				return this;
			}
			if (Equals(h,this))
				return h;
			return find(h.m_next);
		}
		static bool Equals(SymbolSet s,SymbolSet t)
		{
			if (s.m_set.Count!=t.m_set.Count)
				return false;
			IDictionaryEnumerator de = s.GetEnumerator();
			IDictionaryEnumerator ee = t.GetEnumerator();
			for (int pos=0; pos<s.Count; pos++)
			{
				de.MoveNext(); ee.MoveNext();
				if (de.Key != ee.Key)
					return false;
			}
			return true;
		}
		public void AddIn (CSymbol t)
		{
			m_set[t] = true;
		}
		public void Add (SymbolSet s)
		{
			if (s==this)
				return;
			foreach (CSymbol k in s.Keys)
				AddIn(k);
		}
		public static SymbolSet operator+ (SymbolSet s,SymbolSet t)
		{
			SymbolSet r = new SymbolSet(s);
			r.Add(t);
			return r.Resolve();
		}
		public void Print()
		{
			string pr = "[";
			int pos = 0;
			foreach (CSymbol s in Keys)
			{
				pos++;
				if (s.yytext.Equals("\n"))
					pr += "\\n";
				else
					pr += s.yytext;
				if (pos<Count)
					pr += ",";
			}
			pr += "]";
			Console.WriteLine(pr);
		}
	}

	public class Path
	{
		public bool valid = true;
		ParseState[] m_states;
		public Path(ParseState[] s) { m_states = s; }
		public Path(ParseState q,CSymbol[] x)
		{
			m_states = new ParseState[x.Length+1];
			ParseState c;
			c = m_states[0] = q;
			for (int j=0;j<x.Length;j++)
			{
				int k;
				for (k=j;k<x.Length;k++)
					if (!x[k].IsAction())
						break;
				if (k>=x.Length)
				{
					m_states[j+1] = c;
					continue;
				}
				Transition t = (Transition)c.m_transitions[x[k].yytext];
				if (t==null || t.m_next==null)
				{
					valid = false;
					break;
				}
				c = m_states[j+1] = t.m_next.m_next;
			}
		}
		public Path(CSymbol[] x) : this((ParseState)(x[0].m_parser.m_symbols.m_states[0]),x) {}
		public CSymbol[] Spelling
		{
			get
			{
				CSymbol[] r = new CSymbol[m_states.Length-1];
				for (int j=0;j<r.Length;j++)
					r[j] = m_states[j].m_accessingSymbol;
				return r;
			}
		}
		public ParseState Top
		{
			get
			{
				return m_states[m_states.Length-1];
			}
		}
	}

	public class Precedence
	{
		public enum PrecType { left, right, nonassoc, before, after };
		public PrecType m_type;
		public int m_prec;
		public Precedence m_next;
		public Precedence(PrecType t,int p,Precedence next)
		{
			if (CheckType(next,t,0)!=0)
				Console.WriteLine("redeclaration of precedence");
			m_next = next; m_type = t; m_prec = p;
		}
		static int CheckType(Precedence p,PrecType t, int d)
		{
			if (p==null)
				return 0;
			if (p.m_type==t || (p.m_type<=PrecType.nonassoc && t<=PrecType.nonassoc))
				return p.m_prec;
			return Check(p.m_next,t, d+1);
		}
		public static int Check(Precedence p,PrecType t,int d)
		{
			if (p==null)
				return 0;
			if (p.m_type==t)
				return p.m_prec;
			return Check(p.m_next,t,d+1);
		}
		public static int Check(CSymbol s, Production p, int d)
		{
			if (s.m_prec==null)
				return 0;
			int a = CheckType(s.m_prec, PrecType.after,d+1);
			int b = CheckType(s.m_prec, PrecType.left,d+1);
			if (a>b)
				return a - p.m_prec;
			else
				return b - p.m_prec;
		}
		public static void Check(Production p)
		{
			int efflen = p.m_rhs.Count;
			while (efflen>1 && ((CSymbol)p.m_rhs[efflen-1]).IsAction())
				efflen--;
			if (efflen==3)
			{
				CSymbol op = (CSymbol)p.m_rhs[1];
				int b = CheckType(op.m_prec, PrecType.left, 0);
				if (b!=0 && ((CSymbol)p.m_rhs[2])==p.m_lhs)
				 // allow operators such as E : V = E here
					p.m_prec = b;
			}
			else if (efflen==2)
			{
				if ((CSymbol)p.m_rhs[0]==p.m_lhs)
				{
					int aft = Check(((CSymbol)p.m_rhs[1]).m_prec, PrecType.after,0);
					if (aft!=0)
						p.m_prec = aft;
				}
				else if ((CSymbol)p.m_rhs[1]==p.m_lhs)
				{
					int bef = Check(((CSymbol)p.m_rhs[0]).m_prec, PrecType.before,0);
					if (bef!=0)
						p.m_prec = bef;
				}
			}
		}
	}
#endif
	public class CSymbol : TOKEN // may be terminal (symbolic or literal), non-terminal or ParserAction
	{
		// because of forward declarations etc, a named symbol can appear in the rhs of a production
		// without us knowing if it is a terminal or a nonterminal
		// if something is a node, or an OldAction, we will know at once
		public enum SymType { unknown, terminal, nonterminal, nodesymbol, oldaction, simpleaction, eofsymbol }
		public SymType m_symtype;
		public override bool IsTerminal()
		{
			return m_symtype==SymType.terminal;
		}
		public CSymbol(Lexer yyl):base(yyl) {}
#if (GENTIME)
		public SymbolsGen m_parser;
		// this list accumulates information about classes defined in the parser script
		// so that the relevant parts of the generated file can be written out afterwards
		public virtual CSymbol Resolve()
		{
			CSymbol s = (CSymbol)m_parser.m_symbols.symbols[yytext];
			if (s!=null)
				return s;
			m_parser.m_symbols.symbols[yytext] = this;
			return this;
		}
		public CSymbol(SymbolsGen yyp) : base(yyp.m_lexer)
		{
			m_parser = yyp;
			m_symtype = SymType.unknown;
			m_prec = null;
			m_prod = null;
			m_refSymbol = null;
			m_first = new SymbolSet(yyp);
			m_follow = new SymbolSet(yyp);
		}
		public override bool Matches(string s) { return false; }

		internal ParseState Next(ParseState p)
		{
			if (!p.m_transitions.Contains(yytext))
				return null;
			ParserShift ps = ((Transition)p.m_transitions[yytext]).m_next;
			if (ps==null)
				return null;
			return ps.m_next;
		}
		internal Hashtable Reduce(ParseState p) // Objectlist of ParserReduce to distinct productions
		{
			if (!p.m_transitions.Contains(yytext))
				return null;
			return ((Transition)p.m_transitions[yytext]).m_reduce;
		}
		// for adding typecasts to $n
		public virtual string TypeStr() { return yytext; }

		// for terminals
		public Precedence m_prec;
		public bool ShiftPrecedence(Production prod,ParseState ps)
		{
			if (prod==null) // no reduce available
				return true;
			if (!((SymbolSet)prod.m_lhs.m_follow).Contains(this)) // if this is not a follow symbol of the prod's lhs, there is no conflict
				return true;  // do shift
			if (m_prec==null)
			{ // no precedence information
				Console.WriteLine("Shift/Reduce conflict {0} on reduction {1} in state {2}", yytext, prod.m_pno,ps.m_state);
				return true; // do shift anyway
			}
			int p = Precedence.Check(this,prod,0);
			if (p==0)
			{
				if (Precedence.Check(m_prec,Precedence.PrecType.right,0)!=0)
				{ // equal precedence but right associative: shift
					return true;
				}
				return false; // don't shift
			}
			return p>0; // shift if symbol has higher precedence than production, else reduce
		}
		// for non-terminals
		public SymbolSet m_first; // allow EmptySequence
		public SymbolSet m_follow; // for LR(0) phase: allow EOFSymbol
		public bool AddFollow(SymbolSet map)
		{ // CSymbol->bool : add contents of map to m_follow
			bool r = false;
			foreach(CSymbol a in map.Keys)
				r |= m_follow.CheckIn(a);
			return r;
		}
		public ObjectList m_prods = new ObjectList(); // int:  production nos with this symbol as left side
		public void AddStartItems(ParseState pstate,SymbolSet follows)
		{
			for (int pos=0;pos<m_prods.Count;pos++)
			{
				int np = (int)m_prods[pos];
				Production p = (Production)pstate.m_sgen.prods[np];
				pstate.MaybeAdd(new ProdItem(p, 0));
			}
		}
		public bool CouldBeEmpty()
		{
			return (m_symtype==CSymbol.SymType.simpleaction ||
				m_symtype==CSymbol.SymType.oldaction ||
				m_first.Contains(m_first.m_symbols.m_symbols.EmptySequence));
		}
		// for nodesymbols
		public CSymbol m_refSymbol;  // maybe null

		// class definition info
		public string m_initialisation=""; // may be empty
		public bool m_defined = false;
		public bool m_emitted = false;
		public Production m_prod;  // production where this initialisation occurs: maybe null
#endif
		protected CSymbol() {}
		public static object Serialise(object o,Serialiser s)
		{
			if (s==null)
				return new CSymbol();
			CSymbol c = (CSymbol)o;
			if (s.Encode)
			{
				s.Serialise(c.yytext);
				s.Serialise((int)c.m_symtype);
				return null;
			}
			c.yytext = (string)s.Deserialise();
			c.m_symtype = (SymType)s.Deserialise();
			return c;
		}

	}

	// [Serializable]
	public class Literal : CSymbol  // used for %TOKEN in LexerGenerator script and quoted strings
	{
#if (GENTIME)
		public Literal(SymbolsGen yyp) : base(yyp) { m_symtype=SymType.terminal; }
		public override CSymbol Resolve()
		{ // to the first occurrence
			int n = yytext.Length;
			string ns ="";
			for (int p=1;p+1<n;p++) // fix \ escapes
				if (yytext[p] == '\\')
				{
					if (p+1<n)
						p++;
					if (yytext[p]>='0' && yytext[p]<='7')
					{
						int v;
						for (v = yytext[p++]-'0';p<n && yytext[p]>='0' && yytext[p]<='7';p++)
							v=v*8+yytext[p]-'0';
						ns += (char)v;
					}
					else
						switch(yytext[p])
						{
							case 'n' : ns += '\n'; break;
							case 't' : ns += '\t'; break;
							case 'r' : ns += '\r'; break;
							default:   ns += yytext[p]; break;
						}
				}
				else
					ns += yytext[p];
			yytext = ns;
			CSymbol ob = (CSymbol)m_parser.m_symbols.literals[yytext];
			if (ob!=null)
				return ob;
			m_parser.m_symbols.literals[yytext] = this;
			return this;
		}
		public bool CouldStart(CSymbol nonterm) { return false; }
		public override string TypeStr() { return "TOKEN"; }
#endif
		Literal() {}
		public new static object Serialise(object o,Serialiser s)
		{
			if (s==null)
				return new Literal();
			return CSymbol.Serialise(o,s);
		}
	}

	public class Production
	{
		public int m_pno;
#if (GENTIME)
		public CSymbol m_lhs;
		public bool m_actionsOnly;
		public int m_prec;
		public Production(SymbolsGen syms)
		{
			m_lhs=null;
			m_prec=0;
			m_pno=syms.pno++;
			m_actionsOnly = true;
			syms.prods.Add(this);
		}
		public Production(SymbolsGen syms,CSymbol lhs)
		{
			m_lhs=lhs;
			m_prec=0;
			m_pno=syms.pno++;
			m_actionsOnly=true;
			syms.prods.Add(this);
			lhs.m_prods.Add(m_pno);
		}
		public ObjectList m_rhs = new ObjectList(); // CSymbol
		public Hashtable m_alias = new Hashtable(); // string->int
		public void AddToRhs(CSymbol s)
		{
			m_rhs.Add(s);
			m_actionsOnly = m_actionsOnly && s.IsAction();
		}
		public void AddFirst(CSymbol s, int j)
		{
			for (;j<m_rhs.Count;j++)
			{
				CSymbol r = (CSymbol)m_rhs[j];
				s.AddFollow(r.m_first);
				if (!r.CouldBeEmpty())
					return;
			}
		}
		public bool CouldBeEmpty(int j)
		{
			for (;j<m_rhs.Count;j++)
			{
				CSymbol r = (CSymbol)m_rhs[j];
				if (!r.CouldBeEmpty())
					return false;
			}
			return true;
		}
		public CSymbol[] Prefix(int i)
		{
			CSymbol[] r = new CSymbol[i];
			for (int j=0;j<i;j++)
				r[j] = (CSymbol)m_rhs[j];
			return r;
		}

		// inside ACTIONs, $N translates to ((SomeSymbol *)(parser.StackAt(K-N-1).m_value))
		// where K is the position of the action in the production

		public void StackRef(ref string str,int ch, int ix)
		{
			int ln = m_rhs.Count+1;
			CSymbol ts = (CSymbol)m_rhs[ix-1];
			str += String.Format("\n\t(({0})(yyq.StackAt({1}).m_value))\n\t",ts.TypeStr(),ln-ix-1);
		}
#endif
		Production() {}
		public static object Serialise(object o,Serialiser s)
		{
			if (s==null)
				return new Production();
			Production p = (Production)o;
			if (s.Encode)
			{
				s.Serialise(p.m_pno);
				return null;
			}
			p.m_pno = (int)s.Deserialise();
			return p;
		}
	}
#if (GENTIME)
	public class ProdItem
	{
		public ProdItem(Production prod, int pos)
		{
			m_prod=prod;
			m_pos=pos;
			m_done=false;
		}
		public ProdItem()
		{
			m_prod = null;
			m_pos = 0;
			m_done=false;
		}
		public Production m_prod;
		public int m_pos;
		public bool m_done;
		public CSymbol Next()
		{
			if (m_pos<m_prod.m_rhs.Count)
				return (CSymbol)m_prod.m_rhs[m_pos];
			return null;

		}
		public bool IsReducingAction()
		{
			return (m_pos==m_prod.m_rhs.Count-1) && Next().IsAction();
		}
		SymbolSet follow = null;
		public SymbolSet FirstOfRest(SymbolsGen syms)
		{
			if (follow!=null)
				return follow;
			follow = new SymbolSet(syms);
			bool broke=false;
			int n = m_prod.m_rhs.Count;
			for (int j=m_pos+1;j<n;j++)
			{
				CSymbol s = (CSymbol)m_prod.m_rhs[j];
				foreach (CSymbol a in s.m_first.Keys)
					if (a!=syms.m_symbols.EmptySequence)
						follow.CheckIn(a);
				if (!s.m_first.Contains(syms.m_symbols.EmptySequence) &&!s.IsAction())
				{
					broke = true;
					break;
				}
			}
			if (!broke)
				follow.Add(m_prod.m_lhs.m_follow);
			follow = follow.Resolve();
			return follow;
		}
		public void Print()
		{
			int j;
			string str,s;

			if (m_prod.m_lhs!=null)
				str = m_prod.m_lhs.yytext;
			else
				str = "$start";
			Console.Write("   {0}    {1} : ", m_prod.m_pno, str);
			for (j = 0;j<m_prod.m_rhs.Count;j++)
			{
				if (j == m_pos)
					Console.Write("_");
				else
					Console.Write(" ");
				s =((CSymbol)m_prod.m_rhs[j]).yytext;
				if (s.Equals("\n"))
					s = "\\n";
				Console.Write(s);
			}
			if (j==m_pos)
				Console.Write("_");
			Console.Write("  ");
		}
	}

	internal class ProdItemList
	{
		public ProdItem m_pi;
		public ProdItemList m_next;
		public ProdItemList (ProdItem pi,ProdItemList n) { m_pi=pi; m_next=n; }
		public ProdItemList() { m_pi=null; m_next=null; } // sentinel only
		public bool Add(ProdItem pi)
		{
			if (m_pi==null)
			{  // m_pi==null iff m_next==null
				m_next = new ProdItemList();
				m_pi = pi;
			}
			else if (m_pi.m_prod.m_pno < pi.m_prod.m_pno ||
				(m_pi.m_prod.m_pno==pi.m_prod.m_pno && m_pi.m_pos<pi.m_pos))
			{
				m_next = new ProdItemList(m_pi,m_next);
				m_pi = pi;
			}
			else if (m_pi.m_prod.m_pno == pi.m_prod.m_pno && m_pi.m_pos==pi.m_pos)
				return false;
			else
				return m_next.Add(pi);
			return true; // was added
		}
		public bool AtEnd { get { return m_pi==null; } }

	}
#endif
	public class Parser
	{
		public Symbols m_symbols;
		public bool m_debug;
		public bool m_stkdebug=false;
		public Parser(Symbols syms,Lexer lexer)
		{
			m_lexer = lexer;
			m_symbols = syms;
			m_symbols.erh = m_lexer.tokens.erh;
		}
		public Lexer m_lexer;
		internal ObjectList m_stack = new ObjectList(); // ParseStackEntry
		internal SYMBOL m_ungot;

		void Create()
		{
			m_symbols.GetParser(m_lexer);
		}

		protected bool Error(ref ParseStackEntry top, string str)
		{
			SYMBOL er = (SYMBOL)new error(this,top.m_value);
			if (m_debug)
				Console.WriteLine("Error encountered: "+str);
			er.pos = top.m_value.pos;
			ParserEntry pe;
			if (m_symbols.symbolInfo["error"]!=null)
			// first pop the stack until we find an item that can pass error
			for (;top!=null && m_stack.Count>0; Pop(ref top,1,er))
			{
				if (m_debug)
					Console.WriteLine("Error recovery uncovers state {0}",top.m_state);
				if (er.Pass(m_symbols,top.m_state, out pe))
				{
					SYMBOL oldtop = top.m_value;
					top.m_value = er;
					pe.Pass(ref top); // pass the error symbol
					// now discard tokens until we find one we can pass
					while (top.m_value!=m_symbols.EOFSymbol && !top.m_value.Pass(m_symbols,top.m_state, out pe))
					{
						SYMBOL newtop;
						if (pe!=null && pe.IsReduce())
						{
							newtop = null;
							if (pe.m_action!=null)
								newtop = pe.m_action.Action(this); // before we change the stack
							m_ungot = top.m_value;
							Pop(ref top,((ParserReduce)pe).m_depth,er);
							newtop.pos = top.m_value.pos;
							top.m_value = newtop;
						}
						else
						{ // discard it
							string cnm = top.m_value.yyname();
							if (m_debug)
							{
								if (cnm=="TOKEN")
									Console.WriteLine("Error recovery discards literal {0}",(string)((TOKEN)top.m_value).yytext);
								else
									Console.WriteLine("Error recovery discards token {0}",cnm);
							}
							top.m_value = NextSym();
						}
					}
					if (m_debug)
						Console.WriteLine("Recovery complete");
					m_symbols.erh.counter++;
					return true;
				}
			}
			m_symbols.erh.Error(new CSToolsException(13,m_lexer,er.pos,"syntax error",str));
			return false;
		}

		public SYMBOL Parse(StreamReader input)
		{
			m_lexer.Start(input);
			return Parse();
		}
		public SYMBOL Parse(CsReader inFile)
		{
			m_lexer.Start(inFile);
			return Parse();
		}
		public SYMBOL Parse(string buf)
		{
			m_lexer.Start(buf);
			return Parse();
		}
		// The Parsing Algorithm
		SYMBOL Parse()
		{
			ParserEntry pe;
			SYMBOL newtop;
			Create();
			ParseStackEntry top = new ParseStackEntry(this,0,NextSym());
			for (;;)
			{
				string cnm = top.m_value.yyname();
				if (m_debug)
				{
					if (cnm.Equals("TOKEN"))
						Console.WriteLine(String.Format("State {0} with {1} \"{2}\"", top.m_state, cnm,((TOKEN)top.m_value).yytext));
					else
						Console.WriteLine(String.Format("State {0} with {1}", top.m_state, cnm));
				}
				if (top.m_value!=null && top.m_value.Pass(m_symbols,top.m_state, out pe))
					pe.Pass(ref top);
				else if (top.m_value==m_symbols.EOFSymbol)
				{
					if (top.m_state==m_symbols.m_accept.m_state)
					{ // successful parse
						Pop(ref top,1,m_symbols.m_startSymbol);
						newtop = top.m_value; // extract the return value
						top.m_value = null;
						if (m_symbols.erh.counter>0) // report that errors occurred
							return new error(this,newtop);
						return newtop;
					}
					if (!Error(ref top,"Unexpected EOF")) // unrecovered error
						return null;
				}
				else if (!Error(ref top,"syntax error")) // unrecovered error
					return null;
				if (m_debug)
				{
					object ob = null;
					if (top.m_value!=null)
					{
						ob = top.m_value.m_dollar;
						Console.WriteLine("In state {0} top {1} value {2}",top.m_state,top.m_value.yyname(),(ob==null)?"null":ob.GetType().Name);
						if (ob!=null && ob.GetType().Name.Equals("Int32"))
							Console.WriteLine((int)ob);
						else
							((SYMBOL)(top.m_value)).Print();
					}
					else
						Console.WriteLine("In state {0} top NULL",top.m_state);
				}
			}
			// not reached
		}
		internal void Push(ParseStackEntry elt)
		{
			m_stack.Push(elt);
		}
		internal void Pop(ref ParseStackEntry elt, int depth, SYMBOL ns)
		{
			for (;m_stack.Count>0 && depth>0;depth--)
			{
				elt = (ParseStackEntry)m_stack.Pop();
				if (m_symbols.m_concrete) // building the concrete syntax tree
					ns.kids.Push(elt.m_value); // else will be garbage collected
			}
			if (depth!=0)
				m_symbols.erh.Error(new CSToolsException(14,m_lexer,"Pop failed"));
		}
		public ParseStackEntry StackAt(int ix)
		{
			int n = m_stack.Count;
			if (m_stkdebug)
				Console.WriteLine("StackAt({0}),count {1}",ix,n);
			ParseStackEntry pe =(ParseStackEntry)m_stack[ix];
			if (pe == null)
				return new ParseStackEntry(this,0,m_symbols.Special);
			if (pe.m_value is Null)
				return new ParseStackEntry(this,pe.m_state,null);
			if (m_stkdebug)
				Console.WriteLine(pe.m_value.yyname());
			return pe;
		}
		public SYMBOL NextSym()
		{ // like lexer.Next but allows a one-token pushback for reduce
			SYMBOL ret = m_ungot;
			if (ret != null)
			{
				m_ungot = null;
				return ret;
			}
			ret = (SYMBOL)m_lexer.Next();
			if (ret==null)
				ret = m_symbols.EOFSymbol;
			return ret;
		}
		public void Error(int n,SYMBOL sym, string s)
		{
			if (sym!=null)
				m_symbols.erh.Error(new CSToolsException(n,sym.yylx,sym.pos,s,""));
			else
				m_symbols.erh.Error(new CSToolsException(n,s));
		}
	}

	public class error : SYMBOL
	{
		public SYMBOL ast = null;
		public error(Parser yyp,SYMBOL s):base(yyp) { ast=s; }
		public error(Parser yyp):base(yyp) {}
		public override string yyname() { return "error"; }
	}

	public class EOF : CSymbol
	{
#if (GENTIME)
		public EOF(SymbolsGen yyp):base(yyp) { yytext = "EOF"; m_symtype = SymType.eofsymbol; }
#endif
		public EOF(Lexer yyl):base(yyl) { yytext = "EOF"; m_symtype = SymType.eofsymbol; }
		EOF() {}
		public override string yyname() { return "EOF"; }
		public new static object Serialise(object o,Serialiser s)
		{
			if (s==null)
				return new EOF();
			return CSymbol.Serialise(o,s);
		}
	}

	public class Null : TOKEN  // fake up something that will evaluate to null but have the right yyname()
	{
		public Null(Lexer yyl,string proxy):base(yyl) { yytext=proxy; }
		public Null(Parser yyp,string proxy):base(yyp) { yytext=proxy; }
		public override string yyname() { return yytext; }
	}
	// Support for runtime object creation

	public delegate object SCreator(Parser yyp);

	public class Sfactory
	{
		public static object create(string cls_name,Parser yyp)
		{
			SCreator cr = (SCreator)yyp.m_symbols.types[cls_name];
			// Console.WriteLine("TCreating {0} <{1}>",cls_name,yyl.yytext);
			if (cr==null)
				yyp.m_symbols.erh.Error(new CSToolsException(16,yyp.m_lexer,"no factory for {"+cls_name+")"));
			try
			{
				return cr(yyp);
			}
			catch (CSToolsException e)
			{
				yyp.m_symbols.erh.Error(e);
			}
			catch (Exception e)
			{
				yyp.m_symbols.erh.Error(new CSToolsException(17,yyp.m_lexer,string.Format("Create of {0} failed ({1})",cls_name,e.Message)));
			}
			int j = cls_name.LastIndexOf('_');
			if (j>0)
			{
				cr = (SCreator)yyp.m_symbols.types[cls_name.Substring(0,j)];
				if (cr!=null)
				{
					SYMBOL s = (SYMBOL)cr(yyp);
					s.m_dollar = 0;
					return s;
				}
			}
			return null;
		}
		public Sfactory(Symbols syms,string cls_name,SCreator cr)
		{
			syms.types[cls_name] = cr;
		}
	}

}
