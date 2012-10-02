using System;
using SIL.FieldWorks.SharpViews.Hookups;

namespace SIL.FieldWorks.SharpViews.Builders
{
	public class MlsExpressionFlow : Flow
	{
		private Func<IViewMultiString> m_fetchString;
		private string m_substituteString;
		private int m_substituteWs;
		private int Ws;

		internal MlsExpressionFlow(Func<IViewMultiString> fetchString, int ws)
		{
			Ws = ws;
			m_fetchString = fetchString;
		}
		public override void AddContent(ViewBuilder builder)
		{
			if (m_substituteString != null)
				builder.AddString(m_fetchString, Ws, m_substituteString, m_substituteWs);
			else
				builder.AddString(m_fetchString, Ws);
		}

		/// <summary>
		/// Fluent language construct causing the specified substitute in the specified WS to be displayed when the string
		/// normally displayed by the StringExpression is empty.
		/// </summary>
		public Flow WhenEmpty(string substitute, int ws)
		{
			m_substituteString = substitute;
			m_substituteWs = ws;
			return this;
		}

		/// <summary>
		/// Fluent language construct causing the specified substitute (in the same WS as the main string)
		/// to be displayed when the string normally displayed by the StringExpression is empty.
		/// </summary>
		public Flow WhenEmpty(string substitute)
		{
			return WhenEmpty(substitute, 0);
		}
	}
}
