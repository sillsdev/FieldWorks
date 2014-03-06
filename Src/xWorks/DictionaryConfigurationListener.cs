// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class handles the menu sensitivity and function for the dictionary configuration items under Tools->Configure
	/// </summary>
	class DictionaryConfigurationListener : IxCoreColleague
	{
		private Mediator m_mediator;

		public void Init(Mediator mediator, XmlNode configurationParameters)
		{
			m_mediator = mediator;
			m_mediator.AddColleague(this);
		}

		public IxCoreColleague[] GetMessageTargets()
		{
			var targets = new List<IxCoreColleague> { this };
			return targets.ToArray();
		}

		/// <summary>
		/// The configure dictionary dialog may be launched any time this tool is active.
		/// Its name is derived from the name of the tool.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayConfigureDictionary(object commandObject,
																		 ref UIItemDisplayProperties display)
		{
			var configurationName = GetDictionaryConfigurationType(m_mediator);
			if(InFriendlyArea && configurationName != null)
			{
				display.Enabled = true;
				display.Visible = true;
				// REVIEW: SHOULD THE "..." BE LOCALIZABLE (BY MAKING IT PART OF THE SOURCE FOR display.Text)?
				display.Text = String.Format(display.Text, configurationName+"...");
			}
			else
			{
				display.Enabled = false;
				display.Visible = false;
			}

			return true; //we've handled this
		}

		/// <summary>
		/// The old configure dialog should not be accessable for tools where the new one has been implemented.
		/// This hides the old menu if we are handling the type and passes the menu handling on to the old handlers otherwise.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayConfigureXmlDocView(object commandObject,
																		 ref UIItemDisplayProperties display)
		{
			if(GetDictionaryConfigurationType(m_mediator) != null)
			{
				display.Visible = false;
				return true;
			}
			return false;
		}

		internal static string GetDictionaryConfigurationType(Mediator mediator)
		{
			var toolName = mediator.PropertyTable.GetStringProperty("ToolForAreaNamed_lexicon", null);
			switch(toolName)
			{
				case "reversalToolBulkEditReversalEntries":
				case "reversalToolEditComplete":
					return "Reversal Index";
				case "lexiconBrowse":
				case "lexiconDictionary":
				case "lexiconEdit" :
					return "Dictionary";
				default:
					return null;
			}
		}

		/// <summary>
		/// Launch the configure dialog.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public bool OnConfigureDictionary(object commandObject)
		{
			using(var dlg = new DictionaryConfigurationDlg(m_mediator))
			{
				var controller = new DictionaryConfigurationController(dlg, m_mediator);
				var cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
				dlg.ShowDialog(m_mediator.PropertyTable.GetValue("window") as IWin32Window);
			}
			m_mediator.SendMessage("MasterRefresh", null);
			return true; // message handled
		}

		public bool ShouldNotCall { get; private set; }
		public int Priority { get { return (int)ColleaguePriority.High; } }

		/// <summary>
		/// Determine if the current area is relevant for this listener.
		/// </summary>
		/// <remarks>
		/// Dictionary configurations are only relevant in the Lexicon area.
		/// </remarks>
		protected bool InFriendlyArea
		{
			get
			{
				var areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice", null);
				return areaChoice == "lexicon";
			}
		}
	}
}
