// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Lexicon
{
	/// <summary/>
	public class LexEntryChangeHandler : IRecordChangeHandler
	{
		#region Data members

		/// <summary>entry being monitored for changes.</summary>
		protected ILexEntry m_entry;
		/// <remarks>needed to do UnitOfWork</remarks>
		protected LcmCache m_cache;

		protected ICmObject m_obj;

		#endregion Data members

		#region IDisposable & Co. implementation

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(string.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; }

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// </summary>
		~LexEntryChangeHandler()
		{
			Dispose(false);
		}

		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");

			if (IsDisposed) // Must not be run more than once.
				return;

			if (disposing && Disposed != null)
				Disposed(this, EventArgs.Empty);

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_entry = null;

			IsDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region IRecordChangeHandler implementation

		/// <summary>
		/// Let users know it is being disposed
		/// </summary>
		public event EventHandler Disposed;

		/// <summary>
		/// True if this was passed a RecordListUpdater during Setup
		/// </summary>
		public bool HasRecordListUpdater { get; private set; }

		/// <summary/>
		public void Setup(object record, IRecordListUpdater rlu, LcmCache cache)
		{
			CheckDisposed();

			Debug.Assert(record is ILexEntry);
			m_entry = (ILexEntry)record;
			if (rlu != null)
			{
				HasRecordListUpdater = true;
				rlu.RecordChangeHandler = this;
			}
			m_cache = cache;
		}

		/// <summary>
		/// Remove dangling LexEntryRefs (Variants and Complex Forms), because if the user removed the last Component,
		/// they probably want to delete the entire LexEntryRef.
		/// </summary>
		public void Fixup(bool fRefreshList)
		{
			CheckDisposed();
			// If our old entry isn't even valid any more, something has deleted it, and whatever did so should have fixed up the list.
			// We really don't want to reload the whole thing if we don't need to (takes ages in a big lexicon), so do nothing...JohnT
			if (!m_entry.IsValidObject)
				return;

			var danglingRefs = m_entry.EntryRefsOS.Where(ler => !(ler.ComponentLexemesRS.Any() || ler.ComplexEntryTypesRS.Any() || ler.VariantEntryTypesRS.Any())).ToList();
			var typelessRefs = m_entry.EntryRefsOS.Where(ler => ler.ComponentLexemesRS.Any() && !(ler.ComplexEntryTypesRS.Any() || ler.VariantEntryTypesRS.Any())).ToList();

			if (danglingRefs.Any())
			{
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_cache.ActionHandlerAccessor, () => danglingRefs.ForEach(ler => ler.Delete()));
			}

			if (typelessRefs.Any())
			{
				var unspecVariantEntryType = (ILexEntryType)m_entry.Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS
					.First(lrt => lrt.Guid == LexEntryTypeTags.kguidLexTypeUnspecifiedVar);
				var unspecComplexEntryType = (ILexEntryType)m_entry.Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS
					.First(lrt => lrt.Guid == LexEntryTypeTags.kguidLexTypeUnspecifiedComplexForm);
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_cache.ActionHandlerAccessor, () =>
				{
					foreach (var refEntry in typelessRefs)
					{
						switch (refEntry.RefType)
						{
							case 0:
								refEntry.VariantEntryTypesRS.Add(unspecVariantEntryType);
								break;
							case 1:
								refEntry.ComplexEntryTypesRS.Add(unspecComplexEntryType);
								break;
							default:
								Console.WriteLine(@"Unknown RefType: " + refEntry.RefType);
								break;
						}
					}
				});
			}
		}

		#endregion IRecordChangeHandler implementation
	}
}
