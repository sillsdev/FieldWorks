// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.DictionaryConfiguration;
using LanguageExplorer.LcmUi;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.LCModel;

namespace LanguageExplorer
{
	internal static class LanguageExplorerServices
	{
		/// <summary>
		/// Get the pathname for the GOLDEtic.xml file.
		/// </summary>
		internal static string GOLDEticXmlPathname => Path.Combine(FwDirectoryFinder.TemplateDirectory, "GOLDEtic.xml");

		/// <summary>
		/// Make a standard Win32 color from three components.
		/// </summary>
		internal static uint RGB(int r, int g, int b)
		{
			return (uint)((byte)r | ((byte)g << 8) | ((byte)b << 16));
		}

		/// <summary>
		/// Tell the user why we aren't jumping to his record
		/// </summary>
		internal static void GiveSimpleWarning(Form form, string helpFile, ExclusionReasonCode xrc)
		{
			string caption;
			string reason;
			string shlpTopic;
			switch (xrc)
			{
				case ExclusionReasonCode.NotInPublication:
					caption = LanguageExplorerResources.ksEntryNotPublished;
					reason = LanguageExplorerResources.ksEntryNotPublishedReason;
					shlpTopic = "User_Interface/Menus/Edit/Find_a_lexical_entry.htm";
					break;
				case ExclusionReasonCode.ExcludedHeadword:
					caption = LanguageExplorerResources.ksMainNotShown;
					reason = LanguageExplorerResources.ksMainNotShownReason;
					shlpTopic = "khtpMainEntryNotShown";
					break;
				case ExclusionReasonCode.ExcludedMinorEntry:
					caption = LanguageExplorerResources.ksMinorNotShown;
					reason = LanguageExplorerResources.ksMinorNotShownReason;
					shlpTopic = "khtpMinorEntryNotShown";
					break;
				default:
					throw new ArgumentException("Unknown ExclusionReasonCode");
			}
			// TODO-Linux: Help is not implemented on Mono
			MessageBox.Show(form, string.Format(LanguageExplorerResources.ksSelectedEntryNotInDict, reason), caption, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, 0, helpFile, HelpNavigator.Topic, shlpTopic);
		}

		internal static void LexiconLookup(LcmCache cache, FlexComponentParameters flexComponentParameters, RootSite rootSite)
		{
			rootSite.RootBox.Selection.GetWordLimitsOfSelection(out var ichMin, out var ichLim, out var hvo, out var tag, out var ws, out _);
			if (ichLim > ichMin)
			{
				LexEntryUi.DisplayOrCreateEntry(cache, hvo, tag, ws, ichMin, ichLim, flexComponentParameters.PropertyTable.GetValue<IWin32Window>(FwUtilsConstants.window), flexComponentParameters, flexComponentParameters.PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), FwUtilsConstants.UserHelpFile);
			}
		}

		internal static void AddToLexicon(LcmCache lcmCache, FlexComponentParameters flexComponentParameters, RootSite rootSite)
		{
			rootSite.RootBox.Selection.GetWordLimitsOfSelection(out var ichMin, out var ichLim, out _, out _, out var ws, out var tss);
			if (ws == 0)
			{
				ws = tss.GetWsFromString(ichMin, ichLim);
			}
			if (ichLim <= ichMin || ws != lcmCache.DefaultVernWs)
			{
				return;
			}
			var tsb = tss.GetBldr();
			if (ichLim < tsb.Length)
			{
				tsb.Replace(ichLim, tsb.Length, null, null);
			}

			if (ichMin > 0)
			{
				tsb.Replace(0, ichMin, null, null);
			}
			var tssForm = tsb.GetString();
			using (var dlg = new InsertEntryDlg())
			{
				dlg.InitializeFlexComponent(flexComponentParameters);
				dlg.SetDlgInfo(lcmCache, tssForm);
				if (dlg.ShowDialog(flexComponentParameters.PropertyTable.GetValue<Form>(FwUtilsConstants.window)) == DialogResult.OK)
				{
					// is there anything special we want to do, such as jump to the new entry?
				}
			}
		}

