// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.PaneBar;
using SIL.FieldWorks.Resources;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lexicon.Tools.ClassifiedDictionary
{
	/// <summary>
	/// ITool implementation for the "lexiconClassifiedDictionary" tool in the "lexicon" area.
	/// </summary>
	[Export(AreaServices.LexiconAreaMachineName, typeof(ITool))]
	internal sealed class LexiconClassifiedDictionaryTool : ITool
	{
		private LexiconClassifiedDictionaryToolMenuHelper _lexicoClassifiedDictionaryMenuHelper;
		private PaneBarContainer _paneBarContainer;
		private IRecordList _recordList;
		[Import(AreaServices.LexiconAreaMachineName)]
		private IArea _area;

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			PaneBarContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _paneBarContainer);

			// Dispose after the main UI stuff.
			_lexicoClassifiedDictionaryMenuHelper.Dispose();

			_lexicoClassifiedDictionaryMenuHelper = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(LexiconArea.SemanticDomainList_LexiconArea, majorFlexComponentParameters.StatusBar, LexiconArea.SemanticDomainList_LexiconAreaFactoryMethod);
			}

			var panelButton = new PanelButton(majorFlexComponentParameters.FlexComponentParameters, null, "ShowFailingItems-lexiconClassifiedDictionary", LexiconResources.Show_Unused_Items, LexiconResources.Show_Unused_Items)
			{
				Dock = DockStyle.Right
			};
			var xmlDocViewPaneBar = new PaneBar();
			xmlDocViewPaneBar.AddControls(new List<Control> { panelButton });
			var xmlDocView = new XmlDocView(XDocument.Parse(LexiconResources.LexiconClassifiedDictionaryParameters).Root, majorFlexComponentParameters.LcmCache, _recordList);
			_paneBarContainer = PaneBarContainerFactory.Create(
				majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				xmlDocView,
				xmlDocViewPaneBar);
			_lexicoClassifiedDictionaryMenuHelper = new LexiconClassifiedDictionaryToolMenuHelper(majorFlexComponentParameters, xmlDocView, _recordList);

			// Too early before now.
			((ISemanticDomainTreeBarHandler)_recordList.MyTreeBarHandler).FinishInitialization(xmlDocViewPaneBar);
			_lexicoClassifiedDictionaryMenuHelper.Initialize();
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
			_recordList.ReloadIfNeeded();
			((DomainDataByFlidDecoratorBase)_recordList.VirtualListPublisher).Refresh();
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		public void EnsurePropertiesAreCurrent()
		{
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => AreaServices.LexiconClassifiedDictionaryMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Classified Dictionary";
		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area for the tool.
		/// </summary>
		public IArea Area => _area;

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.DocumentView.SetBackgroundColor(Color.Magenta);

		#endregion
	}
}
