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
// File: FxtTestBase.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Xsl;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.Utils;

using NUnit.Framework;

namespace SIL.FieldWorks.Common.FXT
{
	/// <summary>
	/// Summary description for FxtTestBase.
	/// </summary>
	public class FxtTestBase
	{
		FdoCache m_fdoCache;
		/// <summary>Time when <see cref="startClock"/> was called</summary>
		protected DateTime m_dtstart;
		/// <summary>Timespan between the call of <see cref="startClock"/> and
		/// <see cref="stopClock"/></summary>
		protected TimeSpan m_tsTimeSpan;
		protected string m_databaseName;
		/// <summary>
		/// Location of XML result control files
		/// </summary>
		protected string m_sExpectedResultsPath = Path.Combine(SIL.FieldWorks.Common.Utils.DirectoryFinder.FwSourceDirectory,
			@"FXT\FxtDll\FxtDllTests\ExpectedResults");

		/// <summary>
		/// any filters that we want, for example, to only output items which satisfy their constraint.
		/// </summary>
		protected IFilterStrategy[] m_filters;

		/// <summary>
		/// start timing something.  Follow up with stopClock() and writeTimeSpan()
		/// </summary>
		protected void startClock()
		{
			m_dtstart = DateTime.Now;
		}
		/// <summary>
		/// stop timing something.  Follow up with writeTimeSpan ()
		/// </summary>
		protected void stopClock()
		{
			m_tsTimeSpan = new TimeSpan(DateTime.Now.Ticks - m_dtstart.Ticks);
		}
		/// <summary>
		/// write out a timespan in seconds and fractions of seconds to the debugger output window.
		/// </summary>
		/// <param name="sLabel"></param>
		protected void writeTimeSpan(string sLabel)
		{
			Console.WriteLine(sLabel + " " + m_tsTimeSpan.TotalSeconds.ToString() + " Seconds");
			Debug.WriteLine("");
			Debug.WriteLine("");
			Debug.WriteLine("");
			Debug.WriteLine(sLabel + " " + m_tsTimeSpan.TotalSeconds.ToString() + " Seconds");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="UnitTest"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public FxtTestBase()
		{
			m_databaseName= "";
		}

		[TestFixtureSetUp]
		public void FixtureInit()
		{
		}

		protected void LoadCache(string name)
		{
			Dictionary<string, string> cacheOptions = new Dictionary<string, string>();
			cacheOptions.Add("db", name);
			m_fdoCache = FdoCache.Create(cacheOptions);
		}

		[TestFixtureTearDown]
		public void FixtureCleanUp()
		{
		}

		[TearDown]
		public void TestCleanUp()
		{
			if (m_fdoCache != null)
			{
				m_fdoCache.Dispose();
				m_fdoCache = null;
			}
		}

		protected void DoDump (string databaseName, string label, string fxtPath, string answerPath)
		{
			DoDump(databaseName, label, fxtPath, answerPath, false);
		}
		protected void DoDump (string databaseName, string label, string fxtPath, string answerPath, bool outputGuids)
		{
			DoDump(databaseName, label, fxtPath, answerPath, outputGuids, false);
		}

		protected void DoDump (string databaseName, string label, string fxtPath, string answerPath, bool outputGuids, bool bApplyTransform)
		{
#if !Trying
			try
			{
				XDumper dumper = PrepareDumper(databaseName, fxtPath, outputGuids);
				string outputPath = CreateTempFile();
				PerformDump(dumper, outputPath, databaseName, label);
				if(answerPath!=null)
					CheckXmlEquals(answerPath, outputPath, bApplyTransform);
			}
			catch (Exception e)
			{
				string s = e.Message;
				if(e.InnerException != null)
				{
					string s2 = e.InnerException.ToString();
				}
				throw;
			}
#else
			XDumper dumper = PrepareDumper(databaseName, fxtPath, outputGuids);
			string outputPath = CreateTempFile();
			PerformDump(dumper, outputPath, databaseName, label);
			CheckXmlEquals(answerPath, outputPath, bApplyTransform);
#endif
		}

		private void CheckXmlEquals(string sAnswerPath, string outputPath, bool bApplyTransform)
		{
			StreamReader test;
			//disabled because the transform is dropping things like comments, which kills the test
			//			if (bApplyTransform)
//			{
//				string sTransformedResult = NormalizeResult(outputPath);
//				test = new StreamReader(sTransformedResult);
//			}
//			else
				test = new StreamReader(outputPath);
			StreamReader control = new StreamReader(sAnswerPath);
			//			XmlDocument a = new XmlDocument();
			//			XmlDocument b = new XmlDocument();
			//			a.LoadXml(control.ReadToEnd());
			//			a.Save(@"c:\expected.xml");
			//			b.LoadXml(test.ReadToEnd());
			//			b.Save(@"c:\actual.xml");
			//			Assert.AreEqual(a.OuterXml, b.OuterXml, "Output Differs");
			Assert.AreEqual(control.ReadToEnd(), test.ReadToEnd(), "FXT Output Differs. If you have done a model change, you can update the 'correct answer' xml files by runing fw\\bin\\FxtAnswersUpdate.bat.");
			//XmlUnit.XmlAssertion.AssertXmlEquals(control, test);
		}

		private string NormalizeResult(string outputPath)
		{
			string sTransformPath = Path.Combine(SIL.FieldWorks.Common.Utils.DirectoryFinder.FwSourceDirectory,
				@"FXT\FxtDll\FxtDllTests");
			string xsl = Path.Combine(sTransformPath, "NormalizeOutput.xsl");
			string sTransformedResultPath = CreateTempFile();

			//			XmlDocument result = new XmlDocument();
			//			result.LoadXml(outputPath);
			//			transformer.Transform(

			PerformTransform(xsl, outputPath, sTransformedResultPath);
			return sTransformedResultPath;
		}

		protected static void PerformTransform(string xsl, string inputPath, string sTransformedResultPath)
		{
			XslCompiledTransform transformer = new XslCompiledTransform();
			transformer.Load(xsl);
			TextWriter writer = null;
			try
			{
				//writer = File.CreateText(sTransformedResultPath);
				//XmlDocument inputDOM = new XmlDocument();
				//inputDOM.Load(inputPath);
				//transformer.Transform(inputDOM, null, writer);
				transformer.Transform(inputPath, sTransformedResultPath);
			}
			finally
			{
				if (writer != null)
					writer.Close();
			}
		}

		protected void PerformDump(XDumper dumper, string outputPath, string databaseName, string label)
		{
			startClock();
			dumper.Go(m_fdoCache.LangProject as CmObject, File.CreateText(outputPath), m_filters);
			stopClock();
			writeTimeSpan(databaseName+": " + label);
		}

		protected XDumper PrepareDumper(string databaseName, string fxtPath, bool doOutputGuids)
		{
			LoadCache(databaseName);
			XDumper dumper = new XDumper(m_fdoCache);
			dumper.FxtDocument = new XmlDocument();
			dumper.FxtDocument.Load(fxtPath);
			dumper.OutputGuids= doOutputGuids;
			return dumper;
		}

		private string CreateTempFile()
		{
			return CreateTempFile("xml");
		}

		protected string CreateTempFile(string ext)
		{
			return FwTempFile.CreateTempFileAndGetPath(ext);
		}

	}
}
