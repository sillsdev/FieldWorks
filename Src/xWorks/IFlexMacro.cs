using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Implement this interface to create a Macro that can be run from anywhere in Flex (that a View selection can be made).
	/// Drop the Assembly that implements it into the FieldWorks root directory.
	/// The implementation assembly must have a file name starting "Macro". (This is mainly to reduce FLEx startup time
	/// by not making it search every assembly for implementations of IFlexMacro.)
	/// Your implementing class must have a no-argument constructor (or none). One instance will be created for each main window.
	/// You will need to reference at least xWorks and very likely FDO and COMInterfaces.
	/// </summary>
	public interface IFlexMacro
	{
		/// <summary>
		/// The User-visible name that should appear (under Tools) to invoke this command.
		/// It is also used to create a description for Undo/Redo.
		/// </summary>
		string CommandName { get; }

		/// <summary>
		/// Return true if the command should be enabled. You may assume that target is non-null and the selection
		/// is entirely contained in one field of target. (Otherwise the macro will automatically be disabled.)
		/// It is fine to always return true, then display a message (in RunMacro, NOT here) if there is some non-obvious
		/// reason why thecommand cannot be executed. In fact, it is generally preferable to do this unless the reason is
		/// something obvious, like the wrong kind of object selected or an empty selection.
		/// Arguments are the same as for RunMacro.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="targetField"></param>
		/// <param name="wsId"></param>
		/// <param name="start"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		bool Enabled(ICmObject target, int targetField, int wsId, int start, int length);

		/// <summary>
		/// Do the work. Target is the most local selected object. The selection is in a field of that
		/// object indicated by targetField. If it is a multilingual field, the selection is in the
		/// alternative indicated by wsId; otherwise, that is zero. The start and length of the range
		/// of characters selected in that property are indicated by those arguments.
		/// A Unit of Work is open (and will be closed when the method returns).
		/// Hint: you can get access to a lot of things from target.Cache and target.Services.
		/// In particular target.Services.GetInstance (with a type parameter) will get you things
		/// like ILexEntryRepository and ILexEntryFactory which can be used to find and create objects.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="targetField"> </param>
		/// <param name="wsId"> </param>
		/// <param name="start"> </param>
		/// <param name="length"> </param>
		void RunMacro(ICmObject target, int targetField, int wsId, int startOffset, int length);

		/// <summary>
		/// The function key to which you would prefer to have this macro assigned. If multiple macros
		/// are installed which all want the same one, FLEx will make an arbitrary choice as to which of
		/// them gets the key it wants, and assign the others to unused keys. If there are too many altogether,
		/// some will be unavailable.
		/// </summary>
		Keys PreferredFunctionKey { get; }
	}
}
