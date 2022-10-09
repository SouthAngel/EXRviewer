#pragma once

#define DAPI extern "C" __declspec(dllexport)
typedef unsigned long long point64;



void testf();


DAPI void exrf_init(point64* exrc);

DAPI void exrf_open_file(point64 exrc, const char* path, size_t path_len);

DAPI void exrf_get_res_size(point64 exrc, size_t* w, size_t* h);

DAPI int exrf_get_channel_first(point64 exrc, char* name);

DAPI int exrf_get_channel_next(point64 exrc, char* name);

DAPI int exrf_get_layer_first(point64 exrc, char* name);

DAPI int exrf_get_layer_next(point64 exrc, char* name);

DAPI void exrf_bind_channel_data(point64 exrc, const char* channel, point64 data_buffer);

DAPI void exrf_read_pdata(point64 exrc);

DAPI void exrf_clean(point64 exrc);


// Test function

DAPI void exrf_testc(point64 exrc);

DAPI void exrf_testcg(point64 exrc, int* r);

DAPI void exrf_testcc(point64 exrc, point64 pbuff);

