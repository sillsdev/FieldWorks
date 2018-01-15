// Copyright (c) 2015-2018 SIL International
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
		protected int m_flidSub;
		/// <summary>
		/// Initialized with a string like "0:no;1:yes".
		/// </summary>
		/// <param name="itemList"></param>
		/// <param name="flid">main field</param>
		/// <param name="flidSub">subfield</param>
		public IntOnSubfieldChooserBEditControl(string itemList, int flid, int flidSub)
			: base(itemList, flid)
		{
			m_flidSub = flidSub;
		}

		public override List<int> FieldPath
		{
			get
			{
				if (m_flidSub == LexEntryRefTags.kflidHideMinorEntry)
				{
					return new List<int>(new int[] { m_flidSub });
				}
				var fieldPath = base.FieldPath;
				fieldPath.Add(m_flidSub);
				return fieldPath;
			}

		}

		public override void FakeDoit(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnabled, ProgressState state)
		{
			var itemsToChangeAsList = new List<int>(itemsToChange);
			var val = (m_combo.SelectedItem as IntComboItem).Value;
			var tssVal = TsStringUtils.MakeString(m_combo.SelectedItem.ToString(), m_cache.DefaultUserWs);
			var i = 0;
			// Report progress 50 times or every 100 items, whichever is more
			// (but no more than once per item!)
			var interval = Math.Min(100, Math.Max(itemsToChangeAsList.Count / 50, 1));
			if (m_flidSub == LexEntryRefTags.kflidHideMinorEntry)
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
					var valOld = m_sda.get_IntProp(hvoItem, m_flidSub);
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
			return sda.get_IntProp(hvoField, m_flidSub);
		}

		public override void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
		{
			var itemsToChangeAsList = new List<int>(itemsToChange);
			m_cache.DomainDataByFlid.BeginUndoTask(XMLViewsStrings.ksUndoBulkEdit, XMLViewsStrings.ksRedoBulkEdit);
			var sda = m_cache.DomainDataByFlid;
			var val = (m_combo.SelectedItem as IntComboItem).Value;
			var i = 0;
			// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
			var interval = Math.Min(100, Math.Max(itemsToChangeAsList.Count / 50, 1));
			if (m_flidSub == LexEntryRefTags.kflidHideMinorEntry)
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
					var valOld = m_sda.get_IntProp(hvoItem, m_flidSub);
					if (valOld != val)
					{
						sda.SetInt(hvoItem, m_flidSub, val);
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
			m_cache.DomainDataByFlid.EndUndoTask();
		}

		internal virtual void SetValueOfField(ISilDataAccess sda, int hvoField, int val)
		{
			sda.SetInt(hvoField, m_flidSub, val);
		}
	}
}