// ParserGenerator by Malcolm Crowe August 1995, 2000, 2003
// 2003 version (4.1+ of Tools) implements F. DeRemer & T. Pennello:
// Efficient Computation of LALR(1) Look-Ahead Sets
// ACM Transactions on Programming Languages and Systems
// Vol 4 (1982) p. 615-649
// See class SymbolsGen in parser.cs

using System.Collections;

public class ObjectList
{
	class Link {
		internal object it;
		internal Link next;
		internal Link(object o, Link x) { it=o;next=x; }
	}
	void Add0(Link a) {
		if (head==null)
			head = last = a;
		else
			last = last.next = a;
	}
	object Get0(Link a,int x) {
		if (a==null || x<0)  // safety
			return null;
		if (x==0)
			return a.it;
		return Get0(a.next,x-1);
	}
	private Link head = null, last=null;
	private int count = 0;
	public ObjectList() {}
	public void Add(object o) { Add0(new Link(o,null)); count++; }
	public void Push(object o) { head = new Link(o,head); count++; }
	public object Pop() { object r=head.it; head=head.next; count--; return r; }
	public object Top { get { return head.it; }}
	public int Count { get { return count; }}
	public object this[int ix] { get { return Get0(head,ix); } }
	public class OListEnumerator : IEnumerator
	{
		ObjectList list;
		Link cur = null;
		public object Current { get { return cur.it; }}
		public OListEnumerator(ObjectList o)
		{
			list = o;
		}
		public bool MoveNext()
		{
			if (cur==null)
				cur = list.head;
			else
				cur = cur.next;
			return cur!=null;
		}
		public void Reset()
		{
			cur = null;
		}
	}
	public IEnumerator GetEnumerator()
	{
		return new OListEnumerator(this);
	}
}
