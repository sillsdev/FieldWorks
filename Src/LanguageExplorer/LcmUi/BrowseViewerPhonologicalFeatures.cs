// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
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
	/// Browse viewer used for assigning phonolgical features to phonemes
	/// </summary>
	internal class BrowseViewerPhonologicalFeatures : BrowseViewer
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:BrowseViewer"/> class.
		/// The sortItemProvider is typically the RecordList that impelements sorting and
		/// filtering of the items we are displaying.
		/// The data access passed typically is a decorator for the one in the cache, adding
		/// the sorted, filtered list of objects accessed as property madeUpFieldIdentifier of hvoRoot.
		/// </summary>
		public BrowseViewerPhonologicalFeatures(XElement nodeSpec, int hvoRoot,
			LcmCache cache, ISortItemProvider sortItemProvider, ISilDataAccessManaged sda)
			: base(nodeSpec, hvoRoot, cache, sortItemProvider, sda)
		{ }

		///  <summary />
		protected override BulkEditBar CreateBulkEditBar(BrowseViewer bv, XElement spec, IPropertyTable propertyTable, LcmCache cache)
		{
			return new BulkEditBarPhonologicalFeatures(bv, spec, propertyTable, cache);
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
			/// <param name="propertyTable"></param>
			/// <param name="cache"></param>
			public BulkEditBarPhonologicalFeatures(BrowseViewer bv, XElement spec, IPropertyTable propertyTable, LcmCache cache) :
				base(bv, spec, propertyTable, cache)
			{
				m_operationsTabControl.Controls.Remove(BulkCopyTab);
				m_operationsTabControl.Controls.Remove(ClickCopyTab);
				m_operationsTabControl.Controls.Remove(FindReplaceTab);
				m_operationsTabControl.Controls.Remove(TransduceTab);
				m_operationsTabControl.Controls.Remove(DeleteTab);
				if (m_listChoiceControl != null)
					m_listChoiceControl.Text = "";
				EnablePreviewApplyForListChoice();
			}

			void BulkEditBarPhonologicalFeatures_EnableTargetFeatureCombo(object sender, TargetFeatureEventArgs e)
			{
				TargetCombo.Enabled = e.Enable;
			}

			protected override BulkEditItem MakeItem(XElement colSpec)
			{
				BulkEditItem bei = base.MakeItem(colSpec);
				if (bei == null)
					return null;
				var besc = bei.BulkEditControl as PhonologicalFeatureEditor;
				if (besc != null)
					besc.EnableTargetFeatureCombo +=
						new EventHandler<TargetFeatureEventArgs>(BulkEditBarPhonologicalFeatures_EnableTargetFeatureCombo);
				return bei;
			}

			protected override void ShowPreviewItems(ProgressState state)
			{
				m_bv.BrowseView.Vc.MultiColumnPreview = false;
				var itemsToChange = ItemsToChange(false);
				BulkEditItem bei = m_beItems[m_itemIndex];
				var phonFeatEditor = bei.BulkEditControl as PhonologicalFeatureEditor;
				if (phonFeatEditor == null)
				{ // User chose to remove the targeted feature
					bei.BulkEditControl.FakeDoit(itemsToChange, XMLViewsDataCache.ktagAlternateValue,
												 XMLViewsDataCache.ktagItemEnabled, state);
				}
				else
				{
					if (!phonFeatEditor.SelectedItemIsFsFeatStruc)
					{ // User chose one of the values of the targeted feature
						phonFeatEditor.FakeDoit(itemsToChange, XMLViewsDataCache.ktagAlternateValue,
												XMLViewsDataCache.ktagItemEnabled, state);
					}
					else
					{   // User built a FsFeatStruc with the features and values to change.
						// This means we have to find the columns for each feature in the FsFeatStruc and
						// then show the change for that feature in that column.
						int selectedHvo = phonFeatEditor.SelectedHvo;
						string selectedLabel = phonFeatEditor.SelectedLabel;

						string[] featureValuePairs = phonFeatEditor.FeatureValuePairsInSelectedFeatStruc;
						var featureAbbreviations = featureValuePairs.Select(s =>
						{
							int i = s.IndexOf(":");
							return s.Substring(0, i);
						});
						m_bv.BrowseView.Vc.MultiColumnPreview = true;
						for (int iColumn = 0; iColumn < m_beItems.Count(); iColumn++)
						{
							if (m_beItems[iColumn] == null)
								continue;

							var pfe = m_beItems[iColumn].BulkEditControl as PhonologicalFeatureEditor;
							if (pfe != null)
							{
								pfe.ClearPreviousPreviews(itemsToChange, XMLViewsDataCache.ktagAlternateValueMultiBase + iColumn + 1);
								if (featureAbbreviations.Contains(pfe.FeatDefnAbbr))
								{
									int tempSelectedHvo = pfe.SelectedHvo;
									pfe.SelectedHvo = selectedHvo;
									string tempSelectedLabel = pfe.SelectedLabel;
									pfe.SelectedLabel = selectedLabel;
									pfe.FakeDoit(itemsToChange, XMLViewsDataCache.ktagAlternateValueMultiBase + iColumn + 1,
												 XMLViewsDataCache.ktagItemEnabled, state);
									pfe.SelectedHvo = tempSelectedHvo;
									pfe.SelectedLabel = tempSelectedLabel;
								}
							}
						}
					}
				}
			}

			protected override void Dispose(bool disposing)
			{
				if (IsDisposed)
					return;

				if (disposing)
				{
					foreach (BulkEditItem bei in m_beItems)
					{
						if (bei != null)
						{
							var besc = bei.BulkEditControl as PhonologicalFeatureEditor;
							if (besc != null)
								besc.EnableTargetFeatureCombo -=
									new EventHandler<TargetFeatureEventArgs>(BulkEditBarPhonologicalFeatures_EnableTargetFeatureCombo);
						}
					}
				}

				base.Dispose(disposing);
			}
		}
	}
}