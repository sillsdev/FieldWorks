using SIL.FieldWorks.XWorks;
using System;
using System.Text;

public class StringFragment : IFragment
{
	public StringBuilder StrBuilder { get; set; }

	public StringFragment()
	{
		StrBuilder = new StringBuilder();
	}

	// Create a new string fragment linked to an existing string builder.
	public StringFragment(StringBuilder bldr)
	{
		StrBuilder = bldr;
	}

	// Create a new string fragment containing the given string.
	public StringFragment(string str) : this()
	{
		// Add text to the fragment
		StrBuilder.Append(str);
	}

	public override string ToString()
	{
		if (StrBuilder == null)
			return String.Empty;
		return StrBuilder.ToString();
	}

	public int Length()
	{
		if (StrBuilder == null)
			return 0;
		return StrBuilder.Length;
	}

	public void Append(IFragment frag)
	{
		if (frag != null)
			StrBuilder.Append(frag.ToString());
	}

	public void AppendBreak()
	{
		StrBuilder.AppendLine();
	}

	public void TrimEnd(char c)
	{
		string curString = StrBuilder.ToString();
		StrBuilder.Clear();
		StrBuilder.Append(curString.TrimEnd(c));
	}

	public bool IsNullOrEmpty()
	{
		if ((StrBuilder != null) && (!String.IsNullOrEmpty(StrBuilder.ToString())))
			return false;
		return true;
	}

	public void Clear()
	{
		StrBuilder?.Clear();
	}
}
