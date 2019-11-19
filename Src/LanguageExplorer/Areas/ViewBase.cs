// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.XMLViews;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.Xml;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// XmlDocView is a view that shows a complete list as a single view.
	/// A RecordList class does most of the work of managing the list and current object.
	///	list management and navigation is entirely(?) handled by the
	/// RecordList.
	///
	/// The actual view of each object is specified by a child <jtview></jtview> node
	/// of the view node. This specifies how to display an individual list item.
	/// </summary>
	internal abstract class ViewBase : MainUserControl, IMainContentControl, IPaneBarUser
	{
		#region Data members
		/// <summary>
		/// The configuration 'parameters' element for the browser view.
		/// </summary>
		protected XElement m_configurationParametersElement;
		/// <summary>
		/// Optional information bar above the main control.
		/// </summary>
		protected UserControl m_informationBar;
		/// <summary>
		/// the list
		/// </summary>
		protected int m_madeUpFieldIdentifier;
		/// <summary>
		/// This is used to keep us from responding to messages that we get while
		/// we are still trying to get initialized.
		/// </summary>
		protected bool m_fullyInitialized;
		/// <summary>
		/// tell whether the tree bar is required, optional, or not allowed for this view
		/// </summary>
		protected TreebarAvailability m_treebarAvailability;
		/// <summary>
		/// Last known parent that is a MultiPane.
		/// </summary>
		private MultiPane m_mpParent;
		private Container components = null;
		/// <summary>
		/// Sometimes an active record list (eg., in a view) is repurposed (eg., in a dialog for printing).
		/// When finished, recordList.BecomeInactive() is called, but that causes records not to be shown
		/// in the active view. This guard prevents that.
		/// </summary>
		private bool m_haveActiveRecordList;

		#endregion Data members

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public virtual void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;
		}

		#endregion

		#region Consruction and disposal
		/// <summary />
		protected ViewBase()
		{
			m_fullyInitialized = false;
			InitializeComponent();
			AccNameDefault = "ViewBase";
		}

		/// <summary />
		protected ViewBase(XElement configurationParametersElement, LcmCache cache, IRecordList recordList)
			: this()
		{
			ConstructorSurrogate(configurationParametersElement, cache, recordList);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
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
				components?.Dispose();
				if (MyRecordList != null && !m_haveActiveRecordList)
				{
					m_haveActiveRecordList = false;
				}
#if RANDYTODO
				// Block for now.
				if (m_mpParent != null)
				{
					m_mpParent.ShowFirstPaneChanged -= mp_ShowFirstPaneChanged;
				}
#endif
			}
			m_informationBar = null; // Should be disposed automatically, since it is in the Controls collection.
			m_mpParent = null;
			MyRecordList = null;

			base.Dispose(disposing);
		}

		#endregion // Consruction and disposal

		#region Properties

		/// <summary>
		/// LCM cache.
		/// </summary>
		protected LcmCache Cache { get; private set; }

		/// <summary>
		/// Get/Set the record list used by the view.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IRecordList MyRecordList { get; set; }

		internal virtual void ConstructorSurrogate(XElement configurationParametersElement, LcmCache cache, IRecordList recordList)
		{
			m_configurationParametersElement = configurationParametersElement;
			Cache = cache;
			MyRecordList = recordList;
		}

		public IPaneBar MainPaneBar
		{
			get
			{
				if (m_informationBar != null)
				{
					return m_informationBar as IPaneBar;
				}

				return (Parent is IPaneBarContainer) ? ((IPaneBarContainer)Parent).PaneBar : null;
			}
			set
			{
				if (m_informationBar != null)
				{
					throw new NotSupportedException("Don't even 'think' of setting it more than once!");
				}
				m_informationBar = value as UserControl;
			}
		}

		#endregion Properties

		#region IMainContentControl implementation

		/// <summary>
		/// From IMainContentControl
		/// </summary>
		/// <returns>true if ok to go away</returns>
		public virtual bool PrepareToGoAway()
		{
			return true;
		}

		public string AreaName => PropertyTable.GetValue<string>(AreaServices.AreaChoice);
		#endregion // IMainContentControl implementation

		#region ICtrlTabProvider implementation

		public virtual Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			Guard.AgainstNull(targetCandidates, nameof(targetCandidates));

			targetCandidates.Add(this);
			return ContainsFocus ? this : null;
		}

		#endregion  ICtrlTabProvider implementation

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// RecordView
			//
			this.Name = "RecordView";
			this.Size = new System.Drawing.Size(752, 150);
			this.ResumeLayout(false);

		}
		#endregion

		#region Other methods

		protected virtual void AddPaneBar()
		{
		}

		private const string kEllipsis = "...";
		protected string TrimToMaxPixelWidth(int pixelWidthAllowed, string sToTrim)
		{
			if (sToTrim.Length == 0)
			{
				return sToTrim;
			}
			var sPixelWidth = GetWidthOfStringInPixels(sToTrim);
			var avgPxPerChar = sPixelWidth / Convert.ToSingle(sToTrim.Length);
			var charsAllowed = Convert.ToInt32(pixelWidthAllowed / avgPxPerChar);
			if (charsAllowed < 5)
			{
				return string.Empty;
			}
			return sPixelWidth < pixelWidthAllowed ? sToTrim : sToTrim.Substring(0, charsAllowed - 4) + kEllipsis;
		}

		private int GetWidthOfStringInPixels(string sInput)
		{
			using (var g = Graphics.FromHwnd(Handle))
			{
				return Convert.ToInt32(g.MeasureString(sInput, TitleBarFont).Width);
			}
		}

		protected Control TitleBar => m_informationBar.Controls[0];

		protected Font TitleBarFont => TitleBar.Font;

		protected void ResetSpacer(int spacerWidth, string activeLayoutName)
		{
			var bar = TitleBar;
			if (!(bar is Panel) || bar.Controls.Count <= 1)
			{
				return;
			}
			var cctrls = bar.Controls.Count;
			bar.Controls[cctrls - 1].Width = spacerWidth;
			bar.Controls[cctrls - 1].Text = activeLayoutName;
			bar.Refresh();
		}

		protected string GetBaseTitleStringFromConfig()
		{
			var titleStr = string.Empty;
			// See if we have an AlternativeTitle string table id for an alternate title.
			var titleId = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "altTitleId");
			if (titleId != null)
			{
				titleStr = StringTable.Table.GetString(titleId, StringTable.AlternativeTitles);
				if (MyRecordList.OwningObject != null && XmlUtils.GetBooleanAttributeValue(m_configurationParametersElement, "ShowOwnerShortname"))
				{
					// Originally this option was added to enable the Reversal Index title bar to show
					// which reversal index was being shown.
					titleStr = string.Format(AreaResources.ksXReversalIndex, MyRecordList.OwningObject.ShortName, titleStr);
				}
			}
			else if (MyRecordList.OwningObject != null)
			{
				if (XmlUtils.GetBooleanAttributeValue(m_configurationParametersElement, "ShowOwnerShortname"))
				{
					titleStr = MyRecordList.OwningObject.ShortName;
				}
			}
			return titleStr;
		}

		/// <summary>
		/// When our parent changes, we may need to re-evaluate whether to show our info bar.
		/// </summary>
		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);
			if (Parent == null)
			{
				return;
			}
			var mp = Parent as MultiPane ?? Parent.Parent as MultiPane;
			if (mp == null)
			{
				return;
			}
			var suppress = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "suppressInfoBar", "false");
			if (suppress == "ifNotFirst")
			{
#if RANDYTODO
				// Block for now.
				mp.ShowFirstPaneChanged += mp_ShowFirstPaneChanged;
#endif
				m_mpParent = mp;
				mp_ShowFirstPaneChanged(mp, new EventArgs());
			}
		}

		/// <summary>
		/// Read in the parameters to determine which sequence/collection we are editing.
		/// </summary>
		protected virtual void ReadParameters()
		{
		}

		protected virtual void ShowRecord()
		{
			SetInfoBarText();
		}

		protected abstract void SetupDataContext();

		protected virtual void SetupStylesheet()
		{
			// Do nothing here.
		}

		/// <summary>
		/// Sets the title string to an appropriate default when nothing is specified in the xml configuration for the view
		/// </summary>
		protected virtual void SetInfoBarText()
		{
			if (m_informationBar == null)
			{
				return;
			}
			var className = StringTable.Table.GetString("No Record", StringTable.Misc);
			if (MyRecordList.CurrentObject != null)
			{
				var typeName = MyRecordList.CurrentObject.GetType().Name;
				if (MyRecordList.CurrentObject is ICmPossibility)
				{
					var possibility = (ICmPossibility)MyRecordList.CurrentObject;
					className = possibility.ItemTypeName();
				}
				else
				{
					className = StringTable.Table.GetString(typeName, StringTable.ClassNames);
				}
				if (className == "*" + typeName + "*")
				{
					className = typeName;
				}
			}
			else
			{
				var emptyTitleId = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "emptyTitleId");
				if (!string.IsNullOrEmpty(emptyTitleId))
				{
					string titleStr;
					XmlViewsUtils.TryFindString(StringTable.EmptyTitles, emptyTitleId, out titleStr);
					if (titleStr != "*" + emptyTitleId + "*")
					{
						className = titleStr;
					}
					MyRecordList.UpdateStatusBarRecordNumber(titleStr);
				}
			}
			// This code:  ((IPaneBar)m_informationBar).Text = className;
			// causes about 47 of the following exceptions when executed in Flex.
			// First-chance exception at 0x4ed9b280 in Flex.exe: 0xC0000005: Access violation writing location 0x00f90004.
			// The following code doesn't cause the exception, but neither one actually sets the Text to className,
			// so something needs to be changed somewhere. It doesn't enter "override string Text" in PaneBar.cs
			((IPaneBar)m_informationBar).Text = className;
		}

		#endregion Other methods

		#region Event handlers

		private void mp_ShowFirstPaneChanged(object sender, EventArgs e)
		{
			var mpSender = (MultiPane)sender;

			var fWantInfoBar = (this == mpSender.FirstVisibleControl);
			if (fWantInfoBar && m_informationBar == null)
			{
				AddPaneBar();
				if (m_informationBar != null)
				{
					SetInfoBarText();
				}
			}
			else if (m_informationBar != null && !fWantInfoBar)
			{
				Controls.Remove(m_informationBar);
				m_informationBar.Dispose();
				m_informationBar = null;
			}
		}

		#endregion Event handlers

