// MySpinButton.cs
// User: Jean-Marc Giffin at 3:38 P 26/05/2008

using System;

namespace SIL.FieldWorks.GtkCustomWidget
{
	public class MySpinButton : Gtk.SpinButton
	{
		public MySpinButton(System.IntPtr ptr) : base(ptr)
		{
		}

		public MySpinButton(Gtk.Adjustment adj, double dbl, uint ui) : base(adj, dbl, ui)
		{
		}

		public MySpinButton(double dbl1, double dbl2, double dbl3) : base(dbl1, dbl2, dbl3)
		{
		}
	}
}
