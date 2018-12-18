// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using System.Diagnostics;
using SIL.LCModel;
using SIL.Xml;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>Abstact class that is a base for slices that edit one particular field of one object.
	/// It knows how to retrieve the name of that field from the "field" attribute of the configuration,
	/// and supports an overrideable method UpdateDisplayFromDatabase which is called when the value
	/// of the field changes.</summary>
	internal abstract class FieldSlice : Slice
	{
		/// <summary>
		/// The field identifier for the attribute we are displaying.
		/// </summary>
		protected int m_flid = -1;

		protected string m_fieldName;

		/// <summary>
		/// Get the flid.
		/// </summary>
		public override int Flid => m_flid;

		/// <summary />
		protected FieldSlice(Control control)
			: base(control)
		{
		}

		/// <summary />
		protected FieldSlice(Control control, LcmCache cache, ICmObject obj, int flid)
			: base(control)
		{
			Debug.Assert(cache != null);
			Debug.Assert(obj != null);
			Cache = cache;
			MyCmObject = obj;
			m_flid = flid;
			m_fieldName = Cache.DomainDataByFlid.MetaDataCache.GetFieldName(m_flid);
		}

		/// <summary>
		/// Should put it into the same state as a newly created one.
		/// May not be valid for all subclasses; only needs to work for types where SliceFactory calls Reuse.
		/// </summary>
		public virtual void Reuse(ICmObject obj, int flid)
		{
			MyCmObject = obj;
			m_flid = flid;
			Label = null; // new slice normally has this
		}

		protected abstract void UpdateDisplayFromDatabase();

		protected internal override bool UpdateDisplayIfNeeded(int hvo, int tag)
		{
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
		public override void Install(DataTree parentDataTree)
		{
			base.Install(parentDataTree);

			UpdateDisplayFromDatabase();
			Control.AccessibleName = Label;
		}

		protected void SetFieldFromConfig()
		{
			Debug.Assert(Cache != null);
			Debug.Assert(ConfigurationNode != null);

			var className = Cache.DomainDataByFlid.MetaDataCache.GetClassName(MyCmObject.ClassID);
			m_fieldName = XmlUtils.GetMandatoryAttributeValue(ConfigurationNode, "field");
			var mdc = Cache.DomainDataByFlid.MetaDataCache;
			m_flid = mdc.GetFieldId2(mdc.GetClassId(className), m_fieldName, true);
		}
	}
}