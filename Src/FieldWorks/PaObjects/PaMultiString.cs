// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.PaToFdoInterfaces;

namespace SIL.FieldWorks.PaObjects
{
	/// <summary />
	public class PaMultiString : IPaMultiString
	{
		/// <summary />
		public static PaMultiString Create(ITsMultiString msa, ILcmServiceLocator svcloc)
		{
			return (msa == null || msa.StringCount == 0 ? null : new PaMultiString(msa, svcloc));
		}

		/// <summary>
		/// Append to the PaMultiString the contents of the tsMultiString.
		/// For each writing system the contents will end up in a comma separated list
		/// </summary>
		public static void Append(PaMultiString paMultiString, ITsMultiString tsMultiString, ILcmServiceLocator svcloc)
		{
			for (var i = 0; i < tsMultiString.StringCount; ++i)
			{
				int hvoWs;
				var tss = tsMultiString.GetStringFromIndex(i, out hvoWs);

				// hvoWs should *always* be found in AllWritingSystems.
				var ws = svcloc.WritingSystems.AllWritingSystems.SingleOrDefault(w => w.Handle == hvoWs);
				paMultiString.AddString(ws?.Id, tss.Text);
			}
		}

		/// <summary />
		public List<string> Texts { get; set; }

		/// <summary />
		public List<string> WsIds { get; set; }

		/// <summary />
		public PaMultiString()
		{
		}

		/// <summary />
		private PaMultiString(ITsMultiString msa, ILcmServiceLocator svcloc)
		{
			Texts = new List<string>(msa.StringCount);
			WsIds = new List<string>(msa.StringCount);

			for (var i = 0; i < msa.StringCount; i++)
			{
				int hvoWs;
				var tss = msa.GetStringFromIndex(i, out hvoWs);
				Texts.Add(tss.Text);

				// hvoWs should *always* be found in AllWritingSystems.
				var ws = svcloc.WritingSystems.AllWritingSystems.SingleOrDefault(w => w.Handle == hvoWs);
				WsIds.Add(ws?.Id);
			}
		}

		private void AddString(string ws, string text)
		{
			if (Texts == null)
			{
				Texts = new List<string>();
				WsIds = new List<string>();
			}
			var index = WsIds.IndexOf(ws);
			if (index >= 0)
			{
				Texts[index] = Texts[index] + $", {text}";
			}
			else
			{
				Texts.Add(text);
				WsIds.Add(ws);
			}
		}

		#region IPaMultiString Members

		/// <inheritdoc />
		public string GetString(string wsId)
		{
			if (string.IsNullOrEmpty(wsId))
			{
				return null;
			}
			var i = WsIds.IndexOf(wsId);
			return i < 0 ? null : Texts[i];
		}

		#endregion

		/// <inheritdoc />
		public override string ToString()
		{
			return Texts[0];
		}
	}
}
