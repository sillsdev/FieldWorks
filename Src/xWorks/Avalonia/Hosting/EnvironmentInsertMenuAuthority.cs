// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.Reporting;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The native authority for the label menus that carry only the five environment inserts:
	/// the allomorph Environments row's, and the environment String Representation row's. The
	/// inserts type into the row's current text editor, so a row without one shows them
	/// disabled.
	/// </summary>
	internal sealed class EnvironmentInsertMenuAuthority : IDetailMenuAuthority
	{
		internal const string EnvironmentsMenuId = "mnuDataTree-Environments-Insert";
		internal const string StringRepresentationMenuId = "mnuDataTree-StringRepresentation-Insert";

		private readonly DetailMenuRequest _request;
		private readonly IEnvironmentMenuHost _host;
		private bool _unsupportedRowLogged;

		/// <summary>Creates the authority for one label-menu request.</summary>
		/// <param name="request">The request; its editor snapshot decides enablement.</param>
		/// <param name="host">Supplies the natural-class chooser.</param>
		public EnvironmentInsertMenuAuthority(DetailMenuRequest request, IEnvironmentMenuHost host)
		{
			_request = request ?? throw new ArgumentNullException(nameof(request));
			_host = host ?? throw new ArgumentNullException(nameof(host));
		}

		public bool Owns(string menuId)
			=> string.Equals(menuId, EnvironmentsMenuId, StringComparison.Ordinal)
				|| string.Equals(menuId, StringRepresentationMenuId, StringComparison.Ordinal);

		public DetailMenuItem Build(string menuId, ChoiceBase leaf)
		{
			if (leaf == null)
				throw new ArgumentNullException(nameof(leaf));
			if (!(leaf is CommandChoice command) || !EnvironmentMenuLeaves.IsInsertMessage(command.Message))
			{
				throw new InvalidOperationException(string.Format(
					"Menu '{0}' has a leaf '{1}' this authority does not answer.", menuId, leaf.HelpId));
			}
			LogUnsupportedRowOnce();
			return EnvironmentMenuLeaves.BuildInsert(command.Message, XCoreMenuBridge.StripAccelerator(leaf.Label),
				_request, _host);
		}

		// The String Representation row has no Avalonia editor yet, so its inserts cannot act;
		// said once per menu rather than per leaf.
		private void LogUnsupportedRowOnce()
		{
			if (_unsupportedRowLogged || _request.Field?.Kind != DetailFieldKind.Unsupported)
				return;
			_unsupportedRowLogged = true;
			Logger.WriteEvent(string.Format(
				"Environment inserts disabled: row '{0}' has no Avalonia text editor.", _request.Field.Field));
		}
	}
}
