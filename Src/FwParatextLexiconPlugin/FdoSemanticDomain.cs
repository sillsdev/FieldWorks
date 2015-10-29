// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using Paratext.LexicalContracts;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal class FdoSemanticDomain : LexiconSemanticDomain
	{
		private readonly string m_name;

		/// <summary>
		/// Creates a new LexiconSemanticDomain by wrapping the specified data
		/// </summary>
		public FdoSemanticDomain(string name)
		{
			Debug.Assert(name.IsNormalized(), "We expect all strings to be normalized composed");
			m_name = name;
		}

		#region Implementation of LexiconSemanticDomain
		/// <summary>
		/// The name of the semantic domain
		/// </summary>
		public string Name
		{
			get { return m_name; }
		}
		#endregion

		public override bool Equals(object obj)
		{
			var other = obj as FdoSemanticDomain;
			return other != null && m_name == other.m_name;
		}

		public override int GetHashCode()
		{
			return m_name.GetHashCode();
		}
	}
}
