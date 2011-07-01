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

#include "Configuration.h"

#include <windows.h>
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

USING_TESSERACT

class Helper
{
public:
	Helper();
	~Helper();

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
};






class PixConverter
{
public:
	static Pix* PixFromImage(System::Drawing::Image* image)
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
};






class TesseractConverter
{
public:
	static OcrEngineMode ParseOcrEngineMode(int oem)
	{
		return (OcrEngineMode)oem;
		/*switch (oem)
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

		return OcrEngineMode::OEM_DEFAULT;*/
	}

	static PageIteratorLevel ParsePageLevel(int pageLevel)
	{
		return (PageIteratorLevel)pageLevel;
		/*switch (pageLevel)
		{
		case 0:
			return PageIteratorLevel::RIL_BLOCK;
		case 1:
			return PageIteratorLevel::RIL_PARA;
		case 2:
			return PageIteratorLevel::RIL_TEXTLINE;
		case 3:
			return PageIteratorLevel::RIL_WORD;
		case 4:
			return PageIteratorLevel::RIL_SYMBOL;
		default:
			break;
		}

		return PageIteratorLevel::RIL_SYMBOL;*/
	}

	static PageSegMode ParsePageSegMode(int psm)
	{
		return (PageSegMode)psm;
	}
};

END_NAMESPACE
