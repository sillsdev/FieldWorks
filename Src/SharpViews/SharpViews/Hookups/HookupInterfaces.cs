using System;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews.Hookups
{
	/// <summary>
	/// This is the common interface of everything that is (or wants to look like) a hookup.
	/// In particular it serves as the signature for the list of child hookups maintained by a GroupHookup.
	/// </summary>
	public interface IHookup
	{
		object Target { get; }
		GroupHookup ParentHookup { get; }
		InsertionPoint SelectAtEnd();
		InsertionPoint SelectAtStart();
	}
	internal interface IHookupInternal
	{
		void SetParentHookup(GroupHookup parent);
	}

	/// <summary>
	/// An interface implemented by hookups (and LazyBox) that represent one or more items that are
	/// children of some kind of SequenceHookup
	/// </summary>
	internal interface IItemsHookup
	{
		Box FirstBox { get; }
		Box LastBox { get; }
		IHookup LastChild { get; }
		// The items that are the part of the sequence which this hookup covers.
		object[] ItemGroup { get; }
	}
	/// <summary>
	/// This class (more specifically, its subclasses) adapt from one particular property of some class to
	/// what some kind of Hookup needs to monitor it.
	/// </summary>
	public class HookupAdapter
	{
		protected HookupAdapter(object target)
		{
			Target = target;
		}
		/// <summary>
		/// The object to which we are hooking
		/// </summary>
		public object Target { get; private set;}
	}

	/// <summary>
	/// Indicates that a particlar alternative of a multilingual property changed.
	/// This will have to move to FDO along with IViewMultiString.
	/// </summary>
	public class MlsChangedEventArgs : EventArgs
	{
		public int WsId { get; private set; }
		public MlsChangedEventArgs(int ws)
		{
			WsId = ws;
		}
	}

	/// <summary>
	/// The information a TssHookup needs in order to hook to a particular string property of a particular object.
	/// This is intended to support one style of view construction with SharpViews.
	/// The idea is that, given a class like StTxtPara and a property like Contents, we could generate a helper class StTxtParaProps,
	/// with static methods like this:
	///
	/// class StTxtParaProps
	/// {
	///     public static TssHookupAdapter Contents(StTxtPara target)
	///     {
	///         return new TssHookupAdapter(target, () => target.Contents, hookup => target.Contents += hookup.TssPropChanged,
	///             hookup => target.Contents -= hookup.TssPropChanged);
	///     }
	/// }
	///
	/// This then allows us to do something (as yet not fully designed or implemented) like
	/// aView.Add(StTxtParaProps.Contents(aPara));
	///
	/// Problems:
	/// (1) the above doesn't look particularly fluent;
	/// (2) we have a dependency problem between SharpViews (which defines TssHookupAdapter) and FDO (where we'd like to keep the
	/// model-specific code generation). SharpViews already references FDO, and it's unlikely we can change that.
	///
	/// The approach used in ExpressionDemo may be more promising.
	/// </summary>
	public class TssHookupAdapter : HookupAdapter
	{
		public TssHookupAdapter(object target, Func<ITsString> reader, Action<TssHookup> addHook, Action<TssHookup> removeHook)
			: base(target)
		{
			Reader = reader;
			AddHook = addHook;
			RemoveHook = removeHook;
		}
		public Func<ITsString> Reader { get; private set; }
		public Action<TssHookup> AddHook { get; private set; }
		public Action<TssHookup> RemoveHook { get; private set; }
	}

	public class MlsHookupAdapter : HookupAdapter
	{
		public MlsHookupAdapter(object target, Action<MlsHookup> addHook, Action<MlsHookup> removeHook)
			: base(target)
		{
			AddHook = addHook;
			RemoveHook = removeHook;
		}
		public Action<MlsHookup> AddHook { get; private set; }
		public Action<MlsHookup> RemoveHook { get; private set; }
	}

	/// <summary>
	/// A function called by a TssHookup or an MlsHookup when the relevant property changes.
	/// </summary>
	/// <param name="hookup"></param>
	/// <param name="newValue"></param>
	public delegate void StringChangeHandler(Hookup hookup, ITsString newValue);

	/// <summary>
	/// An interface that will eventually have to move to FDO so that the various kinds of MultiStringAccessor can implement it.
	/// </summary>
	public interface IViewMultiString
	{
		/// <summary>
		/// Get the string for a particular WS. If no such alternative is known, must answer an empty string in the correct WS.
		/// Must not answer null.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		ITsString get_String(int ws);
		/// <summary>
		/// Set the specified alternative.
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="_tss"></param>
		void set_String(int ws, ITsString _tss);

		void SetAnalysisDefaultWritingSystem(string val);
		void SetVernacularDefaultWritingSystem(string val);

		ITsString AnalysisDefaultWritingSystem { get; set; }
		ITsString VernacularDefaultWritingSystem { get; set; }

		event EventHandler<MlsChangedEventArgs> StringChanged;
	}

	/// <summary>
	/// Interface implemented by various hookups to receive notifications when a property changes.
	/// </summary>
	public interface IReceivePropChanged
	{
		void PropChanged(object sender, EventArgs args);
		void PropChanged(object sender, ObjectSequenceEventArgs args);
	}

	public interface HookupInterfaces
	{

	}
}