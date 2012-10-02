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
// File: Choice.cs
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
	/// A Choice represents something that may be displayed by a menu or button
	///		E.g.
	///			some command <---- a CommandChoice <---- a MenuItem
	///			some command <---- a CommandChoice <---- a Button
	///
	///			some property <---- a ListPropertyChoice <---- a MenuItem
	///			some property <---- a ListPropertyChoice <---- a sidebar list item
	///
	///			some property <---- a BoolPropertyChoice <---- a MenuItem
	///			some property <---- a BoolPropertyChoice <---- a Button
	///			some property <---- a BoolPropertyChoice <---- a sidebar list item
	///
	///note that in the special case of a TreeNode which has some child nodes, the parent node,
	///which is a ChoiceGroup, may also have an associated ChoiceCommand so that
	///when the user clicks on the group, something can happen (like show some text
	/// in the content view which gives an overview of this section.)
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Xml;
using System.Collections;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;

using SIL.Utils;

namespace XCore
{
	#region ChoiceBase
	//--------------------------------------------------------------------
	/// <summary>
	///	the abstract base class for commandChoice and propertyChoice
	/// </summary>
	//--------------------------------------------------------------------
	abstract public class ChoiceBase : ChoiceRelatedClass
	{
		protected ChoiceGroup m_parent;
		protected PropertyTable.SettingsGroup m_settingsGroup = PropertyTable.SettingsGroup.Undecided;

		protected virtual PropertyTable.SettingsGroup PropertyTableGroup
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
		/// Factory method. Will make the appropriate class, based on the configurationNode.
		/// </summary>
		static public ChoiceBase Make( Mediator mediator,  XmlNode configurationNode, IUIAdapter adapter, ChoiceGroup parent)
		{
			if (XmlUtils.GetAttributeValue(configurationNode, "command","") == "")
			{
				if (XmlUtils.GetAttributeValue(configurationNode, "boolProperty", "") != "")
					return new BoolPropertyChoice(  mediator,    configurationNode,  adapter,  parent);
				else if (XmlUtils.GetAttributeValue(configurationNode, "label", "") == "-")
					return new SeparatorChoice(  mediator,    configurationNode,  adapter,  parent);

					//the case where we want a choice based on a single member of the list
					//e.g. a single view in a list of views, to use on the toolbar.
				else if (XmlUtils.GetAttributeValue(configurationNode, "property", "") != "")
				{
					return MakeListPropertyChoice(mediator, configurationNode, adapter, parent);
				}

				else
					throw new ConfigurationException("Don't know what to do with this item. At least give it a dummy 'boolProperty='foo''.", configurationNode);
			}
			else
				return new CommandChoice(  mediator,    configurationNode,  adapter,  parent);
		}

		/// <summary>
		/// find the matching list and list item referred to buy this note, and create a ListPropertyChoice
		/// to access that item. This is used in the special case where we want a toolbar button or two
		/// instead of showing every item in the list.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="configurationNode"></param>
		/// <param name="adapter"></param>
		/// <returns></returns>
		static public  StringPropertyChoice MakeListPropertyChoice(Mediator mediator,  XmlNode configurationNode, IUIAdapter adapter, ChoiceGroup parent)
		{
			//string listId=XmlUtils.GetAttributeValue(configurationNode, "list", "");
			//			ListItem item = new ListItem();
			//			item.label = "xxxxxx";
			//			item.value = XmlUtils.GetAttributeValue(configurationNode, "value", "");
			//			item.imageName = XmlUtils.GetAttributeValue(configurationNode, "icon", "default");
			//item.parameterNode = parameterNode;

			return new StringPropertyChoice(mediator,configurationNode, adapter, parent);
			//			if(li!=null)
			//				return new ListPropertyChoice( mediator, li,  adapter,  parent);
			//			else
			//	throw new ConfigurationException ("Could not find the list item for '"+val+"' in a list with the id '"+listId+"'.",configurationNode);
		}


