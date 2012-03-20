using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using XCore;
using SIL.FieldWorks.FDO.Application;
using HvoFlidKey = SIL.FieldWorks.FDO.HvoFlidKey;

namespace SIL.FieldWorks.XWorks.LexText
{
	/// <summary>
	/// Summary description for AreaListener.
	/// </summary>
	[MediatorDispose]
	public class AreaListener : IxCoreColleague, IFWDisposable
	{
		#region Member variables

		protected Mediator m_mediator;

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
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
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
			m_ctotalLists = 0;
			m_ccustomLists = 0;
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
					var c = (IxCoreContentControl)m_mediator.PropertyTable.GetValue("currentContentControlObject");
					m_mediator.PropertyTable.SetProperty("ToolForAreaNamed_" + c.AreaName, toolName);
					Logger.WriteEvent("Switched to " + toolName);
					break;

				case "areaChoice":
					string areaName = m_mediator. PropertyTable.GetStringProperty("areaChoice", null);

					if(string.IsNullOrEmpty(areaName))
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

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
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

		public bool OnDisplayNotebookToolsList(object parameters, ref UIListDisplayProperties display)
		{
			CheckDisposed();

			return FillList(display, "notebook");
		}

		/// <summary>
		/// Lists area was getting too different. It no longer goes through this
		/// version of FillList. For Lists area, see FillListAreaList().
		/// </summary>
		/// <param name="display"></param>
		/// <param name="areaId"></param>
		/// <returns></returns>
		private bool FillList(UIListDisplayProperties display, string areaId)
		{
			// Don't bother refreshing this list.
			if (display.List.Count > 0)
				return true;
			var windowConfiguration = (XmlNode)m_mediator.PropertyTable.GetValue("WindowConfiguration");
			StringTable tbl = null;
			if (m_mediator != null && m_mediator.HasStringTable)
				tbl = m_mediator.StringTbl;
			XmlNodeList nodes = windowConfiguration.SelectNodes(GetToolXPath(areaId));
			if (nodes != null)
			{
				foreach (XmlNode node in nodes)
				{
					string label = XmlUtils.GetLocalizedAttributeValue(tbl, node, "label", "???");
					string value = XmlUtils.GetAttributeValue(node, "value", "???");
					string imageName = XmlUtils.GetAttributeValue(node, "icon"); //can be null
					XmlNode controlElement = node.SelectSingleNode("control");
					display.List.Add(label, value, imageName, controlElement);
				}
			}
			return true;
		}

