// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Filters;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainImpl;

namespace LanguageExplorer.Areas.TextsAndWords.Tools
{
	/// <summary />
	internal sealed class WordformGoDlg : BaseGoDlg
	{
		#region	Data members

		private int m_oldSearchWs;

		#endregion

		#region Construction, Initialization, and Disposal

		internal WordformGoDlg()
		{
			SetHelpTopic("khtpFindWordform");
			InitializeComponent();
		}

		/// <summary>
		/// Just load current vernacular
		/// </summary>
		protected override void LoadWritingSystemCombo()
		{
			foreach (var ws in m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems)
			{
				m_cbWritingSystems.Items.Add(ws);
			}
		}

		#endregion Construction, Initialization, and Disposal

		#region Other methods

		protected override void InitializeMatchingObjects()
		{
			m_matchingObjectsBrowser.Initialize(m_cache, FwUtils.StyleSheetFromPropertyTable(PropertyTable), XElement.Parse(TextAndWordsResources.SimpleWordListParameter),
				SearchEngine.Get(PropertyTable, "WordformGoSearchEngine", () => new WordformGoSearchEngine(m_cache)));
			// start building index
			var wsObj = (CoreWritingSystemDefinition)m_cbWritingSystems.SelectedItem;
			if (wsObj == null)
			{
				return;
			}
			m_matchingObjectsBrowser.SearchAsync(new[] { new SearchField(WfiWordformTags.kflidForm, TsStringUtils.EmptyString(wsObj.Handle)) });
		}

		/// <summary>
		/// Reset the list of matching items.
		/// </summary>
		protected override void ResetMatches(string searchKey)
		{
			var wsObj = (CoreWritingSystemDefinition)m_cbWritingSystems.SelectedItem;
			var wsSelHvo = wsObj?.Handle ?? 0;
			if (!GetSearchKey(wsSelHvo, searchKey, out var form, out var vernWs))
			{
				var ws = TsStringUtils.GetWsAtOffset(m_tbForm.Tss, 0);
				if (!GetSearchKey(ws, searchKey, out form, out vernWs))
				{
					return;
				}
				wsSelHvo = ws;
			}

			if (m_oldSearchKey == searchKey && m_oldSearchWs == wsSelHvo)
			{
				return; // Nothing new to do, so skip it.
			}

			if (m_oldSearchKey != string.Empty || searchKey != string.Empty)
			{
				StartSearchAnimation();
			}
			// disable Go button until we rebuild our match list.
			m_btnOK.Enabled = false;
			m_oldSearchKey = searchKey;
			m_oldSearchWs = wsSelHvo;
			m_matchingObjectsBrowser.SearchAsync(new[] { new SearchField(WfiWordformTags.kflidForm, TsStringUtils.MakeString(form ?? string.Empty, vernWs)) });
		}

		private bool GetSearchKey(int ws, string searchKey, out string form, out int vernWs)
		{
			form = null;
			vernWs = 0;

			if (m_vernHvos.Contains(ws))
			{
				vernWs = ws;
				form = searchKey;
			}
			else
			{
				return false;
			}

			return true;
		}
		#endregion

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
			}

			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WordformGoDlg));
			this.m_panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).BeginInit();
			this.SuspendLayout();
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			//
			// m_btnInsert
			//
			resources.ApplyResources(this.m_btnInsert, "m_btnInsert");
			//
			// m_objectsLabel
			//
			resources.ApplyResources(this.m_objectsLabel, "m_objectsLabel");
			//
			// WordformGoDlg
			//
			resources.ApplyResources(this, "$this");
			this.m_helpProvider.SetHelpNavigator(this, ((System.Windows.Forms.HelpNavigator)(resources.GetObject("$this.HelpNavigator"))));
			this.Name = "WordformGoDlg";
			this.m_helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.m_panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		/// <summary>
		/// This is the search engine for WordformGoDlg.
		/// </summary>
		private sealed class WordformGoSearchEngine : SearchEngine
		{
			private readonly Virtuals m_virtuals;

			internal WordformGoSearchEngine(LcmCache cache)
				: base(cache, SearchType.Prefix)
			{
				m_virtuals = Cache.ServiceLocator.GetInstance<Virtuals>();
			}

			protected override IEnumerable<ITsString> GetStrings(SearchField field, ICmObject obj)
			{
				switch (field.Flid)
				{
					case WfiWordformTags.kflidForm:
						var form = ((IWfiWordform)obj).Form.StringOrNull(field.String.get_WritingSystemAt(0));
						if (form != null && form.Length > 0)
						{
							yield return form;
						}
						break;
					default:
						throw new ArgumentException(@"Unrecognized field.", nameof(field));
				}
			}

			protected override IList<ICmObject> GetSearchableObjects()
			{
				return Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances().Cast<ICmObject>().ToArray();
			}

			protected override bool IsIndexResetRequired(int hvo, int flid)
			{
				return flid == m_virtuals.LangProjectAllWordforms || flid == WfiWordformTags.kflidForm;
			}

			protected override bool IsFieldMultiString(SearchField field)
			{
				return field.Flid == WfiWordformTags.kflidForm ? true : throw new ArgumentException(@"Unrecognized field.", nameof(field));
			}

			/// <inheritdoc />
			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");
				if (IsDisposed)
				{
					// No need to run it more than once.
					return;
				}

				base.Dispose(disposing);
			}
		}
	}
}