		public ChoiceBase( Mediator mediator,  XmlNode configurationNode, IUIAdapter adapter, ChoiceGroup parent)
			: base(mediator, adapter,configurationNode)
		{
			m_parent  = parent;
		}
		public ChoiceBase( Mediator mediator, IUIAdapter adapter, ChoiceGroup parent)
			: base(mediator, adapter,null)
		{
			m_parent  = parent;
		}

		//		abstract public void HandleClick();

		protected override void Populate()
		{
		}

		/// <summary>
		/// the id to use when looking up help strings
		/// </summary>
		public virtual string HelpId
		{
			get
			{
				return "";
			}
		}

		virtual public Keys Shortcut
		{
			get
			{
				return Keys.None;
			}
		}

		abstract 		public void OnClick(object sender, System.EventArgs args);


	}
	#endregion

	#region CommandChoice

	//--------------------------------------------------------------------
	/// <summary>
	/// CommandChoice represents a menu or button which sends commands
	/// </summary>
	//--------------------------------------------------------------------
	public class CommandChoice : ChoiceBase
	{
		protected string m_idOfCorrespondingCommand;
		protected string m_defaultLabelOverride;

		/// <summary>
		///
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="listSet"></param>
		/// <param name="configurationNode"></param>
		/// <param name="adapter"></param>
		/// <param name="parent"></param>
		public CommandChoice( Mediator mediator,  XmlNode configurationNode, IUIAdapter adapter, ChoiceGroup parent)
			: base(mediator,  configurationNode, adapter, parent)
		{
			m_idOfCorrespondingCommand = XmlUtils.GetAttributeValue(m_configurationNode, "command");
			StringTable tbl = null;
			if (mediator != null && mediator.HasStringTable)
				tbl = mediator.StringTbl;
			m_defaultLabelOverride = XmlUtils.GetLocalizedAttributeValue(tbl,
				m_configurationNode, "label", null);
		}

		#region Properties
		override public string Label
		{
			get
			{
				if (m_defaultLabelOverride != null)
					return m_defaultLabelOverride;
				else
					return CommandObject.Label;
			}
		}

		/// <summary>
		/// the id to use when looking up help strings
		/// </summary>
		override public string HelpId
		{
			get
			{
				return m_idOfCorrespondingCommand;
			}
		}

		private Command CommandObject
		{
			get
			{
				object command =  m_mediator.CommandSet[m_idOfCorrespondingCommand];
				if (command != null)
					return (Command)command;
				else
					throw new ConfigurationException("This node references the command '"+m_idOfCorrespondingCommand +"' which was not defined. ", m_configurationNode);
			}
		}


		public override Keys Shortcut
		{
			get
			{
				return CommandObject.Shortcut;
			}
		}

		#endregion


		override public UIItemDisplayProperties GetDisplayProperties()
		{
			return QueryDisplayProperties(m_parent, m_mediator, CommandObject, m_defaultVisible, Label);
		}


