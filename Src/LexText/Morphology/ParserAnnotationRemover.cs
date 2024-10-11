using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.LCModel.Infrastructure;
using SIL.LCModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIL.Data;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// This class serves to remove all annotations produced by the parser.
	/// </summary>
	public class ParserAnnotationRemover : IUtility
	{
		#region Data members

		private UtilityDlg m_dlg;
		const string kPath = "/group[@id='Linguistics']/group[@id='Morphology']/group[@id='RemoveParserAnnotations']/";

		#endregion Data members

		/// <summary>
		/// Override method to return the Label property.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Label;
		}

		#region IUtility implementation

		/// <summary>
		/// Get the main label describing the utility.
		/// </summary>
		public string Label
		{
			get
			{
				Debug.Assert(m_dlg != null);
				return StringTable.Table.GetStringWithXPath("Label", kPath);
			}
		}

		/// <summary>
		/// Set the UtilityDlg.
		/// </summary>
		/// <remarks>
		/// This must be set, before calling any other property or method.
		/// </remarks>
		public UtilityDlg Dialog
		{
			set
			{
				Debug.Assert(value != null);
				Debug.Assert(m_dlg == null);

				m_dlg = value;
			}
		}

		/// <summary>
		/// Load 0 or more items in the list box.
		/// </summary>
		public void LoadUtilities()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.Utilities.Items.Add(this);

		}

		/// <summary>
		/// Notify the utility that has been selected in the dlg.
		/// </summary>
		public void OnSelection()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.WhenDescription = StringTable.Table.GetStringWithXPath("WhenDescription", kPath);
			m_dlg.WhatDescription = StringTable.Table.GetStringWithXPath("WhatDescription", kPath);
			m_dlg.RedoDescription = StringTable.Table.GetStringWithXPath("RedoDescription", kPath);
		}

		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		public void Process()
		{
			Debug.Assert(m_dlg != null);
			var cache = m_dlg.PropTable.GetValue<LcmCache>("cache");
			ICmBaseAnnotationRepository repository = cache.ServiceLocator.GetInstance<ICmBaseAnnotationRepository>();
			IList<ICmBaseAnnotation> problemAnnotations = (from ann in repository.AllInstances() where ann.SourceRA is ICmAgent select ann).ToList();
			if (problemAnnotations.Count > 0)
			{
				// Set up progress bar.
				m_dlg.ProgressBar.Minimum = 0;
				m_dlg.ProgressBar.Maximum = problemAnnotations.Count;
				m_dlg.ProgressBar.Step = 1;

				NonUndoableUnitOfWorkHelper.Do(cache.ActionHandlerAccessor, () =>
				{
					foreach (ICmBaseAnnotation problem in problemAnnotations)
					{
						cache.DomainDataByFlid.DeleteObj(problem.Hvo);
						m_dlg.ProgressBar.PerformStep();
					}
				});
			}
		}

		#endregion IUtility implementation
	}
}
