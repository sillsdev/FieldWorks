// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SidebarAdaptor.cs
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
//	this is an adapter for Carlos Perez's "SidebarLibrary" implementation of a sidebar.
//	Since posting this on an open source site, he has retracted it is now sells the package
//	commercially as "SharpLib" (is that it? sharp something).
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.  Drawing;
using System.  Diagnostics;

//for outlookbar
using SidebarLibrary.WinControls;
using SidebarLibrary.CommandBars;
using SidebarLibrary.Menus;
using SidebarLibrary.General;
using SIL.Utils;

namespace XCore
{
	/// <summary>
	/// Summary description for SidebarAdaptor.
	/// </summary>
	public class SidebarAdapter: IUIAdapter, IDisposable
	{
		protected ImageCollection m_smallImages;
		protected ImageCollection m_largeImages;
		protected OutlookBar m_bar;
		protected Mediator m_mediator;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="IUIAdapter"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public SidebarAdapter()
		{
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~SidebarAdapter()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				if (m_bar != null)
					m_bar.Dispose();
			}
			m_bar = null;
			IsDisposed = true;
		}
		#endregion

		/// <summary>
		/// store the location/settings of various widgets so that we can restore them next time
		/// </summary>
		public virtual void PersistLayout()
		{
		}

		public void TabOpened(OutlookBarBand band, OutlookBarItem item)
		{
			Debug.WriteLine("opened");
		}
		public void OnItemClicked(OutlookBarBand band, OutlookBarItem item)
		{
			ChoiceBase control = (ChoiceBase)item.Tag;
			control.OnClick(item, null);
		}

		public void OnTreeNodeSelected (object sender, TreeViewEventArgs arguments)
		{
			ChoiceBase control;
			if (arguments.Node.Tag is ChoiceGroup)
			{
				control = ((ChoiceGroup)arguments.Node.Tag).CommandChoice;
				if (control == null)
					return;//nothing to do then.
			}
			else
				control = (ChoiceBase)arguments.Node.Tag;
			control.OnClick(null, null);
		}

		/// <summary>
		/// Do anything that is needed after all of the other widgets have been Initialize.
		/// </summary>
		public void FinishInit()
		{
		}

		/// <summary>
		/// The OutlookBar calls this when one of its properties changes, specifically, a different band is opened.
		/// </summary>
		/// <param name="band"></param>
		/// <param name="property"></param>
		public void PropertyChanged(OutlookBarBand band, OutlookBarProperty property)
		{
			if (property == OutlookBarProperty.CurrentBandChanged)
			{
				//int index =m_bar.GetCurrentBand();
				//OutlookBarBand band = m_bar.Bands[index];
				ChoiceGroup group = (ChoiceGroup) band.Tag;
				Debug.Assert(group != null);
				group.OnDisplay(this, null);
			}
		}

		public System.Windows.Forms.Control Init(System.Windows.Forms.Form window, ImageCollection smallImages, ImageCollection largeImages, Mediator mediator)
		{
			m_mediator = mediator;
			m_smallImages= smallImages;
			m_largeImages= largeImages;

			// Create controls
			m_bar = new OutlookBar();
			m_bar.Dock = DockStyle.Left;
			m_bar.Width = 140;

			//m_bar.ItemDropped +=(new SidebarLibrary.WinControls.OutlookBarItemDroppedHandler(TabOpened));
			m_bar.ItemClicked +=(new SidebarLibrary.WinControls.OutlookBarItemClickedHandler(OnItemClicked));
			m_bar.PropertyChanged +=(new SidebarLibrary.WinControls.OutlookBarPropertyChangedHandler(PropertyChanged));
			return m_bar;
		}



		public void CreateUIForChoiceGroupCollection(ChoiceGroupCollection groupCollection)
		{
			foreach(ChoiceGroup group in groupCollection)
			{
				// make band
				string label = group.Label;
				label = label.Replace("_", "");
				OutlookBarBand band;
				if (group.HasSubGroups())
				{
					MakeTree(groupCollection, label, out band);
				}
				else
				{
					band = new OutlookBarBand(label);
				}
				band.Tag=group;

				group.ReferenceWidget = band;

				//				band.GotFocus += new System.EventHandler(group.OnDisplay);

				m_bar.Bands.Add(band);
				band.SmallImageList =  m_smallImages.ImageList;
				band.LargeImageList =  m_largeImages.ImageList;

				object s = m_mediator.PropertyTable.GetValue("SidebarSize");
				if (s ==null || (string)s=="small")
					band.IconView= SidebarLibrary.WinControls.IconView.Small;
				else
					band.IconView= SidebarLibrary.WinControls.IconView.Large;

				band.Background = SystemColors.AppWorkspace;
				band.TextColor = Color.White;
				//note that I had to fix the outlook bar code I downloaded to make this work.
				//so if we download a new one and it stops working, go fix it again.
				band.Font = new Font("Microsoft Sans Serif", 12);
			}

		}




