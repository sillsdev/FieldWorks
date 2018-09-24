// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This class implements setting a sequence reference property to a set of values chosen
	/// from a list.
	/// </summary>
	internal class ComplexListChooserBEditControl : IBulkEditSpecControl
	{
		protected LcmCache m_cache;
		private XMLViewsDataCache m_sda;
		protected Button m_launcher;
		protected int m_hvoList;
		protected int m_flid;
		string m_fieldName; // user-viewable name of field to display
		string m_displayNameProperty; // name of method to get what to display for each item.
		string m_displayWs; // key recognized by ObjectLabelCollection
		List<ICmObject> m_chosenObjs = new List<ICmObject>(0);
		bool m_fRemove; // true to remove selected items rather than append or replace
		private GhostParentHelper m_ghostParentHelper;

		public virtual Button SuggestButton => null;

		public event FwSelectionChangedEventHandler ValueChanged;

		public ComplexListChooserBEditControl(LcmCache cache, IPropertyTable propertyTable, XElement colSpec)
			: this(BulkEditBar.GetFlidFromClassDotName(cache, colSpec, "field"),
				BulkEditBar.GetNamedListHvo(cache, colSpec, "list"),
				XmlUtils.GetOptionalAttributeValue(colSpec, "displayNameProperty", "ShortNameTSS"),
				BulkEditBar.GetColumnLabel(colSpec),
				XmlUtils.GetOptionalAttributeValue(colSpec, "displayWs", "best analorvern"),
				BulkEditBar.GetGhostHelper(cache.ServiceLocator, colSpec))
		{
			PropertyTable = propertyTable;
		}

		/// <summary>
		/// C# equivalent of the VB RIGHT function
		/// </summary>
		public string Right(string original, int numberCharacters)
		{
			return original.Substring(original.Length - numberCharacters);
		}

		public ComplexListChooserBEditControl(int flid, int hvoList, string displayNameProperty, string fieldName, string displayWs, GhostParentHelper gph)
		{
			m_hvoList = hvoList;
			m_flid = flid;
			m_launcher = new Button
			{
				Text = XMLViewsStrings.ksChoose_
			};
			m_launcher.Click += m_launcher_Click;
			m_displayNameProperty = displayNameProperty;
			m_fieldName = fieldName;
			m_displayWs = displayWs;
			m_ghostParentHelper = gph;
		}

		protected virtual void m_launcher_Click(object sender, EventArgs e)
		{
			if (m_hvoList == (int)SpecialHVOValues.kHvoUninitializedObject)
			{
				return; // Can't show a chooser for a non-existent list!
			}
			// Show a wait cursor (LT-4673)
			using (new WaitCursor(this.Control))
			{
				var list = m_cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>().GetObject(m_hvoList);
				var persistProvider = PersistenceProviderFactory.CreatePersistenceProvider(PropertyTable);
				var labels = ObjectLabel.CreateObjectLabels(m_cache, list.PossibilitiesOS, m_displayNameProperty, m_displayWs);
				using (var chooser = new ReallySimpleListChooser(persistProvider, labels, m_fieldName, m_cache, m_chosenObjs, PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider)))
				{
					chooser.Atomic = Atomic;
					chooser.Cache = m_cache;
					chooser.SetObjectAndFlid(0, m_flid);
					chooser.ShowFuncButtons();
					if (Convert.ToInt16(Right(m_flid.ToString(), 3)) >= 500 && Convert.ToInt16(Right(m_flid.ToString(), 3)) < 600)
					{
						chooser.SetHelpTopic("khtpBulkEditCustomField");
					}
					else
					{
						chooser.SetHelpTopic("khtpBulkEdit" + m_fieldName.Replace(" ", ""));
					}
					var res = chooser.ShowDialog((sender as Control).TopLevelControl);
					if (DialogResult.Cancel == res)
					{
						return;
					}
					m_chosenObjs = chooser.ChosenObjects.ToList();
					ReplaceMode = chooser.ReplaceMode;
					m_fRemove = chooser.RemoveMode;

					// Tell the parent control that we may have changed the selected item so it can
					// enable or disable the Apply and Preview buttons based on the selection.
					// We are just checking here if any item was selected by the user in the dialog
					if (ValueChanged == null)
					{
						return;
					}
					var hvo = 0;
					if (m_chosenObjs.Count > 0)
					{
						hvo = m_chosenObjs[0].Hvo;
					}
					ValueChanged(sender, new FwObjectSelectionEventArgs(hvo));
				}
			}
		}

		protected void EnableButtonsIfChangesWaiting()
		{
			Debug.Assert(SuggestButton != null, "Only used for Semantic Domain subclass");

			// Tell the parent control that we may have changed the selected item so it can
			// enable or disable the Apply and Preview buttons based on the selection.
			if (ValueChanged == null)
			{
				return;
			}
			var hvo = -1; // not really a hvo, but we want to trigger this
			ValueChanged(this, new FwObjectSelectionEventArgs(hvo));
		}

		#region IBulkEditSpecControl Members

		/// <summary>
		/// Get/Set the property table'
		/// </summary>
		public IPropertyTable PropertyTable { get; set; }

		public Control Control => m_launcher;

		public LcmCache Cache
		{
			get
			{
				return m_cache;
			}
			set
			{
				m_cache = value;
				Atomic = (CellarPropertyType)m_cache.MetaDataCacheAccessor.GetFieldType(m_flid) == CellarPropertyType.ReferenceAtom;
			}
		}

		private bool Atomic { get; set; }
		/// <summary>
		/// The special cache that can handle the preview and check-box properties.
		/// </summary>
		public XMLViewsDataCache DataAccess
		{
			get
			{
				if (m_sda == null)
				{
					throw new InvalidOperationException("Must set the special cache of a BulkEditSpecControl");
				}
				return m_sda;
			}
			set { m_sda = value; }
		}

		/// <summary>
		/// (By default, it is an empty list (int[0]), unless user has used the chooser to select items.)
		/// </summary>
		internal IEnumerable<ICmObject> ChosenObjects
		{
			get { return m_chosenObjs; }
			set { m_chosenObjs = value.ToList(); }
		}

		/// <summary />
		internal bool ReplaceMode { get; set; }

		/// <summary>
		/// required interface member not currently used.
		/// </summary>
		public IVwStylesheet Stylesheet
		{
			set {  }
		}

		public virtual void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
		{
			UndoableUnitOfWorkHelper.Do(XMLViewsStrings.ksUndoBulkEdit, XMLViewsStrings.ksRedoBulkEdit,
				m_cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					//ISilDataAccess sda = m_cache.DomainDataByFlid; // used DataAccess, is that okay?

					var chosenObjs = m_chosenObjs;
					var i = 0;
					// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
					var interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
					foreach (var hvoItem in itemsToChange)
					{
						i++;
						if (i % interval == 0)
						{
							state.PercentDone = i * 100 / itemsToChange.Count();
							state.Breath();
						}
						if (DisableItem(hvoItem))
						{
							continue;
						}
						List<ICmObject> oldVals, newVal;
						ComputeValue(chosenObjs, hvoItem, out oldVals, out newVal);
						if (oldVals.SequenceEqual(newVal))
						{
							continue;
						}

						var newHvos = newVal.Select(obj => obj.Hvo).ToArray();
						var realTarget = hvoItem;
						if (m_ghostParentHelper != null)
						{
							realTarget = m_ghostParentHelper.FindOrCreateOwnerOfTargetProp(hvoItem, m_flid);
						}
						if (Atomic)
						{
							var newHvo = newHvos.Length > 0 ? newHvos[0] : 0;
							DataAccess.SetObjProp(realTarget, m_flid, newHvo);
						}
						else
						{
							DataAccess.Replace(realTarget, m_flid, 0, oldVals.Count, newHvos, newHvos.Length);
						}
					}
				});
		}

		public virtual void FakeDoit(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnabled, ProgressState state)
		{
			var chosenObjs = m_chosenObjs;
			var i = 0;
			// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
			var interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
			var tssChosenVal = BuildValueString(chosenObjs);
			foreach (var hvoItem in itemsToChange)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 100 / itemsToChange.Count();
					state.Breath();
				}
				var tssVal = tssChosenVal;
				var fEnable = false;
				if (!DisableItem(hvoItem))
				{
					List<ICmObject> oldVals;
					List<ICmObject> newVal;
					ComputeValue(chosenObjs, hvoItem, out oldVals, out newVal);
					fEnable = !oldVals.SequenceEqual(newVal);
					if (fEnable)
					{
						if (newVal != chosenObjs)
						{
							tssVal = BuildValueString(newVal);
						}
						m_sda.SetString(hvoItem, tagMadeUpFieldIdentifier, tssVal);
					}
				}
				m_sda.SetInt(hvoItem, tagEnabled, (fEnable ? 1 : 0));
			}
		}

		/// <summary>
		/// Tells SemanticDomainChooserBEditControl to make suggestions and then call FakeDoIt
		/// </summary>
		public virtual void MakeSuggestions(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnabled, ProgressState state)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// subclasses may override to determine if this hvoItem should be excluded.
		/// Basically a kludge to avoid the hassle of trying to figure a way to generate
		/// separate lists for variants/complex entry types since they target the same ListItemsClass (LexEntryRef).
		/// Currently EntriesOrChildClassesRecordList can only determine which virtual property
		/// to load based upon the target ListItemsClass (not a flid). And since we typically don't
		/// want the user to change variant types for complex entry refs (or vice-versa),
		/// we need some way to filter out items in the list based upon the selected column.
		/// </summary>
		protected virtual bool DisableItem(int hvoItem)
		{
			// by default we don't want to automatically exclude selected items from
			// bulk editing.
			return false;
		}

		protected List<ICmObject> GetOldVals(int hvoReal)
		{
			if (hvoReal == 0)
			{
				return new List<ICmObject>();
			}
			if (!Atomic)
			{
				return (m_cache.DomainDataByFlid as ISilDataAccessManaged).VecProp(hvoReal, m_flid).Select(hvo => m_cache.ServiceLocator.GetObject(hvo)).ToList();
			}
			var result = new List<ICmObject>();
			var val = m_cache.DomainDataByFlid.get_ObjectProp(hvoReal, m_flid);
			if (val != 0)
			{
				result.Add(m_cache.ServiceLocator.GetObject(val));
			}
			return result;
		}

		protected int GetRealHvo(int hvoItem)
		{
			return m_ghostParentHelper?.GetOwnerOfTargetProperty(hvoItem) ?? hvoItem;
		}

		protected virtual void ComputeValue(List<ICmObject> chosenObjs, int hvoItem, out List<ICmObject> oldVals, out List<ICmObject> newVal)
		{
			int hvoReal;
			// Check whether we can actually compute values for this item.  If not,
			// just return a pair of empty lists.  (See LT-11016 and LT-11357.)
			if (!CanActuallyComputeValuesFor(hvoItem, out hvoReal))
			{
				oldVals = new List<ICmObject>();
				newVal = oldVals;
				return;
			}

			oldVals = GetOldVals(hvoReal);
			newVal = chosenObjs;

			if (m_fRemove)
			{
				newVal = oldVals; // by default no change in remove mode.
				if (oldVals.Count > 0)
				{
					var newValues = new List<ICmObject>(oldVals);
					foreach (var obj in chosenObjs)
					{
						if (newValues.Contains(obj))
						{
							newValues.Remove(obj);
						}
					}
					newVal = newValues;
				}
			}
			else if (!ReplaceMode && oldVals.Count != 0)
			{
				// Need to handle as append.
				if (Atomic)
				{
					newVal = oldVals; // can't append to non-empty atomic value
				}
				else
				{
					var newValues = new List<ICmObject>(oldVals);
					foreach (var obj in chosenObjs)
					{
						if (!newValues.Contains(obj))
						{
							newValues.Add(obj);
						}
					}
					newVal = newValues;
				}
			}
		}

		protected bool CanActuallyComputeValuesFor(int hvoItem, out int hvoReal)
		{
			hvoReal = GetRealHvo(hvoItem);
			if (hvoReal == 0)
			{
				return true;
			}
			var clidItem = m_cache.ServiceLocator.ObjectRepository.GetClsid(hvoReal);
			var clidField = m_cache.MetaDataCacheAccessor.GetOwnClsId(m_flid);
			if (clidItem == clidField)
			{
				return true;
			}
			var baseClid = m_cache.MetaDataCacheAccessor.GetBaseClsId(clidItem);
			while (baseClid != clidField && baseClid != 0)
			{
				baseClid = m_cache.MetaDataCacheAccessor.GetBaseClsId(baseClid);
			}

			return baseClid == clidField;
		}

		private ITsString BuildValueString(IEnumerable<ICmObject> chosenObjs)
		{
			var bldr = TsStringUtils.MakeStrBldr();
			ITsString sep = null; // also acts as first-time flag.
			foreach (var obj in chosenObjs)
			{
				var pi = obj.GetType().GetProperty(m_displayNameProperty);
				var tss = (ITsString)pi.GetValue(obj, null);
				if (sep == null)
				{
					// first time create it
					sep = TsStringUtils.MakeString(", ", m_cache.ServiceLocator.WritingSystemManager.UserWs);
				}
				else
				{
					// subsequent times insert it.
					bldr.ReplaceTsString(bldr.Length, bldr.Length, sep);
				}
				bldr.ReplaceTsString(bldr.Length, bldr.Length, tss);
			}
			return bldr.Length > 0 ? bldr.GetString() : TsStringUtils.MakeString("", m_cache.ServiceLocator.WritingSystemManager.UserWs);
		}
		#endregion

		/// <summary>
		/// This type of editor can always select null.
		/// </summary>
		public bool CanClearField => true;

		/// <summary>
		/// And does it by setting the list to empty and using overwrite mode.
		/// </summary>
		public void SetClearField()
		{
			m_chosenObjs = new List<ICmObject>(0);
			m_fRemove = false;
			ReplaceMode = true;
		}

		public List<int> FieldPath => new List<int>(new[] { m_flid });
	}
}