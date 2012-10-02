/*
 *    IWorldPadAppModel.cs
 *
 *    <purpose>
 *
 *    Andrew Weaver - 2008-05-01
 *
 *    $Id$
 */

namespace SIL.FieldWorks.WorldPad
{
	public interface IAppModelChangedEventArgs
	{ }

	public delegate void AppModelChangedEventHandler(object model,
		IAppModelChangedEventArgs modelInformation);

	public interface IWorldPadAppModel
	{
		event AppModelChangedEventHandler ModelChanged;

		void ActionPerformed();

		IWorldPadDocModel AddDoc();

		IWorldPadDocModel AddDoc(string text);

		void Init();

		void Subscribe(AppModelChangedEventHandler handler);
	}
}
