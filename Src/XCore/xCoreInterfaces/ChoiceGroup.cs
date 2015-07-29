// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ControlGroup.cs
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using SIL.Utils;

namespace XCore
{
	/// <summary>
	/// Summary description for ChoiceGroupCollection.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification = "variable is a reference; it is owned by parent")]
	public abstract class ChoiceRelatedClass : ArrayList
	{
		protected IUIAdapter m_adapter;
		protected XmlNode m_configurationNode;

		public XmlNode ConfigurationNode
		{
			get
			{
				return m_configurationNode;
			}
		}
		protected Mediator m_mediator;
		protected PropertyTable m_propertyTable;

		protected object m_referenceWidget;

		protected bool m_defaultVisible;

		public ChoiceRelatedClass(Mediator mediator, PropertyTable propertyTable, IUIAdapter adapter, XmlNode configurationNode)
		{
			m_adapter = adapter;
			m_mediator = mediator;
			m_propertyTable = propertyTable;
			m_configurationNode = configurationNode;
			m_defaultVisible= XmlUtils.GetOptionalBooleanAttributeValue(m_configurationNode, "defaultVisible", true);
		}

		protected abstract void Populate();

		abstract public UIItemDisplayProperties GetDisplayProperties();

		/// <summary>
		/// currently, this is used for unit testing.it may be used for more in the future.
		/// </summary>
		virtual public string Id
		{
			get
			{
				string id = XmlUtils.GetAttributeValue(m_configurationNode, "id", "");
				if (id == "")
				{	//default to the label
					id = this.Label.Replace("_","");//remove underscores
				}
				return id;
			}
		}

		virtual public string Label
		{
			get
			{
				return XmlUtils.GetLocalizedAttributeValue(m_configurationNode, "label", null);
			}
		}

		/// <summary>
		/// the icon
		/// </summary>
		public virtual string ImageName
		{
			get
			{
				return XmlUtils.GetAttributeValue(m_configurationNode, "icon", "default");
			}
		}

		/// <summary>
		/// this is used by the IUIAdaptor to store whatever it needs to to link this to a real UI choice
		/// </summary>
		public object ReferenceWidget
		{
			set
			{
				m_referenceWidget = value;
			}
			get
			{
				return m_referenceWidget;
			}
		}
	}

	//menubar, sidebar, toolbars set
	public class ChoiceGroupCollection : ChoiceRelatedClass
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ChoiceGroupCollection"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ChoiceGroupCollection(Mediator mediator, PropertyTable propertyTable, IUIAdapter adapter, XmlNode configurationNode)
			: base(mediator, propertyTable,  adapter, configurationNode)
		{
		}

		public void Init()
		{
			//there is no "OnDisplay" event for the main menu bar
			UpdateUI();
		}
		protected void UpdateUI()
		{
			Populate ();
			m_adapter.CreateUIForChoiceGroupCollection(this);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		protected override void Populate()
		{
			XmlNodeList groups =m_configurationNode.SelectNodes(NodeSelector);
			foreach (XmlNode node in groups)
			{
				ChoiceGroup group = new ChoiceGroup(m_mediator, m_propertyTable, m_adapter, node, null);
				this.Add(group);
			}
		}
		protected string NodeSelector
		{
			get {return "menu | toolbar | tab";}
		}

		override public UIItemDisplayProperties GetDisplayProperties()
		{
			return null;// not using this (yet)... might need it when we want to hide, say, a whole toolbar
		}
		/*
				public bool HandleKeydown(System.Windows.Forms.KeyEventArgs e)
				{
					foreach(ChoiceRelatedClass c in this)
					{
						if(c is ChoiceGroup)
						{
							if( ((ChoiceGroup)c).HandleKeydown(e))
								return true;
						}
					}
					return false;
				}
		*/
		/// <summary>
		/// look for the group(e.g. menu, tab, toolbar) with the supplied id
		/// </summary>
		/// <param name="id"> the id attribute (or the label attribute without any underscores,
		/// if no id is specified in the configuration)</param>
		/// <returns></returns>
		public ChoiceGroup FindById(string id)
		{
			foreach(ChoiceGroup group in this)
			{
				if (group.Id== id)
					return group;
			}
			throw new ArgumentException("There is no item with the id '"+id+"'.");
		}
	}


