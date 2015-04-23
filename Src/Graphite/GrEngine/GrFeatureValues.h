/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2015 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: GrFeatureValues.h
Responsibility: Sharon Correll
Last reviewed: Not yet.

Description:


----------------------------------------------------------------------------------------------*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef GR_FEATVAL_INCLUDED
#define GR_FEATVAL_INCLUDED

//:End Ignore

namespace gr
{

/*----------------------------------------------------------------------------------------------
	A convenient way to group together style and feature information that interacts
	with the rules.

	Hungarian: fval
----------------------------------------------------------------------------------------------*/
class GrFeatureValues
{
	friend class GrCharStream;
	friend class GrSlotAbstract;
	friend class GrSlotState;

public:
	//	Standard constructor:
	GrFeatureValues()
	{
		m_nStyleIndex = 0;
		std::fill(m_rgnFValues, m_rgnFValues + kMaxFeatures, 0);
	}

	//	Copy constructor:
	GrFeatureValues(const GrFeatureValues & fval)
	{
		m_nStyleIndex = fval.m_nStyleIndex;
		std::copy(fval.m_rgnFValues, fval.m_rgnFValues + kMaxFeatures, m_rgnFValues);
	}

	//	For transduction logging:
#ifdef TRACING
	void WriteXductnLog(GrTableManager * ptman, std::ostream &);
#endif // TRACING

protected:
	int		m_nStyleIndex;
	int		m_rgnFValues[kMaxFeatures];
};

} // namespace gr


#endif // !GR_FEATVAL_INCLUDED
