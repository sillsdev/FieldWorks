#ifdef FOR_LT

#include "lt-defs.h"
#include "lt-memory.h"
#include "lt-errmsg.h"
#include "lt-comment.h"
#include "lt-safe.h"
#include "nsl-err.h"

#define Strerror() strErr()
#define Malloc salloc
#define Realloc srealloc
#define Free sfree
#define fopen stdsfopen

#else

#include "system.h"

#define LT_ERROR(err, format) fprintf(stderr, format)
#define LT_ERROR1(err, format, arg) fprintf(stderr, format, arg)
#define LT_ERROR2(err, format, arg1, arg2) fprintf(stderr, format, arg1, arg2)
#define LT_ERROR3(err, format, arg1, arg2, arg3) fprintf(stderr, format, arg1, arg2, arg3)
#define LT_ERROR4(err, format, arg1, arg2, arg3, arg4) fprintf(stderr, format, arg1, arg2, arg3, arg4)
#define WARN(err, format) fprintf(stderr, format)
#define WARN1(err, format, arg) fprintf(stderr, format, arg)

#define Strerror() strerror(errno)

#endif /* FOR_LT */

#include <stdio.h>
#include <ctype.h>
#include <stdlib.h>
#include <assert.h>
#include <errno.h>
#include <string.h>		/* that's where strerror is.  really. */
#include <sys/types.h>

#ifdef SOCKETS_IMPLEMENTED

#if defined(WIN32) && ! defined(__CYGWIN__)
#undef boolean
#include <winsock.h>
#include <fcntl.h>
#else
#include <unistd.h>
#include <netdb.h>
#include <sys/socket.h>
#include <netinet/in.h>
#endif

#endif

#include "string16.h"
#include "stdio16.h"
#include "http.h"
#include "url.h"

static struct http_headers *read_headers(FILE16 *f16);
static void free_headers(struct http_headers *hs);

static int proxy_port;
static char *proxy_host = 0;

int init_http(void)
{
	char *p;

#if defined(WIN32) && ! defined(__CYGWIN__)
	/* random magic for MS Windows */

	WORD version = MAKEWORD(1, 1);
	WSADATA wsaData;
	int err = WSAStartup(version, &wsaData);
	if (err)
	{
	LT_ERROR(LEFILE, "Error: can't init HTTP interface\n");
	return -1;
	}
	else if(LOBYTE(wsaData.wVersion) != 1 || HIBYTE(wsaData.wVersion) != 1)
	{
	LT_ERROR(LEFILE, "Error: wrong version of WINSOCK\n");
	WSACleanup();
	return -1;
	}
#endif

	/* See if we are supposed to use a proxy */

	if((p = getenv("http_proxy")))
	{
	char *colon, *slash;
	if(strncmp(p, "http://", 7) == 0)
		p += 7;
	proxy_host = strdup8(p);
	if((slash = strchr(proxy_host, '/')))
		*slash = 0;
	if((colon = strchr(proxy_host, ':')))
	{
		proxy_port = atoi(colon+1);
		*colon = 0;
	}
	else
		proxy_port = 80;
	}

	return 0;
}

void deinit_http(void)
{
#if defined(WIN32) && ! defined(__CYGWIN__)
	/* No doubt there's some Windozy nonsense that should go here,
	   but I have no idea what it is. */
#endif

	if(proxy_host)
	Free(proxy_host);
}

/* Open an http URL */

