<<<<<<< HEAD:Src/LanguageExplorer/DictionaryConfiguration/ConfiguredLcmGenerator.cs
// Copyright (c) 2014-2020 SIL International
||||||| f013144d5:Src/xWorks/ConfiguredLcmGenerator.cs
// Copyright (c) 2014-2019 SIL International
=======
// Copyright (c) 2014-2021 SIL International
>>>>>>> develop:Src/xWorks/ConfiguredLcmGenerator.cs
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
<<<<<<< HEAD:Src/LanguageExplorer/DictionaryConfiguration/ConfiguredLcmGenerator.cs
using System.Xml;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.Filters;
||||||| f013144d5:Src/xWorks/ConfiguredLcmGenerator.cs
using System.Xml;
=======
using System.Web.UI.WebControls;
using ExCSS;
>>>>>>> develop:Src/xWorks/ConfiguredLcmGenerator.cs
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;
using FileUtils = SIL.LCModel.Utils.FileUtils;
using UnitType = ExCSS.UnitType;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary>
	/// This class groups the static methods used for generating XHTML, according to specified configurations, from Fieldworks model objects
	/// </summary>
	internal static class ConfiguredLcmGenerator
	{
		/// <summary>
		/// Click-to-play icon for media files
		/// </summary>
		internal const string LoudSpeaker = "\uD83D\uDD0A";

		internal const string MovieCamera = "\U0001F3A5";

		/// <summary>
		/// Line-Separator Decimal Code
		/// </summary>
		private const char TxtLineSplit = (char)8232;


		// A sanity check regex. Verifies that we are looking at a potential start of a table
		private static readonly Regex USFMTableStart = new Regex(@"\A(\\d|\\tr)\s");

		/// <summary>
		/// The Assembly that the model Types should be loaded from. Allows test code to introduce a test model.
		/// </summary>
		internal static string AssemblyFile { get; set; }

		/// <summary>
		/// Map of the Assembly to the file name, so that different tests can use different models
		/// </summary>
		internal static Dictionary<string, Assembly> AssemblyMap = new Dictionary<string, Assembly>();

		internal const string LookupComplexEntryType = "LookupComplexEntryType";

		/// <summary>
		/// The number of entries to add to a page when the user asks to see 'a few more'
		/// </summary>
		/// <remarks>internal to facilitate unit tests</remarks>
		internal static int EntriesToAddCount { get; }

		private static Dictionary<ConfigurableDictionaryNode, PropertyType> _configNodeToTypeMap = new Dictionary<ConfigurableDictionaryNode, PropertyType>();

		/// <summary>
		/// Static initializer setting the AssemblyFile to the default LCM dll.
		/// </summary>
		static ConfiguredLcmGenerator()
		{
			Init();
			EntriesToAddCount = 5;
		}

<<<<<<< HEAD:Src/LanguageExplorer/DictionaryConfiguration/ConfiguredLcmGenerator.cs
		internal static bool IsNormalRtl(IReadonlyPropertyTable readOnlyPropertyTable)
||||||| f013144d5:Src/xWorks/ConfiguredLcmGenerator.cs
		internal static bool IsNormalRtl(ReadOnlyPropertyTable propertyTable)
=======
		/// <summary>
		/// Sets initial values (or resets them after tests)
		/// </summary>
		internal static void Init()
>>>>>>> develop:Src/xWorks/ConfiguredLcmGenerator.cs
		{
<<<<<<< HEAD:Src/LanguageExplorer/DictionaryConfiguration/ConfiguredLcmGenerator.cs
			// Right-to-Left for the overall layout is determined by Dictionary-Normal
			// Some tests don't have the expected style.
			var styleSheet = FwUtils.StyleSheetFromPropertyTable(readOnlyPropertyTable);
			if (styleSheet != null)
			{
				if (styleSheet.Styles.Contains("Dictionary-Normal"))
				{
					var normalStyle = styleSheet.Styles["Dictionary-Normal"];
					var dictionaryNormalStyle = new ExportStyleInfo(normalStyle);
					return dictionaryNormalStyle.DirectionIsRightToLeft == TriStateBool.triTrue; // default is LTR
				}
			}
			return true; // default is LTR
||||||| f013144d5:Src/xWorks/ConfiguredLcmGenerator.cs
			// Right-to-Left for the overall layout is determined by Dictionary-Normal
			var dictionaryNormalStyle = new ExportStyleInfo(FontHeightAdjuster.StyleSheetFromPropertyTable(propertyTable).Styles["Dictionary-Normal"]);
			return dictionaryNormalStyle.DirectionIsRightToLeft == TriStateBool.triTrue; // default is LTR
=======
			AssemblyFile = "SIL.LCModel";
		}

		internal static bool IsEntryStyleRtl(ReadOnlyPropertyTable propertyTable, DictionaryConfigurationModel model)
		{
			// Right-to-Left for the overall layout is determined by Dictionary-Normal - or the user selected style for Main Entry
			var mainEntryStyle = GetEntryStyle(model);
			var entryStyle = new ExportStyleInfo(FontHeightAdjuster.StyleSheetFromPropertyTable(propertyTable).Styles[mainEntryStyle]);
			return entryStyle.DirectionIsRightToLeft == TriStateBool.triTrue; // default is LTR
		}

		internal static string GetEntryStyle(DictionaryConfigurationModel model)
		{
			return model.Parts.FirstOrDefault(part => part.IsMainEntry)?.Style
				?? "Dictionary-Normal";
>>>>>>> develop:Src/xWorks/ConfiguredLcmGenerator.cs
		}

		private static bool IsCanceling(IThreadedProgress progress)
		{
			return progress != null && progress.IsCanceling;
		}

		/// <summary>
		/// This method uses a ThreadPool to execute the given individualActions in parallel.
		/// It waits for all the individualActions to complete and then returns.
		/// </summary>
		internal static void SpawnEntryGenerationThreadsAndWait(List<Action> individualActions, IThreadedProgress progress)
		{
			var actionCount = individualActions.Count;
			//Note that our COM classes all implement the STA threading model, while the ThreadPool always uses MTA model threads.
			//I don't understand why using the ThreadPool sometimes works, but not always.  Explicitly allocating STA model
			//threads as done here works in all the cases that have been tried.  (Windows/Linux, program/unit test)  Unfortunately,
			//the speedup on Linux is minimal.
			var maxThreadCount = Math.Min(16, (int)(Environment.ProcessorCount * 1.5));
			maxThreadCount = Math.Min(maxThreadCount, actionCount);
			Exception exceptionThrown = null;
			var threadActionArray = new Action[maxThreadCount];
			using (var countDown = new CountdownEvent(maxThreadCount))
			{
				// Note that the loop index variable startIndex cannot be used in an action defined as a closure.  So we have to define all the
				// possible closures explicitly to achieve the parallelism reliably.  (Remember your theoretical computer science lessons
				// about lambda expressions and the various ways that variables are bound.  For some of us, that's been over 40 years!)
				// ReSharper disable AccessToDisposedClosure Justification: threads are guaranteed to finish before countDown is disposed
				for (var startIndex = 0; startIndex < maxThreadCount; startIndex++)
				{
					// bind a copy of the current value of the loop index to the closure,
					// instead of depending on startIndex which will change
					var index = startIndex;
					threadActionArray[index] = () =>
					{
						try { for (var j = index; j < actionCount && !IsCanceling(progress); j += maxThreadCount) individualActions[j](); }
						catch (Exception e) { exceptionThrown = e; }
						finally { countDown.Signal(); }
					};
				}
				// ReSharper restore AccessToDisposedClosure
				var threads = new List<Thread>(maxThreadCount);
				for (var i = 0; i < maxThreadCount; ++i)
				{
					var x = new Thread(new ThreadStart(threadActionArray[i]));
					x.SetApartmentState(ApartmentState.STA);
					x.Start();
					threads.Add(x);     // ensure thread doesn't get garbage collected prematurely.
				}
				countDown.Wait();
				threads.Clear();
				// Throwing the exception out here avoids hanging up the Green screen AND the progress dialog.
				// The only downside is we see only one exception. See LT-17244.
				if (exceptionThrown != null)
				{
					throw new WorkerThreadException("Exception generating Configured XHTML", exceptionThrown);
				}
			}
		}

		/// <summary>
		/// Get the sort word that will be used to generate the letter headings. The sort word can come from a different
		/// field depending on the sort column.
		/// </summary>
		/// <returns>the sort word in NFD (the heading letter must be normalized to NFC before writing to XHTML, per LT-18177)</returns>
		internal static string GetSortWordForLetterHead(ICmObject entry, RecordClerk clerk)
		{
<<<<<<< HEAD:Src/LanguageExplorer/DictionaryConfiguration/ConfiguredLcmGenerator.cs
			return (entry as ILexEntry)?.HomographForm.TrimStart() ?? (entry as IReversalIndexEntry)?.ReversalForm.BestAnalysisAlternative.Text.TrimStart() ?? string.Empty;
||||||| f013144d5:Src/xWorks/ConfiguredLcmGenerator.cs
			var lexEntry = entry as ILexEntry;
			if (lexEntry == null)
			{
				var revEntry = entry as IReversalIndexEntry;
				return revEntry != null ? revEntry.ReversalForm.BestAnalysisAlternative.Text.TrimStart() : string.Empty;
			}
			return lexEntry.HomographForm.TrimStart();
=======
			var lexEntry = entry as ILexEntry;

			// Reversal Indexes - We are always using the same sorting, regardless of the sort column that
			// was selected.  So always return the same word for the letter head.
			if (lexEntry == null)
			{
				// When viewing the Reversal Indexes the sort column always comes back as "Form",
				// regardless of which column was selected for the sort. If for some cases this assumption changes
				// then we need to assess if those cases should be returning a different property for the sort word.
				if (clerk?.SortName != null)
				{
					Debug.Assert(clerk.SortName.StartsWith("Form"),
						"Should we be getting the letter headers from the sort column: " +
						clerk.SortName);
				}

				var revEntry = entry as IReversalIndexEntry;
				return revEntry != null ? revEntry.ReversalForm.BestAnalysisAlternative.Text.TrimStart() : string.Empty;
			}

			if (clerk?.SortName != null)
			{
				// Lexeme Form
				if (clerk.SortName.StartsWith("Lexeme Form"))
				{
					string retStr = lexEntry.LexemeFormOA?.Form?.VernacularDefaultWritingSystem?.Text?.TrimStart();
					return retStr != null ? retStr : string.Empty;
				}

				// Citation Form
				if (clerk.SortName.StartsWith("Citation Form"))
				{
					string retStr = lexEntry.CitationForm?.UserDefaultWritingSystem?.Text?.TrimStart();
					return (retStr != null && retStr != "***") ? retStr : string.Empty;
				}

				// If we get here and have a sort name other than "Headword" then it should have
				// it's own conditional check and use a different lexEntry field to get the sort word.
				Debug.Assert(clerk.SortName.StartsWith("Headword"),
					"We should be getting the letter headers from the sort column: " +
					clerk.SortName);
			}

			// Headword - Default to using the "Headword" sort word.
			return  lexEntry.HomographForm.TrimStart();
>>>>>>> develop:Src/xWorks/ConfiguredLcmGenerator.cs
		}

		/// <summary>
		/// Get the writing system string for a LexEntry or an IReversalIndexEntry
		/// </summary>
		internal static string GetWsForEntryType(ICmObject entry, LcmCache cache)
		{
			var wsString = cache.WritingSystemFactory.GetStrFromWs(cache.DefaultVernWs);
<<<<<<< HEAD:Src/LanguageExplorer/DictionaryConfiguration/ConfiguredLcmGenerator.cs
			if (entry is IReversalIndexEntry reversalIndexEntry)
			{
				wsString = reversalIndexEntry.SortKeyWs;
			}
||||||| f013144d5:Src/xWorks/ConfiguredLcmGenerator.cs
			if (entry is IReversalIndexEntry)
				wsString = ((IReversalIndexEntry)entry).SortKeyWs;
=======
			if (entry is IReversalIndexEntry revEntry)
				wsString = revEntry.SortKeyWs;
>>>>>>> develop:Src/xWorks/ConfiguredLcmGenerator.cs
			return wsString;
		}

		/// <summary>
		/// Generating the xhtml representation for the given ICmObject using the given configuration node to select which data to write out
		/// If it is a Dictionary Main Entry or non-Dictionary entry, uses the first configuration node.
		/// If it is a Minor Entry, first checks whether the entry should be published as a Minor Entry; then, generates XHTML for each applicable
		/// Minor Entry configuration node.
		/// </summary>
		public static string GenerateXHTMLForEntry(ICmObject entryObj, DictionaryConfigurationModel configuration, DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings, int index = -1)
		{
			if (IsMainEntry(entryObj, configuration))
			{
				return GenerateXHTMLForMainEntry(entryObj, configuration.Parts[0], publicationDecorator, settings, index);
			}
			var entry = (ILexEntry)entryObj;
			return entry.PublishAsMinorEntry ? GenerateXHTMLForMinorEntry(entry, configuration, publicationDecorator, settings, index) : string.Empty;
		}

		public static string GenerateXHTMLForMainEntry(ICmObject entry, ConfigurableDictionaryNode configuration, DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings, int index)
		{
			return configuration.DictionaryNodeOptions != null && ((ILexEntry)entry).ComplexFormEntryRefs.Any() && !IsListItemSelectedForExport(configuration, entry)
				? string.Empty : GenerateXHTMLForEntry(entry, configuration, publicationDecorator, settings, index);
		}

		private static string GenerateXHTMLForMinorEntry(ICmObject entry, DictionaryConfigurationModel configuration, DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings, int index)
		{
			// LT-15232: show minor entries using only the last applicable Minor Entry node (not more than once)
			var applicablePart = configuration.Parts.Skip(1).LastOrDefault(part => IsListItemSelectedForExport(part, entry));
			return applicablePart == null ? string.Empty : GenerateXHTMLForEntry(entry, applicablePart, publicationDecorator, settings, index);
		}

		/// <summary>
		/// If entry is a a Main Entry
		/// For Root-based configs, this means the entry is neither a Variant nor a Complex Form.
		/// For Lexeme-based configs, Complex Forms are considered Main Entries but Variants are not.
		/// </summary>
		internal static bool IsMainEntry(ICmObject entry, DictionaryConfigurationModel config)
		{
			return !(entry is ILexEntry lexEntry) /* only LexEntries can be Minor; others (ReversalIndex, etc) are always Main.*/
				   || !lexEntry.EntryRefsOS.Any() || !config.IsRootBased && lexEntry.EntryRefsOS.Any(ler => ler.RefType == LexEntryRefTags.krtComplexForm);
		}

		/// <summary>Generates XHTML for an ICmObject for a specific ConfigurableDictionaryNode</summary>
		/// <remarks>the configuration node must match the entry type</remarks>
		internal static string GenerateXHTMLForEntry(ICmObject entry, ConfigurableDictionaryNode configuration, DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings, int index = -1)
		{
			Guard.AgainstNull(entry, nameof(entry));
			Guard.AgainstNull(configuration, nameof(configuration));
			Guard.AgainstNull(settings, nameof(settings));

			// ReSharper disable LocalizableElement, because seriously, who cares about localized exceptions?
			if (string.IsNullOrEmpty(configuration.FieldDescription))
			{
				throw new ArgumentException("Invalid configuration: FieldDescription can not be null", nameof(configuration));
			}
			if (entry.ClassID != settings.Cache.MetaDataCacheAccessor.GetClassId(configuration.FieldDescription))
			{
				throw new ArgumentException("The given argument doesn't configure this type", nameof(configuration));
			}
			// ReSharper restore LocalizableElement
			if (!configuration.IsEnabled)
			{
				return string.Empty;
			}
			var pieces = configuration.ReferencedOrDirectChildren.Select(config => GenerateXHTMLForFieldByReflection(entry, config, publicationDecorator, settings))
				.Where(content => !string.IsNullOrEmpty(content)).ToList();
			if (pieces.Count == 0)
			{
				return string.Empty;
			}
			var bldr = new StringBuilder();
			using (var xw = settings.ContentGenerator.CreateWriter(bldr))
			{
				var clerk = settings.PropertyTable.GetValue<RecordClerk>("ActiveClerk", null);
				settings.ContentGenerator.StartEntry(xw, GetClassNameAttributeForConfig(configuration), entry.Guid, index, clerk);
				settings.ContentGenerator.AddEntryData(xw, pieces);
				settings.ContentGenerator.EndEntry(xw);
				xw.Flush();
				return CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFC).Normalize(bldr.ToString()); // All content should be in NFC (LT-18177)
			}
		}

		/// <summary>
		/// This method will write out the class name attribute into the xhtml for the given configuration node
		/// taking into account the current information in ClassNameOverrides
		/// </summary>
		/// <param name="configNode">used to look up any mapping overrides</param>
		public static string GetClassNameAttributeForConfig(ConfigurableDictionaryNode configNode)
		{
			var classAtt = CssGenerator.GetClassAttributeForConfig(configNode);
			if (configNode.ReferencedNode != null)
			{
				classAtt = $"{classAtt} {CssGenerator.GetClassAttributeForConfig(configNode.ReferencedNode)}";
			}
			return classAtt;
		}

		/// <summary>
		/// This method will use reflection to pull data out of the given object based on the given configuration and
		/// write out appropriate XHTML.
		/// </summary>
		/// <remarks>We use a significant amount of boilerplate code for fields and subfields. Make sure you update both.</remarks>
		internal static string GenerateXHTMLForFieldByReflection(object field, ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings, SenseInfo info = new SenseInfo(), bool fUseReverseSubField = false)
		{
			if (!config.IsEnabled)
			{
				return string.Empty;
			}
			var cache = settings.Cache;
			var entryType = field.GetType();
			object propertyValue = null;
			if (config.DictionaryNodeOptions is DictionaryNodeGroupingOptions)
			{
				return GenerateXHTMLForGroupingNode(field, config, publicationDecorator, settings);
			}
			if (config.FieldDescription == "DefinitionOrGloss")
			{
				switch (field)
				{
					case ILexSense lexSense:
						return GenerateXHTMLForDefinitionOrGloss(lexSense, config, settings);
					case ILexEntryRef lexEntryRef:
					{
						var ret = new StringBuilder();
						foreach (var sense in ((ILexEntry)lexEntryRef.Owner).AllSenses)
						{
							ret.Append(GenerateXHTMLForDefinitionOrGloss(sense, config, settings));
						}
						return ret.ToString();
					}
				}
			}
			if (config.IsCustomField && config.SubField == null)
			{
				var customFieldOwnerClassName = GetClassNameForCustomFieldParent(config, settings.Cache.GetManagedMetaDataCache());
				if (!GetPropValueForCustomField(field, config, cache, customFieldOwnerClassName, config.FieldDescription, ref propertyValue))
				{
					return string.Empty;
				}
			}
			else
			{
				var property = entryType.GetProperty(config.FieldDescription);
				if (property == null)
				{
#if DEBUG
					var msg = $"Issue with finding {config.FieldDescription} for {entryType}";
					ShowConfigDebugInfo(msg, config);
#endif
					return string.Empty;
				}
				propertyValue = property.GetValue(field, new object[] { });
				GetSortedReferencePropertyValue(config, ref propertyValue, field);
			}
			// If the property value is null there is nothing to generate
			if (propertyValue == null)
			{
				return string.Empty;
			}
			if (!string.IsNullOrEmpty(config.SubField))
			{
				if (config.IsCustomField)
				{
					// Get the custom field value (in SubField) using the property which came from the field object
					if (!GetPropValueForCustomField(propertyValue, config, cache, ((ICmObject)propertyValue).ClassName, config.SubField, ref propertyValue))
					{
						return string.Empty;
					}
				}
				else
				{
					var subType = propertyValue.GetType();
					var subField = fUseReverseSubField ? "Reverse" + config.SubField : config.SubField;
					var subProp = subType.GetProperty(subField);
					if (subProp == null)
					{
#if DEBUG
						ShowConfigDebugInfo($"Issue with finding (subField) {subField} for (subType) {subType}", config);
#endif
						return string.Empty;
					}
					propertyValue = subProp.GetValue(propertyValue, new object[] { });
					GetSortedReferencePropertyValue(config, ref propertyValue, field);
				}
				// If the property value is null there is nothing to generate
				if (propertyValue == null)
				{
					return string.Empty;
				}
			}
			ICmFile fileProperty;
			ICmObject fileOwner;
			var typeForNode = config.IsCustomField
				? GetPropertyTypeFromReflectedTypes(propertyValue.GetType(), null)
				: GetPropertyTypeForConfigurationNode(config, propertyValue.GetType(), cache.GetManagedMetaDataCache());
			switch (typeForNode)
			{
				case PropertyType.CollectionType:
					return !IsCollectionEmpty(propertyValue) ? GenerateXHTMLForCollection(propertyValue, config, publicationDecorator, field, settings, info) : string.Empty;
				case PropertyType.MoFormType:
					return GenerateXHTMLForMoForm(propertyValue as IMoForm, config, settings);
				case PropertyType.CmObjectType:
					return GenerateXHTMLForICmObject(propertyValue as ICmObject, config, settings);
				case PropertyType.CmPictureType:
					fileProperty = propertyValue as ICmFile;
					fileOwner = field as ICmObject;
					return fileProperty != null && fileOwner != null ? GenerateXHTMLForPicture(fileProperty, config, fileOwner, settings) : GenerateXHTMLForPictureCaption(propertyValue, config, settings);
				case PropertyType.CmPossibility:
					return GenerateXHTMLForPossibility(propertyValue, config, publicationDecorator, settings);
				case PropertyType.CmFileType:
					fileProperty = propertyValue as ICmFile;
					string internalPath = null;
					if (fileProperty?.InternalPath != null)
					{
						internalPath = fileProperty.InternalPath;
					}
					// fileProperty.InternalPath can have a backward slash so that gets replaced with a forward slash in Linux
					if (!Platform.IsWindows && !string.IsNullOrEmpty(internalPath))
					{
						internalPath = fileProperty.InternalPath.Replace('\\', '/');
					}
					if (fileProperty != null && !string.IsNullOrEmpty(internalPath))
					{
						var srcAttr = GenerateSrcAttributeForMediaFromFilePath(internalPath, "AudioVisual", settings);
<<<<<<< HEAD:Src/LanguageExplorer/DictionaryConfiguration/ConfiguredLcmGenerator.cs
						if (IsVideo(fileProperty.InternalPath))
						{
							return GenerateXHTMLForVideoFile(fileProperty.ClassName, srcAttr, MovieCamera);
						}
||||||| f013144d5:Src/xWorks/ConfiguredLcmGenerator.cs
						if (IsVideo(fileProperty.InternalPath))
							return GenerateXHTMLForVideoFile(fileProperty.ClassName, srcAttr, MovieCamera);
=======
>>>>>>> develop:Src/xWorks/ConfiguredLcmGenerator.cs
						fileOwner = field as ICmObject;
						// the XHTML id attribute must be unique. The owning ICmMedia has a unique guid.
						// The ICmFile is used for all references to the same file within the project, so its guid is not unique.
						if (fileOwner != null)
						{
							return IsVideo(fileProperty.InternalPath)
								? GenerateXHTMLForVideoFile(fileProperty.ClassName, fileOwner.Guid.ToString(), srcAttr, MovieCamera, settings)
								: GenerateXHTMLForAudioFile(fileProperty.ClassName, fileOwner.Guid.ToString(), srcAttr, LoudSpeaker, settings);
						}
					}
					return string.Empty;
			}
			var bldr = new StringBuilder(GenerateXHTMLForValue(field, propertyValue, config, settings));
			if (config.ReferencedOrDirectChildren != null)
			{
				foreach (var child in config.ReferencedOrDirectChildren)
				{
					bldr.Append(GenerateXHTMLForFieldByReflection(propertyValue, child, publicationDecorator, settings));
				}
			}
			return bldr.ToString();
		}

		private static string GenerateXHTMLForGroupingNode(object field, ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings)
		{
			if (config.ReferencedOrDirectChildren != null && config.ReferencedOrDirectChildren.Any(child => child.IsEnabled))
			{
				return settings.ContentGenerator.GenerateGroupingNode(field, config, publicationDecorator, settings,
					(f, c, p, s) => GenerateXHTMLForFieldByReflection(f, c, p, s));
			}
			return string.Empty;
		}

		/// <summary>
		/// Gets the value of the requested custom field associated with the fieldOwner object
		/// </summary>
		/// <returns>true if the custom field was valid and false otherwise</returns>
		/// <remarks>propertyValue can be null if the custom field is valid but no value is stored for the owning object</remarks>
		private static bool GetPropValueForCustomField(object fieldOwner, ConfigurableDictionaryNode config, LcmCache cache, string customFieldOwnerClassName, string customFieldName, ref object propertyValue)
		{
			var customFieldFlid = GetCustomFieldFlid(config, cache.GetManagedMetaDataCache(), customFieldOwnerClassName, customFieldName);
			if (customFieldFlid == 0)
			{
				return true;
			}
			var customFieldType = cache.MetaDataCacheAccessor.GetFieldType(customFieldFlid);
			ICmObject specificObject;
			if (fieldOwner is ISenseOrEntry senseOrEntry)
			{
				specificObject = senseOrEntry.Item;
				if (!cache.GetManagedMetaDataCache().GetFields(specificObject.ClassID, true, (int)CellarPropertyTypeFilter.All).Contains(customFieldFlid))
				{
					return false;
				}
			}
			else
			{
				specificObject = (ICmObject)fieldOwner;
			}
			switch (customFieldType)
			{
				case (int)CellarPropertyType.ReferenceCollection:
				case (int)CellarPropertyType.OwningCollection:
				// Collections are stored essentially the same as sequences.
				case (int)CellarPropertyType.ReferenceSequence:
				case (int)CellarPropertyType.OwningSequence:
					{
						var sda = cache.MainCacheAccessor;
						// This method returns the hvo of the object pointed to
						var chvo = sda.get_VecSize(specificObject.Hvo, customFieldFlid);
						int[] contents;
						using (var arrayPtr = MarshalEx.ArrayToNative<int>(chvo))
						{
							sda.VecProp(specificObject.Hvo, customFieldFlid, chvo, out chvo, arrayPtr);
							contents = MarshalEx.NativeToArray<int>(arrayPtr, chvo);
						}
						// if the hvo is invalid set propertyValue to null otherwise get the object
						propertyValue = contents.Select(id => cache.LangProject.Services.GetObject(id));
						break;
					}
				case (int)CellarPropertyType.ReferenceAtomic:
				case (int)CellarPropertyType.OwningAtomic:
					{
						// This method returns the hvo of the object pointed to
						propertyValue = cache.MainCacheAccessor.get_ObjectProp(specificObject.Hvo, customFieldFlid);
						// if the hvo is invalid set propertyValue to null otherwise get the object
						propertyValue = (int)propertyValue > 0 ? cache.LangProject.Services.GetObject((int)propertyValue) : null;
						break;
					}
				case (int)CellarPropertyType.GenDate:
					{
						propertyValue = new GenDate(cache.MainCacheAccessor.get_IntProp(specificObject.Hvo, customFieldFlid));
						break;
					}

				case (int)CellarPropertyType.Time:
					{
						propertyValue = SilTime.ConvertFromSilTime(cache.MainCacheAccessor.get_TimeProp(specificObject.Hvo, customFieldFlid));
						break;
					}
				case (int)CellarPropertyType.MultiUnicode:
				case (int)CellarPropertyType.MultiString:
					{
						propertyValue = cache.MainCacheAccessor.get_MultiStringProp(specificObject.Hvo, customFieldFlid);
						break;
					}
				case (int)CellarPropertyType.String:
					{
						propertyValue = cache.MainCacheAccessor.get_StringProp(specificObject.Hvo, customFieldFlid);
						break;
					}
				case (int)CellarPropertyType.Integer:
					{
						propertyValue = cache.MainCacheAccessor.get_IntProp(specificObject.Hvo, customFieldFlid);
						break;
					}
			}
			return true;
		}

		private static string GenerateXHTMLForVideoFile(string className, string mediaId, string srcAttribute, string caption, GeneratorSettings settings)
		{
<<<<<<< HEAD:Src/LanguageExplorer/DictionaryConfiguration/ConfiguredLcmGenerator.cs
			if (string.IsNullOrEmpty(srcAttribute) && string.IsNullOrEmpty(caption))
			{
				return string.Empty;
			}
			var bldr = new StringBuilder();
			using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
			{
				// This creates a link that will open the video in the same window as the dictionary view/preview
				// refreshing will bring it back to the dictionary
				xw.WriteStartElement("a");
				xw.WriteAttributeString("class", className);
				xw.WriteAttributeString("href", srcAttribute);
				if (!string.IsNullOrEmpty(caption))
				{
					xw.WriteString(caption);
				}
				else
				{
					xw.WriteRaw(string.Empty);
				}
				xw.WriteFullEndElement();
				xw.Flush();
				return bldr.ToString();
			}
||||||| f013144d5:Src/xWorks/ConfiguredLcmGenerator.cs
			if (String.IsNullOrEmpty(srcAttribute) && String.IsNullOrEmpty(caption))
				return String.Empty;
			var bldr = new StringBuilder();
			using (var xw = XmlWriter.Create(bldr, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment }))
			{
				// This creates a link that will open the video in the same window as the dictionary view/preview
				// refreshing will bring it back to the dictionary
				xw.WriteStartElement("a");
				xw.WriteAttributeString("class", className);
				xw.WriteAttributeString("href", srcAttribute);
				if (!String.IsNullOrEmpty(caption))
					xw.WriteString(caption);
				else
					xw.WriteRaw("");
				xw.WriteFullEndElement();
				xw.Flush();
				return bldr.ToString();
			}
