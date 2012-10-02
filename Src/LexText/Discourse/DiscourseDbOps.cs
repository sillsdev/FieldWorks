using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;

namespace SIL.FieldWorks.Discourse
{
	/// <summary>
	/// This class encapsulates in static methods the Discourse functions that require a 'real' database
	/// backing FDO.
	/// </summary>
	class DiscourseDbOps
	{
		/// <summary>
		/// Reminder that instances can't be created.
		/// </summary>
		private DiscourseDbOps()
		{
		}

		/// <summary>
		/// Return the next wfics that have not yet been added to the chart (up to maxContext of them).
		/// </summary>
		/// <param name="maxContext"></param>
		/// <returns></returns>
		public static int[] NextUnusedInput(FdoCache cache, int hvoStText, int maxContext, int hvoChart)
		{
			int hvoWficDefn = CmAnnotationDefn.Twfic(cache).Hvo;
			int hvoCcaDefn = CmAnnotationDefn.ConstituentChartAnnotation(cache).Hvo;
			// First two lines yield annotations on the right text of the right type (wfics).
			// balance further limits it to uncharted ones
			string sql =
			"select top(" + maxContext + ") cba.id, stp.owner$ from CmBaseAnnotation_ cba"
				+ " join StTxtPara_ stp on cba.BeginObject = stp.id and cba.AnnotationType = " + hvoWficDefn
					+ " and stp.OwnFlid$ = 14001 and stp.Owner$ = " + hvoStText
					+ " where not exists(Select * from DsConstChart_Rows row"
					+ " join CmIndirectAnnotation_AppliesTo row_cca on row_cca.src = row.Dst and row.Src = " + hvoChart
					+ " join CmIndirectAnnotation_AppliesTo cca_cba on cca_cba.src = row_cca.dst and cca_cba.Dst = cba.id)"
				+ " order by stp.OwnOrd$, cba.BeginOffset";
			return DbOps.ReadIntArrayFromCommand(cache, sql, null);
		}

		/// <summary>
		/// Return the Row(CCR) and WficGroup(CCA) of a charted Wfic as an array of integers.
		/// [0] is CCR.hvo; [1] is CCA.hvo or empty array if the Wfic isn't charted in the given Chart.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoChart"></param>
		/// <param name="hvoWfic"></param>
		/// <returns></returns>
		public static int[] FindChartLocOfWfic(FdoCache cache, int hvoChart, int hvoWfic)
		{
			string sql =
			"select row_at.src, cca_at.src from CmIndirectAnnotation_AppliesTo cca_at"
				+ " join CmIndirectAnnotation_AppliesTo row_at on row_at.dst = cca_at.Src"
				+ " and cca_at.Dst = " + hvoWfic
				+ " join DsConstChart_Rows cc_rows on cc_rows.dst = row_at.src and cc_rows.src = " + hvoChart;
			return DbOps.ReadIntsFromRow(cache, sql, "", 2);
		}
	}
}
