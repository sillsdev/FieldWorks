/*
 *    IWorldPadDocView.cs
 *
 *    <purpose>
 *
 *    Andrew Weaver - 2008-05-01
 *
 *    $Id$
 */

using Gtk;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.WorldPad
{
	public interface IWorldPadDocView : IWindow
	{
		void SplitPane();

		void ClosePane();

		/// <summary>Load a file into fwview.</summary>
		/// <param name="path">input file</param>
		void LoadFromXml(string path);

//		void on_modelinfochange_event(object obj, ModelInfoEventArgs args);

		void Init();
	}
}
