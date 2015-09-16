// Copyright (c) 2003-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.CoreImpl;
using SIL.FieldWorks.Filters;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This is a record clerk that can be used in a disposable context such as in a
	/// guicontrol in a dialog. For example, a normal RecordClerk will publish that it has become the "ActiveClerk"
	/// whenever ActivateUI is called. We don't want this to happen for record clerks that will only be used in a dialog,
	/// because the "ActiveClerk" will then become disposed after the dialog closes.
	/// </summary>
	public class TemporaryRecordClerk : RecordClerk
	{
		/// <summary>
		/// Contructor.
		/// </summary>
		/// <param name="id">Clerk id/name.</param>
		/// <param name="recordList">Record list for the clerk.</param>
		/// <param name="defaultSorter">The default record sorter.</param>
		/// <param name="defaultSortLabel"></param>
		/// <param name="defaultFilter">The default filter to use.</param>
		/// <param name="allowDeletions"></param>
		/// <param name="shouldHandleDeletion"></param>
		internal TemporaryRecordClerk(string id, RecordList recordList, RecordSorter defaultSorter, string defaultSortLabel, RecordFilter defaultFilter, bool allowDeletions, bool shouldHandleDeletion)
			: base(id, recordList, defaultSorter, defaultSortLabel, defaultFilter, allowDeletions, shouldHandleDeletion)
		{
		}

		public override void ActivateUI(bool useRecordTreeBar)
		{
			// by default, we won't publish that we're the "ActiveClerk" or other usual effects.
			// but we do want to say that we're being actively used in a gui.
			m_fIsActiveInGui = true;
		}

		public override bool IsControllingTheRecordTreeBar
		{
			get
			{
				return true; // assume this will be true, say for instance in the context of a dialog.
			}
			set
			{
				// do not do anything here, unless you want to manage the "ActiveClerk" property.
			}
		}

		public override void OnPropertyChanged(string name)
		{
			// Objects of this class do not respond to 'propchanged' actions.
		}

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
			// If we have a RecordList, it shouldn't generate PropChanged messages.
			if (m_list != null)
				m_list.EnableSendPropChanged = false;
		}

		#endregion
	}
}