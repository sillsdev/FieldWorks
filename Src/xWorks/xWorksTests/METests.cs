using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>finder
	/// Summary description for InterlinTests.
	/// </summary>
	public class METests : RecordClerkTests
	{
		public METests() : base ("", METests.ConfigurationFilePath)
		{
			//
			// TODO: Add constructor logic here
			//
		}

		protected static string ConfigurationFilePath
		{
			get
			{
				string s = SIL.FieldWorks.Common.Utils.DirectoryFinder.FWCodeDirectory;
				return s+@"\morphologyeditor\me.xml";
			}
		}

		[Test]
		public void VisitEachTool()
		{
			//to do: automate the selection of these so that this will not break if something is removed
			//or leave one out if something is added
			SetTool("Adhoc Coprohibitions");
			SetTool("Compound Rules");
			SetTool("Grammar Sketch");
			SetTool("Phonemes");
		//	SetTool("Boundary Markers");
			SetTool("Environments");
			SetTool("Natural Classes");
			GoPartsOfSpeech ();
		}

		protected void GoPartsOfSpeech ()
		{
			SetTool("Categories");
		}

		[Test]
		//[Ignore("Test writing in progress.")]
		public void AddRemovePOS()
		{
			InsertDelete("PartsOfSpeech", "Categories", "CmdInsertPOS");
		}

		public void InsertDelete(string vectorName, string toolId, string insertCmdId)
		{
			VectorName = vectorName;
			SetTool(toolId);
			//DeleteUnnamedRecords();//temp
			DoInsertAndDeletionTest(insertCmdId);
		}
	}
}
