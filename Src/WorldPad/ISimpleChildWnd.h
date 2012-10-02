#ifndef ISimpleChildWnd_h
#define ISimpleChildWnd_h

// This file may be auto-generated in the future
// Created 20080702 MarkS

#include "VwBaseVc.h"
#include "../AppCore/StVc.h"

/**
 * The ISimpleChildWnd interface specifies a minimal subset of methods for the
 * purpose of passing a usable object in place of a normal WpChildWnd to
 * WpDa for WPX XML loading and saving.
 */
interface ISimpleChildWnd : public IUnknown {
	virtual StVc * ViewConstructor() = 0;
	virtual void UpdateView(int c) = 0;
};

#endif // !ISimpleChildWnd_h