	/// <summary>
	//menus, sidebar bands, toolbars
	/// (JohnT) May be used with explicit commands; I have not yet figured out when it has a sequence
	/// of configuration nodes and when just one, but in either case they can have children that are
	/// item or menu (or, in taskbar, group) elements for commands or further submenus.
	/// The more interesting case is when the [menu] element (I'm avoiding angle brackets because they
	/// get interpreted in these doc comments) has a 'list' and 'behavior' attribute. In this case the
	/// group generates subitems in one of two ways (or a combination):
	///  - if the value of the 'list' attribute matches the 'id' of a [list] element somewhere in the
	/// overall configuration document, the list of subitems is initially populated with those items.
	///  - then, a method is broadcast, whose name is 'OnDisplay' plus the value of the 'list' attribute.
	/// It takes a string and UIListDisplayProperties argument, where the string is the value (if any)
	/// of the wsSet attribute. It can obtain the xCore.List of subitems from the List property of the
	/// UIListDisplayProperties argument, and modify it using methods inherited from ArrayList or,
	/// usually preferably, the Add or Insert methods defined on xCore.List. Items added must be of
	/// type ListItem. They can have labels (text of menu item), image (to appear next to item),
	/// and parameterNodes that specify what should happen if the item is chosen.
	/// The behavior when a list item is chosen depends on the 'behavior' attribute. There are currently
	/// three options, singlePropertyAtomicValue, singlePropertySequenceValue, and command. The first two
	/// are fully implemented within the choice group, and affect a property of the mediator; I haven't
	/// fully worked out the details. If the behavior is 'command', clicking that item results in a broadcast of
	/// to OnMessage(paramNode), the paramNode put into the ListItem by the Display{ListId} method or
	/// (I think) the node in the configuration file list that produced the item.
	/// </summary>
	public class ChoiceGroup : ChoiceRelatedClass
	{
		protected ChoiceGroup m_parent;
		protected CommandChoice m_treeGroupCommandChoice;
		protected List<XmlNode> m_configurationNodes;

		/// <summary>
		/// this is the PropertyTable property which is changed when the user selects an item from the group
		/// </summary>
		protected string m_propertyName;
		protected PropertyTable.SettingsGroup m_settingsGroup = PropertyTable.SettingsGroup.Undecided;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ControlGroup"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ChoiceGroup(Mediator mediator, PropertyTable propertyTable, IUIAdapter adapter, XmlNode configurationNode, ChoiceGroup parent)
			: base(mediator, propertyTable, adapter, configurationNode)
		{
			m_parent = parent;

			//allow for a command to be attached to a group (todo: should be for tree groups only)
			//as it doesn't make sense for some menus or command bars to have an associated command.
			//for now, leave it to the schema to prevent anything but the right element from having the command attribute.
			if (XmlUtils.GetAttributeValue(m_configurationNode,"command") != null)
			{
				m_treeGroupCommandChoice = new CommandChoice(mediator, propertyTable, configurationNode, adapter, this);
			}

		}

		static public PropertyTable.SettingsGroup GetSettingsGroup(XmlNode configurationNode, PropertyTable.SettingsGroup defaultSettings)
		{
			string settingsGroupValue = XmlUtils.GetAttributeValue(configurationNode, "settingsGroup");
			if (String.IsNullOrEmpty(settingsGroupValue))
				return defaultSettings;
			switch (settingsGroupValue)
			{
				case "local":
					return PropertyTable.SettingsGroup.LocalSettings;
				case "global":
					return PropertyTable.SettingsGroup.GlobalSettings;
				default:
					throw new Exception(String.Format("GetSettingsGroup does not yet support {0} for 'settingsGroup' attribute.", settingsGroupValue));
			}
		}

		internal PropertyTable.SettingsGroup PropertyTableGroup
		{
			get
			{
				if (m_settingsGroup == PropertyTable.SettingsGroup.Undecided)
				{
					m_settingsGroup = ChoiceGroup.GetSettingsGroup(m_configurationNode, PropertyTable.SettingsGroup.BestSettings);
				}
				return m_settingsGroup;
			}
		}

