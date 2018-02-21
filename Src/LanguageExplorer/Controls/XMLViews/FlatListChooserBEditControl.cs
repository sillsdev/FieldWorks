// Copyright (c) 2005-2018 SIL International
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
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This class implements setting an atomic reference property to a value chosen from a flat list,
	/// by means of a combo box showing the items in the list.
	/// </summary>
	internal class FlatListChooserBEditControl : IBulkEditSpecControl, IGhostable, IDisposable
	{
		protected XMLViewsDataCache m_sda;
		protected FwComboBox m_combo;
		protected int m_ws;
		protected int m_hvoList;
		protected bool m_useAbbr;
		protected int m_flidAtomicProp;
		internal IVwStylesheet m_stylesheet;
		GhostParentHelper m_ghostParentHelper;

		public Button SuggestButton => null;

		public event FwSelectionChangedEventHandler ValueChanged;

		public FlatListChooserBEditControl(int flidAtomicProp, int hvoList, int ws, bool useAbbr)
		{
			m_ws = ws;
			m_hvoList = hvoList;
			m_useAbbr = useAbbr;
			m_flidAtomicProp = flidAtomicProp;
		}

		#region IBulkEditSpecControl Members

		/// <summary>
		/// Get/Set the property table.
		/// </summary>
		public IPropertyTable PropertyTable { get; set; }

		public Control Control
		{
			get
			{
				if (m_combo == null)
				{
					FillComboBox();
				}
				return m_combo;
			}
		}

		public LcmCache Cache { get; set; }

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
		public IVwStylesheet Stylesheet
		{
			set
			{
				m_stylesheet = value;
				if (m_combo != null)
				{
					m_combo.StyleSheet = value;
				}
			}
		}

		public virtual void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
		{
			UndoableUnitOfWorkHelper.Do(XMLViewsStrings.ksUndoBulkEdit, XMLViewsStrings.ksRedoBulkEdit,
				Cache.ActionHandlerAccessor, () =>
				{
					var sda = Cache.DomainDataByFlid;
					var item = m_combo.SelectedItem as HvoTssComboItem;
					if (item == null)
					{
						return;
					}
					var hvoSel = item.Hvo;
					var mdcManaged = Cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
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
						// If the field is on an owned object that might not exist, the hvoItem might
						// refer to the owner, which is likely of a different class.  In such cases,
						// we don't want to try getting the field value, since that produces a pretty
						// green dialog box for the user.  See FWR-3199.
						var clid = m_sda.get_IntProp(hvoItem, CmObjectTags.kflidClass);
						var flids = mdcManaged.GetFields(clid, true, (int)CellarPropertyTypeFilter.All);
						if (!flids.Contains(m_flidAtomicProp))
						{
							continue;
						}
						var hvoOld = GetOriginalListValue(sda, hvoItem);
						if (hvoOld == hvoSel)
						{
							continue;
						}
						UpdateListItemToNewValue(sda, hvoItem, hvoSel, hvoOld);
					}
				});
		}

		/// <summary />
		protected virtual void UpdateListItemToNewValue(ISilDataAccess sda, int hvoItem, int hvoSel, int hvoOld)
		{
			var hvoOwningAtomic = hvoItem;
			if (hvoOld == 0 && m_ghostParentHelper != null)
			{
				// it's possible that hvoItem is actually the owner of a object that needs to be created
				// for hvoSel to be set.
				hvoOwningAtomic = m_ghostParentHelper.FindOrCreateOwnerOfTargetProp(hvoItem, m_flidAtomicProp);
			}
			sda.SetObjProp(hvoOwningAtomic, m_flidAtomicProp, hvoSel);
		}

		/// <summary>
		/// This is called when the preview button is clicked. The control is passed
		/// the list of currently active (filtered and checked) items. It should cache
		/// tagEnabled to zero for any objects that can't be
		/// modified. For ones that can, it should set the string property tagMadeUpFieldIdentifier
		/// to the value to show in the 'modified' fields.
		/// </summary>
		public virtual void FakeDoit(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnabled, ProgressState state)
		{
			var sda = Cache.DomainDataByFlid;
			var item = m_combo.SelectedItem as HvoTssComboItem;
			if (item == null)
			{
				return;
			}
			var hvoSel = item.Hvo;
			var mdcManaged = Cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			var i = 0;
			// Report progress 50 times or every 100 items, whichever is more
			// (but no more than once per item!)
			var interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
			foreach(var hvoItem in itemsToChange)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 100 / itemsToChange.Count();
					state.Breath();
				}
				bool fEnable;
				// If the field is on an owned object that might not exist, the hvoItem might
				// refer to the owner, which is likely of a different class.  In such cases,
				// we don't want to try getting the field value, since that produces a pretty
				// green dialog box for the user.  See FWR-3199.
				var clid = m_sda.get_IntProp(hvoItem, CmObjectTags.kflidClass);
				var flids = mdcManaged.GetFields(clid, true, (int)CellarPropertyTypeFilter.All);
				if (!flids.Contains(m_flidAtomicProp))
				{
					fEnable = false;
				}
				else
				{
					var hvoOld = GetOriginalListValue(sda, hvoItem);
					fEnable = hvoOld != hvoSel;
				}
				if (fEnable)
				{
					m_sda.SetString(hvoItem, tagMadeUpFieldIdentifier, item.AsTss);
				}
				m_sda.SetInt(hvoItem, tagEnabled, (fEnable ? 1 : 0));
			}
		}

		/// <summary>
		/// It's possible that hvoItem will be an owner of a ghost (ie. an absent source object for m_flidAtomicProp).
		/// In that case, the cache should return 0.
		/// </summary>
		private int GetOriginalListValue(ISilDataAccess sda, int hvoItem)
		{
			return sda.get_ObjectProp(hvoItem, m_flidAtomicProp);
		}

		/// <summary>
		/// Used by SemanticDomainChooserBEditControl to make suggestions and then call FakeDoIt
		/// </summary>
		public void MakeSuggestions(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnabled, ProgressState state)
		{
			throw new NotSupportedException();
		}

		#endregion

		protected virtual void FillComboBox()
		{
			m_combo = new FwComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				WritingSystemFactory = Cache.WritingSystemFactory,
				WritingSystemCode = m_ws,
				StyleSheet = m_stylesheet
			};
			var labeledList = GetLabeledList();
			// if the possibilities list IsSorted (misnomer: needs to be sorted), do that now.
			if (labeledList.Count > 1) // could be zero if list non-existant, if 1 don't need to sort either!
			{
				if (Cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>().GetObject(m_hvoList).IsSorted)
				{
					labeledList.Sort();
				}
			}
			// now add list to combo box in that order.
			foreach (var labelItem in labeledList)
			{
				m_combo.Items.Add(new HvoTssComboItem(labelItem.Hvo, labelItem.TssLabel));
			}
			// Don't allow <Not Sure> for MorphType selection.  See FWR-1632.
			if (m_hvoList != Cache.LangProject.LexDbOA.MorphTypesOA.Hvo)
			{
				m_combo.Items.Add(new HvoTssComboItem(0, TsStringUtils.MakeString(XMLViewsStrings.ksNotSure, Cache.WritingSystemFactory.UserWs)));
			}
			m_combo.SelectedIndexChanged += m_combo_SelectedIndexChanged;
		}

		private List<HvoLabelItem> GetLabeledList()
		{
			var tagName = m_useAbbr ? CmPossibilityTags.kflidAbbreviation : CmPossibilityTags.kflidName;
			var chvo = m_hvoList > 0 ? Cache.DomainDataByFlid.get_VecSize(m_hvoList, CmPossibilityListTags.kflidPossibilities) : 0;
			var al = new List<HvoLabelItem>(chvo);
			for (var i = 0; i < chvo; i++)
			{
				var hvoChild = Cache.DomainDataByFlid.get_VecItem(m_hvoList, CmPossibilityListTags.kflidPossibilities, i);
				al.Add(new HvoLabelItem(hvoChild, GetItemLabel(hvoChild, tagName)));
			}
			return al;
		}

		/// <summary>
		/// Gets the item label.
		/// </summary>
		internal ITsString GetItemLabel(int hvoChild, int tagName)
		{
			// Try getting the label with the user writing system.
			var tssLabel = Cache.DomainDataByFlid.get_MultiStringAlt(hvoChild, tagName, m_ws);

			// If that doesn't work, try using the default user writing system.
			if (string.IsNullOrEmpty(tssLabel?.Text))
			{
				tssLabel = Cache.DomainDataByFlid.get_MultiStringAlt(hvoChild, tagName, Cache.ServiceLocator.WritingSystemManager.UserWs);
			}

			// If that doesn't work, then fallback to the whatever the cache considers
			// to be the fallback writing system (probably english).
			if (string.IsNullOrEmpty(tssLabel?.Text))
			{
				tssLabel = Cache.DomainDataByFlid.get_MultiStringAlt(hvoChild, tagName, WritingSystemServices.FallbackUserWs(Cache));
			}

			return tssLabel;
		}

		/// <summary>
		/// Handles the SelectedIndexChanged event of the m_combo control.
		/// </summary>
		protected void m_combo_SelectedIndexChanged(object sender, EventArgs e)
		{
			// Tell the parent control that we may have changed the selected item so it can
			// enable or disable the Apply and Preview buttons based on the selection.
			if (ValueChanged == null)
			{
				return;
			}
			var index = m_combo.SelectedIndex;
			var htci = m_combo.SelectedItem as HvoTssComboItem;
			var hvo = htci?.Hvo ?? 0;
			ValueChanged(sender, new FwObjectSelectionEventArgs(hvo, index));
		}

		/// <summary>
		/// This type of editor can always select null.
		/// </summary>
		public bool CanClearField => true;

		/// <summary>
		/// And does it by choosing the final, 'Not sure' item.
		/// </summary>
		public void SetClearField()
		{
			if (m_combo == null)
			{
				FillComboBox();
			}
			m_combo.SelectedIndex = m_combo.Items.Count - 1;
		}

		public virtual List<int> FieldPath => new List<int>(new int[] { m_flidAtomicProp });

		#region Disposable stuff
#if DEBUG
		/// <summary/>
		~FlatListChooserBEditControl()
		{
			Dispose(false);
		}
#endif

		/// <summary />
		public bool IsDisposed { get; private set; }

		/// <summary />
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary />
		protected virtual void Dispose(bool fDisposing)
		{
			Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				m_combo?.Dispose();
			}
			m_combo = null;
			IsDisposed = true;
		}
		#endregion

		#region IGhostable Members

		public void InitForGhostItems(LcmCache cache, XElement colSpec)
		{
			m_ghostParentHelper = BulkEditBar.GetGhostHelper(cache.ServiceLocator, colSpec);
		}

		#endregion
	}
}