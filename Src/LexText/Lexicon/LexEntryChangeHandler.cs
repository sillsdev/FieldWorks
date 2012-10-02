using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Helper class for handling automatic homograph numbering when the citation form changes.
	/// </summary>
	public class LexEntryChangeHandler : IRecordChangeHandler, IFWDisposable
	{
		#region Data members

		/// <summary>lex entry being monitored for changes</summary>
		protected LexEntry m_le = null;
		/// <summary>original citation form</summary>
		protected string m_originalHomographForm = null;
		/// <summary>original morph type HVO</summary>
		protected int m_originalMorphType = 0;
		/// <summary></summary>
		protected IRecordListUpdater m_rlu = null;

		#endregion Data members

		#region Construction

		/// <summary>Default constructor, needed for reflection</summary>
		public LexEntryChangeHandler()
		{
		}

		#endregion Construction

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
		~LexEntryChangeHandler()
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
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				if (Disposed != null)
					Disposed(this, new EventArgs());

				// Dispose managed resources here.
				if (m_rlu != null)
					m_rlu.RecordChangeHandler = null;
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_le = null;
			m_originalHomographForm = null;
			m_rlu = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region IRecordChangeHandler implementation

		/// <summary>
		/// Let users know it is beiong dispsoed
		/// </summary>
		public event EventHandler Disposed;

		/// <summary>
		/// True, if the updater was not null in the Setup call, otherwise false.
		/// </summary>
		public bool HasRecordListUpdater
		{
			get
			{
				CheckDisposed();

				return m_rlu != null;
			}
		}

		/// <summary></summary>
		public void Setup(object o, IRecordListUpdater rlu)
		{
			CheckDisposed();

			Debug.Assert(o != null && o is LexEntry);
			m_le = o as LexEntry;
			Debug.Assert(m_le != null);
			m_originalHomographForm = m_le.HomographForm;
			m_originalMorphType = m_le.MorphType;
			if (rlu != null)
			{
				m_rlu = rlu;
				m_rlu.RecordChangeHandler = this;
			}
		}

		/// <summary>Handle possible homograph number changes:
		/// 1. Possibly remove homograph from original citation form.
		/// 2. Possibly add homograph for new citation form.
		/// </summary>
		public void Fixup(bool fRefreshList)
		{
			CheckDisposed();

			Debug.Assert(m_le != null);
			if (m_le.IsValidObject())
			{
				string currentHomographForm = m_le.HomographForm;
				int currentMorphType = m_le.MorphType;
				if (currentHomographForm == m_originalHomographForm &&
					currentMorphType == m_originalMorphType)
				{
					// No relevant changes, so do nothing.
					return;
				}

				List<ILexEntry> ieAllEntries = new List<ILexEntry>(m_le.Cache.LangProject.LexDbOA.EntriesOC.ToArray());
				// Reset any homograph numbers associated with the old form.
				// This version of CollectHomographs will exclude m_le,
				// which means all the old homographs of the entry will be renumbered.
				List<ILexEntry> homographs = LexEntry.CollectHomographs(m_originalHomographForm,
					0, // Collect all of them.
					ieAllEntries,
					m_originalMorphType);
				LexEntry.ValidateExistingHomographs(homographs);
				// Set any homograph numbers associated with the new form,
				// and include m_le in the renumbering.
				homographs = LexEntry.CollectHomographs(currentHomographForm,
					0, // Collect all of them.
					ieAllEntries,
					currentMorphType);
				LexEntry.ValidateExistingHomographs(homographs);

				// Fix it so that another call will do the right thing.
				m_originalHomographForm = currentHomographForm;
				m_originalMorphType = currentMorphType;
			}
			else
			{
				// If our old entry isn't even valid any more, something has deleted it,
				// and whatever did so should have fixed up the list. We really don't want
				// to reload the whole thing if we don't need to (takes ages in a big lexicon),
				// so do nothing...JohnT
				return;
			}
			if (fRefreshList && m_rlu != null)
				m_rlu.UpdateList(false);
		}

		#endregion IRecordChangeHandler implementation
	}
}