		/// <summary>
		/// Group made of multiple menus.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// <param name="adapter"></param>
		/// <param name="configurationNodes"></param>
		/// <param name="parent"></param>
		public ChoiceGroup(Mediator mediator, PropertyTable propertyTable, IUIAdapter adapter, List<XmlNode> configurationNodes, ChoiceGroup parent)
			: base(mediator, propertyTable, adapter, configurationNodes[0]) //hack; just give it the first one
		{
			m_parent = parent;
			m_configurationNodes = configurationNodes;
		}


		public CommandChoice CommandChoice
		{
			get
			{
				return m_treeGroupCommandChoice;
			}
		}

		/// <summary>
		/// Return whether the given choice in the ChoiceGroup has been selected.
		/// </summary>
		/// <param name="choiceValue"></param>
		/// <returns></returns>
		public bool IsValueSelected(string choiceValue)
		{
			switch (this.Behavior)
			{
			case "singlePropertyAtomicValue":
				return SinglePropertyValue == choiceValue;

			case "singlePropertySequenceValue":
				string[] rgsValues = DecodeSinglePropertySequenceValue(SinglePropertyValue);
				for (int i = 0; i < rgsValues.Length; ++i)
				{
					if (rgsValues[i] == choiceValue)
						return true;
				}
				return false;
			case "command": // Commands are never 'selected' (i.e., checked?)
				return false;
			default:
				Trace.Fail("The behavior '" + Behavior + "' is not supported or for some other reason was unexpected here(check capitalization).");
				return false;
			}
		}

		/// <summary>
		/// given a single property value, convert it to an array of strings.
		/// </summary>
		/// <param name="singlePropertySequenceValue"></param>
		/// <returns></returns>
		static public string[] DecodeSinglePropertySequenceValue(string singlePropertySequenceValue)
		{
			return singlePropertySequenceValue.Split(new char[] { ',' });
		}

		/// <summary>
		/// given a series of values, encode it into a single property sequence value.
		/// </summary>
		/// <param name="sValues"></param>
		/// <returns>emptry string if sValues is null or empty</returns>
		static public string EncodeSinglePropertySequenceValue(string[] sValues)
		{
			if (sValues == null || sValues.Length == 0)
				return "";
			return String.Join(",", sValues);
		}

		/// <summary>
		/// look for the choice(e.g. menu, tab, toolbar) with the supplied id
		/// </summary>
		/// <param name="id"> the id attribute (or the label attribute without any underscores,
		/// if no id is specified in the configuration)</param>
		/// <returns></returns>
		public ChoiceRelatedClass FindById(string id)
		{
			//since we are lazy about Populating, we need to Populate before we can search
			this.Populate();
			foreach(ChoiceRelatedClass crc in this)
			{
				if (crc.Id== id)
					return crc;
			}
			return null;
		}

		/*		public bool HandleKeydown(System.Windows.Forms.KeyEventArgs e)
				{
					foreach(ChoiceRelatedClass c in this)
					{
						if(c is ChoiceBase)
						{
							ChoiceBase choice = (ChoiceBase)c;
							if(choice.Shortcut == e.KeyData)
							{
								choice.OnClick(this, e);
								return true;
							}
						}
						else if (c is ChoiceGroup)
						{
							if(((ChoiceGroup)c).HandleKeydown(e))
								return true;
						}
					}
					return false;
				}
		*/
		/*		public ImageList GetImageList()
				{
					// This is obviously just a temporary hack.

					ImageList icons = new ImageList();
					icons.ImageSize = new Size(16, 16);
					//TODO: Remove this hard coded path (requires learning the resource system).
					string asmPathname = Assembly.GetExecutingAssembly().CodeBase;
					string asmPath = asmPathname.Substring(0, asmPathname.LastIndexOf("/"));
					string bitmapPath = System.IO.Path.Combine(asmPath, @"..\..\..\Src\XCore\xCoreTests\listitems.bmp");
					Bitmap b = (Bitmap)System.Drawing.Bitmap.FromFile(bitmapPath);
					b.MakeTransparent();
					icons.Images.Add(b);
					return icons;
				}
		*/


		//for menus, this is wired to be "pop up" event
		public void OnDisplay(object sender, System.EventArgs args)
		{
			UpdateUI();
		}
		protected void UpdateUI()
		{
			Populate ();
			m_adapter.CreateUIForChoiceGroup(this);
		}

