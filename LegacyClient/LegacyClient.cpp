
#include <windows.h>
#include <atlbase.h>
#include <comdef.h>

#include <cmath>
#include <iostream>
#include <string>
#include <memory>
#include <stdexcept>

#define cimg_display 0
#include "CImg.h"

#import "ScriptHost.tlb" raw_interfaces_only  

using ScriptHost::IScriptRunner;
using ScriptHost::ScriptRunner;
using ScriptHost::IScriptParams;
using ScriptHost::ScriptParams;

struct COMInit
{
	COMInit() { CoInitialize(NULL); }
	COMInit(const COMInit&) = delete;
	COMInit& operator=(const COMInit&) = delete;
	~COMInit() { CoUninitialize(); }
};

class CheckedHRESULT final
{
public:
	CheckedHRESULT() : m_hr(S_OK) {}
	CheckedHRESULT(HRESULT hr) : m_hr(hr)
	{
		check();
	}
	CheckedHRESULT& operator=(HRESULT hr)
	{
		m_hr = hr;
		check();
		return *this;
	}
	operator HRESULT() const { return m_hr; }

private:
	void check()
	{
		if (!SUCCEEDED(m_hr))
		{
			IErrorInfo* pErrorInfo;
			GetErrorInfo(NULL, &pErrorInfo);
			throw _com_error(m_hr, pErrorInfo);
		}
	}

	HRESULT m_hr;
};


_bstr_t operator "" _bstr(const char* szValue, size_t)
{ 
	return _bstr_t{ szValue };
}


int main(int argc, char** argv)
{
	COMInit com;
	try
	{
		CComPtr<IScriptRunner> cpPluginHost{};
		auto clsidPluginHost = __uuidof(ScriptRunner);
		CheckedHRESULT hr{ cpPluginHost.CoCreateInstance(clsidPluginHost, NULL, CLSCTX_ALL) };

		hr = cpPluginHost->LoadScript("../Script.cs"_bstr, "RunScript"_bstr);

		CComPtr<IScriptParams> cpScriptParams{};
		auto clsidScriptParams = __uuidof(ScriptParams);
		hr = cpScriptParams.CoCreateInstance(clsidScriptParams, NULL, CLSCTX_ALL);

		cpScriptParams->SetParam("OutDir"_bstr, "./"_bstr);
		cpScriptParams->SetParam("FilterSize"_bstr, "21"_bstr);

		cimg_library::CImg<unsigned char> imgBird{ "Bird.bmp" };
		auto image = imgBird.get_shared_channel(0U);
		hr = cpScriptParams->SetImage("WorkImage"_bstr, image.width(), image.height(), image.width(), (long long)image.begin());

		hr = cpPluginHost->Execute(cpScriptParams);

		_bstr_t bstrElapsed;
		hr = cpScriptParams->GetResult("Elapsed"_bstr, bstrElapsed.GetAddress());

		std::cout << "Raw script execution took " << bstrElapsed << " seconds." << std::endl;

		image.save_bmp("Processed.bmp");

		return 0;
	}
	catch (_com_error& exc)
	{
		std::cout << exc.ErrorMessage() << " (" << exc.Description() << ")" << std::endl;
		return exc.Error();
	}

}


