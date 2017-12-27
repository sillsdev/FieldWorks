// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.Works;
using SIL.Code;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Utils;

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

		internal SubservientRecordList(string id, StatusBar statusBar, ISilDataAccessManaged decorator, bool usingAnalysisWs, VectorPropertyParameterObject vectorPropertyParameterObject, IRecordList recordListProvidingRootObject, RecordFilterParameterObject recordFilterParameterObject = null)
			: base(id, statusBar, decorator, usingAnalysisWs, vectorPropertyParameterObject, recordFilterParameterObject)
		{
			Guard.AgainstNull(recordListProvidingRootObject, nameof(recordListProvidingRootObject));

			_recordListProvidingRootObject = recordListProvidingRootObject;
		}

		/// <summary />
		/// <remarks>
		/// This constructor uses the default (parameterless) constructor of RecordList.
		/// </remarks>
		internal SubservientRecordList(string id, StatusBar statusBar, ISilDataAccessManaged decorator, bool usingAnalysisWs, int flid, IRecordList recordListProvidingRootObject)
		{
			Guard.AgainstNull(recordListProvidingRootObject, nameof(recordListProvidingRootObject));
			Guard.AgainstNullOrEmptyString(id, nameof(id));
			Guard.AgainstNull(statusBar, nameof(statusBar));
			Guard.AgainstNull(decorator, nameof(decorator));

			Id = id;
			_statusBar = statusBar;
			m_objectListPublisher = new ObjectListPublisher(decorator, RecordListFlid);
			m_usingAnalysisWs = usingAnalysisWs;
			PropertyName = string.Empty;
			m_fontName = MiscUtils.StandardSansSerif;
			// Only other current option is to specify an ordinary property (or a virtual one).
			m_flid = flid;
			// Review JohnH(JohnT): This is only useful for dependent record lists, but I don't know how to check this is one.
			m_owningObject = null;
			_recordListProvidingRootObject = recordListProvidingRootObject;
		}

		private string DependentPropertyName => RecordListSelectedObjectPropertyId(_recordListProvidingRootObject.Id);

		#region Overrides of RecordList
		protected override bool TryListProvidingRootObject(out IRecordList recordListProvidingRootObject)
		{
			recordListProvidingRootObject = _recordListProvidingRootObject;
			return true;
		}

		protected override void UpdateOwningObject(bool fUpdateOwningObjectOnlyIfChanged = false)
		{
			var old = OwningObject;
			ICmObject newObj = null;
			var rni = PropertyTable.GetValue<RecordNavigationInfo>(DependentPropertyName);
			if (rni != null)
			{
				newObj = rni.MyRecordList.CurrentObject;
			}
			using (var luh = new ListUpdateHelper(new ListUpdateHelperParameterObject { MyRecordList = this }))
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
				Publisher.Publish("RecordListOwningObjChanged", this);
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

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
			}
			_recordListProvidingRootObject = null;

			base.Dispose(disposing);
		}

		protected override bool IsPrimaryRecordList => false;

		#endregion
	}
}