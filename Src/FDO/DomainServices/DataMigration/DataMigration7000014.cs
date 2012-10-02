using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Migrates from 7000013 to 7000014
	/// </summary>
	/// ------------------------------------------------------------------------------------
	internal class DataMigration7000014 : IDataMigration
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Changes the use of CmAgentEvaluation, so that instead of one evaluation per agent/target pair
		/// owned by Agent.Evauations and referring to the target, there are only two CmAgentEvaluations
		/// owned by each CmAgent in their Approves and Disapproves properties, and they are referred to
		/// by the new WfiAnalysis.Evaluations Reference Collection.
		///
		/// Specifically, for each CmAgent,
		/// - Create two CmAgentEvaluations, one owned in Approves and one in Disapproves.
		/// - For each item in Evaluations, if Accepted, add the appropriate Approves evaluation to
		///     the target of the evaluation, otherwise, add the appropriate Disapproves (if there is a target).
		/// - Delete the Evaluations.
		///
		/// As a side effect, since all existing CmAgentEvaluations are deleted, obsolete properties are removed.
		/// </summary>
		/// <param name="domainObjectDtoRepository">Repository of all CmObject DTOs available for
		/// one migration step.</param>
		/// ------------------------------------------------------------------------------------
		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository)
		{
			DataMigrationServices.CheckVersionNumber(domainObjectDtoRepository, 7000013);

			// from guid of a WfiAnalysis to list of evaluations.
			var map = new Dictionary<string, List<string>>();
			foreach (var agentDto in domainObjectDtoRepository.AllInstancesSansSubclasses("CmAgent"))
			{
				var rtElement = XElement.Parse(agentDto.Xml);
				var agentElement = rtElement.Elements("CmAgent").First();
				// Create two new CmAgentEvaluations.
				var newApprovesEval = MakeEvaluation(agentDto.Guid, domainObjectDtoRepository, agentElement, "Approves");
				var newDisapprovesEval = MakeEvaluation(agentDto.Guid, domainObjectDtoRepository, agentElement, "Disapproves");
				var evaluations = agentElement.Elements("Evaluations").FirstOrDefault();
				if (evaluations != null)
				{
					foreach (var objsur in evaluations.Elements("objsur"))
					{
						var evalGuidAttr = objsur.Attribute("guid");
						if (evalGuidAttr == null) continue;

						var evalGuid = evalGuidAttr.Value;
						var obsoleteEvalDto = domainObjectDtoRepository.GetDTO(evalGuid);
						if (obsoleteEvalDto == null)
							continue; // defensive (RandyR says there is no need to be defensive, since the repos will throw an exception, if the guid ois not found.)
						var agentEvalElt = XElement.Parse(obsoleteEvalDto.Xml).Element("CmAgentEvaluation");
						if (agentEvalElt == null)
							continue; // paranoid! (Also, no need for paranoia, since the element *must* be present, or the object cannot be reconstituted.)

						// Delete/Remove the old evaluation.
						domainObjectDtoRepository.Remove(obsoleteEvalDto);

						var acceptedElt = agentEvalElt.Element("Accepted");
						bool accepted = false;
						if (acceptedElt != null)
						{
							var attr = acceptedElt.Attribute("val");
							if (attr != null)
								accepted = attr.Value.ToLowerInvariant() == "true";
						}

						var targetElt = agentEvalElt.Element("Target");
						if (targetElt == null)
							continue;
						var targetObjsur = targetElt.Element("objsur");
						if (targetObjsur == null)
							continue; // paranoid
						var targetGuidAttr = targetObjsur.Attribute("guid");
						if (targetGuidAttr == null)
							continue; // paranoid
						var targetGuid = targetGuidAttr.Value;
						List<String> evals;
						if (!map.TryGetValue(targetGuid, out evals))
						{
							evals = new List<string>();
							map[targetGuid] = evals;
						}

						evals.Add(accepted ? newApprovesEval.Guid : newDisapprovesEval.Guid);
					}
					evaluations.Remove();
				}
				agentDto.Xml = rtElement.ToString();
				domainObjectDtoRepository.Update(agentDto);
			}
			foreach (var kvp in map)
			{
				var analysisDto = domainObjectDtoRepository.GetDTO(kvp.Key);
				var analysisRtElt = XElement.Parse(analysisDto.Xml);
				var analysisElt = analysisRtElt.Element("WfiAnalysis");
				if (analysisElt == null)
					continue; // Paranoid
				var evals = new XElement("Evaluations");
				analysisElt.Add(evals);
				foreach (var eval in kvp.Value)
				{
					evals.Add(new XElement("objsur", new XAttribute("t", "r"), new XAttribute("guid", eval)));
				}
				analysisDto.Xml = analysisRtElt.ToString();
				domainObjectDtoRepository.Update(analysisDto);
			}

			DataMigrationServices.IncrementVersionNumber(domainObjectDtoRepository);
		}

		private static DomainObjectDTO MakeEvaluation(string ownerGuid, IDomainObjectDTORepository domainObjectDtoRepository, XElement agentElement, string owningAttr)
		{
			var newGuid = Guid.NewGuid().ToString().ToLower();
			var newEvalElt = new XElement("rt",
				new XAttribute("class", "CmAgentEvaluation"),
				new XAttribute("guid", newGuid),
				new XAttribute("ownerguid", ownerGuid),
				new XElement("CmObject"),
				new XElement("CmAgentEvaluation"));
			// Create new dto and add to repos.
			var newEval = new DomainObjectDTO(newGuid, "CmAgentEvaluation", newEvalElt.ToString());
			domainObjectDtoRepository.Add(newEval);
			agentElement.Add(new XElement(owningAttr,
				new XElement("objsur", new XAttribute("t", "o"), new XAttribute("guid", newGuid))));

			return newEval;
		}
	}
}
