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


// ===============================================================
// CONTRUCTORS AND DESTRUCTORS
TesseractProcessor::TesseractProcessor()
{
	InitializeWorkingSpace();

	_doMonitor = true;
}

TesseractProcessor::~TesseractProcessor()
{
	InternalFinally();
}
// ===============================================================



// ===============================================================
// INITIALIZE CLASS
void TesseractProcessor::InitializeWorkingSpace()
{
	InitializeEngineAPI();

	InitializeMonitor();
}
	
void TesseractProcessor::InitializeEngineAPI()
{
	if (_apiInstance == null)
	{
	TessBaseAPI* api = new TessBaseAPI();

	if (api == NULL)
	{
		throw new System::Exception(
			"Failed to create TessBaseAPI instance!");
	}

	_apiInstance = api;
	}
	else
	{
		this->Clear();
	}
}

void TesseractProcessor::InitializeMonitor()
{	
	if (_monitorInstance == null)
	{
		int fixed_buffer_factor = 100;
		int n = 127;
		ETEXT_DESC* monitor = new ETEXT_DESC[fixed_buffer_factor*n];
		monitor[1].more_to_come = 127;
		monitor[1].count = 0;
		_monitorInstance = monitor;
	}
	else
	{
		ETEXT_DESC* monitor = (ETEXT_DESC*)_monitorInstance.ToPointer();
		monitor[1].count = 0;
	}
}

void TesseractProcessor::InternalFinally()
{
	if (_apiInstance != NULL)
	{
		TessBaseAPI* api = (TessBaseAPI*)_apiInstance.ToPointer();

		api->End();

		delete api;
		api = null;

		_apiInstance = NULL;
	}

	if (_monitorInstance != null)
	{
		ETEXT_DESC* monitor = (ETEXT_DESC*)_monitorInstance.ToPointer();
		delete monitor;
		monitor = null;
		_monitorInstance = null;
	}
}
// ===============================================================


// ===============================================================
// MAIN INITIALIZE TESSERACT AND WRAPPER MAIN METHODS
bool TesseractProcessor::Init()
{
	bool bSucced = false;

	if (_apiInstance != NULL)
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

	if (_apiInstance != NULL)
	{
		TessBaseAPI* api = (TessBaseAPI*)_apiInstance.ToPointer();
		bSucced = api->Init(
			Helper::StringToPointer(dataPath), 
			Helper::StringToPointer(lang),
			Helper::ParseOcrEngineMode(ocrEngineMode)) >= 0;
	}

	return bSucced;
}

String* TesseractProcessor::GetTesseractEngineVersion()
{
	if (_apiInstance != NULL)
	{
		TessBaseAPI* api = (TessBaseAPI*)_apiInstance.ToPointer();
		return Helper::PointerToString(api->Version());
	}

	return NULL;
}


void TesseractProcessor::Clear()
{
	if (_apiInstance != NULL)
	{
		TessBaseAPI* api = (TessBaseAPI*)_apiInstance.ToPointer();
		api->Clear();
	}
}

void TesseractProcessor::ClearResults()
{
	if (_apiInstance != NULL)
	{	
		this->Clear(); //should call _apiInstance->ClearResults(), but it is internal function.
	}
}

void TesseractProcessor::ClearAdaptiveClassifier()
{
	if (_apiInstance != NULL)
	{
		TessBaseAPI* api = (TessBaseAPI*)_apiInstance.ToPointer();
		api->ClearAdaptiveClassifier();
	}
}

void TesseractProcessor::End()
{
	if (_apiInstance != NULL)
	{
		TessBaseAPI* api = (TessBaseAPI*)_apiInstance.ToPointer();
		api->End();
	}
}
// ===============================================================



// ===============================================================
// GET/SET VARIABLES
bool TesseractProcessor::GetBoolVariable(System::String* name, bool __gc* value)
{
	if (_apiInstance == NULL)
		return false;

	TessBaseAPI* api = (TessBaseAPI*)_apiInstance.ToPointer();

	bool val = false;
	bool succeed = api->GetBoolVariable(Helper::StringToPointer(name), &val);
	*value = val;

	return succeed;
}

