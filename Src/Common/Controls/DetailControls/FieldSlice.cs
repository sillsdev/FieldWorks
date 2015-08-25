// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FieldSlice.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>Abstact class that is a base for slices that edit one particular field of one object.
	/// It knows how to retrieve the name of that field from the "field" attribute of the configuration,
	/// and supports an overrideable method UpdateDisplayFromDatabase which is called when the value
	/// of the field changes.</summary>
	public abstract class FieldSlice : Slice
	{
		/// <summary>
		/// The field identifier for the attribute we are displaying.
		/// </summary>
		protected int m_flid = -1;

		protected string m_fieldName;

		/// <summary>
		/// Get the flid.
		/// </summary>
		public override int Flid
		{
			get
			{
				CheckDisposed();
				return m_flid;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FieldSlice"/> class.
		/// </summary>
		/// <param name="control">The control.</param>
		protected FieldSlice(Control control)
			: base(control)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="control">The control.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="obj">CmObject that is being displayed.</param>
		/// <param name="flid">The field identifier for the attribute we are displaying.</param>
		protected FieldSlice(Control control, FdoCache cache, ICmObject obj, int flid)
			: base(control)
		{
			Debug.Assert(cache != null);
			Debug.Assert(obj != null);
			m_cache = cache;
			Object = obj;
			m_flid = flid;
			m_fieldName = m_cache.DomainDataByFlid.MetaDataCache.GetFieldName(m_flid);
		}

		/// <summary>
		/// Should put it into the same state as a newly created one.
		/// May not be valid for all subclasses; only needs to work for types where SliceFactory calls Reuse.
		/// </summary>
		public virtual void Reuse(ICmObject obj, int flid)
		{
			Object = obj;
			m_flid = flid;
			Label = null; // new slice normally has this
		}

		protected abstract void UpdateDisplayFromDatabase();

		internal protected override bool UpdateDisplayIfNeeded(int hvo, int tag)
		{
			CheckDisposed();
			if (tag == Flid)
			{
				UpdateDisplayFromDatabase();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Called when the slice is first created, but also when it is
		/// "reused" (e.g. refresh or new target object)
		/// </summary>
		/// <param name="parent"></param>
		public override void Install(DataTree parent)
		{
			CheckDisposed();

			base.Install(parent);

			UpdateDisplayFromDatabase();
			Control.AccessibleName = Label;
		}

		protected void SetFieldFromConfig()
		{
			Debug.Assert(m_cache != null);
			Debug.Assert(m_configurationNode != null);

			string className = m_cache.DomainDataByFlid.MetaDataCache.GetClassName(m_obj.ClassID);
			m_fieldName = XmlUtils.GetManditoryAttributeValue(m_configurationNode, "field");
			var mdc = m_cache.DomainDataByFlid.MetaDataCache;
			m_flid = mdc.GetFieldId2(mdc.GetClassId(className), m_fieldName, true);
		}
	}
}