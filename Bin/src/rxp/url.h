/* url.h	-- Henry Thompson
 *
 * $Header: /disk/b/cvsroot/ltg/RXP/include/url.h,v 1.8 2000/01/27 15:22:08 richard Exp $
 */

#ifndef _URL_H
#define _URL_H

#include <stdio.h>
#include "stdio16.h"
#include "charset.h"

extern STD_API int init_url(void);
extern STD_API void deinit_url(void);

extern STD_API char8 * EXPRT
	url_merge(const char8 *url, const char8 *base,
		  char8 **scheme, char8 **host, int *port, char8 **path);
extern STD_API FILE16 *url_open(const char8 *url, const char8 *base,
				const char8 *type, char8 **merged_url);
extern STD_API char8 *EXPRT default_base_url(void);

#endif
