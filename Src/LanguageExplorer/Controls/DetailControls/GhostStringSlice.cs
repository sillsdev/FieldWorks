// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// A ghost string slice displays what purports to be a string (or multistring) property of a missing object.
	/// The canonical example is if a LexSense does not have an example sentence. The ghost slice apparently shows
	/// the missing Example property of the nonexistent ExampleSentence. If the user types something, a real object
	/// is created.
	///
	/// A ghost slice is created when a part displays an object or object sequence that is empty, if the 'obj' or 'seq'
	/// element has an attribute ghost="fieldname" and ghostWs="vernacular/analysis".
	/// Optionally it may also have ghostClass="className". If this is absent it will create an instance of the
	/// base signature class, which had better not be abstract.
	/// </summary>
	internal class GhostStringSlice : ViewPropertySlice
	{
		internal const int kflidFake = -2001;
		internal const int khvoFake = -2002;

		/// <summary>
		/// Create a ghost string slice that pretends to be property flid of the missing object
		/// </summary>
		internal GhostStringSlice(ICmObject obj, int flid, XElement nodeObjProp, LcmCache cache)
			: base(new GhostStringSliceView(obj.Hvo, flid, nodeObjProp, cache), obj, flid)
		{
			AccessibleName = "GhostStringSlice";
		}

		public override bool IsGhostSlice => true;
	}
}