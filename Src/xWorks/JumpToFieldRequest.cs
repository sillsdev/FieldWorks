// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// What <c>EventConstants.JumpToField</c> carries: the piece of the dictionary preview
	/// whose field the detail view should show and focus.
	/// </summary>
	internal sealed class JumpToFieldRequest
	{
		/// <param name="recordHvo">The record the field belongs to when that is not the
		/// previewed one; 0 for the previewed record.</param>
		/// <param name="fieldObjectHvo">The object the piece was generated from.</param>
		/// <param name="fieldName">The preview's name for that object's field.</param>
		/// <param name="fieldValue">The text the piece shows.</param>
		public JumpToFieldRequest(int recordHvo, int fieldObjectHvo, string fieldName, string fieldValue)
		{
			RecordHvo = recordHvo;
			FieldObjectHvo = fieldObjectHvo;
			FieldName = fieldName;
			FieldValue = fieldValue;
		}

		public int RecordHvo { get; }

		public int FieldObjectHvo { get; }

		public string FieldName { get; }

		public string FieldValue { get; }
	}
}
