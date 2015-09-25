// Copyright (c) 2014-15 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs;

namespace LanguageExplorer.Areas.Grammar.Tools.PosEdit
{
	/// <summary>
	/// This class implements a utility to allow users to fix any part of speech guids that do not match the GOLD etic file.
	/// This is needed to simplify cross language analysis. We need this because there was a defect in FLEx for a number of years
	/// which did not use the correct guid for the items inserted into a new project.
	/// </summary>
	internal sealed class GoldEticGuidFixer : IUtility
	{
		#region IUtility implementation
		/// <summary>
		/// Get the main label describing the utility.
		/// </summary>
		public string Label { get { return LanguageExplorerResources.GoldEticGuidFixer_Label; } }

		/// <summary>
		/// Set the UtilityDlg.
		/// </summary>
		public UtilityDlg Dialog { set; private get; }

		/// <summary>
		/// Load any items in list box.
		/// </summary>
		public void LoadUtilities()
		{
			Dialog.Utilities.Items.Add(this);
		}

		/// <summary>
		/// Notify the utility is has been selected in the dlg.
		/// </summary>
		public void OnSelection()
		{
			Dialog.WhenDescription = LanguageExplorerResources.ksWhenToSetPartOfSpeechGUIDsToGold;
			Dialog.WhatDescription = LanguageExplorerResources.ksWhatIsSetPartOfSpeechGUIDsToGold;
			Dialog.RedoDescription = LanguageExplorerResources.ksGenericUtilityCannotUndo;
		}

		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		public void Process()
		{
			var cache = Dialog.PropertyTable.GetValue<FdoCache>("cache");
			NonUndoableUnitOfWorkHelper.DoSomehow(cache.ActionHandlerAccessor, () =>
			{
				var fixedGuids = ReplacePOSGuidsWithGoldEticGuids(cache);
				var caption = fixedGuids ? LanguageExplorerResources.GoldEticGuidFixer_Guids_changed_Title : LanguageExplorerResources.GoldEticGuidFixer_NoChangeTitle;
				var content = fixedGuids ? LanguageExplorerResources.GoldEticGuidFixer_GuidsChangedContent
												 : LanguageExplorerResources.GoldEticGuidFixer_NoChangeContent;
				MessageBox.Show(Dialog, content, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
			});
		}

		#endregion

		/// <summary>
		/// Override to return the Label.
		/// </summary>
		/// <returns>The Label</returns>
		public override string ToString()
		{
			return Label;
		}

		/// <summary />
		/// <remarks>This is only internal, because a test uses it.</remarks>
		/// <returns></returns>
		internal static bool ReplacePOSGuidsWithGoldEticGuids(FdoCache cache)
		{
			var goldDocument = new XmlDocument();
			goldDocument.Load(Path.Combine(FwDirectoryFinder.TemplateDirectory, "GOLDEtic.xml"));
			var itemsWithBadGuids = new Dictionary<IPartOfSpeech, string>();
			foreach(IPartOfSpeech pos in cache.LangProject.PartsOfSpeechOA.PossibilitiesOS)
			{
				CheckPossibilityGuidAgainstGold(pos, goldDocument, itemsWithBadGuids);
			}
			if(!itemsWithBadGuids.Any())
				return false;
			foreach(var badItem in itemsWithBadGuids)
			{
				ReplacePosItemWithCloneWithNewGuid(cache, badItem);
			}
			return true;
		}

		private static void ReplacePosItemWithCloneWithNewGuid(FdoCache cache, KeyValuePair<IPartOfSpeech, string> badItem)
		{
			IPartOfSpeech replacementPos;
			var badPartOfSpeech = badItem.Key;
			var correctedGuid = new Guid(badItem.Value);
			var ownerList = badPartOfSpeech.Owner as ICmPossibilityList;
			if(ownerList != null)
			{
				replacementPos = cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create(correctedGuid, ownerList);
				ownerList.PossibilitiesOS.Insert(badPartOfSpeech.IndexInOwner, replacementPos);
			}
			else
			{
				IPartOfSpeech badPartOfSpeechOwner = badPartOfSpeech.Owner as IPartOfSpeech;
				replacementPos = cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create(correctedGuid, badPartOfSpeechOwner);
				badPartOfSpeechOwner.SubPossibilitiesOS.Insert(badPartOfSpeech.IndexInOwner, replacementPos);
			}
			replacementPos.MergeObject(badPartOfSpeech);
		}

		private static void CheckPossibilityGuidAgainstGold(IPartOfSpeech pos,
																			 XmlDocument dom,
																			 Dictionary<IPartOfSpeech, string> itemsWithBadGuids)
		{
			if(!string.IsNullOrEmpty(pos.CatalogSourceId))
			{
				if(dom.SelectSingleNode(string.Format("//item[@id='{0}' and @guid='{1}']", pos.CatalogSourceId, pos.Guid.ToString())) == null)
				{
					var selectNodeWithoutGuid = dom.SelectSingleNode(string.Format("//item[@id='{0}']", pos.CatalogSourceId));

					itemsWithBadGuids[pos] = selectNodeWithoutGuid.Attributes["guid"].Value;
				}
			}
			if(pos.SubPossibilitiesOS != null)
			{
				foreach(IPartOfSpeech subPos in pos.SubPossibilitiesOS)
				{
					CheckPossibilityGuidAgainstGold(subPos, dom, itemsWithBadGuids);
				}
			}
		}
	}
}
