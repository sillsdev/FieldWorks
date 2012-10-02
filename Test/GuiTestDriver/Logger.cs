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
// File: Logger.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Collections;
using NUnit.Framework; // for Asserts

namespace GuiTestDriver
{
	/// <summary>
	/// Logger records the instructions that executed, comments and results.
	/// It is "crash" safe since data is flushed to the file as it is recorded.
	/// </summary>
	public class Logger
	{
		private static Logger m_log = null;
		private StreamWriter m_sw = null;
		private bool m_init = false;
		private static bool s_isClosed = true;

		public enum Disposition { Pass, Fail, Hung };

		/// <summary>
		/// Gets the only logger object for this test.
		/// It is created from TestState data when necessary.
		/// </summary>
		/// <returns>The logger object.</returns>
		public static Logger getOnly()
		{
			if (m_log == null || s_isClosed) m_log = new Logger();
			return m_log;
		}

		private Logger()
		{  // open a file
			TestState ts = TestState.getOnly();
			string path = ts.getScriptPath() +@"\";
			string script;
			if (ts.Script == null)
				script = "Logger2" + ".xlg";
			else
				script = ts.Script.Split(".".ToCharArray(),2)[0] + ".xlg";
			m_sw = File.CreateText(path + script);
			m_init = false;
			s_isClosed = false;
		}

		/// <summary>
		/// Initializes the log with xml header stuff.
		/// </summary>
		private void initLog()
		{
			TestState ts = TestState.getOnly();
			string date = DateTime.Now.Date.ToShortDateString();
			m_sw.WriteLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
			m_sw.WriteLine(@"<?xml-stylesheet type=""text/xsl"" href=""gtdLog.xsl""?>");
			m_sw.WriteLine(@"<gtdLog>");
			m_sw.WriteLine(@" <set-up date=""{0}"">", date);
			m_sw.WriteLine(@"  <application path=""{0}"" exe=""{1}""/>", ts.getAppPath(), ts.getAppExe());
			m_sw.WriteLine(@"  <script path=""{0}"" name=""{1}""/>", ts.getScriptPath(), ts.Script);
			m_sw.WriteLine(@"  <model path=""{0}"" name=""{1}""/>", ts.getModelPath(), ts.getModelName());
			m_sw.WriteLine(@" </set-up>");
			m_init = true;
		}

