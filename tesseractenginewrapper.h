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

#pragma once

#define BEGIN_NAMSPACE \
	namespace tesseract {

#define END_NAMESPACE		\
	}

#define USING_TESSERACT_ENGINE \
	using namespace tesseract;

#define USING_COMMON_SYSTEM \
	using namespace System; \
	using namespace System::IO; \
	using namespace System::Text; \
	using namespace System::Drawing; \
	using namespace System::Collections; \
	using namespace System::Collections::Generic;

#include "..\api\baseapi.h"
#include "..\ccstruct\ocrblock.h"
#include "..\ccutil\ocrclass.h"
#include "..\ccstruct\pageres.h"

BEGIN_NAMSPACE

USING_COMMON_SYSTEM

class Helper
{
public:
	static char* StringToPointer(String* s)
	{
		if (String::IsNullOrEmpty(s))
			return NULL;

		char* str = (char*)(System::Runtime::InteropServices::Marshal::StringToHGlobalAnsi(s).ToPointer());

		return str;
	}

	static String* PointerToString(const char* str)
	{
		if (str == NULL)
			return NULL;

		return new String(str);
	}

	static OcrEngineMode ParseOcrEngineMode(int oem)
	{
		switch (oem)
		{
		case 0:
			return OcrEngineMode::OEM_TESSERACT_ONLY;
		case 1:
			return OcrEngineMode::OEM_CUBE_ONLY;
		case 2:
			return OcrEngineMode::OEM_TESSERACT_CUBE_COMBINED;
		case 3:
			return OcrEngineMode::OEM_DEFAULT;
		default:
			break;
		}

		return OcrEngineMode::OEM_DEFAULT;
	}
};

__gc public class TesseractProcessor
{	
private:
	String* _dataPath;
	String* _lang;
	int _ocrEngineMode;

public:	
	TesseractProcessor();
	~TesseractProcessor();

public:
	String* GetTesseractEngineVersion();

	bool Init();
	bool Init(String* dataPath, String* lang, int ocrEngineMode);

	void Clear();
	void ClearResults();
	void ClearAdaptiveClassifier();

	void End();
	
public:
	String* Apply(String* filePath);
	String* Apply(Image* image);
	String* Apply(Image* image, int l, int t, int w, int h);	

private:
	Pix* PixFromImage(Image* image);
	String* Process(Pix* pix);
	String* Process(TessBaseAPI* api, Pix* pix);
};

END_NAMESPACE