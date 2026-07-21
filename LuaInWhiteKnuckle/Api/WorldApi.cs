using HarmonyLib;
using LuaInWhiteKnuckle.Registry;
using LuaInWhiteKnuckle.Runtime;
using MoonSharp.Interpreter;
using System.Collections.Generic;
using UnityEngine;

namespace LuaInWhiteKnuckle.Api;

[LuaApi("World")]
[MoonSharpUserData]
public class WorldApi {
	// 与该点最近的关卡
	public M_Level GetClosestLevelToPosition(Vector3 position) =>
		WorldLoader.GetClosestLevelToPosition(position).GetLevel();
	// 玩家所在关卡
	public M_Level GetCurrentLevel()=> WorldLoader.instance.GetCurrentBranch().currentLevel.GetLevel();
}
	

[LuaData(typeof(M_Level))]
[MoonSharpUserData]
public class LevelData {
	#region[基础包装]

	private readonly M_Level _level;

	[MoonSharpHidden]
	public LevelData(M_Level level) {
		_level = level;
	}

	[MoonSharpHidden]
	public M_Level Raw => _level;

	#endregion

	#region[基本信息]
	// 关卡名称
	public string name => _level.levelName;
	// 关卡出口
	public Transform exit => _level.GetLevelExit();
	// 关卡入口
	public Transform entrance => _level.GetLevelEntrance();
	// 玩家出生点
	public Vector3 spawnPosition => _level.GetSpawnPosition();
	// 介绍文本
	public string introText => _level.introText;
	// 是否已进入过
	public bool hasEntered => _level.HasEntered();
	// MASS速度乘数
	public float massSpeedMult {
		get => _level.massSpeedMult;
		set => _level.massSpeedMult = value;
	}
	// HUNTER速度乘数
	public float hunterSpeedMult {
		get => _level.hunterSpeedMult;
		set => _level.hunterSpeedMult = value;
	}
	// 是否为安全区域
	public bool safeArea {
		get => _level.safeArea;
		set => _level.safeArea = value;
	}
	// 是否允许事件
	public bool allowEvents {
		get => _level.allowEvents;
		set => _level.allowEvents = value;
	}
	// 标签列表
	public List<string> tags => _level.tags;

	#endregion
}

[HarmonyPatch(typeof(M_Level))]
public static class Patch_M_Level {
	[HarmonyPatch(nameof(M_Level.OnEnter))]
	[HarmonyPrefix]
	public static void Patch_OnEnter_Prefix(M_Level __instance, out bool __state) {
		// 是否首次进入
		__state = !__instance.HasEntered();
	}
	[HarmonyPatch(nameof(M_Level.OnEnter))]
	[HarmonyPostfix]
	public static void Patch_OnEnter_Postfix(M_Level __instance, bool __state) {
		// Prefix 判断是否为首次进入
		ModEventBus.TriggerEvent("EnterLevel", __instance, __state);
	}

	[HarmonyPatch(nameof(M_Level.OnExit))]
	[HarmonyPostfix]
	public static void Patch_OnExit(M_Level __instance) {
		ModEventBus.TriggerEvent("ExitLevel", __instance);
	}
}