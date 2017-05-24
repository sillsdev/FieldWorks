using System;
using SIL.LCModel;

namespace SIL.FieldWorks.WordWorks.Parser
{
	internal class InvalidAffixProcessException : Exception
	{
		private readonly IMoAffixProcess m_affixProcess;
		private readonly bool m_invalidLhs;

		public InvalidAffixProcessException(IMoAffixProcess affixProcess, bool invalidLhs)
		{
			m_affixProcess = affixProcess;
			m_invalidLhs = invalidLhs;
		}

		public IMoAffixProcess AffixProcess
		{
			get { return m_affixProcess; }
		}

		public bool IsInvalidLhs
		{
			get { return m_invalidLhs; }
		}
	}
}
