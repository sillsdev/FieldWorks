using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;
using System.Collections.Generic;
using Palaso.WritingSystems;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.CoreImpl
{
	#region PalasoWritingSystemManager class
	/// <summary>
	/// A Palaso-based writing system manager.
	/// </summary>
	public class PalasoWritingSystemManager : IWritingSystemManager
	{
		private IFwWritingSystemStore m_localStore;
		private IFwWritingSystemStore m_globalStore;
		private readonly Dictionary<int, PalasoWritingSystem> m_handleWss = new Dictionary<int, PalasoWritingSystem>();

		private PalasoWritingSystem m_userWritingSystem;
		private int m_nextHandle = 999000001;

		private readonly object m_syncRoot = new object();

		/// <summary>
		/// The folder in which the manager looks for template LDML files when a writing system is wanted
		/// that cannot be found in either the local or global store.
		/// </summary>
		public string TemplateFolder { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PalasoWritingSystemManager"/> class.
		/// </summary>
		public PalasoWritingSystemManager()
		{
			GlobalWritingSystemStore = new MemoryWritingSystemStore();
			LocalWritingSystemStore = new LocalMemoryWritingSystemStore(m_globalStore);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PalasoWritingSystemManager"/> class.
		/// </summary>
		/// <param name="store">The store.</param>
		public PalasoWritingSystemManager(IFwWritingSystemStore store) : this(store, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PalasoWritingSystemManager"/> class.
		/// </summary>
		/// <param name="localStore">The local store.</param>
		/// <param name="globalStore">The global store.</param>
		/// <remarks>localStore and globalStore will be disposed by the PalasoWritingSystemManager!</remarks>
		public PalasoWritingSystemManager(IFwWritingSystemStore localStore, IFwWritingSystemStore globalStore)
		{
			GlobalWritingSystemStore = globalStore;
			LocalWritingSystemStore = localStore;
		}

		/// <summary>
		/// Gets or sets the global writing system store.
		/// </summary>
		/// <value>The global writing system store.</value>
		public IFwWritingSystemStore GlobalWritingSystemStore
		{
			get
			{
				lock (m_syncRoot)
					return m_globalStore;
			}

			set
			{
				lock (m_syncRoot)
					m_globalStore = value;
			}
		}

		/// <summary>
		/// Gets or sets the local writing system store.
		/// </summary>
		/// <value>The local writing system store.</value>
		public IFwWritingSystemStore LocalWritingSystemStore
		{
			get
			{
				lock (m_syncRoot)
					return m_localStore;
			}

			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				lock (m_syncRoot)
				{
					if (m_localStore != value)
					{
						m_localStore = value;
						m_handleWss.Clear();
						foreach (PalasoWritingSystem ws in m_localStore.AllWritingSystems)
						{
							ws.WritingSystemManager = this;
							ws.Handle = m_nextHandle++;
							m_handleWss[ws.Handle] = ws;
						}
					}
				}
			}
		}


		/// <summary>
		/// Gets the global writing systems.
		/// </summary>
		/// <value>The global writing systems.</value>
		public IEnumerable<IWritingSystem> GlobalWritingSystems
		{
			get
			{
				lock (m_syncRoot)
				{
					if (m_globalStore != null)
						return m_globalStore.AllWritingSystems.Cast<IWritingSystem>();
				}
				return Enumerable.Empty<IWritingSystem>();
			}
		}

		/// <summary>
		/// Gets the local writing systems.
		/// </summary>
		/// <value>The local writing systems.</value>
		public IEnumerable<IWritingSystem> LocalWritingSystems
		{
			get
			{
				lock (m_syncRoot)
					return m_localStore.AllWritingSystems.Cast<IWritingSystem>();
			}
		}

		/// <summary>
		/// Gets all newer shared writing systems.
		/// </summary>
		/// <value>The newer shared writing systems.</value>
		public IEnumerable<IWritingSystem> CheckForNewerGlobalWritingSystems()
		{
			lock (m_syncRoot)
			{
				if (m_globalStore != null)
				{
					var results = new List<IWritingSystem>();
					foreach (WritingSystemDefinition wsDef in m_localStore.WritingSystemsNewerIn(m_globalStore.AllWritingSystems))
					{
						m_localStore.LastChecked(wsDef.Id, wsDef.DateModified);
						results.Add((IWritingSystem)wsDef);
					}
					return results;
				}
			}
			return Enumerable.Empty<IWritingSystem>();
		}

		/// <summary>
		/// Creates a new writing system.
		/// </summary>
		/// <returns></returns>
		public IWritingSystem Create(string identifier)
		{
			lock (m_syncRoot)
			{
				if (m_globalStore != null)
				{
					WritingSystemDefinition globalWs;
					if (m_globalStore.TryGet(identifier, out globalWs))
						return (PalasoWritingSystem) m_globalStore.MakeDuplicate(globalWs);
				}
			}

			LanguageSubtag languageSubtag;
			ScriptSubtag scriptSubtag;
			RegionSubtag regionSubtag;
			VariantSubtag variantSubtag;
			if (!LangTagUtils.GetSubtags(identifier, out languageSubtag, out scriptSubtag, out regionSubtag, out variantSubtag))
				throw new ArgumentException(identifier + " is not a valid RFC5646 language tag.");
			var result = Create(languageSubtag, scriptSubtag, regionSubtag, variantSubtag);
			if (TemplateFolder != null)
			{
				// try in our master template file
				// Todo: have property TemplateFolderPath, initialize in FdoBackendProvider.InitializeWritingSystemManager
				var template = Path.Combine(TemplateFolder, Path.ChangeExtension(identifier, "ldml"));
				if (File.Exists(template))
				{
					var loader = new FwLdmlAdaptor();
					loader.Read(template, (WritingSystemDefinition)result);
				}
			}
			return result;
		}

		/// <summary>
		/// Creates a new writing system.
		/// </summary>
		/// <param name="languageSubtag">The language subtag.</param>
		/// <param name="scriptSubtag">The script subtag.</param>
		/// <param name="regionSubtag">The region subtag.</param>
		/// <param name="variantSubtag">The variant subtag.</param>
		/// <returns></returns>
		public IWritingSystem Create(LanguageSubtag languageSubtag, ScriptSubtag scriptSubtag, RegionSubtag regionSubtag, VariantSubtag variantSubtag)
		{
			if (!languageSubtag.IsValid)
				throw new ArgumentException("Can not create a new writing system with an invalid language tag.");
			PalasoWritingSystem ws;
			lock (m_syncRoot)
				ws = (PalasoWritingSystem)m_localStore.CreateNew();

			ws.LanguageSubtag = languageSubtag;
			ws.ScriptSubtag = scriptSubtag;
			ws.RegionSubtag = regionSubtag;
			ws.VariantSubtag = variantSubtag;
			if (!string.IsNullOrEmpty(languageSubtag.Name))
				ws.Abbreviation = languageSubtag.Name.Length > 3 ? languageSubtag.Name.Substring(0, 3) : languageSubtag.Name;
			else
				ws.Abbreviation = ws.Id;

			CultureInfo ci = MiscUtils.GetCultureForWs(ws.Id);
			if (ci != null)
			{
				ws.SortUsing = WritingSystemDefinition.SortRulesType.OtherLanguage;
				ws.SortRules = ci.Name;
			}

			ws.Modified = false;
			return ws;
		}

		/// <summary>
		/// Creates a copy of the specified writing system.
		/// </summary>
		/// <param name="ws">The writing system.</param>
		/// <returns></returns>
		public IWritingSystem CreateFrom(IWritingSystem ws)
		{
			lock (m_syncRoot)
				return (PalasoWritingSystem)m_localStore.MakeDuplicate((PalasoWritingSystem)ws);
		}

		/// <summary>
		/// Determines if a writing system exists with the specified handle.
		/// </summary>
		/// <param name="handle">The handle.</param>
		/// <returns></returns>
		public bool Exists(int handle)
		{
			lock (m_syncRoot)
				return m_handleWss.ContainsKey(handle);
		}

		/// <summary>
		/// Determines if a writing system exists with the specified RFC5646 identifier.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <returns></returns>
		public bool Exists(string identifier)
		{
			lock (m_syncRoot)
			{
				var fExists = m_localStore.Contains(identifier);
				if (!fExists)
				{
					if (identifier.StartsWith("cmn"))
					{
						var ident = identifier.Remove(0, 3).Insert(0, "zh");
						fExists = m_localStore.Contains(ident);
						if (!fExists && identifier.StartsWith("cmn"))
						{
							ident = ident.Insert(2, "-CN");
							fExists = m_localStore.Contains(ident);
						}
					}
					else if (identifier.StartsWith("zh"))
					{
						var ident = identifier.Insert(2, "-CN");
						fExists = m_localStore.Contains(ident);
					}
					else if (identifier.StartsWith("pes"))
					{
						var ident = identifier.Remove(0, 3).Insert(0, "fa");
						fExists = m_localStore.Contains(ident);
					}
					else if (identifier.StartsWith("zlm"))
					{
						var ident = identifier.Remove(0, 3).Insert(0, "ms");
						fExists = m_localStore.Contains(ident);
					}
					else if (identifier.StartsWith("arb"))
					{
						var ident = identifier.Remove(2, 1); // changes to "ar"
						fExists = m_localStore.Contains(ident);
					}
				}
				return fExists;
			}
		}

		/// <summary>
		/// Gets the writing system with the specified handle.
		/// </summary>
		/// <param name="handle">The handle.</param>
		/// <returns></returns>
		public IWritingSystem Get(int handle)
		{
			lock (m_syncRoot)
			{
				PalasoWritingSystem ws;
				if (!m_handleWss.TryGetValue(handle, out ws))
					throw new ArgumentOutOfRangeException("handle");
				return ws;
			}
		}

		/// <summary>
		/// Gets the specified writing system. Throws KeyNotFoundException if it can not be found,
		/// there is a TryGet available to avoid this.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <returns></returns>
		public IWritingSystem Get(string identifier)
		{
			lock (m_syncRoot)
			{
				WritingSystemDefinition wrsys;
				if (!m_localStore.TryGet(identifier, out wrsys))
				{
					if (identifier.StartsWith("cmn"))
					{
						var ident = identifier.Remove(0, 3).Insert(0, "zh");
						if (!m_localStore.TryGet(ident, out wrsys) && identifier.StartsWith("cmn"))
						{
							ident = ident.Insert(2, "-CN");
							m_localStore.TryGet(ident, out wrsys);
						}
					}
					else if (identifier.StartsWith("zh"))
					{
						var ident = identifier.Insert(2, "-CN");
						m_localStore.TryGet(ident, out wrsys);
					}
					else if (identifier.StartsWith("pes"))
					{
						var ident = identifier.Remove(0, 3).Insert(0, "fa");
						m_localStore.TryGet(ident, out wrsys);
					}
					else if (identifier.StartsWith("zlm"))
					{
						var ident = identifier.Remove(0, 3).Insert(0, "ms");
						m_localStore.TryGet(ident, out wrsys);
					}
					else if (identifier.StartsWith("arb"))
					{
						var ident = identifier.Remove(2, 1); // changes to "ar"
						m_localStore.TryGet(ident, out wrsys);
					}
					if (wrsys == null) //if all other special cases did not apply or work
					{
						//throw the expected exception for Get
						throw new KeyNotFoundException("The writing system " + identifier + " was not found in this manager.");
					}
				}
				return (IWritingSystem)wrsys;
			}
		}

		/// <summary>
		/// Gets the specified writing system if it exists.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <param name="ws">The writing system.</param>
		/// <returns></returns>
		public bool TryGet(string identifier, out IWritingSystem ws)
		{
			lock (m_syncRoot)
			{
				if (Exists(identifier))
				{
					ws = Get(identifier);
					return true;
				}
				ws = null;
				return false;
			}
		}

		/// <summary>
		/// Gets the specified writing system if it exists, otherwise it creates
		/// a writing system using the specified identifier and sets it.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <param name="ws">The writing system.</param>
		/// <returns><c>true</c> if identifier is valid (and created);
		/// <c>false</c> otherwise</returns>
		public bool TryGetOrSet(string identifier, out IWritingSystem ws)
		{
			lock (m_syncRoot)
			{
				ws = null;
				//if (LangTagUtils.IsScriptCodeValid(identifier))
				if (Exists(identifier) || (m_globalStore != null && m_globalStore.Contains(identifier)))
				{
					GetOrSet(identifier, out ws);
					if (ws != null)
						return true;
				}

				return false;
			}
		}

		/// <summary>
		/// Sets the specified writing system.
		/// </summary>
		/// <param name="ws">The writing system.</param>
		public void Set(IWritingSystem ws)
		{
			var palasoWs = (PalasoWritingSystem)ws;
			lock (m_syncRoot)
			{
				m_localStore.Set(palasoWs);
				palasoWs.WritingSystemManager = this;
				palasoWs.Handle = m_nextHandle++;
				m_handleWss[palasoWs.Handle] = palasoWs;
			}
		}

		/// <summary>
		/// Creates a writing system using the specified identifier and sets it.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <returns></returns>
		public IWritingSystem Set(string identifier)
		{
			bool dummy;
			return Set(identifier, out dummy);
		}

		/// <summary>
		/// Create the writing system. Typically we will create it, but we may have to modify the ID and then find
		/// that there is an existing one. Set foundExisting true if so.
		/// </summary>
		/// <param name="identifier"></param>
		/// <param name="foundExisting"></param>
		/// <returns></returns>
		private IWritingSystem Set(string identifier, out bool foundExisting)
		{
			lock (m_syncRoot)
			{
				foundExisting = false;
				IWritingSystem ws = Create(identifier);
				// Pathologically, the ws that Create chooses to create may not have the exact expected ID.
				// For example, and id of x-kal will produce a new WS with Id qaa-x-kal.
				// In such a case, we may already have a WS with the corrected ID. Set will then fail.
				// So, in such a case, return the already-known WS.
				if (identifier != ws.Id)
				{
					IWritingSystem wsExisting;
					if (TryGet(ws.Id, out wsExisting))
					{
						foundExisting = true;
						return wsExisting;
					}
				}
				Set(ws);
				return ws;
			}
		}

		/// <summary>
		/// Gets the specified writing system if it exists, otherwise it creates
		/// a writing system using the specified identifier and sets it.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <param name="ws">The writing system.</param>
		/// <returns><c>true</c> if the writing system already existed, otherwise <c>false</c></returns>
		public bool GetOrSet(string identifier, out IWritingSystem ws)
		{
			lock (m_syncRoot)
			{
				if (TryGet(identifier, out ws))
					return true;
				bool foundExisting;
				ws = Set(identifier, out foundExisting);
				return foundExisting;
			}
		}

		/// <summary>
		/// Replaces an existing writing system with the specified writing system if they
		/// have the same identifier.
		/// </summary>
		/// <param name="ws">The writing system.</param>
		public void Replace(IWritingSystem ws)
		{
			var palasoWs = (PalasoWritingSystem)ws;
			lock (m_syncRoot)
			{
				IWritingSystem existingWs;
				if (TryGet(palasoWs.Id, out existingWs))
				{
					if (existingWs == palasoWs)
						// don't do anything
						return;

					m_handleWss.Remove(existingWs.Handle);
					m_localStore.Remove(existingWs.Id);
					m_localStore.Set(palasoWs);
					palasoWs.WritingSystemManager = this;
					palasoWs.Handle = existingWs.Handle;
					m_handleWss[palasoWs.Handle] = palasoWs;
				}
				else
				{
					Set(ws);
				}
			}
		}

		/// <summary>
		/// Gets or sets the user writing system.
		/// </summary>
		/// <value>The user writing system.</value>
		public IWritingSystem UserWritingSystem
		{
			get
			{
				lock (m_syncRoot)
				{
					if (m_userWritingSystem == null)
					{
						IWritingSystem ws;
						if (TryGet(Thread.CurrentThread.CurrentUICulture.Name, out ws))
						{
							m_userWritingSystem = (PalasoWritingSystem)ws;
						}
						else
						{
							GetOrSet("en", out ws);
							m_userWritingSystem = (PalasoWritingSystem)ws;
						}
					}
					return m_userWritingSystem;
				}
			}

			set
			{
				lock (m_syncRoot)
				{
					if (!Exists(value.Id))
						Set(value);
					m_userWritingSystem = (PalasoWritingSystem)value;
				}
			}
		}

		/// <summary>
		/// Persists all modified writing systems.
		/// </summary>
		public void Save()
		{
			lock (m_syncRoot)
			{
				DateTime now = DateTime.UtcNow;
				foreach (PalasoWritingSystem ws in m_localStore.AllWritingSystems)
				{
					if (ws.Modified || ws.DateModified.Ticks == 0)
						ws.DateModified = now;

					if (ws.MarkedForDeletion)
					{
						m_handleWss.Remove(ws.Handle);
						if (m_userWritingSystem == ws)
							m_userWritingSystem = null;
					}
				}
				m_localStore.Save();
			}
		}

		/// <summary>
		/// Return true if we expect (absent pathological changes while we're not looking) to be able to save changes
		/// to this writing system.
		/// </summary>
		public bool CanSave(IWritingSystem ws, out string path)
		{
			// JohnT: I don't know why the global store has to be able to save, but the check was there
			// so I left it. However, it needs to be guarded because m_globalStore might not exist.
			return m_localStore.CanSave((WritingSystemDefinition)ws, out path) &&
				(m_globalStore == null || m_globalStore.CanSave((WritingSystemDefinition)ws, out path));
		}

		/// <summary>
		/// Set the path for the local store (needed for project renaming).
		/// </summary>
		public string LocalStoreFolder
		{
			set
			{
				if (m_localStore is LdmlInFolderWritingSystemRepository)
					((LdmlInFolderWritingSystemRepository)m_localStore).PathToWritingSystems = value;
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
			StringBuilder builder = new StringBuilder(nameD.ToLowerInvariant());
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
			if (LangTagUtils.IsLanguageCodeValid(isoCode) && !LangTagInUse("qaa-x-" + isoCode))
				return isoCode; // The generated code is valid and not in use by the local or global store

			// We failed to generate a valid, unused language tag from the language name so
			// find one that isn't taken starting with 'aaa' and incrementing ('aab', 'aac', etc.)
			builder.Remove(0, builder.Length); // Clear the builder
			builder.Append("aaa");
			while (LangTagInUse("qaa-x-" + builder))
			{
				char newCharLast = (char)(builder[2] + 1);
				if (newCharLast > 'z')
				{
					// Incremented the last letter too far so reset it back to 'a' and increment the middle letter
					newCharLast = 'a';
					char newCharMiddle = (char)(builder[1] + 1);
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
			lock (m_syncRoot)
				return m_localStore.Contains(identifier) || (m_globalStore != null && m_globalStore.Contains(identifier));
		}

		#region Implementation of ILgWritingSystemFactory

		/// <summary>
		/// Gets the user writing system's HVO.
		/// </summary>
		/// <value>The user writing system's HVO.</value>
		public int UserWs
		{
			get { return UserWritingSystem.Handle; }

			set
			{
				lock (m_syncRoot)
					UserWritingSystem = Get(value);
			}
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
			IWritingSystem ws;
			GetOrSet(bstrIdentifier, out ws);
			return ws;
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
			lock (m_syncRoot)
			{
				if (!m_handleWss.ContainsKey(ws))
					return null;
				return Get(ws);
			}
		}

		/// <summary>
		/// Gets the HVO from the RFC5646 identifier.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <returns></returns>
		public int GetWsFromStr(string identifier)
		{
			IWritingSystem ws;
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
			lock (m_syncRoot)
			{
				PalasoWritingSystem ws;
				if (m_handleWss.TryGetValue(handle, out ws))
					return ws.Id;
				return null;
			}
		}

		/// <summary>
		/// Get the number of writing systems currently installed in the system
		/// </summary>
		/// <value></value>
		/// <returns>A System.Int32 </returns>
		public int NumberOfWs
		{
			get
			{
				lock (m_syncRoot)
					return m_localStore.Count;
			}
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
			foreach (IWritingSystem ws in LocalWritingSystems)
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
	#endregion

	#region IFwWritingSystemStore interface
	/// <summary>
	///
	/// </summary>
	public interface IFwWritingSystemStore : IWritingSystemRepository
	{
		/// <summary>
		/// Gets the specified writing system if it exists.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <param name="ws">The writing system.</param>
		/// <returns></returns>
		bool TryGet(string identifier, out WritingSystemDefinition ws);

		/// <summary>
		/// True if it is capable of saving changes to the specified WS.
		/// </summary>
		bool CanSave(WritingSystemDefinition ws, out string path);
	}
	#endregion

	#region LocalFileWritingSystemStore class
	/// <summary>
	/// A memory-based writing system store.
	/// </summary>
	public class MemoryWritingSystemStore : WritingSystemRepositoryBase, IFwWritingSystemStore
	{
		/// <summary>
		/// Use the default repository
		/// </summary>
		public MemoryWritingSystemStore() :
			base(WritingSystemCompatibility.Strict)
		{
		}

		/// <summary>
		/// Creates a new writing system definition.
		/// </summary>
		/// <returns></returns>
		public override WritingSystemDefinition CreateNew()
		{
			return new PalasoWritingSystem();
		}

		/// <summary>
		/// Creates an LDML adaptor.
		/// </summary>
		/// <returns></returns>
		protected override LdmlDataMapper CreateLdmlAdaptor()
		{
			return new FwLdmlAdaptor();
		}

		/// <summary>
		/// This is used by the orphan finder, which we don't use (yet). It tells whether, typically in the scope of some
		/// current change log, a writing system ID has changed to something else...call WritingSystemIdHasChangedTo
		/// to find out what.
		/// </summary>
		public override bool WritingSystemIdHasChanged(string id)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// This is used by the orphan finder, which we don't use (yet). It tells what, typically in the scope of some
		/// current change log, a writing system ID has changed to.
		/// </summary>
		public override string WritingSystemIdHasChangedTo(string id)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Saves this instance.
		/// </summary>
		public override void Save()
		{
			List<string> idsToRemove = (from ws in AllWritingSystems
				where ws.MarkedForDeletion
				select ws.Id).ToList();
			foreach (string id in idsToRemove)
				Remove(id);

			List<WritingSystemDefinition> allDefs = (from ws in AllWritingSystems
				where CanSet(ws)
				select ws).ToList();
			foreach (WritingSystemDefinition ws in allDefs)
			{
				Set(ws);
				ws.Modified = false;
				OnChangeNotifySharedStore(ws);
			}
		}

		/// <summary>
		/// Review JohnT: is there ever a case where Save isn't possible for this store?
		/// </summary>
		public bool CanSave(WritingSystemDefinition ws, out string path)
		{
			path = "";
			return true;
		}

		/// <summary>
		/// Gets the specified writing system if it exists.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <param name="ws">The writing system.</param>
		/// <returns></returns>
		public bool TryGet(string identifier, out WritingSystemDefinition ws)
		{
			if (Contains(identifier))
			{
				ws = Get(identifier);
				return true;
			}

			ws = null;
			return false;
		}
	}
	#endregion

	#region LocalMemoryWritingSystemStore class
	/// <summary>
	/// A memory-based local writing system store.
	/// </summary>
	public class LocalMemoryWritingSystemStore : MemoryWritingSystemStore
	{
		private readonly IFwWritingSystemStore m_globalStore;

		/// <summary>
		/// Initializes a new instance of the <see cref="LocalMemoryWritingSystemStore"/> class.
		/// </summary>
		/// <param name="globalStore">The global store.</param>
		public LocalMemoryWritingSystemStore(IFwWritingSystemStore globalStore)
		{
			m_globalStore = globalStore;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="ws">The ws.</param>
		protected override void OnChangeNotifySharedStore(WritingSystemDefinition ws)
		{
			base.OnChangeNotifySharedStore(ws);

			if (m_globalStore != null)
			{
				if (m_globalStore.Contains(ws.Id))
				{
					if (ws.DateModified > m_globalStore.Get(ws.Id).DateModified)
					{
						WritingSystemDefinition newWs = ws.Clone();
						newWs.Modified = true;
						m_globalStore.Remove(ws.Id);
						m_globalStore.Set(newWs);
					}
				}

				else
				{
					m_globalStore.Set(ws.Clone());
				}
			}
		}

		/// <summary>
		/// Saves this instance.
		/// </summary>
		public override void Save()
		{
			base.Save();
			if (m_globalStore != null)
				m_globalStore.Save();
		}
	}
	#endregion

	#region FwLdmlAdaptor class
	/// <summary>
	/// A FieldWorks LDML adaptor.
	/// </summary>
	public class FwLdmlAdaptor : LdmlDataMapper
	{
		/// <summary>
		/// Adds the namespaces.
		/// </summary>
		/// <param name="m">The m.</param>
		protected override void AddNamespaces(XmlNamespaceManager m)
		{
			base.AddNamespaces(m);
			m.AddNamespace("fw", "urn://fieldworks.sil.org/ldmlExtensions/v1");
		}

		/// <summary>
		/// Reads the top level special element.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <param name="ws">The ws.</param>
		protected override void ReadTopLevelSpecialElement(XmlReader reader, WritingSystemDefinition ws)
		{
			if (reader.GetAttribute("xmlns:fw") != null)
			{
				reader.ReadStartElement("special");
				var fwWs = (PalasoWritingSystem)ws;
				fwWs.DefaultFontFeatures = GetSpecialValue(reader, "fw", "defaultFontFeatures");
				bool graphiteEnabled;
				if (bool.TryParse(GetSpecialValue(reader, "fw", "graphiteEnabled"), out graphiteEnabled))
					fwWs.IsGraphiteEnabled = graphiteEnabled;
				fwWs.LegacyMapping = GetSpecialValue(reader, "fw", "legacyMapping");
				fwWs.MatchedPairs = GetSpecialValue(reader, "fw", "matchedPairs");
				fwWs.PunctuationPatterns = GetSpecialValue(reader, "fw", "punctuationPatterns");
				fwWs.QuotationMarks = GetSpecialValue(reader, "fw", "quotationMarks");
				fwWs.RegionName = GetSpecialValue(reader, "fw", "regionName");
				fwWs.ScriptName = GetSpecialValue(reader, "fw", "scriptName");
				fwWs.ValidChars = GetSpecialValue(reader, "fw", "validChars");
				fwWs.VariantName = GetSpecialValue(reader, "fw", "variantName");
				if (fwWs.VariantName == null)
				{
					var variant = fwWs.VariantSubtag;
					if (variant != null)
					{
						var variantName = variant.ToString();
						if (variantName.StartsWith("x-", StringComparison.OrdinalIgnoreCase))
							variantName = variantName.Substring(2);
						fwWs.VariantName = variantName;
					}
				}
				if (fwWs.RegionName == null)
				{
					var region = fwWs.RegionSubtag;
					if (region != null)
					{
						var regionName = region.ToString();
						if (regionName.StartsWith("x-", StringComparison.OrdinalIgnoreCase))
							regionName = regionName.Substring(2);
						fwWs.RegionName = regionName;
					}
				}
				if (fwWs.ScriptName == null)
				{
					var script = fwWs.ScriptSubtag;
					if (script != null)
					{
						var scriptName = script.ToString();
						if (scriptName.StartsWith("x-", StringComparison.OrdinalIgnoreCase))
							scriptName = scriptName.Substring(2);
						fwWs.ScriptName = scriptName;
					}
				}
				int lcid;
				if (int.TryParse(GetSpecialValue(reader, "fw", "windowsLCID"), out lcid))
					fwWs.LCID = lcid;

				while (reader.NodeType != XmlNodeType.EndElement)
					reader.Read();
				reader.ReadEndElement();
			}

			else
			{
				base.ReadTopLevelSpecialElement(reader, ws);
			}
		}

		/// <summary>
		/// Writes the top level special elements.
		/// </summary>
		protected override void WriteTopLevelSpecialElements(XmlWriter writer, XmlReader reader, WritingSystemDefinition ws)
		{
			base.WriteTopLevelSpecialElements(writer, reader, ws);
			var fwWs = (PalasoWritingSystem)ws;
			WriteBeginSpecialElement(writer, "fw");
			WriteSpecialValue(writer, "fw", "defaultFontFeatures", fwWs.DefaultFontFeatures);
			WriteSpecialValue(writer, "fw", "graphiteEnabled", fwWs.IsGraphiteEnabled.ToString());
			WriteSpecialValue(writer, "fw", "legacyMapping", fwWs.LegacyMapping);
			WriteSpecialValue(writer, "fw", "matchedPairs", fwWs.MatchedPairs);
			WriteSpecialValue(writer, "fw", "punctuationPatterns", fwWs.PunctuationPatterns);
			WriteSpecialValue(writer, "fw", "quotationMarks", fwWs.QuotationMarks);
			WriteSpecialValue(writer, "fw", "regionName", fwWs.RegionName);
			WriteSpecialValue(writer, "fw", "scriptName", fwWs.ScriptName);
			WriteSpecialValue(writer, "fw", "validChars", fwWs.ValidChars);
			WriteSpecialValue(writer, "fw", "variantName", fwWs.VariantName);
			if (fwWs.LCID != 0)
				WriteSpecialValue(writer, "fw", "windowsLCID", fwWs.LCID.ToString());
			writer.WriteEndElement();
		}
	}
	#endregion
}
