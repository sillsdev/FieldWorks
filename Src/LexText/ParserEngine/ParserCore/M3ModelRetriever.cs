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
// File: M3ParserModelRetriever.cs
// Responsibility: John Hatton
// Last reviewed:
//
// <remarks>
//	this is  a MethodObject (see "Refactoring", Fowler).
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Xml;

using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FXT;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Summary description for M3ParserModelRetriever.
	/// </summary>
	/// <remarks>Is public for testing purposes</remarks>
	public class M3ParserModelRetriever
	{
		protected TaskReport m_topLevelTask;
		protected string m_sFxtOutputPath;
		protected string m_sFxtTemplateOutputPath;
		protected Stack<TaskReport> m_taskStack;
		protected string m_outputDirectory;
		protected string m_database;
		protected string m_sGafawsFxtPath;
		private XmlDocument m_modelDom;
		private XmlDocument m_templateDom;
		protected List<ChangedDataItem> m_changedItems = new List<ChangedDataItem>(50);


		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="M3ParserModelRetriever"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public M3ParserModelRetriever(string database)
		{
			m_taskStack = new Stack<TaskReport>();
			m_database = database;
			m_outputDirectory = System.IO.Path.GetTempPath();
			m_sGafawsFxtPath = Path.Combine(DirectoryFinder.FWCodeDirectory, @"Language Explorer\Configuration\Grammar\FXTs\M3GAFAWS.fxt");

		}


		/// <summary>
		///
		/// </summary>
		internal TimeStamp RetrieveModel(SqlConnection connection, string LangProject, TaskReport parentTask, ParserScheduler.NeedsUpdate eNeedsUpdate)
		{
			TimeStamp began = new TimeStamp(connection);
			using (FdoCache cache = FdoCache.Create(connection.DataSource, connection.Database, null))
			{

				BaseVirtualHandler.InstallVirtuals(@"Language Explorer\Configuration\Main.xml",
					new string[] { "SIL.FieldWorks.FDO.", "SIL.FieldWorks.IText." }, cache, true);

				string sDescription;
				string sFxtFile;
				SetDescriptionAndFxtFile(eNeedsUpdate, out sDescription, out sFxtFile);
				using (m_topLevelTask = parentTask.AddSubTask(sDescription))
				{
					m_taskStack.Push(m_topLevelTask);
					string sFxtPath = Path.Combine(DirectoryFinder.FWCodeDirectory, sFxtFile);
					m_sFxtOutputPath = Path.Combine(m_outputDirectory, m_database + "ParserFxtResult.xml");
					if ((eNeedsUpdate == ParserScheduler.NeedsUpdate.HaveChangedData) &&
						File.Exists(m_sFxtTemplateOutputPath))
					{
						try
						{
							DoUpdate(cache, sFxtPath, ref m_modelDom);
							DoUpdate(cache, m_sGafawsFxtPath, ref m_templateDom);
						}
						// (SteveMiller): Unremarked, the following often causes an error:
						// Warning as Error: The variable 'e' is declared but never used
						catch (XUpdaterException)
						{
							//Trace.WriteLine("XUpdater exception caught: " + e.Message);
							// do something useful for the user
							DoDump(eNeedsUpdate, cache, sFxtPath);
						}

					}
					else
						DoDump(eNeedsUpdate, cache, sFxtPath);
				}
			}
			m_topLevelTask = null;
			return began;
		}

		private void DoDump(ParserScheduler.NeedsUpdate eNeedsUpdate, FdoCache cache, string sFxtPath)
		{
			using (XDumper fxtDumper = new XDumper(cache))
			{
				//Trace.WriteLine("Retriever.DoDump");
				// N.B. It is crucial to include the ConstraintFilterStrategy here
				//      Without it, we end up passing ill-formed environments to the parser - a very bad thing
				//      See LT-6827 An invalid environment is being passed on to the parser and it should not.
				fxtDumper.Go(cache.LangProject as CmObject, sFxtPath, File.CreateText(m_sFxtOutputPath),
							 new IFilterStrategy[] { new ConstraintFilterStrategy() });
				if (eNeedsUpdate == ParserScheduler.NeedsUpdate.GrammarAndLexicon ||
					eNeedsUpdate == ParserScheduler.NeedsUpdate.LexiconOnly)
				{
					StartSubTask(ParserCoreStrings.ksRetrievingTemplateInformation);
					using (XDumper fxtDumperInner = new XDumper(cache))
					{
						m_sFxtTemplateOutputPath = Path.Combine(m_outputDirectory, m_database + "GAFAWSFxtResult.xml");
						fxtDumperInner.Go(cache.LangProject as CmObject, m_sGafawsFxtPath, File.CreateText(m_sFxtTemplateOutputPath));
					}
					EndSubTask();
				}
			}
		}
		public void DoUpdate(FdoCache cache, string sFxtPath, ref XmlDocument dom)
		{
			//Trace.WriteLine("Retriever.DoUpdate: entering");
			using (XUpdater fxtUpdater = new XUpdater(cache, sFxtPath))
			{
				//Trace.WriteLine("Retriever.DoUpdate: updating");
				dom = fxtUpdater.UpdateFXTResult(m_changedItems, dom);
			}
			//Trace.WriteLine("Retriever.DoUpdate: exiting");

		}

		private void SetDescriptionAndFxtFile(ParserScheduler.NeedsUpdate eNeedsUpdate, out string sDescription, out string sFxtFile)
		{
			const string ksFXTPath = @"Language Explorer\Configuration\Grammar\FXTs";
			switch (eNeedsUpdate)
			{
				case ParserScheduler.NeedsUpdate.GrammarAndLexicon:
					sDescription = ParserCoreStrings.ksRetrievingGrammarAndLexicon;
					sFxtFile = Path.Combine(ksFXTPath,"M3Parser.fxt");
					break;
				case ParserScheduler.NeedsUpdate.GrammarOnly:
					sDescription = ParserCoreStrings.ksRetrievingGrammar;
					sFxtFile = Path.Combine(ksFXTPath, "M3ParserGrammarOnly.fxt");
					break;
				case ParserScheduler.NeedsUpdate.LexiconOnly:
					sDescription = ParserCoreStrings.ksRetrievingLexicon;
					sFxtFile = Path.Combine(ksFXTPath, "M3ParserLexiconOnly.fxt");
					break;
				case ParserScheduler.NeedsUpdate.HaveChangedData:
					sDescription = ParserCoreStrings.ksUpdatingGrammarAndLexicon;
					sFxtFile = Path.Combine(ksFXTPath, "M3Parser.fxt");
					break;
				default:
					throw new ApplicationException("M3ModelRetriever.RetrieveModel() invoked without reason");
			}
		}

		protected void StartSubTask(string label)
		{
			m_taskStack.Push(CurrentTask.AddSubTask(label));
		}
		protected void EndSubTask()
		{
			TaskReport task = m_taskStack.Pop();
		}
		protected TaskReport CurrentTask
		{
			get
			{
				return m_taskStack.Peek();
			}
		}

		/// <summary>
		/// Get the model (FXT result) DOM
		/// </summary>
		/// <remarks>Is public for testing only</remarks>
		public System.Xml.XmlDocument ModelDom
		{
			get
			{
				Debug.Assert(m_sFxtOutputPath != null);
				Debug.Assert(File.Exists(m_sFxtOutputPath));
				if (m_modelDom == null)
				{
					m_modelDom = new XmlDocument();
					m_modelDom.Load(m_sFxtOutputPath);
				}
				return m_modelDom;
			}
		}

		internal System.Xml.XmlDocument TemplateDom
		{
			get
			{
				if (m_templateDom == null)
				{
					Debug.Assert(m_sFxtTemplateOutputPath != null);
					Debug.Assert(File.Exists(m_sFxtTemplateOutputPath));
					m_templateDom = new XmlDocument();
					m_templateDom.Load(m_sFxtTemplateOutputPath);
				}
				return m_templateDom;

			}
		}
		/// <summary>
		/// Get whether there are any stored changes or not
		/// </summary>
		internal bool HaveChangedData
		{
			get
			{
				return m_changedItems.Count > 0;
			}
		}

		public void StoreChangedDataItems(SqlDataReader sqlreader)
		{
			//Trace.WriteLine("StoreChangedDataItems - Clear");
			m_changedItems.Clear();
			while (sqlreader.Read())
			{
				ChangedDataItem item = new ChangedDataItem(sqlreader.GetInt32(0), sqlreader.GetInt32(1), sqlreader.GetInt32(2), sqlreader.GetString(3));
				m_changedItems.Add(item);
				//Trace.WriteLine("ChangedParserData: hvo=" + item.Hvo + " flid=" + item.Flid + " class=" + item.ClassId + " class name=" + item.ClassName);
			}
			sqlreader.Close();
		}

	}
}