		/// <summary>
		/// Find out the enabled state and other display stuff from interested colleagues
		/// </summary>
		/// <remarks>Used by both this class (e.g. menus and toolbars) and the xwindow key handling</remarks>
		/// <param name="mediator"></param>
		/// <param name="command"></param>
		/// <param name="defaultVisible"></param>
		/// <param name="label"></param>
		/// <returns></returns>
		static public UIItemDisplayProperties QueryDisplayProperties(Mediator mediator, Command command, bool defaultVisible, string label)
		{
			ChoiceGroup group = null;
			return QueryDisplayProperties(group, mediator, command, defaultVisible, label);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="group">This provides more context for colleagues to know whether to add a command to a menu.
		/// colleagues who want to support short-cut keys should be able to handle <c>null</c>, apart from a context menu group.</param>
		/// <param name="mediator"></param>
		/// <param name="command"></param>
		/// <param name="defaultVisible"></param>
		/// <param name="label"></param>
		/// <returns></returns>
		private static UIItemDisplayProperties QueryDisplayProperties(ChoiceGroup group, Mediator mediator, Command command, bool defaultVisible, string label)
		{
			// Let the default be that it is enabled if we know that it has
			//at least one potential receiver, based on the method signatures of the
			//current set of colleagues.
			//If one of those colleagues thinks that it should be disabled at the moment,
			//then it needs to implement the corresponding Display method
			//and disable it from there.
			bool hasReceiver = mediator.HasReceiver(command.MessageString);
			UIItemDisplayProperties display = new UIItemDisplayProperties(group, label, hasReceiver, command.IconName, defaultVisible);

			//OK, this is a little non-obvious
			//first we allow anyone who knows about this specific command to influence how it is displayed
			//why was it this way?			m_mediator.SendMessage("Display"+this.m_idOfCorrespondingCommand, CommandObject, ref display);
			mediator.SendMessage("Display" + command.Id, command, ref display);


			//but then, we also allow anyone who knows about this specific message that would be sent
			//to control how it is displayed.  What's the difference?
			//Well, it is not uncommon for a single message, e.g. "InsertRecord", to be associated with
			//multiple commands, e.g. "CmdInsertPersonRecord", "CmdInsertCompanyRecord".
			//And in this case, there may not be any actual code which knows about one of these commands,
			//instead the code may be written to just listen  for the "InsertRecord" message and then act
			//upon its arguments which, in this example, would cause it to either insert a person or a company.
			mediator.SendMessage("Display" + command.MessageString, command, ref display);
			return display;
		}

		override public void OnClick(object sender, System.EventArgs args)
		{
			CommandObject.InvokeCommand();
			//			HandleClick();
		}

		//		override public void HandleClick()
		//		{
		//		}
	}
	#endregion

	#region BoolPropertyChoice
	//--------------------------------------------------------------------
	/// <summary>
	/// BoolPropertyChoice represents a menu item or button which toggles
	/// a boolean (true/false) property
	/// </summary>
	//--------------------------------------------------------------------
	public class BoolPropertyChoice : ChoiceBase
	{
		public BoolPropertyChoice( Mediator mediator,  XmlNode configurationNode, IUIAdapter adapter, ChoiceGroup parent)
			: base(mediator,  configurationNode,  adapter, parent)
		{
		}


		#region Properties

		public string Value
		{
			get
			{
				return XmlUtils.GetAttributeValue(m_configurationNode, "value", "");
			}
		}

		public string BoolPropertyName
		{
			get
			{
				return XmlUtils.GetAttributeValue(m_configurationNode, "boolProperty", "");
			}
		}

		public bool DefaultBoolPropertyValue
		{
			get
			{
				return "true" == XmlUtils.GetAttributeValue(m_configurationNode, "defaultBoolPropertyValue", "false");
			}
		}


		public bool BoolPropertyValue
		{
			get
			{
					return 	m_mediator.PropertyTable.GetBoolProperty(BoolPropertyName, DefaultBoolPropertyValue, this.PropertyTableGroup);
			}
		}


		public bool Checked
		{
			get
			{
				// JohnT: this is bizarre, but I've seen it sometimes on shutdown.
				if (m_mediator == null || m_mediator.PropertyTable == null)
					return DefaultBoolPropertyValue;
				return m_mediator.PropertyTable.GetBoolProperty(BoolPropertyName, DefaultBoolPropertyValue, this.PropertyTableGroup) == true;
			}
		}

		override public string HelpId
		{
			get
			{
				return this.BoolPropertyName;
			}
		}
		#endregion Properties


		override public void OnClick(object sender, System.EventArgs args)
		{
			//if we are a a boolProperty widget, the parent will call us back at HandleClick()
			Debug.Assert(this.BoolPropertyName != "");
			//toggle our value
			m_mediator.PropertyTable.SetProperty(BoolPropertyName, !this.BoolPropertyValue, this.PropertyTableGroup);
		}

