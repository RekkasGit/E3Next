
#include <iostream>
#include <chrono>
#include <mono/metadata/assembly.h>
#include <mono/jit/jit.h>
#include <mono/metadata/debug-helpers.h>
#include <direct.h>
#include <thread> 
#include <map>
#include <filesystem>
#include <deque>
#include <string_view>

#define GetCurrentDir _getcwd

void InitE3();
void UnloadE3();
MonoDomain* _rootDomain;
bool ShowMQ2MonoWindow = true;
std::string monoDir;
bool initialized = false;
std::string currentDirectory;

#pragma region string_methods
struct ci_less
{
	struct nocase_compare
	{
		bool operator() (const unsigned char& c1, const unsigned char& c2) const noexcept
		{
			if (c1 == c2)
				return false;
			return ::tolower(c1) < ::tolower(c2);
		}
	};

	struct nocase_equals
	{
		bool operator() (const unsigned char& c1, const unsigned char& c2) const noexcept
		{
			if (c1 == c2)
				return true;

			return ::tolower(c1) == ::tolower(c2);
		}
	};

	struct nocase_equals_w
	{
		bool operator() (const wchar_t& c1, const wchar_t& c2) const noexcept
		{
			if (c1 == c2)
				return true;

			return ::towlower(c1) == ::towlower(c2);
		}
	};

	bool operator()(std::string_view s1, std::string_view s2) const noexcept
	{
		return std::lexicographical_compare(
			s1.begin(), s1.end(),
			s2.begin(), s2.end(),
			nocase_compare());
	}

	using is_transparent = void;
};
inline int find_substr(std::string_view haystack, std::string_view needle)
{
	auto iter = std::search(std::begin(haystack), std::end(haystack),
		std::begin(needle), std::end(needle));
	if (iter == std::end(haystack)) return -1;
	return static_cast<int>(iter - std::begin(haystack));
}

inline int ci_find_substr(std::string_view haystack, std::string_view needle)
{
	auto iter = std::search(std::begin(haystack), std::end(haystack),
		std::begin(needle), std::end(needle), ci_less::nocase_equals());
	if (iter == std::end(haystack)) return -1;
	return static_cast<int>(iter - std::begin(haystack));
}

inline int ci_find_substr_w(std::wstring_view haystack, std::wstring_view needle)
{
	auto iter = std::search(std::begin(haystack), std::end(haystack),
		std::begin(needle), std::end(needle), ci_less::nocase_equals_w());
	if (iter == std::end(haystack)) return -1;
	return static_cast<int>(iter - std::begin(haystack));
}

inline bool ci_equals(std::string_view sv1, std::string_view sv2)
{
	return sv1.size() == sv2.size()
		&& std::equal(sv1.begin(), sv1.end(), sv2.begin(), ci_less::nocase_equals());
}
inline bool ci_equals(std::wstring_view sv1, std::wstring_view sv2)
{
	return sv1.size() == sv2.size()
		&& std::equal(sv1.begin(), sv1.end(), sv2.begin(), ci_less::nocase_equals_w());
}

inline bool ci_equals(std::string_view haystack, std::string_view needle, bool isExact)
{
	if (isExact)
		return ci_equals(haystack, needle);

	return ci_find_substr(haystack, needle) != -1;
}

inline bool string_equals(std::string_view sv1, std::string_view sv2)
{
	return sv1.size() == sv2.size()
		&& std::equal(sv1.begin(), sv1.end(), sv2.begin());
}

inline bool starts_with(std::string_view a, std::string_view b)
{
	if (a.length() < b.length())
		return false;

	return a.substr(0, b.length()).compare(b) == 0;
}

inline bool ci_starts_with(std::string_view a, std::string_view b)
{
	if (a.length() < b.length())
		return false;

	return ci_equals(a.substr(0, b.length()), b);
}

inline bool ends_with(std::string_view a, std::string_view b)
{
	if (a.length() < b.length())
		return false;

	return a.substr(a.length() - b.length()).compare(b) == 0;
}

inline bool ci_ends_with(std::string_view a, std::string_view b)
{
	if (a.length() < b.length())
		return false;

	return ci_equals(a.substr(a.length() - b.length()), b);
}
#pragma endregion string_methods

