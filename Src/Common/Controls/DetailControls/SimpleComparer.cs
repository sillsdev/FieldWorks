using System;
using System.Collections;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// This code provides a simple comparison function for keyed objects.
	/// The strings are compared using simple string comparison.
	/// </summary>
	public class SimpleComparer : IComparer
	{
		public SimpleComparer()
		{
			//
			// TODO: Add constructor logic here
			//
		}
		public int Compare(Object x, Object y)
		{
			string sX = ((IKeyedObject)x).Key;
			string sY  = ((IKeyedObject)y).Key;
			return String.Compare(sX, sY, true);
		}
	}
}
