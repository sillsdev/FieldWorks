// Copyright (c) 2013-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// Browse viewer used for assigning phonological features to phonemes
	/// </summary>
	internal sealed class BrowseViewerPhonologicalFeatures : BrowseViewer
	{
		/// <summary>
		/// The sortItemProvider is typically the RecordList that implements sorting and
		/// filtering of the items we are displaying.
		/// The data access passed typically is a decorator for the one in the cache, adding
		/// the sorted, filtered list of objects accessed as property madeUpFieldIdentifier of hvoRoot.
		/// </summary>
		public BrowseViewerPhonologicalFeatures(XElement nodeSpec, int hvoRoot, LcmCache cache, ISortItemProvider sortItemProvider, ISilDataAccessManaged sda, UiWidgetController uiWidgetController)
			: base(nodeSpec, hvoRoot, cache, sortItemProvider, sda, uiWidgetController)
		{ }

		///  <summary />
		protected override BulkEditBar CreateBulkEditBar(BrowseViewer bv, XElement spec, FlexComponentParameters flexComponentParameters, LcmCache cache)
		{
			return new BulkEditBarPhonologicalFeatures(bv, spec, flexComponentParameters, cache);
		}

		/// <summary>
		/// Bulk edit bar used for assigning phonological features to phonemes
		/// </summary>
		private sealed class BulkEditBarPhonologicalFeatures : BulkEditBar
		{
			/// <summary>
			/// Create one
			/// </summary>
			/// <param name="bv">The BrowseViewer that it is part of.</param>
			/// <param name="spec">The parameters element of the BV, containing the
			/// 'columns' elements that specify the BE bar (among other things).</param>
			/// <param name="flexComponentParameters"></param>
			/// <param name="cache"></param>
			public BulkEditBarPhonologicalFeatures(BrowseViewer bv, XElement spec, FlexComponentParameters flexComponentParameters, LcmCache cache) :
				base(bv, spec, flexComponentParameters, cache)
			{
				m_operationsTabControl.Controls.Remove(BulkCopyTab);
				m_operationsTabControl.Controls.Remove(ClickCopyTab);
				m_operationsTabControl.Controls.Remove(FindReplaceTab);
				m_operationsTabControl.Controls.Remove(TransduceTab);
				m_operationsTabControl.Controls.Remove(DeleteTab);
				if (m_listChoiceControl != null)
				{
					m_listChoiceControl.Text = string.Empty;
				}
				EnablePreviewApplyForListChoice();
			}

			private void BulkEditBarPhonologicalFeatures_EnableTargetFeatureCombo(object sender, TargetFeatureEventArgs e)
			{
				TargetCombo.Enabled = e.Enable;
			}

			protected override BulkEditItem MakeItem(XElement colSpec)
			{
				var bei = base.MakeItem(colSpec);
				if (bei == null)
				{
					return null;
				}
				var besc = bei.BulkEditControl as PhonologicalFeatureEditor;
				if (besc != null)
				{
					besc.EnableTargetFeatureCombo += BulkEditBarPhonologicalFeatures_EnableTargetFeatureCombo;
				}
				return bei;
			}

			protected override void ShowPreviewItems(ProgressState state)
			{
				m_bv.BrowseView.Vc.MultiColumnPreview = false;
				var itemsToChange = ItemsToChange(false);
				var bei = m_beItems[m_itemIndex];
				var phonFeatEditor = bei.BulkEditControl as PhonologicalFeatureEditor;
				if (phonFeatEditor == null)
				{
					// User chose to remove the targeted feature
					bei.BulkEditControl.FakeDoit(itemsToChange, XMLViewsDataCache.ktagAlternateValue, XMLViewsDataCache.ktagItemEnabled, state);
				}
				else
				{
					if (!phonFeatEditor.SelectedItemIsFsFeatStruc)
					{
						// User chose one of the values of the targeted feature
						phonFeatEditor.FakeDoit(itemsToChange, XMLViewsDataCache.ktagAlternateValue, XMLViewsDataCache.ktagItemEnabled, state);
					}
					else
					{
						// User built a FsFeatStruc with the features and values to change.
						// This means we have to find the columns for each feature in the FsFeatStruc and
						// then show the change for that feature in that column.
						var selectedHvo = phonFeatEditor.SelectedHvo;
						var selectedLabel = phonFeatEditor.SelectedLabel;
						var featureValuePairs = phonFeatEditor.FeatureValuePairsInSelectedFeatStruc;
						var featureAbbreviations = featureValuePairs.Select(s =>
						{
							var i = s.IndexOf(":");
							return s.Substring(0, i);
						});
						m_bv.BrowseView.Vc.MultiColumnPreview = true;
						for (var iColumn = 0; iColumn < m_beItems.Count(); iColumn++)
						{
							if (m_beItems[iColumn] == null)
							{
								continue;
							}
							var pfe = m_beItems[iColumn].BulkEditControl as PhonologicalFeatureEditor;
							if (pfe == null)
							{
								continue;
							}
							pfe.ClearPreviousPreviews(itemsToChange, XMLViewsDataCache.ktagAlternateValueMultiBase + iColumn + 1);
							if (!featureAbbreviations.Contains(pfe.FeatDefnAbbr))
							{
								continue;
							}
							var tempSelectedHvo = pfe.SelectedHvo;
							pfe.SelectedHvo = selectedHvo;
							var tempSelectedLabel = pfe.SelectedLabel;
							pfe.SelectedLabel = selectedLabel;
							pfe.FakeDoit(itemsToChange, XMLViewsDataCache.ktagAlternateValueMultiBase + iColumn + 1, XMLViewsDataCache.ktagItemEnabled, state);
							pfe.SelectedHvo = tempSelectedHvo;
							pfe.SelectedLabel = tempSelectedLabel;
						}
					}
				}
			}

			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (IsDisposed)
				{
					// No need to run it more than once.
					return;
				}

				if (disposing)
				{
					foreach (var bei in m_beItems)
					{
						var besc = bei?.BulkEditControl as PhonologicalFeatureEditor;
						if (besc != null)
						{
							besc.EnableTargetFeatureCombo -= BulkEditBarPhonologicalFeatures_EnableTargetFeatureCombo;
						}
					}
				}

				base.Dispose(disposing);
			}
		}
	}
}