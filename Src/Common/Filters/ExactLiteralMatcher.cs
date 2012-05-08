using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// This class takes a string and a writing system and matches strings that have exactly the
	/// same characters all in exactly that writing system.
	/// </summary>
	public class ExactLiteralMatcher : IMatcher
	{
		private string m_target;
		int m_ws;

		/// <summary>
		/// Make one.
		/// </summary>
		public ExactLiteralMatcher(string target, int ws)
		{
			m_target = target;
			m_ws = ws;
		}
		#region IMatcher Members

		public bool Accept(ITsString tssKey)
		{
			// Fail fast if text doesn't match exactly.
			if (!MatchText(tssKey.Length == 0 ? "" : tssKey.Text))
				return false;
			// Writing system must also match.
			int crun = tssKey.RunCount;
			for (int irun = 0; irun < crun; irun++)
			{
				int nVar;
				if (tssKey.get_Properties(irun).GetIntPropValues((int)FwTextPropType.ktptWs, out nVar) != m_ws)
					return false;
			}
			return true;
		}

		internal virtual bool MatchText(string p)
		{
			return p == m_target;
		}

		public bool Matches(SIL.FieldWorks.Common.COMInterfaces.ITsString arg)
		{
			return Accept(arg);
		}

		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		public bool SameMatcher(IMatcher other)
		{
			// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			if (other.GetType() != this.GetType())
				return false;
			ExactLiteralMatcher other1 = other as ExactLiteralMatcher;
			return m_target == other1.m_target && m_ws == other1.m_ws;
		}

		public bool IsValid()
		{
			return true;
		}

		public string ErrorMessage()
		{
			return "";
		}

		public bool CanMakeValid()
		{
			return true;
		}

		public SIL.FieldWorks.Common.COMInterfaces.ITsString MakeValid()
		{
			throw new NotImplementedException();
		}

		public SIL.FieldWorks.Common.COMInterfaces.ITsString Label
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		private ILgWritingSystemFactory m_wsf = null;
		public SIL.FieldWorks.Common.COMInterfaces.ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				return m_wsf;
			}
			set
			{
				m_wsf = value;
			}
		}

		/// <summary>
		/// This class explicitly looks for a particular ws.
		/// </summary>
		public int WritingSystem
		{
			get { return m_ws; }
		}

		#endregion
	}

	/// <summary>
	/// Like the base class, but match ignores case.
	/// </summary>
	public class ExactCaseInsensitiveLiteralMatcher : ExactLiteralMatcher
	{
		public ExactCaseInsensitiveLiteralMatcher(string target, int ws)
			: base(target.ToLower(), ws)
		{
		}

		internal override bool MatchText(string p)
		{
			return base.MatchText(p.ToLower());
		}
	}
}
