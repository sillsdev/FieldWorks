// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	internal abstract class BulkEditSpecControl : IBulkEditSpecControl, IGhostable
	{
		protected LcmCache m_cache;
		protected XMLViewsDataCache m_sda;
		protected GhostParentHelper m_ghostParentHelper;
		public event FwSelectionChangedEventHandler ValueChanged;

		#region IBulkEditSpecControl Members

		/// <summary>
		/// Get/Set the property table.
		/// </summary>
		public IPropertyTable PropertyTable { get; set; }

		public LcmCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}

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

		public virtual Control Control
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		/// <summary>
		/// Required interface member not yet used.
		/// </summary>
		public virtual IVwStylesheet Stylesheet
		{
			set {  }
		}

		public Button SuggestButton => null;

		public virtual void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
		{
			throw new NotSupportedException();
		}

		public virtual void FakeDoit(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnabled, ProgressState state)
		{
			throw new NotSupportedException();
		}

		public virtual bool CanClearField
		{
			get { throw new NotSupportedException(); }
		}

		public virtual void SetClearField()
		{
			throw new NotSupportedException();
		}

		public virtual List<int> FieldPath
		{
			get { throw new NotSupportedException(); }
		}

		protected void OnValueChanged(object sender, FwObjectSelectionEventArgs args)
		{
			ValueChanged?.Invoke(sender, args);
		}

		/// <summary>
		/// Used by SemanticDomainChooserBEditControl to make suggestions and then call FakeDoIt
		/// </summary>
		public void MakeSuggestions(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnabled, ProgressState state)
		{
			throw new NotSupportedException();
		}

		#endregion

		#region IGhostable Members

		public virtual void InitForGhostItems(LcmCache cache, XElement colSpec)
		{
			m_ghostParentHelper = BulkEditBar.GetGhostHelper(cache.ServiceLocator, colSpec);
		}

		/// <summary>
		/// It's possible that hvoItem will be an owner of a ghost (ie. an absent source object for m_flidAtomicProp).
		/// In that case, the cache should return 0.
		/// </summary>
		/// <param name="sda"></param>
		/// <param name="hvoItem"></param>
		/// <param name="value">the original list value, if we could get at one.</param>
		/// <returns>false if we couldn't get the list value (e.g. if we need to create an item to get the value)</returns>
		protected virtual bool TryGetOriginalListValue(ISilDataAccess sda, int hvoItem, out int value)
		{
			value = int.MinValue;
			return false; // override
		}
		protected virtual void UpdateListItemToNewValue(ISilDataAccess sda, int hvoItem, int newVal, int oldVal)
		{
		}

		#endregion
	}
}