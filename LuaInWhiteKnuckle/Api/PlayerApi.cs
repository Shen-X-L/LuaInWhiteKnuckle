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
		if (ENT_Player.GetPlayer() == null) return;
		ENT_Player.GetPlayer().curBuffs.AddBuff(buffContainerData.ToBuffContainer());
	}

	public void Damage() { 
	
	}
}

[MoonSharpUserData]
public class BuffContainerData {
	public string id;               // 增益容器ID
	public string desc;             // 描述文本
	private readonly List<BuffData> _buffs = new();
	public BuffData[] buffs => _buffs.ToArray();
	public float buffTime = 1f;                 // 当前增益剩余时间(归一化0-1)
	public float loseRate = 0.1f;               // 增益衰减速率
	public bool loseOverTime = true;            // 是否随时间衰减
	public bool loseRateEffectedByPerks = true; // 衰减速率是否受玩家天赋影响
	public float multiplier = 1f;               // 全局倍率

	public void Add(string id, float amount) {
		_buffs.Add(new BuffData {
			id = id,
			maxAmount = amount,
		});
	}

	public BuffContainerData(string id = "", bool loseOverTime = true, 
		float buffTime = 1f, float loseRate = 0.1f, bool loseRateEffectedByPerks = true, 
		float multiplier = 1f,string desc = "") {
		this.id = id;
		this.loseOverTime = loseOverTime;
		this.buffTime = buffTime;
		this.loseRate = loseRate;
		this.loseRateEffectedByPerks = loseRateEffectedByPerks;
		this.multiplier = multiplier;
		this.desc = desc;
	}

	[MoonSharpHidden]
	public BuffContainer ToBuffContainer() {
		var buffContainer = new BuffContainer {
			id = id,
			desc = desc,
			buffTime = buffTime,
			loseRate = loseRate,
			loseOverTime = loseOverTime,
			loseRateEffectedByPerks = loseRateEffectedByPerks,
			multiplier = multiplier,
			buffs = new List<BuffContainer.Buff>()
		};
		foreach (var buff in _buffs) {
			buffContainer.buffs.Add(new BuffContainer.Buff {
				id = buff.id,
				maxAmount = buff.maxAmount
			});
		}
		return buffContainer;
	}

	[MoonSharpHidden]
	public BuffContainerData(BuffContainer buffContainer) { 
		id = buffContainer.id;
		desc = buffContainer.desc;
		_buffs = new List<BuffData>();
		buffTime = buffContainer.buffTime;
		loseRate = buffContainer.loseRate;
		loseOverTime = buffContainer.loseOverTime;
		loseRateEffectedByPerks = buffContainer.loseRateEffectedByPerks;
		multiplier = buffContainer.multiplier;
		foreach (var buff in buffContainer.buffs) {
			_buffs.Add(new BuffData {
				id = buff.id,
				maxAmount = buff.maxAmount
			});
		}
	}
}

[MoonSharpUserData]
public class BuffData {
	public string id;           // 属性ID(如 "addJump", "addSpeed", "addSlow")
	public float maxAmount;     // 最大增益值
}

[MoonSharpUserData]
public class DamageInfoData {
	public float amount;		// 伤害数值
	public string type;			// 伤害类型(如 "fire", "slash", "pierce")
	public string[] tags;       // 伤害标签列表(用于更精细的伤害分类)
	public string sourceObject;	// 造成伤害的游戏对象的名字
	public Vector3 position;	// 伤害发生的位置(用于特效, 击退方向等)
	public Vector3 force;       // 冲击力向量(用于击退效果)
	public BuffContainerData hitBuffContainer; // 击中时施加的增益
	public bool needEnabled = false;    // 是否需要启用或忽略伤害(用于某些特殊情况, 如触发器伤害)

	[MoonSharpHidden]
	public Damageable.DamageInfo ToDamageInfo() {
		var damageInfo = new Damageable.DamageInfo {
			amount = amount,
			type = type,
			tags = new List<string>(tags),
			position = position,
			force = force,
			hitBuffContainer = hitBuffContainer?.ToBuffContainer()
		};
		return damageInfo;
	}

	[MoonSharpHidden]
	public DamageInfoData(Damageable.DamageInfo damageInfo) { 
		amount = damageInfo.amount;
		type = damageInfo.type;
		tags = damageInfo.tags.ToArray();
		sourceObject = damageInfo.sourceObject?.name ?? "";
		position = damageInfo.position;
		force = damageInfo.force;
		hitBuffContainer = new BuffContainerData(damageInfo.hitBuffContainer);
	}
}

[HarmonyPatch(typeof(ENT_Player))]
public class Patch_ENT_Player {
	public static bool isLuaCall = false;

	[HarmonyPatch(nameof(ENT_Player.Damage))]
	[HarmonyPrefix]
	public static void Patch_Damage(Damageable.DamageInfo info) {
		if (Plugin.safeLuaSandbox.Api.Hooks.Contains("OnPlayerDamage")) {
			_ = ModHookBus.InvokeHook<DamageInfoData>("OnPlayerDamage", new DamageInfoData(info));
		}
	}
}

