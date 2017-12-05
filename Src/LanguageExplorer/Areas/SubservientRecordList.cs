// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.Works;
using SIL.Code;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Areas
{
	internal sealed class SubservientRecordList : RecordList
	{
		/// <summary>
		/// when this is not null, that means there is another clerk managing a list,
		/// and the selected item of that list provides the object that this
		/// RecordClerk gets items out of. For example, the WfiAnalysis clerk
		/// is dependent on the WfiWordform clerk to tell it which wordform it is supposed to
		/// be displaying the analyses of.
		/// </summary>
		private IRecordClerk _clerkProvidingRootObject;

		internal SubservientRecordList(string id, StatusBar statusBar, RecordFilter defaultFilter, bool allowDeletions, bool shouldHandleDeletion, ISilDataAccessManaged decorator, bool usingAnalysisWs, int flid, ICmObject owner, string propertyName, IRecordClerk clerkProvidingRootObject)
			: base(id, statusBar, new PropertyRecordSorter("ShortName"), AreaServices.Default, defaultFilter, allowDeletions, shouldHandleDeletion, decorator, usingAnalysisWs, flid, owner, propertyName)
		{
			Guard.AgainstNull(clerkProvidingRootObject, nameof(clerkProvidingRootObject));

			_clerkProvidingRootObject = clerkProvidingRootObject;
		}

		internal SubservientRecordList(string id, StatusBar statusBar, ISilDataAccessManaged decorator, bool usingAnalysisWs, int flid, IRecordClerk clerkProvidingRootObject)
		{
			Guard.AgainstNull(clerkProvidingRootObject, nameof(clerkProvidingRootObject));
			Guard.AgainstNullOrEmptyString(id, nameof(id));
			Guard.AgainstNull(statusBar, nameof(statusBar));
			Guard.AgainstNull(decorator, nameof(decorator));

			Id = id;
			_statusBar = statusBar;
			m_objectListPublisher = new ObjectListPublisher(decorator, RecordListFlid);
			m_usingAnalysisWs = usingAnalysisWs;
			m_propertyName = string.Empty;
			m_fontName = MiscUtils.StandardSansSerif;
			// Only other current option is to specify an ordinary property (or a virtual one).
			m_flid = flid;
			// Review JohnH(JohnT): This is only useful for dependent clerks, but I don't know how to check this is one.
			m_owningObject = null;
			_clerkProvidingRootObject = clerkProvidingRootObject;
		}

		private string DependentPropertyName => ClerkSelectedObjectPropertyId(_clerkProvidingRootObject.Id);

		#region Overrides of RecordList
		public override bool TryClerkProvidingRootObject(out IRecordClerk clerkProvidingRootObject)
		{
			clerkProvidingRootObject = _clerkProvidingRootObject;
			return true;
		}

		protected override void UpdateOwningObject(bool fUpdateOwningObjectOnlyIfChanged = false)
		{
			var old = OwningObject;
			ICmObject newObj = null;
			var rni = PropertyTable.GetValue<RecordNavigationInfo>(DependentPropertyName);
			if (rni != null)
			{
				newObj = rni.Clerk.CurrentObject;
			}
			using (var luh = new ListUpdateHelper(this))
			{
				// in general we want to actually reload the list if something as
				// radical as changing the OwningObject occurs, since many subsequent
				// events and messages depend upon this information.
				luh.TriggerPendingReloadOnDispose = true;
				if (rni != null)
				{
					luh.SkipShowRecord = rni.SkipShowRecord;
				}
				if (!fUpdateOwningObjectOnlyIfChanged || !ReferenceEquals(old, newObj))
				{
					OwningObject = newObj;
				}
			}
			if (!ReferenceEquals(old, newObj))
			{
				Publisher.Publish("ClerkOwningObjChanged", this);
			}
		}

		public override void OnPropertyChanged(string name)
		{
			if (name == CurrentFilterPropertyTableId)
			{
				base.OnPropertyChanged(name);
				return;
			}
			if (name == DependentPropertyName)
			{
				UpdateOwningObjectIfNeeded();
			}
		}

		protected override void DisposeUnmanagedResources()
		{
			_clerkProvidingRootObject = null;

			base.DisposeUnmanagedResources();
		}
		protected override bool IsPrimaryClerk => false;

		#endregion
	}
}