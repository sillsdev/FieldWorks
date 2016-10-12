// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.CoreImpl
{
	/// <summary>
	/// This class represents a run of text in a <see cref="TsString"/>.
	/// </summary>
	internal struct TsRun : IEquatable<TsRun>
	{
		private static readonly TsRun EmptyRunInternal = new TsRun(0, TsTextProps.EmptyProps);
		public static TsRun EmptyRun
		{
			get { return EmptyRunInternal; }
		}

		private readonly int m_ichLim;
		private readonly TsTextProps m_textProps;

		public TsRun(int ichLim, TsTextProps textProps)
		{
			m_ichLim = ichLim;
			m_textProps = textProps;
		}

		public int IchLim
		{
			get { return m_ichLim; }
		}

		public TsTextProps TextProps
		{
			get { return m_textProps; }
		}

		public bool Equals(TsRun other)
		{
			return m_ichLim == other.m_ichLim && m_textProps.Equals(other.m_textProps);
		}

		public override bool Equals(object obj)
		{
			return obj is TsRun && Equals((TsRun) obj);
		}

		public override int GetHashCode()
		{
			int code = 23;
			code = code * 31 + m_ichLim.GetHashCode();
			code = code * 31 + m_textProps.GetHashCode();
			return code;
		}
	}
}
