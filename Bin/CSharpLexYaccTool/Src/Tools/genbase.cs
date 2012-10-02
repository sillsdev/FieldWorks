// ParserGenerator by Malcolm Crowe August 1995, 2000, 2003
// 2003 version (4.1+ of Tools) implements F. DeRemer & T. Pennello:
// Efficient Computation of LALR(1) Look-Ahead Sets
// ACM Transactions on Programming Languages and Systems
// Vol 4 (1982) p. 615-649
// See class SymbolsGen in parser.cs

using System;
using System.IO;
using System.Text;
using YYClass;

namespace Tools
{
	public class GenBase
	{
		public ErrorHandler erh;
		protected GenBase(ErrorHandler eh) { erh = eh; }
		protected TextWriter m_outFile;
		protected Encoding m_scriptEncoding = Encoding.ASCII;
		protected bool toupper = false;
		protected string ScriptEncoding
		{
			set
			{
				m_scriptEncoding = Charset.GetEncoding(value,ref toupper,erh);
			}
		}
		public string m_outname = "tokens";
		// convenience functions
		protected int Braces(int a,string b,ref int p,int max)
		{
			int rv = a;
			int quote = 0;
			for (;p<max;p++)
				if (b[p]=='\\')
					p++;
				else if (quote==0 && b[p]=='{')
					rv++;
				else if (quote==0 && b[p]=='}')
				{
					if (--rv ==0)
					{
						p++;
						break;
					}
				}
				else if (b[p]==quote)
					quote=0;
				else if (b[p]=='\'' || b[p]=='"')
					quote = b[p];
			return rv;
		}
		protected string ToBraceIfFound(ref string buf,ref int p,ref int max,CsReader inf)
		{
			int q = p;
			int brack = Braces(0,buf,ref p,max);
			string rv = buf.Substring(q,p-q);
			while (inf!=null && brack>0)
			{
				buf=inf.ReadLine();
				if (buf==null)
					Error(47,q,"EOF in action or class def??");
				max=buf.Length;
				p=0;
				rv += '\n';
				brack = Braces(brack,buf,ref p,max);
				rv += buf.Substring(0,p);
			}
			return rv;
		}
		public bool White(string buf, ref int offset,int max)
		{
			while (offset<max &&
				(buf[offset]==' '||buf[offset]=='\t'))
				offset++;
			return offset<max; // false if nothing left
		}
		public bool NonWhite(string buf, ref int offset,int max)
		{
			while (offset<max &&
				(buf[offset]!=' ' && buf[offset]!='\t'))
				offset++;
			return offset<max; // false if nothing left
		}
		/// <summary/>
		public void EmitClassDefin(string b,ref int p,int max,CsReader inf,string defbas,out string bas, out string name,bool lx)
		{
			bool defconseen = false;
			name = "";
			bas = defbas;
			if (lx)
				NonWhite(b,ref p,max);
			White(b,ref p,max);
			for(;p<max&&b[p]!='{'&&b[p]!=':'&&b[p]!=';'&&b[p]!=' '&&b[p]!='\t'&&b[p]!='\n';p++)
				name += b[p];
			m_outFile.WriteLine("//%+{0}",name);
			m_outFile.WriteLine(@"/// <summary/>");
			m_outFile.Write("public class ");
			m_outFile.Write(name);
			White(b,ref p,max);
			if (b[p]==':')
			{
				p++;
				White(b,ref p,max);
				for(bas="";p<max&&b[p]!=' '&&b[p]!='{'&&b[p]!='\t'&&b[p]!=';'&&b[p]!='\n';p++)
					bas += b[p];
			}
			new TokClassDef(this,name,bas);
			m_outFile.Write(" : "+bas);
			m_outFile.WriteLine("{");
			do
			{
				if (p>=max)
				{
					b += inf.ReadLine();
					max = b.Length;
				}
				White(b,ref p,max);
			} while (p>=max);
			if (b[p]!=';')
			{
				cs0syntax syms = new cs0syntax(new yycs0syntax(),erh);
				cs0tokens tks = (cs0tokens)syms.m_lexer;
				tks.Out = m_outname;
//				syms.m_debug = true;
				syms.Cls = name;
				syms.Out = m_outname;
				if (lx)
				{
					syms.Ctx = "Lexer yyl";
					syms.Par = "yym";
				}
				else
				{
					syms.Ctx = "Parser yyp";
					syms.Par = "yyq";
				}
				string str = ToBraceIfFound(ref b,ref p, ref max,inf);
				TOKEN s = (TOKEN)syms.Parse(str);
				if (s==null)
				{
					Error(48,p,"Bad class definition for "+name);
					return;
				}
				s.yytext = s.yytext.Replace("yyq","(("+m_outname+")yyp)");
				s.yytext = s.yytext.Replace("yym","(("+m_outname+")yyl)");
				string[] ss = s.yytext.Split('\n');
				for (int j=0;j<ss.Length;j++)
					m_outFile.WriteLine(ss[j]);
				defconseen = syms.defconseen;
			}
			m_outFile.WriteLine(@"/// <summary/>");
			m_outFile.WriteLine("public override string yyname() { return \""+name+"\"; }");
			if (!defconseen)
			{
				if (lx)
				{
					m_outFile.WriteLine(@"/// <summary/>");
					m_outFile.WriteLine(@"/// <param name='yyl'></param>");
					m_outFile.Write("public "+name+"(Lexer yyl):base(yyl){}");
				}
				else
				{
					m_outFile.WriteLine(@"/// <summary/>");
					m_outFile.WriteLine(@"/// <param name='yyp'></param>");
					m_outFile.Write("public "+name+"(Parser yyp):base(yyp){}");
				}
			}
			m_outFile.WriteLine("}");
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="n"></param>
		/// <param name="p"></param>
		/// <param name="str"></param>
		public void Error(int n,int p,string str)
		{
			Console.WriteLine(str);
			if (m_outFile!=null)
			{
				m_outFile.WriteLine();
				m_outFile.WriteLine("#error Generator failed earlier. Fix the parser script and run ParserGenerator again.");
			}
			erh.Error(new CSToolsFatalException(n,line(p),position(p),"",str));
		}
		public virtual int line(int pos) { return 0; }
		public virtual int position(int pos) { return pos; }
		public virtual string Saypos(int pos) { return "at "+pos+": "; }
	}
}
