#include "stdafx.h"
#include "atlbase.h"
#include "Helper.h"
#include <stdio.h>
#include <metahost.h>
#include <Shlwapi.h>
#include <vcclr.h>
#include <tchar.h>
#include <memory>

using namespace System::Diagnostics;

namespace SnkHelper
{

    Helper::Helper()
    {
    }

    System::String^ Helper::PublicKeyFromSnkFile(System::String ^pathToSnkFile)
    {
        CComPtr<ICLRMetaHost> pMetaHost = NULL;
        CComPtr<ICLRRuntimeInfo> pRuntimeInfo = NULL;
        CComPtr<ICLRStrongName> pClrStrongName = NULL;

        pin_ptr<const wchar_t> path = PtrToStringChars(pathToSnkFile);
        if (!PathFileExists(path))
            return "";
   
        HRESULT hr = CLRCreateInstance(CLSID_CLRMetaHost, IID_PPV_ARGS(&pMetaHost));
        if (FAILED(hr))
        {
            Debug::WriteLine("CLRCreateInstance failed"); 
            return "";
        }
        hr = pMetaHost->GetRuntime(L"v4.0.30319", IID_PPV_ARGS(&pRuntimeInfo));
        if (FAILED(hr))
        {
            Debug::WriteLine("pMetaHost->GetRuntime failed");
            return "";
        }
        hr = pRuntimeInfo->GetInterface(CLSID_CLRStrongName, IID_ICLRStrongName, (LPVOID*)&pClrStrongName);
        if (FAILED(hr))
        {
            Debug::WriteLine("pRuntimeInfo->GetInterface");
            return "";
        }

        LONG keyCountBytes = 0;
        std::unique_ptr<BYTE> pKey(LoadKeyInfo(path, &keyCountBytes));
        if (pKey == NULL)
            return "";
        BYTE* pPublicKeyBlob = NULL;
        ULONG dwLen = 0;
        hr = pClrStrongName->StrongNameGetPublicKey(NULL, pKey.get(), keyCountBytes, &pPublicKeyBlob, &dwLen);
        if (FAILED(hr))
        {
            Debug::WriteLine("pClrStrongName->StrongNameGetPublicKey");
            return "";
        }
        
        System::String ^str= GetPrintableBuffer(pPublicKeyBlob, dwLen);
        //pClrStrongName->StrongNameFreeBuffer(pPublicKeyBlob);
        return str->Substring(0,320);
    }

    BYTE* Helper::LoadKeyInfo(const wchar_t* path, long* pSize)
    {
        FILE* fh = NULL;
        if (0 != _wfopen_s(&fh, path, L"rb"))
            return NULL;
        fseek(fh, 0, SEEK_END);
        *pSize = ftell(fh);
        rewind(fh);
        long size = *pSize + 2;
        BYTE* pKey = new BYTE[size];
        ZeroMemory(pKey, size);
        
        fread(pKey, 1, *pSize, fh);
        fclose(fh);
        return pKey;
    }

    System::String ^ Helper::GetPrintableBuffer(BYTE* pData, DWORD dwLen)
    {
        static char syms[] = "0123456789abcdef";
        wchar_t* buffer = new wchar_t[(dwLen * 2)+2];
        ZeroMemory(buffer, (dwLen * 2) + 2);
        int idx = 0;
        for (int i = 0; i < (int)dwLen; i++)
        {
            BYTE value = *((BYTE*)(pData + i));
            wchar_t upperNibble = syms[((value >> 4) & 0xf)];
            wchar_t lowerNibble = syms[value & 0xf];
            buffer[idx++] = upperNibble;
            buffer[idx++] = lowerNibble;
        }
        System::String^ returnValue = gcnew System::String(buffer);
        delete[] buffer;
        return returnValue;
    }
}