// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// A record clerk over natural classes that keeps the "shared" prefix on generated natural
	/// class names current, so whether a prefix exists matches whether more than one rule uses
	/// the class.
	/// </summary>
	public class NaturalClassRecordClerk : RecordClerk
	{
		/// <summary>
		/// Brings the "shared" prefix on every generated natural class name up to date
		/// before the list is shown. A prefix change means the class may have joined or
		/// left the list to display, so the list is reloaded when any prefix changes.
		/// </summary>
		public override void ActivateUI(bool useRecordTreeBar, bool updateStatusBar = true)
		{
			bool changed = false;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => changed = RuleFormulaControl.SyncSharedPrefixes(Cache));
			// A class whose name changed may have gained or lost its place in the list.
			if (changed)
				m_list.ReloadList();

			base.ActivateUI(useRecordTreeBar, updateStatusBar);
		}
	}
}
