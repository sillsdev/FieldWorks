// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas
{
	internal class SubservientRecordList : RecordList
	{
		/// <summary>
		/// When this is not null, that means there is another record list managing a list,
		/// and the selected item of that list provides the object that this
		/// record list gets items out of. For example, the WfiAnalysis record list
		/// is dependent on the WfiWordform record list to tell it which wordform it is supposed to
		/// be displaying the analyses of.
		/// </summary>
		private IRecordList _recordListProvidingRootObject;

		internal SubservientRecordList(string id, StatusBar statusBar, ISilDataAccessManaged decorator, bool usingAnalysisWs, VectorPropertyParameterObject vectorPropertyParameterObject, IRecordList recordListProvidingRootObject, RecordFilterParameterObject recordFilterParameterObject = null)
			: base(id, statusBar, decorator, usingAnalysisWs, vectorPropertyParameterObject, recordFilterParameterObject)
		{
			Guard.AgainstNull(recordListProvidingRootObject, nameof(recordListProvidingRootObject));

			_recordListProvidingRootObject = recordListProvidingRootObject;
			((RecordList)_recordListProvidingRootObject)._subservientRecordList = this;
		}

		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			Subscriber.Subscribe(_recordListProvidingRootObject.PersistedIndexProperty, SelectedItemChangedHandler);
			Subscriber.Subscribe(DependentPropertyName, DependentPropertyName_Handler);
		}

		private void SelectedItemChangedHandler(object obj)
		{
			ReallyResetOwner(_recordListProvidingRootObject.CurrentObject);
		}

		protected virtual void ReallyResetOwner(ICmObject selectedObject)
		{
			if (ReferenceEquals(OwningObject, selectedObject))
			{
				return;
			}
			OwningObject = selectedObject;
		}

		private string DependentPropertyName => RecordListSelectedObjectPropertyId(_recordListProvidingRootObject.Id);

		private void DependentPropertyName_Handler(object obj)
		{
			UpdateOwningObject(true);
		}

		#region Overrides of RecordList

		public override bool IsSubservientRecordList => true;

		protected override void OnRecordChanged(RecordNavigationEventArgs e)
		{
			ReallyResetOwner(_recordListProvidingRootObject.CurrentObject);
			base.OnRecordChanged(e);
		}

		protected override bool TryListProvidingRootObject(out IRecordList recordListProvidingRootObject)
		{
			recordListProvidingRootObject = _recordListProvidingRootObject;
			return true;
		}

		public override void UpdateOwningObject(bool updateOwningObjectOnlyIfChanged = false)
		{
			var oldOwningObject = OwningObject;
			ICmObject newOwningObject = null;
			var rni = PropertyTable.GetValue<RecordNavigationInfo>(DependentPropertyName);
			if (rni != null)
			{
				Debug.Assert(ReferenceEquals(rni.MyRecordList, _recordListProvidingRootObject), "How can the two record lists not be the same?");
				newOwningObject = rni.MyRecordList.CurrentObject;

			}
			if (newOwningObject == null)
			{
				if (OwningObject != null)
				{
					OwningObject = null;
				}
				return;
			}
			using (var luh = new ListUpdateHelper(new ListUpdateHelperParameterObject { MyRecordList = this }))
			{
				// in general we want to actually reload the list if something as
				// radical as changing the OwningObject occurs, since many subsequent
				// events and messages depend upon this information.
				luh.TriggerPendingReloadOnDispose = true;
				luh.SkipShowRecord = rni.SkipShowRecord;
				if (!updateOwningObjectOnlyIfChanged || !ReferenceEquals(oldOwningObject, newOwningObject))
				{
					ReallyResetOwner(newOwningObject);
				}
			}
			if (!ReferenceEquals(oldOwningObject, newOwningObject) && OwningObject != null)
			{
				Publisher.Publish("RecordListOwningObjChanged", this);
			}
		}

		protected override bool IsPrimaryRecordList => false;

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				Subscriber.Unsubscribe(_recordListProvidingRootObject.PersistedIndexProperty, SelectedItemChangedHandler);
				Subscriber.Unsubscribe(DependentPropertyName, DependentPropertyName_Handler);
			}
			_recordListProvidingRootObject = null;

			base.Dispose(disposing);
		}

		#endregion
	}
}