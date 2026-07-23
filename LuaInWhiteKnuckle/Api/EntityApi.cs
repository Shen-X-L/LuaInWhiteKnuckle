using LuaInWhiteKnuckle.Registry;
using LuaInWhiteKnuckle.Game;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static Damageable;

namespace LuaInWhiteKnuckle.Api;

#region[数据类]

[LuaData(typeof(GameEntity))]
[MoonSharpUserData]
public class GameEntityData {
	private readonly GameEntity _gameEntity;

	[MoonSharpHidden]
	public GameEntityData(GameEntity gameEntity) {
		_gameEntity = gameEntity;
	}

	[MoonSharpHidden]
	public GameEntity Raw => _gameEntity;

	// 获取全部实体
	public static List<GameEntity> GetAllGameEntity() => GameEntity.entities;

	// 对象类型: "entity" (实体)
	public string objectType {
		get => _gameEntity?.objectType;
		set => _gameEntity?.objectType = value;
	}
	// 实体预制体 ID: "entity"
	public string entityPrefabID { get => _gameEntity?.entityPrefabID; }
	// 对象名称
	public string name {
		get => _gameEntity?.name;
		set => _gameEntity?.name = value;
	}
	// 生物坐标
	public Transform transform { get => _gameEntity?.transform; }
	// 当前生命值
	public float health {
		get => _gameEntity?.health ?? 0f;
		set => _gameEntity?.health = value;
	}
	// 最大生命值
	public float maxHealth {
		get => _gameEntity?.maxHealth ?? 0f;
		set => _gameEntity?.maxHealth = value;
	}
	public ObjectTagger tagger { get => _gameEntity.gameObject.GetComponent<ObjectTagger>(); }
	// 火焰时间乘数
	public float fireTimeMult {
		get => _gameEntity?.fireTimeMult ?? 0f;
		set => _gameEntity?.fireTimeMult = value;
	}
	// 火焰伤害乘数
	public float fireDamageMult {
		get => _gameEntity?.fireDamageMult ?? 0f;
		set => _gameEntity?.fireDamageMult = value;
	}
	// 是否死亡
	public bool dead { get => _gameEntity?.dead ?? true; }
	// 实际类型
	public string typeName { get => _gameEntity?.GetType().Name; }
	// 造成伤害
	public void Damage(Damageable.DamageInfo info) => _gameEntity?.Damage(info);
	// 击杀实体
	public void Kill(string type)=>_gameEntity?.Kill(type);
}

[LuaData(typeof(DamageInfo))]
[MoonSharpUserData]
public class DamageInfoData {
	private readonly Damageable.DamageInfo _damageInfo;

	[MoonSharpHidden]
	public DamageInfoData(Damageable.DamageInfo damageInfo) { _damageInfo = damageInfo; }

	[MoonSharpHidden]
	public Damageable.DamageInfo Raw => _damageInfo;

	public DamageInfoData() {
		_damageInfo = new Damageable.DamageInfo();
		_damageInfo.type = "";
		_damageInfo.tags = new List<string>();
	}

	public DamageInfoData(float amount, string type = "", params string[] tags) {
		_damageInfo = new Damageable.DamageInfo();
		_damageInfo.amount = amount;
		_damageInfo.type = type;
		_damageInfo.tags = new List<string>(tags.Length);
		_damageInfo.tags.Add(type);
		_damageInfo.tags.AddRange(tags);
	}

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
		get => _damageInfo.sourceObject?.name ?? null;
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
}

[LuaData(typeof(ObjectTagger))]
[MoonSharpUserData]
public class ObjectTaggerData {
	private readonly ObjectTagger _objectTagger;

	[MoonSharpHidden]
	public ObjectTaggerData(ObjectTagger objectTagger) { _objectTagger = objectTagger; }

	[MoonSharpHidden]
	public ObjectTagger Raw => _objectTagger;

	public List<string> tags { get => _objectTagger?.tags; }
}

#endregion