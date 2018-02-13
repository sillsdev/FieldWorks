// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// Summary description for ViewPropertyItem.
	/// </summary>
	internal class StringSlice : ViewPropertySlice
	{
		internal StringSlice(ICmObject obj, int flid)
			: base(new StringSliceView(obj.Hvo, flid, -1), obj, flid)
		{
		}

		internal StringSlice(ICmObject obj, int flid, int ws)
			: base(new StringSliceView(obj.Hvo, flid, ws), obj, flid)
		{
			WritingSystemId = ws;
		}

		/// <summary>
		/// This constructor is mainly intended for subclasses in other DLLs created using the 'custom' element.
		/// Such subclasses must set the ContextObject, the FieldId, and if relevant the Ws, and then call
		/// CreateView(), typically from an override of FinishInit().
		/// </summary>
		internal StringSlice()
		{
		}

		/// <summary>
		/// See comments on no-arg constructor. Call only if using that constructor.
		/// </summary>
		public void CreateView()
		{
			var ssv = new StringSliceView(Object.Hvo, FieldId, WritingSystemId)
			{
				Cache = Cache
			};
			Control = ssv;
		}

		/// <summary>
		/// Get/set the writing system ID. If -1, signifies a non-multilingual property.
		/// </summary>
		public int WritingSystemId { get; set; } = -1;

		bool m_fShowWsLabel;
		/// <summary>
		/// Get/set flag whether to display writing system label even for monolingual string.
		/// </summary>
		public bool ShowWsLabel
		{
			get
			{
				return m_fShowWsLabel;
			}
			set
			{
				m_fShowWsLabel = value;
				((StringSliceView)Control).ShowWsLabel = value;
			}
		}

		int m_wsDefault;
		/// <summary>
		/// Get/set the default writing system associated with this string.
		/// </summary>
		public int DefaultWs
		{
			get
			{
				return m_wsDefault;
			}
			set
			{
				m_wsDefault = value;
				((StringSliceView)Control).DefaultWs = value;
			}
		}

		/// <summary>
		/// Make a selection at the specified character offset.
		/// </summary>
		public void SelectAt(int ich)
		{
			((StringSliceView)Control).SelectAt(ich);
		}
	}
}
