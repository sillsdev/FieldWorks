// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DummyParser.cs
// Responsibility: John Hatton
// Last reviewed:
//
// <remarks>
//	this class is for testing only.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using XAmpleCOMWrapper;
using System.Diagnostics;
using System.Data.SqlClient;

namespace SIL.FieldWorks.WordWorks.Parser
{
#if false // Wht is this used for?
	/// <summary>
	///a Parser which just pretend to do something, but does not.
	/// </summary>
	public class DummyParser : ParserWorker
	{

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyParser"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DummyParser(SqlConnection connection, string database, string LangProject, TaskUpdateEventHandler handler)
			:base(connection, database, LangProject, handler)
		{
		}

		public new void  UpdateWordform(int hvo)
		{
			TaskReport task = new TaskReport("Update Wordform", m_taskUpdateHandler);
			using (task)
			{
				m_xample.ParseWord("testing");

				using(task.AddSubTask("step 1"))
				{
					Delay(2000000);
				}
				using(task.AddSubTask("step 2"))
				{
					Delay(2000000);
				}
			}
		}

		protected void Delay(int ticks)
		{
			DateTime t = DateTime.Now;
			while(DateTime.Now.Ticks < t.Ticks + ticks)
				;
		}

		public new TimeStamp LoadGrammarAndLexicon(ParserScheduler.NeedsUpdate eNeedsUpdate)
		{
			using (new TaskReport("Load Grammar", m_taskUpdateHandler))
			{
				Delay(10000000);
				return new TimeStamp(m_connection);//DateTime.Now;
			}
		}
	}
#endif
}
