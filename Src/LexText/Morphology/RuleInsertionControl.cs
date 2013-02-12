using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Xml;
using System.Linq;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// This class represents a rule insertion control. A rule insertion control
	/// consists of a set of hotlinks that are used to insert various rule items.
	/// Rule formula controls provide information about which type of items it is
	/// interested in inserting and in what context the hotlinks should be displayed.
	/// It provides an <c>Insert</c> event that indicates when a user has attempted
	/// to insert an item.
	/// </summary>
	public class RuleInsertionControl : UserControl, IFWDisposable
	{
		public event EventHandler<RuleInsertEventArgs> Insert;
		public delegate bool DisplayOption(RuleInsertType type);
		public delegate int[] DisplayIndices();
		public delegate string DisplayNoOptionsMessage();

		static string GetOptionString(RuleInsertType type)
		{
			switch (type)
			{
				case RuleInsertType.MORPHEME_BOUNDARY:
					return MEStrings.ksRuleMorphBdryOpt;

				case RuleInsertType.NATURAL_CLASS:
					return MEStrings.ksRuleNCOpt;

				case RuleInsertType.PHONEME:
					return MEStrings.ksRulePhonemeOpt;

				case RuleInsertType.WORD_BOUNDARY:
					return MEStrings.ksRuleWordBdryOpt;

				case RuleInsertType.FEATURES:
					return MEStrings.ksRuleFeaturesOpt;

				case RuleInsertType.VARIABLE:
					return MEStrings.ksRuleVarOpt;

				case RuleInsertType.INDEX:
					return MEStrings.ksRuleIndexOpt;

				case RuleInsertType.COLUMN:
					return MEStrings.ksRuleColOpt;
			}

			return null;
		}

		struct InsertOption
		{
			public InsertOption(RuleInsertType type, DisplayOption shouldDisplay, DisplayIndices displayIndices)
			{
				this.type = type;
				this.shouldDisplay = shouldDisplay;
				this.displayIndices = displayIndices;
			}

			public RuleInsertType type;
			public DisplayOption shouldDisplay;
			public DisplayIndices displayIndices;
		}

		class GrowLabel : Label
		{
			bool m_growing = false;
			public GrowLabel()
			{
				this.AutoSize = false;
			}
			void resizeLabel()
			{
				if (m_growing) return;
				try
				{
					m_growing = true;
					Size sz = new Size(this.Width, Int32.MaxValue);
					sz = TextRenderer.MeasureText(this.Text, this.Font, sz, TextFormatFlags.WordBreak);
					// The mono implementation chops off the bottom line of the display (FWNX-752).
					if (MiscUtils.IsMono)
						this.Height = sz.Height + 7;
					else
						this.Height = sz.Height;
				}
				finally
				{
					m_growing = false;
				}
			}
			protected override void OnTextChanged(EventArgs e)
			{
				base.OnTextChanged(e);
				resizeLabel();
			}
			protected override void OnFontChanged(EventArgs e)
			{
				base.OnFontChanged(e);
				resizeLabel();
			}
			protected override void OnSizeChanged(EventArgs e)
			{
				base.OnSizeChanged(e);
				resizeLabel();
			}
		}

		private Panel m_labelPanel;
		private FlowLayoutPanel m_insertPanel;
		private Label m_insertLabel;

		FdoCache m_cache = null;
		XCore.Mediator m_mediator = null;
		IPersistenceProvider m_persistenceProvider = null;
		List<InsertOption> m_options;
		string m_ruleName = null;
		DisplayNoOptionsMessage m_noOptsMsg = null;
		int m_prevWidth = 0;
		Label m_msgLabel = null;

		public RuleInsertionControl()
		{
			m_options = new List<InsertOption>();
			InitializeComponent();
		}

		/// <summary>
		/// Gets or sets the no options message delegate. This is called to retrieve the appropriate no options
		/// message.
		/// </summary>
		/// <value>The no options message delegate.</value>
		public DisplayNoOptionsMessage NoOptionsMessage
		{
			get
			{
				CheckDisposed();
				return m_noOptsMsg;
			}

			set
			{
				CheckDisposed();
				m_noOptsMsg = value;
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
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
			}

			m_cache = null;
			m_mediator = null;
			m_persistenceProvider = null;
			m_options = null;
			m_ruleName = null;

			base.Dispose(disposing);
		}

		public void Initialize(FdoCache cache, XCore.Mediator mediator, IPersistenceProvider persistenceProvider, string ruleName)
		{
			CheckDisposed();

			m_cache = cache;
			m_mediator = mediator;
			m_persistenceProvider = persistenceProvider;
			m_ruleName = ruleName;
		}

		/// <summary>
		/// Adds an insertion option. A predicate can be provided to determine in what contexts
		/// this insertion option can be displayed.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="shouldDisplay">The should display predicate.</param>
		public void AddOption(RuleInsertType type, DisplayOption shouldDisplay)
		{
			CheckDisposed();

			m_options.Add(new InsertOption(type, shouldDisplay, null));
		}

		/// <summary>
		/// Adds an index option. A predicate can be provided to determine what indices to display.
		/// </summary>
		/// <param name="shouldDisplay">The should display predicate.</param>
		/// <param name="displayIndices">The display indices predicate.</param>
		public void AddIndexOption(DisplayOption shouldDisplay, DisplayIndices displayIndices)
		{
			CheckDisposed();

			m_options.Add(new InsertOption(RuleInsertType.INDEX, shouldDisplay, displayIndices));
		}

		/// <summary>
		/// Updates the options display.
		/// </summary>
		public void UpdateOptionsDisplay()
		{
			CheckDisposed();

			m_insertPanel.SuspendLayout();
			SuspendLayout();
			m_insertPanel.Controls.Clear();
			Font f = new Font(MiscUtils.StandardSansSerif, 10);
			bool displayingOpts = false;
			foreach (InsertOption opt in m_options)
			{
				if (opt.shouldDisplay == null || opt.shouldDisplay(opt.type))
				{
					LinkLabel linkLabel = new LinkLabel();
					linkLabel.AutoSize = true;
					linkLabel.Font = f;
					linkLabel.TabStop = true;
					linkLabel.VisitedLinkColor = Color.Blue;
					linkLabel.LinkClicked += new LinkLabelLinkClickedEventHandler(link_LinkClicked);
					if (opt.type == RuleInsertType.INDEX)
					{
						int[] indices = opt.displayIndices();
						StringBuilder sb = new StringBuilder();
						for (int i = 0; i < indices.Length; i++)
						{
							sb.Append(indices[i]);
							if (i < indices.Length - 1)
								sb.Append(" ");
						}
						linkLabel.Text = sb.ToString();

						linkLabel.Links.Clear();
						int start = 0;
						foreach (int index in indices)
						{
							int len = Convert.ToString(index).Length;
							LinkLabel.Link link = linkLabel.Links.Add(start, len, RuleInsertType.INDEX);
							// use the tag property to store the index for this link
							link.Tag = index;
							start += len + 1;
						}
					}
					else
					{
						linkLabel.Text = GetOptionString(opt.type);
						linkLabel.Links[0].LinkData = opt.type;
					}

					m_insertPanel.Controls.Add(linkLabel);
					displayingOpts = true;
				}
			}

			if (!displayingOpts && m_noOptsMsg != null)
			{
				string text = m_noOptsMsg();
				if (text != null)
				{
					m_msgLabel = new GrowLabel();
					m_msgLabel.Font = f;
					m_msgLabel.Text = text;
					m_msgLabel.Width = m_insertPanel.ClientSize.Width;
					m_insertPanel.Controls.Add(m_msgLabel);
				}
			}
			else if (m_msgLabel != null)
			{
				m_msgLabel = null;
			}

			m_insertPanel.ResumeLayout(false);
			m_insertPanel.PerformLayout();
			ResumeLayout(false);

			Height = m_insertPanel.PreferredSize.Height;
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			if (m_prevWidth != Width)
			{
				if (m_msgLabel != null)
					m_msgLabel.Width = m_insertPanel.ClientSize.Width;
				Height = m_insertPanel.PreferredSize.Height;
				m_prevWidth = Width;
			}
			base.OnSizeChanged(e);
		}

		void link_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			RuleInsertType type = (RuleInsertType)e.Link.LinkData;

			string optStr = GetOptionString(type);
			var undo = string.Format(MEStrings.ksRuleUndoInsert, optStr);
			var redo = string.Format(MEStrings.ksRuleRedoInsert, optStr);

			object data = null;
			switch (type)
			{
				case RuleInsertType.PHONEME:
					var phonemes = m_cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0].PhonemesOC.Cast<ICmObject>();
					data = DisplayChooser(MEStrings.ksRulePhonemeOpt, MEStrings.ksRulePhonemeChooserLink,
						"phonemeEdit", "RulePhonemeFlatList", phonemes);
					break;

				case RuleInsertType.NATURAL_CLASS:
					var natClasses = m_cache.LangProject.PhonologicalDataOA.NaturalClassesOS.Cast<ICmObject>();
					data = DisplayChooser(MEStrings.ksRuleNCOpt, MEStrings.ksRuleNCChooserLink,
						"naturalClassedit", "RuleNaturalClassFlatList", natClasses);
					break;

				case RuleInsertType.FEATURES:
					using (var featChooser = new PhonologicalFeatureChooserDlg())
					{
						RegRuleFormulaControl ruleControl = null;
						IPhNCFeatures natClass = null;
						if (Parent is RegRuleFormulaControl)
						{
							featChooser.ShowFeatureConstraintValues = true;
							ruleControl = Parent as RegRuleFormulaControl;
							IPhSimpleContextNC ctxt = null;
							UndoableUnitOfWorkHelper.Do(undo, redo, m_cache.ActionHandlerAccessor, () =>
							{
								// create an empty natural class assuming user will fill it
								natClass = CreatePhNcFeaturesForAContext();
								// Now we need to insert the (empty) natural class into a context
								// and remember that we did it (via a non-null data variable)
								data = natClass;
								Insert(this, new RuleInsertEventArgs(type, data, undo, redo));
								// Now we can use the context
								ctxt = ruleControl.CurrentContext as IPhSimpleContextNC;
								ctxt.FeatureStructureRA = natClass;
							});
							var rule = ruleControl.RHS;
							featChooser.SetDlgInfo(m_cache, m_mediator, natClass.FeaturesOA, rule.OwningRule, ctxt);
							featChooser.Title = MEStrings.ksRuleFeatsChooserTitle;
							featChooser.SetHelpTopic("khtpChoose-Grammar-PhonFeats-RegRuleFormulaControl");
						}
						else if (Parent is MetaRuleFormulaControl)
						{
							featChooser.SetDlgInfo(m_cache, m_mediator);
							featChooser.SetHelpTopic("khtpChoose-Grammar-PhonFeats-MetaRuleFormulaControl");
						}
						else if (Parent is AffixRuleFormulaControl)
						{
							featChooser.SetDlgInfo(m_cache, m_mediator);
							featChooser.SetHelpTopic("khtpChoose-LexiconEdit-PhonFeats-AffixRuleFormulaControl");
						}
						DialogResult res = featChooser.ShowDialog();
						if (res == DialogResult.OK)
						{
							UndoableUnitOfWorkHelper.Do(undo, redo, m_cache.ActionHandlerAccessor, () =>
							{
								if (Parent is RegRuleFormulaControl)
								{
									if (data != null)
									{   // we had to create a dummy natural class to create the context
										// so we do not need to insert anything but we do need to redraw
										data = null;
										if (ruleControl != null)
											ruleControl.RootSite.RootBox.Reconstruct();
									}
									else
										data = featChooser.NaturalClassFeatures;
								}
								else
								{
									var featNC = m_cache.ServiceLocator.GetInstance<IPhNCFeaturesFactory>().Create();
									m_cache.LangProject.PhonologicalDataOA.NaturalClassesOS.Add(featNC);
									featNC.Name.SetUserWritingSystem(string.Format(MEStrings.ksRuleNCFeatsName, m_ruleName));
									featNC.FeaturesOA = m_cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
									featChooser.FS = featNC.FeaturesOA;
									featChooser.UpdateFeatureStructure();
									data = featNC;
								}
							});
						}
						else
						{
							if (data != null)
							{
								// we had to create a temporary natural class;  it is no longer needed
								UndoableUnitOfWorkHelper.Do(undo, redo, m_cache.ActionHandlerAccessor, () =>
								{
									var phonData = natClass.Owner as IPhPhonData;
									phonData.NaturalClassesOS.Remove(natClass);
									data = null; // no insertion is needed
								});
							}
							if (res != DialogResult.Cancel)
								featChooser.HandleJump();
						}
					}
					break;

				case RuleInsertType.WORD_BOUNDARY:
					data = m_cache.ServiceLocator.GetInstance<IPhBdryMarkerRepository>().GetObject(LangProjectTags.kguidPhRuleWordBdry);
					break;

				case RuleInsertType.MORPHEME_BOUNDARY:
					data = m_cache.ServiceLocator.GetInstance<IPhBdryMarkerRepository>().GetObject(LangProjectTags.kguidPhRuleMorphBdry);
					break;

				case RuleInsertType.INDEX:
					// put the clicked index in the data field
					data = (int)e.Link.Tag;
					break;
			}

			// Some cases will do nothing (and so make an empty UOW) if data is empty. However, other cases
			// (such as inserting X Variable, LT-11136) need the event even if data is empty, and the empty UOW
			// is discarded harmlessly.
			Insert(this, new RuleInsertEventArgs(type, data, undo, redo));
		}

		private IPhNCFeatures CreatePhNcFeaturesForAContext()
		{
			IPhNCFeatures natClass;
			natClass = m_cache.ServiceLocator.GetInstance<IPhNCFeaturesFactory>().Create();
			m_cache.LangProject.PhonologicalDataOA.NaturalClassesOS.Add(natClass);
			natClass.Name.SetUserWritingSystem(string.Format(MEStrings.ksRuleNCFeatsName, m_ruleName));
			natClass.FeaturesOA = m_cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			return natClass;
		}

		internal ICmObject DisplayChooser(string fieldName, string linkText, string toolName, string guiControl,
			IEnumerable<ICmObject> candidates)
		{
			ICmObject obj = null;

			var labels = ObjectLabel.CreateObjectLabels(m_cache, candidates);

			using (var chooser = new SimpleListChooser(m_persistenceProvider, labels,
				fieldName, m_mediator.HelpTopicProvider))
			{
				chooser.Cache = m_cache;
				chooser.TextParamHvo = m_cache.LangProject.PhonologicalDataOA.Hvo;
				Guid guidTextParam = m_cache.LangProject.PhonologicalDataOA.Guid;
				chooser.AddLink(linkText, SimpleListChooser.LinkType.kGotoLink,
					new FwLinkArgs(toolName, guidTextParam));
				chooser.ReplaceTreeView(m_mediator, guiControl);
				if (this.Parent is SIL.FieldWorks.XWorks.MorphologyEditor.RegRuleFormulaControl)
					chooser.SetHelpTopic("khtpChoose-Grammar-PhonFeats-RegRuleFormulaControl");
				else if (this.Parent is SIL.FieldWorks.XWorks.MorphologyEditor.MetaRuleFormulaControl)
					chooser.SetHelpTopic("khtpChoose-Grammar-PhonFeats-MetaRuleFormulaControl");
				else if (this.Parent is SIL.FieldWorks.XWorks.MorphologyEditor.AffixRuleFormulaControl)
					chooser.SetHelpTopic("khtpChoose-LexiconEdit-PhonFeats-AffixRuleFormulaControl");

				DialogResult res = chooser.ShowDialog();
				if (res != DialogResult.Cancel)
				{
					chooser.HandleAnyJump();

					if (chooser.ChosenOne != null)
						obj = chooser.ChosenOne.Object;
				}
			}

			return obj;
		}

		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RuleInsertionControl));
			this.m_labelPanel = new System.Windows.Forms.Panel();
			this.m_insertLabel = new System.Windows.Forms.Label();
			this.m_insertPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.m_labelPanel.SuspendLayout();
			this.SuspendLayout();
			//
			// m_labelPanel
			//
			this.m_labelPanel.Controls.Add(this.m_insertLabel);
			resources.ApplyResources(this.m_labelPanel, "m_labelPanel");
			this.m_labelPanel.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
			this.m_labelPanel.Name = "m_labelPanel";
			//
			// m_insertLabel
			//
			resources.ApplyResources(this.m_insertLabel, "m_insertLabel");
			this.m_insertLabel.Name = "m_insertLabel";
			//
			// m_insertPanel
			//
			resources.ApplyResources(this.m_insertPanel, "m_insertPanel");
			this.m_insertPanel.Name = "m_insertPanel";
			//
			// RuleInsertionControl
			//
			this.Controls.Add(this.m_insertPanel);
			this.Controls.Add(this.m_labelPanel);
			this.Name = "RuleInsertionControl";
			resources.ApplyResources(this, "$this");
			this.m_labelPanel.ResumeLayout(false);
			this.m_labelPanel.PerformLayout();
			this.ResumeLayout(false);

		}
	}

	/// <summary>
	/// The enumeration of rule insertion types.
	/// </summary>
	public enum RuleInsertType { PHONEME, NATURAL_CLASS, WORD_BOUNDARY, MORPHEME_BOUNDARY, FEATURES, VARIABLE, INDEX, COLUMN };

	public class RuleInsertEventArgs : EventArgs
	{
		public RuleInsertEventArgs(RuleInsertType type, object data, string undoMsg, string redoMsg)
		{
			Type = type;
			Data = data;
			UndoMsg = undoMsg;
			RedoMsg = redoMsg;
		}

		public RuleInsertType Type { get; private set; }

		public object Data { get; private set; }

		public string UndoMsg { get; private set; }

		public string RedoMsg { get; private set; }
	}
}
