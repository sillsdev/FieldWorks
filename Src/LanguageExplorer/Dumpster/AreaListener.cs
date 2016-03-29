// Copyright (c) 2012-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.Reporting;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using ConfigurationException = SIL.Utils.ConfigurationException;

namespace LanguageExplorer.Dumpster
{
#if RANDYTODO
	// TODO: I don't expect this class to survive, but its useful code moved elsewhere, as ordinary event handlers.
#endif
	/// <summary>
	/// Summary description for AreaListener.
	/// </summary>
	internal sealed class AreaListener : IFWDisposable
	{
		#region Member variables

		/// <summary>
		/// Keeps track of how many lists are loaded into List area
		/// memory windowConfiguration XML (including Custom ones).
		/// </summary>
		private int m_ctotalLists;

		/// <summary>
		/// Keeps track of how many Custom lists are loaded into List area
		/// memory windowConfiguration XML.
		/// </summary>
		private int m_ccustomLists;

		#endregion

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
		private void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			PropertyTable = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		private IPropertyTable PropertyTable { get; set; }

		/// <summary>
		/// This method is called BY REFLECTION through the mediator from LinkListener.FollowActiveLink, because the assembly dependencies
		/// are in the wrong direction. It finds the name of the tool we need to invoke to edit a given list.
		/// </summary>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public bool OnGetToolForList(object parameters)
		{
			var realParams = (object[]) parameters;
			var list = (ICmPossibilityList)realParams[0];
			var windowConfiguration = PropertyTable.GetValue<XmlNode>("WindowConfiguration");
			foreach (XmlNode tool in windowConfiguration.SelectSingleNode(GetListToolsXPath()).ChildNodes)
			{
				var toolName = XmlUtils.GetManditoryAttributeValue(tool, "value");
				var paramsNode = tool.SelectSingleNode(".//control/parameters[@clerk]");
				if (paramsNode == null)
					continue;
#if RANDYTODO
				var clerkNode = ToolConfiguration.GetClerkNodeFromToolParamsNode(paramsNode);
				if (clerkNode == null)
					continue;
				var listNode = clerkNode.SelectSingleNode("recordList");
				if (listNode == null)
					continue;
				var owner = XmlUtils.GetOptionalAttributeValue(listNode, "owner");
				var listName = XmlUtils.GetOptionalAttributeValue(listNode, "property");
				if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(listName))
					continue;
				var possibleList = PossibilityRecordList.GetListFromOwnerAndProperty(list.Cache, owner, listName);
				if (possibleList == list)
				{
					realParams[1] = toolName;
					return true;
				}
#endif
			}
			// If it's not a known list, try custom.
			realParams[1] = GetCustomListToolName(list);
			return true;
		}

		#region Custom List Methods

		/// <summary>
		/// For each list, create an XmlNode (or several) to plug into the
		/// windowConfiguration before it gets processed to display the lists
		/// in the Lists area.
		/// </summary>
		/// <param name="customLists"></param>
		/// <param name="windowConfig"></param>
		/// <remarks>'internal' for testing</remarks>
		internal void AddListsToWindowConfig(List<ICmPossibilityList> customLists,
											 XmlNode windowConfig)
		{
			// N.B. guaranteed to be at least one list
			foreach (var curList in customLists)
			{
				AddToolToConfigForList(curList, windowConfig);

				AddClerkToConfigForList(curList, windowConfig);

				AddCommandToConfigForList(curList, windowConfig);

				AddContextMenuEntryToConfigForList(curList, windowConfig);
			}
		}

		private static void AddContextMenuEntryToConfigForList(ICmPossibilityList curList, XmlNode windowConfig)
		{
			// Make a context menu node
			var ctxtMenuNode = CreateContextMenuNode(curList);

			// Need to add it to the windowConfig before we click on the list
			windowConfig.SelectSingleNode(GetContextMenusXPath()).AppendChild(
				windowConfig.OwnerDocument.ImportNode(ctxtMenuNode, true));
		}

