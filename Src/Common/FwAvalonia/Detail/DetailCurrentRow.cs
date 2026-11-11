// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.Detail
{
	/// <summary>
	/// The row of the shown record that last had keyboard focus, which commands from outside
	/// the detail view act on. It stays current while focus is elsewhere and across re-shows of
	/// the record, which replace every row.
	/// </summary>
	public sealed class DetailCurrentRow
	{
		private string _rowAutomationId;
		private int _objectHvo;

		/// <summary>Makes <paramref name="row"/> the current row; null forgets it.</summary>
		public void Remember(DetailField row)
		{
			_rowAutomationId = row?.RowAutomationId;
			_objectHvo = row?.ObjectHvo ?? 0;
		}

		/// <summary>Forgets the current row.</summary>
		public void Forget() => Remember(null);

		/// <summary>
		/// The current row as <paramref name="model"/> shows it: the row with the same
		/// automation id, else the first row of the same object. Null when neither is in the
		/// model, no row is current, or the model is null.
		/// </summary>
		public DetailField ResolveIn(DetailModel model)
		{
			if (model == null || _rowAutomationId == null)
				return null;
			return model.Fields.FirstOrDefault(f => f.RowAutomationId == _rowAutomationId)
				?? (_objectHvo == 0 ? null : model.Fields.FirstOrDefault(f => f.ObjectHvo == _objectHvo));
		}
	}
}
