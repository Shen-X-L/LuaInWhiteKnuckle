using LuaInWhiteKnuckle.Core;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LuaInWhiteKnuckle.Api;

public class BuffApi {
}

[LuaData(typeof(BuffContainer))]
[MoonSharpUserData]
public class BuffContainerData {
	private readonly BuffContainer _container;

	public BuffContainerData() {
		_container = new BuffContainer();
		_container.id = "";
		_container.desc = "";
		_container.buffs = new List<BuffContainer.Buff>();
	}

	public BuffContainerData(string id = "", string desc = "", params BuffData[] buffDatas) {
		_container = new BuffContainer();
		_container.id = "";
		_container.desc = "";
		_container.buffs = buffDatas.Select(buffData => buffData.Raw).ToList();
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

	// 添加buff
	public void AddBuff(string id, float amount) {
		_container.buffs.Add(new BuffContainer.Buff {
			id = id,
			maxAmount = amount,
		});
	}

}

[LuaData(typeof(BuffContainer.Buff))]
[MoonSharpUserData]
public class BuffData {
	private readonly BuffContainer.Buff _buff;

	public BuffData() {
		_buff = new BuffContainer.Buff { id = "", maxAmount = 0 };
	}
	public BuffData(string id, float amount) {
		_buff = new BuffContainer.Buff { id = id, maxAmount = amount, };
	}

	[MoonSharpHidden]
	public BuffData(BuffContainer.Buff buff) { _buff = buff; }

	[MoonSharpHidden]
	public BuffContainer.Buff Raw => _buff;

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

	// 实时增益量 仅访问
	public float amount { get => _buff.amount; }
}