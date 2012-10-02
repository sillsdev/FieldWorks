// SelectionState.cs created with MonoDevelop
// User: hindlet at 9:40 A 22/09/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;

using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.WorldPad
{

	/// <summary>
	/// Represents a IVwSelection object state at a given point in time.
	/// This class can be used to Cache a Selection for latter reapplying, using the Apply method.
	/// </summary>
	public class SelectionState : IDisposable

	{
		// The following member variables represent a Selection state.
		// They are essentialy the parameters of IVwSelection::AllTextSelInfo

		protected int hvoRoot;
		protected int cLevels;
		protected SelLevInfo[] sliArray;
		protected int textProp;
		protected int propPrev;
		protected int anchor;
		protected int end;
		protected int ws;
		protected bool assocPrev;
		protected int hvoEnd;


		/// <summary>
		/// Create a Cache of a IVwSelection object
		/// </summary>
		/// <param name="vwSel">
		/// A COM Interface IVwSelection object.
		/// </param>
		public SelectionState(IVwSelection vwSel)
		{
			cLevels = vwSel.CLevels(false);
			cLevels--; // CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
			sliArray = new SelLevInfo[cLevels];

			ITsTextProps temp; // we don't store this value
			ArrayPtr ptr = MarshalEx.ArrayToNative(sliArray);
			vwSel.AllTextSelInfo(out hvoRoot, cLevels, ptr, out textProp, out propPrev, out anchor, out end, out ws, out assocPrev, out hvoEnd, out temp);
			sliArray = (SelLevInfo[]) MarshalEx.NativeToArray(ptr, cLevels, typeof(VwSelLevInfo));
		}

		/// <summary>
		/// Apply this SelectionState back to a rootBox object.
		/// returns the new Selection
		/// </summary>
		/// <param name="vwSel">
		/// A COM IVwRootBox interface object.
		/// </param>
		public IVwSelection Apply(IVwRootBox rootBox)
		{
			if (rootBox == null)
				return null;

			return (rootBox.MakeTextSelection(hvoRoot, cLevels, sliArray, textProp, propPrev, anchor, end, ws, assocPrev, hvoEnd, null, true));
		}

		public void Dispose()
		{
			sliArray = null;
			GC.SuppressFinalize(this);
		}



	}
}
