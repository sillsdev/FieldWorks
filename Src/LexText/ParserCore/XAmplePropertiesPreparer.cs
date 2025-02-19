// Copyright (c) 2024 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Collections;
using System.Xml.XPath;

namespace SIL.FieldWorks.WordWorks.Parser
{
	// Class to prepare custom XAmple properties
	public class XAmplePropertiesPreparer
	{
		public LcmCache Cache { get; set; }
		private bool ShowMessages { get; set; }
		private string entryCustomFieldName = "";
		private string formCustomFieldName = "";
		private string customListName = "";
		private XElement Root { get; set; }
		public XAmplePropertiesPreparer(LcmCache cache, XElement root, bool fShowMessages = true)
		{
			this.Cache = cache;
			this.Root = root;
			this.ShowMessages = fShowMessages;
			InitStringItems();
		}

		private void InitStringItems()
		{
			if (Root != null) {
				customListName = findElementValue("CustomList/Name");
				entryCustomFieldName = findElementValue("EntryLevelCustomField/Name");
				formCustomFieldName = findElementValue("FormLevelCustomField/Name");
			}
		}
		private string findElementValue(string xpath)
		{
			string result = "";
			var item = Root.XPathSelectElement(xpath);
			if (item != null)
			{
				result = item.Value;
			}
			return result;
		}
		public List<FieldDescription> GetListOfCustomFields()
		{
			return (from fd in FieldDescription.FieldDescriptors(Cache)
					where fd.IsCustomField
					select fd).ToList();
		}

		public void AddListsAndFields()
		{
			if (Root == null)
			{
				return;
			}

			AddXAmplePropertiesList();
			var customFields = GetListOfCustomFields();
			AddXAmplePropertiesCustomField(entryCustomFieldName, LexEntryTags.kClassId);
			AddXAmplePropertiesCustomField(formCustomFieldName, MoFormTags.kClassId);
		}

		/// <summary>
		/// Creates a new custom field for properties.
		/// </summary>
		public void AddXAmplePropertiesCustomField(string fieldName, int fieldClassId)
		{
			var customFields = GetListOfCustomFields();
			if (customFields.Find(fd => fd.Name == fieldName) != null)
			{
				// already done; quit
				return;
			}
			var possListRepository = Cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
			var customList = possListRepository.AllInstances().FirstOrDefault(list => list.Name.BestAnalysisAlternative.Text == customListName);
			if (customList == null)
			{
				// need the master possibility list and it does not exist
				if (ShowMessages)
				{
					Console.WriteLine("Need to create the master list of possibilities first.");
				}
				return;
			}
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				int ws = Cache.DefaultAnalWs;
				// create new custom field
				var fd = new FieldDescription(Cache)
				{
					Name = fieldName,
					Userlabel = fieldName,
					HelpString = string.Empty,
					Class = fieldClassId
				};
				fd.Type = CellarPropertyType.ReferenceCollection;
				fd.DstCls = CmCustomItemTags.kClassId;
				fd.WsSelector = WritingSystemServices.kwsAnal;

				fd.ListRootId = customList.Guid;
				fd.UpdateCustomField();
				FieldDescription.ClearDataAbout();
			});
		}
		/// <summary>
		/// Creates a new possibility list for Quechua properties.
		/// </summary>
		public void AddXAmplePropertiesList()
		{
			if (Root == null)
			{
				// nothing to do
				return;
			}
			var possListRepository = Cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
			var customList = possListRepository.AllInstances().FirstOrDefault(list => list.Name.BestAnalysisAlternative.Text == customListName);
			if (customList != null)
			{
				return;
			}
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				int ws = WritingSystemServices.kwsAnal;
				Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().CreateUnowned(customListName, ws);
				customList = possListRepository.AllInstances().Last();
				var propPoss = Cache.ServiceLocator.GetInstance<ICmCustomItemFactory>();
				ws = Cache.DefaultAnalWs;
				var elements = Root.XPathSelectElements("CustomList/Contents/Element");
				foreach (var element in elements)
				{
					var name = element.XPathSelectElement("Name").Value;
					var poss = CreateNewPropertyPossibility(ws, customList, propPoss, name);
					if (poss != null)
					{
						XElement item = element.XPathSelectElement("Abbreviation");
						string value = "";
						if (item != null)
						{
							value = item.Value;
							if (!String.IsNullOrEmpty(value))
							{
								poss.Abbreviation.set_String(ws, value);
							}
						}
						item = element.XPathSelectElement("Description");
						if (item != null)
						{
							value = item.Value;
							if (!String.IsNullOrEmpty(value))
							{
								poss.Description.set_String(ws, value);
							}
						}
					}
				}
			});
		}

		private ICmCustomItem CreateNewPropertyPossibility(int ws, ICmPossibilityList newList, ICmCustomItemFactory propPoss, string name)
		{
			var poss = propPoss.Create();
			newList.PossibilitiesOS.Add(poss);
			poss.Name.set_String(ws, name);
			return poss;
		}


	}
}