struct monoAppDomainInfo
{
	//app domain we have created for e3
	MonoDomain* m_appDomain = nullptr;
	//core.dll information so we can bind to it
	MonoAssembly* m_csharpAssembly = nullptr;
	MonoImage* m_coreAssemblyImage = nullptr;
	MonoClass* m_classInfo = nullptr;
	MonoObject* m_classInstance = nullptr;
	//methods that we call in C# if they are available
	MonoMethod* m_OnPulseMethod = nullptr;
	MonoMethod* m_OnWriteChatColor = nullptr;
	MonoMethod* m_OnIncomingChat = nullptr;
	MonoMethod* m_OnInit = nullptr;
	MonoMethod* m_OnUpdateImGui = nullptr;
	std::map<std::string, bool> m_IMGUI_OpenWindows;
	std::map<std::string, bool> m_IMGUI_CheckboxValues;
	std::map<std::string, bool> m_IMGUI_RadioButtonValues;
	std::string m_CurrentWindow;
	bool m_IMGUI_Open = true;
	int m_delayTime = 0;//amount of time in milliseonds that was set by C#
	std::chrono::steady_clock::time_point m_delayTimer = std::chrono::steady_clock::now(); //the time this was issued + m_delayTime
};
std::map<std::string, monoAppDomainInfo> mqAppDomains;
std::map<MonoDomain*, std::string> mqAppDomainPtrToString;
std::deque<std::string> appDomainProcessQueue;

void mono_Echo(MonoString* string)
{
	char* cppString = mono_string_to_utf8(string);

	std::cout << cppString << std::endl;

	mono_free(cppString);
}
static MonoString* mono_ParseTLO(MonoString* text)
{
	char buffer[2048] = { 0 };
	char* cppString = mono_string_to_utf8(text);
	std::string str(cppString);
	strncpy_s(buffer, str.c_str(), sizeof(buffer));
	//auto old_parser = std::exchange(gParserVersion, 2);
	//ParseMacroData(buffer, sizeof(buffer));
	//gParserVersion = old_parser;
	mono_free(cppString);
	//return buffer;
	return mono_string_new_wrapper(buffer);
}

static void mono_DoCommand(MonoString* text)
{
	char* cppString = mono_string_to_utf8(text);
	std::string str(cppString);
	std::cout << "Command executing:"<< cppString;
	mono_free(cppString);
	

}
static void mono_Delay(int milliseconds)
{
	MonoDomain* currentDomain = mono_domain_get();
	if (currentDomain)
	{
		std::string key = mqAppDomainPtrToString[currentDomain];
		//pointer to the value in the map
		auto& domainInfo = mqAppDomains[key];
		//do domnain lookup via its pointer
		domainInfo.m_delayTimer = std::chrono::steady_clock::now() + std::chrono::milliseconds(milliseconds);
		domainInfo.m_delayTime = milliseconds;
	}

}
static void mono_ImGUI_Begin_OpenFlagSet(MonoString* name, bool open)
{
	char* cppString = mono_string_to_utf8(name);
	std::string str(cppString);
	mono_free(cppString);

	MonoDomain* currentDomain = mono_domain_get();

	if (currentDomain)
	{
		std::string key = mqAppDomainPtrToString[currentDomain];
		//pointer to the value in the map
		auto& domainInfo = mqAppDomains[key];
		if (domainInfo.m_IMGUI_OpenWindows.find(str) == domainInfo.m_IMGUI_OpenWindows.end())
		{
			//key doesn't exist, add it
			domainInfo.m_IMGUI_OpenWindows[str] = true;
		}
		domainInfo.m_IMGUI_OpenWindows[str] = open;
		//put updates back

	}



}
static bool mono_ImGUI_Begin_OpenFlagGet(MonoString* name)
{
	
	char* cppString = mono_string_to_utf8(name);
	std::string str(cppString);
	mono_free(cppString);
	MonoDomain* currentDomain = mono_domain_get();

	if (currentDomain)
	{
		std::string key = mqAppDomainPtrToString[currentDomain];
		//pointer to the value in the map
		auto& domainInfo = mqAppDomains[key];
		if (domainInfo.m_IMGUI_OpenWindows.find(str) == domainInfo.m_IMGUI_OpenWindows.end())
		{
			//key doesn't exist, add it
			domainInfo.m_IMGUI_OpenWindows[str] = true;
		}
		return domainInfo.m_IMGUI_OpenWindows[str];
	}
	return false;

}
//define methods exposde to the plugin to be executed
static bool mono_ImGUI_Begin(MonoString* name, int flags)
{

	char* cppString = mono_string_to_utf8(name);
	std::string str(cppString);
	mono_free(cppString);
	MonoDomain* currentDomain = mono_domain_get();

	if (currentDomain)
	{
		std::string key = mqAppDomainPtrToString[currentDomain];
		//pointer to the value in the map
		auto& domainInfo = mqAppDomains[key];

		domainInfo.m_CurrentWindow = str;
		if (domainInfo.m_IMGUI_OpenWindows.find(str) == domainInfo.m_IMGUI_OpenWindows.end())
		{
			//key doesn't exist, add it
			domainInfo.m_IMGUI_OpenWindows[str] = true;
		}

		//return ImGui::Begin(str.c_str(), &domainInfo.m_IMGUI_OpenWindows[str], flags);
	}
	return false;
}


