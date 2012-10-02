//THIS IS CURRENTLY UNUSED!!!
//THIS IS CURRENTLY UNUSED!!!
//THIS IS CURRENTLY UNUSED!!!
//THIS IS CURRENTLY UNUSED!!!
//THIS IS CURRENTLY UNUSED!!!
//THIS IS CURRENTLY UNUSED!!!
using System;
using System. Diagnostics;
using System.Windows.Forms;
using System.Xml;
using System.Collections.Generic;
using SIL.Utils;

namespace XCore
{
#if BEINGUSED
	/// <summary>
	/// Summary description for PanelMaker.
	/// </summary>
	public class PanelMaker
	{
		protected Mediator m_mediator;
		public PanelMaker(Mediator mediator)
		{
			m_mediator= mediator;
		}

		public List<ListView> GetControlsFromChoice(ListPropertyChoice choice)
		{
			List<ListView> controls = new List<ListView>();
			if (choice.ParameterNode == null)
				return null;

			foreach(XmlNode panel in choice.ParameterNode.SelectNodes("panels/listPanel"))
			{
				string listId = XmlUtils.GetManditoryAttributeValue(panel, "listId");
				string label = XmlUtils.GetManditoryAttributeValue(panel, "label");

				ListView list = MakeList(listId, label);
				controls.Add(list);
			}
			return controls;
		}


		private ListView MakeList(string listId, string label)
		{
			ListView list= new ListView();
			list. View = View.List;
			list.Name = label;
			list.Dock = System.Windows.Forms.DockStyle.Top;
			list.MultiSelect = false;


			XCore.List xlist = new XCore.List(null); //(listNode);
			UIListDisplayProperties display =new UIListDisplayProperties(xlist);
			display.PropertyName= listId;
			m_mediator.SendMessage("Display"+listId, null, ref display);

			foreach (ListItem item in xlist)
			{
				ListViewItem x =list.Items.Add(item.label);
				x.Tag = new StringPropertyChoice(m_mediator, item.parameterNode, null, null);
			}

			list.SelectedIndexChanged+=new EventHandler(OnSelectedIndexChanged);

			return list;
		}

		private void OnSelectedIndexChanged(object sender, EventArgs e)
		{
			ListView list=(ListView) sender;
			if (list.SelectedIndices == null || list.SelectedIndices. Count == 0)
				return;

			ChoiceBase control = (ChoiceBase)list.SelectedItems[0].Tag;
			Debug.Assert(control != null);
			control.OnClick(this, null);
		}
	}
#endif
}