
#include <iostream>
#include <chrono>
#include <mono/metadata/assembly.h>
#include <mono/jit/jit.h>
#include <mono/metadata/debug-helpers.h>
#include <direct.h>
#include <thread> 
#include <map>
#include <filesystem>

#define GetCurrentDir _getcwd

void InitE3();
void UnloadE3();
MonoDomain* _rootDomain;
bool ShowMQ2MonoWindow = true;
std::string monoDir;
bool initialized = false;
std::string currentDirectory;

struct mqAppDomainInfo
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
std::map<std::string, mqAppDomainInfo> mqAppDomains;
std::map<MonoDomain*, std::string> mqAppDomainPtrToString;

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


	csharpAssembly = mono_domain_assembly_open(appDomain, (currentDirectory + "\\E3Core\\bin\\Debug\\Core.dll").c_str());

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

	mqAppDomainInfo domainInfo;
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

	//classConstructor = mono_class_get_method_from_name(m_classInfo, ".ctor", 1);

}

int main()
{
	//std::string str("EmbeddingMono\\");
	//
	//std::string currentDirectory(get_current_dir());
	//std::size_t found = currentDirectory.find(str);
	////Indicate Mono where you installed the lib and etc folders
	//std::cout << "CurrentDirectory:" << currentDirectory << std::endl;

	//if (found != std::string::npos)
	//{
	//	currentDirectory.erase(found+13);
	//}
	////Indicate Mono where you installed the lib and etc folders
	//std::cout << "CurrentDirectory:" << currentDirectory << std::endl;

	//mono_set_dirs((currentDirectory+"\\Mono\\lib").c_str(), (currentDirectory+"\\Mono\\etc").c_str());

	////Create the main CSharp domain
	//MonoDomain* rootDomain = mono_jit_init("Mono_Domain");
	//MonoDomain* e3Domain =mono_domain_create_appdomain((char*)"E3Runtime", nullptr);

	//mono_domain_set(e3Domain, true);
	//
	////Load the binary file as an Assembly
	//MonoAssembly* csharpAssembly = mono_domain_assembly_open(e3Domain, (currentDirectory+"\\E3Core\\bin\\Debug\\Core.dll").c_str());
	//MonoImage* CoreAssemblyImage = mono_assembly_get_image(csharpAssembly);
	//MonoClass* classInfo = mono_class_from_name(CoreAssemblyImage, "MonoCore", "Core");
	//MonoObject* instance = mono_object_new(e3Domain, classInfo);

	//MonoMethod* m_OnPulse = mono_class_get_method_from_name(classInfo, "OnPulse", 0);
	//
	//MonoMethod* m_Constructor = mono_class_get_method_from_name(classInfo, ".ctor", 1);

	//MonoMethod* m_OnWriteChatColor;
	//MonoMethod* m_OnIncomingChat;
	//MonoMethod* m_OnInit;
	//m_OnInit = mono_class_get_method_from_name(classInfo, "OnInit", 0);
	//m_OnWriteChatColor = mono_class_get_method_from_name(classInfo, "OnWriteChatColor", 1);
	//m_OnIncomingChat = mono_class_get_method_from_name(classInfo, "OnIncomingChat", 1);

	//if (!csharpAssembly)
	//{
	//	//Error detected
	//	return -1;
	//}

	////SetUp Internal Calls called from CSharp
	//const int argc = 1;
	//char* argv[argc] = { (char*)"On Pulse from MQ2Mono Says Hello" };
	//
	////Namespace.Class::Method + a Function pointer with the actual definition
	//mono_add_internal_call("MonoCore.Core::mq_Echo", &mono_Echo);
	//mono_add_internal_call("MonoCore.Core::mq_ParseTLO", &mono_ParseTLO);
	//mono_add_internal_call("MonoCore.Core::mq_DoCommand", &mono_DoCommand);
	//mono_add_internal_call("MonoCore.Core::mq_Delay", &mono_Delay);
	////mono_jit_exec(rootDomain, csharpAssembly, argc, argv);
	///*int value = 5;
	//int value2 = 508;
	//void* params[2] =
	//{
	//	&value,
	//	&value2
	//};*/

	InitMono();


	

	////call the init method
	//mono_runtime_invoke(m_OnInit, instance, nullptr, nullptr);
	
	//simulate the onPulse from C++
	while (true)
	{
		for (auto i : mqAppDomains)
		{
			//if we are not in game, kick out no sense running
			//if (gGameState != GAMESTATE_INGAME) return;
			// Run only after timer is up
			if (i.second.m_delayTime > 0 && std::chrono::steady_clock::now() > i.second.m_delayTimer)
			{
				i.second.m_delayTime = 0;
				//WriteChatf("%s", s_environment->monoDir.c_str());
				// Wait 5 seconds before running again
				//PulseTimer = std::chrono::steady_clock::now() + std::chrono::seconds(5);
				//DebugSpewAlways("MQ2Mono::OnPulse()");
			}
			//we are still in a delay
			if (i.second.m_delayTime > 0) continue;
			//WriteChatf("m_delayTime with %d", m_delayTime);
			//WriteChatf("m_delayTimer with %ld", m_delayTimer);
			//Call the main method in this code
			if (i.second.m_appDomain && i.second.m_OnPulseMethod)
			{
				mono_domain_set(i.second.m_appDomain, false);
				mono_runtime_invoke(i.second.m_OnPulseMethod, i.second.m_classInstance, nullptr, nullptr);
			}
		}


		
		std::this_thread::sleep_for(std::chrono::milliseconds(1000));
	}

	system("pause");
	
	return 0;
}