bool TesseractProcessor::GetIntVariable(System::String* name, int __gc* value)
{
	if (_apiInstance == NULL)
		return false;

	TessBaseAPI* api = (TessBaseAPI*)_apiInstance.ToPointer();

	int val = 0;
	bool succeed = api->GetIntVariable(Helper::StringToPointer(name), &val);
	*value = val;

	return succeed;
}

bool TesseractProcessor::GetDoubleVariable(System::String* name, double __gc* value)
{
	if (_apiInstance == NULL)
		return false;

	TessBaseAPI* api = (TessBaseAPI*)_apiInstance.ToPointer();

	double val = 0;
	bool succeed = api->GetDoubleVariable(Helper::StringToPointer(name), &val);
	*value = val;

	return succeed;
}

System::String* TesseractProcessor::GetStringVariable(System::String* name)
{
	if (_apiInstance == NULL)
		return false;

	TessBaseAPI* api = (TessBaseAPI*)_apiInstance.ToPointer();

	const char *value = api->GetStringVariable(Helper::StringToPointer(name));

	return Helper::PointerToString(value);
}

bool TesseractProcessor::SetVariable(System::String* name, System::String* value)
{
	if (_apiInstance == NULL)
		return false;

	TessBaseAPI* api = (TessBaseAPI*)_apiInstance.ToPointer();
	bool succeed = api->SetVariable(Helper::StringToPointer(name), Helper::StringToPointer(value));

	return succeed;
}
// ===============================================================







// ===============================================================


// ===============================================================





// ===============================================================
// PROCESS INTERFACE
BlockList* TesseractProcessor::DetectBlocks(TessBaseAPI* api, Pix* pix)
{
	int imageHeight = pix->h;

	api->SetImage(pix);
	BLOCK_LIST *blockList = api->FindLinesCreateBlockList();
	
	List<RectBound*> *bounds = new List<RectBound*>();

	BLOCK_IT b_it(blockList);
	int blockCount = 0;
	for (b_it.mark_cycle_pt(); !b_it.cycled_list(); b_it.forward())
	{
		BLOCK* block = b_it.data();
		if (block == null)
			continue;

		const TBOX *box = &block->bounding_box();
		int l = System::Math::Min(box->left(), box->right());
		int r = System::Math::Max(box->left(), box->right());
		int t = System::Math::Min(box->top(), box->bottom());
		int b = System::Math::Max(box->top(), box->bottom());

		bounds->Add(new RectBound(l, imageHeight - b, r, imageHeight - t));
		blockCount++;
	}

	System::IntPtr blockListIntPtr = blockList;
	BlockList* blks = new BlockList(blockListIntPtr, bounds, blockCount);

	return blks;
}

BlockList* TesseractProcessor::DetectBlocks(System::Drawing::Image* image)
{
	if (_apiInstance == null)
		return null;

	TessBaseAPI* api = (TessBaseAPI*)_apiInstance.ToPointer();
	Pix* pix = NULL;
	BlockList* blks = null;

	try
	{
		pix = PixFromImage(image);
		blks = DetectBlocks(api, pix);
	}
	catch (System::Exception* exp)
	{
		if (blks != null)
		{
			blks->DeleteRawData();
			blks = null;
		}

		throw exp;
	}
	__finally
	{
		api = null;
		if (pix != null)
		{
			pixDestroy(&pix);
			pix = null;
		}
	}

	return blks;
}

String* TesseractProcessor::Apply(String* filePath)
{
	if (_apiInstance == null)
		return null;


	TessBaseAPI* api = (TessBaseAPI*)_apiInstance.ToPointer();

	String* result = "";

	STRING text_out;
	bool succed = api->ProcessPages(
		Helper::StringToPointer(filePath), null, 0, &text_out);

	result = new String(text_out.string());

	return result;
}

