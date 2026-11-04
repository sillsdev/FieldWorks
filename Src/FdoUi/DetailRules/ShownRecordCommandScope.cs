// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// Where a command that acts on the shown record itself applies (Insert Variant from the
	/// Insert menu, the Lexeme Form conversions): to a record of the command's class, in the
	/// one tool the command may be restricted to.
	/// </summary>
	public static class ShownRecordCommandScope
	{
		/// <summary>
		/// Whether a command whose parameters name <paramref name="className"/> and
		/// <paramref name="restrictToTool"/> applies to <paramref name="root"/> in
		/// <paramref name="currentTool"/>.
		/// </summary>
		/// <param name="restrictToTool">The only tool the command applies in; null for
		/// any.</param>
		public static bool Applies(ICmObject root, string className, string restrictToTool, string currentTool)
		{
			if (root == null || !root.IsValidObject || className != root.ClassName)
				return false;
			return restrictToTool == null || restrictToTool == currentTool;
		}
	}
}