FILE16 *http_open(const char *url,
		  const char *host, int port, const char *path,
		  const char *type, char **redirected_url)
{
#ifndef SOCKETS_IMPLEMENTED
	LT_ERROR(NEUNSUP,
		"http: URLs are not yet implemented on this platform\n");
	return 0;
#else
	FILE16 *f16;
	struct sockaddr_in addr;
	struct hostent *hostent;
	int s, server_major, server_minor, status, count, c;
	char reason[81];
	const char *server_host, *request_uri;
	int server_port;
	char buf[100];
	int i;
	struct http_headers *hs;

	if(*type != 'r')
	{
	LT_ERROR1(LEFILE, "Error: can't open http URL \"%s\" for writing\n",
			 url);
	return 0;
	}

	if(!host)
	{
	LT_ERROR1(LEFILE, "Error: no host part in http URL \"%s\"\n", url);
	return 0;
	}

	if(proxy_host)
	{
	server_host = proxy_host;
	server_port = proxy_port;
	request_uri = url;
	}
	else
	{
	server_host = host;
	server_port = port == -1 ? 80 : port;
	request_uri = path;
	}

	/* Create the socket */

	s = socket(PF_INET, SOCK_STREAM, 0);
#if defined(WIN32) && ! defined(__CYGWIN__)
	if (s == INVALID_SOCKET) {
	  LT_ERROR1(LEFILE, "Error: system call socket failed: %d\n",
		   WSAGetLastError());
	};
#else
	if(s == -1) {
	LT_ERROR1(LEFILE, "Error: system call socket failed: %s\n",
			 Strerror());
	return 0;
	};
#endif

	/* Find the server address */

	hostent = gethostbyname(server_host);
	if(!hostent)
	{
	LT_ERROR3(LEFILE, "Error: can't find address for %shost \"%s\" "
				  "in http URL \"%s\"\n",
		  proxy_host ? "proxy " : "", server_host, url);
	return 0;
	}

	memset(&addr, 0, sizeof(addr));
	addr.sin_family = AF_INET;
	/* If we were really enthusiastic, we would try all the host's addresses */
	memcpy(&addr.sin_addr, hostent->h_addr, hostent->h_length);
	addr.sin_port = htons((unsigned short)server_port);

	/* Connect */

	if(connect(s, (struct sockaddr *)&addr, sizeof(addr)) == -1)
	{
	LT_ERROR4(LEFILE, "Error: connection to %shost \"%s\" failed "
				  "for http URL \"%s\": %s\n",
		  proxy_host ? "proxy " : "", server_host, url, Strerror());
	return 0;
	}

	/* Make a FILE16 */

#if defined(WIN32) && ! defined(__CYGWIN__)
	f16 = MakeFILE16FromWinsock(s, "rw");
#else
	f16 = MakeFILE16FromFD(s, "rw");
#endif
	SetCloseUnderlying(f16, 1);
	SetFileEncoding(f16, CE_unspecified_ascii_superset);
	SetNormalizeLineEnd(f16, 0);

	/* Send the request */

	/*
	 * Apparently on the Macintosh, \n might not be ASCII LF, so we'll
	 * use numerics to be sure.
	 */

	Fprintf(f16, "GET %s HTTP/1.0\015\012Connection: close\015\012",
		request_uri);
	Fprintf(f16, "Accept: text/xml, application/xml, */*\015\012");
	if(port != -1)
	Fprintf(f16, "Host: %s:%d\015\012\015\012", host, port);
	else
	Fprintf(f16, "Host: %s\015\012\015\012", host);

	if(Ferror(f16))
	{
	LT_ERROR1(LEWRTF, "Error: write to socket failed: %s\n",Strerror());
	Fclose(f16);
	return 0;
	}

	/* Read the status line */

	i = 0;
	while(1)
	{
	switch(c = Getu(f16))
	{
	case '\n':
		buf[i] = 0;
		goto done_status;
	case '\r':
		break;
	case EOF:
		LT_ERROR2(LEFILE,
			  "Error: incomplete status line from server for URL \"%s\"\n%s\n",
			  url, Strerror());
		Fclose(f16);
		return 0;
	default:
		if(i < sizeof(buf) - 1)
		buf[i++] = c;
	}
	}
 done_status:

	count=sscanf(buf, "HTTP/%d.%d %d %80[^\012]",
		 &server_major, &server_minor, &status, reason);

	if(count != 4)
	{
	LT_ERROR3(LEFILE,
			 "Error: bad status line from server for URL \"%s\"\n%d %s\n",
			 url, count, Strerror());
	Fclose(f16);
	return 0;
	}

	switch(status)
	{
	case 200:			/* OK */
	break;
	case 301:			/* Moved Permanently */
	case 302:			/* Moved Temporarily */
	break;
	default:
	LT_ERROR3(LEFILE, "Error: can't retrieve \"%s\": %d %s\n",
			 url, status, reason);
	Fclose(f16);
	return 0;
	}

	/* Read other headers */

	if(!(hs = read_headers(f16)))
	{
	LT_ERROR1(LEFILE, "Error: EOF or error in headers retrieving \"%s\"\n", url);
	Fclose(f16);
	return 0;
	}

	if(status == 301 || status == 302)
	{
	char *final_url;

	for(i=0; i<VectorCount(hs->header); i++)
		if(strcmp8(hs->header[i]->name, "Location") == 0)
		{
		Fclose(f16);
		f16 = url_open(hs->header[i]->value, 0, type, &final_url);
		if(!final_url)
			final_url = strdup8(hs->header[i]->value);
		if(redirected_url)
			*redirected_url = final_url;
		else
			Free(final_url);
		free_headers(hs);
		return f16;
		}

	LT_ERROR1(LEFILE, "Error: URL \"%s\" moved, but no new location\n",
		  url);
	Fclose(f16);
	return 0;
	}

	free_headers(hs);

	if(redirected_url)
	*redirected_url = 0;

	return f16;
#endif /* SOCKETS_IMPLEMENTED */
}

