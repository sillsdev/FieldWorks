// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This class tests whether there is a parameter and if so stops the processing.
	/// </summary>
	internal class TestForParameter : IAttributeVisitor
	{
		public TestForParameter()
		{
		}

		public virtual bool Visit(XAttribute xa)
		{
			HasAttribute |= IsParameter(xa.Value);
			return HasAttribute;
		}

		public bool HasAttribute { get; private set; }

		/// <summary>
		/// This is the definition of a parameter-like value.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		internal static bool IsParameter(string input)
		{
			if (input.Length < 2)
			{
				return false;
			}
			if (input[0] != '$')
			{
				return false;
			}
			return (input.IndexOf('=') >= 0);
		}
	}
}