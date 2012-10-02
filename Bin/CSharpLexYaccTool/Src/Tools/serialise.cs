// ParserGenerator by Malcolm Crowe August 1995, 2000, 2003
// 2003 version (4.1+ of Tools) implements F. DeRemer & T. Pennello:
// Efficient Computation of LALR(1) Look-Ahead Sets
// ACM Transactions on Programming Languages and Systems
// Vol 4 (1982) p. 615-649
// See class SymbolsGen in parser.cs

using System.Collections;
using System.IO;
using System.Text;
using System.Globalization;
using System;

namespace Tools
{
	public class Serialiser
	{
		enum SerType
		{
			Null, Int, Bool, Char, String, Hashtable, Encoding,
			UnicodeCategory, Symtype,
			Charset, TokClassDef, Action, Dfa,
			ParserOldAction, ParserSimpleAction, ParserShift, ParserReduce, ParseState,
			ParsingInfo, CSymbol, Literal, Production, EOF };
		delegate object ObjectSerialiser(object o,Serialiser s);
		static Serialiser()
		{
			srs[SerType.Null] = new ObjectSerialiser(NullSerialise);
			tps[typeof(int)] = SerType.Int; srs[SerType.Int] = new ObjectSerialiser(IntSerialise);
			tps[typeof(string)] = SerType.String; srs[SerType.String] = new ObjectSerialiser(StringSerialise);
			tps[typeof(Hashtable)] = SerType.Hashtable; srs[SerType.Hashtable] = new ObjectSerialiser(HashtableSerialise);
			tps[typeof(char)] = SerType.Char; srs[SerType.Char] = new ObjectSerialiser(CharSerialise);
			tps[typeof(bool)] = SerType.Bool; srs[SerType.Bool] = new ObjectSerialiser(BoolSerialise);
			tps[typeof(Encoding)] = SerType.Encoding; srs[SerType.Encoding] = new ObjectSerialiser(EncodingSerialise);
			tps[typeof(UnicodeCategory)] = SerType.UnicodeCategory; srs[SerType.UnicodeCategory] = new ObjectSerialiser(UnicodeCategorySerialise);
			tps[typeof(CSymbol.SymType)] = SerType.Symtype; srs[SerType.Symtype] = new ObjectSerialiser(SymtypeSerialise);
			tps[typeof(Charset)] = SerType.Charset; srs[SerType.Charset] = new ObjectSerialiser(Charset.Serialise);
			tps[typeof(TokClassDef)] = SerType.TokClassDef; srs[SerType.TokClassDef] = new ObjectSerialiser(TokClassDef.Serialise);
			tps[typeof(Dfa)] = SerType.Dfa; srs[SerType.Dfa] = new ObjectSerialiser(Dfa.Serialise);
			tps[typeof(Dfa.Action)] = SerType.Action; srs[SerType.Action] = new ObjectSerialiser(Dfa.Action.Serialise);
			tps[typeof(ParserOldAction)] = SerType.ParserOldAction; srs[SerType.ParserOldAction] = new ObjectSerialiser(ParserOldAction.Serialise);
			tps[typeof(ParserSimpleAction)] = SerType.ParserSimpleAction; srs[SerType.ParserSimpleAction] = new ObjectSerialiser(ParserSimpleAction.Serialise);
			tps[typeof(ParserShift)] = SerType.ParserShift; srs[SerType.ParserShift] = new ObjectSerialiser(ParserShift.Serialise);
			tps[typeof(ParserReduce)] = SerType.ParserReduce; srs[SerType.ParserReduce] = new ObjectSerialiser(ParserReduce.Serialise);
			tps[typeof(ParseState)] = SerType.ParseState; srs[SerType.ParseState] = new ObjectSerialiser(ParseState.Serialise);
			tps[typeof(ParsingInfo)] = SerType.ParsingInfo; srs[SerType.ParsingInfo] = new ObjectSerialiser(ParsingInfo.Serialise);
			tps[typeof(CSymbol)] = SerType.CSymbol; srs[SerType.CSymbol] = new ObjectSerialiser(CSymbol.Serialise);
			tps[typeof(Literal)] = SerType.Literal; srs[SerType.Literal] = new ObjectSerialiser(Literal.Serialise);
			tps[typeof(Production)] = SerType.Production; srs[SerType.Production] = new ObjectSerialiser(Production.Serialise);
			tps[typeof(EOF)] = SerType.EOF; srs[SerType.EOF] = new ObjectSerialiser(EOF.Serialise);
		}
		// on Encode, we ignore the return value which is always null
		// Otherwise, o if non-null is an instance of the subclass
		TextWriter f = null;
		int[] b = null;
		int pos = 0;
		Hashtable obs = new Hashtable(); // object->int (code) or int->object (decode)
		static Hashtable tps = new Hashtable(); // type->SerType
		static Hashtable srs = new Hashtable(); // SerType->ObjectSerialiser
		int id = 100;
		int cl = 0;
		public Serialiser(TextWriter ff)
		{
			f = ff;
		}
		public Serialiser(int[] bb)
		{
			b = bb;
		}
		public bool Encode { get { return f!=null; }}
		void _Write(SerType t)
		{
			_Write((int)t);
		}
		public void _Write(int i)
		{
			if (cl==5)
			{
				f.WriteLine();
				cl = 0;
			}
			cl++;
			f.Write(i);
			f.Write(",");
		}
		public int _Read()
		{
			return b[pos++];
		}
		static object NullSerialise(object o,Serialiser s)
		{
			return null;
		}
		static object IntSerialise(object o,Serialiser s)
		{
			if (s.Encode)
			{
				s._Write((int)o);
				return null;
			}
			return s._Read();
		}
		static object StringSerialise(object o,Serialiser s)
		{
			if (s==null)
				return "";
			Encoding e = new UnicodeEncoding();
			if (s.Encode)
			{
				byte[] b = e.GetBytes((string)o);
				s._Write(b.Length);
				for (int j=0;j<b.Length;j++)
					s._Write((int)b[j]);
				return null;
			}
			int ln = s._Read();
			byte[] bb = new byte[ln];
			for (int k=0;k<ln;k++)
				bb[k] = (byte)s._Read();
			string r = e.GetString(bb,0,ln);
			return r;
		}
		static object HashtableSerialise(object o,Serialiser s)
		{
			if (s==null)
				return new Hashtable();
			Hashtable h = (Hashtable)o;
			if (s.Encode)
			{
				s._Write(h.Count);
				foreach (DictionaryEntry d in h)
				{
					s.Serialise(d.Key);
					s.Serialise(d.Value);
				}
				return null;
			}
			int ct = s._Read();
			for (int j=0;j<ct;j++)
			{
				object k = s.Deserialise();
				object v = s.Deserialise();
				h[k] = v;
			}
			return h;
		}
		static object CharSerialise(object o,Serialiser s)
		{
			Encoding e = new UnicodeEncoding();
			if (s.Encode)
			{
				byte[] b = e.GetBytes(new string((char)o,1));
				s._Write((int)b[0]);
				s._Write((int)b[1]);
				return null;
			}
			byte[] bb = new byte[2];
			bb[0] = (byte)s._Read();
			bb[1] = (byte)s._Read();
			string r = e.GetString(bb,0,2);
			return r[0];
		}
		static object BoolSerialise(object o,Serialiser s)
		{
			if (s.Encode)
			{
				s._Write(((bool)o)?1:0);
				return null;
			}
			int v = s._Read();
			return v!=0;
		}
		static object EncodingSerialise(object o,Serialiser s)
		{
			if (s.Encode)
			{
				Encoding e = (Encoding)o;
				s.Serialise(e.WebName);
				return null;
			}
			switch((string)s.Deserialise())
			{
				case "us-ascii": return Encoding.ASCII;
				case "utf-16": return Encoding.Unicode;
				case "utf-7": return Encoding.UTF7;
				case "utf-8": return Encoding.UTF8;
			}
			throw new Exception("Unknown encoding");
		}
		static object UnicodeCategorySerialise(object o,Serialiser s)
		{
			if (s.Encode)
			{
				s._Write((int)o);
				return null;
			}
			return (UnicodeCategory)s._Read();
		}
		static object SymtypeSerialise(object o,Serialiser s)
		{
			if (s.Encode)
			{
				s._Write((int)o);
				return null;
			}
			return (CSymbol.SymType)s._Read();
		}
		public void Serialise(object o)
		{
			if (o==null)
			{
				_Write(SerType.Null);
				return;
			}
			if (o is Encoding)
			{
				_Write(SerType.Encoding);
				EncodingSerialise(o,this);
				return;
			}
			Type t = o.GetType();
			if (t.IsClass)
			{
				object p = obs[o];
				if (p!=null)
				{
					_Write((int)p);
					return;
				}
				else
				{
					int e = ++id;
					_Write(e);
					obs[o] = e;
				}
			}
			object so = tps[t];
			if (so!=null)
			{
				SerType s = (SerType)so;
				_Write(s);
				ObjectSerialiser os = (ObjectSerialiser)srs[s];
				os(o,this);
			}
			else
				throw new Exception("unknown type "+t.FullName);
		}
		public object Deserialise()
		{
			int t = _Read();
			int u = 0;
			if (t>100)
			{
				u = t;
				if (u<=obs.Count+100)
					return obs[u];
				t = _Read();
			}
			ObjectSerialiser os = (ObjectSerialiser)srs[(SerType)t];
			if (os!=null)
			{
				if (u>0)
				{
					object r = obs[u] = os(null,null); // allow for recursive structures: create and store first
					return obs[u] = os(r,this); // we need to do it again for strings
				}
				return os(null,this);
			}
			else
				throw new Exception("unknown type "+t);
		}
	}
/*	public class Test
	{
		static string curline = "";
		static int pos = 0;
		static bool EOF = false;
		static void GetLine(TextReader f)
		{
			curline = f.ReadLine();
			pos = 0;
			if (curline == null)
				EOF = true;
		}
		static int GetInt(TextReader f)
		{
			int v = 0;
			bool s = false;
			while (pos<curline.Length)
			{
				char c = curline[pos++];
				if (c==' ')
					continue;
				if (c=='-')
				{
					s = true;
					continue;
				}
				if (c==',')
				{
					if (s)
						v = -v;
					if (pos==curline.Length)
						GetLine(f);
					return v;
				}
				if (c>='0' && c<='9')
				{
					v = v*10 + (c-'0');
					continue;
				}
				throw new Exception("illegal character");
			}
			throw new Exception("bad line");
		}
		public static void Main(string[] args)
		{
			TextWriter x = new StreamWriter("out.txt");
			Hashtable t = new Hashtable();
			t["John"] = 12;
			t["Mary"] = 34;
			Serialiser sr = new Serialiser(x);
			sr.Serialise(t);
			x.Close();
			ArrayList a = new ArrayList();
			TextReader y = new StreamReader("out.txt");
			GetLine(y);
			while (!EOF)
				a.Add(GetInt(y));
			y.Close();
			for (int k=0;k<a.Count;k++)
				Console.WriteLine((int)a[k]);
			int[] b = new int[a.Count];
			for (int k=0;k<a.Count;k++)
				b[k] = (int)a[k];
			Serialiser dr = new Serialiser(b);
			Hashtable h = (Hashtable)dr.Deserialise();
			foreach (DictionaryEntry d in h)
				Console.WriteLine((string)d.Key + "->" + (int)d.Value);
		}
	} */
}