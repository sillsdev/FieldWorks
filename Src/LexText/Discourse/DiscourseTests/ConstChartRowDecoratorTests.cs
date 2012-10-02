// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ConstChartRowDecoratorTests.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.Discourse
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the logic of the Constituent Chart Row Decorator's FlushDecorator() routine
	/// that ensures RtL script generates charts correctly.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ConstChartRowDecoratorTests : InMemoryDiscourseTestBase
	{
		private VwEnvSpy m_spy;
		private char m_PDF = '\x202C';

		[SetUp]
		public void CreateTestDecorator()
		{
			//var sPopFormatting = Cache.TsStrFactory.MakeString(Convert.ToString(m_PDF), Cache.DefaultAnalWs);
			m_spy = new VwEnvSpy();
		}

		#region Verification and Setup Methods


		#endregion

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the base case (no calls to the decorator).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Test_NoCalls()
		{
			// Setup
			m_spy.IsRtL = true;

			// SUT
			Assert.AreEqual(0, m_spy.TotalCalls, "Shouldn't be any calls before flushing.");
			m_spy.FlushDecorator();

			// Verification
			Assert.AreEqual(0, m_spy.TotalCalls, "Shouldn't be any calls.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the base case (no calls to the decorator).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Test_FiveCallsLeftToRight()
		{
			// Setup
			var tsStr = Cache.TsStrFactory.MakeString("random", Cache.DefaultAnalWs);
			m_spy.IsRtL = false;
			m_spy.OpenTableCell(1, 1);
			m_spy.OpenParagraph();
			m_spy.AddString(tsStr);
			m_spy.CloseParagraph();
			m_spy.CloseTableCell();
			const int count = 0; // LtR calls should not pass through the Decorator.

			// SUT
			Assert.AreEqual(count, m_spy.TotalCalls, String.Format("Should be {0} calls before flushing.", count));
			m_spy.FlushDecorator();

			// Verification
			Assert.AreEqual(0, m_spy.TotalCalls, "Shouldn't be any calls.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests a simple case opening a table cell and adding a string.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Test_OpenCellAddString()
		{
			// Setup
			var tsStr = Cache.TsStrFactory.MakeString("random", Cache.DefaultAnalWs);
			m_spy.IsRtL = true;
			m_spy.OpenTableCell(1, 1);
			m_spy.OpenParagraph();
			m_spy.AddString(tsStr);
			m_spy.CloseParagraph();
			m_spy.CloseTableCell();
			const int expectedCount = 7; // OpenParagraph() makes 3 calls

			// SUT
			Assert.AreEqual(expectedCount, m_spy.TotalCalls,
				String.Format("Should be {0} calls before flushing.", expectedCount));
			m_spy.FlushDecorator();

			// Verification
			Assert.AreEqual(0, m_spy.TotalCalls, "Shouldn't be any calls.");
			Assert.AreEqual(expectedCount, m_spy.TotalCallsByFlushDecorator,
				String.Format("Should be {0} calls during flush.", expectedCount));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests adding a row number cell.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Test_MakeRowLabelCell()
		{
			// Setup
			m_spy.IsRtL = true;
			m_spy.set_IntProperty((int)FwTextPropType.ktptEditable,
								(int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			m_spy.set_IntProperty((int)FwTextPropType.ktptBorderTrailing,
								(int)FwTextPropVar.ktpvMilliPoint, 500);
			m_spy.set_IntProperty((int)FwTextPropType.ktptBorderColor,
								(int)FwTextPropVar.ktpvDefault,
								(int)ColorUtil.ConvertColorToBGR(Color.Black));

			m_spy.OpenTableCell(1, 1);
			m_spy.AddStringProp(ConstChartRowTags.kflidLabel, null);
			m_spy.CloseTableCell();
			const int expectedCount = 6;

			// SUT
			Assert.AreEqual(expectedCount, m_spy.TotalCalls,
				String.Format("Should be {0} calls before flushing.", expectedCount));
			m_spy.FlushDecorator();

			// Verification
			Assert.AreEqual(0, m_spy.TotalCalls, "Shouldn't be any calls.");
			Assert.AreEqual(expectedCount, m_spy.TotalCallsByFlushDecorator,
				String.Format("Should be {0} calls during flush.", expectedCount));
			var tpt = (int)m_spy.CalledMethodsAfterFlushDecorator[m_spy.m_cCallsBeforeFlush + 1].ParamArray[0];
			Assert.AreEqual((int)FwTextPropType.ktptBorderLeading, tpt,
				"Decorator should have changed this TextPropType to Leading from Trailing.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests adding a Notes cell.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Test_MakeNotesCell()
		{
			// Setup
			m_spy.IsRtL = true;
			m_spy.set_IntProperty((int)FwTextPropType.ktptEditable,
								(int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			m_spy.set_IntProperty((int)FwTextPropType.ktptBorderTrailing,
								(int)FwTextPropVar.ktpvMilliPoint, 500);
			m_spy.set_IntProperty((int)FwTextPropType.ktptBorderColor,
								(int)FwTextPropVar.ktpvDefault,
								(int)ColorUtil.ConvertColorToBGR(Color.Black));

			m_spy.OpenTableCell(1, 1);
			m_spy.AddStringProp(ConstChartRowTags.kflidLabel, null);
			m_spy.CloseTableCell();
			const int expectedCount = 6;

			// SUT
			Assert.AreEqual(expectedCount, m_spy.TotalCalls,
				String.Format("Should be {0} calls before flushing.", expectedCount));
			m_spy.FlushDecorator();

			// Verification
			Assert.AreEqual(0, m_spy.TotalCalls, "Shouldn't be any calls.");
			Assert.AreEqual(expectedCount, m_spy.TotalCallsByFlushDecorator,
				String.Format("Should be {0} calls during flush.", expectedCount));
			var tpt = (int)m_spy.CalledMethodsAfterFlushDecorator[m_spy.m_cCallsBeforeFlush + 1].ParamArray[0];
			Assert.AreEqual((int)FwTextPropType.ktptBorderLeading, tpt,
				"Decorator should have changed this TextPropType to Leading from Trailing.");
		}
	}

	class VwEnvSpy : ChartRowEnvDecorator
	{
		internal int m_cCallsBeforeFlush;
		public int TotalCalls { get { return m_numOfCalls; } }
		internal int TotalCallsByFlushDecorator { get; set; }
		internal List<StoredMethod> CalledMethodsAfterFlushDecorator { get; set; }

		public VwEnvSpy()
		{
			m_vwEnv = this;
		}

		protected override void InternalFlush()
		{
			m_cCallsBeforeFlush = TotalCalls;
			base.InternalFlush();
		}

		protected override void ResetVariables()
		{
			// Grab call data before the base method resets the Decorator's variables.
			TotalCallsByFlushDecorator = TotalCalls - m_cCallsBeforeFlush;
			CalledMethodsAfterFlushDecorator = m_calledMethods;
			base.ResetVariables();
		}

		#region IVwEnv Members

		public override void AddObjProp(int tag, IVwViewConstructor _vwvc, int frag)
		{
			if (!IsRtL)
				return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
			base.AddObjProp(tag, _vwvc, frag);
		}

		public override void AddObjVec(int tag, IVwViewConstructor _vwvc, int frag)
		{
			if (!IsRtL)
				return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
			base.AddObjVec(tag, _vwvc, frag);
		}

		public override void AddObjVecItems(int tag, IVwViewConstructor _vwvc, int frag)
		{
			if (!IsRtL)
				return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
			base.AddObjVecItems(tag, _vwvc, frag);
		}

		public override void AddObj(int hvo, IVwViewConstructor _vwvc, int frag)
		{
			if (!IsRtL)
				return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
			base.AddObj(hvo, _vwvc, frag);
		}

		public override void AddProp(int tag, IVwViewConstructor _vwvc, int frag)
		{
			if (!IsRtL)
				return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
			base.AddProp(tag, _vwvc, frag);
		}

		public override void AddString(ITsString _ss)
		{
			if (!IsRtL)
				return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
			base.AddString(_ss);
		}

		public override void AddStringProp(int tag, IVwViewConstructor _vwvc)
		{
			if (!IsRtL)
				return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
			base.AddStringProp(tag, _vwvc);
		}

		public override void CloseInnerPile()
		{
			if (!IsRtL)
				return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
			base.CloseInnerPile();
		}

		public override void CloseParagraph()
		{
			if (!IsRtL)
				return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
			base.CloseParagraph();
		}

		public override void CloseSpan()
		{
			if (!IsRtL)
				return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
			base.CloseSpan();
		}

		public override void CloseTableCell()
		{
			if (!IsRtL)
				return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
			base.CloseTableCell();
		}

		public override void NoteDependency(int[] _rghvo, int[] _rgtag, int chvo)
		{
			if (!IsRtL)
				return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
			base.NoteDependency(_rghvo, _rgtag, chvo);
		}

		public override void OpenInnerPile()
		{
			if (!IsRtL)
				return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
			base.OpenInnerPile();
		}

		public override void OpenParagraph()
		{
			if (!IsRtL)
				return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
			base.OpenParagraph();
		}

		public override void OpenSpan()
		{
			if (!IsRtL)
				return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
			base.OpenSpan();
		}

		public override void OpenTableCell(int nRowSpan, int nColSpan)
		{
			if (!IsRtL)
				return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
			base.OpenTableCell(nRowSpan, nColSpan);
		}

		public override void set_IntProperty(int tpt, int tpv, int nValue)
		{
			if (!IsRtL)
				return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
			base.set_IntProperty(tpt, tpv, nValue);
		}

		#endregion
	}
}