		/// <summary>
		/// Report failure to make target a component of parent. If startedFromComplex is true, the user is looking
		/// at parent, and tried to make target a component. Otherwise, the user is looking at target, and
		/// tried to make parent a complex form.
		/// </summary>
		internal static void ReportLexEntryCircularReference(ICmObject parent, ICmObject target, bool startedFromComplex)
		{
			MessageBox.Show(Form.ActiveForm, String.Format(startedFromComplex ? FwCoreDlgs.ksComponentIsComponent : FwCoreDlgs.ksComplexFormIsComponent,
					target is ILexEntry ? FwCoreDlgs.ksEntry : FwCoreDlgs.ksSense,
					startedFromComplex ? ((ILexEntry)parent).HeadWord.Text : target.ShortName),
				FwCoreDlgs.ksWhichIsComponent, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		/// <summary>
		/// Return true if the target array starts with the objects in the match array.
		/// </summary>
		internal static bool StartsWith(object[] target, object[] match)
		{
			if (match.Length > target.Length)
			{
				return false;
			}
			for (var i = 0; i < match.Length; i++)
			{
				var x = target[i];
				var y = match[i];
				// We need this special expression because two objects wrapping the same integer
				// are, pathologically, not equal to each other.
				if (x != y && !(x is int xAsInt && y is int yAsInt && xAsInt == yAsInt))
				{
					return false;
				}
			}
			return true;
		}

		internal static IRecordList InterlinearTextsFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == LanguageExplorerConstants.InterlinearTexts, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{LanguageExplorerConstants.InterlinearTexts}'.");
			/*
            <clerk id="interlinearTexts">
              <dynamicloaderinfo assemblyPath="ITextDll.dll" class="SIL.FieldWorks.IText.InterlinearTextsRecordClerk" />
              <recordList owner="LangProject" property="InterestingTexts">
                <!-- We use a decorator here so it can override certain virtual properties and limit occurrences to interesting texts. -->
                <decoratorClass assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.InterestingTextsDecorator" />
              </recordList>
              <filterMethods />
              <sortMethods />
            </clerk>
			*/
			return CreateInterlinearTextsRecordList(LanguageExplorerConstants.InterlinearTexts, statusBar, cache.ServiceLocator, flexComponentParameters.PropertyTable, cache.LangProject);
		}

		internal static IRecordList InterlinearTextsForInfoPaneFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == LanguageExplorerConstants.InterlinearTextsRecordList, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{LanguageExplorerConstants.InterlinearTextsRecordList}'.");
			/*
            <clerk id="InterlinearTextsRecordClerk">
              <dynamicloaderinfo assemblyPath="ITextDll.dll" class="SIL.FieldWorks.IText.InterlinearTextsRecordClerk" />
              <recordList owner="LangProject" property="InterestingTexts">
                <!-- We use a decorator here so it can override certain virtual properties and limit occurrences to interesting texts. -->
                <decoratorClass assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.InterestingTextsDecorator" />
              </recordList>
              <filterMethods />
              <sortMethods />
            </clerk>
			*/
			return CreateInterlinearTextsRecordList(LanguageExplorerConstants.InterlinearTextsRecordList, statusBar, cache.ServiceLocator, flexComponentParameters.PropertyTable, cache.LangProject);
		}

		private static IRecordList CreateInterlinearTextsRecordList(string listId, StatusBar statusBar, ILcmServiceLocator serviceLocator, IPropertyTable propertyTable, ILangProject langProject)
		{
			return new InterlinearTextsRecordList(listId, statusBar, new InterestingTextsDecorator(serviceLocator, propertyTable), false, new VectorPropertyParameterObject(langProject, LanguageExplorerConstants.InterestingTexts, InterestingTextsDecorator.kflidInterestingTexts));
		}
	}
}
