using LuaInWhiteKnuckle.Api;
using LuaInWhiteKnuckle.Runtime;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using static Steamworks.InventoryItem;

namespace LuaInWhiteKnuckle.Game;

public class PerkModule_Lua : PerkModule {
	// 缓存 Lua 传过来的表引用
	private Table _luaModuleTable;

	private Closure _update;// 每帧更新
	private float _time;// 更新计时器
	private float _tick;// 更新间隔

	private Closure _initialize;// 初始化模块
	private Closure _addModule;// 添加模块 (堆叠增加时调用)
	private Closure _onDestroy;// Perk 销毁时调用

	private Closure _getCounterString;// 获取计数器文本 (显示在 Perk 图标下方)
	private Closure _getStatBuff;// 获取指定键的统计 Buff 值
	private Closure _getDescriptionFromKey;// 根据键名获取描述文本

	private bool _isBroken = false;

	/// <summary>
	/// 通过 Lua 传入的表, 实例化一个 C# PerkModule
	/// </summary>
	public static PerkModule_Lua CreateLuaPerkModule(Table luaTable) {
		var module = new PerkModule_Lua {
			_luaModuleTable = luaTable,
			name = luaTable.Get("name").String ?? "Unknown Lua Module"
		};

		module.BindClosure();
		return module;
	}

	/// <summary>
	/// 克隆函数
	/// </summary>
	public PerkModule_Lua Clone() {
		return CreateLuaPerkModule(_luaModuleTable);
	}

	/// <summary>
	/// 构建Lua函数
	/// </summary>
	private void BindClosure() {
		var initFunc = _luaModuleTable.Get("Initialize");
		if (initFunc.Type == DataType.Function) {
			_initialize = initFunc.Function;
		}

		var updateFunc = _luaModuleTable.Get("Update");
		if (updateFunc.Type == DataType.Function) {
			_update = updateFunc.Function;
			var tickData = _luaModuleTable.Get("tick");
			_tick = tickData.Type == DataType.Number
				? Math.Max(tickData.ToObject<float>(), 0.05f)
				: 0.2f;
			_time = Time.time;
		}

		var addFunc = _luaModuleTable.Get("AddModule");
		if (addFunc.Type == DataType.Function) {
			_addModule = addFunc.Function;
		}

		var destroyFunc = _luaModuleTable.Get("OnDestroy");
		if (destroyFunc.Type == DataType.Function) {
			_onDestroy = destroyFunc.Function;
		}

		var counterFunc = _luaModuleTable.Get("GetCounterString")?? _luaModuleTable.Get("Counter");
		if (counterFunc.Type == DataType.Function) {
			_getCounterString = counterFunc.Function;
		}

		var statFunc = _luaModuleTable.Get("GetStatBuff") ?? _luaModuleTable.Get("Stat");
		if (statFunc.Type == DataType.Function) {
			_getStatBuff = statFunc.Function;
		}

		var description = _luaModuleTable.Get("GetDescriptionFromKey") ?? _luaModuleTable.Get("Description");
		if (description.Type == DataType.Function) {
			_getDescriptionFromKey = description.Function;
		}
	}

	#region[虚方法 - 生命周期]

	/// <summary>
	/// 初始化模块: 绑定 Perk 引用.
	/// </summary>
	/// <param name="p">所属 Perk</param>
	/// <param name="firstTime">是否首次初始化</param>
	public override void Initialize(Perk p, bool firstTime) {
		base.Initialize(p, firstTime);
		if (_isBroken || _initialize == null) return;
		LuaTaskManager.Execute(_initialize, $"{name}_initialize", p, firstTime);
	}

	/// <summary>
	/// 添加模块 (堆叠增加时调用).
	/// </summary>
	/// <param name="amount">增加的堆叠数量</param>
	/// <param name="firstTime">是否首次添加</param>
	public override void AddModule(int amount = 1, bool firstTime = true){
		if (_isBroken || _addModule == null) return;
		LuaTaskManager.Execute(_addModule, $"{name}_addModule", amount, firstTime);
	}
	

	/// <summary>
	/// 每帧更新 (非暂停时调用).
	/// </summary>
	public override void Update() {
		// 没有更新函数
		if (_isBroken || _update == null) return;
		// 未到触发时间
		if (_time > Time.time) return;
		_isBroken = !LuaTaskManager.Invoke(_update, $"{name}_update");
		if (_isBroken) {
			Plugin.LogDebug($"[LuaInWK] PerkModule_Lua.Update: {name} 执行错误,以关闭全部模块");
		}
		// 设置新触发时间
		_time = Time.time + _tick;

	}

	/// <summary>
	/// Perk 销毁时调用.
	/// </summary>
	public override void OnDestroy(Perk p) {
		if (_isBroken || _onDestroy == null) return;
		LuaTaskManager.Execute(_onDestroy, $"{name}_onDestroy", p);
	}

	#endregion

	#region[虚方法 - 查询]

	/// <summary>
	/// 获取计数器文本 (显示在 Perk 图标下方).
	/// </summary>
	/// <returns>计数器字符串, 默认返回空字符串</returns>
	public override string GetCounterString() {
		if (_isBroken || _getCounterString == null) return "";
		return LuaTaskManager.InvokeSync<string>(_getCounterString, $"{name}_getCounterString");
	}

	/// <summary>
	/// 获取指定键的统计 Buff 值.
	/// </summary>
	/// <param name="key">统计键名</param>
	/// <param name="total">是否计算总计值</param>
	/// <returns>统计值, 默认返回 NaN (表示未找到)</returns>
	public override float GetStatBuff(string key, bool total = false) {
		if (_isBroken || _getStatBuff == null) return float.NaN;
		return LuaTaskManager.InvokeSync<float>(_getStatBuff, $"{name}_getStatBuff", key, total);
	}

	/// <summary>
	/// 根据键名获取描述文本.
	/// </summary>
	/// <param name="key">描述键名</param>
	/// <returns>描述文本, 默认返回空字符串</returns>
	public override string GetDescriptionFromKey(string key) {
		if (_isBroken || _getDescriptionFromKey == null) return ""; 
		return LuaTaskManager.InvokeSync<string>(
			_getDescriptionFromKey, 
			$"{name}_getDescriptionFromKey", 
			key);
	}

	#endregion
}
