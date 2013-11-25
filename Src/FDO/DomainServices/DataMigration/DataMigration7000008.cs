// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000007.cs
// Responsibility: Bush
//
// <remarks>
// </remarks>

using System.Collections.Generic;
using System.Xml.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000007 to 7000008.
	///
	/// 1) Remove orphaned CmBaseAnnotations, as per FWR-98:
	/// "Since we won't try to reuse wfic or segment annotations that no longer have
	/// BeginObject point to a paragraph, we should remove (ignore) these annotations
	/// when migrating an old database (FW 6.0 or older) into the new architecture"
	/// </summary>
	///
	/// <remarks>
	/// This migration needs to delete all CmBaseAnnotation objects which:
	///		1. Have a null BeginObject property, and
	///		2. Have AnnotationType property with values of:
	///			A. Text Segment (), or
	///			B. Wordform in context (), or
	///			C. Punctuation in context ().
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000008 : IDataMigration
	{
		#region IDataMigration Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 1) Remove orphaned CmBaseAnnotations, as per FWR-98:
		/// "Since we won't try to reuse wfic or segment annotations that no longer have
		/// BeginObject point to a paragraph, we should remove (ignore) these annotations
		/// when migrating an old database (FW 6.0 or older) into the new architecture"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000007);

/* Expected data (relevant data, that is) format.
			<rt guid="22a8431f-f974-412f-a261-8bd1a4e1be1b" class="CmBaseAnnotation">
				<CmObject />
				<CmAnnotation>
					<AnnotationType> <!-- Entire element will be missing if prop is null. -->
						<objsur guid="eb92e50f-ba96-4d1d-b632-057b5c274132" t="r" />
					</AnnotationType>
				</CmAnnotation>
				<CmBaseAnnotation>
					<BeginObject> <!-- Entire element will be missing if prop is null. -->
						<objsur guid="93dcb15d-f622-4329-b1b5-e5cc832daf01" t="r" />
					</BeginObject>
				</CmBaseAnnotation>
			</rt>
*/
			var interestingAnnDefIds = new List<string>
				{
					DataMigrationServices.kSegmentAnnDefnGuid.ToLower(),
					DataMigrationServices.kTwficAnnDefnGuid.ToLower(),
					DataMigrationServices.kPficAnnDefnGuid.ToLower(),
					DataMigrationServices.kConstituentChartAnnotationAnnDefnGuid.ToLower()
				};

			//Collect up the ones to be removed.
			var goners = new List<DomainObjectDTO>();
			foreach (var annDTO in domainObjectDtoRepository.AllInstancesSansSubclasses("CmBaseAnnotation"))
			{
				var annElement = XElement.Parse(annDTO.Xml);
				var typeElement = annElement.Element("CmAnnotation").Element("AnnotationType");
				if (typeElement == null
					|| !interestingAnnDefIds.Contains(typeElement.Element("objsur").Attribute("guid").Value.ToLower()))
					continue; // uninteresing annotation type, so skip it.

				// annDTO is a segment, wordform, punctform, or constituent chart annotation.
				if (annElement.Element("CmBaseAnnotation").Element("BeginObject") != null)
					continue; // Has data in BeginObject property, so skip it.

				goners.Add(annDTO);
			}

			// Remove them.
			foreach (var goner in goners)
				DataMigrationServices.RemoveIncludingOwnedObjects(domainObjectDtoRepository, goner, true);

			// Some stuff may reference the defective Discourse Cahrt Annotation, so clear out the refs to them.
			DataMigrationServices.Delint(domainObjectDtoRepository);

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}
		#endregion
	}
}
