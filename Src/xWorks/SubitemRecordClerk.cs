using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// A SubItemRecordClerk has an additional notion of the current item. Within the current item of the
	/// RecordList, a smaller item may be selected. For example, the main list may be of top-level
	/// RnGenericRecords, but the SubItemRecordClerk can trak owned records.
	/// Currently, the subitem must be owned by the top-level item, and displayed in the document view
	/// using direct owning relationships. Possible subitems are configured by noting the property
	/// that can contain them (possibly recursively).
	/// </summary>
	public class SubitemRecordClerk : RecordClerk
	{
		internal int SubitemFlid { get; private set; }

		#region Overrides of RecordClerk

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		public override void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			base.InitializeFlexComponent(propertyTable, publisher, subscriber);
#if RANDYTODO
			// TODO: Figure out how to set these parameters when the xml is gone.
			XmlNode clerkConfiguration = ToolConfiguration.GetClerkNodeFromToolParamsNode(viewConfiguration);
			var subitemNames = XmlUtils.GetManditoryAttributeValue(clerkConfiguration, "field").Split('.');
			SubitemFlid = Cache.MetaDataCacheAccessor.GetFieldId(subitemNames[0].Trim(), subitemNames[1].Trim(), true);
#endif
		}

		#endregion

		public ICmObject Subitem { get; set; }
		public bool UsedToSyncRelatedClerk { get; set; }

		internal override void SetSubitem(ICmObject subitem)
		{
			base.SetSubitem(subitem);
			Subitem = subitem;
		}

		internal override void ViewChangedSelectedRecord(SIL.FieldWorks.Common.FwUtils.FwObjectSelectionEventArgs e, SIL.FieldWorks.Common.COMInterfaces.IVwSelection sel)
		{
			base.ViewChangedSelectedRecord(e, sel);
			UsedToSyncRelatedClerk = false;
			if (sel == null)
				return;
			// See if we can make an appropriate Subitem selection.
			var clevels = sel.CLevels(false);
			if (clevels < 2)
				return; // paranoia.
			// The object we get with level = clevels - 1 is the root of the whole view, which is of no interest.
			// The one with clevels - 2 is one of the objects in the top level of the list.
			// We get that initially, along with the tag that determines whether we can drill deeper.
			// Starting with clevels - 3, if there are that many, we keep getting more levels
			// as long as there are some and the previous level had the right tag.
			int hvoObj, tag, ihvo, cpropPrevious;
			IVwPropertyStore vps;
			sel.PropInfo(false, clevels - 2, out hvoObj, out tag, out ihvo,
				out cpropPrevious, out vps);
			int hvoTarget = hvoObj;
			for (int index = clevels - 3; index >= 0 && tag == SubitemFlid; index --)
			{
				sel.PropInfo(false, index, out hvoTarget, out tag, out ihvo,
					out cpropPrevious, out vps);
			}
			if (hvoTarget != hvoObj)
			{
				// we did some useful drilling.
				Subitem = Cache.ServiceLocator.GetObject(hvoTarget);
			}
			else
			{
				Subitem = null; // no relevant subitem.
			}
		}

		protected override void ClearInvalidSubitem()
		{
			if (Subitem == null)
				return; // nothing to do.
			if (!Subitem.IsOwnedBy(CurrentObject))
				Subitem = null; // not valid to try to select it as part of selecting current object.
		}
	}
}
