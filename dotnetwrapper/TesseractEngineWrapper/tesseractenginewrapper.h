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

/**
This class has implemented in tessnet2
**/
__gc public class Character
{
public:
	// Value
	char Value;

	// confidence : 0 = perfect; 255 = reject, over 160 = bad reco
	// This value is sensitive to Tesseract variable tessedit_write_ratings and tessedit_zero_rejection
	// After many test I found tessedit_write_ratings=true give the best result, this is the default mode
	// in tessnet2, you can change this setting the variables before calling DoOCR
	Double Confidence;

	// Character location
	int Left, Right, Top, Bottom;

	// User object, you can assign the value you want
	Object* Tag;

	// Constructor
	Character(char character, int left, int top, int right, int bottom)
	{
		Value = character;
		Left = left; Top = top; Right = right; Bottom = bottom;
	}

	// Constructor
	Character(char character, double confidence, int left, int top, int right, int bottom)
	{
		Value = character;

		Confidence = confidence;

		Left = left; 
		Top = top; 
		Right = right; 
		Bottom = bottom;
	}
	// All other values are the same for the Word and all is characters (Confidence, FontIndex, PointSize, Formating...)
};


/**
This class has implemented in tessnet2
**/
__gc public class Word
{
public:
	// The line index for this word
	int LineIndex;

	// Blanks count
	int Blanks;

	// Word location
	int Left, Right, Top, Bottom;

	// Some value directly copied from tessract
	int FontIndex, PointSize, Formating;

	// The text
	String* Text;

	// confidence : 0 = perfect; 255 = reject, over 160 = bad reco
	// This value is sensitive to Tesseract variable tessedit_write_ratings and tessedit_zero_rejection
	// After many test I found tessedit_write_ratings=true give the best result, this is the default mode
	// in tessnet2, you can change this setting the variables before calling DoOCR
	Double Confidence;

	// User object, you can assign value you want
	Object* Tag;

	// Character position
	List<Character*>* CharList;

public:
	Word()
	{
		CharList = new List<Character*>(10);

		Left = System::Int32::MaxValue;
		Top = System::Int32::MaxValue;
		Right = System::Int32::MinValue;
		Bottom = System::Int32::MinValue;

		Text = "";
		Confidence = 0;
	}

	List<Word*>* UpdateConfidenceAndInsertTo(List<Word*>* wordList)
	{
		if (wordList == NULL)
			wordList = new List<Word*>();

		this->Confidence /= this->CharList->Count;
		wordList->Add(this);

		return wordList;
	}

public:
	String* ToString()
	{			
		return System::String::Format("{0} ({1})", Text->ToString(), Confidence.ToString());
	}
};

__gc public class TesseractProcessor
{	
private:
	String* _dataPath;
	String* _lang;
	int _ocrEngineMode;

private:
	bool _doMonitor;

public:	
	__property bool get_DoMonitor()
	{
		return _doMonitor;
	}

	__property void set_DoMonitor(bool doMonitor)
	{
		_doMonitor = doMonitor;
	}
	
private:
	System::IntPtr _apiInstance;
	System::IntPtr _monitorInstance;

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

private:
	void InitializeWorkingSpace();
	void InitializeEngineAPI();
	void InitializeMonitor();

	void InternalFinally();

public:
	String* Apply(String* filePath);
	String* Apply(Image* image);
	String* Apply(Image* image, int l, int t, int w, int h);	
	System::Collections::Generic::List<Word*>* RetriveResultDetail();

private:
	Pix* PixFromImage(Image* image);
	String* Process(Pix* pix);
	String* Process(TessBaseAPI* api, Pix* pix);
};

END_NAMESPACE