// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// A browse viewer which has the select column hooked to an Active boolean
	///  (which is the UI name of the Disabled property of phonological rules,
	///   compound rules, ad hoc rules, and inflectional affix templates).  We
	///  only use this viewer with phonological rules and compound rules.
	/// </summary>
	internal class BrowseActiveViewer : BrowseViewer, IVwNotifyChange
	{
		/// <summary>
		/// Invoked when check box status alters. Typically there is only one item changed
		/// (the one the user clicked on); in this case, client should generate PropChanged as needed
		/// to update the display. When the user does something like CheckAll, a list is sent; in this case,
		/// AFTER invoking the event, the browse view does a Reconstruct, so generating PropChanged is not
		/// necessary (or helpful, unless some other view also needs updating).
		/// </summary>
		public event CheckBoxActiveChangedEventHandler CheckBoxActiveChanged;

		/// <summary />
		public BrowseActiveViewer(XElement configParamsElement, int hvoRoot, LcmCache cache, ISortItemProvider sortItemProvider, ISilDataAccessManaged sda)
			: base(configParamsElement, hvoRoot, cache, sortItemProvider, sda)
		{
		}

		/// <summary>
		/// Actually checks if val is 1, unchecks if val is 0.
		/// Toggles if value is -1
		/// </summary>
		internal override void ResetAll(BrowseViewerCheckState newState)
		{
			var changedHvos = ResetAllCollectChangedHvos(newState);
			ResetAllHandleBulkEditBar();
			var undoMessage = XMLViewsStrings.ksUndoToggle;
			var redoMessage = XMLViewsStrings.ksRedoToggle;
			switch (newState)
			{
				case BrowseViewerCheckState.UncheckAll:
					undoMessage = XMLViewsStrings.ksUndoUncheckAll;
					redoMessage = XMLViewsStrings.ksRedoUncheckAll;
					break;
				case BrowseViewerCheckState.CheckAll:
					undoMessage = XMLViewsStrings.ksUndoCheckAll;
					redoMessage = XMLViewsStrings.ksRedoCheckAll;
					break;
			}
			OnCheckBoxActiveChanged(changedHvos.ToArray(), undoMessage, redoMessage);
			ResetAllHandleReconstruct();
		}

		/// <summary />
		protected void OnCheckBoxActiveChanged(int[] hvosChanged, string undoMessage, string redoMessage)
		{
			try
			{
				if (CheckBoxActiveChanged == null)
				{
					return;
				}
				CheckBoxActiveChanged(this, new CheckBoxActiveChangedEventArgs(hvosChanged, undoMessage, redoMessage));
			}
			finally
			{
				// if a check box has changed by user, clear any record of preserving for a mixed class,
				// since we always want to try to stay consistent with the user's choice in checkbox state.
				if (!m_fInUpdateCheckedItems)
				{
					m_lastChangedSelectionListItemsClass = 0;
					OnListItemsAboutToChange(hvosChanged);
				}
			}
		}

		/// <summary>
		/// Receive a notification that a mouse-up has completed in the browse view.
		/// </summary>
		internal override void BrowseViewMouseUp(MouseEventArgs e)
		{
			var dpiX = GetDpiX();
			var selColWidth = BrowseView.Vc.SelectColumnWidth * dpiX / 72000;
			if (BrowseView.Vc.HasSelectColumn && e.X < selColWidth)
			{
				var hvosChanged = new[] { BrowseView.SelectedObject };
				// we've changed the state of a check box.
				OnCheckBoxActiveChanged(hvosChanged, XMLViewsStrings.ksUndoToggle, XMLViewsStrings.ksRedoToggle);
			}

			if (BulkEditBar != null && BulkEditBar.Visible)
			{
				BulkEditBar.UpdateEnableItems(BrowseView.SelectedObject);
			}
		}

		/// <summary>
		/// Determine whether the specified item is currently considered to be checked.
		/// </summary>
		internal override int GetCheckState(int hvoItem)
		{
			var fDisabled = false;
			var obj = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoItem);
			switch (obj.ClassID)
			{
				case PhRegularRuleTags.kClassId: // fall through
				case PhMetathesisRuleTags.kClassId:
					fDisabled = SpecialCache.get_BooleanProp(hvoItem, PhSegmentRuleTags.kflidDisabled);
					break;
				case MoEndoCompoundTags.kClassId: // fall through
				case MoExoCompoundTags.kClassId:
					fDisabled = SpecialCache.get_BooleanProp(hvoItem, MoCompoundRuleTags.kflidDisabled);
					break;
			}
			return fDisabled ? 0 : 1;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				SpecialCache.RemoveNotification(this);
			}

			base.Dispose(disposing);
		}

		#region Overrides of BrowseViewer

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			// Set the initial value
			var chvo = SpecialCache.get_VecSize(RootObjectHvo, MainTag);
			int[] contents;
			using (var arrayPtr = MarshalEx.ArrayToNative<int>(chvo))
			{
				SpecialCache.VecProp(RootObjectHvo, MainTag, chvo, out chvo, arrayPtr);
				contents = MarshalEx.NativeToArray<int>(arrayPtr, chvo);
			}

			foreach (var hvoItem in contents)
			{
				SetItemCheckedState(hvoItem, GetCheckState(hvoItem), false);
			}
			using (new ReconstructPreservingBVScrollPosition(this))
			{
			}
			SpecialCache.AddNotification(this);
		}

		#endregion

		#region IVwNotifyChange implementation

		/// <summary />
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (tag != PhSegmentRuleTags.kflidDisabled && tag != MoCompoundRuleTags.kflidDisabled)
			{
				return;
			}
			var currentValue = GetCheckState(hvo);
			SetItemCheckedState(hvo, currentValue, false);
		}

		#endregion
	}
}