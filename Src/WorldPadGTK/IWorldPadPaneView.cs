/*
 *    IWorldPadPaneView.cs
 *
 *    <purpose>
 *
 *    Andrew Weaver - 2008-05-01
 *
 *    $Id$
 */

namespace SIL.FieldWorks.WorldPad
{
	public interface IWorldPadView
	{ }

	public interface IWorldPadPaneView
	{
		IWorldPadView /*WorldPad*/View {get; set;}
		/*Gtk.ScrolledWindow ScrolledWindow1 {get; set;}

		Gtk.TextView TextView1 {get; set;}*/

		void Init();
	}
}