String* TesseractProcessor::Apply(System::Drawing::Image* image)
{
	if (_apiInstance == null || image == null)
		return null;

	String* result = "";
	Pix* pix = null;

	try
	{
		pix = this->PixFromImage(image);

		TessBaseAPI* api = (TessBaseAPI*)_apiInstance.ToPointer();
		result = this->Process(api, pix);
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
	if (image == null)
		throw new System::Exception("Input is invalid!");

	if (l != 0 || t != 0 || w != image->Width || image->Height)
	{
		throw new System::Exception("Please crop image and try to call another method!");
	}

	String* result = "";
	return result;
}

String* TesseractProcessor::Process(Pix* pix)
{
	TessBaseAPI* api = (TessBaseAPI*)_apiInstance.ToPointer();
	return this->Process(api, pix);
}

String* TesseractProcessor::Process(TessBaseAPI* api, Pix* pix)
{
	if (api == null || pix == null)
		return null;

	this->InitializeMonitor();
	ETEXT_DESC* monitor = (_doMonitor ? (ETEXT_DESC*)_monitorInstance.ToPointer() : null);

	api->SetImage(pix);
	bool succed = api->Recognize(monitor) >= 0;

	char* text = api->GetUTF8Text();

	String* result = new String(text);

	delete text;
	
	return result;
}



System::Collections::Generic::List<Word*>* TesseractProcessor::RetriveResultDetail()
{
	if (!_doMonitor || _monitorInstance == null)
		return null;
	
	System::Collections::Generic::List<Word*>* wordList = null;

	ETEXT_DESC* monitor = null;
	ETEXT_DESC* head = null;
	Word* currentWord = null;

	try
	{
		monitor = (ETEXT_DESC*)_monitorInstance.ToPointer();
		head = &monitor[1];

		int lineIndex=0;		
		int lineIdx = 0;
		int nChars = head->count;
		int i = 0;
		while (i < nChars)
		{
			EANYCODE_CHAR* ch = &(head + i)->text[0];

			if (ch->blanks > 0)
			{   /*new word condition meets*/
				if (currentWord != null)
					wordList = currentWord->UpdateConfidenceAndInsertTo(wordList);

				currentWord = null; // reset current word
			}

			if (currentWord != null && 
				(ch->left <= currentWord->Left || ch->top >= currentWord->Bottom))				
			{	/*new line condition meets*/
				wordList = currentWord->UpdateConfidenceAndInsertTo(wordList);

				lineIdx++;

				currentWord = null; // reset current word
			}

			if (currentWord == null)
			{   /*create new word*/
				currentWord = new Word();

				currentWord->LineIndex = lineIdx;

				currentWord->FontIndex = ch->font_index;
				currentWord->PointSize = ch->point_size;
				currentWord->Formating = ch->formatting;
			}

			Character* c = new Character(
				static_cast<unsigned char>(ch->char_code), 
				ch->confidence,
				ch->left, ch->top, ch->right, ch->bottom);

			/* update current word */
			currentWord->CharList->Add(c);

			currentWord->Text = System::String::Format(
				"{0}{1}", currentWord->Text->ToString(), c->Value.ToString());

			currentWord->Left = Math::Min(currentWord->Left, (int)ch->left);
			currentWord->Top = Math::Min(currentWord->Top, (int)ch->top);
			currentWord->Right = Math::Max(currentWord->Right, (int)ch->right);
			currentWord->Bottom = Math::Max(currentWord->Bottom, (int)ch->bottom);

			currentWord->Confidence += ch->confidence;
			
			i++; /*go to next char*/
		} /* end while */

		if (currentWord != null)
			wordList = currentWord->UpdateConfidenceAndInsertTo(wordList);
	}
	catch (System::Exception* exp)
	{
		throw exp;
	}
	__finally
	{
		currentWord = null;
		head = null;
		monitor = null;
	}

	return wordList;
}
// ===============================================================


// ===============================================================
// HELPERS
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




// ===============================================================
























// ===============================================================

// ===============================================================

END_NAMESPACE