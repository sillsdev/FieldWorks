// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Class1.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using NAnt.Core.Attributes;
using NAnt.Core;
using NUnit.Framework;

namespace SIL.FieldWorks.Build.Tasks
{
	#region Dummy circular reference task
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Circular reference task for testing
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TaskName("dummychkrefs")]
	internal class DummyCircularReferenceTask: CircularReferencesTask
	{
		private string m_References;
		private StringBuilder m_Logs = new StringBuilder();

		public DummyCircularReferenceTask(string references, string buildFileXml)
		{
			m_References = references;
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(buildFileXml);
			Project = new Project(doc, Level.Info, 1);
			Project.Execute(); // this loads targets
		}

		protected override void LoadReferences()
		{
			XmlSerializer serializer = new XmlSerializer(typeof(ReferenceCache));
			try
			{
				TextReader reader = new StringReader(m_References);
				try
				{
					m_Cache = (ReferenceCache)serializer.Deserialize(reader);
				}
				catch(Exception e)
				{
					System.Diagnostics.Debug.WriteLine("Exception: " + e.Message);
				}
				finally
				{
					reader.Close();
				}
			}
			catch
			{
				// file doesn't exist
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Logs a message
		/// </summary>
		/// <param name="messageLevel"></param>
		/// <param name="message"></param>
		/// <param name="args"></param>
		/// ------------------------------------------------------------------------------------
		public override void Log(Level messageLevel, string message, params object[] args)
		{
			if (messageLevel == Level.Info)
			{
				string msg = string.Format(message, args);
				System.Diagnostics.Debug.WriteLine(msg);
				m_Logs.Append(msg);
				m_Logs.Append(Environment.NewLine);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Log message
		/// </summary>
		/// <param name="messageLevel"></param>
		/// <param name="message"></param>
		/// ------------------------------------------------------------------------------------
		public override void Log(Level messageLevel, string message)
		{
			if (messageLevel == Level.Info)
			{
				System.Diagnostics.Debug.WriteLine(message);
				m_Logs.Append(message);
				m_Logs.Append(Environment.NewLine);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the task
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DoIt()
		{
			ExecuteTask();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string with all logged messages
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string LogMessages
		{
			get
			{
				string logMessage = m_Logs.ToString();
				m_Logs.Remove(0, m_Logs.Length);
				return logMessage;
			}
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the circular reference task
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class CircularReferencesTests
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests detecting a simple ciruclar reference: a - b - a
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(BuildException), ExpectedMessage="Circular reference: a.dll <- b.dll")]
		public void SimpleCircRef()
		{
			DummyCircularReferenceTask task = new DummyCircularReferenceTask(@"<?xml version=""1.0"" encoding=""utf-8""?><referenceCache><assembly name=""a.dll""><references name=""b.dll"" /></assembly><assembly name=""b.dll""><references name=""a.dll"" /></assembly></referenceCache>",
				@"<project name=""test"" default=""a""><target name=""a"" depends=""b""/><target name=""b""/></project>");
			task.DoIt();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// No circular reference: a - b - c
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NoCircRef()
		{
			DummyCircularReferenceTask task = new DummyCircularReferenceTask(@"<?xml version=""1.0"" encoding=""utf-8""?><referenceCache><assembly name=""a.dll""><references name=""b.dll"" /></assembly><assembly name=""b.dll""><references name=""c.dll"" /></assembly><assembly name=""c.dll""></assembly></referenceCache>",
				@"<project name=""test"" default=""a""><target name=""a"" depends=""b""/><target name=""b"" depends=""c""/><target name=""c""/></project>");
			task.DoIt();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// More complex circular referene: a - b - c - a
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(BuildException), ExpectedMessage="Circular reference: a.dll <- c.dll <- b.dll")]
		public void ComplexCircRef()
		{
			DummyCircularReferenceTask task = new DummyCircularReferenceTask(@"<?xml version=""1.0"" encoding=""utf-8""?><referenceCache><assembly name=""a.dll""><references name=""b.dll"" /></assembly><assembly name=""b.dll""><references name=""c.dll"" /></assembly><assembly name=""c.dll""><references name=""a.dll"" /></assembly></referenceCache>",
				@"<project name=""test"" default=""a""><target name=""a"" depends=""b""/><target name=""b"" depends=""c""/><target name=""c""/></project>");
			task.DoIt();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// No circular reference with missing assembly
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MissingAssembly()
		{
			DummyCircularReferenceTask task = new DummyCircularReferenceTask(@"<?xml version=""1.0"" encoding=""utf-8""?><referenceCache><assembly name=""a.dll""><references name=""b.dll"" /></assembly></referenceCache>",
				@"<project name=""test"" default=""a""><target name=""a""/></project>");
			task.DoIt();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// More complex circular reference: a - b - c - a and a - c - a
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ComplexCircRefOrderABC()
		{
			DummyCircularReferenceTask task = new DummyCircularReferenceTask(
				@"<referenceCache>" +
					@"<assembly name=""a.dll""><references name=""b.dll"" /><references name=""c.dll"" /></assembly>" +
					@"<assembly name=""b.dll""><references name=""c.dll"" /></assembly>" +
					@"<assembly name=""c.dll""><references name=""a.dll"" /></assembly>" +
				"</referenceCache>",
				@"<project name=""test"" default=""a"">" +
					@"<target name=""a"" depends=""b,c""/>" +
					@"<target name=""b"" depends=""c""/>" +
					@"<target name=""c""/>" +
				@"</project>");
			task.FailOnError = false;
			task.DoIt();
			Assert.AreEqual("Circular reference: a.dll <- c.dll <- b.dll\r\n", task.LogMessages);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// More complex circular reference: a - b - c - a and a - c - a; b comes first in
		/// reference cache, a last.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ComplexCircRefOrderBCA()
		{
			DummyCircularReferenceTask task = new DummyCircularReferenceTask(
				@"<referenceCache>" +
					@"<assembly name=""b.dll""><references name=""c.dll"" /></assembly>" +
					@"<assembly name=""c.dll""><references name=""a.dll"" /></assembly>" +
					@"<assembly name=""a.dll""><references name=""b.dll"" /><references name=""c.dll"" /></assembly>" +
				"</referenceCache>",
				@"<project name=""test"" default=""a"">" +
					@"<target name=""a"" depends=""b,c""/>" +
					@"<target name=""b"" depends=""c""/>" +
					@"<target name=""c""/>" +
				@"</project>");
			task.FailOnError = false;
			task.DoIt();
			Assert.AreEqual("Circular reference: a.dll <- c.dll <- b.dll\r\n", task.LogMessages);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// More complex circular reference: a - b - c - a and a - c - a; c comes first in
		/// reference cache, a last.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ComplexCircRefOrderCBA()
		{
			DummyCircularReferenceTask task = new DummyCircularReferenceTask(
				@"<referenceCache>" +
				@"<assembly name=""c.dll""><references name=""a.dll"" /></assembly>" +
				@"<assembly name=""b.dll""><references name=""c.dll"" /></assembly>" +
				@"<assembly name=""a.dll""><references name=""b.dll"" /><references name=""c.dll"" /></assembly>" +
				"</referenceCache>",
				@"<project name=""test"" default=""a"">" +
				@"<target name=""a"" depends=""b,c""/>" +
				@"<target name=""b"" depends=""c""/>" +
				@"<target name=""c""/>" +
				@"</project>");
			task.FailOnError = false;
			task.DoIt();
			Assert.AreEqual("Circular reference: a.dll <- c.dll <- b.dll\r\n", task.LogMessages);
		}
	}
}
