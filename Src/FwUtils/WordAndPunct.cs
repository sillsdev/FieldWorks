// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// (optional) key string followed by (optional) punctuation/whitespace string.
	/// </summary>
	public struct WordAndPunct
	{
		public string Word;
		public string Punct;
		public int Offset;

		public override string ToString()
		{
			return $"{Word}/{Punct}/{Offset}";
		}
	}
}