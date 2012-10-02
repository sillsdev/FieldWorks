#ifndef RXPUTIL_H
#define RXPUTIL_H

#define Vector(type, name) \
	int name##_count, name##_alloc; \
	type *name

#define VectorInit(v) \
	((v##_count) = (v##_alloc) = 0, (v) = 0)

#define VectorAlloc(v, n) \
	((v##_alloc) = (n), \
	 ((v) = Realloc((v), (v##_alloc) * sizeof(*(v)))) ? 1 : 0)

#define VectorPush(v, value) \
	(((v##_count) < (v##_alloc) || \
	  ((v) = VectorExtend(v))) ? \
	 ((v)[(v##_count)++] = (value), 1) : \
	 0)

#define VectorPushNothing(v) \
	(((v##_count) < (v##_alloc) || \
	  ((v) = VectorExtend(v))) ? \
	 ((v##_count)++, 1) : \
	 0)

#define VectorPop(v) ((v)[--(v##_count)])

#define VectorLast(v) ((v)[(v##_count)-1])

#define VectorExtend(v) \
	((v##_alloc) = ((v##_alloc) == 0 ? 8 : (v##_alloc) * 2), \
	 Realloc((v), (v##_alloc) * sizeof(*v)))

#define VectorCount(v) (v##_count)

#define VectorSetCount(v, n) \
	(((n) <= (v##_alloc) || \
	  VectorAlloc(v, n)) ? \
	 ((v##_count) = (n)) : \
	 0)

#define VectorCall(v) v, v##_count, v##_alloc

#define VectorProto(type, name) type *name, int name##_count, int name##_alloc

#endif /* RXPUTIL_H */
