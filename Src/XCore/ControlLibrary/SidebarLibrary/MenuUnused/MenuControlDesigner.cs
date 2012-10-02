
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace SidebarLibrary.Menus
{
	public class MenuControlDesigner :  System.Windows.Forms.Design.ParentControlDesigner
	{
		public override ICollection AssociatedComponents
		{
			get
			{
				if (base.Control is SidebarLibrary.Menus.MenuControl)
					return ((SidebarLibrary.Menus.MenuControl)base.Control).MenuCommands;
				else
					return base.AssociatedComponents;
			}
		}

		protected override bool DrawGrid
		{
			get { return false; }
		}
	}
}
