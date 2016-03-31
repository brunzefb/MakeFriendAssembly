#pragma once
namespace SnkHelper
{
    public ref class Helper
    {
    public:
        Helper();
        static System::String^ PublicKeyFromSnkFile(System::String ^pathToSnkFile);
    private:
        static BYTE* LoadKeyInfo(const wchar_t* path, long* psize);
        static System::String ^ GetPrintableBuffer(BYTE* pData, DWORD dwLen);
    };
}
