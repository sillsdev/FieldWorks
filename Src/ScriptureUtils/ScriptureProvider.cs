// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.Common.ScriptureUtils
{
	/// <summary>
	/// This class is a wrapper for the different supported providers of scripture, either Paratext 7 or Paratext 8.1 at the moment.
	/// </summary>
	public class ScriptureProvider
	{
#pragma warning disable 0649 // [ImportMany] *is* the initialization
		[ImportMany]
		private IEnumerable<Lazy<IScriptureProvider, IScriptureProviderMetadata>> _potentialScriptureProviders;
#pragma warning restore 0649

		/// <summary>
		/// The selected IScriptureProvider to use
		/// </summary>
		internal static IScriptureProvider _scriptureProvider;

		private static bool _isInitialized;

		/// <summary>
		/// Determine if Paratext8 is installed, if it is use it, otherwise fall back to Paratext7
		/// </summary>
		static ScriptureProvider()
		{
			var scriptureProvider = new ScriptureProvider();
			var catalog = new AggregateCatalog();
			//Adds all the parts found in the same assembly as the ScriptureProvider class
			catalog.Catalogs.Add(new AssemblyCatalog(typeof(ScriptureProvider).Assembly));
			//Adds all the parts found in assemblies ending in Plugin.dll that reside in the FieldWorksExe path
			var extensionPath = Path.Combine(Path.GetDirectoryName(FwDirectoryFinder.FieldWorksExe));
			catalog.Catalogs.Add(new DirectoryCatalog(extensionPath, "*Plugin.dll"));
			//Create the CompositionContainer with the parts in the catalog
			var container = new CompositionContainer(catalog);
			container.SatisfyImportsOnce(scriptureProvider);

			// Choose the ScriptureProvider that reports the newest version
			// (If both Paratext 7 and 8 are installed, the plugin handling 8 will be used)
			foreach (var provider in scriptureProvider._potentialScriptureProviders)
			{
				if (_scriptureProvider == null || provider.Value.MaximumSupportedVersion > _scriptureProvider.MaximumSupportedVersion)
				{
					_scriptureProvider = provider.Value;
				}
			}
#if DEBUG
			if (_scriptureProvider == null)
			{
				throw new ApplicationException("No scripture providers discovered by MEF");
			}
#endif // DEBUG
			InitializeIfNeeded();
		}

		private static void InitializeIfNeeded()
		{
			if (!_isInitialized && IsInstalled)
			{
				Initialize();
				_isInitialized = true; // save ourselves a few milliseconds next time around (perhaps)
			}
		}

		/// <summary/>
		internal static Version VersionInUse { get { return _scriptureProvider.MaximumSupportedVersion; } }

		/// <returns>true if and only if Paratext is installed and its settings (projects) directory exists (e.g. USB drive plugged or unplugged</returns>
		public static bool IsInstalled { get { return _scriptureProvider != null && _scriptureProvider.IsInstalled; } }

		/// <summary/>
		public interface IScriptureProviderMetadata
		{
			/// <summary/>
			string Version { get; }
		}

		/// <summary/>
		public interface IScriptureProvider
		{
			/// <summary/>
			string SettingsDirectory { get; }
			/// <summary/>
			IEnumerable<string> NonEditableTexts { get; }
			/// <summary/>
			IEnumerable<string> ScrTextNames { get; }
			/// <summary/>
			void Initialize();
			/// <summary/>
			void RefreshScrTexts();
			/// <summary/>
			IEnumerable<IScrText> ScrTexts();
			/// <summary/>
			IVerseRef MakeVerseRef(int bookNum, int i, int i1);
			/// <summary/>
			IScrText Get(string project);
			/// <summary/>
			IScrText MakeScrText(string paratextProjectId);
			/// <summary/>
			IScriptureProviderParserState GetParserState(IScrText ptProjectText, IVerseRef ptCurrBook);
			/// <summary>
			/// The version number of the most recently installed Paratext supported by this provider
			/// </summary>
			/// <remarks>This should return only an installed version</remarks>
			Version MaximumSupportedVersion { get; }
			/// <summary/>
			bool IsInstalled { get; }
		}

		/// <summary/>
		public static IScrText MakeScrText()
		{
			return MakeScrText(string.Empty);
		}

		/// <summary/>
		public static IScrText MakeScrText(string paratextProjectId)
		{
			return _scriptureProvider.MakeScrText(paratextProjectId);
		}

		/// <summary/>
		public static string SettingsDirectory
		{
			get
			{
				InitializeIfNeeded();
				return _scriptureProvider.SettingsDirectory;
			}
		}

		/// <summary/>
		public static IEnumerable<string> NonWorkingTexts
		{
			get { return _scriptureProvider.NonEditableTexts; }
		}

		/// <summary/>
		public static IEnumerable<string> ScrTextNames
		{
			get
			{
				return _scriptureProvider.ScrTextNames;
			}
		}

		/// <summary/>
		public static void Initialize()
		{
			// REVIEW (Hasso) 2017.07: is it our job or the client's not to initialize it's not OK?
			_scriptureProvider.Initialize();
		}

		/// <summary/>
		public static void RefreshScrTexts()
		{
			_scriptureProvider.RefreshScrTexts();
		}

		/// <summary/>
		public static IEnumerable<IScrText> ScrTexts()
		{
			return _scriptureProvider.ScrTexts();
		}

		/// <summary>
		/// Return a collection of paratext objects wrapped in an interface using the provided function to do it
		/// </summary>
		/// <param name="ptCollection">The collection of raw paratext objects</param>
		/// <param name="wrapFunction">The function that takes the paratext object and returns a wrapper using a FLEx interface</param>
		public static IEnumerable<T> WrapPtCollection<T, T2>(IEnumerable<T2> ptCollection, Func<T2, T> wrapFunction)
		{
			return ptCollection.Select(ptObj => wrapFunction.Invoke(ptObj)).ToList();
		}

		/// <summary/>
		public static IScrText Get(string project)
		{
			return _scriptureProvider.Get(project);
		}

		/// <summary/>
		public static IVerseRef MakeVerseRef(int bookNum, int i, int i1)
		{
			return _scriptureProvider.MakeVerseRef(bookNum, i, i1);
		}

		/// <summary/>
		public static IScriptureProviderParserState GetParserState(IScrText ptProjectText, IVerseRef ptCurrBook)
		{
			return _scriptureProvider.GetParserState(ptProjectText, ptCurrBook);
		}

		/// <summary/>
		public interface IScriptureProviderParserState
		{
			/// <summary/>
			IVerseRef VerseRef { get; }

			/// <summary/>
			void UpdateState(List<IUsfmToken> m_ptBookTokens, int m_ptCurrentToken);
		}
	}

	/// <summary/>
	public interface IScrVerse
	{
		/// <summary/>
		int LastChapter(int bookNum);

		/// <summary/>
		int LastVerse(int bookNum, int chapter);
	}
}