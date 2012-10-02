/*
 *    IWorldPadAppController.cs
 *
 *    <purpose>
 *
 *    Andrew Weaver - 2008-05-01
 *
 *    $Id$
 */

namespace SIL.FieldWorks.WorldPad
{
	public interface IWorldPadAppController
	{
		void Init();
		void FileNew();
		void FileOpen(string filename);
		void Quit();
		//void FileClose();
		//void FileClose(object docController);
		void FileClose(IWorldPadDocController docController);
	}
}
