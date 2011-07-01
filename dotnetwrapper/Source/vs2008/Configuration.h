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

#define BEGIN_NAMSPACE	namespace	OCR {	namespace TesseractWrapper {

#define END_NAMESPACE	}; };

#define USING_TESSERACT using namespace tesseract;

#define USING_TESSERACT_ENGINE_WRAPPER using namespace OCR::TesseractWrapper;

#define USING_COMMON_SYSTEM \
	using namespace System; \
	using namespace System::IO; \
	using namespace System::Text; \
	using namespace System::Drawing; \
	using namespace System::Collections; \
	using namespace System::Collections::Generic;

#define NULL 0
#define null NULL

#define SAFE_DELETE(p) if (p) { delete p; p = null; } 