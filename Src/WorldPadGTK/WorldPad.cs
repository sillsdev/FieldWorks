/*
 *    WorldPad.cs
 *
 *    <purpose>
 *
 *    Andrew Weaver - 2008-05-01
 *
 *    $Id$
 */

using Glade;
using Gtk;
using System;

namespace SIL.FieldWorks.WorldPad
{
	class WorldPad
	{
		static void Main(string[] args)
		{
			IWorldPadAppModel appModel = (IWorldPadAppModel) new WorldPadAppModel();
			IWorldPadAppController appController
				= (IWorldPadAppController) new WorldPadAppController(appModel, args);
		}
	}
}
