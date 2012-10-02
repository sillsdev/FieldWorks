#include "GrInclude.h"	// Graphite Include files
#include <afxtempl.h>	// For CList
#include <afxadv.h>		// For CSharedFile

// range is defined range of text with its font properties
typedef struct tagRange {
	LOGFONT lf;					// fontface stored in LOGFONT
	LgCharRenderProps lgchrp;	// font size, colour, etc. grfx cannot be stored.
	int sizePercent;			// font size is defined to be a percentage of the default size
								// for the control
	int numChar;			// number of characters in the range
	bool firstInPara; 		// whether the range is first in paragraph
} Range;


typedef struct tagNamedEngine {
	CString engineName;
	GrEngine * pgreng;
} NamedEngine;


class DrawnSeg
{
public:
	DrawnSeg()
	{
		pgrseg = NULL;
		pgrtext = NULL;
	}
	// Because DrawnSegs are so frequently copied, the DrawnSeg is not responsible for deleting these
	// pointers. Instead, m_segList, the master segment list into which permanent DrawnSegs are placed,
	// will delete them when DrawnSegs are removed.

	GrSegment * pgrseg; //GrSegment use for drawing & other Graphite functions
	GrTextSrc * pgrtext; // text source set up with character properties need for this segment
	RECT rSrc;			//source RECT storing the position of the segment
	int rI;				//index of corresponding range
	int startPos; 	    //logical start position of segment in GrTextSrc
	int stopPos; 		//logical stop position of segment in GrTextSrc
	int dxWidth; 		//physical width
	int rngStartPos;
	int lineHeight;     // line height.
	int lineRsrcTop;    // y-coordinate of top of line, with respect only to rSrc, not rDst. //14AprCSC
};


class link {
public:
	int ichwIp;
	int ichwRp;
	//int NoOfParas;
	CComBSTR element;
	link *next;
	CList<Range,Range> myList;

	link(const BSTR& elemval, const int& Ip,
		const int& Rp,const CList<Range,Range> & elem, link* nextval = NULL)
	{
		element = elemval;
		ichwIp = Ip;
		ichwRp = Rp;
		next = nextval;

		int index = elem.GetCount();
		while(index > 0)
		{
			index--;
			myList.AddHead(elem.GetAt(elem.FindIndex(index)));
		}

		//NoOfParas = NoParas;
	}
	link(link* nextval = NULL) {next = nextval;}
	~link() {myList.RemoveAll(); element.Empty();}
};

class Stack {
private:
	link *top; //pointer to top stack
public:
	Stack()
	{ top = NULL;}
	~Stack() {clear();}
	void clear();
	void push(const BSTR& item, const int& Ip, const int& Rp, const CList<Range,Range> & listElem);
	void pop(BSTR* elem, int* Ip, int* Rp,CList<Range,Range> * listElem);
	void topValue(BSTR* elem, int* Ip, int* Rp,CList<Range,Range> * listElem);
	bool isEmpty();
	long NumOfElem();
};
