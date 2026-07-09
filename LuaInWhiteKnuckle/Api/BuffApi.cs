using LuaInWhiteKnuckle.Registry;
using MoonSharp.Interpreter;
using System.Collections.Generic;
using System.Linq;

namespace LuaInWhiteKnuckle.Api;


[LuaData(typeof(BuffContainer))]
[MoonSharpUserData]
public class BuffContainerData {
	private readonly BuffContainer _container;

	[MoonSharpHidden]
	public BuffContainerData(BuffContainer container) {
		_container = container;
	}

	[MoonSharpHidden]
	public BuffContainer Raw => _container;

	public BuffContainerData() {
		_container = new BuffContainer();
		_container.id = "";
		_container.desc = "";
		_container.buffs = new List<BuffContainer.Buff>();
	}

	public BuffContainerData(string id = "", string desc = "", params BuffContainer.Buff[] buffDatas) {
		_container = new BuffContainer();
		_container.id = "";
		_container.desc = "";
		_container.buffs = buffDatas.ToList();
	}

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