static bool mono_ImGUI_Button(MonoString* name)
{
	return false;
	/*char* cppString = mono_string_to_utf8(name);
	std::string str(cppString);
	mono_free(cppString);
	return ImGui::Button(str.c_str());*/
}

static void mono_ImGUI_End()
{
	//ImGui::End();
}
std::string get_current_dir() {
	char buff[FILENAME_MAX]; //create string buffer to hold path
	GetCurrentDir(buff, FILENAME_MAX);
	std::string current_working_dir(buff);
	return current_working_dir;
}



void InitMono()
{
	std::string str("EmbeddingMono\\");
	currentDirectory=get_current_dir();
	std::size_t found = currentDirectory.find(str);
	//Indicate Mono where you installed the lib and etc folders
	std::cout << "CurrentDirectory:" << currentDirectory << std::endl;

	if (found != std::string::npos)
	{
		currentDirectory.erase(found + 13);
	}
	//Indicate Mono where you installed the lib and etc folders
	std::cout << "CurrentDirectory:" << currentDirectory << std::endl;


	
	mono_set_dirs((currentDirectory + "\\Mono\\lib").c_str(), (currentDirectory + "\\Mono\\etc").c_str());

	

	mono_set_assemblies_path((currentDirectory + "\\Mono\\lib").c_str());
	_rootDomain = mono_jit_init("Mono_Domain");
	mono_domain_set(_rootDomain, false);

	//Namespace.Class::Method + a Function pointer with the actual definition
	//the namespace/class binding too is hard coded to namespace: MonoCore
	//Class: Core
	mono_add_internal_call("MonoCore.Core::mq_Echo", &mono_Echo);
	mono_add_internal_call("MonoCore.Core::mq_ParseTLO", &mono_ParseTLO);
	mono_add_internal_call("MonoCore.Core::mq_DoCommand", &mono_DoCommand);
	mono_add_internal_call("MonoCore.Core::mq_Delay", &mono_Delay);



	//I'm GUI stuff
	mono_add_internal_call("MonoCore.Core::imgui_Begin", &mono_ImGUI_Begin);
	mono_add_internal_call("MonoCore.Core::imgui_Button", &mono_ImGUI_Button);
	mono_add_internal_call("MonoCore.Core::imgui_End", &mono_ImGUI_End);
	mono_add_internal_call("MonoCore.Core::imgui_Begin_OpenFlagSet", &mono_ImGUI_Begin_OpenFlagSet);
	mono_add_internal_call("MonoCore.Core::imgui_Begin_OpenFlagGet", &mono_ImGUI_Begin_OpenFlagGet);

	initialized = true;

	InitE3();

}
void UnloadE3()
{
	std::string appDomainName("E3");

	MonoDomain* domainToUnload = nullptr;
	//check to see if its registered, if so update ptr
	if (mqAppDomains.count(appDomainName) > 0)
	{
		domainToUnload = mqAppDomains[appDomainName].m_appDomain;
	}
	//verify its not the root domain and this is a valid domain pointer
	if (domainToUnload && domainToUnload != mono_get_root_domain())
	{
		mqAppDomains.erase(appDomainName);
		mqAppDomainPtrToString.erase(domainToUnload);

		mono_domain_set(mono_get_root_domain(), false);
		//mono_thread_pop_appdomain_ref();
		mono_domain_unload(domainToUnload);

		//remove from the process queue
		int count = 0;
		int processCount = appDomainProcessQueue.size();

		while (count < processCount)
		{
			count++;
			std::string currentKey = appDomainProcessQueue.front();
			appDomainProcessQueue.pop_front();
			if (!ci_equals(currentKey, appDomainName))
			{
				appDomainProcessQueue.push_back(currentKey);
			}
		}

	}

}
void InitE3()
{
	

	UnloadE3();
	std::string appDomainName("E3");

	//app domain we have created for e3
	MonoDomain* appDomain;
	appDomain = mono_domain_create_appdomain((char*)appDomainName.c_str(), nullptr);


	//core.dll information so we can bind to it
	MonoAssembly* csharpAssembly;
	MonoImage* coreAssemblyImage;
	MonoClass* classInfo;
	MonoObject* classInstance;
	//methods that we call in C# if they are available
	MonoMethod* OnPulseMethod;
	MonoMethod* OnWriteChatColor;
	MonoMethod* OnIncomingChat;
	MonoMethod* OnInit;
	MonoMethod* OnUpdateImGui;
	std::map<std::string, bool> IMGUI_OpenWindows;

	//everything below needs to be moved out to a per application run
	mono_domain_set(appDomain, false);



	std::string fileName = "Core.dll";
	std::string assemblypath = (currentDirectory + "\\E3Core\\bin\\Debug\\");

	bool filepathExists = std::filesystem::exists(assemblypath+fileName);

	if (!filepathExists)
	{
		return;
	}
	std::string shadowDirectory = assemblypath + "Shadowvine\\";
	namespace fs = std::filesystem;
	if (!fs::is_directory(shadowDirectory) || !fs::exists(shadowDirectory)) { // Check if src folder exists
		fs::create_directory(shadowDirectory); // create src folder
	}

	//copy it to a new directory
	std::filesystem::copy(assemblypath, shadowDirectory, std::filesystem::copy_options::overwrite_existing);

	csharpAssembly = mono_domain_assembly_open(appDomain, (shadowDirectory+fileName).c_str());

	if (!csharpAssembly)
	{
		initialized = false;
		//Error detected
		return;
	}
	coreAssemblyImage = mono_assembly_get_image(csharpAssembly);
	classInfo = mono_class_from_name(coreAssemblyImage, "MonoCore", "Core");
	classInstance = mono_object_new(appDomain, classInfo);
	OnPulseMethod = mono_class_get_method_from_name(classInfo, "OnPulse", 0);
	OnWriteChatColor = mono_class_get_method_from_name(classInfo, "OnWriteChatColor", 1);
	OnIncomingChat = mono_class_get_method_from_name(classInfo, "OnIncomingChat", 1);
	OnInit = mono_class_get_method_from_name(classInfo, "OnInit", 0);
	OnUpdateImGui = mono_class_get_method_from_name(classInfo, "OnUpdateImGui", 0);

	//add it to the collection

	monoAppDomainInfo domainInfo;
	domainInfo.m_appDomain = appDomain;
	domainInfo.m_csharpAssembly = csharpAssembly;
	domainInfo.m_coreAssemblyImage = coreAssemblyImage;
	domainInfo.m_classInfo = classInfo;
	domainInfo.m_classInstance = classInstance;
	domainInfo.m_OnPulseMethod = OnPulseMethod;
	domainInfo.m_OnWriteChatColor = OnWriteChatColor;
	domainInfo.m_OnInit = OnInit;
	domainInfo.m_OnUpdateImGui = OnUpdateImGui;


	mqAppDomains[appDomainName] = domainInfo;
	mqAppDomainPtrToString[appDomain] = appDomainName;

	//call the Init
	if (OnInit)
	{
		mono_runtime_invoke(OnInit, classInstance, nullptr, nullptr);
	}


	appDomainProcessQueue.push_front(appDomainName);


	//classConstructor = mono_class_get_method_from_name(m_classInfo, ".ctor", 1);

}

