using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Summary description for ColumnConfigureDialg.
	/// </summary>
	public class ColumnConfigureDialog : Form, IFWDisposable
	{
		private const string s_helpTopic = "khtpConfigureColumns";
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ColumnHeader FieldColumn;
		private System.Windows.Forms.ColumnHeader InfoColumn;
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Button helpButton;
		private System.Windows.Forms.Button addButton;
		private System.Windows.Forms.Button removeButton;
		internal System.Windows.Forms.Button moveUpButton;
		internal System.Windows.Forms.Button moveDownButton;
		private FwOverrideComboBox wsCombo;
		private System.Windows.Forms.Label label3;
		internal System.Windows.Forms.ListView currentList;

		List<XmlNode> m_possibleColumns;
		List<XmlNode> m_currentColumns;
		readonly FdoCache m_cache;
		StringTable m_stringTbl;
		private readonly IHelpTopicProvider m_helpTopicProvider;

		bool m_fUpdatingWsCombo = false; // true during UpdateWsCombo

		/// <summary></summary>
		public enum WsComboContent
		{
			/// <summary></summary>
			kwccNone,
			/// <summary></summary>
			kwccVernAndAnal,
			/// <summary></summary>
			kwccBestVernOrAnal,
			/// <summary></summary>
			kwccAnalAndVern,
			/// <summary></summary>
			kwccBestAnalOrVern,
			/// <summary></summary>
			kwccBestAnalysis,
			/// <summary></summary>
			kwccAnalysis,
			/// <summary></summary>
			kwccVernacular,
			/// <summary></summary>
			kwccPronunciation,
			/// <summary></summary>
			kwccReversalIndexes,
			/// <summary></summary>
			kwccReversalIndex,
			/// <summary></summary>
			kwccBestVernacular,
			/// <summary></summary>
			kwccVernacularInParagraph
		};
		WsComboContent m_wccCurrent = WsComboContent.kwccNone;
		private int m_hvoRootObj = 0;

		private System.Windows.Forms.ListView optionsList;
		private System.Windows.Forms.HelpProvider helpProvider;
		private IContainer components;
		private ColumnHeader columnHeader1;
		private PictureBox blkEditIcon;
		private Label blkEditText;

		private ImageList imageList1;
		private ImageList imageList2;

		private bool showBulkEditIcons = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a column configure dialog. It is passed a list of XmlNodes that
		/// specify the possible columns, and another list, a subset of the first,
		/// of the ones currently displayed.
		/// </summary>
		/// <param name="possibleColumns">The possible columns.</param>
		/// <param name="currentColumns">The current columns.</param>
		/// <param name="mediator">The mediator.</param>
		/// <param name="stringTbl">The string TBL.</param>
		/// ------------------------------------------------------------------------------------
		public ColumnConfigureDialog(List<XmlNode> possibleColumns, List<XmlNode> currentColumns,
			Mediator mediator, StringTable stringTbl)
		{
			m_possibleColumns = possibleColumns;
			m_currentColumns = currentColumns;
			m_cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			m_stringTbl = stringTbl;
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			m_helpTopicProvider = mediator.HelpTopicProvider;
			if (m_helpTopicProvider != null)
			{
				helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
				helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
				helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
				helpProvider.SetShowHelp(this, true);
			}
			InitCurrentList();

			InitChoicesList();

			InitWsCombo(WsComboContent.kwccNone);

			EnableControls();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current specs.
		/// </summary>
		/// <value>The current specs.</value>
		/// ------------------------------------------------------------------------------------
		public List<XmlNode> CurrentSpecs
		{
			get
			{
				CheckDisposed();
				return m_currentColumns;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether [show bulk edit icons].
		/// </summary>
		/// <value><c>true</c> if [show bulk edit icons]; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool ShowBulkEditIcons
		{
			get
			{
				CheckDisposed();
				return showBulkEditIcons;
			}
			set
			{
				CheckDisposed();

				showBulkEditIcons = value;
				blkEditIcon.Visible = blkEditText.Visible = showBulkEditIcons;
				currentList.SmallImageList = imageList1;
				optionsList.SmallImageList = imageList1;
				this.Refresh();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the root object hvo.
		/// </summary>
		/// <value>The root object hvo.</value>
		/// ------------------------------------------------------------------------------------
		public int RootObjectHvo
		{
			get
			{
				CheckDisposed();
				return m_hvoRootObj;
			}
			set
			{
				CheckDisposed();
				m_hvoRootObj = value;
			}
		}

		private void InitWsCombo(WsComboContent contentToDisplay)
		{
			// Default to an empty string, which will prevent anything from being selected
			InitWsCombo(contentToDisplay, "");
		}

		private void InitWsCombo(WsComboContent contentToDisplay, string wsLabel)
		{
			if (m_wccCurrent == contentToDisplay)
			{
				// We may have the correct content up, but we still need to make sure we've selected the correct thing
				SelectWsLabel(wsLabel);
				return;
			}

			wsCombo.Items.Clear();
			AddWritingSystemsToCombo(m_cache, wsCombo.Items, contentToDisplay);
			m_wccCurrent = contentToDisplay;

			SelectWsLabel(wsLabel);
		}

		private void SelectWsLabel(string wsLabel)
		{
			if (wsLabel != "")
			{
				foreach (WsComboItem item in wsCombo.Items)
					if (item.Id == wsLabel)
						wsCombo.SelectedItem = item;
			}
		}

		/// <summary>
		/// Initialize the combo box for the set of reversal index writing systems.
		/// </summary>
		private void InitWsComboForReversalIndexes()
		{
			if (m_wccCurrent == WsComboContent.kwccReversalIndexes)
				return;

			wsCombo.Items.Clear();
			using (var ie = m_cache.LanguageProject.LexDbOA.ReversalIndexesOC.GetEnumerator())
			{
				var rgWs = new IWritingSystem[m_cache.LanguageProject.LexDbOA.ReversalIndexesOC.Count];
				for (int i = 0; i < rgWs.Length; ++i)
				{
					if (!ie.MoveNext())
						throw new Exception("The IEnumerator failed to move to an existing Reversal Index???");
					var ri = (IReversalIndex)ie.Current;
					rgWs[i] = m_cache.ServiceLocator.WritingSystemManager.Get(ri.WritingSystem);
				}
				bool fSort = wsCombo.Sorted;
				wsCombo.Sorted = true;
				AddWritingSystemsToCombo(m_cache, wsCombo.Items, rgWs);
				wsCombo.Sorted = fSort;
				m_wccCurrent = WsComboContent.kwccReversalIndexes;
				wsCombo.Enabled = true;
				string sDefaultRevWsName = GetDefaultReversalWsName();
				int idx = -1;
				for (int i = 0; i < wsCombo.Items.Count; ++i)
				{
					var item = wsCombo.Items[i] as WsComboItem;
					if (item != null)
					{
						if (item.ToString() == sDefaultRevWsName)
						{
							idx = i;
							break;
						}
					}
				}
				Debug.Assert(idx >= 0);
				wsCombo.SelectedIndex = idx >= 0 ? idx : 0;
			}
		}

		private void InitWsComboForReversalIndex()
		{
			if (m_wccCurrent == WsComboContent.kwccReversalIndex)
				return;

			wsCombo.Items.Clear();
			var ri = m_cache.ServiceLocator.GetInstance<IReversalIndexRepository>().GetObject(m_hvoRootObj);
			LanguageSubtag sLang = m_cache.ServiceLocator.WritingSystemManager.Get(ri.WritingSystem).LanguageSubtag;
			bool fSort = wsCombo.Sorted;
			foreach (IWritingSystem ws in WritingSystemServices.GetReversalIndexWritingSystems(m_cache, ri.Hvo, false))
			{
				if (ws.LanguageSubtag == sLang)
				{
					wsCombo.Items.Add(new WsComboItem(ws.DisplayLabel, ws.Id));
				}
			}
			//foreach (NamedWritingSystem nws in m_cache.LangProject.GetDbNamedWritingSystems())
			//{
			//    if (MiscUtils.ExtractLanguageCode(nws.IcuLocale) == sLang)
			//        wsCombo.Items.Add(new WsComboItem(nws.Name, nws.IcuLocale, nws.Hvo));
			//}
			wsCombo.Sorted = fSort;
			m_wccCurrent = WsComboContent.kwccReversalIndex;
			wsCombo.Enabled = true;
			wsCombo.SelectedIndex = 0;
		}

		/// <summary>
		/// Initialize the combo box for the standard set of writing systems.
		/// </summary>
		public static void AddWritingSystemsToCombo(FdoCache cache,
			ComboBox.ObjectCollection items, WsComboContent contentToAdd)
		{
			AddWritingSystemsToCombo(cache, items, contentToAdd, false, false);
		}

		/// <summary>
		/// Initialize the combo box for the standard set of writing systems.
		/// </summary>
		public static void AddWritingSystemsToCombo(FdoCache cache,
			ComboBox.ObjectCollection items, WsComboContent contentToAdd, bool skipDefaults)
		{
			AddWritingSystemsToCombo(cache, items, contentToAdd, skipDefaults, false);
		}

		/// <summary>
		/// Initialize the combo box for the standard set of writing systems.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="items"></param>
		/// <param name="contentToAdd"></param>
		/// <param name="skipDefaults">true if we do NOT want to see the default items, but only
		/// the actual writing systems</param>
		/// <param name="allowMultiple">true to allow values that generate multiple writing
		/// systems, like "all analysis". Used by ConfigureFieldDlg for Dictionary views. Also
		/// adds all reasonable single generic items not already included by skipDefaults.
		/// Ignored if skipDefaults is true.</param>
		/// <remarks>This is static because ConfigureInterlinDialog uses it</remarks>
		public static void AddWritingSystemsToCombo(FdoCache cache,
			ComboBox.ObjectCollection items, WsComboContent contentToAdd, bool skipDefaults,
			bool allowMultiple)
		{
			string sAllAnal = XMLViewsStrings.ksAllAnal;
			string sAllAnalVern = XMLViewsStrings.ksAllAnalVern;
			string sAllPron = XMLViewsStrings.ksAllPron;
			string sAllVern = XMLViewsStrings.ksAllVern;
			string sAllVernAnal = XMLViewsStrings.ksAllVernAnal;
			string sBestAnal = XMLViewsStrings.ksBestAnal;
			string sBestAnalVern = XMLViewsStrings.ksBestAnalVern;
			string sBestVern = XMLViewsStrings.ksBestVern;
			string sBestVernAnal = XMLViewsStrings.ksBestVernAnal;
			string sDefaultAnal = XMLViewsStrings.ksDefaultAnal;
			string sDefaultPron = XMLViewsStrings.ksDefaultPron;
			string sDefaultVern = XMLViewsStrings.ksDefaultVern;
			string sVernacularInPara = XMLViewsStrings.ksVernacularInParagraph;
			switch (contentToAdd)
			{
				case WsComboContent.kwccNone:
					break;
				case WsComboContent.kwccVernAndAnal:
					if (!skipDefaults)
					{
						items.Add(new WsComboItem(sDefaultVern, "vernacular"));
						items.Add(new WsComboItem(sDefaultAnal, "analysis"));
						if (allowMultiple)
						{
							items.Add(new WsComboItem(sAllVern, "all vernacular"));
							items.Add(new WsComboItem(sAllAnal, "all analysis"));
							items.Add(new WsComboItem(sAllVernAnal, "vernacular analysis"));
							items.Add(new WsComboItem(sAllAnalVern, "analysis vernacular"));
							items.Add(new WsComboItem(sBestVernAnal, "best vernoranal"));
							items.Add(new WsComboItem(sBestAnalVern, "best analorvern"));
						}
					}
					AddWritingSystemsToCombo(cache, items,
						cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems);
					AddWritingSystemsToCombo(cache, items,
						cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems);
					break;
				case WsComboContent.kwccAnalAndVern:
					if (!skipDefaults)
					{
						items.Add(new WsComboItem(sDefaultAnal, "analysis"));
						items.Add(new WsComboItem(sDefaultVern, "vernacular"));
						if (allowMultiple)
						{
							items.Add(new WsComboItem(sAllAnal, "all analysis"));
							items.Add(new WsComboItem(sAllVern, "all vernacular"));
							items.Add(new WsComboItem(sAllAnalVern, "analysis vernacular"));
							items.Add(new WsComboItem(sAllVernAnal, "vernacular analysis"));
							items.Add(new WsComboItem(sBestAnalVern, "best analorvern"));
							items.Add(new WsComboItem(sBestVernAnal, "best vernoranal"));
						}
					}
					AddWritingSystemsToCombo(cache, items,
						cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems);
					AddWritingSystemsToCombo(cache, items,
						cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems);
					break;
				case WsComboContent.kwccBestAnalOrVern:
					if (!skipDefaults)
					{
						items.Add(new WsComboItem(sBestAnalVern, "best analorvern"));
						if (allowMultiple)
						{
							items.Add(new WsComboItem(sDefaultAnal, "analysis"));
							items.Add(new WsComboItem(sAllAnal, "all analysis"));
							items.Add(new WsComboItem(sAllAnalVern, "analysis vernacular"));
							items.Add(new WsComboItem(sDefaultVern, "vernacular"));
							items.Add(new WsComboItem(sAllVern, "all vernacular"));
							items.Add(new WsComboItem(sAllVernAnal, "vernacular analysis"));
							items.Add(new WsComboItem(sBestVernAnal, "best vernoranal"));
						}
					}

					AddWritingSystemsToCombo(cache, items,
						cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems);
					AddWritingSystemsToCombo(cache, items,
						cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems);
					break;
				case WsComboContent.kwccBestAnalysis:
					if (!skipDefaults)
					{
						items.Add(new WsComboItem(sBestAnal, "best analysis"));
						if (allowMultiple)
						{
							items.Add(new WsComboItem(sDefaultAnal, "analysis"));
							items.Add(new WsComboItem(sAllAnal, "all analysis"));
						}
					}

					AddWritingSystemsToCombo(cache, items,
						cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems);
					break;
				case WsComboContent.kwccBestVernacular:
					if (!skipDefaults)
					{
						items.Add(new WsComboItem(sBestVern, "best vernacular"));
						if (allowMultiple)
						{
							items.Add(new WsComboItem(sDefaultVern, "vernacular"));
							items.Add(new WsComboItem(sAllVern, "all vernacular"));
						}
					}
					AddWritingSystemsToCombo(cache, items,
						cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems);
					break;
				case WsComboContent.kwccBestVernOrAnal:
					if (!skipDefaults)
					{
						items.Add(new WsComboItem(sBestVernAnal, "best vernoranal"));
						if (allowMultiple)
						{
							items.Add(new WsComboItem(sDefaultVern, "vernacular"));
							items.Add(new WsComboItem(sAllVern, "all vernacular"));
							items.Add(new WsComboItem(sAllVernAnal, "vernacular analysis"));
							items.Add(new WsComboItem(sDefaultAnal, "analysis"));
							items.Add(new WsComboItem(sAllAnal, "all analysis"));
							items.Add(new WsComboItem(sAllAnalVern, "analysis vernacular"));
							items.Add(new WsComboItem(sBestAnalVern, "best analorvern"));
						}
					}

					AddWritingSystemsToCombo(cache, items,
						cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems);
					AddWritingSystemsToCombo(cache, items,
						cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems);
					break;
				case WsComboContent.kwccAnalysis:
					if (!skipDefaults)
					{
						items.Add(new WsComboItem(sDefaultAnal, "analysis"));
						if (allowMultiple)
						{
							items.Add(new WsComboItem(sBestAnal, "best analysis"));
							items.Add(new WsComboItem(sAllAnal, "all analysis"));
						}
					}

					AddWritingSystemsToCombo(cache, items,
						cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems);
					break;
				case WsComboContent.kwccVernacularInParagraph:
					items.Add(new WsComboItem(sVernacularInPara, "vern in para"));
					AddWritingSystemsToCombo(cache, items,
						cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems);
					break;
				case WsComboContent.kwccVernacular:
					if (!skipDefaults)
					{
						items.Add(new WsComboItem(sDefaultVern, "vernacular"));
						if (allowMultiple)
						{
							items.Add(new WsComboItem(sBestVern, "best vernacular"));
							items.Add(new WsComboItem(sAllVern, "all vernacular"));
						}
					}

					AddWritingSystemsToCombo(cache, items,
						cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems);
					break;
				case WsComboContent.kwccPronunciation:
					if (!skipDefaults)
					{
						items.Add(new WsComboItem(sDefaultPron, "pronunciation"));
						if (allowMultiple)
						{
							items.Add(new WsComboItem(sAllPron, "all pronunciation"));
						}
					}

					AddWritingSystemsToCombo(cache, items,
						cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems);
					break;
				default:
					throw new NotImplementedException(
						"AddWritingSystemsToCombo does not know how to add " +
						contentToAdd + " content.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the writing systems to combo.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="items">The items.</param>
		/// <param name="wss">The ws array.</param>
		/// ------------------------------------------------------------------------------------
		public static void AddWritingSystemsToCombo(FdoCache cache, ComboBox.ObjectCollection items, IEnumerable<IWritingSystem> wss)
		{
			foreach(IWritingSystem ws in wss)
				items.Add(new WsComboItem(ws.DisplayLabel, ws.Id));
		}

		void InitChoicesList()
		{
			// LT-12253 It's just possible that AddCurrentItem() will delete a column
			// (e.g. if the user previously deleted a ws that it references).
			// So don't use foreach here!
			for (var i = 0; i < m_currentColumns.Count; i++)
			{
				var node = m_currentColumns[i];
				var item = AddCurrentItem(node);
				if (item == null)
					i--;
			}
		}

		/// <summary>
		/// Creates the ListViewItem for the current Xml node.
		/// </summary>
		/// <param name="node"></param>
		/// <returns>The ListViewItem or null. If null is returned, the caller should delete this
		/// column from the current list.</returns>
		ListViewItem MakeCurrentItem(XmlNode node)
		{
			var cols = new string[2];
			var label = XmlUtils.GetLocalizedAttributeValue(m_stringTbl, node, "label", null);
			if (label == null)
				label = XmlUtils.GetManditoryAttributeValue(node, "label");
			cols[0] = label;
			var wsParam = XmlViewsUtils.FindWsParam(node);
			var dispCategory = TranslateWsParamToLocalizedDisplayCategory(wsParam);
			cols[1] = dispCategory;
			// Failure to translate (Empty string result) means either:
			//    1) wsParam is a specific Writing System... look up how to display it.
			// or 2) the user deleted the Writing System... try to revert to a default ws
			//       unless there is a column for that already, in which case return null
			//       so we can delete this column.
			if (String.IsNullOrEmpty(dispCategory) && !String.IsNullOrEmpty(wsParam))
			{
				// Display the language name, not its ICU locale.
				IWritingSystem ws;
				if (m_cache.ServiceLocator.WritingSystemManager.TryGet(wsParam, out ws))
					cols[1] = ws.DisplayLabel;
				else
				{
					// Probably this ws was deleted. See LT-12253.
					string newWsDispCat;
					string newColName;
					if (!TryToRevertToDefaultWs(node, out newColName, out newWsDispCat))
					{
						// Caller should delete this node from the current list of columns.
						return null;
					}
					cols[0] = newColName;
					cols[1] = newWsDispCat;
				}
			}

			var itemWithToolTip = new ListViewItem(cols);
			itemWithToolTip.ToolTipText = cols[1];
			if (XmlUtils.GetOptionalAttributeValue(node, "bulkEdit") != null ||
				XmlUtils.GetOptionalAttributeValue(node, "transduce") != null)
			{
				itemWithToolTip .ImageIndex = 0;
			}

			return itemWithToolTip ;
		}

		private string TranslateWsParamToLocalizedDisplayCategory(string wsParam)
		{
			var result = String.Empty; // if the switch doesn't match wsParam, this will be returned.
			switch (wsParam)
			{
				case "analysis":
					result = XMLViewsStrings.ksDefaultAnal;
					break;
				case "vernacular":
					result = XMLViewsStrings.ksDefaultVern;
					break;
				case "pronunciation":
					result = XMLViewsStrings.ksDefaultPron;
					break;
				case "best vernoranal":
					result = XMLViewsStrings.ksBestVernAnal;
					break;
				case "best analorvern":
					result = XMLViewsStrings.ksBestAnalVern;
					break;
				case "best analysis":
					result = XMLViewsStrings.ksBestAnal;
					break;
				case "best vernacular":
					result = XMLViewsStrings.ksBestVern;
					break;
				case "reversal":
					{
						// Get the language for this reversal index.
						var sWsName = GetDefaultReversalWsName();
						if (!String.IsNullOrEmpty(sWsName))
							result = sWsName;
					}
					break;
				case "reversal index": // ??? is this case used?
					break;
				case "analysis vernacular":
					result = XMLViewsStrings.ksDefaultAnal;
					break;
				case "vernacular analysis":
					result = XMLViewsStrings.ksDefaultVern;
					break;
			}
			return result;
		}

		private bool TryToRevertToDefaultWs(XmlNode node, out string newColName, out string newWsDispCat)
		{
			newColName = String.Empty;
			newWsDispCat = String.Empty;
			var origWs = XmlUtils.GetOptionalAttributeValue(node, "originalWs");
			if (origWs == null)
				return false;

			var origDisplayCategory = TranslateWsParamToLocalizedDisplayCategory(origWs);
			var origLabel = XmlUtils.GetOptionalAttributeValue(node, "originalLabel");
			if (string.IsNullOrEmpty(origLabel))
				return false; // trash this bizarre column!
			if (CurrentColumnsContainsOriginalDefault(origLabel, origWs))
				return false;

			var dispName = UpdateNodeToReflectDefaultWs(node, origWs);
			if (!String.IsNullOrEmpty(dispName))
				newColName = dispName;
			newWsDispCat = origDisplayCategory;
			return true;
		}

		private string UpdateNodeToReflectDefaultWs(XmlNode node, string origWs)
		{
			var result = String.Empty;
			if (node.Attributes != null)
			{
				const string wsAttrName = "ws";
				if (XmlUtils.GetOptionalAttributeValue(node, wsAttrName)!= null)
				{
					XmlUtils.SetAttribute(node, wsAttrName, StringServices.WsParamLabel+origWs);
					// reset 'label' attribute to 'originalLabel'
					const string origLabelAttrName = "originalLabel";
					var origLabel = XmlUtils.GetOptionalAttributeValue(node, origLabelAttrName);
					if (origLabel != null)
					{
						result = origLabel;
						const string origWsAttrName = "originalWs";
						XmlUtils.SetAttribute(node, "label", origLabel);
						// remove 'originalLabel' and 'originalWs' attributes
						node.Attributes.RemoveNamedItem(origLabelAttrName);
						node.Attributes.RemoveNamedItem(origWsAttrName);
					}
				}
			}
			return result;
		}

		private bool CurrentColumnsContainsOriginalDefault(string label, string origWs)
		{
			// Search through m_currentColumns for one that has the same label attribute
			// and original writing system.
			foreach (var col in m_currentColumns)
			{
				var colLabel = XmlUtils.GetOptionalAttributeValue(col, "label");
				if (label != colLabel)
					continue;
				var wsParam = XmlViewsUtils.FindWsParam(col);
				if (wsParam == origWs)
					return true;
			}
			return false;
		}

		private string GetDefaultReversalWsName()
		{
			IReversalIndex ri = null;
			var rgriCurrent = m_cache.LangProject.LexDbOA.CurrentReversalIndices;
			if (rgriCurrent.Count > 0)
			{
				ri = rgriCurrent[0];
			}
			else if (m_cache.LangProject.LexDbOA.ReversalIndexesOC.Count > 0)
			{
				ri = m_cache.LangProject.LexDbOA.ReversalIndexesOC.ToArray()[0];
			}
			if (ri != null)
			{
				IWritingSystem ws = m_cache.ServiceLocator.WritingSystemManager.Get(ri.WritingSystem);
				return ws.DisplayLabel;
			}
			return null;
		}

		ListViewItem AddCurrentItem(XmlNode node)
		{
			var item = MakeCurrentItem(node);
			// Should only occur if user deleted this ws
			if (item == null)
			{
				if (m_currentColumns.Contains(node))
					m_currentColumns.Remove(node);
			}
			else
				currentList.Items.Add(item);

			return item;
		}

		void InitCurrentList()
		{
			IComparer<XmlNode> columnSorter = new ColumnSorter(m_stringTbl);
			m_possibleColumns.Sort(columnSorter); // Sort the list before it's displayed
			SafelyMakeOptionsList(optionsList);
		}

		private void SafelyMakeOptionsList(ListView optionsList)
		{
			foreach (var node in m_possibleColumns)
			{
				var listItem = MakeCurrentItem(node);
				if (listItem != null)
					optionsList.Items.Add(listItem);
			}
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing )
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ColumnConfigureDialog));
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.currentList = new System.Windows.Forms.ListView();
			this.FieldColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.InfoColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.okButton = new System.Windows.Forms.Button();
			this.cancelButton = new System.Windows.Forms.Button();
			this.helpButton = new System.Windows.Forms.Button();
			this.addButton = new System.Windows.Forms.Button();
			this.removeButton = new System.Windows.Forms.Button();
			this.moveUpButton = new System.Windows.Forms.Button();
			this.imageList2 = new System.Windows.Forms.ImageList(this.components);
			this.moveDownButton = new System.Windows.Forms.Button();
			this.wsCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.label3 = new System.Windows.Forms.Label();
			this.optionsList = new System.Windows.Forms.ListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.blkEditIcon = new System.Windows.Forms.PictureBox();
			this.blkEditText = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.blkEditIcon)).BeginInit();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			this.helpProvider.SetShowHelp(this.label1, ((bool)(resources.GetObject("label1.ShowHelp"))));
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			this.helpProvider.SetShowHelp(this.label2, ((bool)(resources.GetObject("label2.ShowHelp"))));
			//
			// currentList
			//
			resources.ApplyResources(this.currentList, "currentList");
			this.currentList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.FieldColumn,
			this.InfoColumn});
			this.currentList.FullRowSelect = true;
			this.currentList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.currentList.HideSelection = false;
			this.currentList.MultiSelect = false;
			this.currentList.Name = "currentList";
			this.currentList.ShowItemToolTips = true;
			this.currentList.UseCompatibleStateImageBehavior = false;
			this.currentList.View = System.Windows.Forms.View.Details;
			this.currentList.SelectedIndexChanged += new System.EventHandler(this.currentList_SelectedIndexChanged);
			this.currentList.DoubleClick += new System.EventHandler(this.removeButton_Click);
			//
			// FieldColumn
			//
			resources.ApplyResources(this.FieldColumn, "FieldColumn");
			//
			// InfoColumn
			//
			resources.ApplyResources(this.InfoColumn, "InfoColumn");
			//
			// okButton
			//
			resources.ApplyResources(this.okButton, "okButton");
			this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.okButton.Name = "okButton";
			//
			// cancelButton
			//
			resources.ApplyResources(this.cancelButton, "cancelButton");
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Name = "cancelButton";
			//
			// helpButton
			//
			resources.ApplyResources(this.helpButton, "helpButton");
			this.helpButton.Name = "helpButton";
			this.helpButton.Click += new System.EventHandler(this.helpButton_Click);
			//
			// addButton
			//
			resources.ApplyResources(this.addButton, "addButton");
			this.addButton.Name = "addButton";
			this.addButton.Click += new System.EventHandler(this.addButton_Click);
			//
			// removeButton
			//
			resources.ApplyResources(this.removeButton, "removeButton");
			this.removeButton.Name = "removeButton";
			this.removeButton.Click += new System.EventHandler(this.removeButton_Click);
			//
			// moveUpButton
			//
			resources.ApplyResources(this.moveUpButton, "moveUpButton");
			this.moveUpButton.ImageList = this.imageList2;
			this.moveUpButton.Name = "moveUpButton";
			this.moveUpButton.Click += new System.EventHandler(this.moveUpButton_Click);
			//
			// imageList2
			//
			this.imageList2.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList2.ImageStream")));
			this.imageList2.TransparentColor = System.Drawing.Color.Magenta;
			this.imageList2.Images.SetKeyName(0, "LargeUpArrow.bmp");
			this.imageList2.Images.SetKeyName(1, "LargeDownArrow.bmp");
			//
			// moveDownButton
			//
			resources.ApplyResources(this.moveDownButton, "moveDownButton");
			this.moveDownButton.ImageList = this.imageList2;
			this.moveDownButton.Name = "moveDownButton";
			this.moveDownButton.Click += new System.EventHandler(this.moveDownButton_Click);
			//
			// wsCombo
			//
			this.wsCombo.AllowSpaceInEditBox = false;
			resources.ApplyResources(this.wsCombo, "wsCombo");
			this.wsCombo.Name = "wsCombo";
			this.wsCombo.SelectedIndexChanged += new System.EventHandler(this.wsCombo_SelectedIndexChanged);
			//
			// label3
			//
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			//
			// optionsList
			//
			resources.ApplyResources(this.optionsList, "optionsList");
			this.optionsList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.columnHeader1});
			this.optionsList.FullRowSelect = true;
			this.optionsList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.optionsList.HideSelection = false;
			this.optionsList.MultiSelect = false;
			this.optionsList.Name = "optionsList";
			this.optionsList.UseCompatibleStateImageBehavior = false;
			this.optionsList.View = System.Windows.Forms.View.Details;
			this.optionsList.SelectedIndexChanged += new System.EventHandler(this.optionsList_SelectedIndexChanged_1);
			this.optionsList.DoubleClick += new System.EventHandler(this.addButton_Click);
			//
			// columnHeader1
			//
			resources.ApplyResources(this.columnHeader1, "columnHeader1");
			//
			// imageList1
			//
			this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.Magenta;
			this.imageList1.Images.SetKeyName(0, "");
			//
			// blkEditIcon
			//
			resources.ApplyResources(this.blkEditIcon, "blkEditIcon");
			this.blkEditIcon.Name = "blkEditIcon";
			this.blkEditIcon.TabStop = false;
			//
			// blkEditText
			//
			resources.ApplyResources(this.blkEditText, "blkEditText");
			this.blkEditText.Name = "blkEditText";
			//
			// ColumnConfigureDialog
			//
			this.AcceptButton = this.okButton;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.cancelButton;
			this.Controls.Add(this.blkEditText);
			this.Controls.Add(this.blkEditIcon);
			this.Controls.Add(this.optionsList);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.wsCombo);
			this.Controls.Add(this.moveDownButton);
			this.Controls.Add(this.moveUpButton);
			this.Controls.Add(this.removeButton);
			this.Controls.Add(this.addButton);
			this.Controls.Add(this.helpButton);
			this.Controls.Add(this.cancelButton);
			this.Controls.Add(this.okButton);
			this.Controls.Add(this.currentList);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.helpProvider.SetHelpNavigator(this, ((System.Windows.Forms.HelpNavigator)(resources.GetObject("$this.HelpNavigator"))));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ColumnConfigureDialog";
			this.helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.ShowInTaskbar = false;
			this.TransparencyKey = System.Drawing.Color.Fuchsia;
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ColumnConfigureDialog_FormClosing);
			((System.ComponentModel.ISupportInitialize)(this.blkEditIcon)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		void ColumnConfigureDialog_FormClosing(object sender, FormClosingEventArgs e)
		{
			// We only need to validate the choices if the user clicked OK
			if (this.DialogResult != DialogResult.OK)
				return;

			if (HasDuplicateColumns())
			{
				ShowDuplicatesWarning(GetDuplicateColumns());
				e.Cancel = true;
				return;
			}
		}

		private void ShowDuplicatesWarning(List<string> duplicateColumnLabels)
		{
			string duplicates = string.Join(", ", duplicateColumnLabels.ToArray());
			MessageBox.Show(String.Format(XMLViewsStrings.ksDuplicateColumnMsg, duplicates),
							XMLViewsStrings.ksDuplicateColumn,
							MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}

		private bool HasDuplicateColumns()
		{
			return (GetDuplicateColumns().Count > 0);
		}

		private List<string> GetDuplicateColumns()
		{
			// Make sure the ws and label of the current column are accurate (necessary when this is called in addButton_Click()).
			UpdateWsAndLabelOfCurrentColumn();

			var duplicateColumnLabels = new List<string>();

			for (int i = 0; i < CurrentSpecs.Count; i++)
			{
				string label = GetColumnLabel(i);
				string wsParam = XmlViewsUtils.FindWsParam(CurrentSpecs[i]);

				// This tries to interpret the ws paramter into an int.  Sometimes the parameter cannot be interpreted without an object,
				// such as when the ws is a magic string that will change the actual ws depending on the contents of the object.
				// In these cases, we give -50 as a known constant to check for and will just compare the string version of the
				// ws paramter.  This can can possibly throw an exception, so we'll enclose it in a try block.
				int ws = -50;
				int wsMagic = 0;
				try
				{
					if (!XmlViewsUtils.GetWsRequiresObject(wsParam))
						ws = WritingSystemServices.InterpretWsLabel(m_cache, wsParam, null, 0, 0, null, out wsMagic);
				}
				catch { }

				for (int j = 0; j < CurrentSpecs.Count; j++)
				{
					// No need to check against our own node
					if (j == i)
						continue;

					bool sameSpec = false;

					string otherLabel = GetColumnLabel(j);
					if (label == otherLabel)
					{
						string otherWsParam = XmlViewsUtils.FindWsParam(CurrentSpecs[j] as XmlNode);

						// If the ws is not -50, then we know to compare against integer ws codes, not string labels
						if (ws != -50)
						{
							int wsOtherMagic = 0;
							int wsOther = WritingSystemServices.InterpretWsLabel(m_cache, otherWsParam, null, 0, 0, null, out wsOtherMagic);
							if (ws == wsOther && wsMagic == wsOtherMagic)
								sameSpec = true;
						}
						else
						{
							if (wsParam == otherWsParam)
								sameSpec = true;
						}

						if (sameSpec) // Found a duplicate column.
						{
							if (!duplicateColumnLabels.Contains(label)) // Don't add the same label twice!
							{
								duplicateColumnLabels.Add(label);
							}
						}
					}
				}
			}
			return duplicateColumnLabels;
		}

		private string GetColumnLabel(int columnIndex)
		{
			string label = XmlUtils.GetLocalizedAttributeValue(m_stringTbl, CurrentSpecs[columnIndex],
															   "originalLabel", null);
			if (label == null)
				label = XmlUtils.GetLocalizedAttributeValue(m_stringTbl, CurrentSpecs[columnIndex],
															"label", null);
			if (label == null)
				label = XmlUtils.GetManditoryAttributeValue(CurrentSpecs[columnIndex], "label");
			return label;
		}

		private void addButton_Click(object sender, System.EventArgs e)
		{
			XmlNode item = m_possibleColumns[optionsList.SelectedIndices[0]];
			m_currentColumns.Add(item);
			int index = CurrentListIndex;
			if (index >= 0)
				currentList.Items[index].Selected = false;
			AddCurrentItem(item).Selected = true;

			while ((HasDuplicateColumns() && DuplicateIsReleventForItem(item, GetDuplicateColumns()))
				&& (wsCombo.SelectedIndex < wsCombo.Items.Count))
			{
				if (wsCombo.SelectedIndex.Equals(wsCombo.Items.Count - 1))
				{
					wsCombo.SelectedIndex = 0;
					UpdateWsAndLabelOfCurrentColumn();
					break;
				}
				wsCombo.SelectedIndex++;
			}

			if (HasDuplicateColumns())
			{
				List<string> duplicates = GetDuplicateColumns();
				if (DuplicateIsReleventForItem(item, duplicates))
				{
					ShowDuplicatesWarning(duplicates);
				}
			}

			currentList.Focus();
		}

		private bool DuplicateIsReleventForItem(XmlNode item, List<string> duplicateColumnLabels)
		{
			return duplicateColumnLabels.Contains(item.Attributes.GetNamedItem("label").Value);
		}

		private void removeButton_Click(object sender, System.EventArgs e)
		{
			int index = CurrentListIndex;
			if (index < 0 || currentList.Items.Count == 1)
				return;
			currentList.Items.RemoveAt(index);
			m_currentColumns.RemoveAt(index);

			// Select the next logical item
			if (index < currentList.Items.Count)
				currentList.Items[index].Selected = true;
			else
				currentList.Items[currentList.Items.Count - 1].Selected = true;
			currentList.Select();
		}

		private void moveUpButton_Click(object sender, System.EventArgs e)
		{
			int index = CurrentListIndex;
			if (index <= 0)
				return; // should be disabled, but play safe.
			XmlNode itemMove = m_currentColumns[index];
			m_currentColumns[index] = m_currentColumns[index - 1];
			m_currentColumns[index - 1] = itemMove;
			ListViewItem listItemMove = currentList.Items[index];
			currentList.Items.RemoveAt(index);
			currentList.Items.Insert(index - 1, listItemMove);
			listItemMove.Selected = true;
		}

		private void moveDownButton_Click(object sender, System.EventArgs e)
		{
			int index = CurrentListIndex;
			if (index < 0 || index >= m_currentColumns.Count - 1)
				return; // should be disabled, but play safe.
			XmlNode itemMove = m_currentColumns[index];
			m_currentColumns[index] = m_currentColumns[index + 1];
			m_currentColumns[index + 1] = itemMove;
			ListViewItem listItemMove = currentList.Items[index];
			currentList.Items.RemoveAt(index);
			currentList.Items.Insert(index + 1, listItemMove);
			listItemMove.Selected = true;
		}

		private void currentList_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			EnableControls();
		}

		void EnableControls()
		{
			addButton.Enabled =	optionsList.SelectedIndices.Count > 0 && optionsList.SelectedIndices[0] >= 0;
			int index = CurrentListIndex;
			removeButton.Enabled = index >= 0 && currentList.Items.Count > 1;
			moveUpButton.Enabled = index > 0;
			moveDownButton.Enabled = index >= 0 && index < currentList.Items.Count - 1;
			if (index >= 0)
			{
				// The ordering of these next two statements is critical.  We need to enable the combobox by default because we have
				// a valid selection on the current list.  However, if UpdateWsComboValue finds out that the ws for that particular
				// field is not configurable, it will disable it.
				wsCombo.Enabled = true;
				UpdateWsComboValue();
			}
			else
			{
				wsCombo.Enabled = false;
			}
		}

		int CurrentListIndex
		{
			get
			{
				if (currentList.SelectedIndices.Count == 0)
					return -1;
				return currentList.SelectedIndices[0];
			}
		}

		void UpdateWsComboValue()
		{
			try
			{
				m_fUpdatingWsCombo = true;
				int index = CurrentListIndex;
				if (index < 0 || index >= m_currentColumns.Count)
					return;
				XmlNode node = m_currentColumns[index];
				string wsLabel = XmlViewsUtils.FindWsParam(node);
				if (wsLabel == "")
				{
					wsCombo.SelectedIndex = -1;
					wsCombo.Enabled = false;
					wsLabel = XmlUtils.GetOptionalAttributeValue(node, "ws");
				}

				if (!String.IsNullOrEmpty(wsLabel))
				{
					string wsForOptions = XmlUtils.GetOptionalAttributeValue(node, "originalWs", wsLabel);
					if (wsForOptions == "reversal")
					{
						Debug.Assert(m_hvoRootObj != 0);
						int clid = (int)m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(m_hvoRootObj).ClassID;
						switch (clid)
						{
							case ReversalIndexTags.kClassId:
								InitWsComboForReversalIndex();
								break;
							default:
								InitWsComboForReversalIndexes();
								break;
						}
					}
					else if (wsForOptions == "vern in para")
					{
						InitWsCombo(WsComboContent.kwccVernacularInParagraph, wsLabel);
					}
					else if (wsForOptions == "analysis")
					{
						InitWsCombo(WsComboContent.kwccAnalysis, wsLabel);
					}
					else if (wsForOptions == "vernacular")
					{
						InitWsCombo(WsComboContent.kwccVernacular, wsLabel);
					}
					else if (wsForOptions == "pronunciation")
					{
						InitWsCombo(WsComboContent.kwccPronunciation, wsLabel);
					}
					else if (wsForOptions == "best vernoranal")
					{
						InitWsCombo(WsComboContent.kwccBestVernOrAnal, wsLabel);
					}
					else if (wsForOptions == "best analorvern")
					{
						InitWsCombo(WsComboContent.kwccBestAnalOrVern, wsLabel);
					}
					else if (wsForOptions == "best analysis")
					{
						InitWsCombo(WsComboContent.kwccBestAnalysis, wsLabel);
					}
					else if (wsForOptions == "best vernacular")
					{
						InitWsCombo(WsComboContent.kwccBestVernacular, wsLabel);
					}
					// The next two are needed to fix LT-6647.
					else if (wsForOptions == "analysis vernacular")
					{
						InitWsCombo(WsComboContent.kwccAnalAndVern, wsLabel);
					}
					else if (wsForOptions == "vernacular analysis")
					{
						InitWsCombo(WsComboContent.kwccVernAndAnal, wsLabel);
					}
					else
					{
						// There something going on that we don't know how to handle.
						// As a last ditch option, we show all vernacular and analysis systems.
						Debug.Assert(false, "A writing system was specified in the column spec that this method does not understand.");
						InitWsCombo(WsComboContent.kwccVernAndAnal, wsLabel);
					}
				}
			}
			finally
			{
				m_fUpdatingWsCombo = false;
			}
		}


		/// <summary>
		/// given the magic writing system, we'll choose the appropriate ComboContent.
		/// if the given writing system is not magic, we'll use defaultMagicName provided.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="wsForOptions"></param>
		/// <param name="defaultMagicName"></param>
		/// <returns></returns>
		public static WsComboContent ChooseComboContent(FdoCache cache, int wsForOptions, string defaultMagicName)
		{
			string magicName = "";
			if (wsForOptions < 0)
				magicName = WritingSystemServices.GetMagicWsNameFromId(wsForOptions);
			if (magicName == "")
				magicName = defaultMagicName;
			return ChooseComboContent(magicName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Chooses the content of the combo.
		/// </summary>
		/// <param name="wsForOptions">The ws for options.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static WsComboContent ChooseComboContent(string wsForOptions)
		{
			switch (wsForOptions)
			{
				case "analysis": return WsComboContent.kwccAnalysis;
				case "vernacular": return WsComboContent.kwccVernacular;
				case "pronunciation": return WsComboContent.kwccPronunciation;
				case "best vernoranal": return WsComboContent.kwccBestVernOrAnal;
				case "best analorvern": return WsComboContent.kwccBestAnalOrVern;
				case "best analysis": return WsComboContent.kwccBestAnalysis;
				case "best vernacular": return WsComboContent.kwccBestVernacular;
				case "all analysis": return WsComboContent.kwccAnalysis;
				case "all vernacular": return WsComboContent.kwccVernacular;
				// The next two are needed to fix LT-6647.
				case "analysis vernacular": return WsComboContent.kwccAnalAndVern;
				case "vernacular analysis": return WsComboContent.kwccVernAndAnal;
				case "vern in para": return WsComboContent.kwccVernacularInParagraph;
				default:
					// There something going on that we don't know how to handle.
					// As a last ditch option, we show all vernacular and analysis systems.
					Debug.Assert(false, "A writing system was specified in the column spec that this method does not understand.");
					return WsComboContent.kwccVernAndAnal;
			}
		}

		private void optionsList_SelectedIndexChanged_1(object sender, EventArgs e)
		{
			EnableControls();
		}

		private void wsCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_fUpdatingWsCombo)
				return;
			UpdateWsAndLabelOfCurrentColumn();
		}

		private void UpdateWsAndLabelOfCurrentColumn()
		{
			if (!(wsCombo.SelectedItem is WsComboItem))
				return;
			int index = CurrentListIndex;
			if (index < 0)
				return;
			string wsId = ((WsComboItem) wsCombo.SelectedItem).Id;
			XmlNode current = m_currentColumns[index];
			string sWsOrig = XmlViewsUtils.FindWsParam(current);
			if (String.IsNullOrEmpty(sWsOrig))
				sWsOrig = XmlUtils.GetOptionalAttributeValue(current, "ws");
			XmlNode replacement = XmlViewsUtils.CopyReplacingParamDefault(current, "ws", wsId);
			string originalWs = XmlUtils.GetOptionalAttributeValue(replacement, "originalWs");
			if (originalWs == null)
			{
				// We store in the XML (which will be persisted as the spec of the column)
				// the original writing system code. This allows us to more easily
				// generate a label if it is changed again: we know both the original label
				// (to possibly append an abbreviation to) and the original writing system (so
				// we know whether to mark it at all).
				if (!String.IsNullOrEmpty(sWsOrig))
					XmlUtils.AppendAttribute(replacement, "originalWs", sWsOrig);
				else
					XmlUtils.AppendAttribute(replacement, "originalWs", currentList.Items[index].SubItems[1].Text);
			}

			GenerateColumnLabel(replacement, m_cache, m_stringTbl);

			XmlAttribute xa = replacement.Attributes["label"];
			xa.Value = XmlUtils.GetManditoryAttributeValue(replacement, "label");
			var listItem = MakeCurrentItem(replacement);
			if (listItem == null) // The user deleted this ws and there was already one with the default ws.
			{
				Debug.Assert(false, "Did the user delete the ws?!");
				currentList.Items.RemoveAt(index);
				return;
			}
			m_currentColumns[index] = replacement;
			currentList.Items.RemoveAt(index);
			currentList.Items.Insert(index, listItem);
			currentList.Items[index].Selected = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Generates a column label given an XML node of the column spec.  The purpose of this
		/// method is to append an abbreviation of the writing system of the column to the end
		/// of the normal label if the writing system is different from the normal one.  This
		/// method assumes that both the originalWs and the ws attributes have been set on the
		/// column already.
		/// </summary>
		/// <param name="colSpec">The XML node of the column specification</param>
		/// <param name="cache">The FdoCache</param>
		/// <param name="stringTbl">The string TBL.</param>
		/// ------------------------------------------------------------------------------------
		static public void GenerateColumnLabel(XmlNode colSpec, FdoCache cache, StringTable stringTbl)
		{
			string newWs = XmlViewsUtils.FindWsParam(colSpec);
			string originalWs = XmlUtils.GetOptionalAttributeValue(colSpec, "originalWs");

			string originalLabel = XmlUtils.GetOptionalAttributeValue(colSpec, "originalLabel");
			if (originalLabel == null)
			{
				// We store in the XML (which will be persisted as the spec of the column)
				// the original label. This allows us to more easily
				// generate a label if it is changed again: we know both the original label
				// (to possibly append an abbreviation to) and the original writing system (so
				// we know whether to mark it at all).
				originalLabel = XmlUtils.GetManditoryAttributeValue(colSpec, "label");
				XmlUtils.AppendAttribute(colSpec, "originalLabel", originalLabel);
			}

			string label = originalLabel;
			if (!String.IsNullOrEmpty(label) && stringTbl != null)
				label = stringTbl.LocalizeAttributeValue(label);

			// Note that there's no reason to try and make a new label if originalWs isn't defined.  If this is the
			// case, then it means that the ws was never changed, so we don't need to put the new ws in the label
			if (!string.IsNullOrEmpty(originalWs) && (newWs != originalWs))
			{
				string extra = "";
				if (newWs == "vernacular")
				{
					extra = "ver";
				}
				else if (newWs == "analysis")
				{
					extra = "an";
				}
				else
				{
					// Try to use the abbreviation of the language name, not its ICU locale
					// name.
					IWritingSystem ws = cache.ServiceLocator.WritingSystemManager.Get(newWs);
					extra = ws.Abbreviation;
					if (string.IsNullOrEmpty(extra))
						extra = newWs;	// but if all else fails...
				}
				label += " (" + extra + ")";
			}

			XmlUtils.AppendAttribute(colSpec, "label", label);
		}

		private void helpButton_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}

		// Class to sort the columns before they are displayed
		private class ColumnSorter : IComparer<XmlNode>
		{
			private StringTable m_stringTable;

			internal ColumnSorter(StringTable tbl)
			{
				m_stringTable = tbl;
			}

			#region IComparer<T> Members

			public int Compare(XmlNode x, XmlNode y)
			{
				string xVal = XmlUtils.GetLocalizedAttributeValue(m_stringTable, (XmlNode)x, "label", null);
				if (xVal == null)
					xVal = XmlUtils.GetManditoryAttributeValue((XmlNode)x, "label");
				string yVal = XmlUtils.GetLocalizedAttributeValue(m_stringTable, (XmlNode)y, "label", null);
				if (yVal == null)
					yVal = XmlUtils.GetManditoryAttributeValue((XmlNode)y, "label");
				return xVal.CompareTo(yVal);
			}

			#endregion
		}
	}

	/// <summary>
	/// Used for items in optionsList
	/// </summary>
	class OptionListItem
	{
		XmlNode m_item;
		public OptionListItem(XmlNode item)
		{
			m_item = item;
		}

		public XmlNode Item
		{
			get { return m_item; }
		}

		public override string ToString()
		{
			return XmlUtils.GetManditoryAttributeValue(m_item, "label");
		}

	}
	/// <summary>
	///
	/// </summary>
	public class WsComboItem
	{
		private readonly string m_name;
		private readonly string m_id;
		/// <summary>
		/// Initializes a new instance of the <see cref="WsComboItem"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="id">The writing system ID.</param>
		public WsComboItem(string name, string id)
		{
			m_name = name;
			m_id = id;
		}
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </returns>
		public override string ToString()
		{
			return m_name;
		}
		/// <summary>
		/// Gets the writing system identifier.
		/// </summary>
		/// <value>The writing system identifier.</value>
		public string Id
		{
			get { return m_id;}
		}
	}
}