=======
			if (string.IsNullOrEmpty(srcAttribute) && string.IsNullOrEmpty(caption))
				return string.Empty;
			// This creates a link that will open the video in the same window as the dictionary view/preview
			// refreshing will bring it back to the dictionary
			return settings.ContentGenerator.GenerateVideoLinkContent(className, GetSafeXHTMLId(mediaId), srcAttribute, caption);
>>>>>>> develop:Src/xWorks/ConfiguredLcmGenerator.cs
		}

		private static bool IsVideo(string fileName)
		{
			var extension = Path.GetExtension(fileName);
			switch (extension.ToLowerInvariant())
			{
				// any others we should detect?
				case ".mp4":
				case ".avi":
				case ".swf":
				case ".mov":
				case ".flv":
				case ".ogv":
				case ".3gp":
					return true;
			}
			return false;
		}

#if DEBUG
		private static HashSet<ConfigurableDictionaryNode> s_reportedNodes = new HashSet<ConfigurableDictionaryNode>();

		private static void ShowConfigDebugInfo(string msg, ConfigurableDictionaryNode config)
		{
			lock (s_reportedNodes)
			{
//				Debug.WriteLine(msg);
				if (s_reportedNodes.Contains(config))
				{
					return;
				}
				s_reportedNodes.Add(config);
				while (config != null)
				{
					Debug.WriteLine("    Label={0}, FieldDescription={1}, SubField={2}", config.Label, config.FieldDescription, config.SubField ?? "");
					config = config.Parent;
				}
			}
		}