int main()
{


	InitMono();

	


	
	//simulate the onPulse from C++
	while (true)
	{
		if (appDomainProcessQueue.size() < 1) return 0;

		std::chrono::steady_clock::time_point proccessingTimer = std::chrono::steady_clock::now(); //the time this was issued + m_delayTime
		
		int count = 0;
		int processQueueSize = appDomainProcessQueue.size();
		while (count < processQueueSize)
		{
			std::string domainKey = appDomainProcessQueue.front();
			appDomainProcessQueue.pop_front();
			appDomainProcessQueue.push_back(domainKey);
			count++;
			//get pointer to struct

			std::map<std::string, monoAppDomainInfo>::iterator i = mqAppDomains.find(domainKey);
			if (i != mqAppDomains.end())
			{
				//if we have a delay check to see if we can reset it
				if (i->second.m_delayTime > 0 && std::chrono::steady_clock::now() > i->second.m_delayTimer)
				{
					i->second.m_delayTime = 0;
				}
				//check to make sure we are not in an delay
				if (i->second.m_delayTime == 0) {
					//if not, do work
					if (i->second.m_appDomain && i->second.m_OnPulseMethod)
					{
						mono_domain_set(i->second.m_appDomain, false);
						mono_runtime_invoke(i->second.m_OnPulseMethod, i->second.m_classInstance, nullptr, nullptr);
					}
				}
				//check if we have spent the specified time N-ms, or we have processed the entire queue kick out
				if (std::chrono::steady_clock::now() > (proccessingTimer + std::chrono::milliseconds(20)) || count >= appDomainProcessQueue.size())
				{
					break;
				}
			}
			else
			{
				//should never be ever to get here, but if so.
				//get rid of the bad domainKey
				appDomainProcessQueue.pop_back();
			}

			

			
		}
		
		std::this_thread::sleep_for(std::chrono::milliseconds(1000));
	}

	system("pause");
	
	return 0;
}