// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// This class is used for the record list of the concordance view.
	/// We fudge the owning object, since the decorator doesn't care what class it is, but
	/// the base class does care that it is some kind of real object.
	/// </summary>
	internal class MatchingConcordanceItems : InterlinearTextsRecordList
	{
		ConcordanceControlBase _concordanceControl;

		/// <summary>
		/// Create bare-bones RecordList for made up owner and a property on it.
		/// </summary>
		public MatchingConcordanceItems(string id, StatusBar statusBar, ConcDecorator decorator)
			: base(id, statusBar, decorator, false, new VectorPropertyParameterObject(decorator.PropertyTable.GetValue<LcmCache>("cache").LanguageProject, "ConcOccurrences", decorator.MetaDataCache.GetFieldId2(LangProjectTags.kClassId, "ConcOccurrences", false)))
		{
		}

		#region Overrides of RecordList

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			m_owningObject = m_cache.LangProject;
		}

		/// <summary>
		/// Override to force recomputing the list. This is tricky because LoadMatches calls a record list routine which
		/// recursively calls ReloadList. Therefore if we call LoadMatches, we don't need to call the base routine.
		/// If we're in the middle of loading a list, though, we want to only do the base thing.
		/// Finally, if the OwningControl has never been loaded (user hasn't yet selected option), just load the (typically empty) list.
		/// </summary>
		protected override void ReloadList()
		{
			if (OwningControl != null && OwningControl.HasLoadedMatches)
			{
				if (OwningControl.IsLoadingMatches)
				{
					// calling from inside the call to LoadMatches, we've already rebuild the main list,
					// just need to do the rest of the normal reload.
					base.ReloadList();
					return;
				}
				OwningControl.LoadMatches();
				// Fall through to base impl.
			}
			else
			{
				// It's in a disposed state...make it empty for now.
				((ObjectListPublisher)VirtualListPublisher).SetOwningPropValue(new int[0]);
				GetConcDecorator()?.UpdateOccurrences(new int[0]);
			}
			base.ReloadList();
		}

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");

			if (disposing)
			{
			}

			_concordanceControl = null;

			base.Dispose(disposing);
		}

		protected override void RefreshAfterInvalidObject()
		{
			ConcordanceControl.LoadMatches(true);
		}

		/// <summary>
		/// Overridden to prevent trying to get a name for the "current object" which we can't do because
		/// it is not a true CmObject.
		/// </summary>
		protected override string GetStatusBarMsgForCurrentObject()
		{
			return string.Empty;
		}

		#endregion

		internal ConcordanceControlBase OwningControl { get; set; }

		private ConcDecorator GetConcDecorator()
		{
			return ((ObjectListPublisher)VirtualListPublisher).BaseSda as ConcDecorator;
		}

		internal ConcordanceControlBase ConcordanceControl
		{
			get { return _concordanceControl; }
			set
			{
				_concordanceControl = value;
				OwningControl = value;
			}
		}
	}
}