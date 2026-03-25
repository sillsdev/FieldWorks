// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using XCore;
using SIL.FieldWorks.Common.Widgets;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// This control handles all of the various text labels and other widgets
	/// used to set up an MSA of any of the classes.
	/// </summary>
	public class MSAGroupBox : UserControl
	{
		#region Data members

		private PropertyTable m_propertyTable;
		private Form m_parentForm;
		private LcmCache m_cache;
		private Control m_ctrlAssistant;
		private POSPopupTreeManager m_mainPOSPopupTreeManager;
		private POSPopupTreeManager m_secPOSPopupTreeManager;
		private MsaType m_msaType = MsaType.kNotSet;
		private IPartOfSpeech m_selectedMainPOS = null;
		private IPartOfSpeech m_selectedSecondaryPOS = null;
		private IMoInflAffixSlot m_selectedSlot = null;
		private bool m_skipEvents = false;
		private IMoMorphType m_morphType;

		#region Designer data members

		private System.Windows.Forms.GroupBox m_groupBox;
		private SIL.FieldWorks.Common.Widgets.TreeCombo m_tcSecondaryPOS;
		private System.Windows.Forms.Label m_lSLots;
		private SIL.FieldWorks.Common.Widgets.FwComboBox m_fwcbSlots;
		private SIL.FieldWorks.Common.Widgets.TreeCombo m_tcMainPOS;
		private System.Windows.Forms.Label m_lMainCat;
		private System.Windows.Forms.Label m_lAfxType;
		private SIL.FieldWorks.Common.Widgets.FwComboBox m_fwcbAffixTypes;
		private System.Windows.Forms.FlowLayoutPanel m_flowLayout;
		private System.Windows.Forms.Panel m_afxTypePanel;
		private System.Windows.Forms.Panel m_mainCatPanel;
		private System.Windows.Forms.Panel m_slotsPanel;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		#endregion Designer data members

		#endregion Data members

		#region Properties

		public SandboxGenericMSA SandboxMSA
		{
			get
			{
				CheckDisposed();

				SandboxGenericMSA sandoxMSA = new SandboxGenericMSA();
				sandoxMSA.MsaType = MSAType;
				switch (MSAType)
				{
					case MsaType.kRoot: // Fall through
					case MsaType.kStem:
					{
						sandoxMSA.MainPOS = MainPOS;
						break;
					}
					case MsaType.kInfl:
					{
						sandoxMSA.MainPOS = MainPOS;
						if (Slot != null && SlotIsValidForPos)
							sandoxMSA.Slot = Slot;
						break;
					}
					case MsaType.kDeriv:
					{
						sandoxMSA.MainPOS = MainPOS;
						sandoxMSA.SecondaryPOS = SecondaryPOS;
						break;
					}
					case MsaType.kUnclassified:
					{
						sandoxMSA.MainPOS = MainPOS;
						break;
					}
				}
				return sandoxMSA;
			}
		}

		public IPartOfSpeech MainPOS
		{
			get
			{
				CheckDisposed();
				return m_selectedMainPOS;
			}
		}


		public IPartOfSpeech StemPOS
		{
			set
			{
				CheckDisposed();

				// We don't really have a way of setting it to nothing, because that behavior
				// isn't implemented in the PopupTree. We haven't needed this yet, but this
				// Assert should catch a new situation where it is.
				// Basically, it's an error to try to set it to zero, unless it already is.
				Debug.Assert(value != null || m_selectedMainPOS == null);
				if (value == null)
					return; // Can't set it zero, no matter what, until PopupTree supports it.

				m_selectedMainPOS = value;
				if (MSAType != MsaType.kStem)
					MSAType = MsaType.kStem;
				// In order to select the node, we must have loaded the tree.
				TrySelectNode(m_tcMainPOS, m_selectedMainPOS.Hvo);
			}
		}

		public IPartOfSpeech SecondaryPOS
		{
			get
			{
				CheckDisposed();
				return m_selectedSecondaryPOS;
			}
		}

		/// <summary>
		/// Is the current slot valid? It might not be, in the bizarre case that the user
		/// requests to make a new inflectional affix for a slot, but gives it a POS that
		/// doesn't have that slot.
		/// </summary>
		public bool SlotIsValidForPos
		{
			get
			{
				CheckDisposed();
				return m_fwcbSlots.SelectedIndex >= 0;
			}
		}

		public IMoInflAffixSlot Slot
		{
			get
			{
				CheckDisposed();

				return m_selectedSlot;
			}
			set
			{
				CheckDisposed();

				// Setting it to zero is supported, as it sets the selected index to -1.
				if (value == null)
					m_fwcbSlots.SelectedIndex = -1;
				else
				{
					if (MSAType != MsaType.kInfl)
						MSAType = MsaType.kInfl;

					m_selectedSlot = value;
					if (m_fwcbSlots.Items.Count == 0)
					{
						var pos = value.Owner as IPartOfSpeech;
						m_selectedMainPOS = pos;
						// In order to select the node, we must have loaded the tree.
						if (TrySelectNode(m_tcMainPOS, m_selectedMainPOS.Hvo))
						{
							// This is automatic when the user changes it, but not when the program does.
							ResetSlotCombo();
						}
					}
				}
			}
		}

		bool TrySelectNode(TreeCombo treeCombo, int hvoTarget)
		{
			if (treeCombo.Tree.Nodes.Count == 0)
			{
				m_mainPOSPopupTreeManager.LoadPopupTree(hvoTarget);
				return true;
			}
			else
			{
				foreach (HvoTreeNode node in treeCombo.Tree.Nodes)
				{
					HvoTreeNode htn = node.NodeWithHvo(hvoTarget);
					if (htn != null)
					{
						// Selecting the POS here should then fire
						// the event which wil reset the slot combo.
						treeCombo.SelectedNode = htn;
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Disable these two controls (for use when creating an entry for a particular slot)
		/// </summary>
		public void DisableAffixTypeMainPosAndSlot()
		{
			CheckDisposed();

			m_tcMainPOS.Enabled = false;
			m_fwcbSlots.Enabled = false;
			m_fwcbAffixTypes.Enabled = false;
		}

		public MsaType MSAType
		{
			get
			{
				CheckDisposed();
				return m_msaType;
			}
			set
			{
				CheckDisposed();

				if (value == m_msaType)
					return; // Nothing else to do.
				m_msaType = value;
				if (m_ctrlAssistant != null)
					m_ctrlAssistant.Enabled = ((value == MsaType.kInfl)/*See LT-7278. || (value == MsaType.kDeriv)*/);
				try
				{
					m_skipEvents = true;
					switch (value)
					{
						case MsaType.kRoot: // Fall through.
						case MsaType.kStem:
						{
							m_flowLayout.SuspendLayout();
							m_lMainCat.Text = LexTextControls.ksCategor_y;
							// Hide the panels we don't need, show the ones we do.
							m_afxTypePanel.Visible = false;
							m_slotsPanel.Visible = false;

							m_lMainCat.TabIndex = 0;
							m_tcMainPOS.TabIndex = 1;
							m_mainPOSPopupTreeManager.SetEmptyLabel(LexTextControls.ks_NotSure_);
							m_flowLayout.ResumeLayout();
							break;
						}
						case MsaType.kUnclassified:
						{
							m_flowLayout.SuspendLayout();
							m_lMainCat.Text = LexTextControls.ksAttachesToCategor_y;
							// Hide the panels we don't need, show the ones we do.
							m_afxTypePanel.Visible = true;
							m_slotsPanel.Visible = false;

							m_lAfxType.TabIndex = 0;
							m_fwcbAffixTypes.TabIndex = 1;
							m_fwcbAffixTypes.SelectedIndex = 0;
							m_lMainCat.TabIndex = 2;
							m_tcMainPOS.TabIndex = 3;
							m_mainPOSPopupTreeManager.SetEmptyLabel(LexTextControls.ksAny);
							m_flowLayout.ResumeLayout();
							break;
						}
						case MsaType.kInfl:
						{
							m_flowLayout.SuspendLayout();
							m_lMainCat.Text = LexTextControls.ksAttachesToCategor_y;
							m_lSLots.Text = LexTextControls.ks_FillsSlot;
							// Show all panels; within slots panel, show slots combo, hide secondary POS.
							m_afxTypePanel.Visible = true;
							m_slotsPanel.Visible = true;
							m_fwcbSlots.Visible = true;
							m_tcSecondaryPOS.Visible = false;

							m_lAfxType.TabIndex = 0;
							m_fwcbAffixTypes.TabIndex = 1;
							m_fwcbAffixTypes.SelectedIndex = 1;
							m_lMainCat.TabIndex = 2;
							m_tcMainPOS.TabIndex = 3;
							m_lSLots.TabIndex = 4;
							m_fwcbSlots.TabIndex = 5;
							m_fwcbSlots.TabStop = true;
							m_fwcbSlots.Enabled = m_fwcbSlots.Items.Count > 0;
							m_lSLots.Enabled = m_fwcbSlots.Enabled;
							m_mainPOSPopupTreeManager.SetEmptyLabel(LexTextControls.ksAny);
							m_flowLayout.ResumeLayout();
							break;
						}
						case MsaType.kDeriv:
						{
							m_flowLayout.SuspendLayout();
							m_lMainCat.Text = LexTextControls.ksAttachesToCategor_y;
							m_lSLots.Text = LexTextControls.ksC_hangesToCategory;
							// Show all panels; within slots panel, show secondary POS, hide slots combo.
							m_afxTypePanel.Visible = true;
							m_slotsPanel.Visible = true;
							m_fwcbSlots.Visible = false;
							m_tcSecondaryPOS.Visible = true;

							m_lAfxType.TabIndex = 0;
							m_fwcbAffixTypes.TabIndex = 1;
							m_fwcbAffixTypes.SelectedIndex = 2;
							m_lMainCat.TabIndex = 2;
							m_tcMainPOS.TabIndex = 3;
							m_lSLots.TabIndex = 4;
							m_tcSecondaryPOS.TabIndex = 5;
							m_lSLots.Enabled = true;
							m_mainPOSPopupTreeManager.SetEmptyLabel(LexTextControls.ksAny);
							m_flowLayout.ResumeLayout();
							break;
						}
					}
				}
				finally
				{
					m_skipEvents = false;
				}
			}
		}

		public IMoMorphType MorphTypePreference
		{
			set
			{
				CheckDisposed();

				if (value == null)
				{
					// Someone could may try and set it to null,
					// so pick stem, if they do.
					MSAType = MsaType.kStem;
					return;
				}
				m_morphType = value;
				if (MSAType == MsaType.kInfl)
					ResetSlotCombo();
				string sGuid = m_morphType.Guid.ToString();
				Debug.Assert(sGuid != null && sGuid != String.Empty);
				switch (sGuid)
				{
					case MoMorphTypeTags.kMorphStem:
					case MoMorphTypeTags.kMorphBoundStem:
					case MoMorphTypeTags.kMorphPhrase:
					case MoMorphTypeTags.kMorphDiscontiguousPhrase:
						MSAType = MsaType.kStem;
						break;
					case MoMorphTypeTags.kMorphProclitic:
					case MoMorphTypeTags.kMorphClitic:
					case MoMorphTypeTags.kMorphEnclitic:
					case MoMorphTypeTags.kMorphParticle:
					case MoMorphTypeTags.kMorphRoot:
					case MoMorphTypeTags.kMorphBoundRoot:
						MSAType = MsaType.kRoot;
						break;
					default:
						/*
						  MoMorphTypeTags.kMorphInfix
						  MoMorphTypeTags.kMorphPrefix
						  MoMorphTypeTags.kMorphSimulfix
						  MoMorphTypeTags.kMorphSuffix
						  MoMorphTypeTags.kMorphSuprafix
						  MoMorphTypeTags.kMorphCircumfix
						  MoMorphTypeTags.kMorphInfixingInterfix
						  MoMorphTypeTags.kMorphPrefixingInterfix
						  MoMorphTypeTags.kMorphSuffixingInterfix
						*/
						// It may already be set to a better type than MsaType.kUnclassified,
						// so leave it alone, if it is.
						if (MSAType == MsaType.kRoot || MSAType == MsaType.kStem)
							MSAType = MsaType.kUnclassified;
						break;
				}
			}
		}

		/// <summary>
		/// This returns the height of the control if all the internal FwComboBox and
		/// TreeCombo controls are adjusted to their preferred heights.
		/// </summary>
		public int PreferredHeight
		{
			get
			{
				int nHeight = this.Height;
				// All combos/trees are in the same visual row within their panels,
				// so we only need the maximum delta across all of them.
				int maxDelta = 0;
				int delta = m_fwcbAffixTypes.PreferredHeight - m_fwcbAffixTypes.Height;
				if (delta > 0)
					maxDelta = Math.Max(maxDelta, delta);
				delta = m_tcMainPOS.PreferredHeight - m_tcMainPOS.Height;
				if (delta > 0)
					maxDelta = Math.Max(maxDelta, delta);
				delta = Math.Max(m_fwcbSlots.PreferredHeight - m_fwcbSlots.Height,
					m_tcSecondaryPOS.PreferredHeight - m_tcSecondaryPOS.Height);
				if (delta > 0)
					maxDelta = Math.Max(maxDelta, delta);
				return nHeight + maxDelta;
			}
		}

		#endregion Properties

		#region Construction, initialization, and disposal

		/// <summary>
		/// Constructor.
		/// </summary>
		public MSAGroupBox()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
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
		protected override void Dispose( bool disposing )
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (m_mainPOSPopupTreeManager != null)
				{
					m_mainPOSPopupTreeManager.AfterSelect -= new TreeViewEventHandler(m_mainPOSPopupTreeManager_AfterSelect);
					m_mainPOSPopupTreeManager.Dispose();
				}
				m_mainPOSPopupTreeManager = null;
				if (m_secPOSPopupTreeManager != null)
				{
					m_secPOSPopupTreeManager.AfterSelect -= new TreeViewEventHandler(m_secPOSPopupTreeManager_AfterSelect);
					m_secPOSPopupTreeManager.Dispose();
				}
				if(components != null)
				{
					components.Dispose();
				}
			}
			m_parentForm = null;
			m_secPOSPopupTreeManager = null;
			m_lAfxType = null;
			m_fwcbAffixTypes = null;
			m_lSLots = null;
			m_fwcbSlots = null;
			m_tcSecondaryPOS = null;
			m_ctrlAssistant = null;

			base.Dispose( disposing );
		}

		/// <summary>
		/// Initialize the control.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="ctrlAssistant"></param>
		/// <param name="parentForm"></param>
		/// <param name="propertyTable"></param>
		public void Initialize(LcmCache cache, Mediator mediator, XCore.PropertyTable propertyTable, Control ctrlAssistant, Form parentForm)
		{
			CheckDisposed();

			Debug.Assert(ctrlAssistant != null);
			m_ctrlAssistant = ctrlAssistant;
			Initialize(cache, mediator, propertyTable, parentForm, new SandboxGenericMSA());
		}

		/// <summary>
		/// Initialize the control.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// <param name="parentForm"></param>
		/// <param name="sandboxMSA"></param>
		public void Initialize(LcmCache cache, Mediator mediator, PropertyTable propertyTable, Form parentForm, SandboxGenericMSA sandboxMSA)
		{
			CheckDisposed();

			m_parentForm = parentForm;
			m_cache = cache;
			m_propertyTable = propertyTable;

			IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable);
			int defUserWs = m_cache.ServiceLocator.WritingSystemManager.UserWs;
			CoreWritingSystemDefinition defAnalWs = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
			string defAnalWsFont = defAnalWs.DefaultFontName;

			m_fwcbAffixTypes.WritingSystemFactory = m_cache.WritingSystemFactory;
			m_fwcbAffixTypes.WritingSystemCode = defAnalWs.Handle;
			m_fwcbAffixTypes.Items.Add(TsStringUtils.MakeString(LexTextControls.ksNotSure, defUserWs));
			m_fwcbAffixTypes.Items.Add(TsStringUtils.MakeString(LexTextControls.ksInflectional, defUserWs));
			m_fwcbAffixTypes.Items.Add(TsStringUtils.MakeString(LexTextControls.ksDerivational, defUserWs));
			m_fwcbAffixTypes.StyleSheet = stylesheet;
			m_fwcbAffixTypes.AdjustStringHeight = false;

			m_fwcbSlots.Font = new Font(defAnalWsFont, 10);
			m_fwcbSlots.WritingSystemFactory = m_cache.WritingSystemFactory;
			m_fwcbSlots.WritingSystemCode = defAnalWs.Handle;
			m_fwcbSlots.StyleSheet = stylesheet;
			m_fwcbSlots.AdjustStringHeight = false;

			m_tcMainPOS.Font = new Font(defAnalWsFont, 10);
			m_tcMainPOS.WritingSystemFactory = m_cache.WritingSystemFactory;
			m_tcMainPOS.WritingSystemCode = defAnalWs.Handle;
			m_tcMainPOS.StyleSheet = stylesheet;
			m_tcMainPOS.AdjustStringHeight = false;

			m_tcSecondaryPOS.Font = new Font(defAnalWsFont, 10);
			m_tcSecondaryPOS.WritingSystemFactory = m_cache.WritingSystemFactory;
			m_tcSecondaryPOS.WritingSystemCode = defAnalWs.Handle;
			m_tcSecondaryPOS.StyleSheet = stylesheet;
			m_tcSecondaryPOS.AdjustStringHeight = false;

			m_selectedMainPOS = sandboxMSA.MainPOS;
			m_fwcbAffixTypes.SelectedIndex = 0;
			m_fwcbAffixTypes.SelectedIndexChanged += HandleComboMSATypesChange;
			m_mainPOSPopupTreeManager = new POSPopupTreeManager(m_tcMainPOS, m_cache,
				m_cache.LanguageProject.PartsOfSpeechOA,
				defAnalWs.Handle, false, mediator, m_propertyTable,
				m_parentForm);
			m_mainPOSPopupTreeManager.NotSureIsAny = true;
			m_mainPOSPopupTreeManager.LoadPopupTree(m_selectedMainPOS != null ? m_selectedMainPOS.Hvo : 0);
			m_mainPOSPopupTreeManager.AfterSelect += m_mainPOSPopupTreeManager_AfterSelect;
			m_fwcbSlots.SelectedIndexChanged += HandleComboSlotChange;
			m_secPOSPopupTreeManager = new POSPopupTreeManager(m_tcSecondaryPOS, m_cache,
				m_cache.LanguageProject.PartsOfSpeechOA,
				defAnalWs.Handle, false, mediator, m_propertyTable,
				m_parentForm);
			m_secPOSPopupTreeManager.NotSureIsAny = true; // only used for affixes.
			m_selectedSecondaryPOS = sandboxMSA.SecondaryPOS;
			m_secPOSPopupTreeManager.LoadPopupTree(m_selectedSecondaryPOS != null ? m_selectedSecondaryPOS.Hvo : 0);
			m_secPOSPopupTreeManager.AfterSelect += m_secPOSPopupTreeManager_AfterSelect;

			if (m_selectedMainPOS != null && sandboxMSA.MsaType == MsaType.kInfl)
			{
				// This fixes LT-4677, LT-6048, and LT-6201.
				ResetSlotCombo();
			}
			MSAType = sandboxMSA.MsaType;
		}

		#endregion Construction, initialization, and disposal

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MSAGroupBox));
			this.m_groupBox = new System.Windows.Forms.GroupBox();
			this.m_flowLayout = new System.Windows.Forms.FlowLayoutPanel();
			this.m_afxTypePanel = new System.Windows.Forms.Panel();
			this.m_mainCatPanel = new System.Windows.Forms.Panel();
			this.m_slotsPanel = new System.Windows.Forms.Panel();
			this.m_lAfxType = new System.Windows.Forms.Label();
			this.m_fwcbAffixTypes = new SIL.FieldWorks.Common.Widgets.FwComboBox();
			this.m_lMainCat = new System.Windows.Forms.Label();
			this.m_tcMainPOS = new SIL.FieldWorks.Common.Widgets.TreeCombo();
			this.m_lSLots = new System.Windows.Forms.Label();
			this.m_fwcbSlots = new SIL.FieldWorks.Common.Widgets.FwComboBox();
			this.m_tcSecondaryPOS = new SIL.FieldWorks.Common.Widgets.TreeCombo();
			this.m_groupBox.SuspendLayout();
			this.m_flowLayout.SuspendLayout();
			this.m_afxTypePanel.SuspendLayout();
			this.m_mainCatPanel.SuspendLayout();
			this.m_slotsPanel.SuspendLayout();
			this.SuspendLayout();
			//
			// m_lAfxType
			//
			resources.ApplyResources(this.m_lAfxType, "m_lAfxType");
			this.m_lAfxType.Name = "m_lAfxType";
			this.m_lAfxType.Location = new System.Drawing.Point(0, 0);
			this.m_lAfxType.Size = new System.Drawing.Size(170, 23);
			//
			// m_fwcbAffixTypes
			//
			this.m_fwcbAffixTypes.AdjustStringHeight = true;
			this.m_fwcbAffixTypes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_fwcbAffixTypes.DroppedDown = false;
			this.m_fwcbAffixTypes.Name = "m_fwcbAffixTypes";
			this.m_fwcbAffixTypes.PreviousTextBoxText = null;
			this.m_fwcbAffixTypes.SelectedIndex = -1;
			this.m_fwcbAffixTypes.SelectedItem = null;
			this.m_fwcbAffixTypes.StyleSheet = null;
			this.m_fwcbAffixTypes.Location = new System.Drawing.Point(0, 23);
			this.m_fwcbAffixTypes.Size = new System.Drawing.Size(170, 21);
			//
			// m_lMainCat
			//
			resources.ApplyResources(this.m_lMainCat, "m_lMainCat");
			this.m_lMainCat.Name = "m_lMainCat";
			this.m_lMainCat.Location = new System.Drawing.Point(0, 0);
			this.m_lMainCat.Size = new System.Drawing.Size(170, 23);
			//
			// m_tcMainPOS
			//
			this.m_tcMainPOS.AdjustStringHeight = true;
			// Setting width to match the default width used by popuptree
			this.m_tcMainPOS.DropDownWidth = 300;
			this.m_tcMainPOS.DroppedDown = false;
			this.m_tcMainPOS.Name = "m_tcMainPOS";
			this.m_tcMainPOS.SelectedNode = null;
			this.m_tcMainPOS.StyleSheet = null;
			this.m_tcMainPOS.Location = new System.Drawing.Point(0, 23);
			this.m_tcMainPOS.Size = new System.Drawing.Size(170, 21);
			//
			// m_lSLots
			//
			resources.ApplyResources(this.m_lSLots, "m_lSLots");
			this.m_lSLots.Name = "m_lSLots";
			this.m_lSLots.Location = new System.Drawing.Point(0, 0);
			this.m_lSLots.Size = new System.Drawing.Size(170, 23);
			//
			// m_fwcbSlots
			//
			this.m_fwcbSlots.AdjustStringHeight = true;
			this.m_fwcbSlots.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_fwcbSlots.DropDownWidth = 140;
			this.m_fwcbSlots.DroppedDown = false;
			this.m_fwcbSlots.Name = "m_fwcbSlots";
			this.m_fwcbSlots.PreviousTextBoxText = null;
			this.m_fwcbSlots.SelectedIndex = -1;
			this.m_fwcbSlots.SelectedItem = null;
			this.m_fwcbSlots.StyleSheet = null;
			this.m_fwcbSlots.Location = new System.Drawing.Point(0, 23);
			this.m_fwcbSlots.Size = new System.Drawing.Size(170, 21);
			//
			// m_tcSecondaryPOS
			//
			this.m_tcSecondaryPOS.AdjustStringHeight = true;
			this.m_tcSecondaryPOS.DropDownWidth = 140;
			this.m_tcSecondaryPOS.DroppedDown = false;
			this.m_tcSecondaryPOS.Name = "m_tcSecondaryPOS";
			this.m_tcSecondaryPOS.SelectedNode = null;
			this.m_tcSecondaryPOS.StyleSheet = null;
			this.m_tcSecondaryPOS.Location = new System.Drawing.Point(0, 23);
			this.m_tcSecondaryPOS.Size = new System.Drawing.Size(170, 21);
			//
			// m_afxTypePanel
			//
			this.m_afxTypePanel.Controls.Add(this.m_fwcbAffixTypes);
			this.m_afxTypePanel.Controls.Add(this.m_lAfxType);
			this.m_afxTypePanel.Size = new System.Drawing.Size(170, 48);
			this.m_afxTypePanel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.m_afxTypePanel.Name = "m_afxTypePanel";
			//
			// m_mainCatPanel
			//
			this.m_mainCatPanel.Controls.Add(this.m_tcMainPOS);
			this.m_mainCatPanel.Controls.Add(this.m_lMainCat);
			this.m_mainCatPanel.Size = new System.Drawing.Size(170, 48);
			this.m_mainCatPanel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.m_mainCatPanel.Name = "m_mainCatPanel";
			//
			// m_slotsPanel (holds both m_fwcbSlots and m_tcSecondaryPOS, only one visible at a time)
			//
			this.m_slotsPanel.Controls.Add(this.m_fwcbSlots);
			this.m_slotsPanel.Controls.Add(this.m_tcSecondaryPOS);
			this.m_slotsPanel.Controls.Add(this.m_lSLots);
			this.m_slotsPanel.Size = new System.Drawing.Size(170, 48);
			this.m_slotsPanel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.m_slotsPanel.Name = "m_slotsPanel";
			//
			// m_flowLayout
			//
			this.m_flowLayout.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_flowLayout.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
			this.m_flowLayout.WrapContents = false;
			this.m_flowLayout.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.m_flowLayout.Name = "m_flowLayout";
			this.m_flowLayout.Controls.Add(this.m_afxTypePanel);
			this.m_flowLayout.Controls.Add(this.m_mainCatPanel);
			this.m_flowLayout.Controls.Add(this.m_slotsPanel);
			//
			// m_groupBox
			//
			this.m_groupBox.Controls.Add(this.m_flowLayout);
			resources.ApplyResources(this.m_groupBox, "m_groupBox");
			this.m_groupBox.Name = "m_groupBox";
			this.m_groupBox.TabStop = false;
			this.m_groupBox.Dock = System.Windows.Forms.DockStyle.Fill;
			//
			// MSAGroupBox
			//
			this.Controls.Add(this.m_groupBox);
			this.Name = "MSAGroupBox";
			resources.ApplyResources(this, "$this");
			this.m_slotsPanel.ResumeLayout(false);
			this.m_mainCatPanel.ResumeLayout(false);
			this.m_afxTypePanel.ResumeLayout(false);
			this.m_flowLayout.ResumeLayout(false);
			this.m_groupBox.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		#region Other methods

		/// <summary>
		/// Reset the slot combo box.
		/// </summary>
		private void ResetSlotCombo()
		{
			m_fwcbSlots.SuspendLayout();
			m_fwcbSlots.Items.Clear();
			int matchIdx = -1;
			if (m_selectedMainPOS != null)
			{
				// Cache items to add, which prevents prop changed being called for each add. (Fixes FWR-3083)
				List<HvoTssComboItem> itemsToAdd = new List<HvoTssComboItem>();
				foreach (var slot in GetSlots())
				{
					string name = slot.Name.BestAnalysisAlternative.Text;
					if (name != null && name.Length > 0) // Don't add empty strings.
					{
						HvoTssComboItem newItem = new HvoTssComboItem(slot.Hvo,
							TsStringUtils.MakeString(name, m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle));
						itemsToAdd.Add(newItem);
						if (m_selectedSlot != null && m_selectedSlot.Hvo == newItem.Hvo)
							matchIdx = itemsToAdd.Count - 1;
					}
				}
				m_fwcbSlots.Items.AddRange(itemsToAdd.ToArray());
			}
			if (matchIdx == -1)
			{
				m_fwcbSlots.SelectedIndex = -1;
				m_selectedSlot = null; // if the current proposed slot isn't possible for the POS, forget it.
			}
			else
			{
				try
				{
					m_skipEvents = true;
					m_fwcbSlots.SelectedIndex = matchIdx;
				}
				finally
				{
					m_skipEvents = false;
				}
			}
			m_fwcbSlots.Enabled = m_fwcbSlots.Items.Count > 0;
			m_lSLots.Enabled = m_fwcbSlots.Enabled;
			m_fwcbSlots.ResumeLayout();
		}

		private IEnumerable<IMoInflAffixSlot> GetSlots()
		{
			if (m_morphType == null)
			{
				// Not called by InsertEntryDlg; need to figure out the morphtype(s)
				var obj = m_propertyTable.GetValue<ICmObject>("ActiveClerkSelectedObject");
				if (obj is ILexEntry lex)
				{
					return DomainObjectServices.GetSlots(m_cache, lex, m_selectedMainPOS);
				}

					return m_selectedMainPOS.AllAffixSlots;
			}

			// Called by InsertEntryDlg so we know the morphtype
				bool fIsPrefixal = MorphServices.IsPrefixishType(m_cache, m_morphType.Hvo);
				bool fIsSuffixal = MorphServices.IsSuffixishType(m_cache, m_morphType.Hvo);
				if (fIsPrefixal && fIsSuffixal)
			{
					return m_selectedMainPOS.AllAffixSlots;
			}

					return DomainObjectServices.GetSomeSlots(m_cache, m_selectedMainPOS.AllAffixSlots, fIsPrefixal);
			}

		public void AdjustInternalControlsAndGrow()
		{
			// All combos/trees are in the same visual row within their panels.
			// Find the max height increase needed and apply it uniformly.
			int maxDelta = 0;

			int delta = m_fwcbAffixTypes.PreferredHeight - m_fwcbAffixTypes.Height;
			if (delta > 0)
			{
				m_fwcbAffixTypes.Height = m_fwcbAffixTypes.PreferredHeight;
				maxDelta = Math.Max(maxDelta, delta);
			}

			delta = m_tcMainPOS.PreferredHeight - m_tcMainPOS.Height;
			if (delta > 0)
			{
				m_tcMainPOS.Height = m_tcMainPOS.PreferredHeight;
				maxDelta = Math.Max(maxDelta, delta);
			}

			int delta1 = m_fwcbSlots.PreferredHeight - m_fwcbSlots.Height;
			int delta2 = m_tcSecondaryPOS.PreferredHeight - m_tcSecondaryPOS.Height;
			if (delta1 > 0)
				m_fwcbSlots.Height = m_fwcbSlots.PreferredHeight;
			if (delta2 > 0)
				m_tcSecondaryPOS.Height = m_tcSecondaryPOS.PreferredHeight;
			maxDelta = Math.Max(maxDelta, Math.Max(delta1, delta2));

			if (maxDelta > 0)
			{
				// Grow all panels and the overall control to accommodate taller combos.
				m_afxTypePanel.Height += maxDelta;
				m_mainCatPanel.Height += maxDelta;
				m_slotsPanel.Height += maxDelta;
				this.Height += maxDelta;
			}
		}
		#endregion Other methods

		#region Event Handlers

		#region MSA Types combo box

		// Handles a change in the item selected in the MSA Types combo box.
		void HandleComboMSATypesChange(object sender, EventArgs ea)
		{
			if (m_skipEvents)
				return;
			FwComboBox combo = sender as FwComboBox;
			ITsString selTss = combo.SelectedItem as ITsString;
			string label = selTss.Text;
			if (label == LexTextControls.ksNotSure)
				MSAType = MsaType.kUnclassified;
			else if (label == LexTextControls.ksInflectional)
				MSAType = MsaType.kInfl;
			else if (label == LexTextControls.ksDerivational)
				MSAType = MsaType.kDeriv;
			Debug.WriteLine(label);
		}

		#endregion MSA Types combo box

		#region Affix slots combo box
		// Handles a change in the item selected in the affix slot combo box.
		void HandleComboSlotChange(object sender, EventArgs ea)
		{
			if (m_skipEvents)
				return;

			FwComboBox combo = sender as FwComboBox;
			HvoTssComboItem selItem = combo.SelectedItem as HvoTssComboItem;
			m_selectedSlot = (selItem == null) ? null : m_cache.ServiceLocator.GetInstance<IMoInflAffixSlotRepository>().GetObject(selItem.Hvo);
		}
		#endregion Affix slots combo box

		#region TreeCombo handing

		private void m_mainPOSPopupTreeManager_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			m_selectedMainPOS = null;
			var repo = m_cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>();
			if (e.Node is HvoTreeNode)
				repo.TryGetObject((e.Node as HvoTreeNode).Hvo, out m_selectedMainPOS);

			// If this is an inflectional affix MSA,
			// then populate slot list (FwComboBox m_fwcbSlots).
			if (MSAType == MsaType.kInfl)
				ResetSlotCombo();
			if (m_tcMainPOS.Text != e.Node.Text)
				m_tcMainPOS.Text = e.Node.Text;
		}

		private void m_secPOSPopupTreeManager_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			m_selectedSecondaryPOS = null;
			var repo = m_cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>();
			if (e.Node is HvoTreeNode)
				repo.TryGetObject((e.Node as HvoTreeNode).Hvo, out m_selectedSecondaryPOS);
		}

		#endregion TreeCombo handing

		#endregion Event Handlers
	}
}
