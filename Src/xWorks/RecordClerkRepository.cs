// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Implementation of the IRecordClerkRepository.
	/// </summary>
	/// <remarks>
	/// 1. For now, the implementation will live in a static property of the RecordClerk class.
	///		Eventually, it may be added to the IFlexComponent InitializeFlexComponent method.
	/// 2. When the implementation is disposed, then so are all of its remaining clerks.
	/// </remarks>
	internal sealed class RecordClerkRepository : IRecordClerkRepositoryForTools
	{
		private readonly FlexComponentParameters _flexComponentParameters;
		private readonly LcmCache _cache;
		private readonly Dictionary<string, RecordClerk> _clerks = new Dictionary<string, RecordClerk>();
		private RecordClerk _activeRecordClerk;
		private IRecordClerkRepository AsRecordClerkRepository => this;

		internal RecordClerkRepository(LcmCache cache, FlexComponentParameters flexComponentParameters)
		{
			_cache = cache;
			_flexComponentParameters = flexComponentParameters;
		}

		#region Implementation of IRecordClerkRepository
		/// <summary>
		/// Add the <paramref name="recordClerk"/> to the repository.
		/// </summary>
		/// <param name="recordClerk">The clerk to add to the repository.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown if <paramref name="recordClerk"/> is null.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown if <paramref name="recordClerk"/> is already in the rpository, via its Id.
		/// </exception>
		void IRecordClerkRepository.AddRecordClerk(RecordClerk recordClerk)
		{
			if (recordClerk == null)
			{
				throw new ArgumentNullException(nameof(recordClerk));
			}
			if (_clerks.ContainsKey(recordClerk.Id))
			{
				throw new InvalidOperationException($"The clerk with an '{recordClerk.Id}' is already in the repository.");
			}
			_clerks.Add(recordClerk.Id, recordClerk);
		}

		/// <summary>
		/// Remove the <paramref name="recordClerk"/> from the repository.
		/// </summary>
		/// <param name="recordClerk">The clerk to remove from the repository.</param>
		/// <remarks>
		/// A side effect of removing a clerk, is that <see cref="IRecordClerkRepository.ActiveRecordClerk"/> is set to null,
		/// if it is the clerk being removed. The clerk being removed is also disposed.
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		/// Thrown if <paramref name="recordClerk"/> is null.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the clerk's Id is in the repository,
		/// but the one to be removed is not the one in the repository.
		/// </exception>
		void IRecordClerkRepository.RemoveRecordClerk(RecordClerk recordClerk)
		{
			if (recordClerk == null)
			{
				throw new ArgumentNullException(nameof(recordClerk));
			}

			RecordClerk goner;
			if (_clerks.TryGetValue(recordClerk.Id, out goner))
			{
				if (!ReferenceEquals(recordClerk, goner))
				{
					// Hmm. An imposter in our midst.
					throw new InvalidOperationException($"The two clerks have the same Id '{recordClerk.Id}', but they are not the same identical clerk.");
				}
				if (AsRecordClerkRepository.ActiveRecordClerk == recordClerk)
				{
					AsRecordClerkRepository.ActiveRecordClerk = null;
				}
				_clerks.Remove(recordClerk.Id);
				recordClerk.Dispose();
			}
		}

		/// <summary>
		/// Get the clerk with the given <paramref name="clerkId" />, or null if not found.
		/// </summary>
		/// <param name="clerkId">The Id of the clerk to return.</param>
		/// <returns>The clerk with the given <paramref name="clerkId"/>, or null if not found.</returns>
		RecordClerk IRecordClerkRepository.GetRecordClerk(string clerkId)
		{
			if (string.IsNullOrWhiteSpace(clerkId))
				throw new ArgumentNullException(nameof(clerkId));

			RecordClerk clerkToGet;
			_clerks.TryGetValue(clerkId, out clerkToGet);

			return clerkToGet;
		}

		/// <summary>
		/// Get/Set the active clerk. Null is an acceptable value for both 'get' and 'set'.
		/// </summary>
		RecordClerk IRecordClerkRepository.ActiveRecordClerk
		{
			set
			{
				_activeRecordClerk?.BecomeInactive();
				_activeRecordClerk = value;
#if RANDYTODO
				// TODO: Remove those parameters, when a clerk isn't really in charge of what the two do.
				// TODO: For now, we will pretend the clerk doesn't deal with it as of now.
#endif
				_activeRecordClerk?.ActivateUI(false, false);
			}
			get
			{
				return _activeRecordClerk;
			}
		}

		/// <summary>
		/// Get a clerk with the given <paramref name="clerkId"/>, creating one, if needed using <paramref name="clerkFactoryMethod"/>.
		/// </summary>
		/// <param name="clerkId">The clerl Id to return.</param>
		/// <param name="clerkFactoryMethod">The method called to create the clerk, if not found in the repository.</param>
		/// <returns>A RecordClerk instance with the specified Id.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="clerkFactoryMethod"/> doesn't know how to make a clerk with the given Id.</exception>
		RecordClerk IRecordClerkRepositoryForTools.GetRecordClerk(string clerkId, Func<LcmCache, FlexComponentParameters, string, RecordClerk> clerkFactoryMethod)
		{
			RecordClerk retVal;
			if (_clerks.TryGetValue(clerkId, out retVal))
			{
				return retVal;
			}

			retVal = clerkFactoryMethod(_cache, _flexComponentParameters, clerkId);
			retVal.InitializeFlexComponent(_flexComponentParameters);
			AsRecordClerkRepository.AddRecordClerk(retVal);

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
		~RecordClerkRepository()
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
				foreach (var clerk in _clerks.Values)
				{
					clerk.Dispose();
				}
				_clerks.Clear();
			}

			_isDisposed = true;
		}
		#endregion
	}
}