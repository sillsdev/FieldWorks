// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DictionaryConfigManager.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Manages the stored dictionary configurations. In the Model-View-Presenter pattern, this
	/// is the Presenter which acts on the dialog (or testing stub) and persists the data.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DictionaryConfigManager : IDictConfigPresenter, IDictConfigManager
	{
		private readonly IDictConfigViewer m_viewer;

		private Inventory m_layouts;
		private Inventory m_parts;
		protected Dictionary<string, DictConfigItem> m_configList;

		protected string m_originalView;
		protected string m_currentView;

		protected bool m_fPersisted;
		protected List<XmlNode> m_originalViewConfigNodes;

		/// <summary>
		/// Create the Manager for stored dictionary configurations.
		/// </summary>
		public DictionaryConfigManager(IDictConfigViewer viewer, List<XmlNode> configViews,
			XmlNode current)
		{
			m_viewer = viewer;
			m_originalViewConfigNodes = configViews;

			m_configList = new Dictionary<string, DictConfigItem>();
			m_fPersisted = false;

			LoadDataFromInventory(current);
		}

		/// <summary>
		/// Get protected and user-stored dictionary configurations to load into the dialog.
		/// Tests will override this to load the manager in their own fashion.
		/// </summary>
		private void LoadDataFromInventory(XmlNode current)
		{
			// Tuples are <uniqueCode, dispName, IsProtected>
			var configList = new List<Tuple<string, string, bool>>();

			// put them in configList and feed them into the Manager's dictionary.
			foreach (var xnView in m_originalViewConfigNodes)
			{
				var sLabel = XmlUtils.GetManditoryAttributeValue(xnView, "label");
				var sLayout = XmlUtils.GetManditoryAttributeValue(xnView, "layout");
				var fProtected = !sLayout.Contains(Inventory.kcMarkLayoutCopy);
				configList.Add(new Tuple<string, string, bool>(sLayout, sLabel, fProtected));
			}

			LoadInternalDictionary(configList);

			var sLayoutCurrent = XmlUtils.GetManditoryAttributeValue(current, "layout");
			m_originalView = sLayoutCurrent;
			m_currentView = m_originalView;

			if (m_configList.Count == 0)
				return;

			// Now set up the actual dialog's contents
			RefreshView();
		}

		private void RefreshView()
		{
			// Tuples are (uniqueCode, displayName)
			var viewItems = m_configList.Where(item => !item.Value.UserMarkedDelete).Select(
				item => new Tuple<string, string>(item.Key, item.Value.DispName));
			Viewer.SetListViewItems(viewItems, m_currentView);
		}

		protected void LoadInternalDictionary(IEnumerable<Tuple<string, string, bool>> configList)
		{
			// Tuples are <uniqueCode, dispName, IsProtected>
			foreach (var config in configList)
			{
				var code = config.Item1;
				DictConfigItem item;
				if (m_configList.TryGetValue(code, out item))
				{
					Debug.Assert(false, String.Format("The 'configList' code {0} is NOT unique!", code));
					// ReSharper disable HeuristicUnreachableCode
					continue;
					// ReSharper restore HeuristicUnreachableCode
				}
				item = new DictConfigItem(code, config.Item2, config.Item3);
				m_configList.Add(code, item);
			}
		}

		protected void UpdateCurrentView(string curSelCode)
		{
			DictConfigItem item;
			if (!m_configList.TryGetValue(curSelCode, out item))
			{
				Debug.Assert(false, String.Format("Non-existent configuration code {0}", curSelCode));
				// ReSharper disable HeuristicUnreachableCode
				return;
				// ReSharper restore HeuristicUnreachableCode
			}
			m_currentView = curSelCode;
		}

		protected bool IsViewDeleted(string curSelCode)
		{
			DictConfigItem item;
			if (!m_configList.TryGetValue(curSelCode, out item))
			{
				Debug.Assert(false, String.Format("Non-existent configuration code {0}", curSelCode));
				// ReSharper disable HeuristicUnreachableCode
				return true;
				// ReSharper restore HeuristicUnreachableCode
			}
			return item.UserMarkedDelete;
		}

		private bool CurViewHasChanged
		{
			get { return m_originalView != m_currentView; }
		}

		#region IDictConfigPresenter Members

		public IDictConfigViewer Viewer
		{
			get { return m_viewer; }
		}

		/// <summary>
		/// Get the DictConfigItem associated with this code and mark it for deletion.
		/// </summary>
		/// <param name="code"></param>
		/// <returns>true if deletion is successful, false if unsuccessful.</returns>
		public bool TryMarkForDeletion(string code)
		{
			DictConfigItem item;
			if (!m_configList.TryGetValue(code, out item))
			{
				Debug.Assert(false, String.Format("Code {0} not found.", code));
				// ReSharper disable HeuristicUnreachableCode
				return true; // At least its not there, so deleting it is successful!
				// ReSharper restore HeuristicUnreachableCode
			}
			if (item.IsProtected)
				return false;
			item.UserMarkedDelete = true;
			if (item.UniqueCode == m_currentView)
			{
				if (!CurViewHasChanged || (CurViewHasChanged && IsViewDeleted(m_originalView)))
					UpdateViewToFirstProtected();
				else
					UpdateCurrentView(m_originalView);
			}
			// If this entry is newly created since opening the dialog,
			// there's no point in keeping it around anymore.
			if (item.IsNew)
				m_configList.Remove(item.UniqueCode);
			RefreshView();
			return true;
		}

		private void UpdateViewToFirstProtected()
		{
			var newList = m_configList.Where(entry => entry.Value.IsProtected);
			foreach (var entry in newList.OrderBy(item => item.Value.DispName))
			{
				UpdateCurrentView(entry.Key);
				break;
			}
		}

		/// <summary>
		/// Get the DictConfigItem associated with this code and make a copy of it.
		/// The new item's display name will be "Copy of X", where X is the source item's name.
		/// </summary>
		/// <param name="sourceCode"></param>
		public void CopyConfigItem(string sourceCode)
		{
			// For now dis-allow copying a recent copy (since opening the dialog)
			DictConfigItem item;
			if (IsConfigNew(sourceCode, out item))
				return;

			// Copy item
			InternalCopyConfigItem(item);
			RefreshView();
		}

		private void InternalCopyConfigItem(DictConfigItem source)
		{
			var newItem = new DictConfigItem(source);
			EnsureUniqueLabel(newItem);
			var newCode = newItem.UniqueCode;
			m_configList.Add(newCode, newItem);
			UpdateCurrentView(newCode);
		}

		private void EnsureUniqueLabel(DictConfigItem newItem)
		{
			var counter = 1;
			while ((from item in m_configList where item.Value.DispName == newItem.DispName select item).Count() > 0)
			{
				counter++;
				// change DispName on newItem using ksDictConfigMultiCopyOf
				DictConfigItem origItem;
				m_configList.TryGetValue(newItem.CopyOf, out origItem);
				var origName = origItem.DispName;
				newItem.DispName = String.Format(xWorksStrings.ksDictConfigMultiCopyOf, origName, counter);
			}
		}

		/// <summary>
		/// Get the DictConfigItem associated with this code and rename its display name
		/// to the value in newName.
		/// </summary>
		/// <param name="code"></param>
		/// <param name="newName"></param>
		public void RenameConfigItem(string code, string newName)
		{
			DictConfigItem item;
			if (!m_configList.TryGetValue(code, out item))
			{
				Debug.Assert(false, String.Format("Code {0} not found.", code));
				// ReSharper disable HeuristicUnreachableCode
				return; // Should get here if we didn't have a valid item selected!
				// ReSharper restore HeuristicUnreachableCode
			}

			// Do not allow protected configurations to be renamed.
			// This is now checked before the edit is allowed! Nevermind!

			var filteredName = MiscUtils.FilterForFileName(newName, MiscUtils.FilenameFilterStrength.kFilterBackup);

			if (NameAlreadyInUse(filteredName))
			{
				ShowAlreadyInUseMsg();
				// Because the Presenter won't change, so the View needs to revert.
				RefreshView();
				return;
			}

			item.DispName = filteredName;
		}

		/// <summary>
		/// Test subclass will override to do nothing.
		/// </summary>
		protected virtual void ShowAlreadyInUseMsg()
		{
			MessageBox.Show(xWorksStrings.ksChooseAnotherViewName,
							xWorksStrings.ksNameInUseTitle,
							MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private bool NameAlreadyInUse(string newName)
		{
			return m_configList.Any(configItem => configItem.Value.DispName == newName);
		}

		/// <summary>
		/// Sets up stored dictionary configurations as per final values from Viewer.
		/// And change the current selected dictionary view. Prepares Presenter to
		/// communicate to calling dialog (XMLDocConfigureDlg) what needs to be done.
		/// </summary>
		public void PersistState()
		{
			// Actually all this needs to do is set the Persisted flag to true.
			// The IDictConfigManager properties will do the rest.
			m_fPersisted = true;
		}

		public bool IsConfigProtected(string code)
		{
			DictConfigItem item;
			if (!m_configList.TryGetValue(code, out item))
			{
				Debug.Assert(false, String.Format("Code {0} not found.", code));
				// ReSharper disable HeuristicUnreachableCode
				return true; // Should get here if we didn't have a valid item selected!
				// ReSharper restore HeuristicUnreachableCode
			}
			return item.IsProtected;
		}

		public bool IsConfigNew(string code)
		{
			DictConfigItem dummy;
			return IsConfigNew(code, out dummy);
		}

		private bool IsConfigNew(string code, out DictConfigItem item)
		{
			if (!m_configList.TryGetValue(code, out item))
			{
				Debug.Assert(false, String.Format("Code {0} not found.", code));
				// ReSharper disable HeuristicUnreachableCode
				return true; // Should get here if we didn't have a valid item selected!
				// ReSharper restore HeuristicUnreachableCode
			}
			return item.IsNew;
		}

		#endregion

		#region IDictConfigManager Members

		/// <summary>
		/// The view that should be selected in the main dialog after this one closes (if the user clicks OK).
		/// </summary>
		public string FinalConfigurationView { get; set; }

		/// <summary>
		/// If copies of older configuration views have been made, this property will
		/// provide a list of the new views to create.
		/// Items(Tuples) in the list are of the format:
		///		(newUniqueCode, codeOfViewCopiedFrom, newDisplayName)
		/// </summary>
		public IEnumerable<Tuple<string, string, string>> NewConfigurationViews
		{
			get
			{
				if (!m_fPersisted || m_configList.Count == 0)
					return null;
				var result = m_configList.Values.Where(item => item.IsNew).Select(
					configItem => new Tuple<string, string, string>(
						configItem.UniqueCode, configItem.CopyOf, configItem.DispName)).ToList();
				return result.Count > 0 ? result : null;
			}
		}

		/// <summary>
		/// If existing configuration views have been deleted, this property will
		/// provide a list of the unique codes to delete.
		/// N.B.: Make sure Caller processes copying views first, in case some of
		/// the copies are based on views that are to be deleted!
		/// </summary>
		public IEnumerable<string> ConfigurationViewsToDelete
		{
			get
			{
				if (!m_fPersisted || m_configList.Count == 0)
					return null;
				var result = new List<string>();
				result.AddRange(m_configList.Values.Where(
					item => !item.IsNew && item.UserMarkedDelete).Select(item => item.UniqueCode));
				return result.Count > 0 ? result : null;
			}
		}

		/// <summary>
		/// If older configuration views have been renamed, this property will
		/// provide a list of the codes with their new display names.
		/// Items(Tuples) in the list are of the format:
		///		(uniqueCode, newDisplayName)
		/// </summary>
		public IEnumerable<Tuple<string, string>> RenamedExistingViews
		{
			get
			{
				if (!m_fPersisted || m_configList.Count == 0)
					return null;
				var result = m_configList.Values.Where(item => item.IsRenamed).Select(
					configItem => new Tuple<string, string>(
						configItem.UniqueCode, configItem.DispName)).ToList();
				return result.Count > 0 ? result : null;
			}
		}

		#endregion
	}

	/// <summary>
	/// Item to be managed by the DictionaryConfigManager. Represents a user-configurable
	/// Dictionary view layout.
	/// </summary>
	internal class DictConfigItem
	{
		private readonly string m_initialName;

		#region Auto-Properties

		public string DispName { get; set; }
		public string UniqueCode { get; private set; }
		public bool IsProtected { get; private set; }
		public bool UserMarkedDelete { get; set; }
		public string CopyOf { get; private set; }

		#endregion

		private const string ksUnnamed = "NoName";

		/// <summary>
		/// Constructor for config items being sent to dialog.
		/// </summary>
		/// <param name="code"></param>
		/// <param name="name"></param>
		/// <param name="original">Original items to be protected.</param>
		public DictConfigItem(string code, string name, bool original)
		{
			UniqueCode = code;
			m_initialName = name;
			DispName = m_initialName;
			IsProtected = original;
			CopyOf = null;
			UserMarkedDelete = false;
		}

		/// <summary>
		/// Constructor for config items created by the Manager as copies of
		/// an existing item.
		/// </summary>
		/// <param name="source"></param>
		public DictConfigItem(DictConfigItem source)
		{
			UniqueCode = CreateUniqueIdCode(source.DispName);
			IsProtected = false;
			UserMarkedDelete = false;
			CopyOf = source.UniqueCode;
			DispName = String.Format(xWorksStrings.ksDictConfigCopyOf, source.DispName);
			m_initialName = DispName;
		}

		private static string CreateUniqueIdCode(string oldName)
		{
			var nameSeed = oldName.PadRight(5);
			var result = nameSeed + ksUnnamed; // make sure we don't have something too short!
			var num = DateTime.UtcNow.Millisecond.ToString();
			return result.Substring(0, 5) + num;
		}

		public bool IsNew
		{
			get { return (!String.IsNullOrEmpty(CopyOf)); }
		}

		public bool IsRenamed
		{
			get { return !IsNew && !UserMarkedDelete && m_initialName != DispName; }
		}
	}
}
