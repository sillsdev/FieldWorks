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
// File: ParseFiler.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// Implements the ParseFiler.
// </remarks>
// buildtest ParseFiler-nodep
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Globalization;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Summary description for ParseFiler.
	/// </summary>
	public sealed class ParseFiler : IFWDisposable
	{
		#region internal class

		internal class FormMSA
		{
			public int m_formId;
			public int m_msaId;

			public FormMSA(int formId)
			{
				m_formId = formId;
				m_msaId = 0;
			}
		}

		#endregion internal class

		#region Data members

		private SqlConnection m_connection;
		private int m_agentId;
		private int m_currentWordformId;
		private List<FormMSA> m_formMsaObjs;
		private FormMSA m_currentFormMSA;
		private string m_lastEngineUpdate;
		private int m_startAnalysesCount;

		#endregion Data members

		#region Properties

		private string XMLForAnalysis
		{
			get
			{
				// <Pair MsaId="492" FormId="494" Ord="1"/>
				int i = 1; // Ord is 1-based.
				StringBuilder sb = new StringBuilder("<root>");
				foreach(FormMSA fmsa in m_formMsaObjs)
					sb.AppendFormat("<Pair MsaId=\"{0}\" FormId=\"{1}\" Ord=\"{2}\"/>",
						fmsa.m_msaId.ToString(), fmsa.m_formId.ToString(), i++);
				sb.Append("</root>");
				return sb.ToString();
			}
		}

		#endregion Properties

		#region Construction and Disposal

		public ParseFiler(SqlConnection connection, int agentId)
		{
			Debug.Assert(connection != null && connection.State == System.Data.ConnectionState.Open);
			Debug.Assert(agentId > 0);

			m_connection = connection;
			m_agentId = agentId;

			// TODO: Get it from the XML file, when it becomes available.
			// '1998-05-02 01:23:56.123'
			DateTime dt = DateTime.Now;
			m_lastEngineUpdate = dt.ToString("yyyy-MM-dd", DateTimeFormatInfo.InvariantInfo)
				+ " "
				+ dt.ToString("T", DateTimeFormatInfo.InvariantInfo);
			m_formMsaObjs = new List<FormMSA>();
			m_currentWordformId = 0;
			ResetForNextWord();
		}
		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~ParseFiler()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		private void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_formMsaObjs != null)
					m_formMsaObjs.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_formMsaObjs = null;
			// Caller will handle disposal of m_connection,
			// since it provided it.
			m_connection = null;
			m_currentFormMSA = null;
			m_lastEngineUpdate = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#endregion Construction and Disposal

		#region Public methods
		/// <summary>
		/// Process the XML data.
		/// </summary>
		/// <param name="parse">The XML data to process.</param>
		/// <remarks>
		/// The 'parser' XML string may, or may not, be well formed XML.
		/// If there is an Exception node in the XML, then it may not be well-formed XML beyond that node,
		/// since the XAmple parser may have choked.
		/// This is why we can't use a DOM to get all of the XML, but we have to read it as it goes by in a stream.
		/// </remarks>
		public void ProcessParse(string parse)
		{
			CheckDisposed();

			Debug.Assert(parse != null);
			XmlTextReader reader = null;
			try
			{
				// Load the reader with the data file and ignore all white space nodes.
				reader = new XmlTextReader(new StringReader(String.Copy(parse)));
				reader.WhitespaceHandling = WhitespaceHandling.None;
				reader.MoveToContent();
				bool keepReading = true;

				// Parse the file and display each of the nodes.
				while (keepReading && reader.Read())
				{
					switch (reader.NodeType)
					{
						default:
							break; // Skip it.
						case XmlNodeType.Element:
							keepReading = ProcessElementStart(reader);
							break;
						case XmlNodeType.EndElement:
							keepReading = ProcessElementEnd(reader);
							break;
					}
				}
			}
			catch (Exception)
			{
				ResetForNextWord();
				throw; // Rethrow it so client can deal with it.
			}
			finally
			{
				if (reader != null)
					reader.Close();
				reader = null;
			}
		}

		#endregion Public methods

		#region Private methods

		#region Main XML Element processing methods

		private bool ProcessElementStart(XmlTextReader reader)
		{
			bool keepReading = true; // Be optimistic.
			switch (reader.Name)
			{
				default:
					break; // Do nothing for any others.
				case "Wordform":
				{
					Debug.Assert(m_agentId > 0);
					m_currentWordformId = GetId(reader);
					SqlCommand command = m_connection.CreateCommand();
					command.CommandTimeout = 60; // seconds, which is the default of 30.
					command.CommandText = string.Format("SELECT count(*) "
						+ "FROM WfiWordform_Analyses "
						+ "WHERE Src={0}", m_currentWordformId);
					m_startAnalysesCount = (int)command.ExecuteScalar();
					command.CommandText = string.Format("SELECT Id from CmBaseAnnotation_ "
						+ "WHERE BeginObject={0} and Source={1}", m_currentWordformId, m_agentId);
					SqlDataReader sqlReader = null;
					Set<int> problemIDs = new Set<int>();
					try // Needs a try here in order to deal with reader on a problem.
					{
						sqlReader = command.ExecuteReader();
						// Gather up a list of ids, since we have to close the reader,
						// before actually deleting them.
						while (sqlReader.Read())
							problemIDs.Add(sqlReader.GetInt32(0));
					}
					finally
					{
						if (sqlReader != null)
						{
							sqlReader.Close();
							sqlReader = null;
						}
					}
					// REVIEW/TODO: it would be faster to delete these all at once rather than one at a time.
					foreach(int problemID in problemIDs)
					{
						command = m_connection.CreateCommand();
						command.CommandText = string.Format("EXEC DeleteObjects '{0}'", problemID);
						command.ExecuteNonQuery();
					}
#if TrackingPFProblems
					// Set the id to the wordform you want to look at,
					// and then put a breakpoint on the Debug.WriteLine line.
					if (m_currentWordformId == 4616)
						Debug.WriteLine("Have biliya.");
#endif
					// REVIEW RandyR: Should this be turned into an SP?
					command = m_connection.CreateCommand();
					command.CommandText = String.Format(
						"DECLARE @d datetime;\n"
						+ "set @d = '{0}';\n"
						+ "declare @retval int;\n"
						+ "exec @retval = SetAgentEval {1}, {2}, 1, 'a wordform eval', @d;\n"
						+ "select @retval",
						m_lastEngineUpdate, m_agentId, m_currentWordformId);
					if ((int)command.ExecuteScalar() != 0)
						throw new Exception("Unspecified SetAgentEval stored procedure problem.");
					break;
				}
				case "WfiAnalysis":
				{
					Debug.Assert(m_formMsaObjs.Count == 0);
#if DOSOMETHINGFORFAILURE
					if (reader.IsEmptyElement)
					{
						try
						{
							ProcessAnalysis();
						}
						catch
						{
							ResetForNextWord();
							throw;
						}
						finally
						{
							Debug.Assert(m_currentWordformId > 0
								&& m_agentId > 0
								&& m_formMsaObjs.Count == 0);
						}
					}
#endif
					break;
				}
				case "MoForm":
				{
					Debug.Assert(m_currentFormMSA == null);
					m_currentFormMSA = new FormMSA(GetId(reader));
					break;
				}
				case "MSI":
					goto case "MSA"; // Fall through.
				case "MSA":
				{
					Debug.Assert(m_currentFormMSA != null
						&& m_currentFormMSA.m_formId > 0
						&& m_currentFormMSA.m_msaId == 0);

					m_currentFormMSA.m_msaId = GetId(reader);
					m_formMsaObjs.Add(m_currentFormMSA);
					m_currentFormMSA = null;
					break;
				}
				case "Exception":
				{
					Debug.Assert(reader.HasAttributes);
					// "<Exception code='ReachedMaxBufferSize' totalAnalyses='117'/>\n"
					string codeValue = null;
					string totalAnalysesValue = null;
					for (int i = 0; i < reader.AttributeCount; ++i)
					{
						reader.MoveToAttribute(i);
						switch (reader.Name)
						{
							default:
								Debug.Assert(false, "Unknown attribute '" + reader.Name + "' in <Exception> element.");
								break;
							case "code":
								codeValue = reader.Value;
								break;
							case "totalAnalyses":
								totalAnalysesValue = reader.Value;
								break;
						}
					}
					Debug.Assert(codeValue != null && totalAnalysesValue != null);
					string msg = null;
					switch (codeValue)
					{
						default:
							Debug.Assert(false, "Unknown code value: " + codeValue);
							break;
						case "ReachedMaxAnalyses":
							msg = String.Format(ParserCoreStrings.ksReachedMaxAnalysesAllowed,
								totalAnalysesValue);
							break;
						case "ReachedMaxBufferSize":
							msg = String.Format(ParserCoreStrings.ksReachedMaxInternalBufferSize,
								totalAnalysesValue);
							break;
					}
					reader.MoveToElement();
					SqlCommand command = m_connection.CreateCommand();
					command.CommandText = string.Format("exec CreateParserProblemAnnotation '{0}', {1}, {2}, {3}",
						msg, m_currentWordformId, m_agentId, "null"); // TODO: Replace "null" with an annotationDefn some day.
					command.CommandTimeout = 60; // seconds, which is the default of 30.
					command.ExecuteNonQuery();
					if (m_currentWordformId > 0)
					{
						// The least we can do is clear out any stale analyses.
						FinishWordForm();
						ResetForNextWord();
					}
					keepReading = false; // Stop, since the XML beyond this point may not be well-formed.
					break;
				}
			}
			return keepReading;
		}

		private bool ProcessElementEnd(XmlTextReader reader)
		{
			bool keepReading = true; // Be optimistic.
			switch (reader.Name)
			{
				default:
					break; // Do nothing for any others.
				case "AnalyzingAgentResults":
				{
					keepReading = false; // Stop, since nothing else useful can be found.
					break;
				}
				case "Wordform":
				{
					FinishWordForm();
					MarkAnalysisParseFailures();
					ResetForNextWord();
					break;
				}
				case "WfiAnalysis":
				{
					Debug.Assert(m_currentWordformId > 0
						&& m_agentId > 0
						&& m_formMsaObjs.Count > 0);
					try
					{
						ProcessAnalysis();
					}
					catch
					{
						// Do complete reset for an exception.
						ResetForNextWord();
						throw;
					}
					finally
					{
						// Do partial reset otherwise.
						// This will have already been done for an exception,
						// but not for normal useage.
						m_formMsaObjs.Clear();
					}
					break;
				}
			}
			return keepReading;
		}

		#endregion Main XML Element processing methods

		private void ProcessAnalysis()
		{
			string query = String.Format("DECLARE @d datetime;\n"
				+ "set @d = '{0}';\n"
				+ "declare @retval int;\n"
				+ "exec @retval = UpdWfiAnalysisAndEval$ {1}, {2}, '{3}', 1, 'an analysis eval', @d;\n"
				+ "select @retval;\n",
				m_lastEngineUpdate/* 0 */, m_agentId/* 1 */, m_currentWordformId/* 2 */, XMLForAnalysis/* 3 */);
			SqlCommand command = m_connection.CreateCommand();
			command.CommandText = query;
			command.CommandTimeout = 60; // seconds, which is the default of 30.
			switch ((int)command.ExecuteScalar())
			{
				case 0:	// Success.
					break;
				case 1:
					throw new Exception("sp_xml_preparedocument error.");
				case 2:
					throw new Exception("Form or MSA have no owner.");
				case 3:
					throw new Exception("Form and MSA have different owners.");
				case 4:
					throw new Exception("sp_xml_removedocument error.");
				case 5:
					throw new Exception("CreateOwnedObject error.");
				case 6:
					throw new Exception("Could not insert form data.");
				case 7:
					throw new Exception("Could not insert MSA data.");
				default:
					throw new Exception("Unspecified UpdWfiAnalysisAndEval stored procedure problem.");
			}
		}

		private void MarkAnalysisParseFailures()
		{
			// Together with the SetParseFailureEvals stored procedure, this solves LT-1842.
			string query = String.Format(
				"BEGIN TRAN\n"
				+ "DECLARE @d datetime;\n"
				+ "set @d = '{0}';\n"
				+ "declare @retval int;\n"
				+ "exec @retval = SetParseFailureEvals {1}, {2}, N'an analysis eval', @d;\n"
				+ "IF @retval = 0\n"
				+ "	COMMIT\n"
				+ "ELSE\n"
				+ "	ROLLBACK\n"
				+ "select @retval;\n",
				m_lastEngineUpdate/* 0 */, m_agentId/* 1 */, m_currentWordformId/* 2 */);
			SqlCommand command = m_connection.CreateCommand();
			command.CommandText = query;
			command.CommandTimeout = 60; // seconds, which is the default of 30.
			int nError = (int)command.ExecuteScalar();
			if (nError != 0)
			{
				throw new Exception(String.Format(
					"Unspecified SetParseFailureEvals stored procedure problem ({0}).",
					nError));
			}
		}

		private int GetId(XmlTextReader reader)
		{
			Debug.Assert(reader.HasAttributes);
			int id = 0;
			for (int i = 0; i < reader.AttributeCount; ++i)
			{
				reader.MoveToAttribute(i);
				if (reader.Name == "DbRef")
				{
					// Will throw an exception, if it isn't an integer.
					id = Convert.ToInt32(reader.Value);
					break;
				}
			}
			reader.MoveToElement();
			return id;
		}

		#region Wordform Preparation methods

		private void FinishWordForm()
		{
			SqlCommand command = m_connection.CreateCommand();
			command.CommandText = String.Format("exec RemoveUnusedAnalyses$ {0}, {1}, '{2}'",
				m_agentId, m_currentWordformId, m_lastEngineUpdate);
			// If the user has somehow accumulated a large number of unused analyses, it may
			// take more than 10 seconds to clear them out.  (We've measured as long as 74
			// seconds on a 3GHz machine.)  Therefore, the stored procedure removes a maximum
			// of only 16 objects on each call, returning a nonzero flag value to indicate
			// that at least one more object remains to be deleted.  Doing it this way
			// preserves the usefulness of the timeout mechanism while allowing an arbitrarily
			// large number of unused analyses to be deleted.
			int fMoreToDelete;
			int maxRetries = 30;
			int retries = 0;
			do
			{
				command.CommandTimeout = 60; // seconds, which is the default of 30.
				try
				{
					fMoreToDelete = (int)command.ExecuteScalar();
				}
				catch (SqlException s)
				{
					// JohnT: we've had these while interlinearizing with the parser.
					// It seems .NET 2.0 reports locking timeouts, while .NET 1.0 did not.
					// Seems worth a few retries, since this is a background process.
					// In any case, not worth making a crash of it, since there's a good
					// chance we can delete the orphan successfully in some later attempt.
					if (s.Number == 1222) // Lock request time out period exceeded.
					{
						Debug.WriteLine("RemoveUnusedAnalyses$ had a lock timeout");
						fMoreToDelete = 1; // anything non-zero will do
						retries++;
					}
					else
					{
						throw;
					}
				}
			} while (fMoreToDelete != 0 && retries < maxRetries);
			// Add wordform ID to the sync table.
			command.CommandText = String.Format(
				"exec StoreSyncRec$ '{0}', '{1}', '{2}', {3}, {4}",
				m_connection.Database, ParserScheduler.AppGuid, (int)SyncMsg.ksyncSimpleEdit,
				m_currentWordformId.ToString(),
				(int)WfiWordform.WfiWordformTags.kflidAnalyses);
			command.CommandTimeout = 60; // seconds, which is the default of 30.
			command.ExecuteNonQuery();
			// Flag a size change, if it did change.
			command.CommandText = string.Format("SELECT count(*) "
				+ "FROM WfiWordform_Analyses "
				+ "WHERE Src={0}", m_currentWordformId);
			if (m_startAnalysesCount != (int)command.ExecuteScalar())
			{
				// Add wordform ID to the sync table.
				command.CommandText = String.Format(
					"exec StoreSyncRec$ '{0}', '{1}', '{2}', {3}, {4}",
					m_connection.Database, ParserScheduler.AppGuid, (int)SyncMsg.ksyncFullRefresh,
					m_currentWordformId.ToString(),
					(int)WfiWordform.WfiWordformTags.kflidAnalyses);
				command.CommandTimeout = 60; // seconds, which is the default of 30.
				command.ExecuteNonQuery();
			}
		}

		/*------------------------------------------------------------------------------------------
			Clear out the data related to a particular word, get ready for the next one
		------------------------------------------------------------------------------------------*/
		private void ResetForNextWord()
		{
			m_currentWordformId = 0;
			m_currentFormMSA = null;
			m_formMsaObjs.Clear();
		}

		#endregion Wordform Preparation methods

		#endregion Private methods
	}
}
