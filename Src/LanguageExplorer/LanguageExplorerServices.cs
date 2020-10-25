// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.DictionaryConfiguration;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

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
				DisplayOrCreateEntry(cache, hvo, tag, ws, ichMin, ichLim, flexComponentParameters.PropertyTable.GetValue<IWin32Window>(FwUtilsConstants.window), flexComponentParameters, flexComponentParameters.PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), FwUtilsConstants.UserHelpFile);
			}
		}

		/// <summary />
		internal static void DisplayOrCreateEntry(LcmCache cache, int hvoSrc, int tagSrc, int wsSrc, int ichMin, int ichLim, IWin32Window owner, FlexComponentParameters flexComponentParameters, IHelpTopicProvider helpProvider, string helpFileKey)
		{
			var tssContext = cache.DomainDataByFlid.get_StringProp(hvoSrc, tagSrc);
			if (tssContext == null)
			{
				return;
			}
			var text = tssContext.Text;
			// If the string is empty, it might be because it's multilingual.  Try that alternative.
			// (See TE-6374.)
			if (text == null && wsSrc != 0)
			{
				tssContext = cache.DomainDataByFlid.get_MultiStringAlt(hvoSrc, tagSrc, wsSrc);
				if (tssContext != null)
				{
					text = tssContext.Text;
				}
			}
			ITsString tssWf = null;
			if (text != null)
			{
				tssWf = tssContext.GetSubstring(ichMin, ichLim);
			}
			if (tssWf == null || tssWf.Length == 0)
			{
				return;
			}
			// We want to limit the lookup to the current word's current analysis, if one exists.
			// See FWR-956.
			IWfiAnalysis wfa = null;
			if (tagSrc == StTxtParaTags.kflidContents)
			{
				IAnalysis anal = null;
				var para = cache.ServiceLocator.GetInstance<IStTxtParaRepository>().GetObject(hvoSrc);
				foreach (var seg in para.SegmentsOS)
				{
					if (seg.BeginOffset <= ichMin && seg.EndOffset >= ichLim)
					{
						var occurrence = seg.FindWagform(ichMin - seg.BeginOffset, ichLim - seg.BeginOffset, out _);
						if (occurrence != null)
						{
							anal = occurrence.Analysis;
						}
						break;
					}
				}
				if (anal != null)
				{
					switch (anal)
					{
						case IWfiAnalysis analysis:
							wfa = analysis;
							break;
						case IWfiGloss gloss:
							wfa = gloss.OwnerOfClass<IWfiAnalysis>();
							break;
					}
				}
			}
			DisplayEntries(cache, owner, flexComponentParameters, helpProvider, helpFileKey, tssWf, wfa);
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

		/// <summary>
		/// Creates a string representation of the supplied object, an XML string
		/// containing the required class attribute needed to create an
		/// instance using CreateObject, plus whatever gets added to the node by passing
		/// it to the PersistAsXml method of the object. The root element name is supplied
		/// as the elementName argument.
		/// </summary>
		internal static string PersistObject(IPersistAsXml persistAsXml, string elementName)
		{
			Guard.AgainstNull(persistAsXml, nameof(persistAsXml));
			var element = new XElement(elementName);
			PersistObject(persistAsXml, element);
			return element.ToString();
		}

		internal static void PersistObject(IPersistAsXml persistAsXml, XElement parent, string elementName)
		{
			Guard.AgainstNull(persistAsXml, nameof(persistAsXml));
			var element = new XElement(elementName);
			parent.Add(element);
			PersistObject(persistAsXml, element);
		}

		private static void PersistObject(IPersistAsXml persistAsXml, XElement element)
		{
			Guard.AgainstNull(persistAsXml, nameof(persistAsXml));
			element.Add(new XAttribute("class", persistAsXml.GetType().FullName));
			persistAsXml.PersistAsXml(element);
		}

		internal static void DisplayEntries(LcmCache cache, IWin32Window owner, FlexComponentParameters flexComponentParameters, IHelpTopicProvider helpProvider, string helpFileKey, ITsString tssWfIn, IWfiAnalysis wfa)
		{
			var tssWf = tssWfIn;
			var duplicates = false;
			var entries = cache.ServiceLocator.GetInstance<ILexEntryRepository>().FindEntriesForWordform(cache, tssWf, wfa, ref duplicates);
			if (duplicates)
			{
				MessageBox.Show(Form.ActiveForm, string.Format(LanguageExplorerResources.ksDuplicateWordformsMsg, tssWf.Text), LanguageExplorerResources.ksDuplicateWordformsCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			var styleSheet = FwUtils.StyleSheetFromPropertyTable(flexComponentParameters.PropertyTable);
			if (entries == null || entries.Count == 0)
			{
				var entry = ShowFindEntryDialog(cache, flexComponentParameters, tssWf, owner);
				if (entry == null)
				{
					return;
				}

				entries = new List<ILexEntry>(1) { entry };
			}
			//DisplayEntriesRecursive(cache, owner, flexComponentParameters, styleSheet, helpProvider, helpFileKey, entries, tssWf);
			// Loop showing the SummaryDialogForm as long as the user clicks the Other button
			// in that dialog.
			bool otherButtonClicked;
			do
			{
				using (var sdform = new SummaryDialogForm(new List<int>(entries.Select(le => le.Hvo)), helpProvider, helpFileKey, styleSheet, cache, flexComponentParameters.PropertyTable))
				{
					SetCurrentModalForm(sdform);
					if (owner == null)
					{
						sdform.StartPosition = FormStartPosition.CenterScreen;
					}
					sdform.ShowDialog(owner);
					if (sdform.ShouldLink)
					{
						sdform.LinkToLexicon();
					}
					otherButtonClicked = sdform.OtherButtonClicked;
					sdform.Activated -= s_activeModalForm_Activated;
				}
				if (otherButtonClicked)
				{
					// Look for another entry to display.  (If the user doesn't select another
					// entry, loop back and redisplay the current entry.)
					var entry = ShowFindEntryDialog(cache, flexComponentParameters, tssWf, owner);
					if (entry != null)
					{
						// We need a list that contains the entry we found to display on the
						// next go around of this loop.
						entries = new List<ILexEntry> { entry };
						tssWf = entry.HeadWord;
					}
				}
			} while (otherButtonClicked);
		}

		/// <summary>
		/// Set a Modal Form to temporarily show on top of all applications
		/// and have an icon that is accessible for the user after it goes behind other users.
		/// See http://support.ubs-icap.org/default.asp?11269
		/// </summary>
		private static void SetCurrentModalForm(Form newActiveModalForm)
		{
			newActiveModalForm.TopMost = true;
			newActiveModalForm.Activated += s_activeModalForm_Activated;
			newActiveModalForm.ShowInTaskbar = true;
		}

		/// <summary>
		/// setting TopMost in SetCurrentModalForm() forces a dialog to show on top of other applications
		/// in another process that want to launch this dialog (e.g. Paratext via WCF).
		/// but we don't want it to stay on top if the User switches to another application,
		/// so reset TopMost to false after it has launched to the top.
		/// </summary>
		private static void s_activeModalForm_Activated(object sender, EventArgs e)
		{
			((Form)sender).TopMost = false;
		}

		/// <summary>
		/// Launch the Find Entry dialog, and if one is created or selected return it.
		/// </summary>
		/// <returns>The HVO of the selected or created entry</returns>
		private static ILexEntry ShowFindEntryDialog(LcmCache cache, FlexComponentParameters flexComponentParameters, ITsString tssForm, IWin32Window owner)
		{
			using (var entryGoDlg = new EntryGoDlg())
			{
				entryGoDlg.InitializeFlexComponent(flexComponentParameters);
				// Temporarily set TopMost to true so it will launch above any calling app (e.g. Paratext)
				// but reset after activated.
				SetCurrentModalForm(entryGoDlg);
				var wp = new WindowParams
				{
					m_btnText = LanguageExplorerResources.ksShow,
					m_title = LanguageExplorerResources.ksFindInDictionary,
					m_label = LanguageExplorerResources.ksFind_
				};
				if (owner == null)
				{
					entryGoDlg.StartPosition = FormStartPosition.CenterScreen;
				}
				entryGoDlg.Owner = owner as Form;
				entryGoDlg.SetDlgInfo(cache, wp, tssForm);
				entryGoDlg.SetHelpTopic("khtpFindInDictionary");
				if (entryGoDlg.ShowDialog() == DialogResult.OK)
				{
					var entry = entryGoDlg.SelectedObject as ILexEntry;
					Debug.Assert(entry != null);
					entryGoDlg.Activated -= s_activeModalForm_Activated;
					return entry;
				}
				entryGoDlg.Activated -= s_activeModalForm_Activated;
			}
			return null;
		}

		public static bool ConsiderDeletingRelatedFile(this ICmFile me, IPropertyTable propertyTable)
		{
			var refs = me.ReferringObjects;
			if (refs.Count > 1)
			{
				return false; // exactly one if only this CmPicture uses it.
			}
			var path = me.InternalPath;
			if (Path.IsPathRooted(path))
			{
				return false; // don't delete external file
			}
			var msg = string.Format(LanguageExplorerResources.ksDeleteFileAlso, path);
			if (MessageBox.Show(Form.ActiveForm, msg, LanguageExplorerResources.ksDeleteFileCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
			{
				return false;
			}
			if (propertyTable != null && propertyTable.TryGetValue(LanguageExplorerConstants.App, out IFlexApp app))
			{
				app.PictureHolder.ReleasePicture(me.AbsoluteInternalPath);
			}
			var fileToDelete = me.AbsoluteInternalPath;
			propertyTable.GetValue<IFwMainWnd>(FwUtilsConstants.window).IdleQueue.Add(IdleQueuePriority.Low, FwUtils.TryToDeleteFile, fileToDelete);
			return false;
		}
	}
}
