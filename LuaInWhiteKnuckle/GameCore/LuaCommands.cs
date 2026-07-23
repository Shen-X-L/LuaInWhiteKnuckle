using HarmonyLib;
using LuaInWhiteKnuckle.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace LuaInWhiteKnuckle.Game;

public class LuaCommands {
	public static void RegisterCommands() {
		CommandConsole.BuildCommand("lua", ExecuteLuaCommand);
		CommandConsole.BuildCommand("luafile", ExecuteLuaFileCommand)
			.AutocompleteCustom(autocomplete => { autocomplete.FromArray(Plugin.luaFileManager.LuaRelativePaths); });
		CommandConsole.BuildCommand("luakill", KillLuaTaskCommand)
			.AutocompleteCustom(autocomplete => { autocomplete.FromArray(Plugin.luaTaskManager.TasksName); });
		CommandConsole.BuildCommand("luatest", Program.Main)
			.NotCheat()
			.AutocompleteCustom(autocomplete => { autocomplete.FromArray(Program.TestSet); });
	}

	public static void ExecuteLuaCommand(string[] luaCode) {
		LuaTaskManager.Execute(string.Join(" ", luaCode[1..]), luaCode[0]);
	}

	public static void ExecuteLuaFileCommand(string[] luaFilePath) {
		foreach (var path in luaFilePath) {
			string luaScript = Plugin.luaFileManager.ReadLuaFile(path);
			LuaTaskManager.Execute(luaScript, path);
		}
	}

	public static void KillLuaTaskCommand(string[] luaTaskName) {
		LuaTaskManager.KillTask(string.Join(" ", luaTaskName[1..]));
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