		/// <summary>
		/// Records the assertion in the log with no context.
		/// </summary>
		/// <param name="type">The assertion type - notNull, Equals, etc..</param>
		/// <param name="text">The assertion text.</param>
		public void assertion(string type, string text)
		{
			if (!s_isClosed)
			{
				if (!m_init) initLog();
				m_sw.Write(@" <assertion type="""+type+@""">");
				m_sw.Write(Utilities.attrText(text));
				m_sw.WriteLine(@"</assertion>");
				m_sw.Flush();
			}
		}

		/// <summary>
		/// Records the paragraph in the log with no context.
		/// </summary>
		/// <param name="para">The paragraph content.</param>
		public void paragraph(string para)
		{
			if (!s_isClosed && para != null)
			{
				if (!m_init) initLog();
				m_sw.Write(@" <p>");
				m_sw.Write(Utilities.attrText(para));
				m_sw.WriteLine(@"</p>");
				m_sw.Flush();
			}
		}

		/// <summary>
		/// Records the "must see" part of a list in the log.
		/// Can be followed by calls to listItem, but must thereafter
		/// be followed by a call to endList().
		/// </summary>
		/// <param name="head">The list header content.</param>
		public void startList(string head)
		{
			if (!s_isClosed && head != null)
			{
				if (!m_init) initLog();
				m_sw.Write(@" <head>");
				m_sw.WriteLine(Utilities.attrText(head));
				m_sw.Flush();
			}
		}

		/// <summary>
		/// Records the "must see" part of a list in the log with no context.
		/// </summary>
		/// <param name="head">The list header content.</param>
		public void endList()
		{
			if (!s_isClosed)
			{
				if (!m_init) initLog();
				m_sw.WriteLine(@"</head>");
				m_sw.Flush();
			}
		}

		/// <summary>
		/// Records a list item in the log with no context.
		/// a series of list items should be preceded by a call to startList().
		/// </summary>
		/// <param name="item">The item content.</param>
		public void listItem(string item)
		{
			if (!s_isClosed && item != null)
			{
				if (!m_init) initLog();
				m_sw.Write(@" <item>");
				m_sw.Write(Utilities.attrText(item));
				m_sw.WriteLine(@"</item>");
				m_sw.Flush();
			}
		}

		/// <summary>
		/// Create an empty element in the log using the image() method
		/// on an instruction. This is invoked from Instruction.Execute
		/// to insure each instruction is marked at some point before or
		/// during its execution. The image() methods report the state of
		/// the instruction, including some internal class members for
		/// debugging.
		/// </summary>
		/// <param name="ins">An instruction</param>
		public void mark(Instruction ins)
		{
			if (!s_isClosed)
			{
				if (!m_init) initLog();
				m_sw.WriteLine(@" <{0}/>", ins.image());
				m_sw.Flush();
			}
		}

		/// <summary>
		/// Simple results are logged via this empty report element.
		/// More complex ones use the other logging methods.
		/// This uses the resultImage() method from the instruction.
		/// This is typically called after mark().
		/// </summary>
		/// <param name="ins">An instruction</param>
		public void result(Instruction ins)
		{
			if (!s_isClosed)
			{
				if (!m_init) initLog();
				m_sw.WriteLine(@" <result {0}/>", ins.resultImage());
				m_sw.Flush();
			}
		}

		/// <summary>
		/// Close the log and sound off according to how the test ended.
		/// </summary>
		/// <param name="action">The disposition of the test</param>
		public void close(Disposition action)
		{
			if (!s_isClosed)
			{
				Sound snd = new Sound();
				if (action == Disposition.Pass) {
					snd.Frequency = 800;
					snd.Duration = 500;
				}
				else if (action == Disposition.Fail) {
					snd.Frequency = 300;
					snd.Duration = 1000;
				}
				else { // Hung
					snd.Frequency = 500;
					snd.Duration = 200;
				}

				snd.Execute(); // a tone indicating disposition

				try
				{
					m_sw.WriteLine(@"</gtdLog>");
					m_sw.Close();
				}
				catch (Exception e)
				{
					Console.Out.WriteLine("Log failed: " + e.Message);
					action = Disposition.Hung;
				}
				s_isClosed = true;
				m_init = false;
			}
		}

		// The following wrap the NUnit.Framework Assert class methods
		// to insure that the instruction that uses it will be uniquely
		// identified. This helps locate unambiguously the instruction
		// that asserted.
		public void areEqual(object expected, object actual, string message)
		{
			if (!expected.Equals(actual))
			{
				m_log.assertion("areEqual", message);
				m_log.close(Disposition.Fail);
			}
			Assert.AreEqual(expected, actual, message);
		}
		public void areSame(object expected, object actual, string message)
		{
			if (expected != actual)
			{
				m_log.assertion("areSame", message);
				m_log.close(Disposition.Fail);
			}
			Assert.AreSame(expected, actual, message);
		}
		public void fail(string message)
		{
			m_log.assertion("fail", message);
			m_log.close(Disposition.Fail);
			Assert.Fail(message);
		}
		public void ignore(string message)
		{
			Assert.Ignore(message);
		}
		public void isFalse(bool condition, string message)
		{
			if (condition)
			{
				m_log.assertion("isFalse", message);
				m_log.close(Disposition.Fail);
			}
			Assert.IsFalse(condition, message);
		}
		public void isNotNull(object obj, string message)
		{
			if (obj == null)
			{
				m_log.assertion("isNotNull", message);
				m_log.close(Disposition.Fail);
			}
			Assert.IsNotNull(obj, message);
		}
		public void isNull(object obj, string message)
		{
			if (obj != null)
			{
				m_log.assertion("isNull", message);
				m_log.close(Disposition.Fail);
			}
			Assert.IsNull(obj, message);
		}
		public void isTrue(bool condition, string message)
		{
			if (!condition)
			{
				m_log.assertion("isTrue", message);
				m_log.close(Disposition.Fail);
			}
			Assert.IsTrue(condition, message);
		}

	}
}
