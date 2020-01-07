// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Drawing;
using LanguageExplorer.Areas.TextsAndWords.Discourse;
using NUnit.Framework;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorerTests.Areas.TextsAndWords.Discourse
{
	/// <summary>
	/// Tests for the logic of the Constituent Chart Row Decorator's FlushDecorator() routine
	/// that ensures RtL script generates charts correctly.
	/// </summary>
	[TestFixture]
	public class ConstChartRowDecoratorTests : InMemoryDiscourseTestBase
	{
		private VwEnvSpy m_spy;
		private char m_PDF = '\x202C';

		#region Overrides of MemoryOnlyBackendProviderRestoredForEachTestTestBase
		public override void TestSetup()
		{
			base.TestSetup();

			m_spy = new VwEnvSpy();
		}
		#endregion

		/// <summary>
		/// Tests the base case (no calls to the decorator).
		/// </summary>
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

		/// <summary>
		/// Tests the base case (no calls to the decorator).
		/// </summary>
		[Test]
		public void Test_FiveCallsLeftToRight()
		{
			// Setup
			var tsStr = TsStringUtils.MakeString("random", Cache.DefaultAnalWs);
			m_spy.IsRtL = false;
			m_spy.OpenTableCell(1, 1);
			m_spy.OpenParagraph();
			m_spy.AddString(tsStr);
			m_spy.CloseParagraph();
			m_spy.CloseTableCell();
			const int count = 0; // LtR calls should not pass through the Decorator.

			// SUT
			Assert.AreEqual(count, m_spy.TotalCalls, $"Should be {count} calls before flushing.");
			m_spy.FlushDecorator();

			// Verification
			Assert.AreEqual(0, m_spy.TotalCalls, "Shouldn't be any calls.");
		}

		/// <summary>
		/// Tests a simple case opening a table cell and adding a string.
		/// </summary>
		[Test]
		public void Test_OpenCellAddString()
		{
			// Setup
			var tsStr = TsStringUtils.MakeString("random", Cache.DefaultAnalWs);
			m_spy.IsRtL = true;
			m_spy.OpenTableCell(1, 1);
			m_spy.OpenParagraph();
			m_spy.AddString(tsStr);
			m_spy.CloseParagraph();
			m_spy.CloseTableCell();
			const int expectedCount = 7; // OpenParagraph() makes 3 calls

			// SUT
			Assert.AreEqual(expectedCount, m_spy.TotalCalls, $"Should be {expectedCount} calls before flushing.");
			m_spy.FlushDecorator();

			// Verification
			Assert.AreEqual(0, m_spy.TotalCalls, "Shouldn't be any calls.");
			Assert.AreEqual(expectedCount, m_spy.TotalCallsByFlushDecorator, $"Should be {expectedCount} calls during flush.");
		}

		/// <summary>
		/// Tests adding a row number cell.
		/// </summary>
		[Test]
		public void Test_MakeRowLabelCell()
		{
			// Setup
			m_spy.IsRtL = true;
			m_spy.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			m_spy.set_IntProperty((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, 500);
			m_spy.set_IntProperty((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.Black));

			m_spy.OpenTableCell(1, 1);
			m_spy.AddStringProp(ConstChartRowTags.kflidLabel, null);
			m_spy.CloseTableCell();
			const int expectedCount = 6;

			// SUT
			Assert.AreEqual(expectedCount, m_spy.TotalCalls, $"Should be {expectedCount} calls before flushing.");
			m_spy.FlushDecorator();

			// Verification
			Assert.AreEqual(0, m_spy.TotalCalls, "Shouldn't be any calls.");
			Assert.AreEqual(expectedCount, m_spy.TotalCallsByFlushDecorator, $"Should be {expectedCount} calls during flush.");
			var tpt = (int)m_spy.CalledMethodsAfterFlushDecorator[m_spy.m_cCallsBeforeFlush + 1].ParamArray[0];
			Assert.AreEqual((int)FwTextPropType.ktptBorderLeading, tpt, "Decorator should have changed this TextPropType to Leading from Trailing.");
		}

		/// <summary>
		/// Tests adding a Notes cell.
		/// </summary>
		[Test]
		public void Test_MakeNotesCell()
		{
			// Setup
			m_spy.IsRtL = true;
			m_spy.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			m_spy.set_IntProperty((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, 500);
			m_spy.set_IntProperty((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.Black));

			m_spy.OpenTableCell(1, 1);
			m_spy.AddStringProp(ConstChartRowTags.kflidLabel, null);
			m_spy.CloseTableCell();
			const int expectedCount = 6;

			// SUT
			Assert.AreEqual(expectedCount, m_spy.TotalCalls, $"Should be {expectedCount} calls before flushing.");
			m_spy.FlushDecorator();

			// Verification
			Assert.AreEqual(0, m_spy.TotalCalls, "Shouldn't be any calls.");
			Assert.AreEqual(expectedCount, m_spy.TotalCallsByFlushDecorator, $"Should be {expectedCount} calls during flush.");
			var tpt = (int)m_spy.CalledMethodsAfterFlushDecorator[m_spy.m_cCallsBeforeFlush + 1].ParamArray[0];
			Assert.AreEqual((int)FwTextPropType.ktptBorderLeading, tpt, "Decorator should have changed this TextPropType to Leading from Trailing.");
		}

		private sealed class VwEnvSpy : ChartRowEnvDecorator
		{
			internal int m_cCallsBeforeFlush;
			public int TotalCalls => m_numOfCalls;
			internal int TotalCallsByFlushDecorator { get; private set; }
			internal List<StoredMethod> CalledMethodsAfterFlushDecorator { get; private set; }

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

			public override void AddObjProp(int tag, IVwViewConstructor vwvc, int frag)
			{
				if (!IsRtL)
				{
					return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
				}
				base.AddObjProp(tag, vwvc, frag);
			}

			public override void AddObjVec(int tag, IVwViewConstructor vwvc, int frag)
			{
				if (!IsRtL)
				{
					return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
				}
				base.AddObjVec(tag, vwvc, frag);
			}

			public override void AddObjVecItems(int tag, IVwViewConstructor vwvc, int frag)
			{
				if (!IsRtL)
				{
					return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
				}
				base.AddObjVecItems(tag, vwvc, frag);
			}

			public override void AddObj(int hvo, IVwViewConstructor vwvc, int frag)
			{
				if (!IsRtL)
				{
					return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
				}
				base.AddObj(hvo, vwvc, frag);
			}

			public override void AddProp(int tag, IVwViewConstructor vc, int frag)
			{
				if (!IsRtL)
				{
					return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
				}
				base.AddProp(tag, vc, frag);
			}

			public override void AddString(ITsString ss)
			{
				if (!IsRtL)
				{
					return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
				}
				base.AddString(ss);
			}

			public override void AddStringProp(int tag, IVwViewConstructor vwvc)
			{
				if (!IsRtL)
				{
					return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
				}
				base.AddStringProp(tag, vwvc);
			}

			public override void CloseInnerPile()
			{
				if (!IsRtL)
				{
					return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
				}
				base.CloseInnerPile();
			}

			public override void CloseParagraph()
			{
				if (!IsRtL)
				{
					return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
				}
				base.CloseParagraph();
			}

			public override void CloseSpan()
			{
				if (!IsRtL)
				{
					return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
				}
				base.CloseSpan();
			}

			public override void CloseTableCell()
			{
				if (!IsRtL)
				{
					return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
				}
				base.CloseTableCell();
			}

			public override void NoteDependency(int[] rghvo, int[] rgtag, int chvo)
			{
				if (!IsRtL)
				{
					return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
				}
				base.NoteDependency(rghvo, rgtag, chvo);
			}

			public override void OpenInnerPile()
			{
				if (!IsRtL)
				{
					return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
				}
				base.OpenInnerPile();
			}

			public override void OpenParagraph()
			{
				if (!IsRtL)
				{
					return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
				}
				base.OpenParagraph();
			}

			public override void OpenSpan()
			{
				if (!IsRtL)
				{
					return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
				}
				base.OpenSpan();
			}

			public override void OpenTableCell(int nRowSpan, int nColSpan)
			{
				if (!IsRtL)
				{
					return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
				}
				base.OpenTableCell(nRowSpan, nColSpan);
			}

			public override void set_IntProperty(int tpt, int tpv, int nValue)
			{
				if (!IsRtL)
				{
					return; // Don't record calls for LtR, they don't go through the Decorator we're testing.
				}
				base.set_IntProperty(tpt, tpv, nValue);
			}

			#endregion
		}
	}
}
