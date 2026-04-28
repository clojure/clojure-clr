#include <windows.h>
#include <iostream>
#include <string>
#include <cassert>

#include "nethost.h"
#include "hostfxr.h"
#include "coreclr_delegates.h"

#pragma comment(lib, "nethost.lib")

using string_t = std::wstring;

typedef int (CORECLR_DELEGATE_CALLTYPE* eval_clojure_fn)(void*, int);

hostfxr_initialize_for_runtime_config_fn init_fptr;
hostfxr_get_runtime_delegate_fn get_delegate_fptr;
hostfxr_close_fn close_fptr;

void load_hostfxr()
{
    char_t path[MAX_PATH];
    size_t size = sizeof(path) / sizeof(char_t);

    if (get_hostfxr_path(path, &size, nullptr) != 0)
    {
        std::cerr << "Failed to locate hostfxr\n";
        exit(1);
    }

    HMODULE lib = LoadLibraryW(path);
    init_fptr = (hostfxr_initialize_for_runtime_config_fn)GetProcAddress(lib, "hostfxr_initialize_for_runtime_config");
    get_delegate_fptr = (hostfxr_get_runtime_delegate_fn)GetProcAddress(lib, "hostfxr_get_runtime_delegate");
    close_fptr = (hostfxr_close_fn)GetProcAddress(lib, "hostfxr_close");
}

load_assembly_and_get_function_pointer_fn init_runtime(const string_t& config)
{
    load_hostfxr();

    hostfxr_handle cxt = nullptr;
    HRESULT hr = init_fptr(config.c_str(), nullptr, &cxt);
    if (FAILED(hr))
    {
        std::cerr << "Failed to init runtime\n";
        exit(1);
    }

    load_assembly_and_get_function_pointer_fn loader = nullptr;
    hr = get_delegate_fptr(cxt, hdt_load_assembly_and_get_function_pointer, (void**)&loader);
    if (FAILED(hr))
    {
        std::cerr << "Failed to get loader\n";
        exit(1);
    }

    close_fptr(cxt);
    return loader;
}

int wmain()
{
    // Build paths relative to the directory containing this executable
    wchar_t exePath[MAX_PATH];
    GetModuleFileNameW(nullptr, exePath, MAX_PATH);
    string_t exeDir = exePath;
    exeDir = exeDir.substr(0, exeDir.find_last_of(L"\\/") + 1);

    string_t config = exeDir + L"ClrApp.runtimeconfig.json";
    string_t assembly = exeDir + L"ClrApp.dll";
    string_t typeName = L"ClrApp.ClrApp, ClrApp";
    string_t methodName = L"EvalClojure";

    auto loader = init_runtime(config);

    eval_clojure_fn evalFn = nullptr;
    HRESULT hr = loader(
        assembly.c_str(),
        typeName.c_str(),
        methodName.c_str(),
        UNMANAGEDCALLERSONLY_METHOD,
        nullptr,
        (void**)&evalFn);

    if (FAILED(hr))
    {
        std::cerr << "Failed to load EvalClojure\n";
        return 1;
    }

    const char* expr = "(+ 1 2 3 4)";
    int len = (int)strlen(expr);

    int result = evalFn((void*)expr, len);
    std::cout << "Managed returned: " << result << "\n";

    return 0;
}
