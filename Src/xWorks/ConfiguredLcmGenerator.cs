// Copyright (c) 2014-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using ExCSS;
using Icu.Collation;
using SIL.Code;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainImpl;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;
using SIL.Reporting;
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
using System.Web.UI.WebControls;
using XCore;
using UnitType = ExCSS.UnitType;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class groups the static methods used for generating XHTML, according to specified configurations, from Fieldworks model objects
	/// </summary>
	public static class ConfiguredLcmGenerator
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

		/// <summary>
		/// Static initializer setting the AssemblyFile to the default LCM dll.
		/// </summary>
		static ConfiguredLcmGenerator()
		{
			Init();
			EntriesToAddCount = 5;
		}

		/// <summary>
		/// Sets initial values (or resets them after tests)
		/// </summary>
		internal static void Init()
		{
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
		}

		private static bool IsCanceling(IThreadedProgress progress)
		{
			return progress != null && progress.IsCanceling;
		}

		internal static StringBuilder GenerateLetterHeaderIfNeeded(ICmObject entry,
			ref string lastHeader, Collator headwordWsCollator,
			ConfiguredLcmGenerator.GeneratorSettings settings, RecordClerk clerk = null)
		{
			// If performance is an issue these dummies can be stored between calls
			var dummyOne =
				new Dictionary<string, Dictionary<string, ConfiguredExport.CollationLevel>>();
			var dummyTwo = new Dictionary<string, Dictionary<string, string>>();
			var dummyThree = new Dictionary<string, ISet<string>>();
			var cache = settings.Cache;
			var wsString = ConfiguredLcmGenerator.GetWsForEntryType(entry, cache);
			var firstLetter = ConfiguredExport.GetLeadChar(
				ConfiguredLcmGenerator.GetSortWordForLetterHead(entry, clerk), wsString, dummyOne,
				dummyTwo, dummyThree,
				headwordWsCollator, cache);
			if (firstLetter != lastHeader && !string.IsNullOrEmpty(firstLetter))
			{
				var headerTextBuilder = new StringBuilder();
				var upperCase =
					new CaseFunctions(cache.ServiceLocator.WritingSystemManager.Get(wsString))
						.ToTitle(firstLetter);
				var lowerCase = firstLetter.Normalize();
				headerTextBuilder.Append(upperCase);
				if (lowerCase != upperCase)
				{
					headerTextBuilder.Append(' ');
					headerTextBuilder.Append(lowerCase);
				}
				lastHeader = firstLetter;

				return headerTextBuilder;
			}

			return new StringBuilder("");
		}

		/// <summary>
		/// This method uses a ThreadPool to execute the given individualActions in parallel.
		/// It waits for all the individualActions to complete and then returns.
		/// </summary>
		internal static void SpawnEntryGenerationThreadsAndWait(List<Action> individualActions, IThreadedProgress progress)
		{
			var actionCount = individualActions.Count;
			//Note that our COM classes all implement the STA threading model, while the ThreadPool always uses MTA model threads.
			//I don't understand why using the ThreadPool sometimes works, but not always.  Expliciting allocating STA model
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
					throw new WorkerThreadException("Exception generating Configured XHTML", exceptionThrown);
			}
		}

		/// <summary>
		/// Get the sort word that will be used to generate the letter headings. The sort word can come from a different
		/// field depending on the sort column.
		/// </summary>
		/// <returns>the sort word in NFD (the heading letter must be normalized to NFC before writing to XHTML, per LT-18177)</returns>
		internal static string GetSortWordForLetterHead(ICmObject entry, RecordClerk clerk)
		{
			var lexEntry = entry as ILexEntry;

			// Reversal Indexes - We are always using the same sorting, regardless of the sort column that
			// was selected.  So always return the same word for the letter head.
			if (lexEntry == null)
			{
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
			}

			// Headword - Default to using the "Headword" sort word.
			return  lexEntry.HomographForm.TrimStart();
		}

		/// <summary>
		/// Get the writing system string for a LexEntry or an IReversalIndexEntry
		/// </summary>
		internal static string GetWsForEntryType(ICmObject entry, LcmCache cache)
		{
			var wsString = cache.WritingSystemFactory.GetStrFromWs(cache.DefaultVernWs);
			if (entry is IReversalIndexEntry revEntry)
				wsString = revEntry.SortKeyWs;
			return wsString;
		}

		/// <summary>
		/// Generating the xhtml representation for the given ICmObject using the given configuration node to select which data to write out
		/// If it is a Dictionary Main Entry or non-Dictionary entry, uses the first configuration node.
		/// If it is a Minor Entry, first checks whether the entry should be published as a Minor Entry; then, generates XHTML for each applicable
		/// Minor Entry configuration node.
		/// </summary>
		public static IFragment GenerateContentForEntry(ICmObject entryObj, DictionaryConfigurationModel configuration,
			DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings, int index = -1)
		{
			if (IsMainEntry(entryObj, configuration))
				return GenerateContentForMainEntry(entryObj, configuration.Parts[0], publicationDecorator, settings, index);

			var entry = (ILexEntry)entryObj;
			return entry.PublishAsMinorEntry
				? GenerateContentForMinorEntry(entry, configuration, publicationDecorator, settings, index)
				: settings.ContentGenerator.CreateFragment();
		}

		public static IFragment GenerateContentForMainEntry(ICmObject entry, ConfigurableDictionaryNode configuration,
			DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings, int index)
		{
			if (configuration.DictionaryNodeOptions != null && ((ILexEntry)entry).ComplexFormEntryRefs.Any() && !IsListItemSelectedForExport(configuration, entry))
				return settings.ContentGenerator.CreateFragment();
			return GenerateContentForEntry(entry, configuration, publicationDecorator, settings, index);
		}

		private static IFragment GenerateContentForMinorEntry(ICmObject entry, DictionaryConfigurationModel configuration,
			DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings, int index)
		{
			// LT-15232: show minor entries using only the last applicable Minor Entry node (not more than once)
			var applicablePart = configuration.Parts.Skip(1).LastOrDefault(part => IsListItemSelectedForExport(part, entry));
			return applicablePart == null ? settings.ContentGenerator.CreateFragment() : GenerateContentForEntry(entry, applicablePart, publicationDecorator, settings, index);
		}

		/// <summary>
		/// If entry is a a Main Entry
		/// For Root-based configs, this means the entry is neither a Variant nor a Complex Form.
		/// For Lexeme-based configs, Complex Forms are considered Main Entries but Variants are not.
		/// </summary>
		internal static bool IsMainEntry(ICmObject entry, DictionaryConfigurationModel config)
		{
			var lexEntry = entry as ILexEntry;
			if (lexEntry == null // only LexEntries can be Minor; others (ReversalIndex, etc) are always Main.
				|| !lexEntry.EntryRefsOS.Any()) // owning an ILexEntryRef denotes Complex Forms or Variants (not owning any denotes Main Entries)
				return true;
			if (config.IsRootBased) // Root-based configs consider all Complex Forms and Variants to be Minor Entries
				return false;
			// Lexeme-Based and Hybrid configs consider Complex Forms to be Main Entries (Variants are still Minor Entries)
			return lexEntry.EntryRefsOS.Any(ler => ler.RefType == LexEntryRefTags.krtComplexForm);
		}

		/// <summary>Generates content with the GeneratorSettings.ContentGenerator for an ICmObject for a specific ConfigurableDictionaryNode</summary>
		/// <remarks>the configuration node must match the entry type</remarks>
		internal static IFragment GenerateContentForEntry(ICmObject entry, ConfigurableDictionaryNode configuration,
			DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings, int index = -1)
		{
			Guard.AgainstNull(settings, nameof(settings));
			Guard.AgainstNull(configuration, nameof(configuration));
			Guard.AgainstNull(entry, nameof(entry));

			try
			{
				// ReSharper disable LocalizableElement, because seriously, who cares about localized exceptions?
				if (string.IsNullOrEmpty(configuration.FieldDescription))
				{
					throw new ArgumentException(
						"Invalid configuration: FieldDescription can not be null",
						"configuration");
				}

				if (entry.ClassID !=
					settings.Cache.MetaDataCacheAccessor.GetClassId(
						configuration.FieldDescription))
				{
					throw new ArgumentException("The given argument doesn't configure this type",
						"configuration");
				}
				// ReSharper restore LocalizableElement

				if (!configuration.IsEnabled)
				{
					return settings.ContentGenerator.CreateFragment();
				}

				var pieces = configuration.ReferencedOrDirectChildren
					.Select(config => new ConfigFragment(config, GenerateContentForFieldByReflection(entry, config, publicationDecorator,
						settings)))
					.Where(content => content.Frag!=null && !string.IsNullOrEmpty(content.Frag.ToString())).ToList();
				if (pieces.Count == 0)
					return settings.ContentGenerator.CreateFragment();
				var bldr = settings.ContentGenerator.CreateFragment();
				using (var xw = settings.ContentGenerator.CreateWriter(bldr))
				{
					var clerk = settings.PropertyTable.GetValue<RecordClerk>("ActiveClerk", null);
					var entryClassName = settings.StylesGenerator.AddStyles(configuration).Trim('.');
					settings.ContentGenerator.StartEntry(xw, configuration,
						entryClassName, entry.Guid, index, clerk);
					settings.ContentGenerator.AddEntryData(xw, pieces);
					settings.ContentGenerator.EndEntry(xw);
					xw.Flush();

					// Do not normalize the string if exporting to word doc--it is not needed and will cause loss of document styles
					if (bldr is LcmWordGenerator.DocFragment)
						return bldr;

					return settings.ContentGenerator.CreateFragment(CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFC).Normalize(bldr.ToString())); // All content should be in NFC (LT-18177)
				}
			}
			catch (ArgumentException)
			{
				// probably a configuration error
				throw;
			}
			catch (Exception e)
			{
				// unknown exception, give the user the entry information in the crash message
				throw new Exception($"Exception generating entry: {entry.SortKey}", e);
			}
		}

		/// <summary>
		/// This method will write out the class name attribute into the xhtml for the given configuration node
		/// taking into account the current information in ClassNameOverrides
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="configNode">used to look up any mapping overrides</param>
		public static string GetClassNameAttributeForConfig(ConfigurableDictionaryNode configNode)
		{
			var classAtt = CssGenerator.GetClassAttributeForConfig(configNode);
			if (configNode.ReferencedNode != null)
				classAtt = string.Format("{0} {1}", classAtt, CssGenerator.GetClassAttributeForConfig(configNode.ReferencedNode));
			return classAtt;
		}

		/// <summary>
		/// This method will use reflection to pull data out of the given object based on the given configuration and
		/// write out appropriate content using the settings parameter.
		/// </summary>
		/// <remarks>We use a significant amount of boilerplate code for fields and subfields. Make sure you update both.</remarks>
		internal static IFragment GenerateContentForFieldByReflection(object field, ConfigurableDictionaryNode config,
			DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings, SenseInfo info = new SenseInfo(),
			bool fUseReverseSubField = false)
		{
			if (!config.IsEnabled)
			{
				return settings.ContentGenerator.CreateFragment();
			}
			var cache = settings.Cache;
			var entryType = field.GetType();
			object propertyValue = null;
			if (config.DictionaryNodeOptions is DictionaryNodeGroupingOptions)
			{
				return GenerateContentForGroupingNode(field, config, publicationDecorator, settings);
			}
			if (config.FieldDescription == "DefinitionOrGloss")
			{
				if (field is ILexSense)
				{
					return GenerateContentForDefOrGloss(field as ILexSense, config, settings);
				}
				if (field is ILexEntryRef)
				{
					var ret = settings.ContentGenerator.CreateFragment();
					foreach (var sense in (((field as ILexEntryRef).Owner as ILexEntry).AllSenses))
					{
						ret.Append(GenerateContentForDefOrGloss(sense, config, settings));
					}
					return ret;
				}
			}
			if (config.FieldDescription == "CaptionOrHeadword")
			{
				if (field is ICmPicture)
				{
					return GenerateContentForCaptionOrHeadword(field as ICmPicture, config, settings);
				}
			}
			if (config.IsCustomField && config.SubField == null)
			{
				// REVIEW: We have overloaded terms here, this is a C# class not a css class, consider a different name
				var customFieldOwnerClassName = GetClassNameForCustomFieldParent(config, settings.Cache);
				if (!GetPropValueForCustomField(field, config, cache, customFieldOwnerClassName, config.FieldDescription, ref propertyValue))
					return settings.ContentGenerator.CreateFragment();
			}
			else
			{
				MemberInfo property;
				if (IsExtensionMethod(config.FieldDescription))
				{
					var extensionType = GetExtensionMethodType(config.FieldDescription);
					property = extensionType.GetMethod(
						GetExtensionMethodName(config.FieldDescription),
						BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				}
				else
				{
					property = entryType.GetProperty(config.FieldDescription);
				}
				if (property == null)
				{
#if DEBUG
					var msg = string.Format("Issue with finding {0} for {1}", config.FieldDescription, entryType);
					ShowConfigDebugInfo(msg, config);
#endif
					return settings.ContentGenerator.CreateFragment();
				}
				propertyValue = GetValueFromMember(property, field);
				GetSortedReferencePropertyValue(config, ref propertyValue, field);
			}
			// If the property value is null there is nothing to generate
			if (propertyValue == null)
			{
				return settings.ContentGenerator.CreateFragment();
			}
			if (!string.IsNullOrEmpty(config.SubField))
			{
				if (config.IsCustomField)
				{
					// Get the custom field value (in SubField) using the property which came from the field object
					if (!GetPropValueForCustomField(propertyValue, config, cache, ((ICmObject)propertyValue).ClassName,
						config.SubField, ref propertyValue))
					{
						return settings.ContentGenerator.CreateFragment();
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
						var msg = String.Format("Issue with finding (subField) {0} for (subType) {1}", subField, subType);
						ShowConfigDebugInfo(msg, config);
#endif
						return settings.ContentGenerator.CreateFragment();
					}
					propertyValue = subProp.GetValue(propertyValue, new object[] { });
					GetSortedReferencePropertyValue(config, ref propertyValue, field);
				}
				// If the property value is null there is nothing to generate
				if (propertyValue == null)
					return settings.ContentGenerator.CreateFragment();
			}
			ICmFile fileProperty;
			ICmObject fileOwner;
			var typeForNode = config.IsCustomField
										? GetPropertyTypeFromReflectedTypes(propertyValue.GetType(), null)
										: GetPropertyTypeForConfigurationNode(config, propertyValue.GetType(), cache);
			switch (typeForNode)
			{
				case PropertyType.CollectionType:
					return !IsCollectionEmpty(propertyValue) ? GenerateContentForCollection(propertyValue, config, publicationDecorator, field, settings, info) : settings.ContentGenerator.CreateFragment();
				case PropertyType.MoFormType:
					return GenerateContentForMoForm(propertyValue as IMoForm, config, settings);

				case PropertyType.CmObjectType:
					return GenerateContentForICmObject(propertyValue as ICmObject, config, settings);

				case PropertyType.CmPictureType:
					fileProperty = propertyValue as ICmFile;
					fileOwner = field as ICmObject;
					return fileProperty != null && fileOwner != null
						? GenerateContentForPicture(fileProperty, config, fileOwner, settings)
						: GenerateContentForPictureCaption(propertyValue, config, settings);

				case PropertyType.CmPossibility:
					return GenerateContentForPossibility(propertyValue, config, publicationDecorator, settings);

				case PropertyType.CmFileType:
					fileProperty = propertyValue as ICmFile;
					string internalPath = null;
					if (fileProperty != null && fileProperty.InternalPath != null)
						internalPath = fileProperty.InternalPath;
					// fileProperty.InternalPath can have a backward slash so that gets replaced with a forward slash in Linux
					if (!Platform.IsWindows && !string.IsNullOrEmpty(internalPath))
						internalPath = fileProperty.InternalPath.Replace('\\', '/');

					if (fileProperty != null && !string.IsNullOrEmpty(internalPath))
					{
						var srcAttr = GenerateSrcAttributeForMediaFromFilePath(internalPath, "AudioVisual", settings);
						fileOwner = field as ICmObject;
						// the XHTML id attribute must be unique. The owning ICmMedia has a unique guid.
						// The ICmFile is used for all references to the same file within the project, so its guid is not unique.
						if (fileOwner != null)
						{
							return IsVideo(fileProperty.InternalPath)
								? GenerateContentForVideoFile(config, fileProperty.ClassName, fileOwner.Guid.ToString(), srcAttr, MovieCamera, settings)
								: GenerateContentForAudioFile(config, fileProperty.ClassName, fileOwner.Guid.ToString(), srcAttr, LoudSpeaker, settings);
						}
					}
					return settings.ContentGenerator.CreateFragment();
			}

			var bldr = GenerateContentForValue(field, propertyValue, config, settings);
			if (config.ReferencedOrDirectChildren != null)
			{
				foreach (var child in config.ReferencedOrDirectChildren)
				{
					bldr.Append(GenerateContentForFieldByReflection(propertyValue, child, publicationDecorator, settings));
				}
			}
			return bldr;
		}

		private static IFragment GenerateContentForGroupingNode(object field, ConfigurableDictionaryNode config,
			DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings)
		{
			if (config.ReferencedOrDirectChildren != null && config.ReferencedOrDirectChildren.Any(child => child.IsEnabled))
			{
				var className = settings.StylesGenerator.AddStyles(config).Trim('.');
				return settings.ContentGenerator.GenerateGroupingNode(config, field, className, publicationDecorator, settings,
					(f, c, p, s) => GenerateContentForFieldByReflection(f, c, p, s));
			}
			return settings.ContentGenerator.CreateFragment();
		}

		/// <summary>
		/// Gets the value of the requested custom field associated with the fieldOwner object
		/// </summary>
		/// <returns>true if the custom field was valid and false otherwise</returns>
		/// <remarks>propertyValue can be null if the custom field is valid but no value is stored for the owning object</remarks>
		private static bool GetPropValueForCustomField(object fieldOwner, ConfigurableDictionaryNode config,
			LcmCache cache, string customFieldOwnerClassName, string customFieldName, ref object propertyValue)
		{
			int customFieldFlid = GetCustomFieldFlid(config, cache, customFieldOwnerClassName, customFieldName);
			if (customFieldFlid != 0)
			{
				var customFieldType = cache.MetaDataCacheAccessor.GetFieldType(customFieldFlid);
				ICmObject specificObject;
				if (fieldOwner is ISenseOrEntry)
				{
					specificObject = ((ISenseOrEntry)fieldOwner).Item;
					if (!((IFwMetaDataCacheManaged)cache.MetaDataCacheAccessor).GetFields(specificObject.ClassID,
						true, (int)CellarPropertyTypeFilter.All).Contains(customFieldFlid))
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
			}
			return true;
		}

		private static IFragment GenerateContentForVideoFile(ConfigurableDictionaryNode config, string className, string mediaId, string srcAttribute, string caption, GeneratorSettings settings)
		{
			if (string.IsNullOrEmpty(srcAttribute) && string.IsNullOrEmpty(caption))
				return settings.ContentGenerator.CreateFragment();
			// This creates a link that will open the video in the same window as the dictionary view/preview
			// refreshing will bring it back to the dictionary
			return settings.ContentGenerator.GenerateVideoLinkContent(config, className, GetSafeXHTMLId(mediaId), srcAttribute, caption);
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
					return;
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
				return;
			// Calculate and store the ids for each of the references once for efficiency.
			var refsAndIds = new List<Tuple<ILexReference, string>>();
			foreach (var reference in unsortedReferences)
			{
				var id = reference.OwnerType.Guid.ToString();
				if (LexRefTypeTags.IsAsymmetric((LexRefTypeTags.MappingTypes)reference.OwnerType.MappingType))
					id = id + LexRefDirection(reference, parent);
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
				foreach (var duple in refsAndIds)
				{
					if (option.Id == duple.Item2 && !sortedReferences.Contains(duple.Item1))
					{
						sortedReferences.Add(duple.Item1);
					}
				}
			}
			propertyValue = sortedReferences;
		}

		/// <summary/>
		/// <returns>Returns the flid of the custom field identified by the configuration nodes FieldDescription
		/// in the class identified by <code>customFieldOwnerClassName</code></returns>
		private static int GetCustomFieldFlid(ConfigurableDictionaryNode config, LcmCache cache,
														  string customFieldOwnerClassName, string customFieldName = null)
		{
			var fieldName = customFieldName ?? config.FieldDescription;
			var customFieldFlid = 0;
			var mdc = (IFwMetaDataCacheManaged)cache.MetaDataCacheAccessor;
			if (mdc.FieldExists(customFieldOwnerClassName, fieldName, false))
				customFieldFlid = cache.MetaDataCacheAccessor.GetFieldId(customFieldOwnerClassName, fieldName, false);
			else if (customFieldOwnerClassName == "SenseOrEntry")
			{
				// ENHANCE (Hasso) 2016.06: take pity on the poor user who has defined identically-named Custom Fields on both Sense and Entry
				if (mdc.FieldExists("LexSense", config.FieldDescription, false))
					customFieldFlid = mdc.GetFieldId("LexSense", fieldName, false);
				else if (mdc.FieldExists("LexEntry", config.FieldDescription, false))
					customFieldFlid = mdc.GetFieldId("LexEntry", fieldName, false);
			}
			return customFieldFlid;
		}

		/// <summary>
		/// This method will return the string representing the class name for the parent
		/// node of a configuration item representing a custom field.
		/// </summary>
		private static string GetClassNameForCustomFieldParent(ConfigurableDictionaryNode customFieldNode, LcmCache cache)
		{
			// Use the type of the nearest ancestor that is not a grouping node
			var parentNode = customFieldNode.Parent;
			for (; parentNode.DictionaryNodeOptions is DictionaryNodeGroupingOptions; parentNode = parentNode.Parent) { }
			var parentNodeType = GetTypeForConfigurationNode(parentNode, cache, out _);
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
			if (parentNodeType.IsInterface)
			{
				// Strip off the interface designation since custom fields are added to concrete classes
				return parentNodeType.Name.Substring(1);
			}
			return parentNodeType.Name;
		}

		private static IFragment GenerateContentForPossibility(object propertyValue, ConfigurableDictionaryNode config,
			DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings)
		{
			if (config.ReferencedOrDirectChildren == null || !config.ReferencedOrDirectChildren.Any(node => node.IsEnabled))
				return settings.ContentGenerator.CreateFragment();
			var bldr = settings.ContentGenerator.CreateFragment();
			foreach (var child in config.ReferencedOrDirectChildren)
			{
				var content = GenerateContentForFieldByReflection(propertyValue, child, publicationDecorator, settings);
				bldr.Append(content);
			}

			if (bldr.Length() > 0)
			{
				var className = settings.StylesGenerator.AddStyles(config).Trim('.');
				return settings.ContentGenerator.WriteProcessedObject(config, false, bldr, className);
			}

			// bldr is a fragment that is empty of text, since length = 0
			return bldr;
		}

		private static IFragment GenerateContentForPictureCaption(object propertyValue, ConfigurableDictionaryNode config, GeneratorSettings settings)
		{
			// todo: get sense numbers and captions into the same div and get rid of this if else
			IFragment content;
			if (config.DictionaryNodeOptions != null)
				content = GenerateContentForStrings(propertyValue as IMultiString, config, settings);
			else
				content = GenerateContentForString(propertyValue as ITsString, config, settings);
			if (!content.IsNullOrEmpty())
			{
				var className = settings.StylesGenerator.AddStyles(config).Trim('.');
				return settings.ContentGenerator.WriteProcessedObject(config, true, content, className);
			}
			return settings.ContentGenerator.CreateFragment();
		}

		private static IFragment GenerateContentForPicture(ICmFile pictureFile, ConfigurableDictionaryNode config, ICmObject owner,
			GeneratorSettings settings)
		{
			var srcAttribute = GenerateSrcAttributeFromFilePath(pictureFile, settings.UseRelativePaths ? "pictures" : null, settings);
			if (!string.IsNullOrEmpty(srcAttribute))
			{
				var className = settings.StylesGenerator.AddStyles(config).Trim('.');
				// An XHTML id attribute must be unique but the ICmfile is used for all references to the same file within the project.
				// The ICmPicture that owns the file does have unique guid so we use that.
				var ownerGuid = owner.Guid.ToString();
				return settings.ContentGenerator.AddImage(config, className, srcAttribute, ownerGuid);
			}
			return settings.ContentGenerator.CreateFragment();
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
			return (settings.UseRelativePaths || !settings.UseUri) ? filePath : new Uri(filePath).ToString();
		}

		private static string GenerateSrcAttributeForMediaFromFilePath(string filename, string subFolder, GeneratorSettings settings)
		{
			string filePath;
			var linkedFilesRootDir = settings.Cache.LangProject.LinkedFilesRootDir;
			var audioVisualFile = Path.GetDirectoryName(filename) == subFolder ?
				Path.Combine(linkedFilesRootDir, filename) :
				Path.Combine(linkedFilesRootDir, subFolder, filename);
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

		private static IFragment GenerateContentForDefOrGloss(ILexSense sense, ConfigurableDictionaryNode config, GeneratorSettings settings)
		{
			var wsOption = config.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
			if (wsOption == null)
				throw new ArgumentException(@"Configuration nodes for MultiString fields would have WritingSystemOptions", "config");
			var bldr = settings.ContentGenerator.CreateFragment();
			bool first = true;
			foreach (var option in wsOption.Options)
			{
				if (option.IsEnabled)
				{
					int wsId;
					ITsString bestString = sense.GetDefinitionOrGloss(option.Id, out wsId);
					if (bestString != null)
					{
						var contentItem = GenerateWsPrefixAndString(config, settings, wsOption, wsId, bestString, Guid.Empty, first);
						first = false;
						bldr.Append(contentItem);
					}
				}
			}

			if (bldr.Length() > 0)
			{
				var className = settings.StylesGenerator.AddStyles(config).Trim('.');
				return settings.ContentGenerator.WriteProcessedCollection(config, false, bldr, className);
			}
			// bldr is a fragment that is empty of text, since length = 0
			return bldr;
		}

		private static IFragment GenerateContentForCaptionOrHeadword(ICmPicture picture, ConfigurableDictionaryNode config, GeneratorSettings settings)
		{
			var wsOption = config.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
			if (wsOption == null)
				throw new ArgumentException(@"Configuration nodes for MultiString fields should have WritingSystemOptions", "config");
			var bldr = settings.ContentGenerator.CreateFragment();
			bool first = true;
			foreach (var option in wsOption.Options)
			{
				if (option.IsEnabled)
				{
					int wsId;
					ITsString bestString = picture.GetCaptionOrHeadword(option.Id, out wsId);
					if (bestString != null)
					{
						var contentItem = GenerateWsPrefixAndString(config, settings, wsOption, wsId, bestString, Guid.Empty, first);
						first = false;
						bldr.Append(contentItem);
					}
				}
			}
			if (bldr.Length() > 0)
				return settings.ContentGenerator.WriteProcessedCollection(config, false, bldr, GetClassNameAttributeForConfig(config));
			// bldr is a fragment that is empty of text, since length = 0
			return bldr;
		}

		internal static string CopyFileSafely(GeneratorSettings settings, string source, string relativeDestination)
		{
			if (!File.Exists(source))
				return relativeDestination;
			bool isWavExport = settings.IsWebExport && Path.GetExtension(relativeDestination).Equals(".wav");
			if (isWavExport)
				relativeDestination = Path.ChangeExtension(relativeDestination, ".mp3");
			var destination = Path.Combine(settings.ExportPath, relativeDestination);
			var subFolder = Path.GetDirectoryName(relativeDestination);
			FileUtils.EnsureDirectoryExists(Path.GetDirectoryName(destination));
			// If an audio file is referenced by multiple entries they could end up in separate threads.
			// Locking on the PropertyTable seems safe since it will be the same PropertyTable for each thread.
			lock (settings.PropertyTable)
			{
				if (!File.Exists(destination))
				{
					// converts audio files to correct format during Webonary export
					if (isWavExport)
						WavConverter.WavToMp3(source, destination);
					else
						FileUtils.Copy(source, destination);
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
						newFileName = string.Format("{0}{1}{2}", fileWithoutExtension, copyNumber, fileExtension);
						destination = string.IsNullOrEmpty(subFolder)
							? Path.Combine(settings.ExportPath, newFileName)
							: Path.Combine(settings.ExportPath, subFolder, newFileName);
					} while (File.Exists(destination) && !AreFilesIdentical(source, destination, isWavExport));
					// converts audio files to correct format if necessary during Webonary export
					if (!isWavExport)
						File.Copy(source, destination, true); //If call two times, quicker than Windows updates the file system
					else
						WavConverter.WavToMp3(source, destination);
					// Change the filepath to point to the copied file
					relativeDestination = string.IsNullOrEmpty(subFolder) ? newFileName : Path.Combine(subFolder, newFileName);
				}
			}
			return relativeDestination;
		}

		private static bool AreFilesIdentical(string source, string destination, bool isWavExport)
		{
			if (!isWavExport)
				return FileUtils.AreFilesIdentical(source, destination);
			SaveFile exists = WavConverter.AlreadyExists(source, destination);
			if (exists == SaveFile.IdenticalExists)
				return true;
			return false;
		}

		private static string MakeSafeFilePath(string filePath)
		{
			if (Common.FwUtils.Unicode.CheckForNonAsciiCharacters(filePath))
			{
				// Flex keeps the filename as NFD in memory because it is unicode. We need NFC to actually link to the file
				filePath = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFC).Normalize(filePath);
			}
			if (!FileUtils.IsFilePathValid(filePath))
			{
				return "__INVALID_FILE_NAME__";
			}
			return filePath;
		}

		internal enum PropertyType
		{
			CollectionType,
			MoFormType,
			CmObjectType,
			CmPictureType,
			CmFileType,
			CmPossibility,
			PrimitiveType,
			InvalidProperty
		}

		private static Dictionary<ConfigurableDictionaryNode, PropertyType> _configNodeToTypeMap = new Dictionary<ConfigurableDictionaryNode, PropertyType>();

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
		internal static PropertyType GetPropertyTypeForConfigurationNode(ConfigurableDictionaryNode config, LcmCache cache)
		{
			var propertyType = GetPropertyTypeForConfigurationNode(config, null, cache);
			if (config.FieldDescription == "DefinitionOrGloss")
				propertyType = PropertyType.PrimitiveType;
			return propertyType;
		}

		/// <summary>
		/// This method will reflectively return the type that represents the given configuration node as
		/// described by the ancestry and FieldDescription and SubField properties of each node in it.
		/// </summary>
		/// <returns></returns>
		internal static PropertyType GetPropertyTypeForConfigurationNode(ConfigurableDictionaryNode config, Type fieldTypeFromData, LcmCache cache = null)
		{
			Type parentType;
			var fieldType = GetTypeForConfigurationNode(config, cache, out parentType);
			if (fieldType == null)
				fieldType = fieldTypeFromData;
			return GetPropertyTypeFromReflectedTypes(fieldType, parentType);
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
			if (typeof(ICmObject).IsAssignableFrom(fieldType))
			{
				return PropertyType.CmObjectType;
			}
			return PropertyType.PrimitiveType;
		}

		/// <summary>
		/// This method will return the Type that represents the data in the given configuration node.
		/// </summary>
		/// <param name="config">This node and it's lineage will be used to find the type</param>
		/// <param name="cache">Used when dealing with custom field nodes</param>
		/// <param name="parentType">This will be set to the type of the parent of config which is sometimes useful to the callers</param>
		/// <returns></returns>
		internal static Type GetTypeForConfigurationNode(ConfigurableDictionaryNode config, LcmCache cache, out Type parentType)
		{
			if (config == null)
			{
				throw new ArgumentNullException("config", "The configuration node must not be null.");
			}

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
					lineage.Push(next);
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
				throw new ArgumentException(String.Format(xWorksStrings.InvalidRootConfigurationNode, rootNode.FieldDescription));
			}
			var fieldType = lookupType;

			// Traverse the configuration reflectively inspecting the types in parent to child order
			foreach (var node in lineage)
			{
				if (node.IsCustomField)
				{
					fieldType = GetCustomFieldType(lookupType, node, cache);
				}
				else
				{
					var property = GetProperty(lookupType, node);
					if (property != null)
					{
						fieldType = GetTypeFromMember(property);
					}
					else
					{
						return null;
					}
					if (IsCollectionType(fieldType))
					{
						// When a node points to a collection all the child nodes operate on individual items in the
						// collection, so look them up in the type that the collection contains. e.g. IEnumerable<ILexEntry>
						// gives ILexEntry and IFdoVector<ICmObject> gives ICmObject
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

		private static bool IsExtensionMethod(string fieldDescription)
		{
			return fieldDescription.StartsWith("@extension:");
		}

		private static string GetExtensionMethodName(string fieldDescription)
		{
			return fieldDescription.Split('.').Last();
		}

		private static Type GetExtensionMethodType(string fieldDescription)
		{
			var lengthOfMethodName = fieldDescription.LastIndexOf('.') - "@extension:".Length;
			var typeName = fieldDescription.Substring("@extension:".Length, lengthOfMethodName);
			var type = Type.GetType(typeName);
			if (type != null) return type;
			foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
			{
				type = a.GetType(typeName);
				if (type != null)
				{
					return type;
				}
			}
			return null;
		}

		private static Type GetTypeFromMember(MemberInfo property)
		{
			switch (property.MemberType)
			{
				case MemberTypes.Property:
				{
					return ((PropertyInfo)property).PropertyType;
				}
				case MemberTypes.Method:
				{
					return ((MethodInfo)property).ReturnType;
				}
				default:
					return null;
			}
		}

		private static object GetValueFromMember(MemberInfo property, object instance)
		{
			switch (property.MemberType)
			{
				case MemberTypes.Property:
				{
					return ((PropertyInfo)property).GetValue(instance, new object[] {});
				}
				case MemberTypes.Method:
				{
					// Execute the presumed extension method (passing the instance as the 'this' parameter)
					return ((MethodInfo)property).Invoke(instance, new object[] {instance});
				}
				default:
					return null;
			}
		}

		private static Type GetCustomFieldType(Type lookupType, ConfigurableDictionaryNode config, LcmCache cache)
		{
			// FDO doesn't work with interfaces, just concrete classes so chop the I off any interface types
			var customFieldOwnerClassName = lookupType.Name.TrimStart('I');
			var customFieldFlid = GetCustomFieldFlid(config, cache, customFieldOwnerClassName);
			if (customFieldFlid != 0)
			{
				var customFieldType = cache.MetaDataCacheAccessor.GetFieldType(customFieldFlid);
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
							var destClassId = cache.MetaDataCacheAccessor.GetDstClsId(customFieldFlid);
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
			return null;
		}

		/// <summary>
		/// Loading an assembly is expensive so we cache the assembly once it has been loaded
		/// for enahanced performance.
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
		/// Return the property info from a given class and node. Will check interface heirarchy for the property
		/// if <code>lookupType</code> is an interface.
		/// </summary>
		/// <param name="lookupType"></param>
		/// <param name="node"></param>
		/// <returns></returns>
		private static MemberInfo GetProperty(Type lookupType, ConfigurableDictionaryNode node)
		{
			string propertyOfInterest;
			MemberInfo propInfo;
			var typesToCheck = new Stack<Type>();
			typesToCheck.Push(lookupType);
			do
			{
				var current = typesToCheck.Pop();
				propertyOfInterest = node.FieldDescription;
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

				if (IsExtensionMethod(propertyOfInterest))
				{
					var extensionType = GetExtensionMethodType(propertyOfInterest);
					propInfo = extensionType?.GetMethod(
						GetExtensionMethodName(propertyOfInterest),
						BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				}
				else
				{
					propInfo = current.GetProperty(propertyOfInterest, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				}
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

		private static IFragment GenerateContentForMoForm(IMoForm moForm, ConfigurableDictionaryNode config, GeneratorSettings settings)
		{
			// Don't export if there is no such data
			if (moForm == null)
				return settings.ContentGenerator.CreateFragment();
			if (config.ReferencedOrDirectChildren != null && config.ReferencedOrDirectChildren.Any())
			{
				throw new NotImplementedException("Children for MoForm types not yet supported.");
			}
			return GenerateContentForStrings(moForm.Form, config, settings, moForm.Owner.Guid);
		}

		/// <summary>
		/// This method will generate the XHTML that represents a collection and its contents
		/// </summary>
		private static IFragment GenerateContentForCollection(object collectionField, ConfigurableDictionaryNode config,
			DictionaryPublicationDecorator pubDecorator, object collectionOwner, GeneratorSettings settings, SenseInfo info = new SenseInfo())
		{
			// To be used for things like shared grammatical info
			var sharedCollectionInfo = settings.ContentGenerator.CreateFragment();
			var frag = settings.ContentGenerator.CreateFragment();
			IEnumerable collection;
			if (collectionField is IEnumerable)
			{
				collection = (IEnumerable)collectionField;
			}
			else if (collectionField is ILcmVector)
			{
				collection = ((ILcmVector)collectionField).Objects;
			}
			else
			{
				throw new ArgumentException("The given field is not a recognized collection");
			}
			var cmOwner = collectionOwner as ICmObject ?? ((ISenseOrEntry)collectionOwner).Item;

			if (config.DictionaryNodeOptions is DictionaryNodeSenseOptions)
			{
				frag.Append(GenerateContentForSenses(config, pubDecorator, settings, collection, info, ref sharedCollectionInfo));
			}
			else
			{
				FilterAndSortCollectionIfNeeded(ref collection, pubDecorator, config.SubField ?? config.FieldDescription);
				ConfigurableDictionaryNode lexEntryTypeNode;
				if (IsVariantEntryType(config, out lexEntryTypeNode))
				{
					frag.Append(GenerateContentForEntryRefCollection(config, collection, cmOwner, pubDecorator, settings, lexEntryTypeNode, false));
				}
				else if (IsComplexEntryType(config, out lexEntryTypeNode))
				{
					frag.Append(GenerateContentForEntryRefCollection(config, collection, cmOwner, pubDecorator, settings, lexEntryTypeNode, true));
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
						Debug.Assert(config.DictionaryNodeOptions == null,
							"double calls to GenerateContentForLexEntryRefsByType don't play nicely with ListOptions. Everything will be generated twice (if it doesn't crash)");
						// Display typeless refs
						bool first = true;
						foreach (var entry in lerCollection.Where(item => !item.ComplexEntryTypesRS.Any() && !item.VariantEntryTypesRS.Any()))
						{
							frag.Append(GenerateCollectionItemContent(config, pubDecorator, entry, collectionOwner, settings, first, lexEntryTypeNode));
							first = false;
						}
						// Display refs of each type
						GenerateContentForLexEntryRefsByType(config, lerCollection, collectionOwner, pubDecorator, settings, frag, lexEntryTypeNode,
							true); // complex
						GenerateContentForLexEntryRefsByType(config, lerCollection, collectionOwner, pubDecorator, settings, frag, lexEntryTypeNode,
							false); // variants
					}
					else
					{
						Debug.WriteLine("Unable to group " + config.FieldDescription + " by LexRefType; generating sequentially");
						bool first = true;
						foreach (var item in lerCollection)
						{
							frag.Append(GenerateCollectionItemContent(config, pubDecorator, item, collectionOwner, settings, first));
							first = false;
						}
					}
				}
				else if (config.FieldDescription.StartsWith("Subentries"))
				{
					GenerateContentForSubentries(config, collection, cmOwner, pubDecorator, settings, frag);
				}
				else if (IsLexReferenceCollection(config))
				{
					GenerateContentForLexRefCollection(config, collection.Cast<ILexReference>(), cmOwner, pubDecorator, settings, frag);
				}
				else
				{
					bool first = true;
					foreach (var item in collection)
					{
						frag.Append(GenerateCollectionItemContent(config, pubDecorator, item, collectionOwner, settings, first));
						first = false;
					}
				}
			}

			if (frag.Length() > 0 || sharedCollectionInfo.Length() > 0)
			{
				var className = settings.StylesGenerator.AddStyles(config).Trim('.');
				return config.DictionaryNodeOptions is DictionaryNodeSenseOptions ?
					settings.ContentGenerator.WriteProcessedSenses(config, false, frag, className, sharedCollectionInfo) :
					settings.ContentGenerator.WriteProcessedCollection(config, false, frag, className);
			}
			return settings.ContentGenerator.CreateFragment();
		}

		private static bool IsLexReferenceCollection(ConfigurableDictionaryNode config)
		{
			var opt = config.DictionaryNodeOptions as DictionaryNodeListOptions;
			return opt != null && (opt.ListId == DictionaryNodeListOptions.ListIds.Entry ||
				opt.ListId == DictionaryNodeListOptions.ListIds.Sense);
		}

		internal static bool IsFactoredReference(ConfigurableDictionaryNode node, out ConfigurableDictionaryNode typeChild)
		{
			var paraOptions = node.DictionaryNodeOptions as IParaOption;
			if (paraOptions != null && paraOptions.DisplayEachInAParagraph)
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
			if ((config.FieldDescription == "VisibleComplexFormBackRefs" || config.FieldDescription == "ComplexFormsNotSubentries")
				&& config.ReferencedOrDirectChildren != null)
			{
				complexEntryTypeNode = config.ReferencedOrDirectChildren.FirstOrDefault(child => child.FieldDescription == "ComplexEntryTypesRS");
				return complexEntryTypeNode != null;
			}
			return false;
		}

		private static bool IsVariantEntryType(ConfigurableDictionaryNode config, out ConfigurableDictionaryNode variantEntryTypeNode)
		{
			variantEntryTypeNode = null;
			var variantOptions = config.DictionaryNodeOptions as DictionaryNodeListOptions;
			if (variantOptions != null && variantOptions.ListId == DictionaryNodeListOptions.ListIds.Variant)
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

		private static IFragment GenerateContentForEntryRefCollection(ConfigurableDictionaryNode config, IEnumerable collection, ICmObject collectionOwner,
			DictionaryPublicationDecorator pubDecorator, GeneratorSettings settings, ConfigurableDictionaryNode typeNode, bool isComplex)
		{
			var frag = settings.ContentGenerator.CreateFragment();

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
				bool first = true;
				foreach (var entry in lerCollection.Where(item => !item.ComplexEntryTypesRS.Any() && !item.VariantEntryTypesRS.Any()))
				{
					frag.Append(GenerateCollectionItemContent(config, pubDecorator, entry, collectionOwner, settings, first, typeNode));
					first = false;
				}
				// Display refs of each type
				GenerateContentForLexEntryRefsByType(config, lerCollection, collectionOwner, pubDecorator, settings, frag, typeNode, isComplex);
			}
			else
			{
				Debug.WriteLine("Unable to group " + config.FieldDescription + " by LexRefType; generating sequentially");
				bool first = true;
				foreach (var item in lerCollection)
				{
					frag.Append(GenerateCollectionItemContent(config, pubDecorator, item, collectionOwner, settings, first));
					first = false;
				}
			}
			return frag;
		}

		private static void GenerateContentForLexEntryRefsByType(ConfigurableDictionaryNode config, List<ILexEntryRef> lerCollection, object collectionOwner, DictionaryPublicationDecorator pubDecorator,
			GeneratorSettings settings, IFragment bldr, ConfigurableDictionaryNode typeNode, bool isComplex)
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
			var paraOptions = config.DictionaryNodeOptions as IParaOption;
			if (paraOptions != null && paraOptions.DisplayEachInAParagraph)
				typeNode = null;
			// Generate XHTML by Type
			foreach (var typeGuid in lexEntryTypesFiltered)
			{
				var combinedContent = settings.ContentGenerator.CreateFragment();
				bool first = true;
				foreach (var lexEntRef in lerCollection)
				{
					if (isComplex ? lexEntRef.ComplexEntryTypesRS.Any(t => t.Guid == typeGuid) : lexEntRef.VariantEntryTypesRS.Any(t => t.Guid == typeGuid))
					{
						var content = GenerateCollectionItemContent(config, pubDecorator, lexEntRef, collectionOwner, settings, first, typeNode);
						if (!content.IsNullOrEmpty())
						{
							combinedContent.Append(content);
							first = false;
						}
					}
				}

				if (!first)
				{
					var lexEntryType = lexEntryTypes.First(t => t.Guid.Equals(typeGuid));
					// Display the Type if there were refs of this Type (and we are factoring)
					var generateLexType = typeNode != null;
					var lexTypeContent = generateLexType
						? GenerateCollectionItemContent(typeNode, pubDecorator, lexEntryType,
							lexEntryType.Owner, settings, true)
						: null;
					var className = generateLexType ? settings.StylesGenerator.AddStyles(typeNode).Trim('.') : null;
					var refsByType = settings.ContentGenerator.AddLexReferences(typeNode, generateLexType,
						lexTypeContent, className, combinedContent, IsTypeBeforeForm(config));
					bldr.Append(refsByType);
				}
			}
		}

		private static void GenerateContentForSubentries(ConfigurableDictionaryNode config, IEnumerable collection, ICmObject collectionOwner,
			DictionaryPublicationDecorator pubDecorator, GeneratorSettings settings, IFragment frag)
		{
			var listOptions = config.DictionaryNodeOptions as DictionaryNodeListOptions;
			var typeNode = config.ReferencedOrDirectChildren.FirstOrDefault(n => n.FieldDescription == LookupComplexEntryType);
			if (listOptions != null && typeNode != null && typeNode.IsEnabled
				&& typeNode.ReferencedOrDirectChildren != null && typeNode.ReferencedOrDirectChildren.Any(n => n.IsEnabled))
			{
				// Get a list of Subentries including their relevant ILexEntryRefs. We will remove each Subentry from the list as it is
				// generated to prevent multiple generations on the odd chance that a Subentry has multiple Complex Form Types
				var subentries = collection.Cast<ILexEntry>()
					.Select(le => new Tuple<ILexEntryRef, ILexEntry>(EntryRefForSubentry(le, collectionOwner), le)).ToList();

				// Generate any Subentries with no ComplexFormType
				bool first = true;
				for (var i = 0; i < subentries.Count; i++)
				{
					if (subentries[i].Item1 == null || !subentries[i].Item1.ComplexEntryTypesRS.Any())
					{
						frag.Append(GenerateCollectionItemContent(config, pubDecorator, subentries[i].Item2, collectionOwner, settings, first));
						first = false;
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
							frag.Append(GenerateCollectionItemContent(config, pubDecorator, subentries[i].Item2, collectionOwner, settings, first));
							first = false;
							subentries.RemoveAt(i--);
						}
					}
				}
			}
			else
			{
				Debug.WriteLine("Unable to group " + config.FieldDescription + " by LexRefType; generating sequentially");
				bool first = true;
				foreach (var item in collection)
				{
					frag.Append(GenerateCollectionItemContent(config, pubDecorator, item, collectionOwner, settings, first));
					first = false;
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
			if (collection is IEnumerable<ICmObject>)
			{
				var cmCollection = collection.Cast<ICmObject>();
				if (decorator != null)
					cmCollection = cmCollection.Where(item => !decorator.IsExcludedObject(item));
				if (IsCollectionInNeedOfSorting(fieldDescr))
					cmCollection = cmCollection.OrderBy(x => x.SortKey2);
				collection = cmCollection;
			}
			else if (collection is IEnumerable<ISenseOrEntry>)
			{
				var seCollection = collection.Cast<ISenseOrEntry>();
				if (decorator != null)
					seCollection = seCollection.Where(item => !decorator.IsExcludedObject(item.Item));
				if (IsCollectionInNeedOfSorting(fieldDescr))
					seCollection = seCollection.OrderBy(x => x.Item.SortKey2);
				collection = seCollection;
			}
		}

		/// <remarks>Variants and Complex Forms may also need sorting, but it is more efficient to check for them elsewhere</remarks>
		private static bool IsCollectionInNeedOfSorting(string fieldDescr)
		{
			// REVIEW (Hasso) 2016.09: should we check the CellarPropertyType?
			return fieldDescr.EndsWith("RC") || fieldDescr.EndsWith("OC"); // Reference Collection, Owning Collection (vs. Sequence)
		}

		/// <summary>
		/// This method will generate the Content that represents a senses collection and its contents
		/// </summary>
		private static IFragment GenerateContentForSenses(ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator,
			GeneratorSettings settings, IEnumerable senseCollection, SenseInfo info, ref IFragment sharedGramInfo)
		{
			// Check whether all the senses have been excluded from publication.  See https://jira.sil.org/browse/LT-15697.
			var filteredSenseCollection = new List<ILexSense>();
			foreach (ILexSense item in senseCollection)
			{
				Debug.Assert(item != null);
				if (publicationDecorator?.IsExcludedObject(item) ?? false)
					continue;
				filteredSenseCollection.Add(item);
			}
			if (filteredSenseCollection.Count == 0)
				return settings.ContentGenerator.CreateFragment();
			var isSubsense = config.Parent != null && config.FieldDescription == config.Parent.FieldDescription;
			string lastGrammaticalInfo, langId;
			var isSameGrammaticalInfo = IsAllGramInfoTheSame(config, filteredSenseCollection, isSubsense, out lastGrammaticalInfo, out langId);
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
				info.ParentSenseNumberingStyle = senseNode.ParentSenseNumberingStyle;

			info.HomographConfig = settings.Cache.ServiceLocator.GetInstance<HomographConfiguration>();
			// Calculating isThisSenseNumbered may make sense to do for each item in the foreach loop below, but because of how the answer
			// is determined, the answer for all sibling senses is the same as for the first sense in the collection.
			// So calculating outside the loop for performance.
			var isThisSenseNumbered = ShouldThisSenseBeNumbered(filteredSenseCollection[0], config, filteredSenseCollection);
			var bldr = settings.ContentGenerator.CreateFragment();

			bool first = true;
			foreach (var item in filteredSenseCollection)
			{
				info.SenseCounter++;
				bldr.Append(GenerateSenseContent(config, publicationDecorator, item, isThisSenseNumbered, settings,
					isSameGrammaticalInfo, info, first));
				first = false;
			}
			settings.StylesGenerator.AddStyles(config);
			return bldr;
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
		internal static bool ShouldThisSenseBeNumbered(ILexSense sense, ConfigurableDictionaryNode senseConfiguration,
			IEnumerable<ILexSense> siblingSenses)
		{
			var senseOptions = senseConfiguration.DictionaryNodeOptions as DictionaryNodeSenseOptions;
			if (string.IsNullOrEmpty(senseOptions.NumberingStyle))
				return false;
			if (siblingSenses.Count() > 1)
				return true;
			if (senseOptions.NumberEvenASingleSense)
				return true;
			if (sense.SensesOS.Count == 0)
				return false;
			if (!AreThereEnabledSubsensesWithNumberingStyle(senseConfiguration))
				return false;
			return true;
		}

		/// <summary>
		/// Does this sense node have a subsenses node that is enabled in the configuration and has numbering style?
		/// </summary>
		/// <param name="senseNode">sense node that might have subsenses</param>
		internal static bool AreThereEnabledSubsensesWithNumberingStyle(ConfigurableDictionaryNode senseNode)
		{
			if (senseNode == null)
				return false;
			return senseNode.Children.Any(child =>
				child.DictionaryNodeOptions is DictionaryNodeSenseOptions &&
				child.IsEnabled &&
				!string.IsNullOrEmpty(((DictionaryNodeSenseOptions)child.DictionaryNodeOptions).NumberingStyle));
		}

		private static IFragment InsertGramInfoBeforeSenses(ILexSense item, ConfigurableDictionaryNode gramInfoNode,
			DictionaryPublicationDecorator publicationDecorator, GeneratorSettings settings)
		{
			var content = GenerateContentForFieldByReflection(item, gramInfoNode, publicationDecorator, settings);
			if (content.IsNullOrEmpty())
				return settings.ContentGenerator.CreateFragment();
			return settings.ContentGenerator.GenerateGramInfoBeforeSensesContent(content, gramInfoNode);
		}

		private static bool IsAllGramInfoTheSame(ConfigurableDictionaryNode config, IEnumerable<ILexSense> collection, bool isSubsense,
			out string lastGrammaticalInfo, out string langId)
		{
			lastGrammaticalInfo = String.Empty;
			langId = String.Empty;
			var isSameGrammaticalInfo = false;
			if (config.FieldDescription == "SensesOS" || config.FieldDescription == "SensesRS")
			{
				var senseNode = (DictionaryNodeSenseOptions)config.DictionaryNodeOptions;
				if (senseNode == null)
					return false;
				if (senseNode.ShowSharedGrammarInfoFirst)
				{
					if (isSubsense)
					{
						// Add the owning sense to the collection that we want to check.
						var objs = new List<ILexSense>();
						objs.AddRange(collection);
						if (objs.Count == 0 || !(objs[0].Owner is ILexSense))
							return false;
						objs.Add((ILexSense)objs[0].Owner);
						if (!CheckIfAllGramInfoTheSame(config, objs, ref isSameGrammaticalInfo, ref lastGrammaticalInfo, ref langId))
							return false;
					}
					else
					{
						if (!CheckIfAllGramInfoTheSame(config, collection, ref isSameGrammaticalInfo, ref lastGrammaticalInfo, ref langId))
							return false;
					}
				}
			}
			return isSameGrammaticalInfo && !string.IsNullOrEmpty(lastGrammaticalInfo);
		}

		private static bool CheckIfAllGramInfoTheSame(ConfigurableDictionaryNode config, IEnumerable<ILexSense> collection,
			ref bool isSameGrammaticalInfo, ref string lastGrammaticalInfo, ref string langId)
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
					return false;
				var property = entryType.GetProperty(grammaticalInfo.FieldDescription);
				var propertyValue = property.GetValue(item, new object[] { });
				if (propertyValue == null)
					return false;
				var child = grammaticalInfo.ReferencedOrDirectChildren.FirstOrDefault(e => e.IsEnabled && e.ReferencedOrDirectChildren.Count == 0);
				if (child == null)
					return false;
				entryType = propertyValue.GetType();
				property = entryType.GetProperty(child.FieldDescription);
				propertyValue = property.GetValue(propertyValue, new object[] { });
				if (propertyValue is ITsString)
				{
					ITsString fieldValue = (ITsString)propertyValue;
					requestedString = fieldValue.Text;
				}
				else
				{
					IMultiAccessorBase fieldValue = (IMultiAccessorBase)propertyValue;
					var bestStringValue = fieldValue.BestAnalysisAlternative.Text;
					if (bestStringValue != fieldValue.NotFoundTss.Text)
						requestedString = bestStringValue;
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

		private static IFragment GenerateSenseContent(ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator,
			object item, bool isThisSenseNumbered, GeneratorSettings settings, bool isSameGrammaticalInfo, SenseInfo info, bool first)
		{
			var senseNumberSpan = GenerateSenseNumberSpanIfNeeded(config, isThisSenseNumbered, ref info, settings);
			var bldr = settings.ContentGenerator.CreateFragment();
			if (config.ReferencedOrDirectChildren != null)
			{
				foreach (var child in config.ReferencedOrDirectChildren)
				{
					if (child.FieldDescription != "MorphoSyntaxAnalysisRA" || !isSameGrammaticalInfo)
					{
						bldr.Append(GenerateContentForFieldByReflection(item, child, publicationDecorator, settings, info));
					}
				}
			}
			if (bldr.Length() == 0)
				return bldr;

			return settings.ContentGenerator.AddSenseData(config, senseNumberSpan, ((ICmObject)item).Owner.Guid, bldr, first);
		}

		private static IFragment GeneratePictureContent(ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator,
			object item, GeneratorSettings settings)
		{
			if (item is ICmPicture cmPic && !File.Exists(cmPic.PictureFileRA?.AbsoluteInternalPath))
			{
				Logger.WriteEvent($"Skipping generating picture because there is no file at {cmPic.PictureFileRA?.AbsoluteInternalPath ?? "all"}");
				return settings.ContentGenerator.CreateFragment();
			}
			var bldr = settings.ContentGenerator.CreateFragment();
			var contentGenerator = settings.ContentGenerator;
			using (var writer = contentGenerator.CreateWriter(bldr))
			{

				//Adding Thumbnail tag
				foreach (var child in config.ReferencedOrDirectChildren)
				{
					if (child.FieldDescription == "PictureFileRA")
					{
						var content = GenerateContentForFieldByReflection(item, child, publicationDecorator, settings);
						contentGenerator.WriteProcessedContents(writer, config, content);
						break;
					}
				}
				//Adding tags for Sense Number and Caption
				// Note: this SenseNumber comes from a field in the FDO model (not generated based on a DictionaryNodeSenseOptions).
				//  Should we choose in the future to generate the Picture's sense number using ConfiguredLcmGenerator based on a SenseOption,
				//  we will need to pass the SenseOptions to this point in the call tree.

				var captionBldr = settings.ContentGenerator.CreateFragment();
				foreach (var child in config.ReferencedOrDirectChildren)
				{
					if (child.FieldDescription != "PictureFileRA")
					{
						var content = GenerateContentForFieldByReflection(item, child, publicationDecorator, settings);
						captionBldr.Append(content);
					}
				}

				if (captionBldr.Length() != 0)
				{
					contentGenerator.WriteProcessedContents(writer, config, settings.ContentGenerator.AddImageCaption(config, captionBldr));
				}
				writer.Flush();
				return bldr;
			}
		}

		private static IFragment GenerateCollectionItemContent(ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator,
			object item, object collectionOwner, GeneratorSettings settings, bool first, ConfigurableDictionaryNode factoredTypeField = null)
		{
			if (item is IMultiStringAccessor)
				return GenerateContentForStrings((IMultiStringAccessor)item, config, settings);
			if ((config.DictionaryNodeOptions is DictionaryNodeListOptions && !IsListItemSelectedForExport(config, item, collectionOwner))
				|| config.ReferencedOrDirectChildren == null)
				return settings.ContentGenerator.CreateFragment();

			var bldr = settings.ContentGenerator.CreateFragment();
			var listOptions = config.DictionaryNodeOptions as DictionaryNodeListOptions;
			if (listOptions is DictionaryNodeListAndParaOptions)
			{
				foreach (var child in config.ReferencedOrDirectChildren.Where(child => !ReferenceEquals(child, factoredTypeField)))
				{
					bldr.Append(child.FieldDescription == LookupComplexEntryType
						? GenerateSubentryTypeChild(child, publicationDecorator, (ILexEntry)item, collectionOwner, settings)
						: GenerateContentForFieldByReflection(item, child, publicationDecorator, settings));
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
					bldr.Append(GenerateContentForFieldByReflection(item, child, publicationDecorator, settings));
				}
			}
			if (bldr.Length() == 0)
				return bldr;
			var collectionContent = bldr;
			return settings.ContentGenerator.AddCollectionItem(config, IsBlockProperty(config), GetCollectionItemClassAttribute(config), collectionContent, first);
		}

		private static void GenerateContentForLexRefCollection(ConfigurableDictionaryNode config,
			IEnumerable<ILexReference> collection, ICmObject cmOwner, DictionaryPublicationDecorator pubDecorator,
			GeneratorSettings settings, IFragment bldr)
		{
			// The collection of ILexReferences has already been sorted by type,
			// so we'll now group all the targets by LexRefType and sort their targets alphabetically before generating XHTML
			var organizedRefs = SortAndFilterLexRefsAndTargets(collection, cmOwner, config);

			// Now that we have things in the right order, try outputting one type at a time
			bool firstIteration = true;
			foreach (var referenceList in organizedRefs)
			{
				var xBldr = GenerateCrossReferenceChildren(config, pubDecorator, referenceList, cmOwner, settings);
				settings.ContentGenerator.BetweenCrossReferenceType(xBldr, config, firstIteration);
				firstIteration = false;
				bldr.Append(xBldr);
			}
		}

		/// <returns>A list (by Type) of lists of Lex Reference Targets (tupled with their references)</returns>
		private static List<List<Tuple<ISenseOrEntry, ILexReference>>> SortAndFilterLexRefsAndTargets(
			IEnumerable<ILexReference> collection, ICmObject cmOwner, ConfigurableDictionaryNode config)
		{
			var orderedTargets = new List<List<Tuple<ISenseOrEntry, ILexReference>>>();
			var curType = new Tuple<ILexRefType, string>(null, null);
			var allTargetsForType = new List<Tuple<ISenseOrEntry, ILexReference>>();
			foreach (var lexReference in collection)
			{
				var type = new Tuple<ILexRefType, string>(lexReference.OwnerType, LexRefDirection(lexReference, cmOwner));
				if (!type.Item1.Equals(curType.Item1)
					|| (LexRefTypeTags.IsAsymmetric((LexRefTypeTags.MappingTypes)type.Item1.MappingType) && !type.Item2.Equals(curType.Item2)))
				{
					MoveTargetsToMasterList(cmOwner, curType.Item1, config, allTargetsForType, orderedTargets);
				}
				curType = type;
				if (LexRefTypeTags.IsAsymmetric((LexRefTypeTags.MappingTypes)curType.Item1.MappingType) &&
					LexRefDirection(lexReference, cmOwner) == ":r" && lexReference.ConfigTargets.Any())
				{
					// In the reverse direction of an asymmetric lexical reference, we want only the first item.
					// See https://jira.sil.org/browse/LT-16427.
					allTargetsForType.Add(new Tuple<ISenseOrEntry, ILexReference>(lexReference.ConfigTargets.First(t => !IsOwner(t, cmOwner)), lexReference));
				}
				else
				{
					allTargetsForType.AddRange(lexReference.ConfigTargets
						.Select(target => new Tuple<ISenseOrEntry, ILexReference>(target, lexReference)));
				}
			}
			MoveTargetsToMasterList(cmOwner, curType.Item1, config, allTargetsForType, orderedTargets);
			return orderedTargets;
		}

		private static void MoveTargetsToMasterList(ICmObject cmOwner, ILexRefType curType, ConfigurableDictionaryNode config,
			List<Tuple<ISenseOrEntry, ILexReference>> bucketList, List<List<Tuple<ISenseOrEntry, ILexReference>>> lexRefTargetList)
		{
			if (bucketList.Count == 0)
				return;
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
					bucketList.Sort(CompareLexRefTargets);
			}
			lexRefTargetList.Add(new List<Tuple<ISenseOrEntry, ILexReference>>(bucketList));
			bucketList.Clear();
		}

		private static bool IsOwner(ISenseOrEntry target, ICmObject owner)
		{
			return target.Item.Guid.Equals(owner.Guid);
		}

		private static int CompareLexRefTargets(Tuple<ISenseOrEntry, ILexReference> lhs,
			Tuple<ISenseOrEntry, ILexReference> rhs)
		{
			var wsId = lhs.Item1.Item.Cache.ServiceLocator.WritingSystemManager.Get(lhs.Item1.HeadWord.get_WritingSystem(0));
			var comparer = new WritingSystemComparer(wsId);
			var result = comparer.Compare(lhs.Item1.HeadWord.Text, rhs.Item1.HeadWord.Text);
			return result;
		}

		/// <returns>Content for Targets and nodes, except Type, which is returned in ref string typeXHTML</returns>
		private static IFragment GenerateCrossReferenceChildren(ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator,
			List<Tuple<ISenseOrEntry, ILexReference>> referenceList, object collectionOwner, GeneratorSettings settings)
		{
			if (config.ReferencedOrDirectChildren == null)
				return settings.ContentGenerator.CreateFragment();
			var xBldr = settings.ContentGenerator.CreateFragment();
			using (var xw = settings.ContentGenerator.CreateWriter(xBldr))
			{
				settings.ContentGenerator.BeginCrossReference(xw, config, IsBlockProperty(config), GetCollectionItemClassAttribute(config));
				var targetInfo = referenceList.FirstOrDefault();
				if (targetInfo == null)
					return settings.ContentGenerator.CreateFragment();
				var reference = targetInfo.Item2;
				if (targetInfo.Item1 == null || (!publicationDecorator?.IsPublishableLexRef(reference.Hvo) ?? false))
				{
					return settings.ContentGenerator.CreateFragment();
				}

				if (LexRefTypeTags.IsUnidirectional((LexRefTypeTags.MappingTypes)reference.OwnerType.MappingType) &&
					LexRefDirection(reference, collectionOwner) == ":r")
				{
					return settings.ContentGenerator.CreateFragment();
				}

				bool first = true;
				foreach (var child in config.ReferencedOrDirectChildren.Where(c => c.IsEnabled))
				{
					switch (child.FieldDescription)
					{
						case "ConfigTargets":
							var content = settings.ContentGenerator.CreateFragment();
							foreach (var referenceListItem in referenceList)
							{
								var referenceItem = referenceListItem.Item2;
								var targetItem = referenceListItem.Item1;
								content.Append(GenerateCollectionItemContent(child, publicationDecorator, targetItem, referenceItem, settings, first));
								first = false;
							}
							if (!content.IsNullOrEmpty())
							{
								// targets
								settings.ContentGenerator.AddCollection(xw, child, IsBlockProperty(child),
									CssGenerator.GetClassAttributeForConfig(child), content);
								settings.StylesGenerator.AddStyles(child);
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
									child.CSSClassNameOverride = CssGenerator.GetClassAttributeForConfig(child);
								// Flag to prepend "Reverse" to child.SubField when it is used.
								settings.ContentGenerator.WriteProcessedContents(xw, config,
									GenerateContentForFieldByReflection(reference, child, publicationDecorator, settings, fUseReverseSubField: true));
							}
							else
							{
								settings.ContentGenerator.WriteProcessedContents(xw, config,
									GenerateContentForFieldByReflection(reference, child, publicationDecorator, settings));
							}
							break;
						default:
							throw new NotImplementedException("The field " + child.FieldDescription + " is not supported on Cross References or Lexical Relations. Supported fields are OwnerType and ConfigTargets");
					}
				}
				settings.ContentGenerator.EndCrossReference(xw); // config
				xw.Flush();
			}
			return xBldr;
		}

		private static IFragment GenerateSubentryTypeChild(ConfigurableDictionaryNode config, DictionaryPublicationDecorator publicationDecorator,
			ILexEntry subEntry, object mainEntryOrSense, GeneratorSettings settings)
		{
			if (!config.IsEnabled)
				return settings.ContentGenerator.CreateFragment();

			var complexEntryRef = EntryRefForSubentry(subEntry, mainEntryOrSense);
			return complexEntryRef == null
				? settings.ContentGenerator.CreateFragment()
				: GenerateContentForCollection(complexEntryRef.ComplexEntryTypesRS, config, publicationDecorator, subEntry, settings);
		}

		private static ILexEntryRef EntryRefForSubentry(ILexEntry subEntry, object mainEntryOrSense)
		{
			var mainEntry = mainEntryOrSense as ILexEntry ?? ((ILexSense)mainEntryOrSense).Entry;
			var complexEntryRef = subEntry.ComplexFormEntryRefs.FirstOrDefault(entryRef => entryRef.PrimaryLexemesRS.Contains(mainEntry) // subsubentries
																						|| entryRef.PrimaryEntryRoots.Contains(mainEntry)); // subs under sense
			return complexEntryRef;
		}

		private static IFragment GenerateSenseNumberSpanIfNeeded(ConfigurableDictionaryNode senseConfigNode, bool isThisSenseNumbered, ref SenseInfo info, GeneratorSettings settings)
		{
			if (!isThisSenseNumbered)
				return settings.ContentGenerator.CreateFragment();

			var senseOptions = senseConfigNode.DictionaryNodeOptions as DictionaryNodeSenseOptions;

			var formattedSenseNumber = GetSenseNumber(senseOptions.NumberingStyle, ref info);
			info.HomographConfig = settings.Cache.ServiceLocator.GetInstance<HomographConfiguration>();
			var senseNumberWs = string.IsNullOrEmpty(info.HomographConfig.WritingSystem) ? "en" : info.HomographConfig.WritingSystem;
			if (string.IsNullOrEmpty(formattedSenseNumber))
				return settings.ContentGenerator.CreateFragment();
			return settings.ContentGenerator.GenerateSenseNumber(senseConfigNode, formattedSenseNumber, senseNumberWs);
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
					// Use the digits from the CustomHomographNumbers if they are defined
					if (info.HomographConfig.CustomHomographNumbers.Count == 10)
					{
						for (var digit = 0; digit < 10; ++digit)
						{
							nextNumber = nextNumber.Replace(digit.ToString(), info.HomographConfig.CustomHomographNumbers[digit]);
						}
					}
					break;
			}
			info.SenseOutlineNumber = GenerateSenseOutlineNumber(info, nextNumber);
			return info.SenseOutlineNumber;
		}

		private static string GenerateSenseOutlineNumber(SenseInfo info, string nextNumber)
		{
			if (info.ParentSenseNumberingStyle == "%j")
				info.SenseOutlineNumber = string.Format("{0}{1}", info.SenseOutlineNumber, nextNumber);
			else if (info.ParentSenseNumberingStyle == "%.")
				info.SenseOutlineNumber = string.Format("{0}.{1}", info.SenseOutlineNumber, nextNumber);
			else
				info.SenseOutlineNumber = nextNumber;

			return info.SenseOutlineNumber;
		}

		private static string GetAlphaSenseCounter(string numberingStyle, int senseNumber)
		{
			var asciiBytes = 64; // char 'A'
			asciiBytes = asciiBytes + senseNumber;
			var nextNumber = ((char)(asciiBytes)).ToString();
			if (numberingStyle == "%a")
				nextNumber = nextNumber.ToLower();
			return nextNumber;
		}

		private static string GetRomanSenseCounter(string numberingStyle, int senseNumber)
		{
			string roman = string.Empty;
			roman = RomanNumerals.IntToRoman(senseNumber);
			if (numberingStyle == "%i")
				roman = roman.ToLower();
			return roman;
		}

		private static IFragment GenerateContentForICmObject(ICmObject propertyValue, ConfigurableDictionaryNode config, GeneratorSettings settings)
		{
			// Don't export if there is no such data
			if (propertyValue == null || config.ReferencedOrDirectChildren == null || !config.ReferencedOrDirectChildren.Any(node => node.IsEnabled))
				return settings.ContentGenerator.CreateFragment();
			var bldr = settings.ContentGenerator.CreateFragment();
			foreach (var child in config.ReferencedOrDirectChildren)
			{
				var content = GenerateContentForFieldByReflection(propertyValue, child, null, settings);
				bldr.Append(content);
			}

			if (bldr.Length() > 0)
			{
				var className = settings.StylesGenerator.AddStyles(config).Trim('.'); ;
				return settings.ContentGenerator.WriteProcessedObject(config, false, bldr, className);
			}
			return bldr;
		}

		/// <summary>Write the class element in the span for an individual item in the collection</summary>
		internal static string GetCollectionItemClassAttribute(ConfigurableDictionaryNode config)
		{
			var classAtt = CssGenerator.GetClassAttributeForCollectionItem(config);
			if (config.ReferencedNode != null)
				classAtt = string.Format("{0} {1}", classAtt, CssGenerator.GetClassAttributeForCollectionItem(config.ReferencedNode));
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

		internal static bool IsCollectionNode(ConfigurableDictionaryNode configNode, LcmCache cache)
		{
			return GetPropertyTypeForConfigurationNode(configNode, cache) == PropertyType.CollectionType;
		}

		/// <summary>
		/// Determines if the user has specified that this item should generate content.
		/// <returns><c>true</c> if the user has ticked the list item that applies to this object</returns>
		/// </summary>
		internal static bool IsListItemSelectedForExport(ConfigurableDictionaryNode config, object listItem, object parent = null)
		{
			var listOptions = config.DictionaryNodeOptions as DictionaryNodeListOptions;
			if (listOptions == null)
				throw new ArgumentException(string.Format("This configuration node had no options and we were expecting them: {0} ({1})", config.DisplayLabel, config.FieldDescription), "config");

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
				case DictionaryNodeListOptions.ListIds.Variant:
				case DictionaryNodeListOptions.ListIds.Complex:
				case DictionaryNodeListOptions.ListIds.Minor:
				case DictionaryNodeListOptions.ListIds.Note:
					return IsListItemSelectedForExportInternal(listOptions.ListId, listItem, selectedListOptions);
				case DictionaryNodeListOptions.ListIds.Entry:
				case DictionaryNodeListOptions.ListIds.Sense:
					var lexRef = (ILexReference)listItem;
					var entryTypeGuid = lexRef.OwnerType.Guid;
					if (selectedListOptions.Contains(entryTypeGuid))
						return true;
					var entryTypeGuidAndDirection = new Tuple<Guid, string>(entryTypeGuid, LexRefDirection(lexRef, parent));
					return forwardReverseOptions.Contains(entryTypeGuidAndDirection);
				case DictionaryNodeListOptions.ListIds.None:
					return true;
				default:
					Debug.WriteLine("Unhandled list ID encountered: " + listOptions.ListId);
					return true;
			}
		}

		private static bool IsListItemSelectedForExportInternal(DictionaryNodeListOptions.ListIds listId,
			object listItem, IEnumerable<Guid> selectedListOptions)
		{
			var entryTypeGuids = new HashSet<Guid>();
			var entryRef = listItem as ILexEntryRef;
			var entry = listItem as ILexEntry;
			var entryType = listItem as ILexEntryType;
			var note = listItem as ILexExtendedNote;
			if (entryRef != null)
			{
				if (listId == DictionaryNodeListOptions.ListIds.Variant || listId == DictionaryNodeListOptions.ListIds.Minor)
					GetVariantTypeGuidsForEntryRef(entryRef, entryTypeGuids);
				if (listId == DictionaryNodeListOptions.ListIds.Complex || listId == DictionaryNodeListOptions.ListIds.Minor)
					GetComplexFormTypeGuidsForEntryRef(entryRef, entryTypeGuids);
			}
			else if (entry != null)
			{
				if (listId == DictionaryNodeListOptions.ListIds.Variant || listId == DictionaryNodeListOptions.ListIds.Minor)
					foreach (var variantEntryRef in entry.VariantEntryRefs)
						GetVariantTypeGuidsForEntryRef(variantEntryRef, entryTypeGuids);
				if (listId == DictionaryNodeListOptions.ListIds.Complex || listId == DictionaryNodeListOptions.ListIds.Minor)
					foreach (var complexFormEntryRef in entry.ComplexFormEntryRefs)
						GetComplexFormTypeGuidsForEntryRef(complexFormEntryRef, entryTypeGuids);
			}
			else if (entryType != null)
			{
				entryTypeGuids.Add(entryType.Guid);
			}
			else if (note != null)
			{
				if (listId == DictionaryNodeListOptions.ListIds.Note)
					GetExtendedNoteGuidsForEntryRef(note, entryTypeGuids);
			}
			return entryTypeGuids.Intersect(selectedListOptions).Any();
		}

		private static void GetVariantTypeGuidsForEntryRef(ILexEntryRef entryRef, HashSet<Guid> entryTypeGuids)
		{
			if (entryRef.VariantEntryTypesRS.Any())
				entryTypeGuids.UnionWith(entryRef.VariantEntryTypesRS.Select(guid => guid.Guid));
			else
				entryTypeGuids.Add(XmlViewsUtils.GetGuidForUnspecifiedVariantType());
		}

		private static void GetComplexFormTypeGuidsForEntryRef(ILexEntryRef entryRef, HashSet<Guid> entryTypeGuids)
		{
			if (entryRef.ComplexEntryTypesRS.Any())
				entryTypeGuids.UnionWith(entryRef.ComplexEntryTypesRS.Select(guid => guid.Guid));
			else
				entryTypeGuids.Add(XmlViewsUtils.GetGuidForUnspecifiedComplexFormType());
		}

		private static void GetExtendedNoteGuidsForEntryRef(ILexExtendedNote entryRef, HashSet<Guid> entryTypeGuids)
		{
			if (entryRef.ExtendedNoteTypeRA != null)
				entryTypeGuids.Add(entryRef.ExtendedNoteTypeRA.Guid);
			else
				entryTypeGuids.Add(XmlViewsUtils.GetGuidForUnspecifiedExtendedNoteType());
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
		/// <param name="collection"></param>
		/// <exception cref="ArgumentException">if the object given is null, or not a handled collection</exception>
		/// <returns></returns>
		private static bool IsCollectionEmpty(object collection)
		{
			if (collection == null)
			{
				throw new ArgumentNullException("collection");
			}
			if (collection is IEnumerable)
			{
				return !(((IEnumerable)collection).Cast<object>().Any());
			}
			if (collection is ILcmVector)
			{
				return ((ILcmVector)collection).ToHvoArray().Length == 0;
			}
			throw new ArgumentException(@"Cannot test something that isn't a collection", "collection");
		}

		/// <summary>
		/// This method generates XHTML content for a given object
		/// </summary>
		/// <param name="field">This is the object that owns the property, needed to look up writing system info for virtual string fields</param>
		/// <param name="propertyValue">data to generate xhtml for</param>
		/// <param name="config"></param>
		/// <param name="settings"></param>
		private static IFragment GenerateContentForValue(object field, object propertyValue, ConfigurableDictionaryNode config, GeneratorSettings settings)
		{
			// If we're working with a headword, either for this entry or another one (Variant or Complex Form, etc.), store that entry's GUID
			// so we can generate a link to the main or minor entry for this headword.
			var guid = Guid.Empty;
			if (config.IsHeadWord)
			{
				if (field is ILexEntry)
				{
					// For Complex Forms, don't generate the reference if we are not going to publish the entry to Webonary.
					if (settings.IsWebExport &&
						!((ILexEntry)field).PublishAsMinorEntry &&
						((ILexEntry)field).EntryRefsOS.Count > 0)
					{
						guid = Guid.Empty;
					}
					else
					{
						guid = ((ILexEntry)field).Guid;
					}
				}
				else if (field is ILexEntryRef)
				{
					// For Variants, don't generate the reference if we are not going to publish the entry to Webonary.
					if (settings.IsWebExport &&
						!((ILexEntryRef)field).OwningEntry.PublishAsMinorEntry)
					{
						guid = Guid.Empty;
					}
					else
					{
						guid = ((ILexEntryRef)field).OwningEntry.Guid;
					}
				}
				else if (field is ISenseOrEntry)
					guid = ((ISenseOrEntry)field).EntryGuid;
				else if (field is ILexSense)
					guid = ((ILexSense)field).OwnerOfClass(LexEntryTags.kClassId).Guid;
				else
					Debug.WriteLine(String.Format("Need to find Entry Guid for {0}",
						field == null ? DictionaryConfigurationMigrator.BuildPathStringFromNode(config) : field.GetType().Name));
			}

			if (propertyValue is ITsString)
			{
				if (!TsStringUtils.IsNullOrEmpty((ITsString)propertyValue))
				{
					var content = GenerateContentForString((ITsString)propertyValue, config, settings, guid, true);
					if (!content.IsNullOrEmpty())
					{
						var className = settings.StylesGenerator.AddStyles(config).Trim('.'); ;
						return settings.ContentGenerator.WriteProcessedCollection(config, false, content, className);
					}
				}
				return settings.ContentGenerator.CreateFragment();
			}
			if (propertyValue is IMultiStringAccessor)
			{
				return GenerateContentForStrings((IMultiStringAccessor)propertyValue, config, settings, guid);
			}

			if (propertyValue is int)
			{
				var cssClassName = settings.StylesGenerator.AddStyles(config).Trim('.'); ;
				return settings.ContentGenerator.AddProperty(config, cssClassName, false, propertyValue.ToString());
			}
			if (propertyValue is DateTime)
			{
				var cssClassName = settings.StylesGenerator.AddStyles(config).Trim('.'); ;
				return settings.ContentGenerator.AddProperty(config, cssClassName, false, ((DateTime)propertyValue).ToLongDateString());
			}
			else if (propertyValue is GenDate)
			{
				var cssClassName = settings.StylesGenerator.AddStyles(config).Trim('.'); ;
				return settings.ContentGenerator.AddProperty(config, cssClassName, false, ((GenDate)propertyValue).ToLongString());
			}
			else if (propertyValue is IMultiAccessorBase)
			{
				if (field is ISenseOrEntry)
					return GenerateContentForVirtualStrings(((ISenseOrEntry)field).Item, (IMultiAccessorBase)propertyValue, config, settings, guid);
				return GenerateContentForVirtualStrings((ICmObject)field, (IMultiAccessorBase)propertyValue, config, settings, guid);
			}
			else if (propertyValue is string)
			{
				var cssClassName = settings.StylesGenerator.AddStyles(config).Trim('.');
				return settings.ContentGenerator.AddProperty(config, cssClassName, false, propertyValue.ToString());
			}
			else if (propertyValue is IStText)
			{
				var bldr = settings.ContentGenerator.CreateFragment();
				foreach (var para in (propertyValue as IStText).ParagraphsOS)
				{
					var stp = para as IStTxtPara;
					if (stp == null)
						continue;
					var contentPara = GenerateContentForString(stp.Contents, config, settings, guid, true);
					if (!contentPara.IsNullOrEmpty())
					{
						bldr.Append(contentPara);
						bldr.AppendBreak();
					}
				}
				if (bldr.Length() > 0)
					return settings.ContentGenerator.WriteProcessedCollection(config, true, bldr, GetClassNameAttributeForConfig(config));
				// bldr is empty of text
				return bldr;
			}
			else
			{
				if (propertyValue == null)
				{
					Debug.WriteLine(String.Format("Bad configuration node: {0}", DictionaryConfigurationMigrator.BuildPathStringFromNode(config)));
				}
				else
				{
					Debug.WriteLine(String.Format("What do I do with {0}?", propertyValue.GetType().Name));
				}
				return settings.ContentGenerator.CreateFragment();
			}
		}

		private static IFragment WriteElementContents(object propertyValue,
			ConfigurableDictionaryNode config, GeneratorSettings settings)
		{
			var content = propertyValue.ToString();
			if (!String.IsNullOrEmpty(content))
			{
				return settings.ContentGenerator.AddProperty(config, GetClassNameAttributeForConfig(config), IsBlockProperty(config), content);
			}

			return settings.ContentGenerator.CreateFragment();
		}

		private static IFragment GenerateContentForStrings(IMultiStringAccessor multiStringAccessor, ConfigurableDictionaryNode config,
			GeneratorSettings settings)
		{
			return GenerateContentForStrings(multiStringAccessor, config, settings, Guid.Empty);
		}

		/// <summary>
		/// This method will generate an XHTML span with a string for each selected writing system in the
		/// DictionaryWritingSystemOptions of the configuration that also has data in the given IMultiStringAccessor
		/// </summary>
		private static IFragment GenerateContentForStrings(IMultiStringAccessor multiStringAccessor, ConfigurableDictionaryNode config,
			GeneratorSettings settings, Guid guid)
		{
			var wsOptions = config.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
			if (wsOptions == null)
			{
				throw new ArgumentException(@"Configuration nodes for MultiString fields should have WritingSystemOptions", "config");
			}
			// TODO pH 2014.12: this can generate an empty span if no checked WS's contain data
			// gjm 2015.12 but this will help some (LT-16846)
			if (multiStringAccessor == null || multiStringAccessor.StringCount == 0)
				return settings.ContentGenerator.CreateFragment();
			var bldr = settings.ContentGenerator.CreateFragment();
			bool first = true;
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
				var contentItem = GenerateWsPrefixAndString(config, settings, wsOptions, wsId, bestString, guid, first);
				first = false;

				if (!String.IsNullOrEmpty(contentItem.ToString()))
					bldr.Append(contentItem);
			}
			if (bldr.Length() > 0)
			{
				var className = settings.StylesGenerator.AddStyles(config).Trim('.'); ;
				return settings.ContentGenerator.WriteProcessedCollection(config, false, bldr, className);
			}
			// bldr is empty of text
			return bldr;
		}

		/// <summary>
		/// This method will generate an XHTML span with a string for each selected writing system in the
		/// DictionaryWritingSystemOptions of the configuration that also has data in the given IMultiAccessorBase
		/// </summary>
		private static IFragment GenerateContentForVirtualStrings(ICmObject owningObject, IMultiAccessorBase multiStringAccessor,
																			ConfigurableDictionaryNode config, GeneratorSettings settings, Guid guid)
		{
			var wsOptions = config.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions;
			if (wsOptions == null)
			{
				throw new ArgumentException(@"Configuration nodes for MultiString fields should have WritingSystemOptions", "config");
			}

			var bldr = settings.ContentGenerator.CreateFragment();
			bool first = true;
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
				bldr.Append(GenerateWsPrefixAndString(config, settings, wsOptions, wsId, requestedString, guid, first));
				first = false;
			}
			if (bldr.Length() > 0)
			{
				var className = settings.StylesGenerator.AddStyles(config).Trim('.');
				return settings.ContentGenerator.WriteProcessedCollection(config, false, bldr, className);
			}
			// bldr is empty of text
			return bldr;
		}

		private static IFragment GenerateWsPrefixAndString(ConfigurableDictionaryNode config, GeneratorSettings settings,
			DictionaryNodeWritingSystemOptions wsOptions, int wsId, ITsString requestedString, Guid guid, bool first)
		{
			if (String.IsNullOrEmpty(requestedString.Text))
			{
				return settings.ContentGenerator.CreateFragment();
			}
			var wsName = settings.Cache.WritingSystemFactory.get_EngineOrNull(wsId).Id;
			var content = GenerateContentForString(requestedString, config, settings, guid, first, wsName);
			if (String.IsNullOrEmpty(content.ToString()))
				return settings.ContentGenerator.CreateFragment();
			return settings.ContentGenerator.GenerateWsPrefixWithString(config, settings, wsOptions.DisplayWritingSystemAbbreviations, wsId, content);
		}

		private static IFragment GenerateContentForString(ITsString fieldValue, ConfigurableDictionaryNode config,
			GeneratorSettings settings, string writingSystem = null)
		{
			return GenerateContentForString(fieldValue, config, settings, Guid.Empty, true, writingSystem);
		}

		private static IFragment GenerateContentForString(ITsString fieldValue, ConfigurableDictionaryNode config,
			GeneratorSettings settings, Guid linkTarget, bool first, string writingSystem = null)
		{
			if (TsStringUtils.IsNullOrEmpty(fieldValue))
				return settings.ContentGenerator.CreateFragment();
			if (writingSystem != null && writingSystem.Contains("audio"))
			{
				var fieldText = fieldValue.Text;
				if (fieldText.Contains("."))
				{
					var audioId = fieldText.Substring(0, fieldText.IndexOf(".", StringComparison.Ordinal));
					var srcAttr = GenerateSrcAttributeForMediaFromFilePath(fieldText, "AudioVisual", settings);
					var fileContent = GenerateContentForAudioFile(config, writingSystem, audioId, srcAttr, string.Empty, settings);
					var content = GenerateAudioWsContent(writingSystem, linkTarget, fileContent, settings);
					if (!content.IsNullOrEmpty())
						return settings.ContentGenerator.WriteProcessedObject(config, false, content, null);
				}
			}
			else if (config.IsCustomField && IsUSFM(fieldValue.Text))
			{
				// Review: Are any styles needed for tables?
				return GenerateTablesFromUSFM(fieldValue, config, settings, writingSystem);
			}
			else
			{
				// use the passed in writing system unless null
				// otherwise use the first option from the DictionaryNodeWritingSystemOptions or english if the options are null
				var bldr = settings.ContentGenerator.CreateFragment();
				try
				{
					using (var writer = settings.ContentGenerator.CreateWriter(bldr))
					{
						var rightToLeft = settings.RightToLeft;
						if (fieldValue.RunCount > 1)
						{
							writingSystem = writingSystem ?? GetLanguageFromFirstOption(config.DictionaryNodeOptions as DictionaryNodeWritingSystemOptions,
																settings.Cache);
							settings.ContentGenerator.StartMultiRunString(writer, config, writingSystem);
							var wsRtl = settings.Cache.WritingSystemFactory.get_Engine(writingSystem).RightToLeftScript;
							if (rightToLeft != wsRtl)
							{
								rightToLeft = wsRtl; // the outer WS direction will be used to identify embedded runs of the opposite direction.
								settings.ContentGenerator.StartBiDiWrapper(writer, config, rightToLeft);
							}
						}

						for (int i = 0; i < fieldValue.RunCount; i++)
						{
							var text = fieldValue.get_RunText(i);

							// If the text is "<Not Sure>" then don't display any text.
							if (text == LCModelStrings.NotSure)
								text = String.Empty;

							var props = fieldValue.get_Properties(i);
							var style = props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
							string externalLink = null;
							if (style == "Hyperlink")
							{
								externalLink = props.GetStrPropValue((int)FwTextPropType.ktptObjData);
							}
							writingSystem = settings.Cache.WritingSystemFactory.GetStrFromWs(fieldValue.get_WritingSystem(i));

							// The purpose of the boolean argument "first" is to determine if between content should be generated.
							// If first is false, the between content is generated; if first is true, between content is not generated.
							// In the case of a multi-run string, between content should only be placed at the start of the string, not inside the string.
							// When i > 0, we are dealing with a run in the middle of a multi-run string, so we pass value "true" for the argument "first" in order to suppress between content.
							if (i > 0)
								GenerateRunWithPossibleLink(settings, writingSystem, writer, style, text, linkTarget, rightToLeft, config, true, externalLink);
							else
								GenerateRunWithPossibleLink(settings, writingSystem, writer, style, text, linkTarget, rightToLeft, config, first, externalLink);
						}

						if (fieldValue.RunCount > 1)
						{
							if (rightToLeft != settings.RightToLeft)
								settings.ContentGenerator.EndBiDiWrapper(writer);
							settings.ContentGenerator.EndMultiRunString(writer);
						}

						writer.Flush();
						return bldr;
					}
				}
				catch (Exception e)
				{
					// We had some sort of error processing the string, possibly an unmatched surrogate pair.
					// Generate a span with 3 invalid unicode markers and an xml comment instead.
					var badStrBuilder = new StringBuilder();
					var unicodeChars = StringInfo.GetTextElementEnumerator(fieldValue.Text);
					while (unicodeChars.MoveNext())
					{
						if (unicodeChars.GetTextElement().Length == 1 &&
							char.IsSurrogate(unicodeChars.GetTextElement().ToCharArray()[0]))
						{
							badStrBuilder.Append("\u0FFF"); // Generate the 'character not found' char in place of the bad surrogate
						}
						else
						{
							badStrBuilder.Append(unicodeChars.GetTextElement());
						}
					}

					return settings.ContentGenerator.GenerateErrorContent(badStrBuilder);
				}
			}
			return settings.ContentGenerator.CreateFragment();
		}

		private static IFragment GenerateAudioWsContent(string wsId,
			Guid linkTarget, IFragment fileContent, GeneratorSettings settings)
		{
			return settings.ContentGenerator.AddAudioWsContent(wsId, linkTarget, fileContent);
		}

		private static void GenerateRunWithPossibleLink(GeneratorSettings settings, string writingSystem, IFragmentWriter writer, string style,
			string text, Guid linkDestination, bool rightToLeft, ConfigurableDictionaryNode config, bool first, string externalLink = null)
		{
			settings.ContentGenerator.StartRun(writer, config, settings.PropertyTable, writingSystem, first);
			var wsRtl = settings.Cache.WritingSystemFactory.get_Engine(writingSystem).RightToLeftScript;
			if (rightToLeft != wsRtl)
			{
				settings.ContentGenerator.StartBiDiWrapper(writer, config, wsRtl);
			}
			if (!String.IsNullOrEmpty(style))
			{
				settings.ContentGenerator.SetRunStyle(writer, config, settings.PropertyTable, writingSystem, style, false);
			}
			if (linkDestination != Guid.Empty)
			{
				settings.ContentGenerator.StartLink(writer, config, linkDestination);
			}
			if (!string.IsNullOrEmpty(externalLink))
			{
				settings.ContentGenerator.StartLink(writer, config, externalLink.TrimStart((char)FwObjDataTypes.kodtExternalPathName));
			}
			if (text.Contains(TxtLineSplit))
			{
				var txtContents = text.Split(TxtLineSplit);
				for (int i = 0; i < txtContents.Count(); i++)
				{
					settings.ContentGenerator.AddToRunContent(writer, txtContents[i]);
					if (i == txtContents.Count() - 1)
						break;
					settings.ContentGenerator.AddLineBreakInRunContent(writer, config);
				}
			}
			else
			{
				settings.ContentGenerator.AddToRunContent(writer, text);
			}
			if (linkDestination != Guid.Empty || !string.IsNullOrEmpty(externalLink))
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
		/// <param name="audioIcon">Inner text for hyperlink (unicode icon for audio)</param>
		/// <param name="settings"/>
		private static IFragment GenerateContentForAudioFile(ConfigurableDictionaryNode config, string classname,
			string audioId, string srcAttribute, string audioIcon, GeneratorSettings settings)
		{
			if (string.IsNullOrEmpty(audioId) && string.IsNullOrEmpty(srcAttribute) && string.IsNullOrEmpty(audioIcon))
				return settings.ContentGenerator.CreateFragment();
			var safeAudioId = GetSafeXHTMLId(audioId);
			return settings.ContentGenerator.GenerateAudioLinkContent(config, classname, srcAttribute, audioIcon, safeAudioId);
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

		private static IFragment GenerateTablesFromUSFM(ITsString usfm, ConfigurableDictionaryNode config, GeneratorSettings settings, string writingSystem)
		{
			var delimiters = new Regex(@"\\d\s").Matches(usfm.Text);

			// If there is only one table, generate it
			if (delimiters.Count == 0 || delimiters.Count == 1 && delimiters[0].Index == 0)
			{
				return GenerateTableFromUSFM(usfm, config, settings, writingSystem);
			}

			var bldr = settings.ContentGenerator.CreateFragment();
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

			return bldr;
		}

		private static IFragment GenerateTableFromUSFM(ITsString usfm, ConfigurableDictionaryNode config, GeneratorSettings settings, string writingSystem)
		{
			var bldr = settings.ContentGenerator.CreateFragment();
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

				settings.ContentGenerator.StartTable(writer, config);
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
				settings.ContentGenerator.EndTable(writer, config);
				writer.Flush();
			}
			return bldr;
			// TODO (Hasso) 2021.06: impl for JSON
		}

		/// <summary>
		/// Generate the table title from USFM (\d descriptive title in USFM)
		/// </summary>
		private static void GenerateTableTitle(ITsString title, IFragmentWriter writer,
			ConfigurableDictionaryNode config, GeneratorSettings settings, string writingSystem)
		{
			settings.ContentGenerator.AddTableTitle(writer, GenerateContentForString(title, config, settings, writingSystem));
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
					GenerateError(writer, settings, config, junk);
				}
				else
				{
					// Yes, this strips all WS and formatting information, but for an error message, I'm not sure that we care
					GenerateError(writer, settings, config, string.Format(xWorksStrings.InvalidUSFM_TextAfterTR, junk));
				}
			}

			foreach (var cell in cells)
			{
				var contentsGroup = cell.Groups["content"];
				var cellLim = contentsGroup.Index + contentsGroup.Length;
				var contentXHTML = GenerateContentForString(rowUSFM.GetSubstring(contentsGroup.Index, cellLim), config, settings, writingSystem);
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

		private static void GenerateError(IFragmentWriter writer, GeneratorSettings settings, ConfigurableDictionaryNode config, string text)
		{
			var writingSystem = settings.Cache.WritingSystemFactory.GetStrFromWs(settings.Cache.WritingSystemFactory.UserWs);
			settings.ContentGenerator.StartRun(writer, null, settings.PropertyTable, writingSystem, true);
			settings.ContentGenerator.SetRunStyle(writer, null, settings.PropertyTable, writingSystem, null, true);
			if (text.Contains(TxtLineSplit))
			{
				var txtContents = text.Split(TxtLineSplit);
				for (var i = 0; i < txtContents.Length; i++)
				{
					settings.ContentGenerator.AddToRunContent(writer, txtContents[i]);
					if (i == txtContents.Length - 1)
						break;
					settings.ContentGenerator.AddLineBreakInRunContent(writer, config);
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
		/// <param name="wsOptions"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		private static string GetLanguageFromFirstOption(DictionaryNodeWritingSystemOptions wsOptions, LcmCache cache)
		{
			const string defaultLang = "en";
			if (wsOptions == null)
				return defaultLang;
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

		public static DictionaryPublicationDecorator GetPublicationDecoratorAndEntries(PropertyTable propertyTable, out int[] entriesToSave, string dictionaryType)
		{
			var cache = propertyTable.GetValue<LcmCache>("cache");
			if (cache == null)
			{
				throw new ArgumentException(@"PropertyTable had no cache", "mediator");
			}
			var clerk = propertyTable.GetValue<RecordClerk>("ActiveClerk", null);
			if (clerk == null)
			{
				throw new ArgumentException(@"PropertyTable had no clerk", "mediator");
			}

			ICmPossibility currentPublication;
			var currentPublicationString = propertyTable.GetStringProperty("SelectedPublication", xWorksStrings.AllEntriesPublication);
			if (currentPublicationString == xWorksStrings.AllEntriesPublication)
			{
				currentPublication = null;
			}
			else
			{
				currentPublication =
					(from item in cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS
					 where item.Name.UserDefaultWritingSystem.Text == currentPublicationString
					 select item).FirstOrDefault();
			}
			var decorator = new DictionaryPublicationDecorator(cache, clerk.VirtualListPublisher, clerk.VirtualFlid, currentPublication);
			entriesToSave = decorator.GetEntriesToPublish(propertyTable, clerk.VirtualFlid, dictionaryType);
			return decorator;
		}

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

		public class ConfigFragment
		{
			public ConfigurableDictionaryNode Config { get; }
			public IFragment Frag { get; }

			public ConfigFragment(ConfigurableDictionaryNode config, IFragment frag)
			{
				Config = config;
				Frag = frag;
			}
		}

		public class GeneratorSettings
		{
			public ILcmContentGenerator ContentGenerator = new LcmXhtmlGenerator();
			public ILcmStylesGenerator StylesGenerator = new CssGenerator();
			public LcmCache Cache { get; }
			public ReadOnlyPropertyTable PropertyTable { get; }
			public bool UseRelativePaths { get; }

			public bool UseUri { get; }
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
			: this(cache, propertyTable == null ? null : propertyTable, relativePaths, true, copyFiles, exportPath, rightToLeft, isWebExport, isTemplate)
			{
			}

			public GeneratorSettings(LcmCache cache, ReadOnlyPropertyTable propertyTable, bool relativePaths, bool useUri, bool copyFiles, string exportPath, bool rightToLeft = false, bool isWebExport = false, bool isTemplate = false)
			{
				if (cache == null || propertyTable == null)
				{
					throw new ArgumentNullException();
				}
				Cache = cache;
				PropertyTable = propertyTable;
				UseRelativePaths = relativePaths;
				UseUri = useUri;
				CopyFiles = copyFiles;
				ExportPath = exportPath;
				RightToLeft = rightToLeft;
				IsWebExport = isWebExport;
				IsTemplate = isTemplate;
				StylesGenerator.Init(propertyTable);
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
			public HomographConfiguration HomographConfig { get; set; }
		}
	}

	public interface ILcmStylesGenerator
	{
		void AddGlobalStyles(DictionaryConfigurationModel model, ReadOnlyPropertyTable propertyTable);
		string AddStyles(ConfigurableDictionaryNode node);
		void Init(ReadOnlyPropertyTable propertyTable);
	}

	/// <summary>
	/// A disposable writer for generating a fragment of a larger document.
	/// </summary>
	public interface IFragmentWriter : IDisposable
	{
		void Flush();
	}

	/// <summary>
	/// A document fragment
	/// </summary>
	public interface IFragment
	{
		void Append(IFragment frag);
		void AppendBreak();
		string ToString();
		int Length();
		bool IsNullOrEmpty();
		void Clear();
	}
}
