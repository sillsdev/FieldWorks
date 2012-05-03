// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ReferenceSlice.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System.Windows.Forms;
using System.Xml;

using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Base class for slices that edit object reference properties by launching a chooser.
	/// Control is expected to be a subclass of ReferenceLauncher.
	/// </summary>
	public abstract class ReferenceSlice : FieldSlice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceSlice"/> class.
		/// </summary>
		/// <param name="control">The control.</param>
		protected ReferenceSlice(Control control)
			: base(control)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceSlice"/> class.
		/// </summary>
		/// <param name="control">The control.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="obj">The obj.</param>
		/// <param name="flid">The flid.</param>
		protected ReferenceSlice(Control control, FdoCache cache, ICmObject obj, int flid)
			: base(control, cache, obj, flid)
		{
		}

		public override void FinishInit()
		{
			base.FinishInit();

			if (m_fieldName != null)
			{
				// have chooser title use the same text as the label
				if (Mediator != null && Mediator.HasStringTable)
					m_fieldName = XmlUtils.GetLocalizedAttributeValue(Mediator.StringTbl,
																	  m_configurationNode, "label", m_fieldName);
				else if (StringTbl != null)
					m_fieldName = XmlUtils.GetLocalizedAttributeValue(StringTbl,
																	  m_configurationNode, "label", m_fieldName);
				else
					m_fieldName = XmlUtils.GetOptionalAttributeValue(
						m_configurationNode, "label", m_fieldName);
			}
		}

		protected override void UpdateDisplayFromDatabase()
		{
			((ReferenceLauncher)Control).UpdateDisplayFromDatabase();
		}

		protected virtual string DisplayNameProperty
		{
			get
			{
				XmlNode parameters = ConfigurationNode.SelectSingleNode("deParams");
				if (parameters == null)
					return "";

				return XmlUtils.GetOptionalAttributeValue(parameters, "displayProperty", "");
			}
		}

		protected virtual string BestWsName
		{
			get
			{
				XmlNode parameters = ConfigurationNode.SelectSingleNode("deParams");
				if (parameters == null)
					return "analysis";

				return XmlUtils.GetOptionalAttributeValue(parameters, "ws", "analysis");
			}
		}
		/// <summary>
		/// Somehow a slice (I think one that has never scrolled to become visible?)
		/// can get an OnLoad message for its view in the course of deleting it from the
		/// parent controls collection. This can be bad (at best it's a waste of time
		/// to do the Layout in the OnLoad, but it can be actively harmful if the object
		/// the view is displaying has been deleted). So suppress it.
		/// </summary>
		public override void AboutToDiscard()
		{
			CheckDisposed();
			base.AboutToDiscard();
			var launcher = Control as ButtonLauncher;
			if (launcher == null)
				return;
			var rs = launcher.MainControl as SimpleRootSite;
			if (rs != null)
				rs.AboutToDiscard();
		}

		/// <summary>
		/// Set the Editable property on the launcher, which is created before installation, and
		/// then finish installing this slice.
		/// </summary>
		/// <param name="parent"></param>
		public override void Install(DataTree parent)
		{
			var launcher = Control as ReferenceLauncher;
			if (launcher != null)
				launcher.Editable = XmlUtils.GetOptionalBooleanAttributeValue(m_configurationNode, "editable", true);
			base.Install(parent);
		}
	}
}
