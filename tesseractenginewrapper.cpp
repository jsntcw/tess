/**
Copyright 2011, Cong Nguyen

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
**/

#include <windows.h>
#include "tesseractenginewrapper.h"
#include "..\api\baseapi.h"
#include "..\cutil\callcpp.h"
#include "..\wordrec\chop.h"
#include "..\ccmain\tessedit.h"
#include "..\ccmain\tessvars.h"
#include "..\classify\baseline.h"
#include "..\classify\mfoutline.h"
#include "..\classify\normmatch.h"
#include "..\classify\intmatcher.h"
#include "..\classify\speckle.h"
#include "..\dict\permute.h"
#include "..\ccstruct\publictypes.h"
#include "leptprotos.h"


BEGIN_NAMSPACE

USING_COMMON_SYSTEM

#define null NULL

TessBaseAPI* _api = NULL;
ETEXT_DESC* _monitor = NULL;

// ===============================================================
TesseractProcessor::TesseractProcessor()
{
	_api = new TessBaseAPI();

	if (_api == NULL)
	{
		throw new System::Exception(
			"Failed to create TessBaseAPI instance!");
	}
}

TesseractProcessor::~TesseractProcessor()
{
	if (_api != NULL)
	{
		_api->End();

		delete _api;

		_api = NULL;
	}

	if (_monitor != null)
	{
		delete _monitor;
		_monitor = null;
	}
}
// ===============================================================

// ===============================================================
bool TesseractProcessor::Init()
{
	bool bSucced = false;

	if (_api != NULL)
	{
		bSucced = this->Init(NULL, "eng", 3);
	}

	return bSucced;
}

bool TesseractProcessor::Init(String* dataPath, String* lang, int ocrEngineMode)
{
	bool bSucced = false;

	_dataPath = dataPath;
	_lang = lang;
	_ocrEngineMode = ocrEngineMode;

	if (_api != NULL)
	{
		bSucced = _api->Init(
			Helper::StringToPointer(dataPath), 
			Helper::StringToPointer(lang),
			Helper::ParseOcrEngineMode(ocrEngineMode)) >= 0;
	}

	return bSucced;
}

String* TesseractProcessor::GetTesseractEngineVersion()
{
	if (_api != NULL)
	{
		return Helper::PointerToString(_api->Version());
	}

	return NULL;
}


void TesseractProcessor::Clear()
{
	if (_api != NULL)
	{
		_api->Clear();
	}
}

void TesseractProcessor::ClearResults()
{
	if (_api != NULL)
	{		
		_api->Clear(); //should call _api->ClearResults(), but it is internal function.
	}
}

void TesseractProcessor::ClearAdaptiveClassifier()
{
	if (_api != NULL)
	{
		_api->ClearAdaptiveClassifier();
	}
}

void TesseractProcessor::End()
{
	if (_api != NULL)
	{
		_api->End();
	}
}
// ===============================================================

// ===============================================================
String* TesseractProcessor::Apply(String* filePath)
{
	if (_api == null)
		return null;

	String* result = "";

	STRING text_out;
	bool succed = _api->ProcessPages(
		Helper::StringToPointer(filePath), null, 0, &text_out);

	result = new String(text_out.string());

	return result;
}

String* TesseractProcessor::Apply(System::Drawing::Image* image)
{
	if (_api == null || image == null)
		return null;

	String* result = "";
	Pix* pix = null;

	try
	{
		pix = this->PixFromImage(image);

		result = this->Process(_api, pix);
	}
	catch (System::Exception* exp)
	{
		throw exp;
	}
	__finally
	{
		if (pix != null)
		{
			pixDestroy(&pix);
			pix = null;
		}
	}

	return result;
}

String* TesseractProcessor::Apply(System::Drawing::Image* image, int l, int t, int w, int h)
{
	String* result = "";
	return result;
}

// ===============================================================


// ===============================================================
Pix* TesseractProcessor::PixFromImage(System::Drawing::Image* image)
{
	Pix* pix = NULL;

	MemoryStream* mmsTmp = NULL;
	unsigned char srcTmp __gc[] = NULL;
	unsigned char* dstTmp = NULL;

	try
	{		
		/**
		Use memeory stream is easy solution, but poor effective.
		**/
		mmsTmp = new MemoryStream();
		image->Save(mmsTmp, System::Drawing::Imaging::ImageFormat::Tiff);

		int length = mmsTmp->Length;

		srcTmp = mmsTmp->GetBuffer();
		dstTmp = new unsigned char[length];
		System::Runtime::InteropServices::Marshal::Copy(srcTmp, 0, dstTmp, length);
		
		pix = pixReadMem(dstTmp, length);		
	}
	catch (Exception* exp)
	{
		throw exp;
	}
	__finally
	{
		if (mmsTmp != NULL)
		{
			mmsTmp->Close();
			mmsTmp = NULL;
		}

		if (srcTmp != NULL)
		{
			delete srcTmp;
			srcTmp = NULL;
		}

		if (dstTmp != NULL)
		{
			delete dstTmp;
			dstTmp = NULL;
		}
	}

	return pix;
}


String* TesseractProcessor::Process(Pix* pix)
{
	return this->Process(_api, pix);
}

String* TesseractProcessor::Process(TessBaseAPI* api, Pix* pix)
{
	if (api == null || pix == null)
		return null;


	if (_monitor != null)
	{
		delete _monitor;
		_monitor = null;
	}

	_monitor = new ETEXT_DESC();

	_api->SetImage(pix);
	bool succed = _api->Recognize(_monitor) >= 0;

	int count = _monitor->count; // count is always zero???????

	char* text = _api->GetUTF8Text();

	String* result = new String(text);

	delete text;
	
	return result;
}

// ===============================================================

// ===============================================================

// ===============================================================

END_NAMESPACE