// Copyright (c) 2010-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Areas.Notebook
{
	/// <summary>
	/// This struct stores the data associated with a single Standard Format Marker.
	/// </summary>
	internal sealed class RnSfMarker
	{
		//:> Data loaded from the settings file.
		internal string m_sMkr;         // The field marker (without the leading \).
		internal int m_flid;            // Field identifier for destination in FieldWorks database.
		// If zero, then this field is discarded on import.
		internal string m_sName;        // field name for display (read from resources)
		internal string m_sMkrOverThis; // Field marker of parent field, if any.

		// If record specifier, level of the record in the hierarchy (1 = root, 0 = not a record
		// specifier).
		internal int m_nLevel;
		internal TextOptions m_txo = new TextOptions();
		internal TopicsListOptions m_tlo = new TopicsListOptions();
		internal DateOptions m_dto = new DateOptions();
		internal StringOptions m_sto = new StringOptions();
	}
}