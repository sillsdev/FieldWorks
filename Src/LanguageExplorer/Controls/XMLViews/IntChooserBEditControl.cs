// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.SpellChecking;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.XMLViews
{
	internal class IntChooserBEditControl : BulkEditSpecControl, IGetReplacedObjects
	{
		#region IBulkEditSpecControl Members
		protected int m_flid;
		protected ComboBox m_combo;
		Dictionary<int, int> m_replacedObjects = new Dictionary<int, int>();

		/// <summary>
		/// Initialized with a string like "0:no;1:yes".
		/// </summary>
		/// <param name="itemList">The item list.</param>
		/// <param name="flid">The flid.</param>
		public IntChooserBEditControl(string itemList, int flid)
		{
			m_flid = flid;
			m_combo = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList
			};
			foreach (var pair in itemList.Split(';'))
			{
				var vals = pair.Trim().Split(':');
				if (vals.Length != 2)
				{
					throw new Exception("IntChooserBEditControl values must look like n:name");
				}
				var val = int.Parse(vals[0]);
				m_combo.Items.Add(new IntComboItem(vals[1].Trim(), val));
			}
			if (m_combo.Items.Count == 0)
			{
				throw new Exception("IntChooserBEditControl created with zero items");
			}
			m_combo.SelectedIndex = 0;
			m_combo.SelectedIndexChanged += m_combo_SelectedIndexChanged;
		}

		/// <summary>
		/// Initialized with a list of strings; first signifies 0, next 1, etc.
		/// </summary>
		/// <param name="itemList"></param>
		/// <param name="flid"></param>
		/// <param name="initialIndexToSelect">Index of one of the items in the combo box, the most useful choice that should
		/// initially be selected. Comes from defaultBulkEditChoice attribute on [column] element in XML spec. Default 0.</param>
		public IntChooserBEditControl(string[] itemList, int flid, int initialIndexToSelect)
		{
			m_flid = flid;
			m_combo = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList
			};
			var index = 0;
			foreach (var name in itemList)
			{
				m_combo.Items.Add(new IntComboItem(name, index++));
			}
			if (m_combo.Items.Count == 0)
			{
				throw new Exception("IntChooserBEditControl created with zero items");
			}
			m_combo.SelectedIndex = Math.Max(0, Math.Min(m_combo.Items.Count - 1, initialIndexToSelect));
			m_combo.SelectedIndexChanged += m_combo_SelectedIndexChanged;
		}

		protected void m_combo_SelectedIndexChanged(object sender, EventArgs e)
		{
			// Tell the parent control that we may have changed the selected item so it can
			// enable or disable the Apply and Preview buttons based on the selection.
			var index = m_combo.SelectedIndex;
			var hvo = 0;  // we need a dummy hvo value to pass since IntChooserBEditControl
			//displays 'yes' or 'no' which have no hvo
			OnValueChanged(sender, new FwObjectSelectionEventArgs(hvo, index));
		}

		public override Control Control => m_combo;

		public override void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
		{
			var itemsToChangeAsList = new List<int>(itemsToChange);
			m_cache.DomainDataByFlid.BeginUndoTask(XMLViewsStrings.ksUndoBulkEdit, XMLViewsStrings.ksRedoBulkEdit);
			var sda = m_cache.DomainDataByFlid;
			m_replacedObjects.Clear();

			var val = (m_combo.SelectedItem as IntComboItem).Value;
			var i = 0;
			// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
			var interval = Math.Min(100, Math.Max(itemsToChangeAsList.Count / 50, 1));
			var mdcManaged = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			foreach (var hvoItem in itemsToChangeAsList)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 100 / itemsToChangeAsList.Count;
					state.Breath();
				}
				// If the field is on an owned object that might not exist, we don't want to create
				// that owned object just because we're changing the values involved.
				// (See FWR-3199 for an example of such a situation.)
				var clid = m_sda.get_IntProp(hvoItem, CmObjectTags.kflidClass);
				var flids = mdcManaged.GetFields(clid, true, (int)CellarPropertyTypeFilter.All);
				if (!flids.Contains(m_flid))
				{
					continue;
				}
				int valOld;
				if (TryGetOriginalListValue(sda, hvoItem, out valOld) && valOld == val)
				{
					continue;
				}
				UpdateListItemToNewValue(sda, hvoItem, val, valOld);
			}
			// Enhance JohnT: maybe eventually we will want to make a more general mechanism for doing this,
			// e.g., specify a method to call using the XML configuration for the column?
			// Note that resetting them all at the end of the edit is much more efficient than
			// adding an override for each word, which rewrites the exceptions file each time.
			// (This code runs before the code at the end of the UOW which also tries to update the spelling
			// status. That code will find it is already correct and not write the exc file.)
			if (m_flid == WfiWordformTags.kflidSpellingStatus)
			{
				WfiWordformServices.ConformOneSpellingDictToWordforms(m_cache.DefaultVernWs, m_cache);
			}
			m_cache.DomainDataByFlid.EndUndoTask();
		}

		protected override void UpdateListItemToNewValue(ISilDataAccess sda, int hvoItem, int newVal, int oldVal)
		{
			var hvoOwningInt = hvoItem;
			if (m_ghostParentHelper != null)
			{
				// it's possible that hvoItem is actually the owner of a object that needs to be created
				// for hvoSel to be set.
				hvoOwningInt = m_ghostParentHelper.FindOrCreateOwnerOfTargetProp(hvoItem, m_flid);
			}
			SetBasicPropertyValue(sda, newVal, hvoOwningInt);
		}

		protected virtual void SetBasicPropertyValue(ISilDataAccess sda, int newVal, int hvoOwner)
		{
			var type = m_sda.MetaDataCache.GetFieldType(m_flid);
			if (type == (int) CellarPropertyType.Boolean)
			{
				sda.SetBoolean(hvoOwner, m_flid, newVal != 0);
			}
			else
			{
				sda.SetInt(hvoOwner, m_flid, newVal);
			}
		}

		private void FixSpellingStatus(int hvoItem, int val)
		{
			var defVernWS = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
			var tss = m_cache.DomainDataByFlid.get_MultiStringAlt(hvoItem, WfiWordformTags.kflidForm, defVernWS);
			if (tss == null || tss.Length == 0)
			{
				return; // probably can't happen?
			}
			SpellingHelper.SetSpellingStatus(tss.Text, defVernWS, m_cache.WritingSystemFactory, (val == (int)SpellingStatusStates.correct));
		}

		public override void FakeDoit(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnabled, ProgressState state)
		{
			var itemsToChangeAsList = new List<int>(itemsToChange);
			var val = ((IntComboItem) m_combo.SelectedItem).Value;
			var tssVal = TsStringUtils.MakeString(m_combo.SelectedItem.ToString(), m_cache.ServiceLocator.WritingSystemManager.UserWs);
			var i = 0;
			// Report progress 50 times or every 100 items, whichever is more
			// (but no more than once per item!)
			var interval = Math.Min(100, Math.Max(itemsToChangeAsList.Count / 50, 1));
			var mdcManaged = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			var type = m_sda.MetaDataCache.GetFieldType(m_flid);
			foreach (var hvoItem in itemsToChangeAsList)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 100 / itemsToChangeAsList.Count;
					state.Breath();
				}
				bool fEnable;
				// If the field is on an owned object that might not exist, the hvoItem might
				// refer to the owner, which is likely of a different class.  In such cases,
				// we don't want to try getting the field value, since that produces a pretty
				// green dialog box for the user.  See FWR-3199.
				var clid = m_sda.get_IntProp(hvoItem, CmObjectTags.kflidClass);
				var flids = mdcManaged.GetFields(clid, true, (int)CellarPropertyTypeFilter.All);
				if (!flids.Contains(m_flid))
				{
					fEnable = false;
				}
				else if (type == (int) CellarPropertyType.Boolean)
				{
					fEnable = m_sda.get_BooleanProp(hvoItem, m_flid) != (val != 0);
				}
				else
				{
					fEnable = m_sda.get_IntProp(hvoItem, m_flid) != val;
				}
				if (fEnable)
				{
					m_sda.SetString(hvoItem, tagMadeUpFieldIdentifier, tssVal);
				}
				m_sda.SetInt(hvoItem, tagEnabled, (fEnable ? 1 : 0));
			}
		}

		protected override bool TryGetOriginalListValue(ISilDataAccess sda, int hvoItem, out int value)
		{
			value = int.MinValue;
			var hvoOwningInt = hvoItem;
			if (m_ghostParentHelper != null)
			{
				hvoOwningInt = m_ghostParentHelper.GetOwnerOfTargetProperty(hvoItem);
			}
			if (hvoOwningInt == 0)
			{
				return false;
			}
			value = GetBasicPropertyValue(sda, hvoOwningInt);
			return true;
		}

		protected virtual int GetBasicPropertyValue(ISilDataAccess sda, int hvoOwner)
		{
			var type = m_sda.MetaDataCache.GetFieldType(m_flid);
			if (type == (int) CellarPropertyType.Boolean)
			{
				return sda.get_BooleanProp(hvoOwner, m_flid) ? 1 : 0;
			}
			return sda.get_IntProp(hvoOwner, m_flid);
		}

		public override bool CanClearField => false;

		public override void SetClearField()
		{
			throw new Exception("The IntChooserBEditControl.SetClearField() method is not implemented.");
		}

		public override List<int> FieldPath => new List<int>(new int[] { m_flid });

		#endregion

		#region IGetReplacedObjects Members

		/// <summary>
		/// Objects get replaced here when dummies are changed to real.
		/// </summary>
		public Dictionary<int, int> ReplacedObjects => m_replacedObjects;

		#endregion
	}
}