// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DictionaryConfigViewerStub.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;

namespace SIL.FieldWorks.XWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test stub used to replace the DictionaryConfigMgrDlg in testing.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DictionaryConfigViewerStub : IDictConfigViewer
	{
		private readonly DictionaryConfigTestPresenter m_presenter;
		private List<Tuple<string, string>> m_listItems;

		public DictionaryConfigViewerStub()
		{
			m_presenter = new DictionaryConfigTestPresenter(this);
			m_listItems = new List<Tuple<string, string>>();
		}

		public DictionaryConfigTestPresenter TestPresenter
		{
			get { return m_presenter; }
		}

		public string SelectedItem { get; private set; }

		#region Implementation of IDictConfigViewer

		public IDictConfigPresenter Presenter
		{
			get { return m_presenter; }
		}

		/// <summary>
		/// Tuples of strings are (uniqueCode, dispName) pairs to be displayed.
		/// </summary>
		/// <param name="listItems"></param>
		/// <param name="selectedItem">uniqueCode of item that should be selected.</param>
		public void SetListViewItems(IEnumerable<Tuple<string, string>> listItems,
			string selectedItem)
		{
			if (m_listItems == null)
				m_listItems = new List<Tuple<string, string>>();
			m_listItems.Clear();
			foreach (var listItem in listItems)
				m_listItems.Add(listItem);

			Debug.Assert(m_listItems.Where(
				tpl => tpl.Item1 == selectedItem).FirstOrDefault() != null,
				"Selected item does not exist in list.");
			SelectedItem = selectedItem;
		}

		/// <summary>
		/// The unique code for the item currently selected in the dialog listView.
		/// </summary>
		public string CurrentSelectedCode
		{
			get { return SelectedItem; }
		}

		#endregion
	}

	internal class DictionaryConfigTestPresenter : DictionaryConfigManager
	{
		private static XmlNode s_firstConfig;
		public DictionaryConfigTestPresenter(IDictConfigViewer viewer)
			: base(viewer, GetConfigs(), s_firstConfig)
		{
		}

		private static List<XmlNode> GetConfigs()
		{
			const string sConfigs = "<configureLayouts>" +
									"<layoutType label=\"Stem-based (complex forms as main entries)\" layout=\"publishStem\">" +
									"<configure class=\"LexEntry\" label=\"Main Entry\" layout=\"publishStemEntry\"/>" +
									"<configure class=\"LexEntry\" label=\"Minor Entry\" layout=\"publishStemMinorEntry\"/>" +
									"</layoutType>" +
									"<layoutType label=\"Root-based (complex forms as subentries)\" layout=\"publishRoot\">" +
									"<configure class=\"LexEntry\" label=\"Main Entry\" layout=\"publishRootEntry\"/>" +
									"<configure class=\"LexEntry\" label=\"Minor Entry\" layout=\"publishRootMinorEntry\"/>" +
									"</layoutType>" +
									"</configureLayouts>";
			var xdoc = new XmlDocument();
			xdoc.LoadXml(sConfigs);
			var configs = new List<XmlNode>();
			foreach (var xn in xdoc.FirstChild.ChildNodes.Cast<XmlNode>().Where(xn => xn.Name == "layoutType"))
			{
				configs.Add(xn);
				if (s_firstConfig == null)
					s_firstConfig = xn;
			}
			return configs;
		}

		internal Dictionary<string, DictConfigItem> StubConfigDict
		{
			get { return m_configList; }
		}

		protected override void ShowAlreadyInUseMsg()
		{
			// Do nothing for tests.
		}

		internal string StubCurView
		{
			get { return m_currentView; }
			set { m_currentView = value; }
		}

		internal string StubOrigView
		{
			get { return m_originalView; }
			set
			{
				m_originalView = value;
				m_currentView = m_originalView;
			}
		}

		internal void LoadConfigList(IEnumerable<Tuple<string, string, bool>> codeNamePairs)
		{
			LoadInternalDictionary(codeNamePairs);
		}

		internal void UpdateCurSelection(string curCode)
		{
			UpdateCurrentView(curCode);
		}
	}
}
