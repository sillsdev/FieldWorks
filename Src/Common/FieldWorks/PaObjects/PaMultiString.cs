using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.FDO;
using SIL.PaToFdoInterfaces;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.PaObjects
{
	/// ----------------------------------------------------------------------------------------
	public class PaMultiString : IPaMultiString
	{
		/// ------------------------------------------------------------------------------------
		public static PaMultiString Create(ITsMultiString msa, IFdoServiceLocator svcloc)
		{
			return (msa == null || msa.StringCount == 0 ? null : new PaMultiString(msa, svcloc));
		}

		/// ------------------------------------------------------------------------------------
		public List<string> Texts { get; set; }

		/// ------------------------------------------------------------------------------------
		public List<string> WsIds { get; set; }

		/// ------------------------------------------------------------------------------------
		public PaMultiString()
		{
		}

		/// ------------------------------------------------------------------------------------
		private PaMultiString(ITsMultiString msa, IFdoServiceLocator svcloc)
		{
			Texts = new List<string>(msa.StringCount);
			WsIds = new List<string>(msa.StringCount);

			for (int i = 0; i < msa.StringCount; i++)
			{
				int hvoWs;
				ITsString tss = msa.GetStringFromIndex(i, out hvoWs);
				Texts.Add(tss.Text);

				// hvoWs should *always* be found in AllWritingSystems.
				var ws = svcloc.WritingSystems.AllWritingSystems.SingleOrDefault(w => w.Handle == hvoWs);
				WsIds.Add(ws == null ? null : ws.ID);
			}
		}

		#region IPaMultiString Members
		/// ------------------------------------------------------------------------------------
		public string GetString(string wsId)
		{
			if (string.IsNullOrEmpty(wsId))
				return null;

			int i = WsIds.IndexOf(wsId);
			return (i < 0 ? null : Texts[i]);
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return Texts[0];
		}
	}
}
