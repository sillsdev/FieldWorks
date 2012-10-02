// ParserGenerator by Malcolm Crowe August 1995, 2000, 2003
// 2003 version (4.1+ of Tools) implements F. DeRemer & T. Pennello:
// Efficient Computation of LALR(1) Look-Ahead Sets
// ACM Transactions on Programming Languages and Systems
// Vol 4 (1982) p. 615-649
// See class SymbolsGen in parser.cs

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Tools
{
	public abstract class LNode
	{
		public int m_state;
#if (GENTIME)
		public TokensGen m_tks;
		public LNode(TokensGen tks)
		{
			m_tks = tks;
			m_state = tks.NewState();
		}
#endif
		protected LNode() {}
	}

	public class TokClassDef
	{
#if (GENTIME)
		public TokClassDef(GenBase gbs,string name,string bas)
		{
			if (gbs is TokensGen)
			{
				TokensGen tks = (TokensGen) gbs;
				m_name=name;
				tks.m_tokens.tokens[name] = this;
				m_refToken=bas;
			}
		}
#endif
		TokClassDef() {}
		public string m_name = "";
		public string m_refToken = "";
		public string m_initialisation = "";
		public string m_implement = "";
		public static object Serialise(object o,Serialiser s)
		{
			if (s==null)
				return new TokClassDef();
			TokClassDef t = (TokClassDef)o;
			if (s.Encode)
			{
				s.Serialise(t.m_name);
				return null;
			}
			t.m_name = (string)s.Deserialise();
			return t;
		}
	}

	public class Dfa : LNode
	{
		Dfa() {}
#if (GENTIME)
		public Dfa(TokensGen tks) : base(tks)
		{
			m_tokens = tks.m_tokens;
		}
#endif
		Tokens m_tokens = null;
		public static void SetTokens(Tokens tks, Hashtable h) // needed after deserialisation
		{
			foreach (Dfa v in h.Values)
			{
				if (v.m_tokens!=null)
					continue;
				v.m_tokens = tks;
				Dfa.SetTokens(tks,v.m_map);
			}
		}
		public Hashtable m_map = new Hashtable(); // char->Dfa: arcs leaving this node
		public class Action
		{
			public int a_act;
			public Action a_next;
			public Action(int act,Action next) { a_act = act; a_next = next; }
			Action() {}
			public static object Serialise(object o,Serialiser s)
			{
				if (s==null)
					return new Action();
				Action a = (Action)o;
				if (s.Encode)
				{
					s.Serialise(a.a_act);
					s.Serialise(a.a_next);
					return null;
				}
				a.a_act = (int)s.Deserialise();
				a.a_next = (Action)s.Deserialise();
				return a;
			}
		}
		public string m_tokClass = ""; // token class name if m_actions!=null
		public Action m_actions; // for old-style REJECT
#if (GENTIME)
		void AddAction(int act)
		{
			Action a = new Action(act,m_actions);
			m_actions = a;
		}
		void MakeLastAction(int act)
		{
			while (m_actions!=null && m_actions.a_act>=act)
				m_actions = m_actions.a_next;
			AddAction(act);
		}
		public Dfa(Nfa nfa):base(nfa.m_tks)
		{
			m_tokens = m_tks.m_tokens;
			AddNfaNode(nfa); // the starting node is Closure(start)
			Closure();
			AddActions(); // recursively build the Dfa
		}
		internal bool AddNfaNode(NfaNode nfa)
		{
			if (!m_nfa.Add(nfa))
				return false;
			if (nfa.m_sTerminal!="")
			{
				int qi,n;
				string tokClass = "";
				string p=nfa.m_sTerminal;
				if (p[0]=='%')
				{ // check for %Tokname special action
					for (n=0,qi=1;qi<p.Length;qi++,n++) // extract the class name
						if (p[qi]==' '||p[qi]=='\t'||p[qi]=='\n'||p[qi]=='{'||p[qi]==':')
							break;
					tokClass = nfa.m_sTerminal.Substring(1,n);
				}
				// special action is always last in the list
				if (tokClass=="")
				{ //nfa has an old action
					if (m_tokClass=="" // if both are old-style
						|| // or we have a special action that is later
						(m_actions.a_act)>nfa.m_state)   // m_actions has at least one entry
						AddAction(nfa.m_state);
					// else we have a higher-precedence special action so we do nothing
				}
				else if (m_actions==null || m_actions.a_act>nfa.m_state)
				{
					MakeLastAction(nfa.m_state);
					m_tokClass = tokClass;
				} // else we have a higher-precedence special action so we do nothing
			}
			return true;
		}

		internal NList m_nfa = new NList(); // nfa nodes in m_state order

		internal void AddActions()
		{
			// This routine is called for a new DFA node
			m_tks.states.Add(this);

			// Follow all the arcs from here
			foreach (Charset cs in m_tks.m_tokens.cats.Values)
				foreach (char j in cs.m_chars.Keys)
				{
					Dfa dfa = Target(j);
					if (dfa!=null)
						m_map[j] = dfa;
				}
		}

		internal Dfa Target(char ch)
		{ // construct or lookup the target for a new arc
			Dfa n = new Dfa(m_tks);

			for (NList pos = m_nfa; !pos.AtEnd; pos=pos.m_next)
				pos.m_node.AddTarget(ch,n);
			// check we actually got something
			if (n.m_nfa.AtEnd)
				return null;
			n.Closure();
			// now check we haven't got it already
			for (int pos1=0;pos1<m_tks.states.Count;pos1++)
				if (((Dfa)m_tks.states[pos1]).SameAs(n))
					return (Dfa)m_tks.states[pos1];
			// this is a brand new Dfa node so recursively build it
			n.AddActions();
			return n;
		}
		void Closure()
		{
			for (NList pos=m_nfa; !pos.AtEnd; pos=pos.m_next)
				ClosureAdd(pos.m_node);
		}
		void ClosureAdd(NfaNode nfa)
		{
			for (int pos=0;pos<nfa.m_eps.Count;pos++)
			{
				NfaNode p = (NfaNode)nfa.m_eps[pos];
				if (AddNfaNode(p))
					ClosureAdd(p);
			}
		}
		internal bool SameAs(Dfa dfa)
		{
			NList pos1 = m_nfa;
			NList pos2 = dfa.m_nfa;
			while (pos1.m_node==pos2.m_node && !pos1.AtEnd)
			{
				pos1 = pos1.m_next;
				pos2 = pos2.m_next;
			}
			return pos1.m_node==pos2.m_node;
		}
		// match a Dfa agsint a given string
		public int Match(string str,int ix,ref int action)
		{ // return number of chars matched
			int r=0;
			Dfa dfa=null;
			// if there is no arc or the string is exhausted, this is okay at a terminal
			if (ix>=str.Length ||
				(dfa=((Dfa)m_map[m_tokens.Filter(str[ix])]))==null ||
				(r=dfa.Match(str,ix+1,ref action))<0)
			{
				if (m_actions!=null)
				{
					action = m_actions.a_act;
					return 0;
				}
				return -1;
			}
			return r+1;
		}
		public void Print()
		{
			Console.Write("{0}:",m_state);
			if (m_actions!=null)
			{
				Console.Write(" (");
				for (Action a = m_actions; a!=null; a=a.a_next)
					Console.Write("{0} <",a.a_act);
				if (m_tokClass!="")
					Console.Write(m_tokClass);
				Console.Write(">)");
			}
			Console.WriteLine();
			Hashtable amap = new Hashtable(); // char->bool
			IDictionaryEnumerator idx = m_map.GetEnumerator();
			for (int count=m_map.Count; count-->0;)
			{
				idx.MoveNext();
				char j = (char)idx.Key;
				Dfa pD = (Dfa)idx.Value;
				if (!amap.Contains(j))
				{
					amap[j] = true;
					Console.Write("  {0}  ",pD.m_state);
					int ij = (int)j;
					if (ij>=32 && ij<128)
						Console.Write(j);
					else
						Console.Write(" #{0} ",ij);
					IDictionaryEnumerator idy = m_map.GetEnumerator();
					for (;;)
					{
						idy.MoveNext();
						Dfa pD1 = (Dfa)idy.Value;
						if (pD1==pD)
							break;
					}
					for (int count1=count;count1>0;count1--)
					{
						idy.MoveNext();
						j = (char)idy.Key;
						Dfa pD1 = (Dfa)idy.Value;
						if (pD==pD1)
						{
							amap[j]=true;
							ij = (int)j;
							if (ij>=32 && ij<128)
								Console.Write(j);
							else
								Console.Write(" #{0} ",ij);
						}
					}
					Console.WriteLine();
				}
			}
		}
#endif
		public static object Serialise(object o,Serialiser s)
		{
			if (s==null)
				return new Dfa();
			Dfa d = (Dfa)o;
			if (s.Encode)
			{
				s.Serialise(d.m_state);
				s.Serialise(d.m_map);
				s.Serialise(d.m_actions);
				s.Serialise(d.m_tokClass);
				return null;
			}
			d.m_state = (int)s.Deserialise();
			d.m_map = (Hashtable)s.Deserialise();
			d.m_actions = (Action)s.Deserialise();
			d.m_tokClass = (string)s.Deserialise();
			return d;
		}
	}
#if (GENTIME)
	public class Regex
	{
		/*
			Construct a Regex from a given string

		1.  First examine the given string.
			If it is empty, there is nothing to do, so return (having cleared m_sub as a precaution).
		2.  Look to see if the string begins with a bracket ( . If so, find the matching ) .
			This is not as simple as it might be because )s inside quotes or [] or escaped will not count.
			Recursively call the constructor for the regular expression between the () s.
			Mark everything up to the ) as used, and go to step 9.
		3.  Look to see if the string begins with a bracket [ . If so, find the matching ] , watching for escapes.
			Construct a ReRange for everything between the []s.
			Mark everything up to the ] as used, and go to step 9.
		4.  Look to see if the string begins with a ' or " . If so, build the contents interpreting
			escaped special characters correctly, until the matching quote is reached.
			Construct a ReStr for the contents, mark everything up to the final quote as used, and go to step 9.
		4a.  Look to see if the string begins with a U' or U" . If so, build the contents interpreting
			escaped special characters correctly, until the matching quote is reached.
			Construct a ReUStr for the contents, mark everything up to the final quote as used, and go to step 9.
		5.  Look to see if the string begins with a \ .
			If so, build a ReStr for the next character (special action for ntr),
			mark it as used, and go to step 9.
		6.  Look to see if the string begins with a { .
			If so, find the matching }, lookup the symbolic name in the definitions table,
			recursively call this constructor on the contents,
			mark everything up to the } as used, and go to step 9.
		7.  Look to see if the string begins with a dot.
			If so, construct a ReRange("^\n"), mark the . as used, and go to step 9.
		8.  At this point we conclude that there is a simple character at the start of the regular expression.
			Construct a ReStr for it, mark it as used, and go to step 9.
		9.  If the string is exhausted, return.
			We have a simple Regex whose m_sub contains what we can constructed.
		10.  If the next character is a ? , *, or +, construct a ReOpt, ReStart, or RePlus respectively
			out of m_sub, and make m_sub point to this new class instead. Mark the character as used.
		11.  If the string is exhausted, return.
		12.  If the next character is a | , build a ReAlt using the m_sub we have and the rest of the string.
		13.  Otherwise build a ReCat using the m_sub we have and the rest of the string.
		*/
		public Regex(TokensGen tks,int p,string str)
		{
			int n = str.Length;
			int nlp = 0;
			int lbrack = 0;
			int quote = 0;
			int j;
			char ch;

			//1.  First examine the given string.
			//	If it is empty, there is nothing to do, so return (having cleared m_sub as a precaution).
			m_sub = null;
			if (n==0)
				return;
				//2.  Look to see if the string begins with a bracket ( . If so, find the matching ) .
				//	This is not as simple as it might be because )s inside quotes or [] or escaped will not count.
				// 	Recursively call the constructor for the regular expression between the () s.
				// 	Mark everything up to the ) as used, and go to step 9.
			else if (str[0]=='(')
			{ // identify a bracketed expression
				for (j=1;j<n;j++)
					if (str[j]=='\\')
						j++;
					else if (str[j]=='[' && quote==0 && lbrack==0)
						lbrack++;
					else if (str[j]==']' && lbrack>0)
						lbrack = 0;
					else if (str[j]=='"' || str[j]=='\'')
					{
						if (quote==str[j])
							quote = 0;
						else if (quote==0)
							quote = str[j];
					}
					else if (str[j]=='(' && quote==0 && lbrack==0)
						nlp++;
					else if (str[j]==')' && quote==0 && lbrack==0 && nlp--==0)
						break;
				if (j==n)
					goto bad;
				m_sub = new Regex (tks,p+1,str.Substring(1,j-1));
				j++;
				//3.  Look to see if the string begins with a bracket [ . If so, find the matching ] , watching for escapes.
				//	Construct a ReRange for everything between the []s.
				//	Mark everything up to the ] as used, and go to step 9.
			}
			else if (str[0]=='[')
			{	   	// range of characters
				for (j=1;j<n && str[j]!=']';j++)
					if (str[j]=='\\')
						j++;
				if (j==n)
					goto bad;
				m_sub = new ReRange(tks,str.Substring(0,j+1));
				j++;
			}
			//4.  Look to see if the string begins with a ' or " . If so, build the contents interpreting
			//	escaped special characters correctly, until the matching quote is reached.
			//	Construct a CReStr for the contents, mark everything up to the final quote as used, and go to step 9.
			else if (str[0] == '\'' || str[0] == '"')
			{  // quoted string needs special treatment
				string qs ="";
				for (j=1;j<n && str[j]!=str[0];j++)
					if (str[j]=='\\')
						switch (str[++j])
						{
							case 'n':	qs += '\n'; break;
							case 'r':	qs += '\r'; break;
							case 't':	qs += '\t'; break;
							case '\\':	qs += '\\'; break;
							case '\'':	qs += '\''; break;
							case '"':	qs += '"'; break;
							case '\n':	break;
							default:	qs += str[j]; break;
						}
					else
						qs += str[j];
				if (j==n)
					goto bad;
				j++;
				m_sub = new ReStr(tks,qs);
			}
				//4a.  Look to see if the string begins with a U' or U" . If so, build the contents interpreting
				//	escaped special characters correctly, until the matching quote is reached.
				//	Construct a ReUStr for the contents, mark everything up to the final quote as used, and go to step 9.
			else if (str.StartsWith("U\"")||str.StartsWith("U'"))
			{  // quoted string needs special treatment
				string qs ="";
				for (j=2;j<n && str[j]!=str[1];j++)
					if (str[j]=='\\')
						switch (str[++j])
						{
							case 'n':	qs += '\n'; break;
							case 'r':	qs += '\r'; break;
							case 't':	qs += '\t'; break;
							case '\\':	qs += '\\'; break;
							case '\'':	qs += '\''; break;
							case '"':	qs += '"'; break;
							case '\n':	break;
							default:	qs += str[j]; break;
						}
					else
						qs += str[j];
				if (j==n)
					goto bad;
				j++;
				m_sub = new ReUStr(tks,qs);
			}
				//5.  Look to see if the string begins with a \ .
			//	If so, build a ReStr for the next character (special action for ntr),
			//	mark it as used, and go to step 9.
			else if (str[0]=='\\')
			{
				switch (ch = str[1])
				{
					case 'n': ch = '\n'; break;
					case 't': ch = '\t'; break;
					case 'r': ch = '\r'; break;
				}
				m_sub = new ReStr(tks,ch);
				j = 2;
				//6.  Look to see if the string begins with a { .
				//	If so, find the matching }, lookup the symbolic name in the definitions table,
				//	recursively call this constructor on the contents,
				//	mark everything up to the } as used, and go to step 9.
			}
			else if (str[0]=='{')
			{
				for (j=1;j<n && str[j]!='}';j++)
					;
				if (j==n)
					goto bad;
				string ds = str.Substring(1,j-1);
				string s = (string)tks.defines[ds];
				if (s==null)
					m_sub = new ReCategory(tks,ds);
				else
					m_sub = new Regex(tks,p+1,s);
				j++;
			}
			else
			{	  // simple character at start of regular expression
				//7.  Look to see if the string begins with a dot.
				//	If so, construct a CReDot, mark the . as used, and go to step 9.
				if (str[0]=='.')
					m_sub = new ReRange(tks,"[^\n]");
					//8.  At this point we conclude that there is a simple character at the start of the regular expression.
					//	Construct a ReStr for it, mark it as used, and go to step 9.
				else
					m_sub = new ReStr(tks,str[0]);
				j = 1;
			}
			//9.  If the string is exhausted, return.
			//	We have a simple Regex whose m_sub contains what we can constructed.
			if (j>=n)
				return;
			//10.  If the next character is a ? , *, or +, construct a CReOpt, CReStart, or CRePlus respectively
			//	out of m_sub, and make m_sub point to this new class instead. Mark the character as used.
			if (str[j]=='?')
			{
				m_sub = new ReOpt(m_sub);
				j++;
			}
			else if (str[j]=='*')
			{
				m_sub = new ReStar(m_sub);
				j++;
			}
			else if (str[j]=='+')
			{
				m_sub = new RePlus(m_sub);
				j++;
			}
			// 11.  If the string is exhausted, return.
			if (j>=n)
				return;
			// 12.  If the next character is a | , build a ReAlt using the m_sub we have and the rest of the string.
			if (str[j]=='|')
				m_sub = new ReAlt(tks,m_sub,p+j+1,str.Substring(j+1,n-j-1));
				// 13.  Otherwise build a ReCat using the m_sub we have and the rest of the string.
			else if (j<n)
				m_sub = new ReCat(tks,m_sub,p+j,str.Substring(j,n-j));
			return;
			bad:
				tks.erh.Error(new CSToolsFatalException(1,tks.line(p),tks.position(p),str,String.Format("ill-formed regular expression <{0}>\n", str)));
		}
		protected Regex() {} // private
		public Regex m_sub;
		public virtual void Print(TextWriter s)
		{
			if (m_sub!=null)
				m_sub.Print(s);
		}
		// Match(ch) is used only in arc handling for ReRange
		public virtual bool Match(char ch) { return false; }
		// These two Match methods are only required if you want to use
		// the Regex direcly for lexing. This is a very strange thing to do:
		// it is non-deterministic and rather slow.
		public int Match(string str)
		{
			return Match(str,0,str.Length);
		}
		public virtual int Match(string str,int pos,int max)
		{
			if (max<0)
				return -1;
			if (m_sub!=null)
				return m_sub.Match(str,pos,max);
			return 0;
		}
		public virtual void Build(Nfa nfa)
		{
			if (m_sub!=null)
			{
				Nfa sub = new Nfa(nfa.m_tks,m_sub);
				nfa.AddEps(sub);
				sub.m_end.AddEps(nfa.m_end);
			}
			else
				nfa.AddEps(nfa.m_end);
		}
	}

	internal class ReAlt : Regex
	{
		public ReAlt(TokensGen tks,Regex sub,int p,string str)
		{
			m_sub = sub;
			m_alt = new Regex(tks,p,str);
		}
		public Regex m_alt;
		public override void Print(TextWriter s)
		{
			s.Write("(");
			if (m_sub!=null)
				m_sub.Print(s);
			s.Write("|");
			if (m_alt!=null)
				m_alt.Print(s);
			s.Write(")");
		}
		public override int Match(string str, int pos, int max)
		{
			int a= -1, b= -1;
			if (m_sub!=null)
				a = m_sub.Match(str, pos, max);
			if (m_alt!=null)
				b = m_sub.Match(str, pos, max);
			return (a>b)?a:b;
		}
		public override void Build(Nfa nfa)
		{
			if (m_alt!=null)
			{
				Nfa alt = new Nfa(nfa.m_tks,m_alt);
				nfa.AddEps(alt);
				alt.m_end.AddEps(nfa.m_end);
			}
			base.Build(nfa);
		}
	}

	internal class ReCat : Regex
	{
		public ReCat(TokensGen tks,Regex sub, int p, string str)
		{
			m_sub = sub;
			m_next = new Regex(tks,p,str);
		}
		Regex m_next;
		public override void Print(TextWriter s)
		{
			s.Write("(");
			if (m_sub!=null)
				m_sub.Print(s);
			s.Write(")(");
			if (m_next!=null)
				m_next.Print(s);
			s.Write(")");
		}
		public override int Match(string str, int pos, int max)
		{
			int first, a, b, r = -1;

			if (m_next==null)
				return base.Match(str,pos,max);
			if (m_sub==null)
				return m_next.Match(str,pos,max);
			for (first = max;first>=0;first=a-1)
			{
				a = m_sub.Match(str,pos,first);
				if (a<0)
					break;
				b = m_next.Match(str,pos+a,max);
				if (b<0)
					continue;
				if (a+b>r)
					r = a+b;
			}
			return r;
		}
		public override void Build(Nfa nfa)
		{
			if (m_next!=null)
			{
				if (m_sub!=null)
				{
					Nfa first = new Nfa(nfa.m_tks,m_sub);
					Nfa second = new Nfa(nfa.m_tks,m_next);
					nfa.AddEps(first);
					first.m_end.AddEps(second);
					second.m_end.AddEps(nfa.m_end);
				}
				else
					m_next.Build(nfa);
			}
			else
				base.Build(nfa);
		}
	}

	internal class ReStr : Regex
	{
		public ReStr() {}
		public ReStr(TokensGen tks,string str)
		{
			m_str = str;
			for (int i=0;i<str.Length;i++)
				tks.m_tokens.UsingChar(str[i]);
		}
		public ReStr(TokensGen tks,char ch)
		{
			m_str = new string(ch,1);
			tks.m_tokens.UsingChar(ch);
		}
		public string m_str = "";
		public override void Print(TextWriter s)
		{
			s.Write(String.Format("(\"{0}\")",m_str));
		}
		public override int Match(string str, int pos, int max)
		{
			int j,n = m_str.Length;

			if (n>max)
				return -1;
			if (n>max-pos)
				return -1;
			for(j=0;j<n;j++)
				if (str[j]!=m_str[j])
					return -1;
			return n;
		}
		public override void Build(Nfa nfa)
		{
			int j,n = m_str.Length;
			NfaNode p, pp = nfa;

			for (j=0;j<n;pp = p,j++)
			{
				p = new NfaNode(nfa.m_tks);
				pp.AddArc(m_str[j],p);
			}
			pp.AddEps(nfa.m_end);
		}
	}

	internal class ReUStr : ReStr
	{
		public ReUStr(TokensGen tks,string str)
		{
			m_str = str;
			for (int i=0;i<str.Length;i++)
			{
				tks.m_tokens.UsingChar(Char.ToLower(str[i]));
				tks.m_tokens.UsingChar(Char.ToUpper(str[i]));
			}
		}
		public ReUStr(TokensGen tks,char ch)
		{
			m_str = new string(ch,1);
			tks.m_tokens.UsingChar(Char.ToLower(ch));
			tks.m_tokens.UsingChar(Char.ToUpper(ch));
		}
		public override void Print(TextWriter s)
		{
			s.Write(String.Format("(U\"{0}\")",m_str));
		}
		public override int Match(string str, int pos, int max)
		{
			int j,n = m_str.Length;

			if (n>max)
				return -1;
			if (n>max-pos)
				return -1;
			for(j=0;j<n;j++)
				if (Char.ToUpper(str[j])!=Char.ToUpper(m_str[j]))
					return -1;
			return n;
		}
		public override void Build(Nfa nfa)
		{
			int j,n = m_str.Length;
			NfaNode p, pp = nfa;

			for (j=0;j<n;pp = p,j++)
			{
				p = new NfaNode(nfa.m_tks);
				pp.AddUArc(m_str[j],p);
			}
			pp.AddEps(nfa.m_end);
		}
	}

	internal class ReCategory : Regex
	{
		public ReCategory(TokensGen tks,string str)
		{
			m_str = str;
			m_test = tks.m_tokens.GetTest(str);
		}
		string m_str;
		ChTest m_test;
		public override bool Match(char ch) { return m_test(ch); }
		public override void Print(TextWriter s)
		{
			s.WriteLine("{"+m_str+"}");
		}
		public override void Build(Nfa nfa)
		{
			nfa.AddArcEx(this,nfa.m_end);
		}
	}

	internal class ReRange : Regex
	{
		public ReRange(TokensGen tks,string str)
		{
			string ns = "";
			int n = str.Length-1,v;
			int p;

			for (p=1;p<n;p++) // fix \ escapes
				if (str[p] == '\\')
				{
					if (p+1<n)
						p++;
					if (str[p]>='0' && str[p]<='7')
					{
						for (v = str[p++]-'0';p<n && str[p]>='0' && str[p]<='7';p++)
							v=v*8+str[p]-'0';
						ns += (char)v;
					}
					else
						switch(str[p])
						{
							case 'n' : ns += '\n'; break;
							case 't' : ns += '\t'; break;
							case 'r' : ns += '\r'; break;
							default:   ns += str[p]; break;
						}
				}
				else
					ns += str[p];
			n = ns.Length;
			if (ns[0] == '^')
			{// invert range
				m_invert = true;
				ns = ns.Substring(1)+(char)0 +(char)0xFFFF;
			}
			for (p=0;p<n;p++)
				if (p+1<n && ns[p+1]=='-')
				{
					for (v=ns[p];v<=ns[p+2];v++)
						Set(tks,(char)v);
					p += 2;
				}
				else
					Set(tks,ns[p]);
		}
		public Hashtable m_map = new Hashtable(); // char->bool
		public bool m_invert = false; // implement ^
		public override void Print(TextWriter s)
		{
			s.Write("[");
			if (m_invert)
				s.Write("^");
			foreach (char x in m_map.Keys)
				s.Write(x);
			s.Write("]");
		}
		void Set(TokensGen tks,char ch)
		{
			m_map[ch] = true;
			tks.m_tokens.UsingChar(ch);
		}
		public override bool Match(char ch)
		{
			if (m_invert)
				return !m_map.Contains(ch);
			return m_map.Contains(ch);
		}
		public override int Match(string str, int pos, int max)
		{
			if (max<pos)
				return -1;
			return Match(str[pos])?1:-1;
		}
		public override void Build(Nfa nfa)
		{
			nfa.AddArcEx(this,nfa.m_end);
		}
	}

	internal class ReOpt : Regex
	{
		public ReOpt(Regex sub) { m_sub = sub; }
		public override	void Print(TextWriter s)
		{
			m_sub.Print(s);
			s.Write("?");
		}
		public override int Match(string str, int pos, int max)
		{
			int r;

			r = m_sub.Match(str, pos, max);
			if (r<0)
				r = 0;
			return r;
		}
		public override void Build(Nfa nfa)
		{
			nfa.AddEps(nfa.m_end);
			base.Build(nfa);
		}
	}

	internal class RePlus : Regex
	{
		public RePlus(Regex sub) {m_sub = sub; }
		public override void Print(TextWriter s)
		{
			m_sub.Print(s);
			s.Write("+");
		}
		public override int Match(string str, int pos, int max)
		{
			int n,r;

			r = m_sub.Match(str, pos, max);
			if (r<0)
				return -1;
			for (n=r;r>0;n+=r)
			{
				r = m_sub.Match(str, pos+n, max);
				if (r<0)
					break;
			}
			return n;
		}
		public override void Build(Nfa nfa)
		{
			base.Build(nfa);
			nfa.m_end.AddEps(nfa);
		}
	}

	internal class ReStar : Regex
	{
		public ReStar(Regex sub) {m_sub = sub; }
		public override void Print(TextWriter s)
		{
			m_sub.Print(s);
			s.Write("*");
		}
		public override int Match(string str, int pos, int max)
		{
			int n,r;

			r = m_sub.Match(str,pos,max);
			if (r<0)
				return -1;
			for (n=0;r>0;n+=r)
			{
				r = m_sub.Match(str, pos+n, max);
				if (r<0)
					break;
			}
			return n;
		}
		public override void Build(Nfa nfa)
		{
			Nfa sub = new Nfa(nfa.m_tks,m_sub);
			nfa.AddEps(sub);
			nfa.AddEps(nfa.m_end);
			sub.m_end.AddEps(nfa);
		}
	}

	/* The .NET Framework has its own Regex class which is an NFA recogniser
	We don't want to use this for lexing because
		it would be too slow (DFA is always faster)
		programming in actions looks difficult
		we want to explain the NFA->DFA algorithm to students
	So in this project we are not using the Framework's Regex class but the one defined in regex.cs
	*/

	internal class Arc
	{
		public char m_ch;
		public NfaNode m_next;
		public Arc() {}
		public Arc(char ch, NfaNode next) { m_ch=ch; m_next=next; }
		public virtual bool Match(char ch)
		{
			return ch==m_ch;
		}
		public virtual void Print(TextWriter s)
		{
			s.WriteLine(String.Format("  {0} {1}",m_ch,m_next.m_state));
		}
	}

	internal class ArcEx : Arc
	{
		public Regex m_ref;
		public ArcEx(Regex re,NfaNode next) { m_ref=re; m_next=next; }
		public override bool Match(char ch)
		{
			return m_ref.Match(ch);
		}
		public override void Print(TextWriter s)
		{
			s.Write("  ");
			m_ref.Print(s);
			s.WriteLine(m_next.m_state);
		}
	}

	internal class UArc : Arc
	{
		public UArc() {}
		public UArc(char ch,NfaNode next) : base(ch,next) {}
		public override bool Match(char ch)
		{
			return Char.ToUpper(ch)==Char.ToUpper(m_ch);
		}
		public override void Print(TextWriter s)
		{
			s.WriteLine(String.Format("  U\'{0}\' {1}",m_ch,m_next.m_state));
		}
	}

	public class NfaNode : LNode
	{
		public string m_sTerminal = ""; // or something for the Lexer
		public ObjectList m_arcs = new ObjectList(); // of Arc for labelled arcs
		public ObjectList m_eps = new ObjectList(); // of NfaNode for unlabelled arcs
		public NfaNode(TokensGen tks):base(tks) {}

		// build helpers
		public void AddArc(char ch,NfaNode next)
		{
			m_arcs.Add(new Arc(ch,next));
		}
		public void AddUArc(char ch,NfaNode next)
		{
			m_arcs.Add(new UArc(ch,next));
		}
		public void AddArcEx(Regex re,NfaNode next)
		{
			m_arcs.Add(new ArcEx(re,next));
		}
		public void AddEps(NfaNode next)
		{
			m_eps.Add(next);
		}

		// helper for building DFa
		public void AddTarget(char ch, Dfa next)
		{
			for (int j=0; j<m_arcs.Count; j++)
			{
				Arc a = (Arc)m_arcs[j];
				if (a.Match(ch))
					next.AddNfaNode(a.m_next);
			}
		}
	}

	// An NFA is defined by a start and end state
	// Here we derive the Nfa from a NfaNode which acts as the start state

	public class Nfa : NfaNode
	{
		public NfaNode m_end;
		public Nfa(TokensGen tks) : base(tks)
		{
			m_end = new NfaNode(m_tks);
		}
		// build an NFA for a given regular expression
		public Nfa(TokensGen tks,Regex re) : base(tks)
		{
			m_end = new NfaNode(tks);
			re.Build(this);
		}
	}

	// shame we have to do this ourselves, but SortedList doesn't allow incremental building of Dfas
	internal class NList
	{ // sorted List of NfaNode
		public NfaNode m_node; // null for the sentinel
		public NList m_next;
		public NList() { m_node=null; m_next=null; } // sentinel only
		NList(NfaNode nd,NList nx) { m_node=nd; m_next=nx; }
		public bool Add(NfaNode n)
		{
			if (m_node==null)
			{  // m_node==null iff m_next==null
				m_next = new NList();
				m_node = n;
			}
			else if (m_node.m_state < n.m_state)
			{
				m_next = new NList(m_node,m_next);
				m_node = n;
			}
			else if (m_node.m_state == n.m_state)
				return false;  // Add fails, there already
			else
				return m_next.Add(n);
			return true; // was added
		}
		public bool AtEnd { get { return m_node==null; } }
	}
#endif
}
