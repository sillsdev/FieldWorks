// --------------------------------------------------------------------------------------------
// Copyright (C) 2004 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: ConstraintFailure.cs
// Originator: John Hatton
// Last reviewed: never
//
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------

using System.Linq;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// <summary>
	/// information about why a constraint failed
	/// </summary>
	public class ConstraintFailure
	{
		private readonly ICmObject m_object;
		private readonly int m_flid;
		private readonly string m_explanation;
		private readonly FdoCache m_cache;
		private string m_xmlDescription;

		//	protected string m_helpId;

		/// <summary>
		/// constructor using a simple string explanation.
		/// Often, work is needed to make (and/or remove) an annotation related to this constraint.
		/// If so, there are two usage patterns, depending on how important it is to avoid making database changes
		/// if nothing changed.
		///
		/// The simple pattern:
		/// ConstraintFailure.RemoveObsoleteAnnotations(obj);
		/// if (there's a problem)
		///	{
		///		var failure = new ConstraintFailure(...);
		///		failure.MakeAnnotation();
		/// }
		///
		/// The more complex pattern:
		///
		/// var anyChanges = false;
		/// ConstraintFailure failure = null;
		/// if (there's a problem)
		/// {
		///		failure = new ConstraintFailure(...);
		///		anyChanges = failure.IsAnnotationCorrect();
		/// }
		/// else
		///		anyChanges = ConstraintFailure.AreThereObsoleteChanges(obj);
		/// if (anyChanges)
		/// {
		///		...wrap in a UOW...
		///		ConstraintFailure.RemoveObsoleteAnnotations(obj);
		///		if (failure != null)
		///			failure.MakeAnnotation();
		/// }
		/// </summary>
		public ConstraintFailure(ICmObject problemObject, int flid, string explanation)
		{
			m_cache = problemObject.Cache;
			m_object = problemObject;
			m_flid = flid;
			m_explanation = explanation;
//			m_explanation = new StText();
//			StTxtParaBldr paraBldr = new StTxtParaBldr(m_cache);
//			//review: I have no idea what this is as to be
//			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps("Paragraph");
//			//todo: this pretends that the default analysis writing system is also the user interface 1.
//			//but I don't really know what's the right thing to do.
//			paraBldr.AppendRun(m_explanation, StyleUtils.CharStyleTextProps(null, m_cache.DefaultAnalWs));
//			paraBldr.CreateParagraph(annotation.TextOAHvo);
		}

		/// <summary>
		/// optional XML that can be used to highlight or describe the particular problem in an object.
		/// corresponds to the annotation field CompDetails
		/// </summary>
		public string XmlDescription
		{
			get
			{
				return m_xmlDescription;
			}
			set
			{
				m_xmlDescription = value;
			}
		}

		/// <summary>
		/// generate message to display to the user describing the problem
		/// </summary>
		/// <returns>a message</returns>
		public string GetMessage()
		{
			return !string.IsNullOrEmpty(m_explanation) ? m_explanation : Strings.ksNoExplanation;
		}

		/// <summary>
		/// Remove any obsolete annotations for this object for our agent. Caller should make UOW.
		/// </summary>
		/// <param name="target"></param>
		public static void RemoveObsoleteAnnotations(ICmObject target)
		{
			var agt = target.Cache.LanguageProject.ConstraintCheckerAgent;
			var errors = from error in target.Cache.ServiceLocator.GetInstance<ICmBaseAnnotationRepository>().AllInstances()
						 where error.BeginObjectRA == target && error.SourceRA == agt
						 select error;
			// Remove all existing error reports
			var anns = target.Cache.LanguageProject.AnnotationsOC;
			foreach (var errReport in errors)
				anns.Remove(errReport);
		}

		/// <summary>
		/// Answer true if there are any existing annotations for this target and agent.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public static bool AreThereObsoleteAnnotations(ICmObject target)
		{
			var agt = target.Cache.LanguageProject.ConstraintCheckerAgent;
			return (from error in target.Cache.ServiceLocator.GetInstance<ICmBaseAnnotationRepository>().AllInstances()
				where error.BeginObjectRA == target && error.SourceRA == agt
				select error).Any();

		}

		/// <summary>
		/// attach an annotation describing this failure to the object. *Does Not* remove previous annotations! Generally call RemoveObsoleteAnnotations first.
		/// </summary>
		/// <remarks> I say it does not remove previous annotations because I haven't thought about how much smarts
		///  it would take to only remove once associated with this particular failure. So I am stipulating for now that
		///  the caller should first remove all of the kinds of indications which it might create.</remarks>
		/// <returns></returns>
		public ICmBaseAnnotation MakeAnnotation()
		{
			var annotation = m_cache.ServiceLocator.GetInstance<ICmBaseAnnotationFactory>().Create();
			m_cache.LanguageProject.AnnotationsOC.Add(annotation);
			annotation.CompDetails = m_xmlDescription;

			annotation.TextOA = m_cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			var paraBldr = new StTxtParaBldr(m_cache);
			//review: I have no idea what this has to be.
			paraBldr.ParaStyleName = "Paragraph";
			//todo: this pretends that the default analysis writing system is also the user
			// interface 1.  but I don't really know what's the right thing to do.
			paraBldr.AppendRun(m_explanation,
				StyleUtils.CharStyleTextProps(null,
				m_cache.DefaultAnalWs));
			paraBldr.CreateParagraph(annotation.TextOA);

			annotation.BeginObjectRA = m_object;
			annotation.Flid = m_flid;
			annotation.CompDetails = m_xmlDescription;
			annotation.SourceRA = m_cache.LanguageProject.ConstraintCheckerAgent;
			return annotation;
		}

		/// <summary>
		/// Answer true if there is exactly one relevant annotation for our target object which has exactly the state indicated by our data.
		/// </summary>
		/// <returns></returns>
		public bool IsAnnotationCorrect()
		{
			var agt = m_object.Cache.LanguageProject.ConstraintCheckerAgent;
			var errors = (from error in m_object.Cache.ServiceLocator.GetInstance<ICmBaseAnnotationRepository>().AllInstances()
					 where error.BeginObjectRA == m_object && error.SourceRA == agt
					 select error).ToList();
			if (errors.Count() != 1)
				return false; // things can't be in the exact right state; we are missing the annotation or have too many
			var existing = errors.First();
			// We already know its BeginObjectRA and SourceRA are correct.
			if (existing.CompDetails != m_xmlDescription || existing.Flid != m_flid)
				return false;
			var text = existing.TextOA;
			if (text == null || text.ParagraphsOS.Count != 1)
				return false;
			var para = text.ParagraphsOS[0] as IStTxtPara;
			return para.Contents.Text == m_explanation; // we could get very picky and check the writing system etc, but I don't think it is necessary.
		}
	}
}