#if RANDYTODO
		public bool OnDisplayShowTreeBar(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = (m_treebarAvailability == TreebarAvailability.Optional);
			return true;//we handled this, no need to ask anyone else.
		}

		public bool OnDisplayExport(object commandObject, ref UIItemDisplayProperties display)
		{
			string areaChoice = m_propertyTable.GetValue<string>(AreaServices.AreaChoice);
			bool inFriendlyTerritory = (areaChoice == AreaServices.InitialAreaMachineName
#if RANDYTODO
			// TODO: The "notebook" area uses its own dlg. See: RecordList's method: OnExport
#endif
				|| areaChoice == AreaServices.NotebookAreaMachineName
#if RANDYTODO
			// TODO: These "textsWords" tools use the "concordanceWords" record list, so can handle the File_Export menu:
			// TODO: Analyses, bulkEditWordforms, and wordListConcordance.
			// TODO: These tools in the "textsWords" do not support the File_Export menu, so it is not visible for them:
			// TODO: complexConcordance, concordance, corpusStatistics, interlinearEdit
#endif
				|| (areaChoice == AreaServices.TextAndWordsAreaMachineName && MyRecordList.Id == "concordanceWords")
#if RANDYTODO
			// TODO: The "grammarSketch" tool in the "grammar" area uses its own dlg. See: GrammarSketchHtmlViewer's method: OnExport
			// TODO: All other "grammar" area tools use the basic dlg and worry about some custom lexicon properties.
#endif
				|| areaChoice == AreaServices.GrammarAreaMachineName
				|| areaChoice == AreaServices.ListsAreaMachineName);
			if (inFriendlyTerritory)
				display.Enabled = display.Visible = true;
			else
				display.Enabled = display.Visible = false;

			return true;
		}
#endif
	}
}