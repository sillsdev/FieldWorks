// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Marker interface for objects that scope Pub/Sub delivery.
	///
	/// The Publisher and Subscriber are process-wide singletons, but most messages are
	/// window-scoped: the legacy Mediator they replace delivered only within one main window.
	/// To reproduce that, a subscriber passes its main window when subscribing, and a publisher
	/// passes the same window in its PublisherParameterObject; the Publisher then delivers a
	/// scoped publish only to subscribers with the identical (reference-equal) scope.
	/// A Publisher with a null scope will go to all the message subscribers and a Subscriber
	/// with a null scope will receive all the messages, regardless of them being published
	/// with or without a scope.
	/// Null is for publishers with no window context (e.g. app bootstrap code) and for
	/// messages that must reach a window the publisher cannot name (e.g. Send/Receive messages
	/// aimed at the project's reopened instance). (Process-wide delivery is a Pub/Sub-era
	/// capability, not preserved Mediator behavior. The Mediator had no all-windows broadcast;
	/// instead its app-wide coordination was accomplished by iterating FwApp.MainWindows directly.)
	///
	/// The scope IS the main window: the XCore IxWindow interface extends this one, so main
	/// windows pass <c>this</c> and everything else obtains the window via
	/// <c>PropertyTable.GetWindow()</c>.
	/// Do not implement this interface on anything that is not a main window.
	/// </summary>
	public interface IPubSubScope
	{
	}
}
