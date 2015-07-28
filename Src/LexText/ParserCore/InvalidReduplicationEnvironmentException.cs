// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public class InvalidReduplicationEnvironmentException : Exception
	{
		private readonly string m_morpheme;

		public InvalidReduplicationEnvironmentException(string message, string morpheme)
			: base(message)
		{
			m_morpheme = morpheme;
		}

		public string Morpheme
		{
			get { return m_morpheme; }
		}
	}
}
