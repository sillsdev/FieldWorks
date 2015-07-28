// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.SharpViews
{
	public class Triple<T1, T2, T3>
	{
		public T1 First { get; private set; }
		public T2 Second { get; private set; }
		public T3 Third { get; private set; }
		public Triple(T1 first, T2 second, T3 third)
		{
			First = first;
			Second = second;
			Third = third;
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as Triple<T1, T2, T3>);
		}

		public bool Equals(Triple<T1, T2, T3> other)
		{
			if (other == null)
				return false;
			return other.First.Equals(First) && other.Second.Equals(Second) && other.Third.Equals(Third);
		}

		public override int GetHashCode()
		{
			return First.GetHashCode() ^ Second.GetHashCode() ^ Third.GetHashCode();
		}
	}
}
