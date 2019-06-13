// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Windows.Forms;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;

namespace SIL.FieldWorks.Common.FwUtils
{
	public static class ControlExtensions
	{
		/// <summary>
		/// Find a control
		/// </summary>
		/// <typeparam name="T">A Control instance, or a subclass of Control.</typeparam>
		/// <param name="me">The control that we want to get the parent from.</param>
		/// <returns>The parent of the given class, or null, if there is none.</returns>
		public static T ParentOfType<T>(this Control me) where T : Control
		{
			while (true)
			{
				if (me?.Parent == null)
				{
					// 'me' is null, or Parent of 'me' is null.
					return null;
				}
				var myParent = me.Parent;
				if (myParent is T)
				{
					return (T)myParent;
				}
				me = myParent;
			}
		}

		/// <summary>
		/// Add writing systems to combo box.
		/// </summary>
		public static bool InitializeWritingSystemCombo(this ComboBox me, LcmCache cache, string writingSystem = null, CoreWritingSystemDefinition[] writingSystems = null)
		{
			if (string.IsNullOrEmpty(writingSystem))
			{
				writingSystem = cache.WritingSystemFactory.GetStrFromWs(cache.DefaultAnalWs);
			}
			if (writingSystems == null)
			{
				writingSystems = cache.ServiceLocator.WritingSystems.AllWritingSystems.ToArray();
			}
			me.Items.Clear();
			me.Sorted = true;
			me.Items.AddRange(writingSystems);
			foreach (CoreWritingSystemDefinition ws in me.Items)
			{
				if (ws.Id == writingSystem)
				{
					me.SelectedItem = ws;
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Reset old Text property with <paramref name="newText"/>.
		/// </summary>
		public static void ResetTextIfDifferent(this ToolStripMenuItem me, string newText)
		{
			if (me.Text != newText)
			{
				me.Text = newText;
			}
		}
	}
}