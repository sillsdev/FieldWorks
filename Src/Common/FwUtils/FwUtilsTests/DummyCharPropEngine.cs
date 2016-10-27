// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DummyCharPropEngine.cs
// Responsibility: TE Team

using System;
using SIL.FieldWorks.Common.ViewsInterfaces;
using NMock;
using SIL.FieldWorks.Common.FwKernelInterfaces;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Char prop engine that lets us mock out most things and provide simple default behavior
	/// for others. Note that if you use a method not needed by any other tests, you'll have to
	/// provide an implementation, even if all you need it to do is call the mock.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyCharPropEngine : ILgCharacterPropertyEngine
	{
		private DynamicMock m_cpe;
		/// <summary>the mock instance that handles most things</summary>
		public ILgCharacterPropertyEngine m_mockCPE;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyCharPropEngine"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyCharPropEngine()
		{
			m_cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			m_mockCPE = (ILgCharacterPropertyEngine)m_cpe.MockInstance;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the expected methods were called the correct number of times.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Verify()
		{
			m_cpe.Verify();
		}

		#region ILgCharacterPropertyEngine Members

		#endregion

		/// <summary></summary>
		public bool get_IsWordForming(int ch)
		{
			return Icu.IsLetter(ch);
		}

		/// <summary></summary>
		public void GetLineBreakProps(string _rgchIn, int cchIn, ArrayPtr _rglbOut)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public void GetLineBreakInfo(string _rgchIn, int cchIn, int ichMin, int ichLim, ArrayPtr _rglbsOut, out int _ichBreak)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public void GetLineBreakText(int cchMax, out ushort _rgchOut, out int _cchOut)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public void set_LineBreakText(string _rgchIn, int cchMax)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public void LineBreakBefore(int ichIn, out int _ichOut, out LgLineBreak _lbWeight)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public void LineBreakAfter(int ichIn, out int _ichOut, out LgLineBreak _lbWeight)
		{
			throw new NotImplementedException();
		}
	}
}
