using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.WritingSystems;

namespace SIL.CoreImpl
{
	/// <summary>
	/// The writing system manager.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_renderEngines is a singleton and gets disposed by SingletonsContainer")]
	public class WritingSystemManager : ILgWritingSystemFactory
	{
		#region DisposableRenderEngineWrapper class
		private class DisposableRenderEngineWrapper : IComponent
		{
			public IRenderEngine RenderEngine { get; private set;}

			public DisposableRenderEngineWrapper(IRenderEngine renderEngine)
			{
				RenderEngine = renderEngine;
			}

			#region Disposable stuff
			#if DEBUG
			/// <summary/>
			~DisposableRenderEngineWrapper()
			{
				Dispose(false);
			}
			#endif

			/// <summary/>
			public bool IsDisposed { get; private set; }

			/// <summary/>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary/>
			protected virtual void Dispose(bool fDisposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + ". *******");
				if (fDisposing && !IsDisposed)
				{
					// dispose managed and unmanaged objects

					// The render engines hold a reference to the PalasoWritingSystemManager.
					// Things work better if we call release (FWNX-837).
					if (Marshal.IsComObject(RenderEngine))
						Marshal.ReleaseComObject(RenderEngine);
					RenderEngine = null;

					if (Disposed != null)
						Disposed(this, EventArgs.Empty);
				}
				IsDisposed = true;
			}
			#endregion

			#region IComponent implementation

			public event EventHandler Disposed;

			public ISite Site { get; set; }

			#endregion
		}
		#endregion

		private IWritingSystemRepository<CoreWritingSystemDefinition> m_repo;
		private readonly Dictionary<int, CoreWritingSystemDefinition> m_handleWSs = new Dictionary<int, CoreWritingSystemDefinition>();
		// List of render engines that get created during our lifetime.
		private readonly Container m_renderEngines = SingletonsContainer.Get<Container>("RenderEngineContainer");

		private CoreWritingSystemDefinition m_userWritingSystem;
		private int m_nextHandle = 999000001;

		private readonly object m_syncRoot = new object();

