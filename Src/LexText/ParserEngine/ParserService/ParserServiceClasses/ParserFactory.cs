using System;
using System.Collections.Generic;
using System.Diagnostics;

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// ParserFactory manages ParserSchedulers, maintaining at most one for each database.
	/// It is normally a singleton.
	/// </summary>
	public sealed class ParserFactory : IFWDisposable
	{
		private int itest;
		private Dictionary<string, ParserScheduler> m_hParsers;
		static ParserFactory s_factory;

		private ParserFactory()
		{
			m_hParsers = new Dictionary<string, ParserScheduler>();
			itest = 0;
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
		~ParserFactory()
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
				ReleaseAllParsers();
				if (m_hParsers != null)
					m_hParsers.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			// All parsers in m_hParsers are Disposed in the ReleaseAllParsers method.
			m_hParsers = null;
			s_factory = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		static private ParserFactory TheOneInstance
		{
			get
			{
				if (s_factory == null)
					s_factory = new ParserFactory();
				return s_factory;
			}
		}

		static public ParserScheduler GetDefaultParser(String sServer, String sDatabase, String sLangProj)
		{
			return TheOneInstance.GetDefaultParser1(sServer, sDatabase, sLangProj);
		}

		/// <summary>
		/// Return true if a parser has already been created for this key.
		/// </summary>
		/// <param name="sServer"></param>
		/// <param name="sDatabase"></param>
		/// <param name="sLangProj"></param>
		/// <returns></returns>
		static public bool HasParser(String sServer, String sDatabase, String sLangProj)
		{
			return TheOneInstance.m_hParsers.ContainsKey(MakeKey(sServer, sDatabase, sLangProj));
		}

		static private string MakeKey(String sServer, String sDatabase, String sLangProj)
		{
			return sServer + "  " + sDatabase + "  " + sLangProj;
		}

		// per KenZ, convention is for server to already include the \\SILFW,e.g. HATTON1\\SILFW
		private ParserScheduler GetDefaultParser1(String sServer, String sDatabase, String sLangProj)
		{
			String sKey = MakeKey(sServer, sDatabase, sLangProj);

			if(m_hParsers.ContainsKey(sKey))
			{
				/* This caused a security problem with at least one machine configuration, so
					 we no longer want to use it. LT-5599
				System.Diagnostics.EventLog.WriteEntry("ParserFactory", "Gave out existing parser for " + sServer +"-"+sDatabase+"-"+sLangProj); */
				ParserScheduler parsched = m_hParsers[sKey];
				parsched.AddRef();
				return parsched;
			}

			try
			{
				ParserScheduler parser = ParserScheduler.CreateInDomain(sServer, sDatabase, sLangProj);

				m_hParsers.Add(sKey, parser);
				//System.Diagnostics.EventLog.WriteEntry("ParserFactory", "Created a parser for " + sServer +"-"+sDatabase+"-"+sLangProj);
				return parser;
			}
			catch(Exception)
			{
				//MessageBox.Show("ParserFactory could not start the M3Parser. " + err.Message);
				throw;
			}
		}

		static public void ReleaseScheduler(ParserScheduler parser)
		{
			TheOneInstance.ReleaseScheduler1(parser);
			// If there are multiple windows open with their own Scheduler,
			// we shouldn't dispose of the factory too.  That will cause the other
			// parser objects to be disposed out from under the other windows.
			// See LT-6266.
			//s_factory.Dispose();
			//s_factory = null;
		}

		private void ReleaseScheduler1(ParserScheduler parser)
		{
			int cref = parser.SubtractRef();
			if (cref <= 0)
			{
				m_hParsers.Remove(MakeKey(parser.Server, parser.Database, parser.LangProject));
				parser.Dispose();
			}
		}

		// what we want here is to release this service's refs to the parsers
		private void ReleaseAllParsers()
		{
			foreach(ParserScheduler parser in m_hParsers.Values)
			{
				// review:  if someone else is holding on to the ref, they will choke when they touch it.
				// RBR: I'd say it was too bad for them, since we made it,
				// we are the only one who should zap it.
				// If a client is to be responsible for Disposing it,
				// then this object must provide a way for it to remove the parser.
				// The main problem with having a client do it, is that there could be more than one client,
				// so the Disposal will still cause problems.
				parser.Dispose();
			}
			m_hParsers.Clear();
		}

		private System.Collections.ICollection GetRunningParsersKeys()
		{
			return m_hParsers.Keys;
		}

		public int test
		{
			get
			{
				CheckDisposed();

				return itest;
			}
			set
			{
				CheckDisposed();

				itest = test;
			}
		}

		public void increment()
		{
			CheckDisposed();

			itest++;
		}
	}
}