		private static void AddToolToConfigForList(ICmPossibilityList curList,
												   XmlNode windowConfig)
		{
			// Make a custom Control node
			var controlNode = CreateCustomControlNode(curList);
			// Get a custom Tool node
			var toolNode = CreateCustomToolNode(curList, controlNode);

			// Need to add it to the windowConfig in order to CreateClerk later
			var importedToolNode = windowConfig.OwnerDocument.ImportNode(toolNode, true);
			windowConfig.SelectSingleNode(GetListToolsXPath()).AppendChild(importedToolNode);
			//// Add tool to display after importing it to windowConfig!
			//var label = GetCustomListLabel(curList, false);
			//var value = GetCustomListToolName(curList);
			//const string sbsview = "SideBySideView";
			//display.List.Add(label, value, sbsview, importedToolNode.SelectSingleNode("control"));
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="see REVIEW comment - code is possibly wrong")]
		private void AddClerkToConfigForList(ICmPossibilityList curList, XmlNode windowConfig)
		{
			// Put the clerk node in the window configuration for this list
			var clerkNode = CreateCustomClerkNode(curList);
			windowConfig.SelectSingleNode(GetListClerksXPath()).AppendChild(
				windowConfig.OwnerDocument.ImportNode(clerkNode, true));

			// THEN create the clerk
			var toolParamNodeXPath = GetToolParamNodeXPath(curList);
			XmlNode x = windowConfig.SelectSingleNode(toolParamNodeXPath);
			if (x == null)
				x = FindToolParamNode(windowConfig, curList);
#if RANDYTODO
			// TODO: I'm not sure who the "I" is, but clerks are disposed by the PropertyTable.
			// REVIEW: I'm not sure where the created RecordClerk gets disposed
			RecordClerkFactory.CreateClerk(PropertyTable, Publisher, Subscriber, true);
#endif
		}

		private void AddCommandToConfigForList(ICmPossibilityList curList, XmlNode windowConfig)
		{
			// Create a new command node
			var cmdNode = CreateCustomCommandNode(curList);
			var cmdNodeImported = windowConfig.OwnerDocument.ImportNode(cmdNode, true);

			// Put the command node in the window configuration
			windowConfig.SelectSingleNode(GetCommandsXPath()).AppendChild(cmdNodeImported);

#if RANDYTODO
			// Add the command to the mediator
			var command = new Command(m_mediator, cmdNodeImported);
			Debug.Assert(m_mediator.CommandSet != null,
						 "Empty mediator CommandSet. Should only occur in tests. Make sure it doesn't there either!");
			m_mediator.CommandSet.Add(command.Id, command);
#endif
		}

		private static XmlNode CreateCustomToolNode(ICmPossibilityList curList, XmlNode controlNode)
		{
			var label = XmlUtils.MakeSafeXmlAttribute(GetCustomListLabel(curList, false));
			var value = XmlUtils.MakeSafeXmlAttribute(GetCustomListToolName(curList));
			const string sbsview = "SideBySideView";
			var doc = new XmlDocument();
			doc.LoadXml(
				"<tool label=\"" + label + "\" value=\"" + value + "\" icon=\"" + sbsview + "\">"
				+ controlNode.OuterXml
				+ "</tool>");
			return doc.DocumentElement;
		}

		#region Static XPaths

		private static string GetToolXPath(string areaId)
		{
			if(areaId == null)
				return "//item/parameters/tools/tool";

			return "//item[@value='"+areaId + "']/parameters/tools/tool";
		}

		private static string GetListToolsXPath()
		{
			return "//item[@value='lists']/parameters/tools";
		}

		private static string GetContextMenusXPath()
		{
			return "//contextMenus";
		}

		private static string GetToolParamNodeXPath(ICmPossibilityList curList)
		{
			return "//item[@value='lists']/parameters/tools/tool[@value='" +
				XmlUtils.MakeSafeXmlAttribute(GetCustomListToolName(curList)) +
				"']/control/parameters/control/parameters";
		}

		/// <summary>
		/// Make up for weakness of XmlNode.SelectSingleNode.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private XmlNode FindToolParamNode(XmlNode windowConfig, ICmPossibilityList curList)
		{
			string toolname = GetCustomListToolName(curList);
			foreach (XmlNode node in windowConfig.SelectNodes("//item[@value='lists']/parameters/tools/tool"))
			{
				string value = XmlUtils.GetAttributeValue(node, "value");
				if (value == toolname)
				{
					XmlNode xn = node.SelectSingleNode("control/parameters/control/parameters");
					return xn;
				}
			}
			return null;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private XmlNode FindToolNode(XmlNode windowConfig, string areaName, string toolName)
		{
			foreach (XmlNode node in windowConfig.SelectNodes(GetToolXPath(areaName)))
			{
				string value = XmlUtils.GetAttributeValue(node, "value");
				if (value == toolName)
					return node;
			}
			return null;
		}

		private static string GetListClerksXPath()
		{
			return "//item[@value='lists']/parameters/clerks";
		}

		private static string GetCommandsXPath()
		{
			return "//commands";
		}

		#endregion

		private static XmlNode CreateContextMenuNode(ICmPossibilityList curList)
		{
			var toolName = XmlUtils.MakeSafeXmlAttribute(GetCustomListToolName(curList));
			var doc = new XmlDocument();
			doc.LoadXml(
				"<menu id=\"PaneBar-ShowHiddenFields-" + toolName + "\" label=\"\">"
				+ "<item label=\"Show Hidden Fields\" boolProperty=\"ShowHiddenFields-" + toolName + "\""
				+ " defaultVisible=\"true\" settingsGroup=\"local\"/>"
				+ "</menu>");
			return doc.DocumentElement;
		}

		private static XmlNode CreateCustomCommandNode(ICmPossibilityList curList)
		{
			var itemClass = curList.Cache.MetaDataCacheAccessor.GetClassName(curList.ItemClsid);
			var xmlString =
				"<command id=\"CmdJumpTo" + XmlUtils.MakeSafeXmlAttribute(GetCustomListClerkName(curList))
				+ "\" label=\"Show in {0} list\" message=\"JumpToTool\">"
				+ "<parameters tool=\"" + XmlUtils.MakeSafeXmlAttribute(GetCustomListToolName(curList)) +
				"\" className=\"" + XmlUtils.MakeSafeXmlAttribute(itemClass) + "\"/>"
				+ "</command>";
			var doc = new XmlDocument();
			doc.LoadXml(xmlString);
			return doc.DocumentElement;
		}

		private static XmlNode CreateCustomClerkNode(ICmPossibilityList curList)
		{
			var clerk = GetCustomListClerkName(curList);
			var hierarchy = curList.Depth > 1 ? "true" : "false";
			var includeAbbr = curList.DisplayOption == (int)PossNameType.kpntName ? false : true;
			string ws = curList.GetWsString();
			var xmlString =
				"<clerk id=\"" + XmlUtils.MakeSafeXmlAttribute(clerk) + "\">"
				+ "<recordList owner=\"unowned\" property=\"" + curList.Guid + "\">"
				+ "<dynamicloaderinfo assemblyPath=\"xWorks.dll\" class=\"SIL.FieldWorks.XWorks.PossibilityRecordList\"/>"
				+ "</recordList>";
			xmlString +=
				"<treeBarHandler assemblyPath=\"xWorks.dll\" expand=\"false\" hierarchical=\"" + hierarchy
				+ "\" includeAbbr=\"" + includeAbbr + "\" ws=\"" + ws + "\" "
				+ "class=\"SIL.FieldWorks.XWorks.PossibilityTreeBarHandler\"/>"
				+ "<filters/>";
			if (curList.IsSorted)
				xmlString +=
					"<sortMethods>"
					+ "<sortMethod label=\"Default\" assemblyPath=\"Filters.dll\""
					+ " class=\"SIL.FieldWorks.Filters.PropertyRecordSorter\" sortProperty=\"ShortName\"/>"
					+ "</sortMethods>";
			else
				xmlString += "<sortMethods/>";
			xmlString += "</clerk>";
			var doc = new XmlDocument();
			doc.LoadXml(xmlString);
			return doc.DocumentElement;
		}

		private static XmlNode CreateCustomControlNode(ICmPossibilityList curList)
		{
			var toolName = XmlUtils.MakeSafeXmlAttribute(GetCustomListToolName(curList));
			var clerk = XmlUtils.MakeSafeXmlAttribute(GetCustomListClerkName(curList));
			var doc = new XmlDocument();
			doc.LoadXml(
				"<control>"
				+ "<dynamicloaderinfo assemblyPath=\"LanguageExplorer.dll\" class=\"LanguageExplorer.Controls.PaneBarContainer\"/>"
				+ "<parameters PaneBarGroupId=\"PaneBar-ShowHiddenFields-"+ toolName + "\" collapse=\"144000\">"
				+   "<control>"
				+     "<dynamicloaderinfo assemblyPath=\"xWorks.dll\" class=\"SIL.FieldWorks.XWorks.RecordEditView\"/>"
				+     "<parameters area=\"lists\" clerk=\"" + clerk + "\""
				+      " filterPath=\"Language Explorer\\Configuration\\Lists\\Edit\\DataEntryFilters\\completeFilter.xml\""
				+	   " persistContext=\"listsEdit\" layout=\"CmPossibility\" emptyTitleId=\"No-ListItems\"/>"
				+ "</control>"
				+ "</parameters>"
				+ "</control>");
			return doc.DocumentElement;
		}

		private static string GetCustomListLabel(ICmPossibilityList curList, bool ftrim)
		{
			if (!ftrim)
				return curList.Name.BestAnalysisAlternative.Text;

			// if ftrim is 'true' we want to take out all whitespace in the list name
			return curList.Name.BestAnalysisAlternative.Text.Replace(" ", string.Empty);
		}

		private static string GetCustomListToolName(ICmPossibilityList curList)
		{
			return GetCustomListLabel(curList, true) + "Edit";
		}

		private static string GetCustomListClerkName(ICmPossibilityList curList)
		{
			return GetCustomListLabel(curList, true) + "List";
		}

		#endregion

		/// <summary>
		/// This is called by CustomListDlg to get a new/modified list to show up in the tools list.
		/// </summary>
		/// <param name="areaId"></param>
		/// <returns></returns>
		public bool OnReloadAreaTools(object areaId)
		{
			CheckDisposed();

			var areaName = (string) areaId;

			if (!IsValidAreaName(areaName))
				throw new ApplicationException(String.Format("Unknown area name '{0}'", areaName));

			switch (areaName)
			{
				default:
					// don't do anything
					break;
				case "lists":
					// Need to do a 'real' refresh of the UI of the current window

					// Enhance: if we could only refresh the sidebar of all the active windows,
					// that would be better.
					if (PropertyTable == null)
						throw new ApplicationException("PropertyTable is null.");

					// don't try to use ReplaceMainWindow on all the windows, only the active one!
					var app = PropertyTable.GetValue<IFlexApp>("App");
					var win = (IFwMainWnd)app.ActiveMainWindow;
					app.ReplaceMainWindow(win);
					break;
			}
			return true;
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
			var windowConfiguration = PropertyTable.GetValue<XmlNode>("WindowConfiguration");
			areaParams = windowConfiguration.SelectSingleNode("//lists/list[@id='AreasList']/item[@value='" + areaName + "']/parameters");
			return areaParams != null;
		}

		/// <summary>
		/// This is designed to be called by reflection through the mediator, when something typically in xWorks needs to get
		/// the parameter node for a given tool. The last argument is a one-item array used to return the result,
		/// since I don't think we handle Out parameters in our SendMessage protocol.
		/// </summary>
		public bool OnGetContentControlParameters(object parameterObj)
		{
			var param = parameterObj as Tuple<string, string, XmlNode[]>;
			if (param == null)
				return false; // we sure can't handle it; should we throw?
			string area = param.Item1;
			string tool = param.Item2;
			XmlNode[] result = param.Item3;
			XmlNode node;
			if (TryGetToolNode(area, tool, out node))
			{
				result[0] = node.SelectSingleNode("control");
			}
			return true; // whatever happened, we did the best that can be done.
		}

		private bool TryGetToolNode(string areaName, string toolName, out XmlNode node)
		{
			string xpath = GetToolXPath(areaName) + "[@value = '" + XmlUtils.MakeSafeXmlAttribute(toolName) + "']";
			var windowConfiguration = PropertyTable.GetValue<XmlNode>("WindowConfiguration");
			node = windowConfiguration.SelectSingleNode(xpath);
			if (node == null)
				node = FindToolNode(windowConfiguration, areaName, toolName);
			return node != null;
		}

		private string GetCurrentAreaName()
		{
			return PropertyTable.GetValue<string>("areaChoice");
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
				throw new ApplicationException (String.Format(LanguageExplorerResources.CannotFindToolNamed0, toolName));

			var windowConfiguration = PropertyTable.GetValue<XmlNode>("WindowConfiguration");
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
					PropertyTable.SetProperty("ToolForAreaNamed_" + area, toolName, true, true);
					PropertyTable.SetProperty("areaChoice", area, true, true);
				}
			}
			else
			{
				// JohnT: when following a link, it seems to be important to set this, not just
				// the currentContentControl (is that partly obsolete?).
				if (area != null)
				{
					PropertyTable.SetProperty("ToolForAreaNamed_" + area, toolName, true, true);
				}
			}
			PropertyTable.SetProperty("currentContentControlParameters", node.SelectSingleNode("control"), true, true);
			PropertyTable.SetProperty("currentContentControl", toolName, true, true);
			return true;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private static bool IsToolInArea(string toolName, string area, XmlNode windowConfiguration)
		{
			XmlNodeList nodes = windowConfiguration.SelectNodes(GetToolXPath(area));
			if (nodes != null)
			{
				foreach (XmlNode node in nodes)
				{
					string value = XmlUtils.GetOptionalAttributeValue(node, "value", "???");
					if (value == toolName)
						return true;
				}
			}
			return false;
		}

		private static string GetAreaNeededForTool(string toolName, XmlNode windowConfiguration)
		{
			if (IsToolInArea(toolName, "lexicon", windowConfiguration))
				return "lexicon";
			if (IsToolInArea(toolName, "grammar", windowConfiguration))
				return "grammar";
			if (IsToolInArea(toolName, "textsWords", windowConfiguration))
				return "textsWords";
			if (IsToolInArea(toolName, "lists", windowConfiguration))
				return "lists";
			if (IsToolInArea(toolName, "notebook", windowConfiguration))
				return "notebook";
			return null;
		}
	}
}