#endif

		private static void GetSortedReferencePropertyValue(ConfigurableDictionaryNode config, ref object propertyValue, object parent)
		{
			var options = config.DictionaryNodeOptions as DictionaryNodeListOptions;
			var unsortedReferences = propertyValue as IEnumerable<ILexReference>;
			if (options == null || unsortedReferences == null || !unsortedReferences.Any())
			{
				return;
			}
			// Calculate and store the ids for each of the references once for efficiency.
			var refsAndIds = new List<Tuple<ILexReference, string>>();
			foreach (var reference in unsortedReferences)
			{
				var id = reference.OwnerType.Guid.ToString();
				if (LexRefTypeTags.IsAsymmetric((LexRefTypeTags.MappingTypes)reference.OwnerType.MappingType))
				{
					id = id + LexRefDirection(reference, parent);
				}
				refsAndIds.Add(new Tuple<ILexReference, string>(reference, id));
			}
			// LT-17384: LexReferences are not ordered (they are put in some order each time FLEx starts), but we want to have a consistent order each
			// time we export the dictionary (even after restarting FLEx), so we sort them here.
			// LT-15764 We're actually going to sort the ConfigTargets of the LexReference objects later, so what this really accomplishes
			// is sorting all the LexReferences of the same type together based on the DictionaryNodeListOptions.
			var sortedReferences = new List<ILexReference>();
			// REVIEW (Hasso) 2016.03: this Where is redundant to the IsListItemSelectedForExport call in GenerateCollectionItemContent
			// REVIEW (cont): Filtering here is more performant; the other filter can be removed if it is verifiably redundant.
			foreach (var option in options.Options.Where(optn => optn.IsEnabled))
			{
				foreach (var duple in refsAndIds.Where(duple => option.Id == duple.Item2 && !sortedReferences.Contains(duple.Item1)))
				{
					sortedReferences.Add(duple.Item1);
				}
			}
			propertyValue = sortedReferences;
		}

		/// <summary/>
		/// <returns>Returns the flid of the custom field identified by the configuration nodes FieldDescription
		/// in the class identified by <code>customFieldOwnerClassName</code></returns>
		private static int GetCustomFieldFlid(ConfigurableDictionaryNode config, IFwMetaDataCacheManaged metaDataCacheAccessor, string customFieldOwnerClassName, string customFieldName = null)
		{
			var fieldName = customFieldName ?? config.FieldDescription;
			var customFieldFlid = 0;
			if (metaDataCacheAccessor.FieldExists(customFieldOwnerClassName, fieldName, false))
			{
				customFieldFlid = metaDataCacheAccessor.GetFieldId(customFieldOwnerClassName, fieldName, false);
			}
			else if (customFieldOwnerClassName == "SenseOrEntry")
			{
				// ENHANCE (Hasso) 2016.06: take pity on the poor user who has defined identically-named Custom Fields on both Sense and Entry
				if (metaDataCacheAccessor.FieldExists("LexSense", config.FieldDescription, false))
				{
					customFieldFlid = metaDataCacheAccessor.GetFieldId("LexSense", fieldName, false);
				}
				else if (metaDataCacheAccessor.FieldExists("LexEntry", config.FieldDescription, false))
				{
					customFieldFlid = metaDataCacheAccessor.GetFieldId("LexEntry", fieldName, false);
				}
			}
			return customFieldFlid;
		}

		/// <summary>
		/// This method will return the string representing the class name for the parent
		/// node of a configuration item representing a custom field.
		/// </summary>
		private static string GetClassNameForCustomFieldParent(ConfigurableDictionaryNode customFieldNode, IFwMetaDataCacheManaged metaDataCacheAccessor)
		{
<<<<<<< HEAD:Src/LanguageExplorer/DictionaryConfiguration/ConfiguredLcmGenerator.cs
			// If the parent node of the custom field represents a collection, calling GetTypeForConfigurationNode
			// with the parent node returns the collection type. We want the type of the elements in the collection.
			var parentNodeType = GetTypeForConfigurationNode(customFieldNode.Parent, metaDataCacheAccessor, out _);
||||||| f013144d5:Src/xWorks/ConfiguredLcmGenerator.cs
			Type unneeded;
			// If the parent node of the custom field represents a collection, calling GetTypeForConfigurationNode
			// with the parent node returns the collection type. We want the type of the elements in the collection.
			var parentNodeType = GetTypeForConfigurationNode(customFieldNode.Parent, cache, out unneeded);
=======
			// Use the type of the nearest ancestor that is not a grouping node
			var parentNode = customFieldNode.Parent;
			for (; parentNode.DictionaryNodeOptions is DictionaryNodeGroupingOptions; parentNode = parentNode.Parent) { }
			var parentNodeType = GetTypeForConfigurationNode(parentNode, cache, out _);
>>>>>>> develop:Src/xWorks/ConfiguredLcmGenerator.cs
			if (parentNodeType == null)
			{
				Debug.Assert(parentNodeType != null, "Unable to find type for configuration node");
				return string.Empty;
			}

			// If the parent node of the custom field represents a collection, calling GetTypeForConfigurationNode
			// with the parent node returns the collection type. We want the type of the elements in the collection.
			if (IsCollectionType(parentNodeType))
			{
				parentNodeType = parentNodeType.GetGenericArguments()[0];
			}
			// Strip off the interface designation since custom fields are added to concrete classes (true option).
			return parentNodeType.IsInterface ? parentNodeType.Name.Substring(1) : parentNodeType.Name;
		}

		private static string GenerateXHTMLForPossibility(object propertyValue, ConfigurableDictionaryNode config,
			DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings)
		{
			if (config.ReferencedOrDirectChildren == null || !config.ReferencedOrDirectChildren.Any(node => node.IsEnabled))
			{
				return string.Empty;
			}
			var bldr = new StringBuilder();
			foreach (var content in config.ReferencedOrDirectChildren.Select(child => GenerateXHTMLForFieldByReflection(propertyValue, child, publicationDecorator, settings)))
			{
				bldr.Append(content);
			}
			return bldr.Length > 0 ? settings.ContentGenerator.WriteProcessedObject(false, bldr.ToString(), GetClassNameAttributeForConfig(config)) : string.Empty;
		}

		private static string GenerateXHTMLForPictureCaption(object propertyValue, ConfigurableDictionaryNode config, GeneratorSettings settings)
		{
			// todo: get sense numbers and captions into the same div and get rid of this if else
			var content = config.DictionaryNodeOptions != null ? GenerateXHTMLForStrings(propertyValue as IMultiString, config, settings) : GenerateXHTMLForString(propertyValue as ITsString, config, settings);
			return !string.IsNullOrEmpty(content) ? settings.ContentGenerator.WriteProcessedObject(true, content, GetClassNameAttributeForConfig(config)) : string.Empty;
		}

		private static string GenerateXHTMLForPicture(ICmFile pictureFile, ConfigurableDictionaryNode config, ICmObject owner, GeneratorSettings settings)
		{
			var srcAttribute = GenerateSrcAttributeFromFilePath(pictureFile, settings.UseRelativePaths ? "pictures" : null, settings);
			// the XHTML id attribute must be unique. The owning ICmPicture has a unique guid.
			// The ICmFile is used for all references to the same file within the project, so its guid is not unique.
			return !string.IsNullOrEmpty(srcAttribute) ? settings.ContentGenerator.AddImage(GetClassNameAttributeForConfig(config), srcAttribute, owner.Guid.ToString()) : string.Empty;
		}

		/// <summary>
		/// This method will generate a src attribute which will point to the given file from the xhtml.
		/// </summary>
		/// <para name="subfolder">If not null the path generated will be a relative path with the file in subfolder</para>
		private static string GenerateSrcAttributeFromFilePath(ICmFile file, string subFolder, GeneratorSettings settings)
		{
			string filePath;
			if (settings.UseRelativePaths && subFolder != null && file.InternalPath != null)
			{
				filePath = Path.Combine(subFolder, Path.GetFileName(MakeSafeFilePath(file.InternalPath)));
				if (settings.CopyFiles)
				{
					filePath = CopyFileSafely(settings, MakeSafeFilePath(file.AbsoluteInternalPath), filePath);
				}
			}
			else
			{
				filePath = MakeSafeFilePath(file.AbsoluteInternalPath);
			}
			return settings.UseRelativePaths ? filePath : new Uri(filePath).ToString();
		}

		private static string GenerateXHTMLForDefinitionOrGloss(ILexSense sense, ConfigurableDictionaryNode config, GeneratorSettings settings)
		{
			var wsOption = config.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
			if (wsOption == null)
			{
				throw new ArgumentException("Configuration nodes for MultiString fields whould have WritingSystemOptions", nameof(config));
			}
			var bldr = new StringBuilder();
			foreach (var option in wsOption.Options)
			{
				if (option.IsEnabled)
				{
					var bestString = sense.GetDefinitionOrGloss(option.Id, out var wsId);
					if (bestString != null)
					{
						var contentItem = GenerateWsPrefixAndString(config, settings, wsOption, wsId, bestString, Guid.Empty);
						bldr.Append(contentItem);
					}
				}
			}
			return bldr.Length > 0 ? settings.ContentGenerator.WriteProcessedCollection(false, bldr.ToString(), GetClassNameAttributeForConfig(config)) : string.Empty;
		}

		private static string GenerateSrcAttributeForMediaFromFilePath(string filename, string subFolder, GeneratorSettings settings)
		{
			string filePath;
			var linkedFilesRootDir = settings.Cache.LangProject.LinkedFilesRootDir;
			var audioVisualFile = Path.GetDirectoryName(filename) == subFolder ? Path.Combine(linkedFilesRootDir, filename) : Path.Combine(linkedFilesRootDir, subFolder, filename);
			if (settings.UseRelativePaths && subFolder != null)
			{
				filePath = Path.Combine(subFolder, Path.GetFileName(MakeSafeFilePath(filename)));
				if (settings.CopyFiles)
				{
					filePath = CopyFileSafely(settings, MakeSafeFilePath(audioVisualFile), filePath);
				}
			}
			else
			{
				filePath = MakeSafeFilePath(audioVisualFile);
			}
			return settings.UseRelativePaths ? filePath : new Uri(filePath).ToString();
		}

		internal static string CopyFileSafely(GeneratorSettings settings, string source, string relativeDestination)
		{
			if (!File.Exists(source))
			{
				return relativeDestination;
			}
			var isWavExport = settings.IsWebExport && Path.GetExtension(relativeDestination).Equals(".wav");
			if (isWavExport)
			{
				relativeDestination = Path.ChangeExtension(relativeDestination, ".mp3");
			}
			var destination = Path.Combine(settings.ExportPath, relativeDestination);
			var subFolder = Path.GetDirectoryName(relativeDestination);
			FileUtils.EnsureDirectoryExists(Path.GetDirectoryName(destination));
			// If an audio file is referenced by multiple entries they could end up in separate threads.
			// Locking on the PropertyTable seems safe since it will be the same PropertyTable for each thread.
			lock (settings.ReadOnlyPropertyTable)
			{
				if (!File.Exists(destination))
				{
					// converts audio files to correct format during Webonary export
					if (isWavExport)
					{
						WavConverter.WavToMp3(source, destination);
					}
					else
					{
						FileUtils.Copy(source, destination);
					}
				}
				else if (!AreFilesIdentical(source, destination, isWavExport))
				{
					var fileWithoutExtension = Path.GetFileNameWithoutExtension(relativeDestination);
					var fileExtension = Path.GetExtension(relativeDestination);
					var copyNumber = 0;
					string newFileName;
					do
					{
						++copyNumber;
						newFileName = $"{fileWithoutExtension}{copyNumber}{fileExtension}";
						destination = string.IsNullOrEmpty(subFolder) ? Path.Combine(settings.ExportPath, newFileName) : Path.Combine(settings.ExportPath, subFolder, newFileName);
					} while (File.Exists(destination) && !AreFilesIdentical(source, destination, isWavExport));
					// converts audio files to correct format if necessary during Webonary export
					if (!isWavExport)
					{
						File.Copy(source, destination, true); //If call two times, quicker than Windows updates the file system
					}
					else
					{
						WavConverter.WavToMp3(source, destination);
					}
					// Change the filepath to point to the copied file
					relativeDestination = string.IsNullOrEmpty(subFolder) ? newFileName : Path.Combine(subFolder, newFileName);
				}
			}
			return relativeDestination;
		}

		private static bool AreFilesIdentical(string source, string destination, bool isWavExport)
		{
			return isWavExport
				? WavConverter.AlreadyExists(source, destination) == SaveFile.IdenticalExists
				: FileUtils.AreFilesIdentical(source, destination);
		}

		private static string MakeSafeFilePath(string filePath)
		{
			if (Unicode.CheckForNonAsciiCharacters(filePath))
			{
				// Flex keeps the filename as NFD in memory because it is unicode. We need NFC to actually link to the file
				filePath = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFC).Normalize(filePath);
			}
			return !FileUtils.IsFilePathValid(filePath) ? "__INVALID_FILE_NAME__" : filePath;
		}

		/// <summary>
		/// Get the property type for a configuration node.  There is no other data available but the node itself.
		/// </summary>
		internal static PropertyType GetPropertyTypeForConfigurationNode(ConfigurableDictionaryNode config)
		{
			return GetPropertyTypeForConfigurationNode(config, null, null);
		}

		/// <summary>
		/// Get the property type for a configuration node, using a cache to help out if necessary.
		/// </summary>
		internal static PropertyType GetPropertyTypeForConfigurationNode(ConfigurableDictionaryNode config, IFwMetaDataCacheManaged metaDataCacheAccessor)
		{
			var propertyType = GetPropertyTypeForConfigurationNode(config, null, metaDataCacheAccessor);
			if (config.FieldDescription == "DefinitionOrGloss")
			{
				propertyType = PropertyType.PrimitiveType;
			}
			return propertyType;
		}

		/// <summary>
		/// This method will reflectively return the type that represents the given configuration node as
		/// described by the ancestry and FieldDescription and SubField properties of each node in it.
		/// </summary>
		/// <returns></returns>
		internal static PropertyType GetPropertyTypeForConfigurationNode(ConfigurableDictionaryNode config, Type fieldTypeFromData, IFwMetaDataCacheManaged metaDataCacheAccessor)
		{
			return GetPropertyTypeFromReflectedTypes(GetTypeForConfigurationNode(config, metaDataCacheAccessor, out var parentType) ?? fieldTypeFromData, parentType);
		}

		private static PropertyType GetPropertyTypeFromReflectedTypes(Type fieldType, Type parentType)
		{
			if (fieldType == null)
			{
				return PropertyType.InvalidProperty;
			}
			if (typeof(IStText).IsAssignableFrom(fieldType))
			{
				return PropertyType.PrimitiveType;
			}
			if (IsCollectionType(fieldType))
			{
				return PropertyType.CollectionType;
			}
			if (typeof(ICmPicture).IsAssignableFrom(parentType) && typeof(ICmFile).IsAssignableFrom(fieldType))
			{
				return PropertyType.CmPictureType;
			}
			if (typeof(ICmFile).IsAssignableFrom(fieldType))
			{
				return PropertyType.CmFileType;
			}
			if (typeof(IMoForm).IsAssignableFrom(fieldType))
			{
				return PropertyType.MoFormType;
			}
			if (typeof(ICmPossibility).IsAssignableFrom(fieldType))
			{
				return PropertyType.CmPossibility;
			}
			return typeof(ICmObject).IsAssignableFrom(fieldType) ? PropertyType.CmObjectType : PropertyType.PrimitiveType;
		}

		/// <summary>
		/// This method will return the Type that represents the data in the given configuration node.
		/// </summary>
		/// <param name="config">This node and it's lineage will be used to find the type</param>
		/// <param name="metaDataCacheAccessor">Used when dealing with custom field nodes</param>
		/// <param name="parentType">This will be set to the type of the parent of config which is sometimes useful to the callers</param>
		/// <returns></returns>
		internal static Type GetTypeForConfigurationNode(ConfigurableDictionaryNode config, IFwMetaDataCacheManaged metaDataCacheAccessor, out Type parentType)
		{
			Guard.AgainstNull(config, nameof(config));

			parentType = null;
			var lineage = new Stack<ConfigurableDictionaryNode>();
			// Build a list of the direct line up to the top of the configuration
			lineage.Push(config);
			var next = config;
			while (next.Parent != null)
			{
				next = next.Parent;
				// Grouping nodes are skipped because they do not represent properties of the model and break type finding
				if (!(next.DictionaryNodeOptions is DictionaryNodeGroupingOptions))
				{
					lineage.Push(next);
				}
			}
			// pop off the root configuration and read the FieldDescription property to get our starting point
			var assembly = GetAssemblyForFile(AssemblyFile);
			var rootNode = lineage.Pop();
			var lookupType = assembly.GetType(rootNode.FieldDescription);
			if (lookupType == null) // If the FieldDescription didn't load prepend the default model namespace and try again
			{
				lookupType = assembly.GetType("SIL.LCModel.DomainImpl." + rootNode.FieldDescription);
			}
			if (lookupType == null)
			{
				throw new ArgumentException(string.Format(DictionaryConfigurationStrings.InvalidRootConfigurationNode, rootNode.FieldDescription));
			}
			var fieldType = lookupType;
			// Traverse the configuration reflectively inspecting the types in parent to child order
			foreach (var node in lineage)
			{
				if (node.IsCustomField)
				{
					fieldType = GetCustomFieldType(lookupType, node, metaDataCacheAccessor);
				}
				else
				{
					var property = GetProperty(lookupType, node);
					if (property != null)
					{
						fieldType = property.PropertyType;
					}
					else
					{
						return null;
					}
					if (IsCollectionType(fieldType))
					{
						// When a node points to a collection all the child nodes operate on individual items in the
						// collection, so look them up in the type that the collection contains. e.g. IEnumerable<ILexEntry>
						// gives ILexEntry and ILcmVector<ICmObject> gives ICmObject
						lookupType = fieldType.GetGenericArguments()[0];
					}
					else
					{
						parentType = lookupType;
						lookupType = fieldType;
					}
				}
			}
			return fieldType;
		}

		private static Type GetCustomFieldType(Type lookupType, ConfigurableDictionaryNode config, IFwMetaDataCacheManaged metaDataCacheAccessor)
		{
			// LCM doesn't work with interfaces, just concrete classes so chop the I off any interface types
			var customFieldOwnerClassName = lookupType.Name.TrimStart('I');
			var customFieldFlid = GetCustomFieldFlid(config, metaDataCacheAccessor, customFieldOwnerClassName);
			if (customFieldFlid == 0)
			{
				return null;
			}
			var customFieldType = metaDataCacheAccessor.GetFieldType(customFieldFlid);
			switch (customFieldType)
			{
				case (int)CellarPropertyType.ReferenceSequence:
				case (int)CellarPropertyType.OwningSequence:
				case (int)CellarPropertyType.ReferenceCollection:
					{
						return typeof(ILcmVector);
					}
				case (int)CellarPropertyType.ReferenceAtomic:
				case (int)CellarPropertyType.OwningAtomic:
					{
						var destClassId = metaDataCacheAccessor.GetDstClsId(customFieldFlid);
						if (destClassId == StTextTags.kClassId)
						{
							return typeof(IStText);
						}
						return typeof(ICmObject);
					}
				case (int)CellarPropertyType.Time:
					{
						return typeof(DateTime);
					}
				case (int)CellarPropertyType.MultiUnicode:
					{
						return typeof(IMultiUnicode);
					}
				case (int)CellarPropertyType.MultiString:
					{
						return typeof(IMultiString);
					}
				case (int)CellarPropertyType.String:
					{
						return typeof(string);
					}
				default:
					return null;
			}
		}

		/// <summary>
		/// Loading an assembly is expensive so we cache the assembly once it has been loaded
		/// for enhanced performance.
		/// </summary>
		private static Assembly GetAssemblyForFile(string assemblyFile)
		{
			if (!AssemblyMap.ContainsKey(assemblyFile))
			{
				AssemblyMap[assemblyFile] = Assembly.Load(AssemblyFile);
			}
			return AssemblyMap[assemblyFile];
		}

		/// <summary>
		/// Return the property info from a given class and node. Will check interface hierarchy for the property
		/// if <code>lookupType</code> is an interface.
		/// </summary>
		private static PropertyInfo GetProperty(Type lookupType, ConfigurableDictionaryNode node)
		{
			PropertyInfo propInfo;
			var typesToCheck = new Stack<Type>();
			typesToCheck.Push(lookupType);
			do
			{
				var current = typesToCheck.Pop();
				var propertyOfInterest = node.FieldDescription;
				// if there is a SubField we need to use the type of the FieldDescription
				// for the rest of this method so set current to the FieldDescription type.
				if (node.SubField != null)
				{
					var property = current.GetProperty(node.FieldDescription);
					propertyOfInterest = node.SubField;
					if (property != null)
					{
						current = property.PropertyType;
					}
				}
				propInfo = current.GetProperty(propertyOfInterest, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (propInfo == null)
				{
					foreach (var i in current.GetInterfaces())
					{
						typesToCheck.Push(i);
					}
				}
			} while (propInfo == null && typesToCheck.Count > 0);
			return propInfo;
		}

		private static string GenerateXHTMLForMoForm(IMoForm moForm, ConfigurableDictionaryNode config, GeneratorSettings settings)
		{
			// Don't export if there is no such data
			if (moForm == null)
			{
				return string.Empty;
			}
			if (config.ReferencedOrDirectChildren != null && config.ReferencedOrDirectChildren.Any())
			{
				throw new NotImplementedException("Children for MoForm types not yet supported.");
			}
			return GenerateXHTMLForStrings(moForm.Form, config, settings, moForm.Owner.Guid);
		}

		/// <summary>
		/// This method will generate the XHTML that represents a collection and its contents
		/// </summary>
		private static string GenerateXHTMLForCollection(object collectionField, ConfigurableDictionaryNode config,
			DictionaryPublicationDecorator pubDecorator, object collectionOwner, GeneratorSettings settings, SenseInfo info = new SenseInfo())
		{
			// To be used for things like shared grammatical info
			var sharedCollectionInfo = string.Empty;
			var bldr = new StringBuilder();
			IEnumerable collection;
			switch (collectionField)
			{
				case IEnumerable field:
					collection = field;
					break;
				case ILcmVector vector:
					collection = vector.Objects;
					break;
				default:
					throw new ArgumentException("The given field is not a recognized collection");
			}
			var cmOwner = collectionOwner as ICmObject ?? ((ISenseOrEntry)collectionOwner).Item;

			if (config.DictionaryNodeOptions is DictionaryNodeSenseOptions)
			{
				bldr.Append(GenerateXHTMLForSenses(config, pubDecorator, settings, collection, info, ref sharedCollectionInfo));
			}
			else
			{
				FilterAndSortCollectionIfNeeded(ref collection, pubDecorator, config.SubField ?? config.FieldDescription);
				if (IsVariantEntryType(config, out var lexEntryTypeNode))
				{
					bldr.Append(GenerateXHTMLForILexEntryRefCollection(config, collection, cmOwner, pubDecorator, settings, lexEntryTypeNode, false));
				}
				else if (IsComplexEntryType(config, out lexEntryTypeNode))
				{
					bldr.Append(GenerateXHTMLForILexEntryRefCollection(config, collection, cmOwner, pubDecorator, settings, lexEntryTypeNode, true));
				}
				else if (IsPrimaryEntryReference(config, out lexEntryTypeNode))
				{
					// Order by guid (to order things consistently; see LT-17384).
					// Though perhaps another sort key would be better, such as ICmObject.SortKey or ICmObject.SortKey2.
					var lerCollection = collection.Cast<ILexEntryRef>().OrderBy(ler => ler.Guid).ToList();
					// Group by Type only if Type is selected for output.
					if (lexEntryTypeNode.IsEnabled && lexEntryTypeNode.ReferencedOrDirectChildren != null
						&& lexEntryTypeNode.ReferencedOrDirectChildren.Any(y => y.IsEnabled))
					{
						Debug.Assert(config.DictionaryNodeOptions == null, "double calls to GenerateXHTMLForILexEntryRefsByType don't play nicely with ListOptions. Everything will be generated twice (if it doesn't crash)");
						// Display typeless refs
						foreach (var entry in lerCollection.Where(item => !item.ComplexEntryTypesRS.Any() && !item.VariantEntryTypesRS.Any()))
						{
							bldr.Append(GenerateCollectionItemContent(config, pubDecorator, entry, collectionOwner, settings, lexEntryTypeNode));
						}
						// Display refs of each type
						GenerateXHTMLForILexEntryRefsByType(config, lerCollection, collectionOwner, pubDecorator, settings, bldr, lexEntryTypeNode, true); // complex
						GenerateXHTMLForILexEntryRefsByType(config, lerCollection, collectionOwner, pubDecorator, settings, bldr, lexEntryTypeNode, false); // variants
					}
					else
					{
						Debug.WriteLine("Unable to group " + config.FieldDescription + " by LexRefType; generating sequentially");
						foreach (var item in lerCollection)
						{
							bldr.Append(GenerateCollectionItemContent(config, pubDecorator, item, collectionOwner, settings));
						}
					}
				}
				else if (config.FieldDescription.StartsWith("Subentries"))
				{
					GenerateXHTMLForSubentries(config, collection, cmOwner, pubDecorator, settings, bldr);
				}
				else if (IsLexReferenceCollection(config))
				{
					GenerateXHTMLForILexReferenceCollection(config, collection.Cast<ILexReference>(), cmOwner, pubDecorator, settings, bldr);
				}
				else
				{
					foreach (var item in collection)
					{
						bldr.Append(GenerateCollectionItemContent(config, pubDecorator, item, collectionOwner, settings));
					}
				}
			}
			if (bldr.Length > 0 || sharedCollectionInfo.Length > 0)
			{
				return config.DictionaryNodeOptions is DictionaryNodeSenseOptions
					? settings.ContentGenerator.WriteProcessedSenses(false, bldr.ToString(), GetClassNameAttributeForConfig(config), sharedCollectionInfo)
					: settings.ContentGenerator.WriteProcessedCollection(false, bldr.ToString(), GetClassNameAttributeForConfig(config));
			}
			return string.Empty;
		}

		private static bool IsLexReferenceCollection(ConfigurableDictionaryNode config)
		{
			return config.DictionaryNodeOptions is DictionaryNodeListOptions dictionaryNodeListOptions && (dictionaryNodeListOptions.ListId == ListIds.Entry || dictionaryNodeListOptions.ListId == ListIds.Sense);
		}

		internal static bool IsFactoredReference(ConfigurableDictionaryNode node, out ConfigurableDictionaryNode typeChild)
		{
			if (node.DictionaryNodeOptions is IParaOption paraOptions && paraOptions.DisplayEachInAParagraph)
			{
				typeChild = null;
				return false;
			}
			return IsVariantEntryType(node, out typeChild) || IsComplexEntryType(node, out typeChild) || IsPrimaryEntryReference(node, out typeChild);
		}

		/// <summary>
		/// Whether the selected node represents Complex Entries.
		/// This does *not* include Subentries, because Subentries are (a) never factored and (b) ILexEntries instead of ILexEntryRefs.
		/// </summary>
		private static bool IsComplexEntryType(ConfigurableDictionaryNode config, out ConfigurableDictionaryNode complexEntryTypeNode)
		{
			complexEntryTypeNode = null;
			// REVIEW (Hasso)2017.01: better to check ListId==Complex && !FieldDesc.StartsWith("Subentries")?
			if ((config.FieldDescription == "VisibleComplexFormBackRefs" || config.FieldDescription == "ComplexFormsNotSubentries") && config.ReferencedOrDirectChildren != null)
			{
				complexEntryTypeNode = config.ReferencedOrDirectChildren.FirstOrDefault(child => child.FieldDescription == "ComplexEntryTypesRS");
				return complexEntryTypeNode != null;
			}
			return false;
		}

		private static bool IsVariantEntryType(ConfigurableDictionaryNode config, out ConfigurableDictionaryNode variantEntryTypeNode)
		{
			variantEntryTypeNode = null;
			if (config.DictionaryNodeOptions is DictionaryNodeListOptions variantOptions && variantOptions.ListId == ListIds.Variant)
			{
				variantEntryTypeNode = config.ReferencedOrDirectChildren.FirstOrDefault(x => x.FieldDescription == "VariantEntryTypesRS");
				return variantEntryTypeNode != null;
			}
			return false;
		}

		private static bool IsPrimaryEntryReference(ConfigurableDictionaryNode config, out ConfigurableDictionaryNode entryTypesNode)
		{
			entryTypesNode = null;
			if (config.FieldDescription == "MainEntryRefs" || config.FieldDescription == "EntryRefsWithThisMainSense")
			{
				entryTypesNode = config.ReferencedOrDirectChildren.FirstOrDefault(n => n.FieldDescription == "EntryTypes");
				return entryTypesNode != null;
			}
			return false;
		}

		private static string GenerateXHTMLForILexEntryRefCollection(ConfigurableDictionaryNode config, IEnumerable collection, ICmObject collectionOwner,
			DictionaryPublicationDecorator pubDecorator, GeneratorSettings settings, ConfigurableDictionaryNode typeNode, bool isComplex)
		{
			var bldr = new StringBuilder();
			var lerCollection = collection.Cast<ILexEntryRef>().ToList();
			// ComplexFormsNotSubentries is a filtered version of VisibleComplexFormBackRefs, so it doesn't have it's own VirtualOrdering.
			var fieldForVO = config.FieldDescription == "ComplexFormsNotSubentries" ? "VisibleComplexFormBackRefs" : config.FieldDescription;
			if (lerCollection.Count > 1 && !VirtualOrderingServices.HasVirtualOrdering(collectionOwner, fieldForVO))
			{
				// Order things (LT-17384) alphabetically (LT-17762) if and only if the user hasn't specified an order (LT-17918).
				var wsId = settings.Cache.ServiceLocator.WritingSystemManager.Get(lerCollection.First().SortKeyWs);
				var comparer = new WritingSystemComparer(wsId);
				lerCollection.Sort((left, right) => comparer.Compare(left.SortKey, right.SortKey));
			}
			// Group by Type only if Type is selected for output.
			if (typeNode.IsEnabled && typeNode.ReferencedOrDirectChildren != null && typeNode.ReferencedOrDirectChildren.Any(y => y.IsEnabled))
			{
				// Display typeless refs
				foreach (var entry in lerCollection.Where(item => !item.ComplexEntryTypesRS.Any() && !item.VariantEntryTypesRS.Any()))
				{
					bldr.Append(GenerateCollectionItemContent(config, pubDecorator, entry, collectionOwner, settings, typeNode));
				}
				// Display refs of each type
				GenerateXHTMLForILexEntryRefsByType(config, lerCollection, collectionOwner, pubDecorator, settings, bldr, typeNode, isComplex);
			}
			else
			{
				Debug.WriteLine("Unable to group " + config.FieldDescription + " by LexRefType; generating sequentially");
				foreach (var item in lerCollection)
				{
					bldr.Append(GenerateCollectionItemContent(config, pubDecorator, item, collectionOwner, settings));
				}
			}
			return bldr.ToString();
		}

		private static void GenerateXHTMLForILexEntryRefsByType(ConfigurableDictionaryNode config, List<ILexEntryRef> lerCollection, object collectionOwner, DictionaryPublicationDecorator pubDecorator,
			GeneratorSettings settings, StringBuilder bldr, ConfigurableDictionaryNode typeNode, bool isComplex)
		{
			var lexEntryTypes = isComplex
				? settings.Cache.LangProject.LexDbOA.ComplexEntryTypesOA.ReallyReallyAllPossibilities
				: settings.Cache.LangProject.LexDbOA.VariantEntryTypesOA.ReallyReallyAllPossibilities;
			// Order the types by their order in their list in the configuration options, if any (LT-18018).
			var listOptions = config.DictionaryNodeOptions as DictionaryNodeListOptions;
			var lexEntryTypesFiltered = listOptions == null
				? lexEntryTypes.Select(t => t.Guid)
				: listOptions.Options.Where(o => o.IsEnabled).Select(o => new Guid(o.Id));
			// Don't factor out Types when displaying in a paragraph
			if (config.DictionaryNodeOptions is IParaOption paraOptions && paraOptions.DisplayEachInAParagraph)
			{
				typeNode = null;
			}
			// Generate XHTML by Type
			foreach (var typeGuid in lexEntryTypesFiltered)
			{
				var innerBldr = new StringBuilder();
				foreach (var lexEntRef in lerCollection.Where(lexEntRef => isComplex ? lexEntRef.ComplexEntryTypesRS.Any(t => t.Guid == typeGuid) : lexEntRef.VariantEntryTypesRS.Any(t => t.Guid == typeGuid)))
				{
					innerBldr.Append(GenerateCollectionItemContent(config, pubDecorator, lexEntRef, collectionOwner, settings, typeNode));
				}
				if (innerBldr.Length > 0)
				{
					var lexEntryType = lexEntryTypes.First(t => t.Guid.Equals(typeGuid));
					// Display the Type iff there were refs of this Type (and we are factoring)
					var generateLexType = typeNode != null;
					var lexTypeContent = generateLexType
						? GenerateCollectionItemContent(typeNode, pubDecorator, lexEntryType, lexEntryType.Owner, settings)
						: null;
					var className = generateLexType ? GetClassNameAttributeForConfig(typeNode) : null;
<<<<<<< HEAD:Src/LanguageExplorer/DictionaryConfiguration/ConfiguredLcmGenerator.cs
					var refsByType = settings.ContentGenerator.AddLexReferences(generateLexType, lexTypeContent, className, innerBldr.ToString());
||||||| f013144d5:Src/xWorks/ConfiguredLcmGenerator.cs
					var refsByType = settings.ContentGenerator.AddLexReferences(generateLexType,
						lexTypeContent, className, innerBldr.ToString());
=======
					var refsByType = settings.ContentGenerator.AddLexReferences(generateLexType,
						lexTypeContent, className, innerBldr.ToString(), IsTypeBeforeForm(config));
>>>>>>> develop:Src/xWorks/ConfiguredLcmGenerator.cs
					bldr.Append(refsByType);
				}
			}
		}

		private static void GenerateXHTMLForSubentries(ConfigurableDictionaryNode config, IEnumerable collection, ICmObject collectionOwner,
			DictionaryPublicationDecorator pubDecorator, GeneratorSettings settings, StringBuilder bldr)
		{
			var listOptions = config.DictionaryNodeOptions as DictionaryNodeListOptions;
			var typeNode = config.ReferencedOrDirectChildren.FirstOrDefault(n => n.FieldDescription == LookupComplexEntryType);
			if (listOptions != null && typeNode != null && typeNode.IsEnabled
				&& typeNode.ReferencedOrDirectChildren != null && typeNode.ReferencedOrDirectChildren.Any(n => n.IsEnabled))
			{
				// Get a list of Subentries including their relevant ILexEntryRefs. We will remove each Subentry from the list as it is
				// generated to prevent multiple generations on the odd chance that a Subentry has multiple Complex Form Types
				var subentries = collection.Cast<ILexEntry>().Select(le => new Tuple<ILexEntryRef, ILexEntry>(EntryRefForSubentry(le, collectionOwner), le)).ToList();
				// Generate any Subentries with no ComplexFormType
				for (var i = 0; i < subentries.Count; i++)
				{
					if (subentries[i].Item1 == null || !subentries[i].Item1.ComplexEntryTypesRS.Any())
					{
						bldr.Append(GenerateCollectionItemContent(config, pubDecorator, subentries[i].Item2, collectionOwner, settings));
						subentries.RemoveAt(i--);
					}
				}
				// Generate Subentries by ComplexFormType
				foreach (var typeGuid in listOptions.Options.Where(o => o.IsEnabled).Select(o => new Guid(o.Id)))
				{
					for (var i = 0; i < subentries.Count; i++)
					{
						if (subentries[i].Item1.ComplexEntryTypesRS.Any(t => t.Guid == typeGuid))
						{
							bldr.Append(GenerateCollectionItemContent(config, pubDecorator, subentries[i].Item2, collectionOwner, settings));
							subentries.RemoveAt(i--);
						}
					}
				}
			}
			else
			{
				Debug.WriteLine("Unable to group " + config.FieldDescription + " by LexRefType; generating sequentially");
				foreach (var item in collection)
				{
					bldr.Append(GenerateCollectionItemContent(config, pubDecorator, item, collectionOwner, settings));
				}
			}
		}

		/// <summary>
		/// Don't show examples or subentries that have been marked to exclude from publication.
		/// See https://jira.sil.org/browse/LT-15697 and https://jira.sil.org/browse/LT-16775.
		/// Consistently sort indeterminately-ordered collections. See https://jira.sil.org/browse/LT-17384
		/// </summary>
		private static void FilterAndSortCollectionIfNeeded(ref IEnumerable collection, DictionaryPublicationDecorator decorator, string fieldDescr)
		{
			switch (collection)
			{
				case IEnumerable<ICmObject> _:
				{
					var cmCollection = collection.Cast<ICmObject>();
					if (decorator != null)
					{
						cmCollection = cmCollection.Where(item => !decorator.IsExcludedObject(item));
					}
					if (IsCollectionInNeedOfSorting(fieldDescr))
					{
						cmCollection = cmCollection.OrderBy(x => x.SortKey2);
					}
					collection = cmCollection;
					break;
				}
				case IEnumerable<ISenseOrEntry> _:
				{
					var seCollection = collection.Cast<ISenseOrEntry>();
					if (decorator != null)
					{
						seCollection = seCollection.Where(item => !decorator.IsExcludedObject(item.Item));
					}
					if (IsCollectionInNeedOfSorting(fieldDescr))
					{
						seCollection = seCollection.OrderBy(x => x.Item.SortKey2);
					}
					collection = seCollection;
					break;
				}
			}
		}

		/// <remarks>Variants and Complex Forms may also need sorting, but it is more efficient to check for them elsewhere</remarks>
		private static bool IsCollectionInNeedOfSorting(string fieldDescr)
		{
			// REVIEW (Hasso) 2016.09: should we check the CellarPropertyType?
			return fieldDescr.EndsWith("RC") || fieldDescr.EndsWith("OC"); // Reference Collection, Owning Collection (vs. Sequence)
		}

		/// <summary>
		/// This method will generate the XHTML that represents a senses collection and its contents
		/// </summary>
		private static string GenerateXHTMLForSenses(ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator,
			GeneratorSettings settings, IEnumerable senseCollection, SenseInfo info, ref string sharedGramInfo)
		{
			// Check whether all the senses have been excluded from publication.  See https://jira.sil.org/browse/LT-15697.
			var filteredSenseCollection = new List<ILexSense>();
			foreach (ILexSense item in senseCollection)
			{
				Debug.Assert(item != null);
				if (publicationDecorator != null && publicationDecorator.IsExcludedObject(item))
				{
					continue;
				}
				filteredSenseCollection.Add(item);
			}
			if (filteredSenseCollection.Count == 0)
			{
				return string.Empty;
			}
			var isSubsense = config.Parent != null && config.FieldDescription == config.Parent.FieldDescription;
			var isSameGrammaticalInfo = IsAllGramInfoTheSame(config, filteredSenseCollection, isSubsense, out _, out _);
			if (isSameGrammaticalInfo && !isSubsense)
			{
				sharedGramInfo = InsertGramInfoBeforeSenses(filteredSenseCollection.First(),
					config.ReferencedOrDirectChildren.FirstOrDefault(e => e.FieldDescription == "MorphoSyntaxAnalysisRA" && e.IsEnabled),
					publicationDecorator, settings);
			}
			//sensecontent sensenumber sense morphosyntaxanalysis mlpartofspeech en
			info.SenseCounter = 0; // This ticker is more efficient than computing the index for each sense individually
			var senseNode = (DictionaryNodeSenseOptions)config.DictionaryNodeOptions;
			if (senseNode != null)
			{
				info.ParentSenseNumberingStyle = senseNode.ParentSenseNumberingStyle;
			}
			// Calculating isThisSenseNumbered may make sense to do for each item in the foreach loop below, but because of how the answer
			// is determined, the answer for all sibling senses is the same as for the first sense in the collection.
			// So calculating outside the loop for performance.
			var isThisSenseNumbered = ShouldThisSenseBeNumbered(filteredSenseCollection[0], config, filteredSenseCollection);
			var bldr = new StringBuilder();
			foreach (var item in filteredSenseCollection)
			{
				info.SenseCounter++;
				bldr.Append(GenerateSenseContent(config, publicationDecorator, item, isThisSenseNumbered, settings, isSameGrammaticalInfo, info));
			}
			return bldr.ToString();
		}

		/// <summary>
		/// Some behaviour discussed regarding whether to show a sense number while working on LT-17906 is as follows.
		///
		/// Does the numbering style for senses say to number it?
		///  - No? Don't number.
		/// Yes? Is this the only sense-level sense?
		///  - No? Number it.
		/// Yes? Is the box for 'Number even a single sense' checked?
		///  - Yes? Number it.
		/// No? Is there a subsense?
		///  - No? Don't number.
		/// Yes? Is the subsense showing (enabled in the config)?
		///  - No? Don't number.
		/// Yes? Does the style for the subsense say to number the subsense?
		///  - No? Don't number.
		///  - Yes? Number it.
		/// </summary>
		internal static bool ShouldThisSenseBeNumbered(ILexSense sense, ConfigurableDictionaryNode senseConfiguration, IEnumerable<ILexSense> siblingSenses)
		{
			var senseOptions = (DictionaryNodeSenseOptions)senseConfiguration.DictionaryNodeOptions;
			return !string.IsNullOrEmpty(senseOptions.NumberingStyle) && (siblingSenses.Count() > 1 || senseOptions.NumberEvenASingleSense || sense.SensesOS.Any() && AreThereEnabledSubsensesWithNumberingStyle(senseConfiguration));
		}

		/// <summary>
		/// Does this sense node have a subsenses node that is enabled in the configuration and has numbering style?
		/// </summary>
		/// <param name="senseNode">sense node that might have subsenses</param>
		internal static bool AreThereEnabledSubsensesWithNumberingStyle(ConfigurableDictionaryNode senseNode)
		{
			return senseNode != null && senseNode.Children.Any(child => child.DictionaryNodeOptions is DictionaryNodeSenseOptions dictionaryNodeSenseOptions
																		&& child.IsEnabled && !string.IsNullOrEmpty(dictionaryNodeSenseOptions.NumberingStyle));
		}

		private static string InsertGramInfoBeforeSenses(ILexSense item, ConfigurableDictionaryNode gramInfoNode, DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings)
		{
			var content = GenerateXHTMLForFieldByReflection(item, gramInfoNode, publicationDecorator, settings);
			if (string.IsNullOrEmpty(content))
			{
				return string.Empty;
			}
			return settings.ContentGenerator.GenerateGramInfoBeforeSensesContent(content);
		}

		private static bool IsAllGramInfoTheSame(ConfigurableDictionaryNode config, IEnumerable<ILexSense> collection, bool isSubsense, out string lastGrammaticalInfo, out string langId)
		{
			lastGrammaticalInfo = string.Empty;
			langId = string.Empty;
			var isSameGrammaticalInfo = false;
			if (config.FieldDescription == "SensesOS" || config.FieldDescription == "SensesRS")
			{
				var senseNode = (DictionaryNodeSenseOptions)config.DictionaryNodeOptions;
				if (senseNode == null)
				{
					return false;
				}
				if (senseNode.ShowSharedGrammarInfoFirst)
				{
					if (isSubsense)
					{
						// Add the owning sense to the collection that we want to check.
						var objs = new List<ILexSense>();
						objs.AddRange(collection);
						if (objs.Count == 0 || !(objs[0].Owner is ILexSense))
						{
							return false;
						}
						objs.Add((ILexSense)objs[0].Owner);
						if (!CheckIfAllGramInfoTheSame(config, objs, ref isSameGrammaticalInfo, ref lastGrammaticalInfo, ref langId))
						{
							return false;
						}
					}
					else
					{
						if (!CheckIfAllGramInfoTheSame(config, collection, ref isSameGrammaticalInfo, ref lastGrammaticalInfo,
							ref langId))
						{
							return false;
						}
					}
				}
			}
			return isSameGrammaticalInfo && !string.IsNullOrEmpty(lastGrammaticalInfo);
		}

		private static bool CheckIfAllGramInfoTheSame(ConfigurableDictionaryNode config, IEnumerable<ILexSense> collection, ref bool isSameGrammaticalInfo, ref string lastGrammaticalInfo, ref string langId)
		{
			foreach (var item in collection)
			{
				var requestedString = string.Empty;
				var owningObject = (ICmObject)item;
				var defaultWs = owningObject.Cache.WritingSystemFactory.get_EngineOrNull(owningObject.Cache.DefaultUserWs);
				langId = defaultWs.Id;
				var entryType = item.GetType();
				var grammaticalInfo = config.ReferencedOrDirectChildren.FirstOrDefault(e => e.FieldDescription == "MorphoSyntaxAnalysisRA" && e.IsEnabled);
				if (grammaticalInfo == null)
				{
					return false;
				}
				var property = entryType.GetProperty(grammaticalInfo.FieldDescription);
				var propertyValue = property.GetValue(item, new object[] { });
				if (propertyValue == null)
				{
					return false;
				}
				var child = grammaticalInfo.ReferencedOrDirectChildren.FirstOrDefault(e => e.IsEnabled && e.ReferencedOrDirectChildren.Count == 0);
				if (child == null)
				{
					return false;
				}
				entryType = propertyValue.GetType();
				property = entryType.GetProperty(child.FieldDescription);
				propertyValue = property.GetValue(propertyValue, new object[] { });
				if (propertyValue is ITsString tsString)
				{
					requestedString = tsString.Text;
				}
				else
				{
					var fieldValue = (IMultiAccessorBase)propertyValue;
					var bestStringValue = fieldValue.BestAnalysisAlternative.Text;
					if (bestStringValue != fieldValue.NotFoundTss.Text)
					{
						requestedString = bestStringValue;
					}
				}
				if (string.IsNullOrEmpty(lastGrammaticalInfo))
				{
					lastGrammaticalInfo = requestedString;
					isSameGrammaticalInfo = true;
				}
				else if (requestedString != lastGrammaticalInfo)
				{
					return false;
				}
			}
			return true;
		}

		private static string GenerateSenseContent(ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator,
			object item, bool isThisSenseNumbered, GeneratorSettings settings, bool isSameGrammaticalInfo, SenseInfo info)
		{
			var senseNumberSpan = GenerateSenseNumberSpanIfNeeded(config, isThisSenseNumbered, ref info, settings);
			var bldr = new StringBuilder();
			if (config.ReferencedOrDirectChildren != null)
			{
				foreach (var child in config.ReferencedOrDirectChildren.Where(child => child.FieldDescription != "MorphoSyntaxAnalysisRA" || !isSameGrammaticalInfo))
				{
					bldr.Append(GenerateXHTMLForFieldByReflection(item, child, publicationDecorator, settings, info));
				}
			}
			if (bldr.Length == 0)
			{
				return string.Empty;
			}
			var senseContent = bldr.ToString();
			bldr.Clear();
			return settings.ContentGenerator.AddSenseData(senseNumberSpan, IsBlockProperty(config), ((ICmObject)item).Owner.Guid,
				senseContent, GetCollectionItemClassAttribute(config));
		}

		private static string GeneratePictureContent(ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator, object item, GeneratorSettings settings)
		{
			var bldr = new StringBuilder();
			var contentGenerator = settings.ContentGenerator;
			using (var writer = contentGenerator.CreateWriter(bldr))
			{
				//Adding Thumbnail tag
				foreach (var content in config.ReferencedOrDirectChildren
					.Where(child => child.FieldDescription == "PictureFileRA")
					.Select(child => GenerateXHTMLForFieldByReflection(item, child, publicationDecorator, settings)))
				{
					contentGenerator.WriteProcessedContents(writer, content);
					break;
				}
				//Adding tags for Sense Number and Caption
				// Note: this SenseNumber comes from a field in the FDO model (not generated based on a DictionaryNodeSenseOptions).
				//  Should we choose in the future to generate the Picture's sense number using ConfiguredLcmGenerator based on a SenseOption,
				//  we will need to pass the SenseOptions to this point in the call tree.
				var captionBldr = new StringBuilder();
				foreach (var content in config.ReferencedOrDirectChildren
					.Where(child => child.FieldDescription != "PictureFileRA")
					.Select(child => GenerateXHTMLForFieldByReflection(item, child, publicationDecorator, settings)))
				{
					captionBldr.Append(content);
				}
				if (captionBldr.Length != 0)
				{
					contentGenerator.WriteProcessedContents(writer, settings.ContentGenerator.AddImageCaption(captionBldr.ToString()));
				}
				writer.Flush();
			}
			return bldr.ToString();
		}

		private static string GenerateCollectionItemContent(ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator,
			object item, object collectionOwner, GeneratorSettings settings, ConfigurableDictionaryNode factoredTypeField = null)
		{
			if (item is IMultiStringAccessor multiStringAccessor)
			{
				return GenerateXHTMLForStrings(multiStringAccessor, config, settings);
			}
			if (config.DictionaryNodeOptions is DictionaryNodeListOptions && !IsListItemSelectedForExport(config, item, collectionOwner) || config.ReferencedOrDirectChildren == null)
			{
				return string.Empty;
			}
			var bldr = new StringBuilder();
			var listOptions = config.DictionaryNodeOptions as DictionaryNodeListOptions;
			if (listOptions is DictionaryNodeListAndParaOptions)
			{
				foreach (var child in config.ReferencedOrDirectChildren.Where(child => !ReferenceEquals(child, factoredTypeField)))
				{
					bldr.Append(child.FieldDescription == LookupComplexEntryType
						? GenerateSubentryTypeChild(child, publicationDecorator, (ILexEntry)item, collectionOwner, settings)
						: GenerateXHTMLForFieldByReflection(item, child, publicationDecorator, settings));
				}
			}
			else if (config.DictionaryNodeOptions is DictionaryNodePictureOptions)
			{
				bldr.Append(GeneratePictureContent(config, publicationDecorator, item, settings));
			}
			else
			{
				// If a type field has been factored out and generated then skip generating it here
				foreach (var child in config.ReferencedOrDirectChildren.Where(child => !ReferenceEquals(child, factoredTypeField)))
				{
					bldr.Append(GenerateXHTMLForFieldByReflection(item, child, publicationDecorator, settings));
				}
			}
			if (bldr.Length == 0)
			{
				return string.Empty;
			}
			var collectionContent = bldr.ToString();
			bldr.Clear();
			return settings.ContentGenerator.AddCollectionItem(IsBlockProperty(config), GetCollectionItemClassAttribute(config), collectionContent);
		}

		private static void GenerateXHTMLForILexReferenceCollection(ConfigurableDictionaryNode config, IEnumerable<ILexReference> collection, ICmObject cmOwner,
			DictionaryPublicationDecorator pubDecorator, GeneratorSettings settings, StringBuilder bldr)
		{
			// The collection of ILexReferences has already been sorted by type,
			// so we'll now group all the targets by LexRefType and sort their targets alphabetically before generating XHTML
			var organizedRefs = SortAndFilterLexRefsAndTargets(collection, cmOwner, config);
			// Now that we have things in the right order, try outputting one type at a time
			foreach (var xBldr in organizedRefs.Select(referenceList => GenerateCrossReferenceChildren(config, pubDecorator, referenceList, cmOwner, settings)))
			{
				bldr.Append(xBldr);
			}
		}

		/// <returns>A list (by Type) of lists of Lex Reference Targets (tupled with their references)</returns>
		private static List<List<Tuple<ISenseOrEntry, ILexReference>>> SortAndFilterLexRefsAndTargets(IEnumerable<ILexReference> collection, ICmObject cmOwner, ConfigurableDictionaryNode config)
		{
			var orderedTargets = new List<List<Tuple<ISenseOrEntry, ILexReference>>>();
			var curType = new Tuple<ILexRefType, string>(null, null);
			var allTargetsForType = new List<Tuple<ISenseOrEntry, ILexReference>>();
			foreach (var lexReference in collection)
			{
				var type = new Tuple<ILexRefType, string>(lexReference.OwnerType, LexRefDirection(lexReference, cmOwner));
				if (!type.Item1.Equals(curType.Item1) || (LexRefTypeTags.IsAsymmetric((LexRefTypeTags.MappingTypes)type.Item1.MappingType) && !type.Item2.Equals(curType.Item2)))
				{
					MoveTargetsToMasterList(cmOwner, curType.Item1, config, allTargetsForType, orderedTargets);
				}
				curType = type;
				if (LexRefTypeTags.IsAsymmetric((LexRefTypeTags.MappingTypes)curType.Item1.MappingType) && LexRefDirection(lexReference, cmOwner) == ":r" && lexReference.ConfigTargets.Any())
				{
					// In the reverse direction of an asymmetric lexical reference, we want only the first item.
					// See https://jira.sil.org/browse/LT-16427.
					allTargetsForType.Add(new Tuple<ISenseOrEntry, ILexReference>(lexReference.ConfigTargets.First(t => !IsOwner(t, cmOwner)), lexReference));
				}
				else
				{
					allTargetsForType.AddRange(lexReference.ConfigTargets.Select(target => new Tuple<ISenseOrEntry, ILexReference>(target, lexReference)));
				}
			}
			MoveTargetsToMasterList(cmOwner, curType.Item1, config, allTargetsForType, orderedTargets);
			return orderedTargets;
		}

		private static void MoveTargetsToMasterList(ICmObject cmOwner, ILexRefType curType, ConfigurableDictionaryNode config,
			List<Tuple<ISenseOrEntry, ILexReference>> bucketList, List<List<Tuple<ISenseOrEntry, ILexReference>>> lexRefTargetList)
		{
			if (bucketList.Count == 0)
			{
				return;
			}
			if (!IsListItemSelectedForExport(config, bucketList.First().Item2, cmOwner))
			{
				bucketList.Clear();
				return;
			}
			// In a "Sequence" type lexical relation (e.g. days of the week), the current item should be displayed in its location in the sequence.
			if (!LexRefTypeTags.IsSequence((LexRefTypeTags.MappingTypes)curType.MappingType))
			{
				bucketList.RemoveAll(t => IsOwner(t.Item1, cmOwner));
				// "Unidirectional" relations, like Sequences, are user-orderable (but only sequences include their owner)
				if (!LexRefTypeTags.IsUnidirectional((LexRefTypeTags.MappingTypes)curType.MappingType))
				{
					bucketList.Sort(CompareLexRefTargets);
				}
			}
			lexRefTargetList.Add(new List<Tuple<ISenseOrEntry, ILexReference>>(bucketList));
			bucketList.Clear();
		}

		private static bool IsOwner(ISenseOrEntry target, ICmObject owner)
		{
			return target.Item.Guid.Equals(owner.Guid);
		}

		private static int CompareLexRefTargets(Tuple<ISenseOrEntry, ILexReference> lhs, Tuple<ISenseOrEntry, ILexReference> rhs)
		{
			var wsId = lhs.Item1.Item.Cache.ServiceLocator.WritingSystemManager.Get(lhs.Item1.HeadWord.get_WritingSystem(0));
			var comparer = new WritingSystemComparer(wsId);
			var result = comparer.Compare(lhs.Item1.HeadWord.Text, rhs.Item1.HeadWord.Text);
			return result;
		}

		/// <returns>Content for Targets and nodes, except Type, which is returned in ref string typeXHTML</returns>
		private static string GenerateCrossReferenceChildren(ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator,
			List<Tuple<ISenseOrEntry, ILexReference>> referenceList, object collectionOwner, GeneratorSettings settings)
		{
			if (config.ReferencedOrDirectChildren == null)
			{
				return string.Empty;
			}
			var xBldr = new StringBuilder();
			using (var xw = settings.ContentGenerator.CreateWriter(xBldr))
			{
				settings.ContentGenerator.BeginCrossReference(xw, IsBlockProperty(config), GetCollectionItemClassAttribute(config));
				var targetInfo = referenceList.FirstOrDefault();
				if (targetInfo == null)
				{
					return string.Empty;
				}
				var reference = targetInfo.Item2;
				if (LexRefTypeTags.IsUnidirectional((LexRefTypeTags.MappingTypes)reference.OwnerType.MappingType) && LexRefDirection(reference, collectionOwner) == ":r")
				{
					return string.Empty;
				}
				foreach (var child in config.ReferencedOrDirectChildren.Where(c => c.IsEnabled))
				{
					switch (child.FieldDescription)
					{
						case "ConfigTargets":
							var contentBldr = new StringBuilder();
							foreach (var referenceListItem in referenceList)
							{
								var referenceItem = referenceListItem.Item2;
								var targetItem = referenceListItem.Item1;
								contentBldr.Append(GenerateCollectionItemContent(child, publicationDecorator, targetItem, referenceItem, settings));
							}
							if (contentBldr.Length > 0)
							{
								// targets
								settings.ContentGenerator.AddCollection(xw, IsBlockProperty(child), CssGenerator.GetClassAttributeForConfig(child), contentBldr.ToString());
							}
							break;
						case "OwnerType":
							// OwnerType is a LexRefType, some of which are asymmetric (e.g. Part/Whole). If this Type is symmetric or we are currently
							// working in the forward direction, the generic code will work; however, if we are working on an asymmetric LexRefType
							// in the reverse direction, we need to display the ReverseName or ReverseAbbreviation instead of the Name or Abbreviation.
							if (LexRefTypeTags.IsAsymmetric((LexRefTypeTags.MappingTypes)reference.OwnerType.MappingType) && LexRefDirection(reference, collectionOwner) == ":r")
							{
								// Changing the SubField changes the default CSS Class name.
								// If there is no override, override with the default before changing the SubField.
								if (string.IsNullOrEmpty(child.CSSClassNameOverride))
								{
									child.CSSClassNameOverride = CssGenerator.GetClassAttributeForConfig(child);
								}
								// Flag to prepend "Reverse" to child.SubField when it is used.
								settings.ContentGenerator.WriteProcessedContents(xw, GenerateXHTMLForFieldByReflection(reference, child, publicationDecorator, settings, fUseReverseSubField: true));
							}
							else
							{
								settings.ContentGenerator.WriteProcessedContents(xw, GenerateXHTMLForFieldByReflection(reference, child, publicationDecorator, settings));
							}
							break;
						default:
							throw new NotImplementedException("The field " + child.FieldDescription + " is not supported on Cross References or Lexical Relations. Supported fields are OwnerType and ConfigTargets");
					}
				}
				settings.ContentGenerator.EndCrossReference(xw); // config
				xw.Flush();
			}
			return xBldr.ToString();
		}

		private static string GenerateSubentryTypeChild(ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator, ILexEntry subEntry, object mainEntryOrSense, GeneratorSettings settings)
		{
			if (!config.IsEnabled)
			{
				return string.Empty;
			}
			var complexEntryRef = EntryRefForSubentry(subEntry, mainEntryOrSense);
			return complexEntryRef == null ? string.Empty : GenerateXHTMLForCollection(complexEntryRef.ComplexEntryTypesRS, config, publicationDecorator, subEntry, settings);
		}

		private static ILexEntryRef EntryRefForSubentry(ILexEntry subEntry, object mainEntryOrSense)
		{
			var mainEntry = mainEntryOrSense as ILexEntry ?? ((ILexSense)mainEntryOrSense).Entry;
			return subEntry.ComplexFormEntryRefs.FirstOrDefault(entryRef => entryRef.PrimaryLexemesRS.Contains(mainEntry) // subsubentries
																			|| entryRef.PrimaryEntryRoots.Contains(mainEntry)); // subs under sense
		}

		private static string GenerateSenseNumberSpanIfNeeded(ConfigurableDictionaryNode senseConfigNode, bool isThisSenseNumbered, ref SenseInfo info, GeneratorSettings settings)
		{
			if (!isThisSenseNumbered)
			{
				return string.Empty;
			}
			var senseOptions = (DictionaryNodeSenseOptions)senseConfigNode.DictionaryNodeOptions;
			var formattedSenseNumber = GetSenseNumber(senseOptions.NumberingStyle, ref info);
			return string.IsNullOrEmpty(formattedSenseNumber) ? string.Empty : settings.ContentGenerator.GenerateSenseNumber(formattedSenseNumber);
		}

		private static string GetSenseNumber(string numberingStyle, ref SenseInfo info)
		{
			string nextNumber;
			switch (numberingStyle)
			{
				case "%a":
				case "%A":
					nextNumber = GetAlphaSenseCounter(numberingStyle, info.SenseCounter);
					break;
				case "%i":
				case "%I":
					nextNumber = GetRomanSenseCounter(numberingStyle, info.SenseCounter);
					break;
				default: // handles %d and %O. We no longer support "%z" (1  b  iii) because users can hand-configure its equivalent
					nextNumber = info.SenseCounter.ToString();
					break;
			}
			info.SenseOutlineNumber = GenerateSenseOutlineNumber(info, nextNumber);
			return info.SenseOutlineNumber;
		}

		private static string GenerateSenseOutlineNumber(SenseInfo info, string nextNumber)
		{
			switch (info.ParentSenseNumberingStyle)
			{
				case "%j":
					info.SenseOutlineNumber = $"{info.SenseOutlineNumber}{nextNumber}";
					break;
				case "%.":
					info.SenseOutlineNumber = $"{info.SenseOutlineNumber}.{nextNumber}";
					break;
				default:
					info.SenseOutlineNumber = nextNumber;
					break;
			}
			return info.SenseOutlineNumber;
		}

		private static string GetAlphaSenseCounter(string numberingStyle, int senseNumber)
		{
			var asciiBytes = 64; // char 'A'
			asciiBytes = asciiBytes + senseNumber;
			var nextNumber = ((char)asciiBytes).ToString();
			if (numberingStyle == "%a")
			{
				nextNumber = nextNumber.ToLower();
			}
			return nextNumber;
		}

		private static string GetRomanSenseCounter(string numberingStyle, int senseNumber)
		{
			var roman = RomanNumerals.IntToRoman(senseNumber);
			if (numberingStyle == "%i")
			{
				roman = roman.ToLower();
			}
			return roman;
		}

		private static string GenerateXHTMLForICmObject(ICmObject propertyValue, ConfigurableDictionaryNode config, GeneratorSettings settings)
		{
			// Don't export if there is no such data
			if (propertyValue == null || config.ReferencedOrDirectChildren == null || !config.ReferencedOrDirectChildren.Any(node => node.IsEnabled))
			{
				return string.Empty;
			}
			var bldr = new StringBuilder();
			foreach (var child in config.ReferencedOrDirectChildren)
			{
				var content = GenerateXHTMLForFieldByReflection(propertyValue, child, null, settings);
				bldr.Append(content);
			}
			return bldr.Length > 0
				? settings.ContentGenerator.WriteProcessedObject(false, bldr.ToString(), GetClassNameAttributeForConfig(config))
				: string.Empty;
		}

		/// <summary>Write the class element in the span for an individual item in the collection</summary>
		private static string GetCollectionItemClassAttribute(ConfigurableDictionaryNode config)
		{
			var classAtt = CssGenerator.GetClassAttributeForCollectionItem(config);
			if (config.ReferencedNode != null)
			{
				classAtt = $"{classAtt} {CssGenerator.GetClassAttributeForCollectionItem(config.ReferencedNode)}";
			}
			return classAtt;
		}

		/// <summary>
		/// This method is used to determine if we need to iterate through a property and generate xhtml for each item
		/// </summary>
		internal static bool IsCollectionType(Type entryType)
		{
			// The collections we test here are generic collection types (e.g. IEnumerable<T>). Note: This (and other code) does not work for arrays.
			// We do have at least one collection type with at least two generic arguments; hence `> 0` instead of `== 1`
			return entryType.GetGenericArguments().Length > 0 || typeof(ILcmVector).IsAssignableFrom(entryType);
		}

		internal static bool IsCollectionNode(ConfigurableDictionaryNode configNode, IFwMetaDataCacheManaged metaDataCacheAccessor)
		{
			return GetPropertyTypeForConfigurationNode(configNode, metaDataCacheAccessor) == PropertyType.CollectionType;
		}

		/// <summary>
		/// Determines if the user has specified that this item should generate content.
		/// <returns><c>true</c> if the user has ticked the list item that applies to this object</returns>
		/// </summary>
		internal static bool IsListItemSelectedForExport(ConfigurableDictionaryNode config, object listItem, object parent = null)
		{
			var listOptions = config.DictionaryNodeOptions as DictionaryNodeListOptions;
			if (listOptions == null)
			{
				throw new ArgumentException($@"This configuration node had no options and we were expecting them: {config.DisplayLabel} ({config.FieldDescription})", nameof(config));
			}
			var selectedListOptions = new List<Guid>();
			var forwardReverseOptions = new List<Tuple<Guid, string>>();
			foreach (var option in listOptions.Options.Where(optn => optn.IsEnabled))
			{
				var forwardReverseIndicator = option.Id.IndexOf(':');
				if (forwardReverseIndicator > 0)
				{
					var guid = new Guid(option.Id.Substring(0, forwardReverseIndicator));
					forwardReverseOptions.Add(new Tuple<Guid, string>(guid, option.Id.Substring(forwardReverseIndicator)));
				}
				else
				{
					selectedListOptions.Add(new Guid(option.Id));
				}
			}
			switch (listOptions.ListId)
			{
				case ListIds.Variant:
				case ListIds.Complex:
				case ListIds.Minor:
				case ListIds.Note:
					return IsListItemSelectedForExportInternal(listOptions.ListId, listItem, selectedListOptions);
				case ListIds.Entry:
				case ListIds.Sense:
					var lexRef = (ILexReference)listItem;
					var entryTypeGuid = lexRef.OwnerType.Guid;
					if (selectedListOptions.Contains(entryTypeGuid))
					{
						return true;
					}
					var entryTypeGuidAndDirection = new Tuple<Guid, string>(entryTypeGuid, LexRefDirection(lexRef, parent));
					return forwardReverseOptions.Contains(entryTypeGuidAndDirection);
				case ListIds.None:
					return true;
				default:
					Debug.WriteLine("Unhandled list ID encountered: " + listOptions.ListId);
					return true;
			}
		}

		private static bool IsListItemSelectedForExportInternal(ListIds listId, object listItem, IEnumerable<Guid> selectedListOptions)
		{
			var entryTypeGuids = new HashSet<Guid>();
			switch (listItem)
			{
				case ILexEntryRef entryRef:
				{
					if (listId == ListIds.Variant || listId == ListIds.Minor)
					{
						GetVariantTypeGuidsForEntryRef(entryRef, entryTypeGuids);
					}
					if (listId == ListIds.Complex || listId == ListIds.Minor)
					{
						GetComplexFormTypeGuidsForEntryRef(entryRef, entryTypeGuids);
					}
					break;
				}
				case ILexEntry entry:
				{
					if (listId == ListIds.Variant || listId == ListIds.Minor)
					{
						foreach (var variantEntryRef in entry.VariantEntryRefs)
						{
							GetVariantTypeGuidsForEntryRef(variantEntryRef, entryTypeGuids);
						}
					}
					if (listId == ListIds.Complex || listId == ListIds.Minor)
					{
						foreach (var complexFormEntryRef in entry.ComplexFormEntryRefs)
						{
							GetComplexFormTypeGuidsForEntryRef(complexFormEntryRef, entryTypeGuids);
						}
					}
					break;
				}
				case ILexEntryType entryType:
					entryTypeGuids.Add(entryType.Guid);
					break;
				case ILexExtendedNote note:
				{
					if (listId == ListIds.Note)
					{
						GetExtendedNoteGuidsForEntryRef(note, entryTypeGuids);
					}
					break;
				}
			}
			return entryTypeGuids.Intersect(selectedListOptions).Any();
		}

		private static void GetVariantTypeGuidsForEntryRef(ILexEntryRef entryRef, HashSet<Guid> entryTypeGuids)
		{
			if (entryRef.VariantEntryTypesRS.Any())
			{
				entryTypeGuids.UnionWith(entryRef.VariantEntryTypesRS.Select(guid => guid.Guid));
			}
			else
			{
				entryTypeGuids.Add(XmlViewsUtils.GetGuidForUnspecifiedVariantType());
			}
		}

		private static void GetComplexFormTypeGuidsForEntryRef(ILexEntryRef entryRef, HashSet<Guid> entryTypeGuids)
		{
			if (entryRef.ComplexEntryTypesRS.Any())
			{
				entryTypeGuids.UnionWith(entryRef.ComplexEntryTypesRS.Select(guid => guid.Guid));
			}
			else
			{
				entryTypeGuids.Add(XmlViewsUtils.GetGuidForUnspecifiedComplexFormType());
			}
		}

		private static void GetExtendedNoteGuidsForEntryRef(ILexExtendedNote entryRef, HashSet<Guid> entryTypeGuids)
		{
			entryTypeGuids.Add(entryRef.ExtendedNoteTypeRA?.Guid ?? XmlViewsUtils.GetGuidForUnspecifiedExtendedNoteType());
		}

		/// <returns>
		/// ":f" if we are working in the forward direction (the parent is the head of a tree or asymmetric pair);
		/// ":r" if we are working in the reverse direction (the parent is a subordinate in a tree or asymmetric pair).
		/// </returns>
		/// <remarks>This method does not determine symmetry; use <see cref="LexRefTypeTags.IsAsymmetric"/> for that.</remarks>
		private static string LexRefDirection(ILexReference lexRef, object parent)
		{
			return Equals(lexRef.TargetsRS[0], parent) ? ":f" : ":r";
		}

		/// <summary>
		/// Returns true if the given collection is empty (type determined at runtime)
		/// </summary>
		private static bool IsCollectionEmpty(object collection)
		{
			switch (collection)
			{
				case null:
					throw new ArgumentNullException(nameof(collection));
				case IEnumerable enumerable:
					return !(enumerable.Cast<object>().Any());
				case ILcmVector lcmVector:
					return lcmVector.ToHvoArray().Length == 0;
				default:
					throw new ArgumentException(@"Cannot test something that isn't a collection", nameof(collection));
			}
		}

		/// <summary>
		/// This method generates XHTML content for a given object
		/// </summary>
		/// <param name="field">This is the object that owns the property, needed to look up writing system info for virtual string fields</param>
		/// <param name="propertyValue">data to generate xhtml for</param>
		/// <param name="config"></param>
		/// <param name="settings"></param>
		private static string GenerateXHTMLForValue(object field, object propertyValue, ConfigurableDictionaryNode config, GeneratorSettings settings)
		{
			// If we're working with a headword, either for this entry or another one (Variant or Complex Form, etc.), store that entry's GUID
			// so we can generate a link to the main or minor entry for this headword.
			var guid = Guid.Empty;
			if (config.IsHeadWord)
			{
				switch (field)
				{
					case ILexEntry lexEntry:
						guid = lexEntry.Guid;
						break;
					case ILexEntryRef lexEntryRef:
						guid = lexEntryRef.OwningEntry.Guid;
						break;
					case ISenseOrEntry senseOrEntry:
						guid = senseOrEntry.EntryGuid;
						break;
					case ILexSense lexSense:
						guid = lexSense.OwnerOfClass(LexEntryTags.kClassId).Guid;
						break;
					default:
						Debug.WriteLine($"Need to find Entry Guid for {field?.GetType().Name ?? DictionaryConfigurationServices.BuildPathStringFromNode(config)}");
						break;
				}
			}
			if (propertyValue is ITsString tsString)
			{
				if (TsStringUtils.IsNullOrEmpty(tsString))
				{
					return string.Empty;
				}
				var content = GenerateXHTMLForString(tsString, config, settings, guid);
				return !string.IsNullOrEmpty(content)
					? settings.ContentGenerator.WriteProcessedCollection(false, content, GetClassNameAttributeForConfig(config))
					: string.Empty;
			}
			switch (propertyValue)
			{
				case IMultiStringAccessor multiStringAccessor:
					return GenerateXHTMLForStrings(multiStringAccessor, config, settings, guid);
				case int asInt:
					return settings.ContentGenerator.AddProperty(GetClassNameAttributeForConfig(config), false, asInt.ToString());
				case DateTime dateTime:
					return settings.ContentGenerator.AddProperty(GetClassNameAttributeForConfig(config), false, dateTime.ToLongDateString());
				case GenDate genDate:
					return settings.ContentGenerator.AddProperty(GetClassNameAttributeForConfig(config), false, genDate.ToLongString());
				case IMultiAccessorBase multiAccessorBase:
					return field is ISenseOrEntry senseOrEntry
						? GenerateXHTMLForVirtualStrings(senseOrEntry.Item, multiAccessorBase, config, settings, guid)
						: GenerateXHTMLForVirtualStrings((ICmObject)field, multiAccessorBase, config, settings, guid);
				case string _:
					return settings.ContentGenerator.AddProperty(GetClassNameAttributeForConfig(config), false, propertyValue.ToString());
				case IStText stText:
				{
					var bldr = new StringBuilder();
					foreach (var para in stText.ParagraphsOS)
					{
							if (!(para is IStTxtPara stTxtPara))
							{
								continue;
							}
							var contentPara = GenerateXHTMLForString(stTxtPara.Contents, config, settings, guid);
						if (!string.IsNullOrEmpty(contentPara))
						{
							bldr.Append(contentPara);
							bldr.AppendLine();
						}
					}
					return bldr.Length > 0 ? settings.ContentGenerator.WriteProcessedObject(true, bldr.ToString(), null) : string.Empty;
				}
				default:
					Debug.WriteLine(propertyValue == null ? $"Bad configuration node: {DictionaryConfigurationServices.BuildPathStringFromNode(config)}" : $"What do I do with {propertyValue.GetType().Name}?");
					return string.Empty;
			}
		}

		private static string GenerateXHTMLForStrings(IMultiStringAccessor multiStringAccessor, ConfigurableDictionaryNode config, GeneratorSettings settings)
		{
			return GenerateXHTMLForStrings(multiStringAccessor, config, settings, Guid.Empty);
		}

		/// <summary>
		/// This method will generate an XHTML span with a string for each selected writing system in the
		/// DictionaryWritingSystemOptions of the configuration that also has data in the given IMultiStringAccessor
		/// </summary>
		private static string GenerateXHTMLForStrings(IMultiStringAccessor multiStringAccessor, ConfigurableDictionaryNode config, GeneratorSettings settings, Guid guid)
		{
			var wsOptions = config.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
			if (wsOptions == null)
			{
				throw new ArgumentException(@"Configuration nodes for MultiString fields should have WritingSystemOptions", nameof(config));
			}
			// TODO pH 2014.12: this can generate an empty span if no checked WS's contain data
			// gjm 2015.12 but this will help some (LT-16846)
			if (multiStringAccessor == null || multiStringAccessor.StringCount == 0)
			{
				return string.Empty;
			}
			var bldr = new StringBuilder();
			foreach (var option in wsOptions.Options)
			{
				if (!option.IsEnabled)
				{
					continue;
				}
				var wsId = WritingSystemServices.GetMagicWsIdFromName(option.Id);
				// The string for the specific wsId in the option, or the best string option in the accessor if the wsId is magic
				ITsString bestString;
				if (wsId == 0)
				{
					// This is not a magic writing system, so grab the user requested string
					wsId = settings.Cache.WritingSystemFactory.GetWsFromStr(option.Id);
					if (wsId == 0) // The config is bad or stale, but we don't need to crash in this instance.
					{
						Debug.WriteLine("Writing system requested that is not known in local store: {0}", option.Id);
						continue;
					}
					bestString = multiStringAccessor.get_String(wsId);
				}
				else
				{
					// Writing system is magic i.e. 'best vernacular' or 'first pronunciation'
					// use the method in the multi-string to get the right string and set wsId to the used one
					bestString = multiStringAccessor.GetAlternativeOrBestTss(wsId, out wsId);
				}
				var contentItem = GenerateWsPrefixAndString(config, settings, wsOptions, wsId, bestString, guid);

				if (!string.IsNullOrEmpty(contentItem))
				{
					bldr.Append(contentItem);
				}
			}
			return bldr.Length > 0
				? settings.ContentGenerator.WriteProcessedCollection(false, bldr.ToString(), GetClassNameAttributeForConfig(config))
				: string.Empty;
		}

		/// <summary>
		/// This method will generate an XHTML span with a string for each selected writing system in the
		/// DictionaryWritingSystemOptions of the configuration that also has data in the given IMultiAccessorBase
		/// </summary>
		private static string GenerateXHTMLForVirtualStrings(ICmObject owningObject, IMultiAccessorBase multiStringAccessor, ConfigurableDictionaryNode config, GeneratorSettings settings, Guid guid)
		{
			var wsOptions = config.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
			if (wsOptions == null)
			{
				throw new ArgumentException(@"Configuration nodes for MultiString fields should have WritingSystemOptions", nameof(config));
			}
			var bldr = new StringBuilder();
			foreach (var option in wsOptions.Options)
			{
				if (!option.IsEnabled)
				{
					continue;
				}
				var wsId = WritingSystemServices.GetMagicWsIdFromName(option.Id);
				// The string for the specific wsId in the option, or the best string option in the accessor if the wsId is magic
				if (wsId == 0)
				{
					// This is not a magic writing system, so grab the user requested string
					wsId = settings.Cache.WritingSystemFactory.GetWsFromStr(option.Id);
				}
				else
				{
					var defaultWs = owningObject.Cache.WritingSystemFactory.get_EngineOrNull(owningObject.Cache.DefaultUserWs);
					wsId = WritingSystemServices.InterpretWsLabel(owningObject.Cache, option.Id, (CoreWritingSystemDefinition)defaultWs,
																					owningObject.Hvo, multiStringAccessor.Flid, (CoreWritingSystemDefinition)defaultWs);
				}
				var requestedString = multiStringAccessor.get_String(wsId);
				bldr.Append(GenerateWsPrefixAndString(config, settings, wsOptions, wsId, requestedString, guid));
			}
			return bldr.Length > 0
				? settings.ContentGenerator.WriteProcessedCollection(false, bldr.ToString(), GetClassNameAttributeForConfig(config))
				: string.Empty;
		}

		private static string GenerateWsPrefixAndString(ConfigurableDictionaryNode config, GeneratorSettings settings, DictionaryNodeWritingSystemOptions wsOptions, int wsId, ITsString requestedString, Guid guid)
		{
			if (string.IsNullOrEmpty(requestedString.Text))
			{
				return string.Empty;
			}
			var wsName = settings.Cache.WritingSystemFactory.get_EngineOrNull(wsId).Id;
			var content = GenerateXHTMLForString(requestedString, config, settings, guid, wsName);
			return string.IsNullOrEmpty(content)
				? string.Empty
				: settings.ContentGenerator.GenerateWsPrefixWithString(settings, wsOptions.DisplayWritingSystemAbbreviations, wsId, content);
		}

		private static string GenerateXHTMLForString(ITsString fieldValue, ConfigurableDictionaryNode config, GeneratorSettings settings, string writingSystem = null)
		{
			return GenerateXHTMLForString(fieldValue, config, settings, Guid.Empty, writingSystem);
		}

		private static string GenerateXHTMLForString(ITsString fieldValue, ConfigurableDictionaryNode config, GeneratorSettings settings, Guid linkTarget, string writingSystem = null)
		{
			if (TsStringUtils.IsNullOrEmpty(fieldValue))
			{
				return string.Empty;
			}
			if (writingSystem != null && writingSystem.Contains("audio"))
			{
				var fieldText = fieldValue.Text;
				if (fieldText.Contains("."))
				{
					var audioId = fieldText.Substring(0, fieldText.IndexOf(".", StringComparison.Ordinal));
					var srcAttr = GenerateSrcAttributeForMediaFromFilePath(fieldText, "AudioVisual", settings);
					var fileContent = GenerateXHTMLForAudioFile(writingSystem, audioId, srcAttr, string.Empty, settings);
					var content = GenerateAudioWsContent(writingSystem, linkTarget, fileContent, settings);
					if (!string.IsNullOrEmpty(content))
					{
						return settings.ContentGenerator.WriteProcessedObject(false, content, null);
					}
				}
			}
			else if (config.IsCustomField && IsUSFM(fieldValue.Text))
			{
				return GenerateTablesFromUSFM(fieldValue, config, settings, writingSystem);
			}
			else
			{
				// use the passed in writing system unless null
				// otherwise use the first option from the DictionaryNodeWritingSystemOptions or english if the options are null
				var bldr = new StringBuilder();
				try
				{
					using (var writer = settings.ContentGenerator.CreateWriter(bldr))
					{
						var rightToLeft = settings.RightToLeft;
						if (fieldValue.RunCount > 1)
						{
							writingSystem = writingSystem ?? GetLanguageFromFirstOption(config.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions, settings.Cache);
							settings.ContentGenerator.StartMultiRunString(writer, writingSystem);
							var wsRtl = settings.Cache.WritingSystemFactory.get_Engine(writingSystem).RightToLeftScript;
							if (rightToLeft != wsRtl)
							{
								rightToLeft = wsRtl; // the outer WS direction will be used to identify embedded runs of the opposite direction.
								settings.ContentGenerator.StartBiDiWrapper(writer, rightToLeft);
							}
						}
						for (var i = 0; i < fieldValue.RunCount; i++)
						{
							var text = fieldValue.get_RunText(i);

							// If the text is "<Not Sure>" then don't display any text.
							if (text == LCModelStrings.NotSure)
								text = String.Empty;

							var props = fieldValue.get_Properties(i);
							var style = props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
							writingSystem = settings.Cache.WritingSystemFactory.GetStrFromWs(fieldValue.get_WritingSystem(i));
							GenerateRunWithPossibleLink(settings, writingSystem, writer, style, text, linkTarget, rightToLeft);
						}

						if (fieldValue.RunCount > 1)
						{
							if (rightToLeft != settings.RightToLeft)
							{
                                settings.ContentGenerator.EndBiDiWrapper(writer);
                            }
							settings.ContentGenerator.EndMultiRunString(writer);
						}
						writer.Flush();
						return bldr.ToString();
					}
				}
				catch (Exception)
				{
					// We had some sort of error processing the string, possibly an unmatched surrogate pair.
					// Generate a span with 3 invalid unicode markers and an xml comment instead.
					var badStrBuilder = new StringBuilder();
					var unicodeChars = StringInfo.GetTextElementEnumerator(fieldValue.Text);
					while (unicodeChars.MoveNext())
					{
						if (unicodeChars.GetTextElement().Length == 1 && char.IsSurrogate(unicodeChars.GetTextElement().ToCharArray()[0]))
						{
							badStrBuilder.Append("\u0FFF"); // Generate the 'character not found' char in place of the bad surrogate
						}
						else
						{
							badStrBuilder.Append(unicodeChars.GetTextElement());
						}
					}
<<<<<<< HEAD:Src/LanguageExplorer/DictionaryConfiguration/ConfiguredLcmGenerator.cs
					//FIXME: The error content here needs to come from the settings.ContentGenerator implementation (won't work for json)
					return $"<span>\u0FFF\u0FFF\u0FFF<!-- Error generating content for string: '{badStrBuilder}' invalid surrogate pairs replaced with \\u0fff --></span>";
||||||| f013144d5:Src/xWorks/ConfiguredLcmGenerator.cs
					//FIXME: The error content here needs to come from the settings.ContentGenerator implementation (won't work for json)
					return string.Format("<span>\u0FFF\u0FFF\u0FFF<!-- Error generating content for string: '{0}' invalid surrogate pairs replaced with \\u0fff --></span>",
						badStrBuilder);
=======

					return settings.ContentGenerator.GenerateErrorContent(badStrBuilder);
>>>>>>> develop:Src/xWorks/ConfiguredLcmGenerator.cs
				}
			}
			return string.Empty;
		}

		private static string GenerateAudioWsContent(string wsId, Guid linkTarget, string fileContent, GeneratorSettings settings)
		{
			return settings.ContentGenerator.AddAudioWsContent(wsId, linkTarget, fileContent);
		}

		private static void GenerateRunWithPossibleLink(GeneratorSettings settings, string writingSystem, IFragmentWriter writer, string style, string text, Guid linkDestination, bool rightToLeft)
		{
			settings.ContentGenerator.StartRun(writer, writingSystem);
			var wsRtl = settings.Cache.WritingSystemFactory.get_Engine(writingSystem).RightToLeftScript;
			if (rightToLeft != wsRtl)
			{
				settings.ContentGenerator.StartBiDiWrapper(writer, wsRtl);
			}
			if (!string.IsNullOrEmpty(style))
			{
				var wsId = settings.Cache.WritingSystemFactory.GetWsFromStr(writingSystem);
				var cssStyle = CssGenerator.GenerateCssStyleFromLcmStyleSheet(style, wsId, FwUtils.StyleSheetFromPropertyTable(settings.ReadOnlyPropertyTable), settings.Cache.ServiceLocator.WritingSystemManager.get_EngineOrNull(wsId));
				var css = cssStyle.ToString();
				if (!string.IsNullOrEmpty(css))
				{
					settings.ContentGenerator.SetRunStyle(writer, css);
				}
			}
			if (linkDestination != Guid.Empty)
			{
				settings.ContentGenerator.StartLink(writer, linkDestination);
			}
<<<<<<< HEAD:Src/LanguageExplorer/DictionaryConfiguration/ConfiguredLcmGenerator.cs
			const char txtlineSplit = (char)8232; //Line-Separator Decimal Code
			if (text.Contains(txtlineSplit))
||||||| f013144d5:Src/xWorks/ConfiguredLcmGenerator.cs
			const char txtlineSplit = (Char)8232; //Line-Seperator Decimal Code
			if (text.Contains(txtlineSplit))
=======
			if (text.Contains(TxtLineSplit))
>>>>>>> develop:Src/xWorks/ConfiguredLcmGenerator.cs
			{
<<<<<<< HEAD:Src/LanguageExplorer/DictionaryConfiguration/ConfiguredLcmGenerator.cs
				var txtContents = text.Split(txtlineSplit);
				for (var i = 0; i < txtContents.Count(); i++)
||||||| f013144d5:Src/xWorks/ConfiguredLcmGenerator.cs
				var txtContents = text.Split(txtlineSplit);
				for (int i = 0; i < txtContents.Count(); i++)
=======
				var txtContents = text.Split(TxtLineSplit);
				for (int i = 0; i < txtContents.Count(); i++)
>>>>>>> develop:Src/xWorks/ConfiguredLcmGenerator.cs
				{
					settings.ContentGenerator.AddToRunContent(writer, txtContents[i]);
					if (i == txtContents.Count() - 1)
					{
						break;
					}
					settings.ContentGenerator.AddLineBreakInRunContent(writer);
				}
			}
			else
			{
				settings.ContentGenerator.AddToRunContent(writer, text);
			}
			if (linkDestination != Guid.Empty)
			{
				settings.ContentGenerator.EndLink(writer);
			}
			if (rightToLeft != wsRtl)
			{
				settings.ContentGenerator.EndBiDiWrapper(writer);
			}
			settings.ContentGenerator.EndRun(writer);
		}

		/// <param name="classname">value for class attribute for audio tag</param>
		/// <param name="audioId">value for Id attribute for audio tag</param>
		/// <param name="srcAttribute">Source location path for audio file</param>
<<<<<<< HEAD:Src/LanguageExplorer/DictionaryConfiguration/ConfiguredLcmGenerator.cs
		/// <param name="caption">Innertext for hyperlink</param>
		/// <param name="settings"></param>
		/// <returns></returns>
		private static string GenerateXHTMLForAudioFile(string classname, string audioId, string srcAttribute, string caption, GeneratorSettings settings)
||||||| f013144d5:Src/xWorks/ConfiguredLcmGenerator.cs
		/// <param name="caption">Innertext for hyperlink</param>
		/// <returns></returns>
		private static string GenerateXHTMLForAudioFile(string classname,
			string audioId, string srcAttribute, string caption, GeneratorSettings settings)
=======
		/// <param name="audioIcon">Inner text for hyperlink (unicode icon for audio)</param>
		/// <param name="settings"/>
		private static string GenerateXHTMLForAudioFile(string classname,
			string audioId, string srcAttribute, string audioIcon, GeneratorSettings settings)
>>>>>>> develop:Src/xWorks/ConfiguredLcmGenerator.cs
		{
<<<<<<< HEAD:Src/LanguageExplorer/DictionaryConfiguration/ConfiguredLcmGenerator.cs
			if (string.IsNullOrEmpty(audioId) && string.IsNullOrEmpty(srcAttribute) && string.IsNullOrEmpty(caption))
			{
				return string.Empty;
			}
			return settings.ContentGenerator.GenerateAudioLinkContent(classname, srcAttribute, caption, GetSafeXHTMLId(audioId));
||||||| f013144d5:Src/xWorks/ConfiguredLcmGenerator.cs
			if (String.IsNullOrEmpty(audioId) && String.IsNullOrEmpty(srcAttribute) && String.IsNullOrEmpty(caption))
				return String.Empty;
			var safeAudioId = GetSafeXHTMLId(audioId);
			return settings.ContentGenerator.GenerateAudioLinkContent(classname, srcAttribute, caption, safeAudioId);
=======
			if (string.IsNullOrEmpty(audioId) && string.IsNullOrEmpty(srcAttribute) && string.IsNullOrEmpty(audioIcon))
				return string.Empty;
			var safeAudioId = GetSafeXHTMLId(audioId);
			return settings.ContentGenerator.GenerateAudioLinkContent(classname, srcAttribute, audioIcon, safeAudioId);
>>>>>>> develop:Src/xWorks/ConfiguredLcmGenerator.cs
		}

		private static string GetSafeXHTMLId(string audioId)
		{
			// Prepend a letter, since some filenames start with digits, which gives an invalid id
			// Are there other characters that are unsafe in XHTML Ids or Javascript?
			return "g" + audioId.Replace(" ", "_").Replace("'", "_");
		}

		/// <summary>
		/// Determines whether the candidate string should be parsed as USFM.
		/// As of 2021.06, only strings beginning with \d (descriptive title) or \tr (table row) are supported by this code.
		/// </summary>
		private static bool IsUSFM(string candidate)
		{
			return USFMTableStart.IsMatch(candidate);
		}

		private static string GenerateTablesFromUSFM(ITsString usfm, ConfigurableDictionaryNode config, GeneratorSettings settings, string writingSystem)
		{
			var delimiters = new Regex(@"\\d\s").Matches(usfm.Text);

			// If there is only one table, generate it
			if (delimiters.Count == 0 || delimiters.Count == 1 && delimiters[0].Index == 0)
			{
				return GenerateTableFromUSFM(usfm, config, settings, writingSystem);
			}

			var bldr = new StringBuilder();
			// If there is a table before the first title, generate it
			if (delimiters[0].Index > 0)
			{
				bldr.Append(GenerateTableFromUSFM(usfm.GetSubstring(0, delimiters[0].Index), config, settings, writingSystem));
			}

			for (var i = 0; i < delimiters.Count; i++)
			{
				var lim = i == delimiters.Count - 1 ? usfm.Length : delimiters[i + 1].Index;
				bldr.Append(GenerateTableFromUSFM(usfm.GetSubstring(delimiters[i].Index, lim), config, settings, writingSystem));
			}

			return bldr.ToString();
		}

		private static string GenerateTableFromUSFM(ITsString usfm, ConfigurableDictionaryNode config, GeneratorSettings settings, string writingSystem)
		{
			var bldr = new StringBuilder();
			using (var writer = settings.ContentGenerator.CreateWriter(bldr))
			{
				// Regular expression to match the end of a string or a table row marker at the end of a title or row
				const string usfmRowTerminator = @"(?=\\tr\s|$)";

				// Regular expression to match at the beginning of the string a \d followed by one or more spaces
				// then grouping any number of characters as 'contents' until encountering any number of spaces followed
				// by \tr or the end of the string
				const string usfmHeaderGroup = @"(?<header>\A\\d\s+(?<contents>.*?)\s*" + usfmRowTerminator + ")";

				// Match the header optionally, then capture any contents found between \tr tags or between \tr and the end of the string
				// in groups labeled 'rowcontents' - The header including the sfm and spaces is captured as header and the row with the
				// sfm and surrounding space is captured as <row>
				var usfmTableRegEx = new Regex(usfmHeaderGroup + @"?(?<row>\\tr\s+(?<rowcontents>.*?)" + usfmRowTerminator + ")?", RegexOptions.Compiled | RegexOptions.Singleline);
				var usfmText = usfm.Text;
				var fancyMatch = usfmTableRegEx.Matches(usfmText);
				var headerContent = fancyMatch.Count > 0 && fancyMatch[0].Groups["contents"].Success ? fancyMatch[0].Groups["contents"].Captures[0] : null;
				var rows = from Match match in fancyMatch
					where match.Success && match.Groups["rowcontents"].Success
					select match.Groups["rowcontents"] into rowContentsGroup
					select new Tuple<int, int>(rowContentsGroup.Index, rowContentsGroup.Index + rowContentsGroup.Length);

				settings.ContentGenerator.StartTable(writer);
				if (headerContent != null && headerContent.Length > 0)
				{
					var title = usfm.GetSubstring(headerContent.Index, headerContent.Index + headerContent.Length);
					GenerateTableTitle(title, writer, config, settings, writingSystem);
				}
				settings.ContentGenerator.StartTableBody(writer);
				foreach(var row in rows)
				{
					GenerateTableRow(usfm.GetSubstring(row.Item1, row.Item2), writer, config, settings, writingSystem);
				}
				settings.ContentGenerator.EndTableBody(writer);
				settings.ContentGenerator.EndTable(writer);
				writer.Flush();
			}
			return bldr.ToString();
			// TODO (Hasso) 2021.06: impl for JSON
		}

		/// <summary>
		/// Generate the table title from USFM (\d descriptive title in USFM)
		/// </summary>
		private static void GenerateTableTitle(ITsString title, IFragmentWriter writer,
			ConfigurableDictionaryNode config, GeneratorSettings settings, string writingSystem)
		{
			settings.ContentGenerator.AddTableTitle(writer, GenerateXHTMLForString(title, config, settings, writingSystem));
		}

		/// <remarks>
		/// rowUSFM should have at least one leading whitespace character so that the regular expression matches the first \tc# or \th#
		/// </remarks>>
		private static void GenerateTableRow(ITsString rowUSFM, IFragmentWriter writer,
			ConfigurableDictionaryNode config, GeneratorSettings settings, string writingSystem)
		{
			var usfmText = rowUSFM.Text;
			if (string.IsNullOrEmpty(usfmText))
			{
				return;
			}

			settings.ContentGenerator.StartTableRow(writer);
			var rowToCellsRegex = new Regex(
				@"\\t(?<tag>c|h)(?<align>r|c|l)?((?<min>\d+)(-(?<lim>\d+))?)?(\s+|$)(?<content>.*?)\s*(?=\\t(c|h)(r|c|l)?(\d+(-\d+)?)?\s|$)",
				RegexOptions.Compiled | RegexOptions.Singleline);
			var cellMatches = rowToCellsRegex.Matches(usfmText);
			var cells = new List<Match>(from Match match in cellMatches where match.Success && match.Groups["content"].Success select match);

			// check for extra text before the first cell
			if (cells.Count == 0 || cells[0].Index > 0)
			{
				var junk = cells.Count == 0 ? usfmText : usfmText.Remove(cells[0].Index).Trim();
				if (new Regex(@"\A\\(t((h|c)(r|c|l)?(\d+(-\d*)?)?)?)?$").IsMatch(junk))
				{
					// The user seems to be starting to type a valid marker; call attention to its location
					GenerateError(junk, writer, settings);
				}
				else
				{
					// Yes, this strips all WS and formatting information, but for an error message, I'm not sure that we care
					GenerateError(string.Format(xWorksStrings.InvalidUSFM_TextAfterTR, junk), writer, settings);
				}
			}

			foreach (var cell in cells)
			{
				var contentsGroup = cell.Groups["content"];
				var cellLim = contentsGroup.Index + contentsGroup.Length;
				var contentXHTML = GenerateXHTMLForString(rowUSFM.GetSubstring(contentsGroup.Index, cellLim), config, settings, writingSystem);
				var alignment = HorizontalAlign.NotSet;
				if (cell.Groups["align"].Success)
				{
					switch (cell.Groups["align"].Value)
					{
						case "r":
							alignment = HorizontalAlign.Right;
							break;
						case "c":
							alignment = HorizontalAlign.Center;
							break;
						case "l":
							alignment = HorizontalAlign.Left;
							break;
					}
				}

				var colSpan = 1;
				if (cell.Groups["lim"].Success)
				{
					// Add one because the range includes both ends
					colSpan = int.Parse(cell.Groups["lim"].Value) - int.Parse(cell.Groups["min"].Value) + 1;
				}
				settings.ContentGenerator.AddTableCell(writer, cell.Groups["tag"].Value.Equals("h"), colSpan, alignment, contentXHTML);
			}
			settings.ContentGenerator.EndTableRow(writer);
		}

		private static void GenerateError(string text, IFragmentWriter writer, GeneratorSettings settings)
		{
			var writingSystem = settings.Cache.WritingSystemFactory.GetStrFromWs(settings.Cache.WritingSystemFactory.UserWs);
			settings.ContentGenerator.StartRun(writer, writingSystem);
			// Make the error red and slightly larger than the surrounding text
			var css = new StyleDeclaration
			{
				new ExCSS.Property("color") { Term = new HtmlColor(222, 0, 0) },
				new ExCSS.Property("font-size") { Term = new PrimitiveTerm(UnitType.Ems, 1.5f) }
			};
			settings.ContentGenerator.SetRunStyle(writer, css.ToString());
			if (text.Contains(TxtLineSplit))
			{
				var txtContents = text.Split(TxtLineSplit);
				for (var i = 0; i < txtContents.Length; i++)
				{
					settings.ContentGenerator.AddToRunContent(writer, txtContents[i]);
					if (i == txtContents.Length - 1)
						break;
					settings.ContentGenerator.AddLineBreakInRunContent(writer);
				}
			}
			else
			{
				settings.ContentGenerator.AddToRunContent(writer, text);
			}
			settings.ContentGenerator.EndRun(writer);
		}

		internal static bool IsBlockProperty(ConfigurableDictionaryNode config)
		{
			//TODO: Improve this logic to deal with subentries if necessary
			return config.FieldDescription.Equals("LexEntry") || config.DictionaryNodeOptions is DictionaryNodePictureOptions;
		}

		/// <summary>
		/// This method returns the lang attribute value from the first selected writing system in the given options.
		/// </summary>
		private static string GetLanguageFromFirstOption(DictionaryNodeWritingSystemOptions wsOptions, LcmCache cache)
		{
			const string defaultLang = "en";
			if (wsOptions == null)
			{
				return defaultLang;
			}
			foreach (var option in wsOptions.Options)
			{
				if (option.IsEnabled)
				{
					var wsId = WritingSystemServices.GetMagicWsIdFromName(option.Id);
					// if the writing system isn't a magic name just use it
					if (wsId == 0)
					{
						return option.Id;
					}
					// otherwise get a list of the writing systems for the magic name, and use the first one
					return WritingSystemServices.GetWritingSystemList(cache, wsId, true).First().Id;
				}
			}
			// paranoid fallback to first option of the list in case there are no enabled options
			return wsOptions.Options[0].Id;
		}

		internal static DictionaryPublicationDecorator GetPublicationDecoratorAndEntries(IPropertyTable propertyTable, out int[] entriesToSave, string dictionaryType, LcmCache cache, IRecordList activeRecordList)
		{
			Guard.AgainstNull(cache, nameof(cache));
			Guard.AgainstNull(activeRecordList, nameof(activeRecordList));
			var currentPublicationString = propertyTable.GetValue(LanguageExplorerConstants.SelectedPublication, LanguageExplorerResources.AllEntriesPublication);
			var currentPublication = currentPublicationString == LanguageExplorerResources.AllEntriesPublication ? null : (cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Where(item => item.Name.UserDefaultWritingSystem.Text == currentPublicationString)).FirstOrDefault();
			var decorator = new DictionaryPublicationDecorator(cache, activeRecordList.VirtualListPublisher, activeRecordList.VirtualFlid, currentPublication);
			entriesToSave = decorator.GetEntriesToPublish(propertyTable, activeRecordList.VirtualFlid, dictionaryType);
			return decorator;
		}
<<<<<<< HEAD:Src/LanguageExplorer/DictionaryConfiguration/ConfiguredLcmGenerator.cs
||||||| f013144d5:Src/xWorks/ConfiguredLcmGenerator.cs

		public class GeneratorSettings
		{
			public ILcmContentGenerator ContentGenerator = new LcmXhtmlGenerator();
			public LcmCache Cache { get; }
			public ReadOnlyPropertyTable PropertyTable { get; }
			public bool UseRelativePaths { get; }
			public bool CopyFiles { get; }
			public string ExportPath { get; }
			public bool RightToLeft { get; }
			public bool IsWebExport { get; }
			public bool IsTemplate { get; }

			public GeneratorSettings(LcmCache cache, PropertyTable propertyTable, bool relativePaths,bool copyFiles, string exportPath, bool rightToLeft = false, bool isWebExport = false)
				: this(cache, propertyTable == null ? null : new ReadOnlyPropertyTable(propertyTable), relativePaths, copyFiles, exportPath, rightToLeft, isWebExport)
			{
			}


			public GeneratorSettings(LcmCache cache, ReadOnlyPropertyTable propertyTable, bool relativePaths, bool copyFiles, string exportPath, bool rightToLeft = false, bool isWebExport = false, bool isTemplate = false)
			{
				if (cache == null || propertyTable == null)
				{
					throw new ArgumentNullException();
				}
				Cache = cache;
				PropertyTable = propertyTable;
				UseRelativePaths = relativePaths;
				CopyFiles = copyFiles;
				ExportPath = exportPath;
				RightToLeft = rightToLeft;
				IsWebExport = isWebExport;
				IsTemplate = isTemplate;
			}
		}

		/// <remarks>
		/// Presently, this handles only Sense Info, but if other info needs to be handed down the call stack in the future, we could rename this
		/// </remarks>
		internal struct SenseInfo
		{
			public int SenseCounter { get; set; }
			public string SenseOutlineNumber { get; set; }
			public string ParentSenseNumberingStyle { get; set; }
		}
=======

		/// <summary>
		/// Determines if Variant Type comes before or after Variant Form.
		/// </summary>
		/// <param name="config"></param>
		/// <returns>Returns True if Variant Type is before Variant Form.</returns>
		private static bool IsTypeBeforeForm(ConfigurableDictionaryNode config)
		{
			bool typeBefore = true;

			// Determine if 'Variant Type' should be before or after 'Variant Form'.
			ConfigurableDictionaryNode node = null;
			var variantOptions = config.DictionaryNodeOptions as DictionaryNodeListOptions;
			if (variantOptions != null && variantOptions.ListId == DictionaryNodeListOptions.ListIds.Variant)
			{
				node = config.ReferencedOrDirectChildren.FirstOrDefault(x => ((x.FieldDescription == "VariantEntryTypesRS") || (x.FieldDescription == "OwningEntry")));
				if (node != null)
				{
					if (node.FieldDescription == "OwningEntry")
					{
						typeBefore = false;
					}
				}
			}

			return typeBefore;
		}

		public class GeneratorSettings
		{
			public ILcmContentGenerator ContentGenerator = new LcmXhtmlGenerator();
			public LcmCache Cache { get; }
			public ReadOnlyPropertyTable PropertyTable { get; }
			public bool UseRelativePaths { get; }
			public bool CopyFiles { get; }
			public string ExportPath { get; }
			public bool RightToLeft { get; }
			public bool IsWebExport { get; }
			public bool IsTemplate { get; }

			public GeneratorSettings(LcmCache cache, PropertyTable propertyTable, bool relativePaths,bool copyFiles, string exportPath, bool rightToLeft = false, bool isWebExport = false)
				: this(cache, propertyTable == null ? null : new ReadOnlyPropertyTable(propertyTable), relativePaths, copyFiles, exportPath, rightToLeft, isWebExport)
			{
			}


			public GeneratorSettings(LcmCache cache, ReadOnlyPropertyTable propertyTable, bool relativePaths, bool copyFiles, string exportPath, bool rightToLeft = false, bool isWebExport = false, bool isTemplate = false)
			{
				if (cache == null || propertyTable == null)
				{
					throw new ArgumentNullException();
				}
				Cache = cache;
				PropertyTable = propertyTable;
				UseRelativePaths = relativePaths;
				CopyFiles = copyFiles;
				ExportPath = exportPath;
				RightToLeft = rightToLeft;
				IsWebExport = isWebExport;
				IsTemplate = isTemplate;
			}
		}

		/// <remarks>
		/// Presently, this handles only Sense Info, but if other info needs to be handed down the call stack in the future, we could rename this
		/// </remarks>
		internal struct SenseInfo
		{
			public int SenseCounter { get; set; }
			public string SenseOutlineNumber { get; set; }
			public string ParentSenseNumberingStyle { get; set; }
		}
>>>>>>> develop:Src/xWorks/ConfiguredLcmGenerator.cs
	}
}