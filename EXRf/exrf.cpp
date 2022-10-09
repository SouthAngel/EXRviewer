#include "pch.h"
#include "exrf.h"
#include <OpenEXR/openexr.h>
#include <OpenEXR/ImfTestFile.h>
#include <OpenEXR/ImfStdIO.h>
#include <OpenEXR/ImfInputFile.h>
#include <OpenEXR/ImfTiledInputFile.h>
#include <OpenEXR/ImfRgbaFile.h>
#include <OpenEXR/ImfArray.h>
#include <OpenEXR/ImfBoxAttribute.h>
#include <OpenEXR/ImfHeader.h>
#include <OpenEXR/ImfChannelList.h>
#include <OpenEXR/ImfConvert.h>
#include <OpenEXR/ImfFrameBuffer.h>

//#pragma comment(lib, "OpenEXR-3_2.lib")

struct ExrfContext
{
	int tv1 = 1;
	char fileName[1024] = { 0 };
	bool isTiled = false;
	Imf::InputFile* inputFile;
	Imf::ChannelList::ConstIterator channelIterBegin;
	Imf::ChannelList::ConstIterator channelIter;
	Imf::ChannelList::ConstIterator channelIterEnd;
	std::set<std::string> layers;
	std::set<std::string>::const_iterator layerIterBegin;
	std::set<std::string>::const_iterator layerIter;
	std::set<std::string>::const_iterator layerIterEnd;
	Imf::FrameBuffer frameBuffer;
	std::map<std::string, float*> channelMapf;
	std::map<std::string, Imf::Array2D<Imath::half>*> channelMaph;
	int width = 0;
	int height= 0;
};

void testf() {
	std::wcout << L"testf start" << std::endl;
	Imf::InputFile ifile("D:\\001\\Works\\net_files\\3\\2\\CYJM_EP11_13_15_V1_00530_Animation_V002.1267.exr");
	const Imf::Header fheader = ifile.header();
	const Imf::ChannelList &channelList = fheader.channels();
	for (Imf::ChannelList::ConstIterator i = channelList.begin(); i != channelList.end(); ++i) {
		const Imf::Channel &cl = i.channel();
	}
}

void exrf_init(point64* exrc)
{
	auto _exrc = (ExrfContext**)exrc;
	*_exrc = new ExrfContext;
	}

void exrf_open_file(point64 exrc, const char* path, size_t path_len) {
	auto _exrc = (ExrfContext*)exrc;
	memcpy_s(_exrc->fileName, sizeof(_exrc->fileName), path, path_len);
	memset(_exrc->fileName + path_len, 0, 1);
	_exrc->inputFile = new Imf::InputFile(_exrc->fileName);
	bool lt;
	lt = Imf::isOpenExrFile(path);
	_exrc->isTiled = Imf::isTiledOpenExrFile(path);
	auto& header = _exrc->inputFile->header();
	auto& chas = header.channels();
	_exrc->channelIterBegin = chas.begin();
	_exrc->channelIterEnd = chas.end();
	_exrc->channelIter = _exrc->channelIterBegin;
	chas.layers(_exrc->layers);
	_exrc->layerIterBegin = _exrc->layers.begin();
	_exrc->layerIterEnd = _exrc->layers.end();
	_exrc->layerIter = _exrc->layerIterBegin;
	}

void exrf_get_res_size(point64 exrc, size_t* w, size_t* h) {
	auto _exrc = (ExrfContext*)exrc;
	auto& header = _exrc->inputFile->header();
	auto& chas = header.channels();
	auto& dbox = header.dataWindow();
	*w  = _exrc->width = dbox.max.x - dbox.min.x + 1;
	*h  =_exrc->height = dbox.max.y - dbox.min.y + 1;
	}

int exrf_get_channel_first(point64 exrc, char* name)
{
	auto _exrc = (ExrfContext*)exrc;
	_exrc->channelIter = _exrc->channelIterBegin;
	if (_exrc->channelIter == _exrc->channelIterEnd) {
		return 1;
	}
	auto cname = _exrc->channelIter.name();
	strcpy_s(name, 256, cname);
	_exrc->channelIter ++;
	return 0;
}

int exrf_get_channel_next(point64 exrc, char* name)
{
	auto _exrc = (ExrfContext*)exrc;
	if (_exrc->channelIter == _exrc->channelIterEnd) {
		return 1;
	}
	auto cname = _exrc->channelIter.name();
	strcpy_s(name, 256, cname);
	_exrc->channelIter ++;
	return 0;
}

int exrf_get_layer_first(point64 exrc, char* name)
{
	auto _exrc = (ExrfContext*)exrc;
	_exrc->layerIter = _exrc->layerIterBegin;
	if (_exrc->layerIter == _exrc->layerIterEnd) {
		return 1;
	}
	auto cname = *(_exrc->layerIter);
	strcpy_s(name, 256, cname.c_str());
	_exrc->layerIter ++;
	return 0;
}

int exrf_get_layer_next(point64 exrc, char* name)
{
	auto _exrc = (ExrfContext*)exrc;
	if (_exrc->layerIter == _exrc->layerIterEnd) {
		return 1;
	}
	auto cname = *(_exrc->layerIter);
	strcpy_s(name, 256, cname.c_str());
	_exrc->layerIter ++;
	return 0;
}

