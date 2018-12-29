// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;

namespace LanguageExplorer.Areas.Lexicon.Tools.ReversalIndexes
{
	/// <summary />
	internal sealed class ReversalEntryGoDlg : BaseGoDlg
	{
		private readonly HashSet<int> m_FilteredReversalEntryHvos = new HashSet<int>();

		/// <summary />
		public ReversalEntryGoDlg()
		{
			SetHelpTopic("khtpFindReversalEntry");
			InitializeComponent();
		}

		/// <summary>
		/// Gets or sets the reversal index.
		/// </summary>
		public IReversalIndex ReversalIndex { get; set; }

		/// <summary />
		public ICollection<int> FilteredReversalEntryHvos => m_FilteredReversalEntryHvos;

		/// <summary />
		protected override string PersistenceLabel => "ReversalEntryGo";

		/// <summary />
		protected override void InitializeMatchingObjects()
		{
#if RANDYTODO
			// TODO: Make a resource from this xml (discarding junk from it) and feed it where needed.
/*
			<guicontrol id="matchingReversalEntries">
				<parameters id="reventryMatchList" listItemsClass="ReversalIndexEntry" filterBar="false" treeBarAvailability="NotAllowed" defaultCursor="Arrow" hscroll="true" altTitleId="ReversalIndexEntry-Plural" editable="false" disableConfigButton="true">
					<columns>
						<column label="Form" sortmethod="FullSortKey" ws="$ws=reversal" editable="false" width="96000">
							<span>
								<properties>
									<editable value="false"/>
								</properties>
								<string field="ReversalForm" ws="reversal"/>
							</span>
						</column>
						<column label="Category" width="96000">
							<span>
								<properties>
									<editable value="false"/>
								</properties>
								<obj field="PartOfSpeech" layout="empty">
									<span>
										<properties>
											<editable value="false"/>
										</properties>
										<string field="Name" ws="best analysis"/>
									</span>
								</obj>
							</span>
						</column>
					</columns>
				</parameters>
			</guicontrol>
*/
#endif
#if RANDYTODO
			// TODO: Nobody will be home.
#endif
			var xnWindow = PropertyTable.GetValue<XElement>("WindowConfiguration");
			var configNode = xnWindow.XPathSelectElement("controls/parameters/guicontrol[@id=\"matchingReversalEntries\"]/parameters");

			var searchEngine = (ReversalEntrySearchEngine)SearchEngine.Get(PropertyTable, "ReversalEntrySearchEngine-" + ReversalIndex.Hvo,
				() => new ReversalEntrySearchEngine(m_cache, ReversalIndex));
			searchEngine.FilteredEntryHvos = m_FilteredReversalEntryHvos;

			m_matchingObjectsBrowser.Initialize(m_cache, FwUtils.StyleSheetFromPropertyTable(PropertyTable), configNode, searchEngine, m_cache.ServiceLocator.WritingSystemManager.Get(ReversalIndex.WritingSystem));

			// start building index
			var wsObj = (CoreWritingSystemDefinition)m_cbWritingSystems.SelectedItem;
			if (wsObj != null)
			{
				var tss = TsStringUtils.EmptyString(wsObj.Handle);
				var field = new SearchField(ReversalIndexEntryTags.kflidReversalForm, tss);
				m_matchingObjectsBrowser.SearchAsync(new[] { field });
			}
		}

		/// <summary />
		protected override void LoadWritingSystemCombo()
		{
			m_cbWritingSystems.Items.Add(m_cache.ServiceLocator.WritingSystemManager.Get(ReversalIndex.WritingSystem));
		}

		/// <summary />
		public override void SetDlgInfo(LcmCache cache, WindowParams wp)
		{
			SetDlgInfo(cache, wp, cache.ServiceLocator.WritingSystemManager.GetWsFromStr(ReversalIndex.WritingSystem));
		}

		/// <summary />
		public override void SetDlgInfo(LcmCache cache, WindowParams wp, string form)
		{
			SetDlgInfo(cache, wp, form, cache.ServiceLocator.WritingSystemManager.GetWsFromStr(ReversalIndex.WritingSystem));
		}

