// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.CoreImpl
{
#if RANDYTODO
	// TODO: I wonder if this interface is really needed now?
	// TODO: I expect there to be far less need for a control to know about its area name now,
	// TODO: and PrepareToGoAway sounds like a poor man's version of
	// TODO: what the new Deactivate method does on IMajorFlexComponent
#endif
	/// <summary>
	/// All Controls that can be used as main controls in an IFwMainWnd,
	/// must implement the IMainContentControl interface.
	/// It 'extends' other required interfaces, just to keep them all in one bundle.
	/// </summary>
	public interface IMainContentControl : IFlexComponent, IMainUserControl, ICtrlTabProvider
	{
		/// <summary>
		/// The control is about to go away, so do something first.
		/// </summary>
		bool PrepareToGoAway();

		/// <summary>
		/// The Area name that uses this control.
		/// </summary>
		string AreaName { get;}
	}
}