		/// <summary>
		/// allow a colleague to enable or disabled this item
		/// </summary>
		/// <returns></returns>
		override public UIItemDisplayProperties GetDisplayProperties()
		{
			//review: the enabled parameter is set to the same value as the defaultVisible
			// value so that only defaultVisible items are visible by default.  Previously
			// enabled items would be 'visible' and enabled was true by default.
			UIItemDisplayProperties display =new UIItemDisplayProperties(m_parent, this.Label,
				this.m_defaultVisible, ImageName, this.m_defaultVisible);
			display.Checked = this.Checked;
			m_mediator.SendMessage("Display"+this.BoolPropertyName, null, ref display);
			if (display.Text.StartsWith("$"))
			{
				int iOfEquals = display.Text.IndexOf("=");
				if (iOfEquals > 0)
					display.Text = display.Text.Substring(iOfEquals + 1);
				else
					throw new ConfigurationException("Parameter (" + display.Text + ") needs format $label=<value>");
			}
			return display;
		}

	}
	#endregion

	#region StringPropertyChoice
	//--------------------------------------------------------------------
	/// <summary>
	/// StringPropertyChoice a button which changes the value of the property.
	/// It is something of an afterthought that is still on probation in Jan 2004...
	/// we made it to allow us to have a toolbar item which would choose from
	/// what would otherwise be a list of choices (e.g. Views in xworks).
	///
	/// Later. TODO: combine this and ListPropertyChoice. this is a fair
	/// amount of work, so I'm choosing not to mess with it now, just before our
	/// February meeting.
	/// </summary>
	//--------------------------------------------------------------------
	public class StringPropertyChoice : ChoiceBase
	{
		public StringPropertyChoice( Mediator mediator,  XmlNode configurationNode, IUIAdapter adapter, ChoiceGroup parent)
			: base(mediator,  configurationNode,  adapter, parent)
		{
		}


		#region Properties


		public string Value
		{
			get
			{
				return XmlUtils.GetAttributeValue(m_configurationNode, "value", "");
			}
		}

		public string PropertyName
		{
			get
			{
				return XmlUtils.GetAttributeValue(m_configurationNode, "property", "");
			}
		}

		//		public bool DefaultBoolPropertyValue
		//		{
		//			get
		//			{
		//				return "true" == XmlUtils.GetAttributeValue(m_configurationNode, "defaultBoolPropertyValue", "false");
		//			}
		//		}
//		public string ImageName
//		{
//			get
//			{
//				return XmlUtils.GetAttributeValue(m_configurationNode, "icon", "default");
//			}
//		}

		public string PropertyValueSetting
		{
			get
			{
				return XmlUtils.GetAttributeValue(m_configurationNode, "value", "");
			}
		}

		public XmlNode ParameterNode
		{
			get
			{
				return m_configurationNode.FirstChild;
			}
		}

		public bool Checked
		{
			get
			{
				Object x = m_mediator.PropertyTable.GetValue(PropertyName, this.PropertyTableGroup);
				return x!=null && (string)x== Value;
			}
		}
		#endregion Properties


		override public void OnClick(object sender, System.EventArgs args)
		{
			ChoiceGroup.ChooseSinglePropertyAtomicValue(m_mediator, this.Value, this.ParameterNode, this.PropertyName, this.PropertyTableGroup);
		}

		/// <summary>
		/// allow a colleague to enable or disabled this item
		/// </summary>
		/// <returns></returns>
		override public UIItemDisplayProperties GetDisplayProperties()
		{
			UIItemDisplayProperties display =new UIItemDisplayProperties(m_parent, this.Label, true, ImageName, this.m_defaultVisible);
			display.Checked = this.Checked;
			m_mediator.SendMessage("Display"+this.PropertyName, null, ref display);

			return display;
		}

	}
	#endregion

	#region ListPropertyChoice
	//--------------------------------------------------------------------
	/// <summary>
	/// ListPropertyChoice represents a menu item or button which can set
	/// the property of the parent list to a particular value.
	/// </summary>
	/// <remarks> this is perhaps poorly named, because the property
	/// itself is not a list; there is just a list of values.
	/// Maybe "PropertyListChoice" would be better?</remarks>
	//--------------------------------------------------------------------
	//TODO: this class is unlike all of the others, in that it does not have a valid
	//m_configurationNote; this is because it is often dynamically generated
	//therefore, the class structure may need refactoring.
	public class ListPropertyChoice : ChoiceBase
	{
		#region fields
		protected ListItem m_listItem;
		static bool m_fHandlingClick = false;	// prevent multiple simultaneous OnClick operations.
		#endregion

