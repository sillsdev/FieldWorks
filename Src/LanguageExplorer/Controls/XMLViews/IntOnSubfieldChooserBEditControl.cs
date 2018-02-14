// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Controls.XMLViews
{
	internal class IntOnSubfieldChooserBEditControl : IntChooserBEditControl
	{
		protected int m_subFieldId;
		/// <summary>
		/// Initialized with a string like "0:no;1:yes".
		/// </summary>
		public IntOnSubfieldChooserBEditControl(string itemList, int mainFieldId, int subFieldId)
			: base(itemList, mainFieldId)
		{
			m_subFieldId = subFieldId;
		}

		public override List<int> FieldPath
		{
			get
			{
				if (m_subFieldId == LexEntryRefTags.kflidHideMinorEntry)
				{
					return new List<int>(new[] { m_subFieldId });
				}
				var fieldPath = base.FieldPath;
				fieldPath.Add(m_subFieldId);
				return fieldPath;
			}

		}

		public override void FakeDoit(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnabled, ProgressState state)
		{
			var itemsToChangeAsList = new List<int>(itemsToChange);
			var val = (m_combo.SelectedItem as IntComboItem).Value;
			var tssVal = TsStringUtils.MakeString(m_combo.SelectedItem.ToString(), Cache.DefaultUserWs);
			var i = 0;
			// Report progress 50 times or every 100 items, whichever is more
			// (but no more than once per item!)
			var interval = Math.Min(100, Math.Max(itemsToChangeAsList.Count / 50, 1));
			if (m_subFieldId == LexEntryRefTags.kflidHideMinorEntry)
			{
				// we present this to the user as "Show" instead of "Hide"
				val = val == 0 ? 1 : 0;
				foreach (var hvoItem in itemsToChangeAsList)
				{
					i++;
					if (i % interval == 0)
					{
						state.PercentDone = i * 100 / itemsToChangeAsList.Count;
						state.Breath();
					}
					Debug.Assert(m_sda.get_IntProp(hvoItem, CmObjectTags.kflidClass) == LexEntryRefTags.kClassId);
					var valOld = m_sda.get_IntProp(hvoItem, m_subFieldId);
					var fEnable = valOld != val;
					if (fEnable)
					{
						m_sda.SetString(hvoItem, tagMadeUpFieldIdentifier, tssVal);
					}
					m_sda.SetInt(hvoItem, tagEnabled, (fEnable ? 1 : 0));
				}
			}
			else
			{
				foreach (var hvoItem in itemsToChangeAsList)
				{
					i++;
					if (i % interval == 0)
					{
						state.PercentDone = i * 100 / itemsToChangeAsList.Count;
						state.Breath();
					}
					var hvoField = m_sda.get_ObjectProp(hvoItem, m_flid);
					if (hvoField == 0)
					{
						continue;
					}
					var valOld = GetValueOfField(m_sda, hvoField);
					var fEnable = valOld != val;
					if (fEnable)
					{
						m_sda.SetString(hvoItem, tagMadeUpFieldIdentifier, tssVal);
					}
					m_sda.SetInt(hvoItem, tagEnabled, (fEnable ? 1 : 0));
				}
			}
		}

		internal virtual int GetValueOfField(ISilDataAccess sda, int hvoField)
		{
			return sda.get_IntProp(hvoField, m_subFieldId);
		}

		public override void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
		{
			var itemsToChangeAsList = new List<int>(itemsToChange);
			Cache.DomainDataByFlid.BeginUndoTask(XMLViewsStrings.ksUndoBulkEdit, XMLViewsStrings.ksRedoBulkEdit);
			var sda = Cache.DomainDataByFlid;
			var val = (m_combo.SelectedItem as IntComboItem).Value;
			var i = 0;
			// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
			var interval = Math.Min(100, Math.Max(itemsToChangeAsList.Count / 50, 1));
			if (m_subFieldId == LexEntryRefTags.kflidHideMinorEntry)
			{
				// we present this to the user as "Show" instead of "Hide"
				val = val == 0 ? 1 : 0;
				foreach (var hvoItem in itemsToChangeAsList)
				{
					i++;
					if (i % interval == 0)
					{
						state.PercentDone = i * 100 / itemsToChangeAsList.Count;
						state.Breath();
					}
					Debug.Assert(m_sda.get_IntProp(hvoItem, CmObjectTags.kflidClass) == LexEntryRefTags.kClassId);
					var valOld = m_sda.get_IntProp(hvoItem, m_subFieldId);
					if (valOld != val)
					{
						sda.SetInt(hvoItem, m_subFieldId, val);
					}
				}
			}
			else
			{
				foreach (var hvoItem in itemsToChangeAsList)
				{
					i++;
					if (i % interval == 0)
					{
						state.PercentDone = i * 100 / itemsToChangeAsList.Count;
						state.Breath();
					}
					var hvoField = sda.get_ObjectProp(hvoItem, m_flid);
					if (hvoField == 0)
					{
						continue;
					}
					var valOld = GetValueOfField(sda, hvoField);
					if (valOld == val)
					{
						continue;
					}
					SetValueOfField(sda, hvoField, val);
				}
			}
			Cache.DomainDataByFlid.EndUndoTask();
		}

		internal virtual void SetValueOfField(ISilDataAccess sda, int hvoField, int val)
		{
			sda.SetInt(hvoField, m_subFieldId, val);
		}
	}
}