// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.LCModel;

namespace LanguageExplorer
{
	internal static class LanguageExplorerServices
	{
		internal static void LexiconLookup(LcmCache cache, FlexComponentParameters flexComponentParameters, RootSite rootSite)
		{
			rootSite.RootBox.Selection.GetWordLimitsOfSelection(out var ichMin, out var ichLim, out var hvo, out var tag, out var ws, out _);
			if (ichLim > ichMin)
			{
				LexEntryUi.DisplayOrCreateEntry(cache, hvo, tag, ws, ichMin, ichLim, flexComponentParameters.PropertyTable.GetValue<IWin32Window>(FwUtils.window), flexComponentParameters, flexComponentParameters.PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), "UserHelpFile");
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
				if (dlg.ShowDialog(flexComponentParameters.PropertyTable.GetValue<Form>(FwUtils.window)) == DialogResult.OK)
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
	}
}