		public ListPropertyChoice( Mediator mediator, ListItem listItem, IUIAdapter adapter, ChoiceGroup parent)
			: base(mediator,  adapter, parent)
		{
			m_listItem = listItem;
		}


		#region Properties

		/// <summary>
		/// currently, this is used for unit testing.it may be used for more in the future.
		/// </summary>
		override public string Id
		{
			get
			{
				// Check for a null label.  See LT-9007.
				if (String.IsNullOrEmpty(m_listItem.label))
					return m_listItem.label;
				else
					return m_listItem.label.Replace("_","");//remove underscores
			}
		}

		/// <summary>
		/// Return whether the menu item should be enabled.
		/// </summary>
		public bool Enabled
		{
			get { return m_listItem.enabled; }
		}
		public override string ToString()
		{
			return Label;
		}
		override public string Label
		{
			get
			{
				return m_listItem.label;
			}
		}
		public override string ImageName
		{
			get
			{
				return m_listItem.imageName;
			}
		}

		public string Value
		{
			get
			{
				return m_listItem.value;
			}
		}

		public XmlNode ParameterNode
		{
			get
			{
				return m_listItem.parameterNode;
			}
		}
		/// <summary>
		/// the property of its parent group (which makes sense only if it is part of any group
		/// which controls a property.) (e.g. parent's "behavior" is something other than "pushOnly")
		/// </summary>
		public string ParentProperty
		{
			get
			{
				return m_parent.PropertyName;
			}
		}

		public bool Checked
		{
			get
			{
				return this.m_parent.IsValueSelected(this.Value);
			}
		}
		#endregion Properties


		override public void OnClick(object sender, System.EventArgs args)
		{
			// the boolean is a crude attempt to handle timing problems caused by clicking
			// before the previous click is done being handled.  so... ignore the second
			// click altogether rather than crash!  :-)
			// See Jira LT-587 for the initial bug report describing this problem (along
			// with another independent bug).
			if (!m_fHandlingClick)
			{
				m_fHandlingClick = true;
				//parent gets first crack at it, then calls us back if we should handle it.
				m_parent.HandleItemClick(this);
				m_fHandlingClick = false;
			}
		}


		override public UIItemDisplayProperties GetDisplayProperties()
		{
			UIItemDisplayProperties display =new UIItemDisplayProperties(m_parent, this.Label, this.Enabled, ImageName, this.m_defaultVisible);
			display.Checked = this.Checked;

			//
			//			//TODO: do some polling (not clear what we want to do...
			//			// may be structure the name based on the name of the property value,
			//			//but then get the specific list item into the parameters somehow?
			//
			//			//unlike the command parameters, we can go ahead and allow these
			//			//to be enabled and they will work just fine.  So, until we ran into a situation
			//			//where we need to disable some of these, let's just let them always be enabled.
			//
			//			//when we run into a case where we need to control them, then it should be clear
			//			//how to deal with this.
			//			display.Checked = Checked;
			//
			return display;
		}

	}
	#endregion

	#region NullPropertyChoice
	//--------------------------------------------------------------------
	/// <summary>
	/// SeparatorChoice represents separator menus
	/// </summary>
	//--------------------------------------------------------------------
	public class SeparatorChoice : ChoiceBase
	{
		public SeparatorChoice( Mediator mediator,  XmlNode configurationNode, IUIAdapter adapter, ChoiceGroup parent)
			: base(mediator,  configurationNode,  adapter, parent)
		{
		}
		override public void OnClick(object sender, System.EventArgs args)
		{
			Debug.Fail("this should never be called");
		}
		override public UIItemDisplayProperties GetDisplayProperties()
		{
			Debug.Fail("this should never be called");
			return null;
		}
	}
	#endregion
}
