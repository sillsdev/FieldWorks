// // Copyright (c) -2020 SIL International
// // This software is licensed under the LGPL, version 2.1 or later
// // (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary/>
	internal interface ISliceFactory
	{
		/// <summary/>
		ISlice Create(LcmCache cache, string editor, int flid, XElement configurationElement, ICmObject obj, IPersistenceProvider persistenceProvider, FlexComponentParameters flexComponentParameters, XElement caller, ObjSeqHashMap reuseMap, ISharedEventHandlers sharedEventHandlers);

		/// <summary/>
		void MakeGhostSlice(DataTree dataTree, LcmCache cache, FlexComponentParameters flexComponentParameters, ArrayList path, XElement node, ObjSeqHashMap reuseMap, ICmObject obj, ISlice parentSlice, int flidEmptyProp, XElement caller, int indent, ref int insertPosition);

		/// <summary/>
		void SetNodeWeight(XElement node, ISlice slice);
		/// <summary>
		/// Look for a reusable slice that matches the current path. If found, remove from map and return;
		/// otherwise, return null.
		/// </summary>
		ISlice GetMatchingSlice(ArrayList path, ObjSeqHashMap reuseMap);
		/// <summary/>
		ISlice CreateDummyObject(int indent, XElement node, ArrayList path, ICmObject obj, int flid, int cnt, string layoutOverride, string layoutChoiceField, XElement caller);
	}
}