		public bool OnDisplayListsToolsList(object parameters, ref UIListDisplayProperties display)
		{
			CheckDisposed();

			if(FillListAreaList(display))
			{
				// By now the labels in this List are localized, so if we sort we get what
				// we want; a localized sorted list of lists [LT-9579]
				display.List.Sort();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Lists area listener was getting too different from other areas, so I made a
		/// separate version of FillList just for the Lists area.
		/// </summary>
		/// <param name="display"></param>
		/// <returns></returns>
		private bool FillListAreaList(UIListDisplayProperties display)
		{
			var customLists = GetListOfOwnerlessLists();
			var fcustomChanged = customLists.Count != m_ccustomLists;
			// Since we're in the 'Lists' area, don't bother refreshing this
			// list, unless the number of Custom lists has changed. This happens
			// whenever someone adds one.
			if (display.List.Count > 0 && !fcustomChanged)
				return true;

			// We can get here in the following cases:
			// Case 1: display.List is empty and m_ctotalLists == 0
			//       Load 'windowConfiguration' with all Custom lists,
			//       Update both list counts,
			//       Load 'display' with ALL lists.
			// Case 2: display.List is empty, but m_ctotalLists > 0
			//       We MAY have a recent Custom list addition to add.
			//       If 'fcustomChanged', load the new Custom list into 'windowConfiguration',
			//          and update both list counts,
			//       Load 'display' with ALL lists.
			// Case 3: display.List is loaded, but we have a recent Custom list addition to add.
			//       Load the new Custom list into 'windowConfiguration',
			//       Update both list counts,
			//       Only update 'display' with new Custom list.
			// N.B. This may need changing if we allow the user to DELETE Custom lists someday.
			var windowConfiguration = (XmlNode)m_mediator.PropertyTable.GetValue("WindowConfiguration");
			UpdateWinConfig(fcustomChanged, customLists, windowConfiguration);

			// Now update 'display'
			StringTable tbl = null;
			FdoCache cache = null;
			ICmPossibilityListRepository possRepo = null;
			if (m_mediator != null && m_mediator.HasStringTable)
				tbl = m_mediator.StringTbl;
			if (m_mediator != null)
				 cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			if (cache != null)
				possRepo = cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
			if (display.List.Count > 0)
			{
				var node = windowConfiguration.SelectSingleNode(GetListToolsXPath()).LastChild;
				if (node != null)
					AddToolNodeToDisplay(possRepo, cache, display, tbl, node);
			}
			else
			{
				var nodes = windowConfiguration.SelectNodes(GetToolXPath("lists"));
				if (nodes != null)
					foreach (XmlNode node in nodes)
					{
						if ((!FwUtils.IsTEInstalled) && XmlUtils.GetOptionalBooleanAttributeValue(node, "bteOnly", false))
							continue;
						AddToolNodeToDisplay(possRepo, cache, display, tbl, node);
					}
			}
			return true;
		}

		#region Custom List Methods

		private static void AddToolNodeToDisplay(ICmPossibilityListRepository possRepo, FdoCache cache,
												 UIListDisplayProperties display, StringTable tbl, XmlNode node)
		{
			// Modified how this works, so it uses the current UI version of the PossibilityList Name,
			// if possible.
			var localizedLabel = FindMatchingPossibilityListUIName(node, possRepo, cache);
			if (localizedLabel == null)
				localizedLabel = XmlUtils.GetLocalizedAttributeValue(tbl, node, "label", "???");
			var value = XmlUtils.GetAttributeValue(node, "value", "???");
			var imageName = XmlUtils.GetAttributeValue(node, "icon"); //can be null
			var controlElement = node.SelectSingleNode("control");
			display.List.Add(localizedLabel, value, imageName, controlElement);
		}

		private static string FindMatchingPossibilityListUIName(XmlNode toolNode,
																ICmPossibilityListRepository possRepo, FdoCache cache)
		{
			var recordListNode = GetClerkRecordListNodeFromToolNode(toolNode);
			if (recordListNode == null)
				return null;
			var ownerAttr = XmlUtils.GetAttributeValue(recordListNode, "owner");
			var propertyAttr = XmlUtils.GetAttributeValue(recordListNode, "property");
			ICmPossibilityList possList;
			if (ownerAttr == "unowned")
			{
				// If its owner is "unowned" then the property is the list's guid
				possList = GetListByGuid(possRepo, propertyAttr);
			}
			else
			{
				// If it has an owner and property, get the right list's hvo via SDA
				possList = GetListBySda(cache, ownerAttr, propertyAttr);
			}
			return possList == null ? null : possList.Name.UserDefaultWritingSystem.Text;
		}

		private static ICmPossibilityList GetListBySda(FdoCache cache,
													   string ownerAttr, string propertyAttr)
		{
			var mdc = cache.MetaDataCacheAccessor;
			var sda = cache.DomainDataByFlid;
			var className = ownerAttr;
			if (ownerAttr.Contains("FeatureSystem") || ownerAttr.Contains("ReversalIndex"))
				return null; // These don't lead to a PossibilityList
			var flid = mdc.GetFieldId(className, propertyAttr, true);
			var hvoMainObj = GetHvoFromXMLOwnerAttribut(cache, ownerAttr);
			if (flid == 0 || hvoMainObj == 0)
				return null;
			var listHvo = sda.get_ObjectProp(hvoMainObj, flid);
			return listHvo > 0 ? (ICmPossibilityList)cache.ServiceLocator.GetObject(listHvo) : null;
		}

		/// <summary>
		/// Finds a Major Object from the cache and returns its Hvo.
		/// If the search string is unknown, it returns zero.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="ownerAttr"></param>
		/// <returns></returns>
		private static int GetHvoFromXMLOwnerAttribut(FdoCache cache, string ownerAttr)
		{
			var hvoResult = 0;
			switch (ownerAttr)
			{
				case "LangProject":
				case "LanguageProject":
					hvoResult = cache.LangProject.Hvo;
					break;
				case "LexDb":
					hvoResult = cache.LangProject.LexDbOA.Hvo;
					break;
				case "DsDiscourseData":
					if (cache.LangProject.DiscourseDataOA == null)
						return 0;
					hvoResult = cache.LangProject.DiscourseDataOA.Hvo;
					break;
				case "RnResearchNbk":
					if (cache.LangProject.ResearchNotebookOA == null)
						return 0;
					hvoResult = cache.LangProject.ResearchNotebookOA.Hvo;
					break;
				default:
					break;
			}
			return hvoResult;
		}

		private static ICmPossibilityList GetListByGuid(ICmPossibilityListRepository possRepo, string listGuid)
		{
			return possRepo.GetObject(new Guid(listGuid));
		}

		private static XmlNode GetClerkRecordListNodeFromToolNode(XmlNode toolNode)
		{
			var clerkId = XmlUtils.GetAttributeValue(toolNode.SelectSingleNode("control//parameters[@clerk != '']"), "clerk");
			var clerkNode = toolNode.SelectSingleNode(GetListClerksXPath() + "/clerk[@id='"+XmlUtils.MakeSafeXmlAttribute(clerkId)+"']");
			if (clerkNode == null)
				clerkNode = FindClerkNode(toolNode, clerkId);
			return clerkNode == null ? null : clerkNode.SelectSingleNode("recordList");
		}

		/// <summary>
		/// Make up for weakness of XmlNode.SelectSingleNode.
		/// </summary>
		private static XmlNode FindClerkNode(XmlNode toolNode, string clerkId)
		{
			foreach (XmlNode node in toolNode.SelectNodes(GetListClerksXPath() + "/clerk"))
			{
				string id = XmlUtils.GetAttributeValue(node, "id");
				if (id == clerkId)
					return node;
			}
			return null;
		}

		private void UpdateWinConfig(bool fcustomChanged, List<ICmPossibilityList> customLists, XmlNode windowConfig)
		{
			// See caller FillListAreaList() for description of Case 1-3
			if (m_ctotalLists == 0) // Case 1
				LoadAllCustomLists(customLists, windowConfig);
			else // Case 2 and 3
				if (fcustomChanged) // Case 3 is automatically true
					AddACustomList(customLists[customLists.Count - 1], windowConfig);
		}

		private void AddACustomList(ICmPossibilityList customList, XmlNode windowConfig)
		{
			// Add 'customList' to windowConfig
			AddListsToWindowConfig(new List<ICmPossibilityList> {customList}, windowConfig);

			// We have to update this because other things besides 'tools' need to get set.
			UpdateMediatorConfig(windowConfig);

			m_ccustomLists++;
			m_ctotalLists++;
		}

		private void LoadAllCustomLists(List<ICmPossibilityList> customLists, XmlNode windowConfig)
		{

			AddListsToWindowConfig(customLists, windowConfig);

			// We have to update this because other things besides 'tools' need to get set.
			UpdateMediatorConfig(windowConfig);

			var nodes = windowConfig.SelectNodes(GetToolXPath("lists"));
			if (nodes != null)
				m_ctotalLists = nodes.Count;
			m_ccustomLists = customLists.Count;
			m_ctotalLists += m_ccustomLists;
		}

		private void UpdateMediatorConfig(XmlNode windowConfig)
		{
			// We have to update this because other things besides 'tools' need to get set.
			m_mediator.PropertyTable.SetProperty("WindowConfiguration", windowConfig);
			m_mediator.PropertyTable.SetPropertyPersistence("WindowConfiguration", false);
		}

		/// <summary>
		/// Use the Mediator to get the CmPossibilityList repo and find all the ownerless lists.
		/// </summary>
		/// <returns></returns>
		private List<ICmPossibilityList> GetListOfOwnerlessLists()
		{
			// Get the cache and ICmPossibilityListRepository via the mediator
			var cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			var repo = cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();

			//// Find all custom lists (lists that own CmCustomItems)
			//return repo.AllInstances().Where(
			//    list => list.ItemClsid == CmCustomItemTags.kClassId).ToList();

			//Find all custom lists (ownerless lists)
			//The above effort didn't include the Weather list.
			return repo.AllInstances().Where(
				list => list.Owner == null).ToList();
		}

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
			// REVIEW: I'm not sure where the created RecordClerk gets disposed
			RecordClerkFactory.CreateClerk(m_mediator, x, true);
		}

		private void AddCommandToConfigForList(ICmPossibilityList curList, XmlNode windowConfig)
		{
			// Create a new command node
			var cmdNode = CreateCustomCommandNode(curList);
			var cmdNodeImported = windowConfig.OwnerDocument.ImportNode(cmdNode, true);

			// Put the command node in the window configuration
			windowConfig.SelectSingleNode(GetCommandsXPath()).AppendChild(cmdNodeImported);

			// Add the command to the mediator
			var command = new Command(m_mediator, cmdNodeImported);
			Debug.Assert(m_mediator.CommandSet != null,
						 "Empty mediator CommandSet. Should only occur in tests. Make sure it doesn't there either!");
			m_mediator.CommandSet.Add(command.Id, command);
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
				+ " defaultVisible=\"false\" settingsGroup=\"local\"/>"
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
			string ws = GetWsString(curList);
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

		private static string GetWsString(ICmPossibilityList curList)
		{
			string ws;
			switch (curList.WsSelector)
			{
				case WritingSystemServices.kwsAnal: // fall through; shouldn't happen
				case WritingSystemServices.kwsAnals:
					ws = "best analysis";
					break;
				case WritingSystemServices.kwsVern: // fall through; shouldn't happen
				case WritingSystemServices.kwsVerns:
					ws = "best vernacular";
					break;
				case WritingSystemServices.kwsAnalVerns:
					ws = "best analorvern";
					break;
				case WritingSystemServices.kwsVernAnals:
					ws = "best vernoranal";
					break;
				default:
					throw new Exception("Unknown writing system code found.");
			}
			return ws;
		}

		private static XmlNode CreateCustomControlNode(ICmPossibilityList curList)
		{
			var toolName = XmlUtils.MakeSafeXmlAttribute(GetCustomListToolName(curList));
			var clerk = XmlUtils.MakeSafeXmlAttribute(GetCustomListClerkName(curList));
			var doc = new XmlDocument();
			doc.LoadXml(
				"<control>"
				+ "<dynamicloaderinfo assemblyPath=\"xCore.dll\" class=\"XCore.PaneBarContainer\"/>"
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

			ActivateToolForArea(areaName);

			return true;	//we handled this.
		}

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
					if (m_mediator == null)
						throw new ApplicationException("Mediator is null.");

					// don't try to use ReplaceMainWindow on all the windows, only the active one!
					var app = (LexTextApp)m_mediator.PropertyTable.GetValue("App");
					var win = (FwXWindow)app.ActiveMainWindow;
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
			var windowConfiguration = (XmlNode)m_mediator.PropertyTable.GetValue("WindowConfiguration");
			areaParams = windowConfiguration.SelectSingleNode("//lists/list[@id='AreasList']/item[@value='" + areaName + "']/parameters");
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
				var windowConfiguration = (XmlNode)m_mediator.PropertyTable.GetValue("WindowConfiguration");
				toolName = windowConfiguration.SelectSingleNode("//defaultProperties/property[@name='" + property + "']/@value").InnerText;
				if (!TryGetToolNode(areaName, toolName, out node))
					throw new ConfigurationException("There must be a property named " + property + " in the <defaultProperties> section of the configuration file.");
			}
			return node;
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
			var windowConfiguration = (XmlNode)m_mediator.PropertyTable.GetValue("WindowConfiguration");
			node = windowConfiguration.SelectSingleNode(xpath);
			if (node == null)
				node = FindToolNode(windowConfiguration, areaName, toolName);
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

			var windowConfiguration = (XmlNode)m_mediator.PropertyTable.GetValue("WindowConfiguration");
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

		private static bool IsToolInArea(string toolName, string area, XmlNode windowConfiguration)
		{
			XmlNodeList nodes = windowConfiguration.SelectNodes(GetToolXPath(area));
			if (nodes != null)
			{
				foreach (XmlNode node in nodes)
				{
					string value = XmlUtils.GetAttributeValue(node, "value", "???");
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
			return null;
		}
	}
}