#define CR 13
#define LF 10

static struct http_headers *read_headers(FILE16 *f16)
{
	struct http_headers *hs;
	struct http_header *h;
	int c;
	Vector(char8, text);
	char8 *s, *t;

	if(!(hs = Malloc(sizeof(*hs))))
	return 0;
	VectorInit(hs->header);

	/* Read all the headers into a string */

	VectorInit(text);
	while(1)
	{
	switch(c = Getu(f16))
	{
	case CR:
		/* Ignore */
		break;
	case EOF:
		/* An error, I suppose, but just treat as end of headers */
		goto done;
	case LF:
		/* blank line -> end of headers */
		if(VectorCount(text) == 0 || VectorLast(text) == LF)
		goto done;
		/* otherwise fall through */
	default:
		if(!VectorPush(text, c))
		return 0;
		break;
	}
	}
 done:
	VectorPush(text, 0);	/* nul-terminate */

	/* Parse into name and value */

	t = text;
	while(*t)
	{
	if(!(h = Malloc(sizeof(*h))))
		return 0;
	if(!VectorPush(hs->header, h))
		return 0;

	/* Get field name */

	s = t;
	while(*t != ':')
	{
		if(*t == 0)
		goto error;
		/* could check for some other errors */
		t++;
	}

	*t++ = 0;
	if(!(h->name = strdup8(s)))
		return 0;

	/* Skip whitespace between name: and value */

	while(*t == ' ' || *t == '\t')
		t++;

	/* Get field value */

	s = t;
	while(1)
	{
		if(*t == 0)
		goto error;
		if(*t == LF && *(t+1) != ' ' && *(t+1) != '\t')
		{
		*t++ = 0;
		if(!(h->value = strdup8(s)))
			return 0;
		break;
		}
		else
		t++;
	}
	}

	Free(text);
	return hs;

 error:
	free_headers(hs);
	Free(text);

	return 0;
}

static void free_headers(struct http_headers *hs)
{
	int i;

	for(i=0; i<VectorCount(hs->header); i++)
	{
	Free(hs->header[i]->name);
	Free(hs->header[i]->value);
	Free(hs->header[i]);
	}
	Free(hs->header);
	Free(hs);
}
