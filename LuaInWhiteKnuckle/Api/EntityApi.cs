using LuaInWhiteKnuckle.Core;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static Damageable;

namespace LuaInWhiteKnuckle.Api;

public class EntityApi {
}

[LuaData(typeof(DamageInfo))]
[MoonSharpUserData]
public class DamageInfoData {
	private readonly Damageable.DamageInfo _damageInfo;

	public DamageInfoData() {_damageInfo = new Damageable.DamageInfo();}

	[MoonSharpHidden]
	public DamageInfoData(Damageable.DamageInfo damageInfo) {_damageInfo = damageInfo;}

	[MoonSharpHidden]
	public Damageable.DamageInfo Raw => _damageInfo;

	// 伤害数值
	public float amount {
		get => _damageInfo.amount;
		set => _damageInfo.amount = value;
	}

	// 伤害类型(如 "Melle", "flare", "rebar")
	public string type {
		get => _damageInfo.type;
		set => _damageInfo.type = value;
	}

	// 伤害标签列表
	public List<string> tags => _damageInfo.tags;

	// 造成伤害的实体
	public string sourceObject {
		get => _damageInfo.sourceObject.name;
	}

	// 伤害发生的位置
	public Vector3 position {
		get => _damageInfo.position;
		set => _damageInfo.position = value;
	}

	// 冲击力向量
	public Vector3 force {
		get => _damageInfo.force;
		set => _damageInfo.force = value;
	}

	// 击中时施加的增益/减益
	public BuffContainer hitBuffContainer {
		get => _damageInfo.hitBuffContainer;
		set => _damageInfo.hitBuffContainer = value;
	}

	// 是否应该被使用
	public bool needEnabled = true;
}

[LuaData(typeof(BuffContainer))]
[MoonSharpUserData]
public class BuffContainerData {
	private readonly BuffContainer _container;

	public BuffContainerData() {
		_container = new BuffContainer();
	}

	[MoonSharpHidden]
	public BuffContainerData(BuffContainer container) {
		_container = container;
	}

	[MoonSharpHidden]
	public BuffContainer Raw => _container;

	// 增益容器ID
	public string id {
		get => _container.id;
		set => _container.id = value;
	}

	// 描述文本
	public string desc {
		get => _container.desc;
		set => _container.desc = value;
	}

	public List<BuffContainer.Buff> buffs => _container.buffs;

	// 当前增益剩余时间(归一化0-1)
	public float buffTime {
		get => _container.buffTime;
		set => _container.buffTime = value;
	}

	// 增益衰减速率
	public float loseRate {
		get => _container.loseRate;
		set => _container.loseRate = value;
	}

	// 是否随时间衰减
	public bool loseOverTime {
		get => _container.loseOverTime;
		set => _container.loseOverTime = value;
	}

	// 衰减速率是否受玩家天赋影响
	public bool loseRateEffectedByPerks {
		get => _container.loseRateEffectedByPerks;
		set => _container.loseRateEffectedByPerks = value;
	}

	// 全局倍率
	public float multiplier {
		get => _container.multiplier;
		set => _container.multiplier = value;
	}

	public void Add(string id, float amount) {
		_container.buffs.Add(new BuffContainer.Buff {
			id = id,
			maxAmount = amount
		});
	}          

}

[LuaData(typeof(BuffContainer.Buff))]
[MoonSharpUserData]
public class BuffData {
	private readonly BuffContainer.Buff _buff;

	public BuffData() {_buff = new BuffContainer.Buff();}

	[MoonSharpHidden]
	public BuffData(BuffContainer.Buff buff) {_buff = buff;}

	// 属性ID(如 "addJump", "addSpeed", "addSlow")
	public string id {
		get => _buff.id;
		set => _buff.id = value;
	}

	// 最大增益值
	public float maxAmount {
		get => _buff.maxAmount;
		set => _buff.maxAmount = value;
	}
}