		/// <summary />
		protected override void ResetMatches(string searchKey)
		{
			if (m_oldSearchKey == searchKey)
			{
				return; // Nothing new to do, so skip it.
			}
			// disable Go button until we rebuild our match list.
			m_btnOK.Enabled = false;
			m_oldSearchKey = searchKey;
			var wsObj = (CoreWritingSystemDefinition)m_cbWritingSystems.SelectedItem;
			if (wsObj == null)
			{
				return;
			}
			if (m_oldSearchKey != string.Empty || searchKey != string.Empty)
			{
				StartSearchAnimation();
			}
			var tss = TsStringUtils.MakeString(searchKey, wsObj.Handle);
			var field = new SearchField(ReversalIndexEntryTags.kflidReversalForm, tss);
			m_matchingObjectsBrowser.SearchAsync(new[] { field });
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ReversalEntryGoDlg));
			this.m_panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).BeginInit();
			this.SuspendLayout();
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			//
			// m_btnInsert
			//
			resources.ApplyResources(this.m_btnInsert, "m_btnInsert");
			//
			// m_cbWritingSystems
			//
			resources.ApplyResources(this.m_cbWritingSystems, "m_cbWritingSystems");
			//
			// m_objectsLabel
			//
			resources.ApplyResources(this.m_objectsLabel, "m_objectsLabel");
			//
			// ReversalEntryGoDlg
			//
			resources.ApplyResources(this, "$this");
			this.m_helpProvider.SetHelpNavigator(this, ((System.Windows.Forms.HelpNavigator)(resources.GetObject("$this.HelpNavigator"))));
			this.Name = "ReversalEntryGoDlg";
			this.m_helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.m_panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		private sealed class ReversalEntrySearchEngine : SearchEngine
		{
			private readonly IReversalIndex m_reversalIndex;
			private readonly IReversalIndexEntryRepository m_revEntryRepository;

			public ICollection<int> FilteredEntryHvos { private get; set; }

			public ReversalEntrySearchEngine(LcmCache cache, IReversalIndex reversalIndex)
				: base(cache, SearchType.Prefix)
			{
				m_reversalIndex = reversalIndex;
				m_revEntryRepository = Cache.ServiceLocator.GetInstance<IReversalIndexEntryRepository>();
			}

			/// <summary />
			protected override IEnumerable<ITsString> GetStrings(SearchField field, ICmObject obj)
			{
				var rie = (IReversalIndexEntry)obj;
				var ws = field.String.get_WritingSystemAt(0);
				switch (field.Flid)
				{
					case ReversalIndexEntryTags.kflidReversalForm:
						var form = rie.ReversalForm.StringOrNull(ws);
						if (form != null && form.Length > 0)
						{
							yield return form;
						}
						break;

					default:
						throw new ArgumentException(@"Unrecognized field.", "field");
				}
			}

			/// <summary />
			protected override IList<ICmObject> GetSearchableObjects()
			{
				return m_reversalIndex.AllEntries.Cast<ICmObject>().ToArray();
			}

			/// <summary />
			protected override bool IsIndexResetRequired(int hvo, int flid)
			{
				switch (flid)
				{
					case ReversalIndexTags.kflidEntries:
						return hvo == m_reversalIndex.Hvo;
					case ReversalIndexEntryTags.kflidReversalForm:
						return m_revEntryRepository.GetObject(hvo).ReversalIndex == m_reversalIndex;
				}
				return false;
			}

			/// <summary />
			protected override bool IsFieldMultiString(SearchField field)
			{
				switch (field.Flid)
				{
					case ReversalIndexEntryTags.kflidReversalForm:
						return true;
				}

				throw new ArgumentException(@"Unrecognized field.", "field");
			}

			protected override IEnumerable<int> FilterResults(IEnumerable<int> results)
			{
				return results?.Where(hvo => !FilteredEntryHvos.Contains(hvo));
			}

			/// <inheritdoc />
			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");
				base.Dispose(disposing);
			}
		}
	}
}