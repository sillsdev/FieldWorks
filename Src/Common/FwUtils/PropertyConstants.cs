// Copyright (c) 2020-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

// ReSharper disable InconsistentNaming _ underscores are included
namespace SIL.FieldWorks.Common.FwUtils
{
	public static class PropertyConstants
	{
		public const string ActiveClerk = "ActiveClerk";
		public const string ActiveClerkSelectedObject = "ActiveClerkSelectedObject";
		public const string AreaChoice = "areaChoice";
		public const string BestStyleName = "BestStyleName";
		/// <summary>
		/// Starts every Pub/Sub message that announces a new value of a property; the property
		/// name follows it. No message in <see cref="EventConstants"/> starts with it.
		/// </summary>
		public const string ChangedMessagePrefix = "PropertyChanged:";
		public const string CurrentContentControl = "currentContentControl";
		public const string CurrentContentControlObject = "currentContentControlObject";
		public const string CurrentContentControlParameters = "currentContentControlParameters";
		public const string CurrentContextMenuSelectedWsIds = "SelectedWritingSystemHvosForCurrentContextMenu";
		/// <summary>
		/// Starts the name of the property that holds a record clerk's choice in the Filters
		/// menu; the clerk's id follows it.
		/// </summary>
		public const string CurrentFilterForRecordClerkPrefix = "currentFilterForRecordClerk_";
		public const string ITexts_AddWordsToLexicon = "ITexts_AddWordsToLexicon";
		public const string SelectedPublication = "SelectedPublication";
		public const string ShowHiddenFields = "ShowHiddenFields";
		public const string ShowHiddenFieldsSpecificPrefix = "ShowHiddenFields-";
		public const string ShowRecordList = "ShowRecordList";
		public const string SortedByLength = "SortedByLength";
		public const string ToolForAreaNamed_lexicon = "ToolForAreaNamed_lexicon";
		public const string ToolForAreaPrefix = "ToolForAreaNamed_";
		public const string WritingSystemHvo = "WritingSystemHvo";

		/// <summary>
		/// The Pub/Sub message that announces a new value of the property
		/// <paramref name="propertyName"/>, with the new value as its data.
		/// </summary>
		public static string PropertyChanged(string propertyName)
		{
			return ChangedMessagePrefix + propertyName;
		}
	}
}
