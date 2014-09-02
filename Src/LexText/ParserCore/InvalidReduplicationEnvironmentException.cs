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
