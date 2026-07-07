using HarmonyLib;
using LuaInWhiteKnuckle.Core;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static BuffContainer;
using static Damageable;
using static GameEntity;
using static Steamworks.InventoryItem;

namespace LuaInWhiteKnuckle.Api;

[LuaApi("Player")]
[MoonSharpUserData]
public class PlayerApi {

	private static readonly AccessTools.FieldRef<EntityBuff, Dictionary<string, Buff>> _finalBuffsRef =
		AccessTools.FieldRefAccess<EntityBuff, Dictionary<string, Buff>>("finalBuffs");

	#region[玩家BUFF API]

	/// <summary>
	/// 快速添加一个增益到玩家身上
	/// </summary>
	/// <param name="id">buff ID (重要 需要匹配游戏本身buff的ID)</param>
	/// <param name="amount">效率倍率</param>
	/// <param name="buffTime">buff时长 (秒)</param>
	/// <param name="loseRate">衰减速率 (每一秒减x秒)</param>
	/// <param name="loseOverTime">是否随时间衰减</param>
	public void AddBuff(string id, float amount, string containerId = "", float buffTime = 1f, float loseRate = 0.1f, bool loseOverTime = true) {
		if (ENT_Player.GetPlayer() == null) return;
		var buffContainer = new BuffContainer {
			id = containerId,
			desc = "",
			buffs = new List<Buff> {
				new Buff {
					id = id,
					maxAmount = amount,
				}
			},
			buffTime = buffTime,
			loseRate = loseRate,
			loseOverTime = loseOverTime,
		};
		ENT_Player.GetPlayer().curBuffs.AddBuff(buffContainer);
	}

	/// <summary>
	/// 获取特定BUFF
	/// </summary>
	public float GetBuff(string id) {
		return ENT_Player.GetPlayer()?.curBuffs.GetBuff(id) ?? 0f;
	}

	/// <summary>
	/// 获取特定BUFF
	/// </summary>
	public Dictionary<string, Buff> GetAllBuff() {
		if (ENT_Player.GetPlayer()?.curBuffs != null)
			return _finalBuffsRef(ENT_Player.GetPlayer()?.curBuffs);
		return null;
	}

	/// <summary>
	/// 获取全部buff容器
	/// </summary>
	public List<BuffContainer> GetAllBuffContainer() =>
		ENT_Player.GetPlayer()?.curBuffs?.currentBuffs ?? new List<BuffContainer>();

	/// <summary>
	/// 添加一个增益容器到玩家身上
	/// </summary>
	/// <param name="buffContainerData"></param>
	public void AddBuffContainer(BuffContainerData buffContainerData) =>
		ENT_Player.GetPlayer()?.curBuffs.AddBuff(buffContainerData.Raw);

	/// <summary>
	/// 根据ID删除容器
	/// </summary>
	public void RemoveBuffContainer(string id) =>
		ENT_Player.GetPlayer()?.curBuffs?.RemoveBuffContainer(id);

	/// <summary>
	/// 删除容器
	/// </summary>
	public void RemoveBuffContainerA(BuffContainerData buffContainerData) =>
		ENT_Player.GetPlayer()?.curBuffs?.RemoveBuffContainer(buffContainerData.Raw);

	/// <summary>
	/// 删除容器
	/// </summary>
	public void RemoveBuffContaineB(BuffContainer buffContainer) =>
		ENT_Player.GetPlayer()?.curBuffs?.RemoveBuffContainer(buffContainer);
	#endregion

	/// <summary>
	/// 给玩家造成伤害
	/// </summary>
	public void Damage(DamageInfoData damage) {
		ENT_Player.GetPlayer()?.Damage(damage.Raw);
	}
}

[HarmonyPatch(typeof(ENT_Player))]
public class Patch_ENT_Player {
	public static bool isLuaHookCall = false;
	public static bool isLuaCall = false;

	[HarmonyPatch(nameof(ENT_Player.Damage))]
	[HarmonyPrefix]
	public static bool Patch_Damage(DamageInfo info) {
		// 是Lua回调调用
		if (isLuaHookCall) {
			// 取消标记并执行原逻辑
			isLuaHookCall = false;
			return true;
		}
		// 禁止事件响应立刻造成玩家伤害
		if (isLuaCall) {
			return false;
		}
		isLuaCall = true;
		ModEventBus.TriggerEvent("OnPlayerDamage", info);
		isLuaCall = false;
		// 无HOOK 执行原逻辑
		if (!Plugin.safeLuaSandbox.Api.Hooks.Contains("OnPlayerDamage"))
			return true;

		DamageInfoData damageInfoData;
		try {
			damageInfoData = ModHookBus.InvokeHook<DamageInfoData>("OnPlayerDamage", new DamageInfoData(info));
		} catch (Exception e) {
			Plugin.LogError(e.Message);
			return true; // Lua异常 仍然按原伤害执行
		}
		// Lua异常 仍然按原伤害执行
		if (damageInfoData == null)
			return true;
		// 伤害被避免
		if (!damageInfoData.needEnabled)
			return false;
		// 标记并执行新伤害代码
		isLuaHookCall = true;
		ENT_Player.GetPlayer().Damage(damageInfoData.Raw);

		return false;
	}
}

