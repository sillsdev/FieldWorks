// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Works
{
	/// <summary>
	/// Implementation of the IRecordListRepository.
	/// </summary>
	/// <remarks>
	/// 1. For now, the implementation will live in a static property of the RecordList class.
	///		Eventually, it may be added to the IFlexComponent InitializeFlexComponent method.
	/// 2. When the implementation is disposed, then so are all of its remaining record lists.
	/// </remarks>
	internal sealed class RecordListRepository : IRecordListRepositoryForTools
	{
		private readonly FlexComponentParameters _flexComponentParameters;
		private readonly LcmCache _cache;
		private readonly Dictionary<string, IRecordList> _recordLists = new Dictionary<string, IRecordList>();
		private IRecordList _activeRecordList;
		private IRecordListRepository AsRecordListRepository => this;

		internal RecordListRepository(LcmCache cache, FlexComponentParameters flexComponentParameters)
		{
			_cache = cache;
			_flexComponentParameters = flexComponentParameters;
		}

		#region Implementation of IRecordListRepository
		/// <summary>
		/// Add the <paramref name="recordList"/> to the repository.
		/// </summary>
		/// <param name="recordList">The record list to add to the repository.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown if <paramref name="recordList"/> is null.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown if <paramref name="recordList"/> is already in the repository, via its Id.
		/// </exception>
		void IRecordListRepository.AddRecordList(IRecordList recordList)
		{
			if (recordList == null)
			{
				throw new ArgumentNullException(nameof(recordList));
			}
			if (_recordLists.ContainsKey(recordList.Id))
			{
				throw new InvalidOperationException($"The record list with an '{recordList.Id}' is already in the repository.");
			}
			_recordLists.Add(recordList.Id, recordList);
		}

		/// <summary>
		/// Remove the <paramref name="recordList"/> from the repository.
		/// </summary>
		/// <param name="recordList">The record list to remove from the repository.</param>
		/// <remarks>
		/// A side effect of removing a record list, is that <see cref="IRecordListRepository.ActiveRecordList"/> is set to null,
		/// if it is the record list being removed. The record list being removed is also disposed.
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		/// Thrown if <paramref name="recordList"/> is null.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the record list's Id is in the repository,
		/// but the one to be removed is not the one in the repository.
		/// </exception>
		void IRecordListRepository.RemoveRecordList(IRecordList recordList)
		{
			if (recordList == null)
			{
				throw new ArgumentNullException(nameof(recordList));
			}

			IRecordList goner;
			if (_recordLists.TryGetValue(recordList.Id, out goner))
			{
				if (!ReferenceEquals(recordList, goner))
				{
					// Hmm. An imposter in our midst.
					throw new InvalidOperationException($"The two record lists have the same Id '{recordList.Id}', but they are not the same identical record list.");
				}
				if (AsRecordListRepository.ActiveRecordList == recordList)
				{
					AsRecordListRepository.ActiveRecordList = null;
				}
				_recordLists.Remove(recordList.Id);
				recordList.Dispose();
			}
		}

		/// <summary>
		/// Get the record list with the given <paramref name="recordListId" />, or null if not found.
		/// </summary>
		/// <param name="recordListId">The Id of the record list to return.</param>
		/// <returns>The record list with the given <paramref name="recordListId"/>, or null if not found.</returns>
		IRecordList IRecordListRepository.GetRecordList(string recordListId)
		{
			if (string.IsNullOrWhiteSpace(recordListId))
				throw new ArgumentNullException(nameof(recordListId));

			IRecordList clerkToGet;
			_recordLists.TryGetValue(recordListId, out clerkToGet);

			return clerkToGet;
		}

		/// <summary>
		/// Get/Set the active record list. Null is an acceptable value for both 'get' and 'set'.
		/// </summary>
		IRecordList IRecordListRepository.ActiveRecordList
		{
			set
			{
				_activeRecordList?.BecomeInactive();
				_activeRecordList = value;
				_activeRecordList?.ActivateUI();
			}
			get
			{
				return _activeRecordList;
			}
		}

		/// <summary>
		/// Get a record list with the given <paramref name="recordListId"/>, creating one, if needed using <paramref name="recordListFactoryMethod"/>.
		/// </summary>
		/// <param name="recordListId">The record list Id to return.</param>
		/// <param name="statusBar"></param>
		/// <param name="recordListFactoryMethod">The method called to create the record list, if not found in the repository.</param>
		/// <returns>The record list instance with the specified Id.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="recordListFactoryMethod"/> doesn't know how to make a record list with the given Id.</exception>
		IRecordList IRecordListRepositoryForTools.GetRecordList(string recordListId, StatusBar statusBar, Func<LcmCache, FlexComponentParameters, string, StatusBar, IRecordList> recordListFactoryMethod)
		{
			IRecordList retVal;
			if (_recordLists.TryGetValue(recordListId, out retVal))
			{
				return retVal;
			}

			retVal = recordListFactoryMethod(_cache, _flexComponentParameters, recordListId, statusBar);
			retVal.InitializeFlexComponent(_flexComponentParameters);
			AsRecordListRepository.AddRecordList(retVal);

			return retVal;
		}

		/// <summary>
		/// Get a record list for a custom possibility list with the given <paramref name="recordListId"/>, creating one, if needed using <paramref name="recordListFactoryMethod"/>.
		/// </summary>
		/// <param name="recordListId">The record list Id to return.</param>
		/// <param name="statusBar"></param>
		/// <param name="customList">The user created possibility list.</param>
		/// <param name="recordListFactoryMethod">The method called to create the record list, if not found in the repository.</param>
		/// <returns>The record list instance with the specified Id.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="recordListFactoryMethod"/> doesn't know how to make a record list with the given Id.</exception>
		IRecordList IRecordListRepositoryForTools.GetRecordList(string recordListId, StatusBar statusBar, ICmPossibilityList customList, Func<ICmPossibilityList, LcmCache, FlexComponentParameters, string, StatusBar, IRecordList> recordListFactoryMethod)
		{
			IRecordList retVal;
			if (_recordLists.TryGetValue(recordListId, out retVal))
			{
				return retVal;
			}

			retVal = recordListFactoryMethod(customList, _cache, _flexComponentParameters, recordListId, statusBar);
			retVal.InitializeFlexComponent(_flexComponentParameters);
			retVal.InitLoad(true);
			AsRecordListRepository.AddRecordList(retVal);

			return retVal;
		}
		#endregion

		#region Implementation of IDisposable

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool _isDisposed;

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~RecordListRepository()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		void IDisposable.Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (_isDisposed)
				return;

			if (disposing)
			{
				foreach (var recordList in _recordLists.Values)
				{
					recordList.Dispose();
				}
				_recordLists.Clear();
			}

			_isDisposed = true;
		}
		#endregion
	}
}