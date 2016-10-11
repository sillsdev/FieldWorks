// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.CoreImpl
{
	/// <summary>
	/// This class represents an int property value.
	/// </summary>
	internal class TsIntPropValue : IEquatable<TsIntPropValue>
	{
		private readonly int m_variation;
		private readonly int m_value;

		public TsIntPropValue(int variation, int value)
		{
			m_variation = variation;
			m_value = value;
		}

		public int Variation
		{
			get { return m_variation; }
		}

		public int Value
		{
			get { return m_value; }
		}

		public bool Equals(TsIntPropValue other)
		{
			return other != null && m_variation == other.m_variation && m_value == other.m_value;
		}

		public override bool Equals(object obj)
		{
			var other = obj as TsIntPropValue;
			return other != null && Equals(other);
		}

		public override int GetHashCode()
		{
			int code = 23;
			code = code * 31 + m_variation.GetHashCode();
			code = code * 31 + m_value.GetHashCode();
			return code;
		}
	}
}
