using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace LuaInWhiteKnuckle.Core;

public class LuaCommands {
	public static void RegisterCommands() {
		CommandConsole.BuildCommand("lua",ExecuteLuaCommand);
		CommandConsole.BuildCommand("luafile",ExecuteLuaFileCommand)
			.AutocompleteCustom(autocomplete => { autocomplete.FromArray(Plugin.luaFileManager.LuaRelativePaths); });
		CommandConsole.BuildCommand("luakill", KillLuaTaskCommand)
			.AutocompleteCustom(autocomplete => { autocomplete.FromArray(Plugin.luaTaskManager.TasksName); });
		CommandConsole.BuildCommand("luatest", Program.Main)
			.NotCheat()
			.AutocompleteCustom(autocomplete => { autocomplete.FromArray(Program.TestSet); });
	}

	public static void ExecuteLuaCommand(string[] luaCode) {
		Plugin.luaTaskManager.Execute(string.Join(" ", luaCode[1..]), luaCode[0]);
	}

	public static void ExecuteLuaFileCommand(string[] luaFilePath) {
		string luaScript = Plugin.luaFileManager.ReadLuaFile(string.Join(" ", luaFilePath));
		Plugin.luaTaskManager.Execute(luaScript, luaFilePath[0]);
	}

	public static void KillLuaTaskCommand(string[] luaTaskName) {
		Plugin.luaTaskManager.KillTask(string.Join(" ", luaTaskName[1..]));
	}
}

[HarmonyPatch(typeof(CommandConsole))]
public class Patch_CommandConsole {
	// 启用时注册命令
	[HarmonyPatch("Awake")]
	[HarmonyPostfix]
	public static void Awake_RegisterCommands() {
		LuaCommands.RegisterCommands();
		return;
	}
}