		public void CreateUIForChoiceGroup (ChoiceGroup group)
		{
			OutlookBarBand band = (OutlookBarBand) group.ReferenceWidget;

			if (band.ChildControl != null)
				FillTreeNodes(((TreeView)(band.ChildControl)).Nodes, group);
			else
			{
				band.Items.Clear();
				//doesn't make a difference: band.Click += new System.  EventHandler(group.OnDisplay);
				foreach(ChoiceRelatedClass item in group)
				{
					Debug.Assert(item is ChoiceBase, "only things that can be made into buttons should be appearing here.else, we should have a tree.");
					MakeButton(band, (ChoiceBase)item);
				}
			}
		}
		#region tree control stuff
		private void MakeTree(ChoiceGroupCollection groupCollection, string label, out OutlookBarBand band)
		{
			TreeView tree = new TreeView();
			tree.Tag=groupCollection;
			tree.AfterSelect += new TreeViewEventHandler(OnTreeNodeSelected);
			band = new OutlookBarBand(label, tree);
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks> the first time this is called, the group will be the
		/// band.  It will then be called recursively for each note that contains other nodes.</remarks>
		/// <param name="tree"></param>
		/// <param name="group"></param>
		protected void FillTreeNodes (TreeNodeCollection nodes, ChoiceGroup group)
		{
			if (nodes.Count >0)//hack...without this, we were losing expansion during OnIdle()
				return;
			nodes.Clear();
			group.PopulateNow();
			foreach(ChoiceRelatedClass item in group)
			{
				TreeNode node =MakeTreeNode(item);
				nodes.Add(node);
				if (item is ChoiceGroup)
					FillTreeNodes (node.Nodes, (ChoiceGroup) item);
			}
		}

		protected TreeNode MakeTreeNode (ChoiceRelatedClass item)
		{
			TreeNode node = new TreeNode (item.Label.Replace("_",""));
			node.Tag=item;
			item.ReferenceWidget = node;
			return node;
		}

		#endregion

		protected void MakeButton(OutlookBarBand band , ChoiceBase control)
		{
			UIItemDisplayProperties display = control.GetDisplayProperties();
			display.Text = display.Text.Replace("_", "");
			OutlookBarItem button;
			//			if(m_images!=null && m_images.ImageList.Images.Count>0)
			//			{
			//				button = new OutlookBarItem(display.Text,0,control);
			//			}
			//			else		//no images have been supplied to us!
			button = new OutlookBarItem();
			button.Tag = control;
			control.ReferenceWidget = button;

			if(band.IconView == SidebarLibrary.WinControls.IconView.Large)
			{
				if(m_largeImages.ImageList.Images.Count>0)
					button.ImageIndex = m_largeImages.GetImageIndex(display.ImageLabel);
			}
			else
			{
				if(m_smallImages.ImageList.Images.Count>0)
					button.ImageIndex = m_smallImages.GetImageIndex(display.ImageLabel);
			}

			button.Selected = display.Checked;
			button.Text = display.Text;
//			if(display.Checked)
//				button.Text = button.Text + " (X)";

			if(!display.Enabled)
				button.Text = button.Text + " NA";


			//note that this sidebar library we are using does not provide click events on individual items.
			//So we cannot wire them up here.
			band.Items.Add (button);
		}

		/// <summary>
		/// take the opportunity to read draw this band, so that these selected and enabled items are up to date.
		/// </summary>
		public void OnIdle()
		{
			OutlookBarBand band = this.m_bar.Bands[this.m_bar.GetCurrentBand()];
			if(band== null)
				return;

			ChoiceGroup group = (ChoiceGroup)band.Tag;
			CreateUIForChoiceGroup(group);
			m_bar.Refresh();
		}
	}

}
