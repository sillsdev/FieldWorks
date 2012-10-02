#ifndef HTTP_H
#define HTTP_H

#include "charset.h"
#include "stdio16.h"
#include "rxputil.h"

struct http_header {
	char8 *name;
	char8 *value;
};

struct http_headers {
	Vector(struct http_header *, header);
};

int init_http(void);
void deinit_http(void);

FILE16 *http_open(const char *url,
		  const char *host, int port, const char *path,
		  const char *type, char **redirected_url);

#endif /* HTTP_H */