		/// <summary>
		/// Initializes a new instance of the <see cref="WritingSystemManager"/> class.
		/// </summary>
		public WritingSystemManager()
		{
			WritingSystemStore = new MemoryWritingSystemRepository(new MemoryWritingSystemRepository());
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WritingSystemManager"/> class.
		/// </summary>
		public WritingSystemManager(IWritingSystemRepository<CoreWritingSystemDefinition> wsRepo)
		{
			WritingSystemStore = wsRepo;
		}

		/// <summary>
		/// Registers a render engine. This should be called after creating a new render engine.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="DisposableRenderEngineWrapper object gets added to m_renderEngines singleton and disposed there.")]
		internal void RegisterRenderEngine(IRenderEngine engine)
		{
			m_renderEngines.Add(new DisposableRenderEngineWrapper(engine));
		}

		/// <summary>
		/// Gets or sets the local writing system store.
		/// </summary>
		/// <value>The local writing system store.</value>
		public IWritingSystemRepository<CoreWritingSystemDefinition> WritingSystemStore
		{
			get { return m_repo; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (m_repo != value)
				{
					m_repo = value;
					m_handleWSs.Clear();
					foreach (CoreWritingSystemDefinition ws in m_repo.AllWritingSystems)
					{
						ws.WritingSystemManager = this;
						ws.Handle = m_nextHandle++;
						m_handleWSs[ws.Handle] = ws;
					}
				}
			}
		}


		/// <summary>
		/// Gets a list of other writing systems. These are typically the global writing systems.
		/// </summary>
		public IEnumerable<CoreWritingSystemDefinition> OtherWritingSystems
		{
			get
			{
			var localRepo = m_repo as ILocalWritingSystemRepository<CoreWritingSystemDefinition>;
			return localRepo != null ? localRepo.GlobalWritingSystemRepository.AllWritingSystems : Enumerable.Empty<CoreWritingSystemDefinition>();
			}
		}

		/// <summary>
		/// Gets a list of writing systems.
		/// </summary>
		public IEnumerable<CoreWritingSystemDefinition> WritingSystems
		{
			get { return m_repo.AllWritingSystems; }
		}

		/// <summary>
		/// Gets all newer shared writing systems.
		/// </summary>
		/// <value>The newer shared writing systems.</value>
		public IEnumerable<CoreWritingSystemDefinition> CheckForNewerGlobalWritingSystems()
		{
			var localRepo = m_repo as ILocalWritingSystemRepository<CoreWritingSystemDefinition>;
			return localRepo != null ? localRepo.CheckForNewerGlobalWritingSystems() : Enumerable.Empty<CoreWritingSystemDefinition>();
		}

		/// <summary>
		/// Creates a new writing system.
		/// </summary>
		/// <returns></returns>
		public CoreWritingSystemDefinition Create(string ietfLanguageTag)
		{
			LanguageSubtag language;
			ScriptSubtag script;
			RegionSubtag region;
			IEnumerable<VariantSubtag> variants;
			if (!IetfLanguageTagHelper.TryGetSubtags(ietfLanguageTag, out language, out script, out region, out variants))
				throw new ArgumentException("The IETF language tag is invalid.", "ietfLanguageTag");
			return Create(language, script, region, variants);
		}

		/// <summary>
		/// Creates a new writing system.
		/// </summary>
		/// <param name="languageSubtag">The language subtag.</param>
		/// <param name="scriptSubtag">The script subtag.</param>
		/// <param name="regionSubtag">The region subtag.</param>
		/// <param name="variantSubtags">The variant subtags.</param>
		/// <returns></returns>
		public CoreWritingSystemDefinition Create(LanguageSubtag languageSubtag, ScriptSubtag scriptSubtag, RegionSubtag regionSubtag, IEnumerable<VariantSubtag> variantSubtags)
		{
			VariantSubtag[] variantSubtagsArray = variantSubtags.ToArray();
			string langTag = IetfLanguageTagHelper.ToIetfLanguageTag(languageSubtag, scriptSubtag, regionSubtag, variantSubtagsArray);
			CoreWritingSystemDefinition ws = m_repo.WritingSystemFactory.Create(langTag);
			ws.Language = languageSubtag;
			ws.Script = scriptSubtag;
			ws.Region = regionSubtag;
			ws.Variants.ReplaceAll(variantSubtagsArray);
			if (ws.Language != null && !string.IsNullOrEmpty(ws.Language.Name))
				ws.Abbreviation = ws.Language.Name.Length > 3 ? ws.Language.Name.Substring(0, 3) : ws.Language.Name;
			else
				ws.Abbreviation = ws.IetfLanguageTag;

			if (ws.DefaultCollation == null)
				ws.DefaultCollation = new IcuCollationDefinition("standard");
			if (ws.DefaultFont == null)
				ws.DefaultFont = new FontDefinition("Charis SIL");

			ws.AcceptChanges();
			return ws;
		}

		/// <summary>
		/// Creates a copy of the specified writing system.
		/// </summary>
		/// <param name="ws">The writing system.</param>
		/// <returns></returns>
		public CoreWritingSystemDefinition CreateFrom(CoreWritingSystemDefinition ws)
		{
			return new CoreWritingSystemDefinition(ws);
		}

		/// <summary>
		/// Determines if a writing system exists with the specified handle.
		/// </summary>
		/// <param name="handle">The handle.</param>
		/// <returns></returns>
		public bool Exists(int handle)
		{
			return m_handleWSs.ContainsKey(handle);
		}

		/// <summary>
		/// Determines if a writing system exists with the specified RFC5646 identifier.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <returns></returns>
		public bool Exists(string identifier)
		{
			bool fExists = m_repo.Contains(identifier);
			if (!fExists)
			{
				if (identifier.StartsWith("cmn"))
				{
					string ident = identifier.Remove(0, 3).Insert(0, "zh");
					fExists = m_repo.Contains(ident);
					if (!fExists && identifier.StartsWith("cmn"))
					{
						ident = ident.Insert(2, "-CN");
						fExists = m_repo.Contains(ident);
					}
				}
				else if (identifier.StartsWith("zh"))
				{
					string ident = identifier.Insert(2, "-CN");
					fExists = m_repo.Contains(ident);
				}
				else if (identifier.StartsWith("pes"))
				{
					string ident = identifier.Remove(0, 3).Insert(0, "fa");
					fExists = m_repo.Contains(ident);
				}
				else if (identifier.StartsWith("zlm"))
				{
					string ident = identifier.Remove(0, 3).Insert(0, "ms");
					fExists = m_repo.Contains(ident);
				}
				else if (identifier.StartsWith("arb"))
				{
					string ident = identifier.Remove(2, 1); // changes to "ar"
					fExists = m_repo.Contains(ident);
				}
			}
			return fExists;
		}

		/// <summary>
		/// Gets the writing system with the specified handle.
		/// </summary>
		/// <param name="handle">The handle.</param>
		/// <returns></returns>
		public CoreWritingSystemDefinition Get(int handle)
		{
			CoreWritingSystemDefinition ws;
			if (!m_handleWSs.TryGetValue(handle, out ws))
				throw new ArgumentOutOfRangeException("handle");
			return ws;
		}

		/// <summary>
		/// Gets the specified writing system. Throws KeyNotFoundException if it can not be found,
		/// there is a TryGet available to avoid this.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <returns></returns>
		public CoreWritingSystemDefinition Get(string identifier)
		{
			WritingSystemDefinition wrsys;
			if (!m_repo.TryGet(identifier, out wrsys))
			{
				if (identifier.StartsWith("cmn"))
				{
					string ident = identifier.Remove(0, 3).Insert(0, "zh");
					if (!m_repo.TryGet(ident, out wrsys) && identifier.StartsWith("cmn"))
					{
						ident = ident.Insert(2, "-CN");
						m_repo.TryGet(ident, out wrsys);
					}
				}
				else if (identifier.StartsWith("zh"))
				{
					string ident = identifier.Insert(2, "-CN");
					m_repo.TryGet(ident, out wrsys);
				}
				else if (identifier.StartsWith("pes"))
				{
					string ident = identifier.Remove(0, 3).Insert(0, "fa");
					m_repo.TryGet(ident, out wrsys);
				}
				else if (identifier.StartsWith("zlm"))
				{
					string ident = identifier.Remove(0, 3).Insert(0, "ms");
					m_repo.TryGet(ident, out wrsys);
				}
				else if (identifier.StartsWith("arb"))
				{
					string ident = identifier.Remove(2, 1); // changes to "ar"
					m_repo.TryGet(ident, out wrsys);
				}
				if (wrsys == null) //if all other special cases did not apply or work
				{
					//throw the expected exception for Get
					throw new KeyNotFoundException("The writing system " + identifier + " was not found in this manager.");
				}
			}
			return (CoreWritingSystemDefinition) wrsys;
		}

		/// <summary>
		/// Gets the specified writing system if it exists.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <param name="ws">The writing system.</param>
		/// <returns></returns>
		public bool TryGet(string identifier, out CoreWritingSystemDefinition ws)
		{
			if (Exists(identifier))
			{
				ws = Get(identifier);
				return true;
			}
			ws = null;
			return false;
		}

		/// <summary>
		/// Sets the specified writing system.
		/// </summary>
		/// <param name="ws">The writing system.</param>
		public void Set(CoreWritingSystemDefinition ws)
		{
			m_repo.Set(ws);
			ws.WritingSystemManager = this;
			ws.Handle = m_nextHandle++;
			m_handleWSs[ws.Handle] = ws;
		}

		/// <summary>
		/// Creates a writing system using the specified identifier and sets it.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <returns></returns>
		public CoreWritingSystemDefinition Set(string identifier)
		{
			CoreWritingSystemDefinition ws;
			Set(identifier, out ws);
			return ws;
		}

		/// <summary>
		/// Create the writing system. Typically we will create it, but we may have to modify the ID and then find
		/// that there is an existing one. Set foundExisting true if so.
		/// </summary>
		/// <param name="identifier"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		private bool Set(string identifier, out CoreWritingSystemDefinition ws)
		{
			ws = Create(identifier);
			// Pathologically, the ws that Create chooses to create may not have the exact expected ID.
			// For example, and id of x-kal will produce a new WS with Id qaa-x-kal.
			// In such a case, we may already have a WS with the corrected ID. Set will then fail.
			// So, in such a case, return the already-known WS.
			if (identifier != ws.IetfLanguageTag)
			{
				CoreWritingSystemDefinition wsExisting;
				if (TryGet(ws.IetfLanguageTag, out wsExisting))
				{
					ws = wsExisting;
					return true;
				}
			}
			Set(ws);
			return false;
		}

		/// <summary>
		/// Gets the specified writing system if it exists, otherwise it creates
		/// a writing system using the specified identifier and sets it.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <param name="ws">The writing system.</param>
		/// <returns><c>true</c> if the writing system already existed, otherwise <c>false</c></returns>
		public bool GetOrSet(string identifier, out CoreWritingSystemDefinition ws)
		{
			if (TryGet(identifier, out ws))
				return true;
			bool foundExisting;
			return Set(identifier, out ws);
		}

		/// <summary>
		/// Replaces an existing writing system with the specified new writing system if they
		/// have the same identifier.
		/// </summary>
		/// <param name="ws">The writing system.</param>
		public void Replace(CoreWritingSystemDefinition ws)
		{
			CoreWritingSystemDefinition existingWs;
			if (TryGet(ws.IetfLanguageTag, out existingWs))
			{
				if (existingWs == ws)
					// don't do anything
					return;

				m_handleWSs.Remove(existingWs.Handle);
				m_repo.Remove(existingWs.Id);
				m_repo.Set(ws);
				ws.WritingSystemManager = this;
				ws.Handle = existingWs.Handle;
				m_handleWSs[ws.Handle] = ws;
			}
			else
			{
				Set(ws);
			}
		}

		/// <summary>
		/// Gets or sets the user writing system.
		/// </summary>
		/// <value>The user writing system.</value>
		public CoreWritingSystemDefinition UserWritingSystem
		{
			get
			{
				lock (m_syncRoot)
				{
					if (m_userWritingSystem == null)
					{
						CoreWritingSystemDefinition ws;
						if (TryGet(Thread.CurrentThread.CurrentUICulture.Name, out ws))
						{
							m_userWritingSystem = ws;
						}
						else
						{
							GetOrSet("en", out ws);
							m_userWritingSystem = ws;
						}
					}
					return m_userWritingSystem;
				}
			}

			set
			{
				if (!Exists(value.Id))
					Set(value);
				m_userWritingSystem = value;
			}
		}

		/// <summary>
		/// Persists all modified writing systems.
		/// </summary>
		public void Save()
		{
			foreach (CoreWritingSystemDefinition ws in m_repo.AllWritingSystems)
			{
				if (ws.MarkedForDeletion)
				{
					m_handleWSs.Remove(ws.Handle);
					if (m_userWritingSystem == ws)
						m_userWritingSystem = null;
				}
			}
			m_repo.Save();
		}

		/// <summary>
		/// Return true if we expect (absent pathological changes while we're not looking) to be able to save changes
		/// to this writing system.
		/// </summary>
		public bool CanSave(CoreWritingSystemDefinition ws)
		{
			return m_repo.CanSave(ws);
		}

		/// <summary>
		/// Gets the LDML file path of the specified writing system.
		/// </summary>
		public string GetLdmlFilePath(CoreWritingSystemDefinition ws)
		{
			var localFileRepo = m_repo as CoreLdmlInFolderWritingSystemRepository;
			if (localFileRepo != null)
			{
				if (localFileRepo.Contains(ws.Id))
					return localFileRepo.GetFilePathFromIetfLanguageTag(ws.IetfLanguageTag);
				if (localFileRepo.GlobalWritingSystemRepository.Contains(ws.IetfLanguageTag))
					return localFileRepo.GlobalWritingSystemRepository.GetFilePathFromIetfLanguageTag(ws.IetfLanguageTag);
			}
			return string.Empty;
		}

		/// <summary>
		/// Set the path for the local store (needed for project renaming).
		/// </summary>
		public string LocalStoreFolder
		{
			set
			{
				var localFileRepo = m_repo as CoreLdmlInFolderWritingSystemRepository;
				if (localFileRepo != null)
					localFileRepo.PathToWritingSystems = value;
			}
		}

		/// <summary>
		/// The folder in which the manager looks for template LDML files when a writing system is wanted
		/// that cannot be found in either the local or global store.
		/// </summary>
		public string TemplateFolder
		{
			set
			{
				var localFileFactory = m_repo.WritingSystemFactory as CoreLdmlInFolderWritingSystemFactory;
				if (localFileFactory != null)
					localFileFactory.TemplateFolder = value;
			}
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
		public string GetValidLangTagForNewLang(string langName)
		{
			string nameD = langName.Normalize(NormalizationForm.FormD); // Get the name in NFD format
			var builder = new StringBuilder(nameD.ToLowerInvariant());
			int index = 0;
			while (index < builder.Length)
			{
				char c = builder[index];
				bool charValid = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
				if (!charValid)
				{
					// Found an invalid character, so remove it.
					builder.Remove(index, 1);
					continue;
				}
				index++;
			}

			string isoCode = builder.ToString().Substring(0, Math.Min(3, builder.Length));
			if (IetfLanguageTagHelper.IsValidLanguageCode(isoCode) && !LangTagInUse("qaa-x-" + isoCode))
				return isoCode; // The generated code is valid and not in use by the local or global store

			// We failed to generate a valid, unused language tag from the language name so
			// find one that isn't taken starting with 'aaa' and incrementing ('aab', 'aac', etc.)
			builder.Remove(0, builder.Length); // Clear the builder
			builder.Append("aaa");
			while (LangTagInUse("qaa-x-" + builder))
			{
				var newCharLast = (char) (builder[2] + 1);
				if (newCharLast > 'z')
				{
					// Incremented the last letter too far so reset it back to 'a' and increment the middle letter
					newCharLast = 'a';
					var newCharMiddle = (char) (builder[1] + 1);
					if (newCharMiddle > 'z')
					{
						// Incremented the middle letter too far so reset it back to 'a' and increment the first letter
						// Assume we won't ever have more then 4096 (26^3) custom writing systems
						newCharMiddle = 'a';
						builder[0] = (char)(builder[0] + 1);
					}
					builder[1] = newCharMiddle;
				}
				builder[2] = newCharLast;
			}
			return builder.ToString();
		}

		/// <summary>
		/// Determines whether or not the specified language tag is in use by another writing system
		/// in either the local or global writing system store.
		/// </summary>
		/// <param name="identifier">The language tag to check.</param>
		private bool LangTagInUse(string identifier)
		{
			if (m_repo.Contains(identifier))
				return true;

			var localRepo = m_repo as ILocalWritingSystemRepository<CoreWritingSystemDefinition>;
			return localRepo != null && localRepo.GlobalWritingSystemRepository != null && localRepo.GlobalWritingSystemRepository.Contains(identifier);
		}

		/// <summary>
		/// Gets all distinct writing systems (local and global) from the writing system manager. Local writing systems
		/// take priority over global writing systems.
		/// </summary>
		public IEnumerable<CoreWritingSystemDefinition> AllDistinctWritingSystems
		{
			get { return WritingSystems.Concat(OtherWritingSystems.Except(WritingSystems, new WritingSystemLangTagEqualityComparer())); }
		}

		#region Implementation of ILgWritingSystemFactory

		/// <summary>
		/// Gets the user writing system's HVO.
		/// </summary>
		/// <value>The user writing system's HVO.</value>
		public int UserWs
		{
			get { return UserWritingSystem.Handle; }
			set { UserWritingSystem = Get(value); }
		}

		/// <summary>
		/// Get the actual writing system object for a given ICU Locale string.
		/// The current implementation returns any existing writing system for that ICU Locale,
		/// or creates one with default settings if one is not already known.
		/// (Use <c>get_EngineOrNull</c> to avoid automatic creation of a new engine.)
		/// </summary>
		/// <param name="bstrIdentifier">The identifier.</param>
		/// <returns></returns>
		public ILgWritingSystem get_Engine(string bstrIdentifier)
		{
			lock (m_syncRoot)
			{
				CoreWritingSystemDefinition ws;
				GetOrSet(bstrIdentifier, out ws);
				return ws;
			}
		}

		/// <summary>
		/// Get the actual writing system object for a given code, or returns NULL if one does
		/// not already exist.
		/// (Use <c>get_Engine</c> if you prefer to have an writing system created automatically if
		/// one does not already exist.)
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		public ILgWritingSystem get_EngineOrNull(int ws)
		{
			if (!m_handleWSs.ContainsKey(ws))
				return null;
			return Get(ws);
		}

		/// <summary>
		/// Gets the HVO from the RFC5646 identifier.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <returns></returns>
		public int GetWsFromStr(string identifier)
		{
			CoreWritingSystemDefinition ws;
			if (TryGet(identifier, out ws))
				return ws.Handle;
			return 0;
		}

		/// <summary>
		/// Gets the RFC5646 identifier from the handle.
		/// </summary>
		/// <param name="handle">The handle.</param>
		/// <returns></returns>
		public string GetStrFromWs(int handle)
		{
			CoreWritingSystemDefinition ws;
			if (m_handleWSs.TryGetValue(handle, out ws))
				return ws.Id;
			return null;
		}

		/// <summary>
		/// Get the number of writing systems currently installed in the system
		/// </summary>
		/// <value></value>
		/// <returns>A System.Int32 </returns>
		public int NumberOfWs
		{
			get { return m_repo.Count; }
		}

		/// <summary>
		/// Get the list of writing systems currrently installed in the system.
		/// </summary>
		/// <param name="rgws"></param>
		/// <param name="cws"></param>
		public void GetWritingSystems(ArrayPtr rgws, int cws)
		{
			var wss = new int[cws];
			int i = 0;
			foreach (CoreWritingSystemDefinition ws in WritingSystems)
			{
				if (i >= cws)
					break;

				wss[i] = ws.Handle;
				i++;
			}

			for (; i < cws; i++)
				wss[i] = 0;

			MarshalEx.ArrayToNative(rgws, cws, wss);
		}

		/// <summary>
		/// Get the char prop engine for a particular WS
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		public ILgCharacterPropertyEngine get_CharPropEngine(int ws)
		{
			return Get(ws).CharPropEngine;
		}

		/// <summary>
		/// Get the renderer for a particular WS
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="vg"></param>
		/// <returns></returns>
		public IRenderEngine get_Renderer(int ws, IVwGraphics vg)
		{
			return Get(ws).get_Renderer(vg);
		}

		/// <summary>
		/// Get the renderer for a particular Chrp
		/// </summary>
		public IRenderEngine get_RendererFromChrp(IVwGraphics vg, ref LgCharRenderProps chrp)
		{
			vg.SetupGraphics(ref chrp);
			return get_Renderer(chrp.ws, vg);
		}

		#endregion
	}
}
