// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.DisambiguateInFLExDB;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIL.DisambiguateInFLExDBTests
{
	[TestFixture]
	class PcPatrInvokerTests : DisambiguateTests
	{
		string AnaString { get; set; }
		string AndString { get; set; }
		string TakeString { get; set; }

		/// <summary>
		/// Test extracting of lexicon.
		/// </summary>
		[Test, Ignore("Ignoring this test for timing purposes")]
		public void PcPatrInvokerTest()
		{
			// Check for the existence of the PcPatr executable
			Assert.IsTrue(File.Exists(Path.Combine(FwDirectoryFinder.ExeOrDllDirectory, "pcpatr64.exe")));
			// Check for the existence of the PcPatr batch and take files
			Assert.IsTrue(File.Exists(Path.Combine(Path.GetTempPath(), "PcPatrFLEx.bat")));
			Assert.IsTrue(File.Exists(Path.Combine(Path.GetTempPath(), "PcPatrFLEx.tak")));
			string grammarFile = "Invoker.grm";
			File.Copy(Path.Combine(TestDataDir, grammarFile), Path.Combine(Path.GetTempPath(), grammarFile), true);
			string anaFile = Path.Combine(TestDataDir, "Invoker.ana");
			File.Copy(anaFile, Path.Combine(Path.GetTempPath(), "Invoker.ana"), true);
			string andFile = Path.Combine(TestDataDir, "InvokerB4.and");
			using (var streamReader = new StreamReader(andFile, Encoding.UTF8))
			{
				AndString = streamReader.ReadToEnd().Replace("\r", "");
			}
			var invoker = new PCPatrInvoker(grammarFile, anaFile, "Off");
			invoker.Invoke();
			Assert.AreEqual(true, invoker.InvocationSucceeded);
			string andResult = "";
			using (var streamReader = new StreamReader(invoker.AndFile, Encoding.UTF8))
			{
				andResult = streamReader.ReadToEnd().Replace("\r", "");
			}
			// The \id line has the location of the Invoker.grm file which will vary by machine.
			// So we just check the firset 23 characters (which are always the same)
			// and what starts at "Invoker.grm".
			int iIDBeginning = 23;
			int iExpected = AndString.IndexOf("Invoker.grm");
			int iResult = andResult.IndexOf("Invoker.grm");
			Assert.AreEqual(
				AndString.Substring(0, iIDBeginning),
				andResult.Substring(0, iIDBeginning)
			);
			Assert.AreEqual(AndString.Substring(iExpected), andResult.Substring(iResult));
			checkRootGlossState(invoker, null);
			checkRootGlossState(invoker, "off");
			checkRootGlossState(invoker, "leftheaded");
			checkRootGlossState(invoker, "rightheaded");
			checkRootGlossState(invoker, "all");
			checkRootGlossStateValue(invoker, null, null);
			checkRootGlossStateValue(invoker, "Off", "off");
			checkRootGlossStateValue(invoker, "Leftheaded", "leftheaded");
			checkRootGlossStateValue(invoker, "Rightheaded", "rightheaded");
			checkRootGlossStateValue(invoker, "All", "all");
			checkRootGlossStateValue(invoker, "Of course", "off");
			checkRootGlossStateValue(invoker, "Luis", "leftheaded");
			checkRootGlossStateValue(invoker, "Rival", "rightheaded");
			checkRootGlossStateValue(invoker, "Alone", "all");
		}

		private void checkRootGlossState(PCPatrInvoker invoker, string state)
		{
			string takeFile = Path.Combine(Path.GetTempPath(), "PcPatrFLEx.tak");

			invoker.RootGlossState = state;
			invoker.Invoke();
			using (var streamReader = new StreamReader(takeFile, Encoding.UTF8))
			{
				TakeString = streamReader.ReadToEnd().Replace("\r", "");
			}
			if (String.IsNullOrEmpty(state))
			{
				Assert.IsFalse(TakeString.Contains("set rootgloss "));
			}
			else
			{
				Assert.IsTrue(TakeString.Contains("set rootgloss " + state + "\n"));
			}
		}

		private void checkRootGlossStateValue(
			PCPatrInvoker invoker,
			string state,
			string expectedValue
		)
		{
			string takeFile = Path.Combine(Path.GetTempPath(), "PcPatrFLEx.tak");

			invoker.RootGlossState = state;
			invoker.Invoke();
			// Give it time to completely finish or the output file won't be available
			Thread.Sleep(500);
			using (var streamReader = new StreamReader(takeFile, Encoding.UTF8))
			{
				TakeString = streamReader.ReadToEnd().Replace("\r", "");
			}
			if (String.IsNullOrEmpty(state))
			{
				Assert.IsFalse(TakeString.Contains("set rootgloss "));
			}
			else
			{
				Assert.IsTrue(TakeString.Contains("set rootgloss " + expectedValue + "\n"));
			}
		}

		/// <summary>
		/// Test extracting of lexicon.
		/// </summary>
		[Test, Ignore("Ignoring this test for timing purposes")]
		public void PcPatrInvokerFailureTest()
		{
			string grammarFile = "GrammarFail.grm";
			File.Copy(Path.Combine(TestDataDir, grammarFile), Path.Combine(Path.GetTempPath(), grammarFile), true);
			string anaFile = Path.Combine(TestDataDir, "InvokerFail.ana");
			var invoker = new PCPatrInvoker(grammarFile, anaFile, "Off");
			invoker.Invoke();
			Assert.AreEqual(false, invoker.InvocationSucceeded);
		}
	}
}
