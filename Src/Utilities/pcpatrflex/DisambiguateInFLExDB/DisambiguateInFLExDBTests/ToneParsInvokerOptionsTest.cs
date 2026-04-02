// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.DisambiguateInFLExDB;
using SIL.LCModel;
using SIL.ToneParsFLEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIL.DisambiguateInFLExDBTests
{
	[TestFixture]
	class ToneParsInvokerOptionsTests //: MemoryOnlyBackendProviderTestBase
	{
		[SetUp]
		public void FixtureSetup()
		{
			ToneParsInvokerOptions.Instance.ResetAllOptions();
		}

		/// <summary></summary>
		[TearDown]
		public void FixtureTeardown()
		{
			// nothing to do
		}

		/// <summary>
		/// Test setting of trace and verify options.
		/// </summary>
		[Test, Ignore("Ignoring this test for timing purposes")]
		public void TraceOptionsTest()
		{
			Assert.AreEqual("", ToneParsInvokerOptions.Instance.GetOptionsString());
			ToneParsInvokerOptions.Instance.VerifyInformation = true;
			Assert.AreEqual("-v ", ToneParsInvokerOptions.Instance.GetOptionsString());
			ToneParsInvokerOptions.Instance.DoTracing = true;
			Assert.AreEqual("-v -T -D 512 ", ToneParsInvokerOptions.Instance.GetOptionsString());
			ToneParsInvokerOptions.Instance.VerifyInformation = false;
			Assert.AreEqual("-T -D 512 ", ToneParsInvokerOptions.Instance.GetOptionsString());
			ToneParsInvokerOptions.Instance.SegmentParsingTrace = true;
			Assert.AreEqual("-T -D 513 ", ToneParsInvokerOptions.Instance.GetOptionsString());
			ToneParsInvokerOptions.Instance.MoraParsingTrace = true;
			Assert.AreEqual("-T -D 515 ", ToneParsInvokerOptions.Instance.GetOptionsString());
			ToneParsInvokerOptions.Instance.SyllableParsingTrace = true;
			Assert.AreEqual("-T -D 519 ", ToneParsInvokerOptions.Instance.GetOptionsString());
			ToneParsInvokerOptions.Instance.SegmentParsingTrace = false;
			ToneParsInvokerOptions.Instance.MoraParsingTrace = false;
			ToneParsInvokerOptions.Instance.SyllableParsingTrace = false;
			ToneParsInvokerOptions.Instance.MorphemeLinkingTrace = true;
			Assert.AreEqual("-T -D 768 ", ToneParsInvokerOptions.Instance.GetOptionsString());
			ToneParsInvokerOptions.Instance.MorphemeToneAssignmentTrace = true;
			Assert.AreEqual("-T -D 4864 ", ToneParsInvokerOptions.Instance.GetOptionsString());
			ToneParsInvokerOptions.Instance.MorphemeLinkingTrace = false;
			ToneParsInvokerOptions.Instance.MorphemeToneAssignmentTrace = false;
			ToneParsInvokerOptions.Instance.DomainAssignmentTrace = true;
			Assert.AreEqual("-T -D 1536 ", ToneParsInvokerOptions.Instance.GetOptionsString());
			ToneParsInvokerOptions.Instance.TBUAssignmentTrace = true;
			Assert.AreEqual("-T -D 1664 ", ToneParsInvokerOptions.Instance.GetOptionsString());
			ToneParsInvokerOptions.Instance.DomainAssignmentTrace = false;
			ToneParsInvokerOptions.Instance.TBUAssignmentTrace = false;
			ToneParsInvokerOptions.Instance.TierAssignmentTrace = true;
			Assert.AreEqual("-T -D 2560 ", ToneParsInvokerOptions.Instance.GetOptionsString());
			ToneParsInvokerOptions.Instance.SegmentParsingTrace = true;
			ToneParsInvokerOptions.Instance.MorphemeLinkingTrace = true;
			ToneParsInvokerOptions.Instance.MoraParsingTrace = true;
			ToneParsInvokerOptions.Instance.SyllableParsingTrace = true;
			ToneParsInvokerOptions.Instance.TBUAssignmentTrace = true;
			ToneParsInvokerOptions.Instance.MorphemeToneAssignmentTrace = true;
			ToneParsInvokerOptions.Instance.DomainAssignmentTrace = true;
			ToneParsInvokerOptions.Instance.TierAssignmentTrace = true;
			ToneParsInvokerOptions.Instance.RuleTrace = true;
			Assert.AreEqual("-T -D 8071 ", ToneParsInvokerOptions.Instance.GetOptionsString());
			ToneParsInvokerOptions.Instance.DoTracing = false;
			//ToneParsInvokerOptions.Instance.RuleTrace = false;
			Assert.AreEqual("", ToneParsInvokerOptions.Instance.GetOptionsString());
		}
	}
}
