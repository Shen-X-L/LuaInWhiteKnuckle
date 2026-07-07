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

	[MoonSharpHidden]
	public DamageInfoData(Damageable.DamageInfo damageInfo) {_damageInfo = damageInfo;}

	[MoonSharpHidden]
	public Damageable.DamageInfo Raw => _damageInfo;

	public static void Test() {
		Plugin.LogTest("TestA");
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

	// 是否应该被使用
	public bool needEnabled = true;
}

