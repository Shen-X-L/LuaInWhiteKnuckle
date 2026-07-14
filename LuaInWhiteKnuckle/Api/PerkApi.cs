using LuaInWhiteKnuckle.Registry;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static Perk;

namespace LuaInWhiteKnuckle.Api;

#region[API类]
[LuaApi("Perk")]
[MoonSharpUserData]
public class PerkApi {
	[MoonSharpHidden]
	public static Sprite DefaultModIcon;
	[MoonSharpHidden]
	public static Sprite DefaultModCard;
	[MoonSharpHidden]
	public static Sprite DefaultModFrame;

	[MoonSharpHidden]
	public static void Setup(Sprite defaultIcon, Sprite defaultCard, Sprite defaultFrame) {
		DefaultModIcon = defaultIcon;
		DefaultModCard = defaultCard;
		DefaultModFrame = defaultFrame;
	}

	public Dictionary<string, Perk> LuaPerks { get; } = new();

	/// <summary>
	/// 提供给 Lua 调用的接口：动态创建一个纯 Buff 的自定义 Perk
	/// </summary>
	public Perk CreateCustomBuffPerk(string id, string title = "", string description = "", bool attenuation = true) {
		// 在内存中创建一个全新的 ScriptableObject 实例
		Perk perk = ScriptableObject.CreateInstance<Perk>();

		// 初始化
		perk.id = id;
		perk.title = title;
		perk.description = description;
		perk.flavorText = "LuaInWK Create Perk";
		perk.spawnPool = Perk.PerkPool.never; // 默认不污染游戏原版随机生成池
		perk.perkType = Perk.PerkType.standard;

		perk.canStack = true;
		perk.stackMax = 100;
		// 默认堆叠曲线,否则 GetStackEvaluationMultiplier 异常
		// 衰减曲线
		if (attenuation) perk.multiplierCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
		// 不衰减曲线
		else perk.multiplierCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));

		// 设置图标
		perk.icon = DefaultModIcon;
		perk.perkFrame = DefaultModFrame;
		perk.perkCard = DefaultModCard;

		// 分配空列表,防止 Initialize 里的 foreach 报空指针
		perk.tags = new List<string> { "lua" };
		perk.flags = new List<string>();
		perk.playerTag = new List<string>();
		perk.modules = new List<PerkModule>();

		// 初始化 Buff 容器
		perk.useBuff = true;
		perk.buff = new BuffContainer {
			id = id + "_Buff",
			buffs = new List<BuffContainer.Buff>()
		};
		perk.useBaseBuff = false;
		perk.baseBuff = new BuffContainer {
			id = id + "_Base_Buff",
			buffs = new List<BuffContainer.Buff>()
		};
		perk.buffMultiplier = 1f;

		LuaPerks.Add(id, perk);

		return perk;
	}

	public Perk GetPerk(string perkId) {
		if (LuaPerks.TryGetValue(perkId, out var perk))
			return perk;
		return CL_AssetManager.GetPerkAsset(perkId);
	}
}

#endregion

#region[数据类]

[LuaData(typeof(Perk))]
[MoonSharpUserData]
public class PerkData {
	#region[基础包装]

	private readonly Perk _perk;

	[MoonSharpHidden]
	public PerkData(Perk perk) {
		_perk = perk;
		id = perk.id;
	}

	[MoonSharpHidden]
	public Perk Raw => _perk;

	#endregion

	#region[基本信息]

	// 标签列表
	public string id;
	// 标签列表
	public List<string> tags { get => _perk.tags; }
	// 标题
	public string title { get => _perk.title; set => _perk.title = value; }
	// 描述文本
	public string description { get => _perk.description; set => _perk.description = value; }
	// 风味文本
	public string flavorText { get => _perk.flavorText; set => _perk.flavorText = value; }
	// Perk 类型
	public string perkType {
		get => EnumStringMapper<PerkType>.GetString(_perk.perkType);
		set => _perk.perkType = EnumStringMapper<PerkType>.GetEnum(value);
	}
	// 排序类别
	public int sortingCategory { get => _perk.sortingCategory; set => _perk.sortingCategory = value; }
	// 是否在净化时移除 (如 Rho 房间)
	public bool removeOnCleanse { get => _perk.removeOnCleanse; set => _perk.removeOnCleanse = value; }

	#endregion

	#region[生成与堆叠]

	// 是否在竞技模式中可用
	public bool competitive { get => _perk.competitive; set => _perk.competitive = value; }
	// 消耗点数
	public int cost { get => _perk.cost; set => _perk.cost = value; }
	// 生成池
	public string spawnPool {
		get => EnumStringMapper<PerkPool>.GetString(_perk.spawnPool);
		set => _perk.spawnPool = EnumStringMapper<PerkPool>.GetEnum(value);
	}
	// 是否在无尽模式中生成
	public bool spawnInEndless { get => _perk.spawnInEndless; set => _perk.spawnInEndless = value; }
	// 是否可堆叠
	public bool canStack { get => _perk.canStack; set => _perk.canStack = value; }
	// 最大堆叠数
	public int stackMax { get => _perk.stackMax; set => _perk.stackMax = value; }
	// 编辑堆叠曲线
	public CurveData multiplierCurve {
		get => new CurveData(_perk.multiplierCurve);
		set => _perk.multiplierCurve = value?.Raw ?? CurveData.Constant(1).Raw;
	}

	#endregion

	#region[Buff 配置]

	// 是否使用 Buff
	public bool useBuff { get => _perk.useBuff; set => _perk.useBuff = value; }
	// 主 Buff 容器
	public BuffContainer buff { get => _perk.buff; }
	// 是否使用基础 Buff
	public bool useBaseBuff { get => _perk.useBaseBuff; set => _perk.useBaseBuff = value; }
	// 基础 Buff 容器
	public BuffContainer baseBuff { get => _perk.baseBuff; }
	// Buff 乘数
	public float buffMultiplier { get => _perk.buffMultiplier; set => _perk.buffMultiplier = value; }
	// 激活时设置的标记
	public List<string> flags { get => _perk.flags; }
	// 激活时添加的玩家标签
	public List<string> playerTag { get => _perk.playerTag; }

	#endregion

	#region[运行时状态]

	// 当前堆叠数量
	public int stackAmount { get => _perk.stackAmount; set => _perk.stackAmount = value; }
	// 是否激活
	public bool IsActive() => _perk.IsActive();

	#endregion

	#region[函数]

	// 是否在玩家上
	public bool IsPlayerPerk() => ENT_Player.GetPlayer()?.perks?.Contains(_perk) ?? false;
	// 从玩家身上移除
	public void RemoveFromPlayer() => _perk.RemoveFromPlayer();
	// 克隆函数
	public Perk Clone() => UnityEngine.Object.Instantiate(_perk);

	#endregion
}

#endregion