using HarmonyLib;
using LuaInWhiteKnuckle.Core;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static BuffContainer;
using static Steamworks.InventoryItem;

namespace LuaInWhiteKnuckle.Api;

[LuaApi("Player")]
[MoonSharpUserData]
public class PlayerApi {

	/// <summary>
	/// 快速添加一个增益容器到玩家身上
	/// </summary>
	/// <param name="id"></param>
	/// <param name="amount"></param>
	/// <param name="buffTime"></param>
	/// <param name="loseRate"></param>
	/// <param name="loseOverTime"></param>
	public void AddBuff(string id, float amount, float buffTime = 1f, float loseRate = 0.1f, bool loseOverTime = true) {
		if (ENT_Player.GetPlayer() == null) return;
		var buffContainer = new BuffContainer {
			buffs = new List<BuffContainer.Buff> {
				new BuffContainer.Buff {
					id = id,
					maxAmount = amount
				}
			},
			buffTime = buffTime,
			loseRate = loseRate,
			loseOverTime = loseOverTime,
		};
		ENT_Player.GetPlayer().curBuffs.AddBuff(buffContainer);
	}

	/// <summary>
	/// 添加一个增益容器到玩家身上
	/// </summary>
	/// <param name="buffContainerData"></param>
	public void AddBuff(BuffContainerData buffContainerData) {

	}

	public void Damage() { 
	
	}
}

[HarmonyPatch(typeof(ENT_Player))]
public class Patch_ENT_Player {
	public static bool isLuaCall = false;

	[HarmonyPatch(nameof(ENT_Player.Damage))]
	[HarmonyPrefix]
	public static bool Patch_Damage(Damageable.DamageInfo info) {
		// 无HOOK 执行原逻辑
		if (!Plugin.safeLuaSandbox.Api.Hooks.Contains("OnPlayerDamage"))
			return true;
		var damageInfoData = ModHookBus.InvokeHook<DamageInfoData>("OnPlayerDamage", new DamageInfoData(info));
		return false;
	}
}

