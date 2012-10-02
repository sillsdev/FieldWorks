using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FdoUi;

namespace CSharpLinker
{
	[Guid("43352DC3-027F-4060-8D66-44CFDDCE05AA")]
	public interface IDictionaryFinder
	{
		void FindInDictionary(IOleDbEncap ode, IFwMetaDataCache mdc, IVwOleDbDa oleDbAccess, IVwSelection sel);
	}

	/// <summary>
	/// The dictionary finder exists to wrap the functionality of LexEntryUi.FindEntryForWordform
	/// in a COM interface that the Notebook can call.
	/// </summary>
	[GuidAttribute("DDA50035-341C-4f46-9C38-2E079516F8FC")]
	[ClassInterface(ClassInterfaceType.None)]
	[ProgId("SIL.FieldWorks.DictionaryFinder")]
	public class DictionaryFinder : IDictionaryFinder
	{
		public DictionaryFinder()
		{
		}

		public void FindInDictionary(IOleDbEncap ode, IFwMetaDataCache mdc, IVwOleDbDa oleDbAccess, IVwSelection sel)
		{
			using (FdoCache cache = new FdoCache(ode, mdc, oleDbAccess))
			{
				if (sel == null)
					return;
				IVwSelection sel2 = sel.EndPoint(false);
				if (sel2 == null)
					return;
				IVwSelection sel3 = sel2.GrowToWord();
				if (sel3 == null)
					return;
				ITsString tss;
				int ichMin, ichLim, hvo, tag, ws;
				bool fAssocPrev;
				sel3.TextSelInfo(false, out tss, out ichMin, out fAssocPrev, out hvo, out tag, out ws);
				sel3.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvo, out tag, out ws);
				// TODO (TimS): need to supply help information (last 2 params)
				LexEntryUi.DisplayOrCreateEntry(cache, hvo, tag, ws, ichMin, ichLim, null, null, null, string.Empty);
				return;
			}
		}
	}
}
