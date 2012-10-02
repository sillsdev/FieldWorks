	using System;
	using System.Diagnostics;
	using System.IO;
	using SIL.FieldWorks.Common.Controls;
	using SIL.FieldWorks.Common.Utils;
	using SIL.FieldWorks.FDO;
	using System.Runtime.InteropServices; // needed for Marshal
	using SIL.FieldWorks.Common.COMInterfaces;
	using XCore;

	namespace SIL.FieldWorks.XWorks.LexText
	{
		/// <summary>
		/// ExecuteSQLScript can be used with the Tools:Utilities dialog
		/// It was actually built for Dennis Walters, but could be useful for someone else.
		/// </summary>
		public class ExecuteSQLScript : IUtility
		{
			#region Data members

			private UtilityDlg m_dlg;
			// Name to show in list box.
			private string m_name;
			// SQL query to execute
			private string m_sqlQuery;
			// When description to show in list box.
			private string m_whenDescr;
			// What description to show in list box
			private string m_whatDescr;
			// Caution (Redo) description to show in list box
			private string m_redoDescr;

			#endregion Data members

			/// <summary>
			/// Constructor.
			/// </summary>
			public ExecuteSQLScript()
			{
			}

			/// <summary>
			/// Constructor.
			/// </summary>
			public ExecuteSQLScript(UtilityDlg dlg, string name, string whenDescr,
				string whatDescr, string redoDescr, string sqlQuery)
			{
				m_dlg = dlg;
				m_name = name;
				m_whenDescr = whenDescr;
				m_whatDescr = whatDescr;
				m_redoDescr = redoDescr;
				m_sqlQuery = sqlQuery;
			}

			/// <summary>
			/// Override method to return the Label property.
			/// </summary>
			/// <returns></returns>
			public override string ToString()
			{
				return Label;
			}

			#region IUtility implementation

			/// <summary>
			/// Get the main label describing the utility.
			/// </summary>
			public string Label
			{
				get
				{
					Debug.Assert(m_dlg != null);
					return m_name;
				}
			}

			/// <summary>
			/// Set the UtilityDlg.
			/// </summary>
			/// <remarks>
			/// This must be set, before calling any other property or method.
			/// </remarks>
			public UtilityDlg Dialog
			{
				set
				{
					Debug.Assert(value != null);
					Debug.Assert(m_dlg == null);

					m_dlg = value;
				}
			}

			/// <summary>
			/// Load 0 or more items in the list box.
			/// </summary>
			public void LoadUtilities()
			{
				Debug.Assert(m_dlg != null);

				// Set up a Utility item for every valid *.sql file in the Extensions directory.
				string sqlPath = Path.Combine(DirectoryFinder.FWCodeDirectory,
					@"Language Explorer\Configuration\Extensions");
				string[] sqlFiles = Directory.GetFiles(sqlPath, "*.sql");

				foreach (string path in sqlFiles)
				{
					if (!path.EndsWith(".sql"))
						continue;	// ignore editor backup files.
					using (StreamReader sr = new StreamReader(path))
					{
						string name;
						string whenDescr;
						string whatDescr;
						string redoDescr;
						string sqlQuery = null;
						String line;
						line = sr.ReadLine();
						if (line != null && line.StartsWith("----Name: "))
						{
							name = line.Substring(10);
							sqlQuery += line + "\r\n";
						}
						else
							continue;
						line = sr.ReadLine();
						if (line != null && line.StartsWith("----What: "))
						{
							whatDescr = line.Substring(10);
							sqlQuery += line + "\r\n";
						}
						else
							continue;
						line = sr.ReadLine();
						if (line != null && line.StartsWith("----When: "))
						{
							whenDescr = line.Substring(10);
							sqlQuery += line + "\r\n";
						}
						else
							continue;
						line = sr.ReadLine();
						if (line != null && line.StartsWith("----Caution: "))
						{
							redoDescr = line.Substring(13);
							sqlQuery += line + "\r\n";
						}
						else
							continue;
						sqlQuery += sr.ReadToEnd();
						m_dlg.Utilities.Items.Add(new ExecuteSQLScript(m_dlg, name,
							whenDescr, whatDescr, redoDescr, sqlQuery));
					}
				}
			}

			/// <summary>
			/// Notify the utility is has been selected in the dlg.
			/// </summary>
			public void OnSelection()
			{
				Debug.Assert(m_dlg != null);
				m_dlg.WhenDescription = m_whenDescr;
				m_dlg.WhatDescription = m_whatDescr;
				m_dlg.RedoDescription = m_redoDescr;
			}

			/// <summary>
			/// Have the utility do what it does.
			/// </summary>
			public void Process()
			{
				IOleDbCommand odc = null;
				m_dlg.ProgressBar.Minimum = 0;
				m_dlg.ProgressBar.Maximum = 100;
				m_dlg.ProgressBar.Step = 50;
				m_dlg.ProgressBar.PerformStep();
				try
				{
					Debug.Assert(m_dlg != null);
					// Send the query to the database.
					FdoCache cache = (FdoCache)m_dlg.Mediator.PropertyTable.GetValue("cache");
					string db = cache.DatabaseName;
					cache.DatabaseAccessor.CreateCommand(out odc);
					odc.ExecCommand(m_sqlQuery, (int)SqlStmtType.knSqlStmtNoResults);
				}
				catch(Exception e)
				{
					System.Windows.Forms.MessageBox.Show(
						String.Format(LexTextStrings.ksErrorMsgWithStackTrace, e.Message, e.StackTrace));
				}
				finally
				{
					if (odc != null)
						Marshal.FinalReleaseComObject(odc);
				}
			}
			#endregion IUtility implementation
		}

	}
