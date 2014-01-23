// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: OnDesktop.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Xml;
using NUnit.Framework;
using System.Threading;

namespace GuiTestDriver
{
	/// <summary>
	/// RunTest sets up and runs a test
	/// </summary>
	public class RunTest
	{
		string m_appSymbol = "NONE";
		private System.Collections.ArrayList m_vars = new System.Collections.ArrayList();
		private bool varsDefined = false;

		/// <summary>
		/// This class encapsulates running tests from NUnit
		/// so a helper method is not needed in each NUnit TestFixture.
		/// Use: This class is instantiated in the [TestFixture] constructor.
		/// A call to {RunTest}.fromFile("...") defines each [Test].
		/// One call per [Test] is typical, however, test scripts may be
		/// factored to run as test components.
		/// </summary>
		/// <param name="appSymbol">The application's abbreviated id used in the GtdConfig.xml file.</param>
		public RunTest(string appSymbol)
		{
			m_appSymbol = appSymbol;
			//TestState ts = TestState.getOnly(m_appSymbol);
			//ts.PublishVars();
			varsDefined = false;
		}

		/// <summary>
		/// Gets the name of a test script and runs it.
		/// There must be a corresponding XML script file in the
		/// location specified in the GtdConfig.xml file.
		/// </summary>
		/// <param name="script">The name of the test script (.xml not needed).</param>
		public void fromFile(string script)
		{
			if(!(script.ToLower()).EndsWith(@".xml"))
				script += ".xml";
			TestState ts = null;
			if (varsDefined) // true only if AddVariable() was called.
			{
				ts = TestState.getOnly();
				// Re-open the log using the script name - lose what's there.
				Logger.getOnly().close(Logger.Disposition.Hung);
				// The next call to Logger.getOnly() will create one with using the script name.
			}
			else ts = TestState.getOnly(m_appSymbol); // Allocating ts here insures deallocation
			ts.Script = script; // must come before ts.PublishVars();
			ts.PublishVars();

			// get the script path from the configuration file
			string path = ts.getScriptPath() + @"\" + script;
			XmlElement scriptRoot = XmlFiler.getDocumentElement(path, "accil", false);
			Assert.IsNotNull(scriptRoot, "Missing document element 'accil'.");
			Instructionator.initialize("AxilInstructions.xml");
			OnDesktop dt = new OnDesktop();
			Assert.IsNotNull(dt, "OnDesktop not created for script " + path);
			dt.Element = scriptRoot; // not quite code-behind, but OK
			dt.Number = ts.IncInstructionCount;
			//dt = new XmlInstructionBuilder().Parse(path);

			// now execute any variables if they've been added before executing the desktop
			foreach (Var v in m_vars)
			{
				v.Execute();
			}

			System.GC.Collect(); // forces collection on NUnit process only

			Beep beeep = new Beep();
			beeep.Execute(); // play a beep tone so we know when the test starts

			dt.Execute();
			varsDefined = false; // the next test may not have any
			Logger.getOnly().close(Logger.Disposition.Pass);

			Thread.Sleep(1000);
		}

		/// <summary>
		/// Method to add a variable to a TestState. This is useful for adding vars during
		/// a test routine in C# that is used in the xml script. Long term it will be beter
		/// to have the script interact with the teststate and get values directly.
		/// </summary>
		/// <param name="name">id of the variable</param>
		/// <param name="val">value for the variable</param>
		public void AddVariable(string name, string val)
		{
			TestState ts = null; // local
			if (!varsDefined) ts = TestState.getOnly(m_appSymbol); // sets a global singleton
			// a new Var creates a Logger that needs a TestState to get the script path
			varsDefined = true;
			Var var = new Var();
			var.Id = name;
			var.Set = val;
			m_vars.Add(var);
//			// have to execute the instruction to get it added to the TestState
//			var.Execute();
		}

	}
}
