// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TestState.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Xml;
using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// TestState keeps track of state.
	/// This is done via configuration data and by
	/// keeping a hash table of instructions that have identifiers.
	/// AddNamedInstruction is used to add named instructions to the hash.
	/// When the data maintained or created by one of these instructions is
	/// needed, the instruction is retrieved via the Instruction(name) method.
	/// </summary>
	public class TestState
	{
		static private TestState m_ts = null; // the singleton TestState

		Hashtable   m_htNamedInstructions = new Hashtable();
		XmlDocument m_doc;
		XmlNode     m_app = null;
		string      m_appSymbol = "NONE";
		string      m_appPath = null;
		string      m_appExe = null;
		string      m_scriptPath = null;
		string      m_modelPath = null;
		string      m_modelName = null;
		string      m_script = null;
		int         m_insCount = 0;

		static public TestState getOnly() {return m_ts;}

		static public TestState getOnly(string appSymbol)
		{
			m_ts = new TestState(appSymbol);
			return m_ts;
		}

		/// <summary>
		/// Constructs an empty TestState and reads the
		/// configuration file GtdConfig.xml.
		/// This should be called for each on-application context,
		/// to create a local connection to the app data in the config file,
		/// but the main one is created via getOnly(appSymbol).
		/// </summary>
		/// <param name="appSymbol">The application's abbreviated id used in the GtdConfig.xml file.</param>
		public TestState(string appSymbol)
		{
			m_appSymbol = appSymbol;
			m_doc = new XmlDocument();
			string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).Substring(6);
			try
			{
				m_doc.Load(path+@"\GtdConfig.xml");
			}
			catch (XmlException xe)
			{
				Assert.Fail("GtdConfig.xml file not located with GuiTestDriver.dll on path "+path+". "+xe.Message);
			}
			XmlNode config = m_doc["config"];
			Assert.IsNotNull(config,"Missing document element 'config' in GuiTestDriver GtdConfig.xml.");
			string xpath = "app[@id='"+appSymbol+"']";
			XmlNodeList apps = config.SelectNodes(xpath);
			Assert.IsNotNull(apps,"GuiTestDriver GtdConfig.xml has no node for app "+appSymbol+".");
			m_app = apps[0];
			Assert.IsNotNull(m_app,"GuiTestDriver GtdConfig.xml has no node for app "+appSymbol+".");

			XmlAttribute aPath = m_app.Attributes["path"];
			if (aPath != null)
				m_appPath = aPath.Value;

			XmlAttribute aExe = m_app.Attributes["exe"];
			if (aExe != null)
				m_appExe = aExe.Value;
		}

		public void PublishVars()
		{
			Var sp = new Var();
			// sp.finishCreation() not needed
			sp.Id = "ScriptPath";
			sp.Set = getScriptPath();
			sp.Execute(); // sets and interprets the value and adds to the hash
			Var ep = new Var();
			// ep.finishCreation() not needed
			ep.Id = "AppPath";
			ep.Set = getAppPath();
			ep.Execute(); // sets and interprets the value and adds to the hash
			Var mp = new Var();
			// mp.finishCreation() not needed
			mp.Id = "ModelPath";
			mp.Set = getModelPath();
			mp.Execute(); // sets and interprets the value and adds to the hash
		}

		public int IncInstructionCount
		{
			/// <summary>
			/// Increments the instruction count,
			/// returning the previous count
			/// </summary>
			get { return m_insCount++; }
		}

		/// <summary>
		/// Property script - the script currently running.
		/// </summary>
		public string Script
		{
			get {return m_script;}
			set {m_script = value;}
		}

		/// <summary>
		/// Retrieves a named instruction by name from the pool.
		/// </summary>
		/// <param name="name">The id of the instruction.</param>
		/// <returns>The named instruction.</returns>
		public Instruction Instruction(string name)
		{
			return (Instruction) m_htNamedInstructions[name];
		}

		/// <summary>
		/// Add a named instruction to the pool to be retrieved by calling "Instruction".
		/// </summary>
		/// <param name="name">The id of the instruction.</param>
		/// <param name="ins">The instruction called by name.</param>
		public void AddNamedInstruction(string name, Instruction ins)
		{
			m_htNamedInstructions.Add(name, ins);
		}

		/// <summary>
		/// Removes an Instruction from the hash table.
		/// </summary>
		/// <param name="name">of the Instruction to remove.</param>
		public void RemoveInstruction(string name)
		{
			m_htNamedInstructions.Remove(name);
		}

		/// <summary>
		/// Gets the path for the application being tested from the configuration file.
		/// </summary>
		/// <returns>null or the path to the application being tested.</returns>
		public string getAppPath() {return m_appPath;}

		/// <summary>
		/// Gets the executable name for the application being tested from the configuration file.
		/// </summary>
		/// <returns>null or the executable name of the application being tested.</returns>
		public string getAppExe() {return m_appExe;}

		/// <summary>
		/// Gets the path for the application scripts from the configuration file.
		/// Adds the variable ScriptPath to the hash table.
		/// </summary>
		/// <returns>The path to the scripts for the application being tested.</returns>
		public string getScriptPath()
		{
			if (m_scriptPath != null) return m_scriptPath;
			string xpath = "scripts/path";
			XmlNodeList paths = m_app.SelectNodes(xpath);
			Assert.IsNotNull(paths,"GuiTestDriver GtdConfig.xml has no path node for app "+m_appSymbol+" scripts.");
			XmlNode pathNode = paths[0];
			Assert.IsNotNull(pathNode,"GuiTestDriver GtdConfig.xml has no path for app "+m_appSymbol+" scripts.");
			m_scriptPath = pathNode.InnerText;
			return m_scriptPath;
		}

		/// <summary>
		/// Gets the path for the application model from the configuration file.
		/// </summary>
		/// <returns>The path to the model for the application being tested.</returns>
		public string getModelPath()
		{
			if (m_modelPath != null) return m_modelPath;
			string xpath = "model/path";
			XmlNodeList paths = m_app.SelectNodes(xpath);
			Assert.IsNotNull(paths,"GuiTestDriver GtdConfig.xml has no path node for app "+m_appSymbol+" model.");
			XmlNode pathNode = paths[0];
			Assert.IsNotNull(pathNode,"GuiTestDriver GtdConfig.xml has no path for app "+m_appSymbol+" model.");
			m_modelPath = pathNode.InnerText;
			return m_modelPath;
		}

		/// <summary>
		/// Gets the model name for the application being tested from the configuration file.
		/// </summary>
		/// <returns>null or the model name of the application being tested.</returns>
		public string getModelName()
		{
			if (m_modelName != null) return m_modelName;
			string xpath = "model";
			XmlNodeList paths = m_app.SelectNodes(xpath);
			Assert.IsNotNull(paths,"GuiTestDriver GtdConfig.xml has no model node for app "+m_appSymbol+" model.");
			XmlNode model = paths[0];
			Assert.IsNotNull(model,"GuiTestDriver GtdConfig.xml has no model for app "+m_appSymbol+" model.");
			XmlAttribute mainAttr = model.Attributes["main"];
			Assert.IsNotNull(mainAttr,"GuiTestDriver GtdConfig.xml has no model/@main for app "+m_appSymbol+".");
			m_modelName = mainAttr.Value;
			return m_modelName;
		}

	}
}
