using System;
using System.Collections;
using System.Diagnostics;
using System.Xml;

using SIL.Utils;
using XCore;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.XWorks.LexText
{
	/// <summary>
	/// Summary description for AreaListener.
	/// </summary>
	[XCore.MediatorDispose]
	public class AreaListener : IxCoreColleague, IFWDisposable
	{
		protected XCore.Mediator m_mediator;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="AreaListener"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public AreaListener()
		{
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

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
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~AreaListener()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_mediator != null)
					m_mediator.RemoveColleague(this);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		public void Init(Mediator mediator, XmlNode configurationParameters)
		{
			CheckDisposed();

			m_mediator = mediator;
			mediator.AddColleague(this);
		}

		public void OnPropertyChanged(string name)
		{
			CheckDisposed();

			switch(name)
			{
				default:
					break;
				/* remember, what XCore thinks of as a "Content Control", is what this AreaManager sees as a "tool".
					* with that in mind, this case is invoked when the user chooses a different tool.
					* the purpose of this code is to then store the name of that tool so that
					* next time we come back to this area, we can remember to use this same tool.
					*/
				case "currentContentControlObject":
					string toolName = m_mediator.PropertyTable.GetStringProperty("currentContentControl", "");
					IxCoreContentControl c = (IxCoreContentControl)m_mediator.PropertyTable.GetValue("currentContentControlObject");
					m_mediator.PropertyTable.SetProperty("ToolForAreaNamed_" + c.AreaName, toolName);
					m_mediator.Log("Switched to " + toolName);
					break;

				case "areaChoice":
					string areaName = m_mediator. PropertyTable.GetStringProperty("areaChoice", null);

					if(areaName == null || areaName == "")
						break;//this can happen when we use this property very early in the initialization

					//for next startup
					m_mediator.PropertyTable.SetProperty("InitialArea", areaName);

					ActivateToolForArea(areaName);
					break;
			}
		}

		/// <summary>
		/// return an array of all of the objects which should
		/// 1) be queried when looking for someone to deliver a message to
		/// 2) be potential recipients of a broadcast
		/// </summary>
		/// <returns></returns>
		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			return new IxCoreColleague[]{this};
		}

		public bool OnDisplayLexicalToolsList(object parameters, ref UIListDisplayProperties display)
		{
			CheckDisposed();

			return FillList(display, "lexicon");
		}

		public bool OnDisplayGrammarToolsList(object parameters, ref UIListDisplayProperties display)
		{
			CheckDisposed();

			return FillList(display, "grammar");
		}

		public bool OnDisplayWordToolsList(object parameters, ref UIListDisplayProperties display)
		{
			CheckDisposed();

			return FillList(display, "textsWords");
		}
		public bool OnDisplayTextToolsList(object parameters, ref UIListDisplayProperties display)
		{
			CheckDisposed();

			return FillList(display, "textsWords");
		}

		public bool OnDisplayListsToolsList(object parameters, ref UIListDisplayProperties display)
		{
			CheckDisposed();

			if(FillList(display, "lists"))
			{
				// By now the labels in this List are localized, so if we sort we get what
				// we want; a localized sorted list of lists [LT-9579]
				display.List.Sort();
				return true;
			}
			return false;
		}

		private bool FillList(UIListDisplayProperties display, string areaId)
		{
			//don't bother refreshing this list.
			if (display.List.Count > 0)
				return true;
			XmlNode windowConfiguration = (XmlNode)m_mediator. PropertyTable.GetValue("WindowConfiguration");
			StringTable tbl = null;
			if (m_mediator != null && m_mediator.HasStringTable)
				tbl = m_mediator.StringTbl;
			foreach (XmlNode node in windowConfiguration.SelectNodes(GetToolXPath(areaId)))
			{
				string label = XmlUtils.GetLocalizedAttributeValue(tbl, node, "label", "???");
				string value = XmlUtils.GetAttributeValue(node, "value", "???");
				string imageName = XmlUtils.GetAttributeValue(node, "icon");//can be null
				XmlNode controlElement = node.SelectSingleNode("control");
				display.List.Add(label, value, imageName, controlElement);
			}
			return true;
		}

		private string GetToolXPath(string areaId)
		{
			if(areaId == null)
				return "//item/parameters/tools/tool";

			else
				return "//item[@value='"+areaId + "']/parameters/tools/tool";
		}

		/// <summary>
		/// this is called by xWindow just before it sets the initial control which will actually
		/// take over the content area.
		/// </summary>
		/// <param name="windowConfigurationNode"></param>
		/// <returns></returns>
		public bool OnSetInitialContentObject(object windowConfigurationNode)
		{
			CheckDisposed();

			string areaName = m_mediator.PropertyTable.GetStringProperty("InitialArea", "");
			Debug.Assert( areaName !="", "The configuration files should set a default for 'InitialArea' under <defaultProperties>");

			// if an old configuration is preserving an obsolete InitialArea, reset it now, so we don't crash (cf. LT-7977)
			if (!IsValidAreaName(areaName))
			{
				areaName = "lexicon";
				if (!IsValidAreaName(areaName))
					throw new ApplicationException(String.Format("could not find default area name '{0}'", areaName));
			}

			//this will cause our "onPropertyChanged" method to fire, and it will then set the tool appropriately.
			m_mediator.PropertyTable.SetProperty("areaChoice", areaName);
			m_mediator.PropertyTable.SetPropertyPersistence("areaChoice", false);

			return true;	//we handled this.
		}


		/// <summary>
		/// Tests whether the parameters node for the given areaName is still valid. Changing areas can cause a crash
		/// if we don't check that they're valid (cf. LT-7977).
		/// </summary>
		/// <param name="areaName"></param>
		/// <returns></returns>
		private bool IsValidAreaName(string areaName)
		{
			XmlNode areaParams;
			return TryGetAreaParametersNode(areaName, out areaParams);
		}

		/// <summary>
		/// Get the parameters node for the given areaName
		/// </summary>
		/// <param name="areaName"></param>
		/// <param name="areaParams"></param>
		/// <returns>true if we could find an area item with parameters </returns>
		private bool TryGetAreaParametersNode(string areaName, out XmlNode areaParams)
		{
			XmlNode windowConfiguration = (XmlNode)m_mediator.PropertyTable.GetValue("WindowConfiguration");
			areaParams = ((XmlNode)windowConfiguration).SelectSingleNode("//lists/list[@id='AreasList']/item[@value='" + areaName + "']/parameters");
			return areaParams != null;
		}

		private void ActivateToolForArea(string areaName)
		{
			object current = m_mediator.PropertyTable.GetValue("currentContentControlObject");
			if (current != null && ((IxCoreContentControl)current).AreaName == areaName)
				return;//we are already in a control of this area, don't change anything.

			string toolName;
			XmlNode node = GetToolNodeForArea(areaName, out toolName);
			m_mediator.PropertyTable.SetProperty("currentContentControlParameters", node.SelectSingleNode("control"));
			m_mediator.PropertyTable.SetPropertyPersistence("currentContentControlParameters", false);
			m_mediator.PropertyTable.SetProperty("currentContentControl", toolName);
			m_mediator.PropertyTable.SetPropertyPersistence("currentContentControl", false);
		}

		private XmlNode GetToolNodeForArea(string areaName, out string toolName)
		{
			string property = "ToolForAreaNamed_" + areaName;
			toolName = m_mediator.PropertyTable.GetStringProperty(property, "");
			if (toolName == "")
				throw new ConfigurationException("There must be a property named " + property + " in the <defaultProperties> section of the configuration file.");

			XmlNode node;
			if (!TryGetToolNode(areaName, toolName, out node))
			{
				// the tool must be obsolete, so just get the default tool for this area
				XmlNode windowConfiguration = (XmlNode)m_mediator.PropertyTable.GetValue("WindowConfiguration");
				toolName = windowConfiguration.SelectSingleNode("//defaultProperties/property[@name='" + property + "']/@value").InnerText;
				if (!TryGetToolNode(areaName, toolName, out node))
					throw new ConfigurationException("There must be a property named " + property + " in the <defaultProperties> section of the configuration file.");
			}
			return node;
		}

		private bool TryGetToolNode(string areaName, string toolName, out XmlNode node)
		{
			string xpath = GetToolXPath(areaName) + "[@value = '" + toolName + "']";
			XmlNode windowConfiguration = (XmlNode)m_mediator.PropertyTable.GetValue("WindowConfiguration");
			node = windowConfiguration.SelectSingleNode(xpath);
			return node != null;
		}

		protected string GetCurrentAreaName()
		{
			return (string)m_mediator.PropertyTable.GetValue("areaChoice");
		}

		/// <summary>
		/// used by the link listener
		/// </summary>
		/// <returns></returns>
		public bool OnSetToolFromName(object toolName)
		{
			CheckDisposed();

			XmlNode node;
			if (!TryGetToolNode(null, (string)toolName, out node))
				throw new ApplicationException (String.Format(LexTextStrings.CannotFindToolNamed0, toolName));

			XmlNode windowConfiguration = (XmlNode)m_mediator.PropertyTable.GetValue("WindowConfiguration");
			// We might not be in the right area, so adjust that if needed (LT-4511).
			string area = GetCurrentAreaName();
			if (!IsToolInArea(toolName as string, area, windowConfiguration))
			{
				area = GetAreaNeededForTool(toolName as string, windowConfiguration);
				if (area != null)
				{
					// Before switching areas, we need to fix the tool recorded for that area,
					// otherwise ActivateToolForArea will override our tool choice with the last
					// tool active in the area (LT-4696).
					m_mediator.PropertyTable.SetProperty("ToolForAreaNamed_" + area, toolName);
					m_mediator.PropertyTable.SetProperty("areaChoice", area);
				}
			}
			else
			{
				// JohnT: when following a link, it seems to be important to set this, not just
				// the currentContentControl (is that partly obsolete?).
				if (area != null)
					m_mediator.PropertyTable.SetProperty("ToolForAreaNamed_" + area, toolName);
			}
			m_mediator.PropertyTable.SetProperty("currentContentControlParameters", node.SelectSingleNode("control"));
			m_mediator.PropertyTable.SetProperty("currentContentControl", toolName);
			return true;
		}

		private bool IsToolInArea(string toolName, string area, XmlNode windowConfiguration)
		{
			foreach (XmlNode node in windowConfiguration.SelectNodes(GetToolXPath(area)))
			{
				string value = XmlUtils.GetAttributeValue(node, "value", "???");
				if (value == toolName)
					return true;
			}
			return false;
		}

		private string GetAreaNeededForTool(string toolName, XmlNode windowConfiguration)
		{
			if (IsToolInArea(toolName, "lexicon", windowConfiguration))
				return "lexicon";
			if (IsToolInArea(toolName, "grammar", windowConfiguration))
				return "grammar";
			if (IsToolInArea(toolName, "textsWords", windowConfiguration))
				return "textsWords";
			if (IsToolInArea(toolName, "lists", windowConfiguration))
				return "lists";
			return null;
		}
	}
}