void exrf_bind_channel_data(point64 exrc, const char* channel, point64 data_buffer) {
	auto _exrc = (ExrfContext*)exrc;
	_exrc->channelMapf[channel] = (float*)data_buffer;
	_exrc->channelMaph[channel] = new Imf::Array2D<Imath::half>(_exrc->height, _exrc->width);
	Imf::Array2D<Imath::half>& tbf = *(_exrc->channelMaph[channel]);
	tbf.resizeErase(_exrc->height, _exrc->width);
	//Imf::Array2D<Imath::half> tbf = Imf::Array2D<Imath::half>(_exrc->height, _exrc->width);
	//_exrc->channelMaph[channel] = tbf;
	//Imf::Array2D<Imath::half> tbf = *_ptbf;
	_exrc->frameBuffer.insert(channel, Imf::Slice(
		Imf::HALF, (char*)(&tbf[0][0]), sizeof(tbf[0][0]) * 1, sizeof(tbf[0][0]) * (_exrc->width), 1, 1, 0.0
	));
}

void exrf_read_pdata(point64 exrc) {
	auto _exrc = (ExrfContext*)exrc;
	_exrc->inputFile->setFrameBuffer(_exrc->frameBuffer);
	if (_exrc->isTiled&&0)
	{
	}
	else {
		auto dbox = _exrc->inputFile->header().dataWindow();
		_exrc->inputFile->readPixels(dbox.min.y, dbox.max.y);
	}
	auto ite = _exrc->channelMaph.begin();
	for (ite; ite!=_exrc->channelMaph.end(); ite++)
	{
		auto fdp = _exrc->channelMapf[ite->first];
		auto hdp = (Imath::half*)ite->second[0][0];
		for (size_t i = 0; i < _exrc->width*_exrc->height; i++)
		{
			fdp[i] = hdp[i];
			if (fdp[i] > 0.0f) 
			{
			}
		}
	}
}

void exrf_clean(point64 exrc) {
	auto _exrc = (ExrfContext*)exrc;
	//_exrc->ifstream->clear();
	delete _exrc->inputFile;
	for (auto ite=_exrc->channelMaph.begin(); ite != _exrc->channelMaph.end(); ite++)
	{
		delete ite->second;
	}
	_exrc->channelMaph.clear();
	delete _exrc;
	}

void exrf_testc(point64 exrc)
{
	auto _exrc = (ExrfContext*)exrc;
}

void exrf_testcg(point64 exrc, int* r)
{
	auto _exrc = (ExrfContext*)exrc;
	}

BYTE half_to_cbyte(Imath::half _in) {
	return (BYTE)(std::min(255.0f * (float)_in, 255.0f));
}

void exrf_testcc(point64 exrc, point64 pbuff) {
	auto _exrc = (ExrfContext*)exrc;
	auto _pbuff = (BYTE*)pbuff;
	auto pcb = _pbuff;
	Imf::InputFile inputFile(_exrc->fileName);
	auto header = inputFile.header();
	auto chas = header.channels();
	std::set<std::string> layerNames;
	chas.layers(layerNames);
	/*
	Imf::Array2D<Imath::half> abuff_R(_exrc->height, _exrc->width);
	Imf::Array2D<Imath::half> abuff_G(_exrc->height, _exrc->width);
	Imf::Array2D<Imath::half> abuff_B(_exrc->height, _exrc->width);
	abuff_R.resizeErase(_exrc->height, _exrc->width);
	abuff_G.resizeErase(_exrc->height, _exrc->width);
	abuff_B.resizeErase(_exrc->height, _exrc->width);
	Imf::FrameBuffer frameBuffer;
	frameBuffer.insert("R", Imf::Slice(
		Imf::HALF, (char*)(&abuff_R[0][0]), sizeof(abuff_R[0][0]) * 1, sizeof(abuff_R[0][0]) * (_exrc->width), 1, 1, 0.0
	));
	frameBuffer.insert("G", Imf::Slice(
		Imf::HALF, (char*)(&abuff_G[0][0]), sizeof(abuff_G[0][0]) * 1, sizeof(abuff_G[0][0]) * (_exrc->width), 1, 1, 0.0
	));
	frameBuffer.insert("B", Imf::Slice(
		Imf::HALF, (char*)(&abuff_B[0][0]), sizeof(abuff_B[0][0]) * 1, sizeof(abuff_B[0][0]) * (_exrc->width), 1, 1, 0.0
	));
	inputFile.setFrameBuffer(frameBuffer);
	auto dbox = inputFile.header().dataWindow();
	inputFile.readPixels(dbox.min.y, dbox.max.y);
	for (auto hi = 0; hi < _exrc->height; hi++) {
		for (auto wi = 0; wi < _exrc->width; wi++) {
			auto hdb = abuff_R[hi][wi];
			auto fdb = half_to_cbyte(hdb);
			_pbuff[(hi * (_exrc->width) + wi) * 4 + 2] = fdb;
			hdb = abuff_G[hi][wi];
			fdb = half_to_cbyte(hdb);
			_pbuff[(hi * (_exrc->width) + wi) * 4 + 0] = fdb;
			hdb = abuff_B[hi][wi];
			fdb = half_to_cbyte(hdb);
			_pbuff[(hi * (_exrc->width) + wi) * 4 + 1] = fdb;
		}
	}
	tb = (float)abuff_R[0][0];*/
	}
