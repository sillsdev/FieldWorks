// This really needs to be refactored with MasterCategoryListDlg.cs
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for MsaInflectionFeatureListDlg.
	/// </summary>
	public class MsaInflectionFeatureListDlg : Form, IFWDisposable
	{
		private Mediator m_mediator;
		private IPropertyTable m_propertyTable;
		protected FdoCache m_cache;
		// The dialog can be initialized with an existing feature structure,
		// or just with an owning object and flid in which to create one.
		private IFsFeatStruc m_fs;
		// Where to put a new feature structure if needed. Owning flid may be atomic
		// or collection. Used only if m_fs is initially null.
		int m_hvoOwner;
		int m_owningFlid;
		private IPartOfSpeech m_highestPOS;
		private Dictionary<int, IPartOfSpeech> m_poses = new Dictionary<int, IPartOfSpeech>();
		private Button m_btnOK;
		private Button m_btnCancel;
		private Button m_bnHelp;
		private PictureBox pictureBox1;
		protected LinkLabel linkLabel1;
		private ImageList m_imageList;
		private ImageList m_imageListPictures;
		protected FeatureStructureTreeView m_tvMsaFeatureList;
		protected Label labelPrompt;
		private System.ComponentModel.IContainer components;

		private const string m_helpTopic = "khtpChoose-lexiconEdit-InflFeats";
		private HelpProvider helpProvider;

		public MsaInflectionFeatureListDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;
			pictureBox1.Image = m_imageListPictures.Images[0];
		}

		#region OnLoad
		/// <summary>
		/// Overridden to defeat the standard .NET behavior of adjusting size by
		/// screen resolution. That is bad for this dialog because we remember the size,
		/// and if we remember the enlarged size, it just keeps growing.
		/// If we defeat it, it may look a bit small the first time at high resolution,
		/// but at least it will stay the size the user sets.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLoad(EventArgs e)
		{
			Size size = this.Size;
			base.OnLoad (e);
			if (this.Size != size)
				this.Size = size;
		}
		#endregion

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
				if (components != null)
				{
					components.Dispose();
				}

				if (helpProvider != null)
					helpProvider.Dispose();
			}
			m_cache = null;
			m_fs = null;
			m_highestPOS = null;
			m_poses = null;
			m_mediator = null;
			m_cache = null;
			helpProvider = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// Init the dialog with an existing FS. Warning: the fs passed in
		/// might get deleted if it proves to be a duplicate. Retrieve the new FS after running it.
		/// This constructor is used in MsaInflectionFeatureListDlgLauncher.HandleChooser.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// <param name="fs"></param>
		/// <param name="owningFlid"></param>
		public void SetDlgInfo(FdoCache cache, Mediator mediator, IPropertyTable propertyTable, IFsFeatStruc fs, int owningFlid)
		{
			CheckDisposed();

			m_fs = fs;
			m_propertyTable = propertyTable;
			Mediator = mediator;
			m_cache = cache;
			m_owningFlid = owningFlid;
			LoadInflFeats(fs);
			EnableLink();
		}

		/// <summary>
		/// Init the dialog with an MSA and flid that does not yet contain a feature structure.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// <param name="cobj"></param>
		/// <param name="owningFlid"></param>
		public void SetDlgInfo(FdoCache cache, Mediator mediator, IPropertyTable propertyTable, ICmObject cobj, int owningFlid)
		{
			CheckDisposed();

			m_fs = null;
			m_owningFlid = owningFlid;
			m_hvoOwner = cobj.Hvo;
			m_propertyTable = propertyTable;
			Mediator = mediator;
			m_cache = cache;
			LoadInflFeats(cobj, owningFlid);
			EnableLink();
		}

		/// <summary>
		/// Init the dialog with a POS.
		/// If a new feature structure is created, it will currently be in the ReferenceForms of the POS.
		/// Eventually we want to make a new field for this purpose. (This is used by bulk edit
		/// to store previously used feature structures.)
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// <param name="pos"></param>
		public void SetDlgInfo(FdoCache cache, Mediator mediator, IPropertyTable propertyTable, IPartOfSpeech pos)
		{
			SetDlgInfo(cache, mediator, propertyTable, pos, PartOfSpeechTags.kflidReferenceForms);
		}

		protected virtual void EnableLink()
		{
			linkLabel1.Enabled = m_highestPOS != null;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="helpProvider gets disposed in Dispose()")]
		private Mediator Mediator
		{
			set
			{
				m_mediator = value;
				if (m_mediator != null)
				{
					// Reset window location.
					// Get location to the stored values, if any.
					Point dlgLocation;
					Size dlgSize;
					if (m_propertyTable.TryGetValue("msaInflFeatListDlgLocation", out dlgLocation)
						&& m_propertyTable.TryGetValue("msaInflFeatListDlgSize", out dlgSize))
					{
						var rect = new Rectangle(dlgLocation, dlgSize);
						ScreenUtils.EnsureVisibleRect(ref rect);
						DesktopBounds = rect;
						StartPosition = FormStartPosition.Manual;
					}
					var helpTopicProvider = m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider");
					if (helpTopicProvider != null) // Will be null when running tests
					{
						helpProvider = new HelpProvider { HelpNamespace = helpTopicProvider.HelpFile };
						helpProvider.SetHelpKeyword(this, helpTopicProvider.GetHelpString(m_helpTopic));
						helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
					}
				}
			}
		}

		/// <summary>
		/// Load the tree items if the starting point is a feature structure.
		/// </summary>
		/// <param name="fs"></param>
		protected virtual void LoadInflFeats(IFsFeatStruc fs)
		{
			var cobj = fs.Owner;
			switch(cobj.ClassID)
			{
			case MoAffixAllomorphTags.kClassId:
				PopulateTreeFromPosInEntry(cobj);
				break;
			default:
				// load inflectable features of this POS and any inflectable features of its parent POS
				var pos = GetOwningPOSOfFS(fs, cobj);
				PopulateTreeFromPos(pos);
				break;
			}
			m_tvMsaFeatureList.PopulateTreeFromFeatureStructure(fs);
			FinishLoading();
		}

		/// <summary>
		/// Load the tree items if the starting point is an owning MSA and flid.
		/// </summary>
		/// <param name="cobj"></param>
		/// <param name="owningFlid"></param>
		protected virtual void LoadInflFeats(ICmObject cobj, int owningFlid)
		{
			switch(cobj.ClassID)
			{
				case MoAffixAllomorphTags.kClassId:
					PopulateTreeFromPosInEntry(cobj);
					break;
				case PartOfSpeechTags.kClassId:
					PopulateTreeFromPos((IPartOfSpeech)cobj);
					break;
				default:
					PopulateTreeFromPos(GetPosFromCmObjectAndFlid(cobj, owningFlid));
					break;
			}
			FinishLoading();
		}

		private void PopulateTreeFromPosInEntry(ICmObject cobj)
		{
			var entry = cobj.Owner as ILexEntry;
			if (entry == null)
				return;
			foreach (var msa in entry.MorphoSyntaxAnalysesOC)
			{
				var pos = GetPosFromCmObjectAndFlid(msa, MoDerivAffMsaTags.kflidFromMsFeatures);
				PopulateTreeFromPos(pos);
			}
		}

		/// <summary>
		/// After populating the tree with items, expand them, sort them, and select one.
		/// </summary>
		protected void FinishLoading()
		{
			m_tvMsaFeatureList.ExpandAll();
			m_tvMsaFeatureList.Sort();
			if (m_tvMsaFeatureList.Nodes.Count > 0)
				m_tvMsaFeatureList.SelectedNode = m_tvMsaFeatureList.Nodes[0]; // have it show first one initially
		}

		private void PopulateTreeFromPos(IPartOfSpeech pos)
		{
			if (pos != null && !m_poses.ContainsKey(pos.Hvo))
			{
				m_poses.Add(pos.Hvo, pos);
			}
			m_highestPOS = pos;
			while (pos != null)
			{
				m_tvMsaFeatureList.PopulateTreeFromInflectableFeats(pos.InflectableFeatsRC);
				var cobj = pos.Owner;
				m_highestPOS = pos;
				pos = cobj as IPartOfSpeech;
			}
		}

		private IPartOfSpeech GetOwningPOSOfFS(IFsFeatStruc fs, ICmObject cobj)
		{
			return GetPosFromCmObjectAndFlid(cobj, fs.OwningFlid);
		}

		/// <summary>
		/// Given a (potentially) owning object, and the flid in which is does/will own
		/// the feature structure, find the relevant POS.
		/// </summary>
		/// <param name="cobj"></param>
		/// <param name="owningFlid"></param>
		/// <returns></returns>
		private IPartOfSpeech GetPosFromCmObjectAndFlid(ICmObject cobj, int owningFlid)
		{
			switch (cobj.ClassID)
			{
				case MoInflAffMsaTags.kClassId:
					var infl = (IMoInflAffMsa) cobj;
					return infl.PartOfSpeechRA;
				case MoDerivAffMsaTags.kClassId:
					var deriv = (IMoDerivAffMsa) cobj;
					if (owningFlid == MoDerivAffMsaTags.kflidFromMsFeatures)
						return deriv.FromPartOfSpeechRA;
					if (owningFlid == MoDerivAffMsaTags.kflidToMsFeatures)
						return deriv.ToPartOfSpeechRA;
					break;
				case MoStemMsaTags.kClassId:
					var stem = (IMoStemMsa) cobj;
					return stem.PartOfSpeechRA;
				case MoStemNameTags.kClassId:
					var sn = (IMoStemName) cobj;
					return sn.Owner as IPartOfSpeech;
				case MoAffixAllomorphTags.kClassId:
					// get entry of the allomorph and then get the msa of first sense and return its (from) POS
					var entry = cobj.Owner as ILexEntry;
					if (entry == null)
						return null;
					var sense = entry.SensesOS[0];
					if (sense == null)
						return null;
					var msa = sense.MorphoSyntaxAnalysisRA;
					return GetPosFromCmObjectAndFlid(msa, MoDerivAffMsaTags.kflidFromMsFeatures);
			}
			return null;
		}

		/// <summary>
		/// Get Feature Structure resulting from dialog operation
		/// </summary>
		public IFsFeatStruc FS
		{
			get
			{
				CheckDisposed();

				return m_fs;
			}
		}
		/// <summary>
		/// Get highest level POS of msa
		/// </summary>
		public IPartOfSpeech HighestPOS
		{
			get
			{
				CheckDisposed();

				return m_highestPOS;
			}
		}
		/// <summary>
		/// Get/Set prompt text
		/// </summary>
		public virtual string Prompt
		{
			get
			{
				CheckDisposed();

				return labelPrompt.Text;
			}
			set
			{
				CheckDisposed();

				string s1 = value ?? LexTextControls.ksFeaturesForX;
				string s2;
				if (m_poses.Count == 0)
					s2 = LexTextControls.ksUnknownCategory;
				else
				{
					var sb = new StringBuilder();
					Dictionary<int, IPartOfSpeech>.ValueCollection poses = m_poses.Values;
					bool fFirst = true;
					foreach (var pos in poses)
					{
						if (!fFirst)
							sb.Append(", ");
						sb.Append(pos.Name.BestAnalysisAlternative.Text);
						fFirst = false;
					}
					s2 = sb.ToString();
				}
				labelPrompt.Text = String.Format(s1, s2);
			}
		}
		/// <summary>
		/// Get/Set dialog title text
		/// </summary>
		public string Title
		{
			get
			{
				CheckDisposed();

				return Text;
			}
			set
			{
				CheckDisposed();

				Text = value;
			}
		}
		/// <summary>
		/// Get/Set link text
		/// </summary>
		public virtual string LinkText
		{
			get
			{
				CheckDisposed();

				return linkLabel1.Text;
			}
			set
			{
				CheckDisposed();

				string s1 = value ?? LexTextControls.ksAddFeaturesToX;
				string s2 = m_highestPOS == null ? LexTextControls.ksUnknownCategory : m_highestPOS.Name.AnalysisDefaultWritingSystem.Text;
				linkLabel1.Text = String.Format(s1, s2);
			}
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: LinkLabel.TabStop is missing from Mono")]
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MsaInflectionFeatureListDlg));
			this.labelPrompt = new System.Windows.Forms.Label();
			this.m_imageList = new System.Windows.Forms.ImageList(this.components);
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_bnHelp = new System.Windows.Forms.Button();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.linkLabel1 = new System.Windows.Forms.LinkLabel();
			this.m_imageListPictures = new System.Windows.Forms.ImageList(this.components);
			this.m_tvMsaFeatureList = new SIL.FieldWorks.LexText.Controls.FeatureStructureTreeView(this.components);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			//
			// labelPrompt
			//
			resources.ApplyResources(this.labelPrompt, "labelPrompt");
			this.labelPrompt.Name = "labelPrompt";
			//
			// m_imageList
			//
			this.m_imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_imageList.ImageStream")));
			this.m_imageList.TransparentColor = System.Drawing.Color.Transparent;
			this.m_imageList.Images.SetKeyName(0, "");
			this.m_imageList.Images.SetKeyName(1, "");
			this.m_imageList.Images.SetKeyName(2, "");
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnOK.Name = "m_btnOK";
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Name = "m_btnCancel";
			//
			// m_bnHelp
			//
			resources.ApplyResources(this.m_bnHelp, "m_bnHelp");
			this.m_bnHelp.Name = "m_bnHelp";
			this.m_bnHelp.Click += new System.EventHandler(this.m_bnHelp_Click);
			//
			// pictureBox1
			//
			resources.ApplyResources(this.pictureBox1, "pictureBox1");
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.TabStop = false;
			//
			// linkLabel1
			//
			resources.ApplyResources(this.linkLabel1, "linkLabel1");
			this.linkLabel1.Name = "linkLabel1";
			this.linkLabel1.TabStop = true;
			this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
			//
			// m_imageListPictures
			//
			this.m_imageListPictures.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_imageListPictures.ImageStream")));
			this.m_imageListPictures.TransparentColor = System.Drawing.Color.Magenta;
			this.m_imageListPictures.Images.SetKeyName(0, "");
			//
			// m_tvMsaFeatureList
			//
			resources.ApplyResources(this.m_tvMsaFeatureList, "m_tvMsaFeatureList");
			this.m_tvMsaFeatureList.FullRowSelect = true;
			this.m_tvMsaFeatureList.HideSelection = false;
			this.m_tvMsaFeatureList.Name = "m_tvMsaFeatureList";
			//
			// MsaInflectionFeatureListDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_btnCancel;
			this.Controls.Add(this.linkLabel1);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.m_bnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.labelPrompt);
			this.Controls.Add(this.m_tvMsaFeatureList);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MsaInflectionFeatureListDlg";
			this.ShowInTaskbar = false;
			this.Closing += new System.ComponentModel.CancelEventHandler(this.MsaInflectionFeatureListDlg_Closing);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// If OK, then make FS have the selected feature value(s).
		/// JohnT: This is a really ugly kludge, which I have only partly repaired.
		/// We need the dialog to return with m_fs set to an FsFeatStruc (if OK was clicked),
		/// since that is what the bulk edit bar wants to copy to MoStemMsas for any items
		/// it is asked to modify. Also, the new FsFeatStruc needs to be in the ReferenceForms
		/// (which is what m_owningFlid apparently always is, currently) so that it will become
		/// one of the items in the combo list and can be selected. However, Andy says this is
		/// not the intended use of ReferenceForms at all.
		/// A further ugliness is that we always make a new FsFeatStruc (unless one was passed
		/// in to one of the SegDlgInfo methods, but AFAIK that override is never used), but
		/// we then delete it if it turns out to be a duplicate. There is no other straightforward
		/// way to detect that the current choices in the dialog correspond to an existing item.
		/// This may cause problems in the new world, where we can't do this "suppress sub tasks"
		/// trick without losing our Undo stack.
		/// It may be possible in the new world to create an object without initially giving it an
		/// owner, and only persist it if it is NOT a duplicate. But even that we don't really want
		/// to be undoable, nor should it clear the undo stack. Really the list of possible choices
		/// for the combo should not be separately persisted as model data, but it should be persisted
		/// somehow...
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MsaInflectionFeatureListDlg_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (DialogResult == DialogResult.OK)
			{
				// making and maybe then deleting the new item for the combo is not undoable
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor,
					() =>
						{
							if (m_fs == null)
							{
								// Didn't have one to begin with. See whether we want to create one.
								if (CheckFeatureStructure(m_tvMsaFeatureList.Nodes))
								{
									var repo = m_cache.ServiceLocator.GetInstance<IFsFeatStrucRepository>();
									// FsFeatStruc may be owned atomically or in a colllection. See which fake insertion index we need.
									int where = m_cache.MetaDataCacheAccessor.GetFieldType(m_owningFlid) == (int) CellarPropertyType.OwningAtomic ? -2: -1;
									int hvoNew = m_cache.DomainDataByFlid.MakeNewObject(FsFeatStrucTags.kClassId, m_hvoOwner,
										m_owningFlid, where);
									m_fs = repo.GetObject(hvoNew);
								}
								else
								{
									return; // leave it null.
								}
							}
							// clean out any extant features in the feature structure
							foreach (var spec in m_fs.FeatureSpecsOC)
								m_fs.FeatureSpecsOC.Remove(spec);
							UpdateFeatureStructure(m_tvMsaFeatureList.Nodes);
							// The (usually) newly created one may be a duplicate. If we find a duplicate
							// delete the one we just made (or were passed) and return the duplicate.
							int cpt = m_cache.MetaDataCacheAccessor.GetFieldType(m_owningFlid);
							if (m_hvoOwner != 0 && cpt != (int) CellarPropertyType.OwningAtomic)
							{
								int chvo = m_cache.DomainDataByFlid.get_VecSize(m_hvoOwner, m_owningFlid);
								for (int ihvo = 0; ihvo < chvo; ihvo++)
								{
									int hvo = m_cache.DomainDataByFlid.get_VecItem(m_hvoOwner, m_owningFlid, ihvo);
									if (hvo == m_fs.Hvo)
										continue;
									IFsFeatStruc fs = m_cache.ServiceLocator.GetInstance<IFsFeatStrucRepository>().GetObject(hvo);
									if (DomainObjectServices.AreEquivalent(fs, m_fs))
									{
										m_cache.DomainDataByFlid.DeleteObj(m_fs.Hvo);
										m_fs = fs;
										break;
									}
								}
							}
							// If the user emptied all the FeatureSpecs (i.e. chose "None of the above" in each area),
							// then we need to delete the FsFeatStruc. (LT-13596)
							if (FS.FeatureSpecsOC.Count == 0)
							{
								if (m_fs.CanDelete)
									m_fs.Delete();
								m_fs = null;
							}
						});
			}

			if (m_propertyTable != null)
			{
				m_propertyTable.SetProperty("msaInflFeatListDlgLocation", Location, true, true);
				m_propertyTable.SetProperty("msaInflFeatListDlgSize", Size, true, true);
			}
		}

		/// <summary>
		/// Answer true if the tree node collection, passed to UpdateFeatureStructure,
		/// will produce a non-empty feature structure.
		/// </summary>
		/// <param name="col"></param>
		/// <returns></returns>
		private bool CheckFeatureStructure(TreeNodeCollection col)
		{
			foreach (FeatureTreeNode tn in col)
			{
				if (tn.Nodes.Count > 0)
				{
					if (CheckFeatureStructure(tn.Nodes))
						return true;
				}
				else if (tn.Chosen && (0 != tn.Hvo))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Makes the feature structure reflect the values chosen in the treeview
		/// </summary>
		/// <remarks>Is public for Unit Testing</remarks>
		/// <param name="col">collection of nodes at this level</param>
		public void UpdateFeatureStructure(TreeNodeCollection col)
		{
			CheckDisposed();

			foreach (FeatureTreeNode tn in col)
			{
				if (tn.Nodes.Count > 0)
					UpdateFeatureStructure(tn.Nodes);
				else if (tn.Chosen && (0 != tn.Hvo))
				{
					var fs = m_fs;
					IFsFeatureSpecification val = null;
					// add any terminal nodes to db
					BuildFeatureStructure(tn, ref fs, ref val);
				}
			}
		}

		/// <summary>
		/// Recursively builds the feature structure based on contents of treeview node path.
		/// It recurses back up the treeview node path to the top and then builds the feature structure
		/// as it goes back down.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="fs"></param>
		/// <param name="val"></param>
		/// <returns></returns>
		private void BuildFeatureStructure(FeatureTreeNode node, ref IFsFeatStruc fs, ref IFsFeatureSpecification val)
		{
			if (node.Parent != null)
				BuildFeatureStructure((FeatureTreeNode)node.Parent, ref fs, ref val);
			switch (node.Kind)
			{
				case FeatureTreeNodeInfo.NodeKind.Complex:
					var complexFeat = m_cache.ServiceLocator.GetInstance<IFsComplexFeatureRepository>().GetObject(node.Hvo);
					var complex = fs.GetOrCreateValue(complexFeat);
					val = complex;
					val.FeatureRA = complexFeat;
					if (fs.TypeRA == null)
						fs.TypeRA = m_cache.LanguageProject.MsFeatureSystemOA.TypesOC.SingleOrDefault(type => type.FeaturesRS.Contains(complexFeat));
					fs = (IFsFeatStruc)complex.ValueOA;
					if (fs.TypeRA == null)
					{
						// this is the type of what's being embedded in the fs
						var cf = val.FeatureRA as IFsComplexFeature;
						if (cf != null)
						{
							fs.TypeRA = cf.TypeRA;
						}
					}
					break;
				case FeatureTreeNodeInfo.NodeKind.Closed:
					var closedFeat = m_cache.ServiceLocator.GetInstance<IFsClosedFeatureRepository>().GetObject(node.Hvo);
					val = fs.GetOrCreateValue(closedFeat);
					val.FeatureRA = closedFeat;
					if (fs.TypeRA == null)
					{
						// SingleOrDefault() gave an exception if 2 complex features used the same feature (LT-12780)
						fs.TypeRA = m_cache.LanguageProject.MsFeatureSystemOA.TypesOC.FirstOrDefault(type => type.FeaturesRS.Contains(closedFeat));
					}
					break;
				case FeatureTreeNodeInfo.NodeKind.SymFeatValue:
					var closed = val as IFsClosedValue;
					if (closed != null)
						closed.ValueRA = m_cache.ServiceLocator.GetInstance<IFsSymFeatValRepository>().GetObject(node.Hvo);
					break;
			}
		}
		protected virtual void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			if (m_highestPOS == null)
				return;  // nowhere to go
			// code in the launcher handles the jump
			DialogResult = DialogResult.Yes;
			Close();
		}

		private void m_bnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), m_helpTopic);
		}
	}
	public class FeatureSystemInflectionFeatureListDlg : MsaInflectionFeatureListDlg
	{
		public FeatureSystemInflectionFeatureListDlg()
			: base()
		{
		}

		protected override void EnableLink()
		{
			linkLabel1.Enabled = true;
		}
		protected override void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			// code in the launcher handles the jump
			DialogResult = DialogResult.Yes;
			Close();
		}
		/// <summary>
		/// Get/Set prompt text
		/// </summary>
		public override string Prompt
		{
			get
			{
				CheckDisposed();

				return labelPrompt.Text;
			}
			set
			{
				CheckDisposed();

				labelPrompt.Text = value;
			}
		}

		/// <summary>
		/// Get/Set link text
		/// </summary>
		public override string LinkText
		{
			get
			{
				CheckDisposed();

				return linkLabel1.Text;
			}
			set
			{
				CheckDisposed();

				linkLabel1.Text = value;
			}
		}
		/// <summary>
		/// Load the tree items if the starting point is a feature structure.
		/// </summary>
		/// <param name="fs"></param>
		protected override void LoadInflFeats(IFsFeatStruc fs)
		{
			PopulateTreeFromFeatureSystem();
			m_tvMsaFeatureList.PopulateTreeFromFeatureStructure(fs);
			FinishLoading();
		}

		/// <summary>
		/// Load the tree items if the starting point is an owning MSA and flid.
		/// </summary>
		/// <param name="cobj"></param>
		/// <param name="owningFlid"></param>
		protected override void LoadInflFeats(ICmObject cobj, int owningFlid)
		{
			PopulateTreeFromFeatureSystem();
			FinishLoading();
		}

		/// <summary>
		/// Get the top level complex features
		/// Also get top level closed features which are not used by any complex feature
		/// (to tell, we have to look at the types)
		/// </summary>
		private void PopulateTreeFromFeatureSystem()
		{
			var featureSystem = m_cache.LangProject.MsFeatureSystemOA;
			var topLevelComplexFeatureDefinitions =
				featureSystem.FeaturesOC.Where(fd => fd.ClassID == FsComplexFeatureTags.kClassId);
			m_tvMsaFeatureList.PopulateTreeFromInflectableFeats(topLevelComplexFeatureDefinitions);
			var topLevelClosedFeatureDefinitions =
				featureSystem.FeaturesOC.Where(fd => fd.ClassID == FsClosedFeatureTags.kClassId);
			foreach (var closedFeatureDefinition in topLevelClosedFeatureDefinitions)
			{
				var typeUsedByComplexFormForThisClosedFeature =
					topLevelComplexFeatureDefinitions.Cast<IFsComplexFeature>().Select(cx => cx.TypeRA).Where(
						t => t.FeaturesRS.Contains(closedFeatureDefinition));
				if (!typeUsedByComplexFormForThisClosedFeature.Any())
					m_tvMsaFeatureList.PopulateTreeFromInflectableFeat(closedFeatureDefinition);
			}
		}

		public FeatureStructureTreeView TreeView
		{
			get { return m_tvMsaFeatureList; }
		}

	}
}
