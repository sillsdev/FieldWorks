using System.Collections.Generic;

using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.CoreImpl
{
	/// <summary>
	///
	/// </summary>
	public interface IWritingSystemManager : ILgWritingSystemFactory
	{
		/// <summary>
		/// Gets the global writing systems.
		/// </summary>
		/// <value>The global writing systems.</value>
		IEnumerable<IWritingSystem> GlobalWritingSystems
		{
			get;
		}

		/// <summary>
		/// Gets the local writing systems.
		/// </summary>
		/// <value>The writing systems.</value>
		IEnumerable<IWritingSystem> LocalWritingSystems
		{
			get;
		}

		/// <summary>
		/// Gets all newer global writing systems.
		/// </summary>
		/// <value>The newer shared writing systems.</value>
		IEnumerable<IWritingSystem> CheckForNewerGlobalWritingSystems();

		/// <summary>
		/// Creates a new writing system. If a writing system with the same identifier
		/// exists in the global store it will be used to set the default property values.
		/// </summary>
		/// <returns></returns>
		IWritingSystem Create(string identifier);

		/// <summary>
		/// Creates a new writing system.
		/// </summary>
		/// <param name="languageSubtag">The language subtag.</param>
		/// <param name="scriptSubtag">The script subtag.</param>
		/// <param name="regionSubtag">The region subtag.</param>
		/// <param name="variantSubtag">The variant subtag.</param>
		/// <returns></returns>
		IWritingSystem Create(LanguageSubtag languageSubtag, ScriptSubtag scriptSubtag, RegionSubtag regionSubtag, VariantSubtag variantSubtag);

		/// <summary>
		/// Creates a copy of the specified writing system.
		/// </summary>
		/// <param name="ws">The writing system.</param>
		/// <returns></returns>
		IWritingSystem CreateFrom(IWritingSystem ws);

		/// <summary>
		/// Determines if a writing system exists with the specified handle.
		/// </summary>
		/// <param name="handle">The writing system handle.</param>
		/// <returns></returns>
		bool Exists(int handle);

		/// <summary>
		/// Determines if a writing system exists with the specified writing system identifier.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <returns></returns>
		bool Exists(string identifier);

		/// <summary>
		/// Gets the writing system with the specified handle.
		/// </summary>
		/// <param name="handle">The handle.</param>
		/// <returns></returns>
		IWritingSystem Get(int handle);

		/// <summary>
		/// Gets the specified writing system.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <returns></returns>
		IWritingSystem Get(string identifier);

		/// <summary>
		/// Gets the specified writing system if it exists.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <param name="ws">The writing system.</param>
		/// <returns></returns>
		bool TryGet(string identifier, out IWritingSystem ws);

		/// <summary>
		/// Sets the specified writing system.
		/// </summary>
		/// <param name="ws">The writing system.</param>
		void Set(IWritingSystem ws);

		/// <summary>
		/// Creates a writing system using the specified identifier and sets it.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <returns></returns>
		IWritingSystem Set(string identifier);

		/// <summary>
		/// Gets the specified writing system if it exists, otherwise it creates
		/// a writing system using the specified identifier and sets it.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <param name="ws">The writing system.</param>
		/// <returns><c>true</c> if identifier is valid (and created);
		/// <c>false</c> otherwise</returns>
		bool TryGetOrSet(string identifier, out IWritingSystem ws);

		/// <summary>
		/// Gets the specified writing system if it exists, otherwise it creates
		/// a writing system using the specified identifier and sets it.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <param name="ws">The writing system.</param>
		/// <returns><c>true</c> if the writing system already existed, otherwise <c>false</c></returns>
		bool GetOrSet(string identifier, out IWritingSystem ws);

		/// <summary>
		/// Replaces an existing writing system with the specified writing system if they
		/// have the same identifier.
		/// </summary>
		/// <param name="ws">The writing system.</param>
		void Replace(IWritingSystem ws);

		/// <summary>
		/// Gets or sets the user writing system.
		/// </summary>
		/// <value>The user writing system.</value>
		IWritingSystem UserWritingSystem
		{
			get;
			set;
		}

		/// <summary>
		/// Persists all modified writing systems.
		/// </summary>
		void Save();

		/// <summary>
		/// Returns true if it is possible to save changes to the specified writing system.
		/// If not, indicate the path to the file we want to write.
		/// </summary>
		bool CanSave(IWritingSystem ws, out string path);

		/// <summary>
		/// Set the path for the local store (needed for project renaming).
		/// </summary>
		string LocalStoreFolder
		{
			set;
		}

		/// <summary>
		/// Returns an ISO 639 language tag that is guaranteed to be valid and unique for both the
		/// local and the global writing system store.
		/// NOTE: This method should only be used for writing systems that are custom (i.e. not
		/// defined in the current version of the ethnologue).
		/// The returned code will *not* have the 'x-' prefix denoting a user-defined writing system,
		/// but it will check that an existing user-defined writing system does not exist with
		/// the returned language tag.
		/// This method also does not worry about regions, variants, etc. as it's use is restricted to
		/// the language tag for a custom writing system.
		/// </summary>
		/// <param name="langName">The full name of the language.</param>
		string GetValidLangTagForNewLang(string langName);
	}
}
