
#include <iostream>
#include <chrono>
#include <mono/metadata/assembly.h>
#include <mono/jit/jit.h>
#include <mono/metadata/debug-helpers.h>
#include <direct.h>
#include <thread> 
#define GetCurrentDir _getcwd


int m_delayTime;
std::chrono::steady_clock::time_point m_delayTimer;

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
	MonoDomain* tDomain = mono_domain_get();
	//do domnain lookup via its pointer
	m_delayTimer = std::chrono::steady_clock::now()+std::chrono::milliseconds(milliseconds);
	m_delayTime = milliseconds;

}
std::string get_current_dir() {
	char buff[FILENAME_MAX]; //create string buffer to hold path
	GetCurrentDir(buff, FILENAME_MAX);
	std::string current_working_dir(buff);
	return current_working_dir;
}
int main()
{
	std::string str("EmbeddingMono\\");
	
	std::string currentDirectory(get_current_dir());
	std::size_t found = currentDirectory.find(str);
	//Indicate Mono where you installed the lib and etc folders
	std::cout << "CurrentDirectory:" << currentDirectory << std::endl;

	if (found != std::string::npos)
	{
		currentDirectory.erase(found+13);
	}
	//Indicate Mono where you installed the lib and etc folders
	std::cout << "CurrentDirectory:" << currentDirectory << std::endl;

	mono_set_dirs((currentDirectory+"\\Mono\\lib").c_str(), (currentDirectory+"\\Mono\\etc").c_str());

	//Create the main CSharp domain
	MonoDomain* rootDomain = mono_jit_init("Mono_Domain");
	MonoDomain* e3Domain =mono_domain_create_appdomain((char*)"E3Runtime", nullptr);

	mono_domain_set(e3Domain, true);
	
	//Load the binary file as an Assembly
	MonoAssembly* csharpAssembly = mono_domain_assembly_open(e3Domain, (currentDirectory+"\\E3Core\\bin\\Debug\\Core.dll").c_str());
	MonoImage* CoreAssemblyImage = mono_assembly_get_image(csharpAssembly);
	MonoClass* classInfo = mono_class_from_name(CoreAssemblyImage, "MonoCore", "Core");
	MonoObject* instance = mono_object_new(e3Domain, classInfo);

	MonoMethod* m_OnPulse = mono_class_get_method_from_name(classInfo, "OnPulse", 0);
	
	MonoMethod* m_Constructor = mono_class_get_method_from_name(classInfo, ".ctor", 1);

	MonoMethod* m_OnWriteChatColor;
	MonoMethod* m_OnIncomingChat;
	MonoMethod* m_OnInit;
	m_OnInit = mono_class_get_method_from_name(classInfo, "OnInit", 0);
	m_OnWriteChatColor = mono_class_get_method_from_name(classInfo, "OnWriteChatColor", 1);
	m_OnIncomingChat = mono_class_get_method_from_name(classInfo, "OnIncomingChat", 1);

	if (!csharpAssembly)
	{
		//Error detected
		return -1;
	}

	//SetUp Internal Calls called from CSharp
	const int argc = 1;
	char* argv[argc] = { (char*)"On Pulse from MQ2Mono Says Hello" };
	
	//Namespace.Class::Method + a Function pointer with the actual definition
	mono_add_internal_call("MonoCore.Core::mq_Echo", &mono_Echo);
	mono_add_internal_call("MonoCore.Core::mq_ParseTLO", &mono_ParseTLO);
	mono_add_internal_call("MonoCore.Core::mq_DoCommand", &mono_DoCommand);
	mono_add_internal_call("MonoCore.Core::mq_Delay", &mono_Delay);
	//mono_jit_exec(rootDomain, csharpAssembly, argc, argv);
	/*int value = 5;
	int value2 = 508;
	void* params[2] =
	{
		&value,
		&value2
	};*/

	//call the init method
	mono_runtime_invoke(m_OnInit, instance, nullptr, nullptr);
	
	//simulate the onPulse from C++
	while (true)
	{
		if (m_OnIncomingChat) {

			MonoString* monoLine = mono_string_new(e3Domain, "BOB");
			void* params[1] =
			{
				monoLine

			};
			mono_runtime_invoke(m_OnIncomingChat, instance, params, nullptr);
			//do not free monoLine as its now part of the GC
		}
		if (m_delayTime > 0)
		{
			if (std::chrono::steady_clock::now() < m_delayTimer)
			{
				continue;
			}
			m_delayTime = 0;
		}
		mono_runtime_invoke(m_OnPulse, instance, nullptr, nullptr);
		std::this_thread::sleep_for(std::chrono::milliseconds(1000));
	}

	system("pause");
	
	return 0;
}