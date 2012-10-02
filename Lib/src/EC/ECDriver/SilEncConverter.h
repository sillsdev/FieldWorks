#pragma once

#import "SilEncConverters22.tlb"  raw_interfaces_only
using namespace SilEncConverters22;

#import "ECInterfaces.tlb"  raw_interfaces_only
using namespace ECInterfaces;

typedef CComPtr<SilEncConverters22::IEncConverters> IEC22s;
typedef CComPtr<SilEncConverters22::IEncConverter>  IEC22;
typedef CComPtr<ECInterfaces::IEncConverters> IEC30s;
typedef CComPtr<ECInterfaces::IEncConverter>  IEC30;

class CSilEncConverter
{
public:
	CSilEncConverter(void);
	~CSilEncConverter(void);

	CStringW Convert(const CStringW& strInput);

	bool operator!() const throw()
	{
		return (!m_aEC22 && !m_aEC30);
	}

	bool IsInputLegacy() const;
	bool IsOutputLegacy() const;
	int	CodePageInput() const;
	int	CodePageOutput() const;

	// initial the IEC
	HRESULT Initialize(const CStringW& strFriendlyName, BOOL bDirectionForward, int eNormalizeFlag);

	HRESULT AutoSelect();

	CStringW	Description();

	// my properties
	CStringW	ConverterName;
	BOOL		DirectionForward;
	int			NormalizeOutput;

protected:
	bool IsSEC30() const
	{
		return !!m_aEC30;
	}

	bool IsSEC22() const
	{
		return !!m_aEC22;
	}

	void Detach();

protected:
	IEC22	m_aEC22;
	IEC30	m_aEC30;
};

extern BOOL ProcessHResult(HRESULT hr, IUnknown* p, const IID& iid);
