using HarmonyLib;
using LuaInWhiteKnuckle.Registry;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace LuaInWhiteKnuckle.Api;

[LuaApi("MASS")]
[MoonSharpUserData]
public class MASSApi {
	[MoonSharpHidden]
	public static DEN_DeathFloor _mass;

	// 是否激活
	public bool active {
		get => _mass.IsActive();
		set => _mass.SetActive(value);
	}
	// 是否可以击杀玩家
	public bool canKill {
		get => Patch_DEN_DeathFloor._canKillField(_mass);
		set => Patch_DEN_DeathFloor._canKillField(_mass) = value;
	}
	// 绝对高度
	public float worldHeight {
		get => _mass.transform.position.y;
		set => _mass.SetHeight(value);
	}
	// 与玩家的相对高度
	public float offsetHeight {
		get => _mass.transform.position.y - ENT_Player.GetPlayer().transform.position.y;
		set => _mass.transform.position = ENT_Player.GetPlayer().transform.position + Vector3.up * value;
	}
	// 基础上升速度
	public float speed {
		get => _mass.speed;
		set => _mass.speed = value;
	}
	// 最大速度
	public float maxSpeed {
		get => _mass.maxSpeed;
		set => _mass.maxSpeed = value;
	}
	// 速度增长速率
	public float speedIncreaseRate {
		get => _mass.speedIncreaseRate;
		set => _mass.speedIncreaseRate = value;
	}
	// 速度增长速率乘数
	public float speedIncreaseRateMultiplier {
		get => Patch_DEN_DeathFloor._speedIncreaseRateMultiplierField(_mass);
		set => Patch_DEN_DeathFloor._speedIncreaseRateMultiplierField(_mass) = value;
	}
	// 速度乘数 (持久)
	public float speedMult {
		get => Patch_DEN_DeathFloor._speedMultField(_mass);
		set => Patch_DEN_DeathFloor._speedMultField(_mass) = value;
	}
	// 橡皮筋效应乘数
	public float rubberbandMult {
		get => _mass.rubberbandMult;
		set => _mass.rubberbandMult = value;
	}
	// 橡皮筋效应是否激活
	public bool rubberbandActive {
		get => Patch_DEN_DeathFloor._rubberbandActiveField(_mass);
		set => Patch_DEN_DeathFloor._rubberbandActiveField(_mass) = value;
	}
}
[HarmonyPatch(typeof(DEN_DeathFloor))]
public static class Patch_DEN_DeathFloor {
	public static readonly AccessTools.FieldRef<DEN_DeathFloor, bool> _canKillField =
		AccessTools.FieldRefAccess<DEN_DeathFloor, bool>("canKill");
	public static readonly AccessTools.FieldRef<DEN_DeathFloor, float> _speedIncreaseRateMultiplierField =
		AccessTools.FieldRefAccess<DEN_DeathFloor, float>("speedIncreaseRateMultiplier");
	public static readonly AccessTools.FieldRef<DEN_DeathFloor, float> _speedMultField =
		AccessTools.FieldRefAccess<DEN_DeathFloor, float>("speedMult");
	public static readonly AccessTools.FieldRef<DEN_DeathFloor, bool> _rubberbandActiveField =
		AccessTools.FieldRefAccess<DEN_DeathFloor, bool>("rubberbandActive");

	[HarmonyPatch("Start")]
	[HarmonyPostfix]
	public static void Patch_Start() {
		MASSApi._mass = DEN_DeathFloor.instance;
	}
}