		/// <summary>
		/// called by the ui adaptor to get updated display parameters for a widget showing this group
		/// </summary>
		/// <returns></returns>
		override public UIItemDisplayProperties GetDisplayProperties()
		{
			//review: the enabled parameter is set to the same value as the defaultVisible
			// value so that only defaultVisible items are visible by default.  Previously
			// enabled items would be 'visible' and enabled was true by default.
			UIItemDisplayProperties display =new UIItemDisplayProperties(this, this.Label,
				this.m_defaultVisible, ImageName, this.m_defaultVisible);
			if (this.PropertyName != null && this.PropertyName != string.Empty)
				m_mediator.SendMessage("Display"+this.PropertyName, null, ref display);
			else
				m_mediator.SendMessage("Display"+this.Id, null, ref display);

			return display;
		}

		public string ListId
		{
			get
			{
				return XmlUtils.GetAttributeValue(m_configurationNode, "list");
			}
		}
		protected override void Populate()
		{
			Clear();
			if (IsAListGroup)
			{
				PopulateFromList();
			}
			else if (m_configurationNodes != null)
			{
				foreach (XmlNode n in m_configurationNodes)
				{
					Populate(n);
				}
			}
			else
			{
				Populate(m_configurationNode);
			}
		}

		/// <summary>
		/// force the group to populate, even if it wants to be lazy!
		/// </summary>
		/// <remarks> this is a hack because the interface says that Populate() is protected</remarks>
		public void PopulateNow()
		{
			Populate();
		}

		protected void PopulateFromList ()
		{
			/// Just before this group is displayed, allow the group's contents to be modified by colleagues
			//if this is a list-populated group.

			//first, we get the list as it is in the XML configuration file
			XmlNode listNode = m_configurationNode.OwnerDocument.SelectSingleNode("//list[@id='"+this.ListId+"']");

			List list = new List(listNode);

			UIListDisplayProperties display = new UIListDisplayProperties(list);
			display.PropertyName = PropertyName;
			string wsSet = XmlUtils.GetOptionalAttributeValue(m_configurationNode, "wsSet");
			m_mediator.SendMessage("Display"+ ListId, wsSet, ref display);

			PropertyName = display.PropertyName;

			foreach (ListItem item in list)
			{
				if(item is SeparatorItem)
				{
					Add(new SeparatorChoice(null, null, null, null, null));
				}
				else
				{
					Add(new ListPropertyChoice(m_mediator, m_propertyTable, item, m_adapter, this));
				}
			}

			// select the first one if none is selected
			if(
				(!m_propertyTable.PropertyExists(PropertyName, PropertyTableGroup))	// there isn't a value already (from depersisting)
				&& (Count > 0))
			{
				//ListPropertyChoice first = (ListPropertyChoice)this[0];
				//				first.OnClick(this, null);
			}
		}


