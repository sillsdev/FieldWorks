#include "stdafx.h"
#include "LinkedStack.h"

void Stack::push(const BSTR& item, const int& Ip, const int& Rp,
	const CList<Range,Range> & listElem)
{
	top = new link(item,Ip,Rp,listElem, top);
}

void Stack::pop(BSTR* elem, int * Ip, int * Rp, CList<Range,Range> * listElem)
{
	if(!isEmpty())
	{
		*Ip = top->ichwIp;
		*Rp = top->ichwRp;
		*elem = top->element;
		listElem->RemoveAll();
		listElem->AddHead(&(top->myList));
		link* ltemp = top->next;
		delete top;
		top = ltemp;
	}
}


void Stack::topValue(BSTR* elem, int * Ip, int * Rp, CList<Range,Range> * listElem)
{
	if(!isEmpty())
	{
		*Ip = top->ichwIp;
		*Rp = top->ichwRp;
		*elem = top->element;
		listElem->AddHead(&(top->myList));
	}
}

bool Stack::isEmpty()
{ return (top == NULL); }

void Stack::clear()
{
	while(top != NULL)
	{
		link *temp = top;
		top = top->next;
		delete temp;
	}
}

long Stack::NumOfElem()
{
	if(!isEmpty())
	{
		long count = 0;
		link *ltemp = top;
		while(ltemp != NULL)
		{
			ltemp = ltemp->next;
			count++;
		}
		return count;
	}
	return 0;
}