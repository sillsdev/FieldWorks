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
using System.Xml;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Summary description for ReferenceSlice.
	/// </summary>
	public abstract class ReferenceSlice : FieldSlice
	{
		/// <summary>
		/// Default Constructor.
		/// </summary>
		public ReferenceSlice() : base()
		{
		}
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceSlice"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ReferenceSlice(FdoCache cache, ICmObject obj, int flid,
			XmlNode configurationNode, IPersistenceProvider persistenceProvider,
			Mediator mediator, StringTable stringTbl)
			: base(null, cache, obj, flid)
		{
			ConfigurationNode = configurationNode;//todo: remove this when FieldSlice gets the node as part of its constructor
			// have chooser title use the same text as the label
			if (mediator != null && mediator.HasStringTable)
				m_fieldName = XmlUtils.GetLocalizedAttributeValue(mediator.StringTbl,
					configurationNode, "label", m_fieldName);
			else if (stringTbl != null)
				m_fieldName = XmlUtils.GetLocalizedAttributeValue(stringTbl,
					configurationNode, "label", m_fieldName);
			else
				m_fieldName = XmlUtils.GetOptionalAttributeValue(
					configurationNode, "label", m_fieldName);

			SetupControls(persistenceProvider, mediator, stringTbl);
		}

		/// <summary>
		/// Sets up the ChooserLauncher control.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this to create the m_leftControl.
		/// </remarks>
		protected abstract void SetupControls(IPersistenceProvider persistenceProvider, Mediator mediator, StringTable stringTbl);

		protected override void UpdateDisplayFromDatabase()
		{
			((ReferenceLauncher)this.Control).UpdateDisplayFromDatabase();
		}

		protected string DisplayNameProperty
		{
			get
			{
				XmlNode parameters = ConfigurationNode.SelectSingleNode("deParams");
				if (parameters == null)
					return "";
				else
					return XmlUtils.GetOptionalAttributeValue(parameters, "displayProperty", "");
			}
		}

		protected string BestWsName
		{
			get
			{
				XmlNode parameters = ConfigurationNode.SelectSingleNode("deParams");
				if (parameters == null)
					return "analysis";
				else
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
			ButtonLauncher launcher = this.Control as ButtonLauncher;
			if (launcher == null)
				return;
			SIL.FieldWorks.Common.RootSites.SimpleRootSite rs = launcher.MainControl as SIL.FieldWorks.Common.RootSites.SimpleRootSite;
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
			ReferenceLauncher launcher = Control as ReferenceLauncher;
			if (launcher != null)
				launcher.Editable = XmlUtils.GetOptionalBooleanAttributeValue(m_configurationNode, "editable", true);
			base.Install(parent);
		}


	}
}