		/// <summary>
		/// this is used by the sidebar adapter to determine if we need a tree to represent this group.
		/// </summary>
		/// <param name="group"></param>
		/// <returns></returns>
		public bool HasSubGroups()
		{
			this.Clear();
			if (IsAListGroup)
				return false;//hierarchical lists are not currently supported
			else //enhance: all we really need here is in XPATH here to look for sub-group nodes.
			{
				Populate(m_configurationNode);

				foreach(ChoiceRelatedClass item in this )
				{
					if (item is ChoiceGroup)
						return true;
				}
				return false;
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		protected void Populate(XmlNode node)
		{
			Debug.Assert( node != null);
			XmlNodeList items =node.SelectNodes("item | menu | group");
			foreach (XmlNode childNode in items)
			{
				switch (childNode.Name)
				{
					case "item":
						ChoiceBase choice = ChoiceBase.Make(m_mediator, m_propertyTable, childNode, m_adapter, this);
						this.Add(choice);
						break;
					case "menu":
						ChoiceGroup group = new ChoiceGroup(m_mediator, m_propertyTable, m_adapter, childNode, this);
						group.Populate(childNode);
						//Only add the submenu if it contains a list of items what will be visible.
						//We do not want an empty submenu  LT-8791.
						string hasList = XmlUtils.GetAttributeValue(childNode, "list");
						if (hasList != null || ASubmenuItemIsVisible(group))
							this.Add(group);
						break;
					case "group":	//for tree views in the sidebar
						group = new ChoiceGroup(m_mediator, m_propertyTable, m_adapter, childNode, this);
						this.Add(group);
						break;
					default:
						Debug.Fail("Didn't understand node type '"+childNode.Name+"' in this context."+node.OuterXml);
						break;
				}

			}
		}

		private bool ASubmenuItemIsVisible(ChoiceGroup group)
		{
			foreach (object cb in group)
			{
				if (cb is ChoiceBase && !(cb is SeparatorChoice))
				{
					UIItemDisplayProperties display = (cb as ChoiceBase).GetDisplayProperties();
					if (display.Visible == true)
						return true;
				}
				if (cb is ChoiceGroup && ASubmenuItemIsVisible(cb as ChoiceGroup))
					return true;
			}
			return group.IsAListGroup; // assume a list has at least one viable choice
		}

		public string Behavior
		{
			get
			{
				return XmlUtils.GetAttributeValue(m_configurationNode, "behavior", "pushOnly");
			}
		}

		//		public string Property
		//		{
		//			get
		//			{
		//				return XmlUtils.GetAttributeValue(m_configurationNode, "property", "");
		//			}
		//		}



		public string PropertyName
		{
			//			get
			//			{
			//				return XmlUtils.GetAttributeValue(m_configurationNode, "property", "");
			//			}
			get
			{
				if(m_propertyName == null)
					m_propertyName =  XmlUtils.GetAttributeValue(m_configurationNode, "property", "");

				return m_propertyName;
			}
			set
			{
				m_propertyName = value;
			}
		}

		public string DefaultSinglePropertyValue
		{
			get
			{	//I don't know what to do about the default here
				return XmlUtils.GetAttributeValue(m_configurationNode, "defaultPropertyValue", "????");
			}
		}

		public string SinglePropertyValue
		{
			get
			{
				return m_propertyTable.GetStringProperty(PropertyName, DefaultSinglePropertyValue, PropertyTableGroup);
			}
		}

		public void HandleItemClick(ListPropertyChoice choice)
		{
			m_mediator.SendMessage("ProgressReset", this);
			switch (Behavior)
			{
			case "singlePropertyAtomicValue":
				HandleClickedWhenSinglePropertyAtomicValue(choice);
				break;
			case "singlePropertySequenceValue":
				HandleClickedWhenSinglePropertySequenceValue(choice);
				break;
			case "command":
				HandleClickedWhenCommand(choice);
				break;
			default:
				Trace.Fail("The behavior '" + Behavior + "' is not supported or for some other reason was unexpected here(check capitalization).");
				break;
			}
			m_mediator.SendMessage("ProgressReset", this);

		}

		private string CommandMessage
		{
			get { return XmlUtils.GetManditoryAttributeValue(m_configurationNode, "message"); }
		}

		/// <summary>
		/// Called when a list subitem is clicked in a list where we want to invoke a command when clicked.
		/// Passes as argument the XmlNode that comes from the list item; this may come from a list in
		/// the configuration, or have been generted by the Display{ListId}() method. In the latter case,
		/// it is up to that method to include enough information in the XmlNode to complete the command.
		/// </summary>
		/// <param name="choice"></param>
		private void HandleClickedWhenCommand(ListPropertyChoice choice)
		{
			m_mediator.SendMessage(CommandMessage, choice.ParameterNode);
		}

		protected bool IsAListGroup
		{
			get
			{
				return this.Behavior == "singlePropertyAtomicValue" ||
					this.Behavior == "singlePropertySequenceValue" ||
					this.Behavior == "command";
			}
		}

		protected void HandleClickedWhenSinglePropertyAtomicValue (ListPropertyChoice choice)
		{
			ChooseSinglePropertyAtomicValue(m_mediator, m_propertyTable, choice.Value, choice.ParameterNode, PropertyName, PropertyTableGroup);
		}

		/// <summary>
		/// set the value of a property to a node of a list. Also sets the corresponding parameters property.
		/// </summary>
		/// <remarks> this is static so that it can be called by the XCore initializationcode and set from the contents of the XML configuration file</remarks>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// <param name="choiceValue"></param>
		/// <param name="choiceParameters"></param>
		/// <param name="propertyName"></param>
		/// <param name="settingsGroup"></param>
		static public void ChooseSinglePropertyAtomicValue(Mediator mediator, PropertyTable propertyTable, string choiceValue,
			XmlNode choiceParameters,string propertyName, PropertyTable.SettingsGroup settingsGroup)
		{
			//a hack (that may be we could live with)
			//	if(choiceParameters !=null)
			//	{
			propertyTable.SetProperty(propertyName + "Parameters", choiceParameters, settingsGroup, true);
			//it is possible that we would like to persist these parameters
			//however, as a practical matter, you cannot have XmlNodes witch do not belong to a document.
			//therefore, they could not be deserialize if we did save them.
			//unless, of course, we convert them to a string before serializing.
			//However, when de-serializing, what document would we attach the new xmlnode to?
			propertyTable.SetPropertyPersistence(propertyName + "Parameters", false, settingsGroup);
			//}


			//remember, each of these calls to SetProperty() generate a broadcast, so the order of these two calls
			//is relevant.
			propertyTable.SetProperty(propertyName, choiceValue, settingsGroup, true);

			if (choiceParameters != null)
			{
				//since we cannot persist the parameters, it's safer to not persist the choice either.
				propertyTable.SetPropertyPersistence(propertyName, false, settingsGroup);
			}

		}

		protected void HandleClickedWhenSinglePropertySequenceValue(ListPropertyChoice choice)
		{
			bool fEmptyAllowed = XmlUtils.GetOptionalBooleanAttributeValue(m_configurationNode,
				"emptyAllowed", false);
			ChooseSinglePropertySequenceValue(m_mediator, m_propertyTable, choice.Value, choice.ParameterNode,
				PropertyName, fEmptyAllowed, PropertyTableGroup);
		}

		static int IndexOf(string[] rgs, string s)
		{
			for (int i = 0; i < rgs.Length; ++i)
			{
				if (rgs[i] == s)
					return i;
			}
			return -1;
		}

		static public void ChooseSinglePropertySequenceValue(Mediator mediator, PropertyTable propertyTable, string choiceValue,
			XmlNode choiceParameterNode, string propertyName, bool fEmptyAllowed, PropertyTable.SettingsGroup settingsGroup)
		{
			propertyTable.SetProperty(propertyName + "Parameters", choiceParameterNode, settingsGroup, true);
			propertyTable.SetPropertyPersistence(propertyName + "Parameters", false, settingsGroup);
			string sValue = propertyTable.GetStringProperty(propertyName, "", settingsGroup);
			string[] rgsValues = sValue.Split(',');
			int idx = -1;
			if (sValue == choiceValue)
			{
				if (fEmptyAllowed)
					sValue = "";
			}
			else if ((idx = IndexOf(rgsValues, choiceValue)) != -1)
			{
				// remove the choiceValue from the string.
				Debug.Assert(rgsValues.Length > 1);
				System.Text.StringBuilder sbValues = new System.Text.StringBuilder(sValue.Length);
				for (int i = 0; i < rgsValues.Length; ++i)
				{
					if (idx != i)
					{
						if (sbValues.Length > 0)
							sbValues.Append(",");
						sbValues.Append(rgsValues[i]);
					}
				}
				sValue = sbValues.ToString();
			}
			else
			{
				if (sValue.Length == 0)
					sValue = choiceValue;
				else
					sValue = sValue + "," + choiceValue;
			}
			propertyTable.SetProperty(propertyName, sValue, settingsGroup, true);
			propertyTable.SetPropertyPersistence(propertyName, false, settingsGroup);
		}

		public bool IsTopLevelMenu
		{
			get
			{
				return m_parent == null;
			}
		}

		public bool IsContextMenu
		{
			get
			{
				return (m_configurationNode != null &&
					m_configurationNode.SelectSingleNode("ancestor::contextMenus") != null);
			}
		}

		public bool IsSubmenu
		{
			get
			{
				return !IsTopLevelMenu && !IsInlineChoiceList;
			}
		}

		public bool IsInlineChoiceList
		{
			get
			{
				return  !IsTopLevelMenu && XmlUtils.GetBooleanAttributeValue(m_configurationNode, "inline") ;
			}
		}
	}

}
