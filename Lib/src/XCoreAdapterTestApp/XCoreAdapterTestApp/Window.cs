// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
// </copyright>
#endregion
//
// File:
// Authorship History:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using XCore;
using SIL.Utils;
using System.Xml;

namespace XCoreAdapterTestApp
{
	/// <summary>
	/// Test program to show the Flex sidebar using a Flex sidebar adapter.
	/// </summary>
	public partial class Window : Form
	{
		protected Control m_sidebar;
		protected IUIAdapter m_sidebarAdapter;
		protected Mediator m_mediator;
		protected ImageCollection m_smallImages = new ImageCollection(false);
		protected ImageCollection m_largeImages = new ImageCollection(true);
		protected XmlNode m_windowConfigurationNode = null;
		protected MyChoiceGroupCollection m_sidebarChoiceGroupCollection;
		protected Label m_mainPaneText;

		public Window()
		{
			InitializeComponent();

			m_mainPaneText = new Label();
			m_mainPaneText.Text = "No item was yet clicked.";
			m_mainPaneText.Dock = DockStyle.Right;

			string[] iconLabels = { "iconName" };
			var imagelist = new ImageList();
			var itemIcon = new Bitmap(32, 32);
			for(int x = 0; x < itemIcon.Width; ++x)
				for (int y = 0; y < itemIcon.Height; ++y)
					itemIcon.SetPixel(x, y, Color.Blue);

			imagelist.Images.Add(itemIcon);

			m_smallImages.AddList(imagelist, iconLabels);
			m_largeImages.AddList(imagelist, iconLabels);

			m_mediator = new Mediator();
			m_mediator.StringTbl = new SIL.Utils.StringTable("../../DistFiles/Language Explorer/Configuration");
			m_sidebarAdapter = new SidebarAdapter();
			m_sidebar = m_sidebarAdapter.Init(this, m_smallImages, m_largeImages, m_mediator);

			m_sidebarChoiceGroupCollection = new MyChoiceGroupCollection(m_mediator, m_sidebarAdapter, null);
			m_sidebarChoiceGroupCollection.Init();

			this.Controls.Add(m_sidebar);
			this.Controls.Add(m_mainPaneText);
			m_sidebarAdapter.FinishInit();

			((IUIAdapter)m_sidebarAdapter).OnIdle();
			m_mediator.SendMessage("Idle", null);

			m_mediator.AddColleague(new MyCoreColleague(m_mainPaneText));
		}
	}

	public class MyChoiceGroupCollection : ChoiceGroupCollection
	{
		public MyChoiceGroupCollection(Mediator mediator, IUIAdapter adapter, XmlNode configurationNode)
			: base(mediator, adapter, configurationNode)
		{

		}

		public MyChoiceGroup[] myChoiceGroupArray;

		protected override void Populate()
		{
			myChoiceGroupArray = new MyChoiceGroup[3];
			myChoiceGroupArray[2] = new MyChoiceGroup(m_mediator, m_adapter, null, null, "Areas", "0");
			myChoiceGroupArray[1] = new MyChoiceGroup(m_mediator, m_adapter, null, null, "Fruit", "1");
			myChoiceGroupArray[0] = new MyChoiceGroup(m_mediator, m_adapter, null, null, "Vegetables", "2");
			foreach (var v in myChoiceGroupArray)
			{
				this.Add(v);
			}
		}
	}

	public class MyChoiceGroup : ChoiceGroup
	{
		public MyChoiceGroup(Mediator mediator, IUIAdapter adapter, XmlNode configurationNode, ChoiceGroup parent, string name, string id)
			: base(mediator, adapter, configurationNode, parent)
		{
			var doc = new XmlDocument();
			var config = doc.CreateElement("config");
			config.SetAttribute("behavior", "command");
			config.SetAttribute("id", id);
			config.SetAttribute("label", name);
			config.SetAttribute("icon", "iconName");

			if (name == "Areas")
			{
				config.SetAttribute("message", "TabClick");
			}
			else
			{
				config.SetAttribute("message", "ButtonClick");
			}

			config.SetAttribute("list", name);
			m_configurationNode = config;
		}

		private void AddTabHelper(string label, string val)
		{
			var doc = new XmlDocument();
			ListItem li = new ListItem();
			li.label = label;
			li.enabled = true;
			li.value = val;
			li.imageName = "iconName";

			// <parameters><panels><listPanel listId="controlName"></panels></parameters>
			doc = new XmlDocument();
			var parametersNode = doc.CreateElement("parameters");
			var panelsNode = doc.CreateElement("panels");
			var listPanelNode = doc.CreateElement("listPanel");
			listPanelNode.SetAttribute("listId", label);
			panelsNode.AppendChild(listPanelNode);
			parametersNode.AppendChild(panelsNode);

			li.parameterNode = parametersNode;

			var listPropChoice = new ListPropertyChoice(m_mediator, li, m_adapter, this);
			Add(listPropChoice);
		}

		private void AddItemHelper(string label)
		{
			var doc = new XmlDocument();
			var li = new ListItem();
			li.label = label;
			li.enabled = true;
			li.value = label;
			li.imageName = "iconName";
			li.parameterNode = doc.CreateElement(label);

			var listPropChoice = new ListPropertyChoice(m_mediator, li, m_adapter, this);
			Add(listPropChoice);
		}

		protected override void Populate()
		{
			if (Label == "Fruit")
			{
				AddItemHelper("Apple");
				AddItemHelper("Banana");
			}

			if (Label == "Vegetables")
			{
				AddItemHelper("Carrot");
				AddItemHelper("Broccoli");
			}

			if (Label == "Areas")
			{
				AddTabHelper("Fruit", "hello1");
				AddTabHelper("Vegetables", "hello2");
			}
		}
	}


	public class MyCoreColleague : IxCoreColleague
	{
		public MyCoreColleague(Label l)
		{
			m_label = l;
		}

		/// <summary>
		/// ref to a label to update when receiving a xcore message.
		/// </summary>
		protected Label m_label;

		#region IxCoreColleague Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="configurationParameters"></param>
		/// ------------------------------------------------------------------------------------
		public void Init(Mediator mediator, System.Xml.XmlNode configurationParameters)
		{

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the possible message targets, i.e. the view(s) we are showing
		/// </summary>
		/// <returns>Message targets</returns>
		/// ------------------------------------------------------------------------------------
		public IxCoreColleague[] GetMessageTargets()
		{
			// return list of view windows with focused window being the first one
			List<IxCoreColleague> targets = new List<IxCoreColleague>();
			targets.Add(this);
			return targets.ToArray();
		}

		private void OnButtonClick(object sender)
		{
			m_label.Text = "TabItem: " + (sender as XmlNode).Name;
		}

		private void OnTabClick(object sender)
		{
			m_label.Text = "Tab: " + (sender as XmlNode).Name;
		}
		#endregion
	}
}
