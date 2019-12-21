// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls.XMLViews;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas
{
	internal sealed class SubservientRecordList : RecordList
	{
		/// <summary>
		/// When this is not null, that means there is another record list managing a list,
		/// and the selected item of that list provides the object that this
		/// record list gets items out of. For example, the WfiAnalysis record list
		/// is dependent on the WfiWordform record list to tell it which wordform it is supposed to
		/// be displaying the analyses of.
		/// </summary>
		private IRecordList _recordListProvidingRootObject;
		private ConcDecorator _decorator;

		internal SubservientRecordList(string id, StatusBar statusBar, ISilDataAccessManaged decorator, bool usingAnalysisWs, VectorPropertyParameterObject vectorPropertyParameterObject, IRecordList recordListProvidingRootObject, RecordFilterParameterObject recordFilterParameterObject = null)
			: base(id, statusBar, decorator, usingAnalysisWs, vectorPropertyParameterObject, recordFilterParameterObject)
		{
			Guard.AgainstNull(recordListProvidingRootObject, nameof(recordListProvidingRootObject));

			_recordListProvidingRootObject = recordListProvidingRootObject;
			((RecordList)_recordListProvidingRootObject)._subservientRecordList = this;
		}

		/// <summary />
		internal SubservientRecordList(string id, StatusBar statusBar, ConcDecorator decorator, bool usingAnalysisWs, int flid, IRecordList recordListProvidingRootObject)
			: this(id, statusBar, decorator, usingAnalysisWs, new VectorPropertyParameterObject(null, string.Empty, flid, false), recordListProvidingRootObject)
		{
			Guard.AgainstNull(decorator, nameof(decorator));

			_decorator = decorator;
		}

		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			Subscriber.Subscribe(_recordListProvidingRootObject.PersistedIndexProperty, SelectedItemChangedHandler);
			Subscriber.Subscribe(DependentPropertyName, DependentPropertyName_Handler);
		}

		private void DependentPropertyName_Handler(object obj)
		{
			UpdateOwningObject(true);
		}

		private void SelectedItemChangedHandler(object obj)
		{
			ReallyResetOwner((IAnalysis)_recordListProvidingRootObject.CurrentObject);
		}

		private void ReallyResetOwner(IAnalysis selectedAnalysis)
		{
			_decorator?.UpdateAnalysisOccurrences(selectedAnalysis, true);
			((ObjectListPublisher)VirtualListPublisher).CacheVecProp(selectedAnalysis.Hvo, _decorator.VecProp(selectedAnalysis.Hvo, ConcDecorator.kflidWfOccurrences));
			OwningObject = selectedAnalysis;
		}

		private string DependentPropertyName => RecordListSelectedObjectPropertyId(_recordListProvidingRootObject.Id);

		#region Overrides of RecordList

		public override bool IsSubservientRecordList => true;

		protected override bool TryListProvidingRootObject(out IRecordList recordListProvidingRootObject)
		{
			recordListProvidingRootObject = _recordListProvidingRootObject;
			return true;
		}

		protected override void OnRecordChanged(RecordNavigationEventArgs e)
		{
			ReallyResetOwner((IAnalysis)_recordListProvidingRootObject.CurrentObject);
			base.OnRecordChanged(e);
		}

		public override void UpdateOwningObject(bool updateOwningObjectOnlyIfChanged = false)
		{
			var oldOwningObject = OwningObject;
			IAnalysis newOwningObject = null;
			var rni = PropertyTable.GetValue<RecordNavigationInfo>(DependentPropertyName);
			if (rni != null)
			{
				Debug.Assert(ReferenceEquals(rni.MyRecordList, _recordListProvidingRootObject), "How can the two record lists not be the same?");
				newOwningObject = (IAnalysis)rni.MyRecordList.CurrentObject;

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
			}
			_decorator = null;
			_recordListProvidingRootObject = null;

			base.Dispose(disposing);
		}

		protected override bool IsPrimaryRecordList => false;

		#endregion
	}
}