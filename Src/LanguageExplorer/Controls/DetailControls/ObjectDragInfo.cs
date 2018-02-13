// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Runtime.Serialization;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary />
	[Serializable()]
	public class ObjectDragInfo : ISerializable
	{
		string m_label; // The label to display during dragging.

		/// <summary />
		public ObjectDragInfo(int hvoSrcOwner, int flidSrc, int ihvoSrcStart, int ihvoSrcEnd, string label)
		{
			HvoSrcOwner = hvoSrcOwner;
			FlidSrc = flidSrc;
			IhvoSrcStart = ihvoSrcStart;
			IhvoSrcEnd = ihvoSrcEnd;
			m_label = label;
		}

		/// <summary />
		public override string ToString()
		{
			return m_label;
		}

		/// <summary>Deserialization constructor.</summary>
		public ObjectDragInfo (SerializationInfo info, StreamingContext context)
		{
			HvoSrcOwner = (int)info.GetValue("SrcOwner", typeof(int));
			FlidSrc = (int)info.GetValue("FlidSrc", typeof(int));
			IhvoSrcStart = (int)info.GetValue("IhvoSrcStart", typeof(int));
			IhvoSrcEnd = (int)info.GetValue("IhvoSrcEnd", typeof(int));
			m_label = (string)info.GetValue("label", typeof(string));
		}

		/// <summary>Serialization function.</summary>
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("SrcOwner", HvoSrcOwner);
			info.AddValue("FlidSrc", FlidSrc);
			info.AddValue("IhvoSrcStart", IhvoSrcStart);
			info.AddValue("IhvoSrcEnd", IhvoSrcEnd);
			info.AddValue("label", m_label);
		}

		/// <summary />
		public int HvoSrcOwner { get; }

		/// <summary />
		public int FlidSrc { get; }

		/// <summary />
		public int IhvoSrcStart { get; }

		/// <summary />
		public int IhvoSrcEnd { get; }
	}
}