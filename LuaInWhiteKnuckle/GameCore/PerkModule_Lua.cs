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

	private class ClosuresCache {

		#region[Update接口]

		public readonly Closure update;// 每帧更新
		public readonly float tick;// 更新间隔

		#endregion

		#region[生命周期接口]

		public readonly Closure initialize;// 初始化模块
		public readonly Closure addModule;// 添加模块 (堆叠增加时调用)
		public readonly Closure onDestroy;// Perk 销毁时调用

		#endregion

		#region[数据查询接口]

		public readonly Closure getCounterString;// 获取计数器文本 (显示在 Perk 图标下方)
		public readonly Closure getStatBuff;// 获取指定键的统计 Buff 值
		public readonly Closure getDescriptionFromKey;// 根据键名获取描述文本

		#endregion

		/// <summary>
		/// 构建Lua函数
		/// </summary>
		public ClosuresCache(Table moduleTable) {
			var initFunc = moduleTable.Get("Initialize");
			if (initFunc.Type == DataType.Function) {
				initialize = initFunc.Function;
			}

			var updateFunc = moduleTable.Get("Update");
			if (updateFunc.Type == DataType.Function) {
				update = updateFunc.Function;
				var tickData = moduleTable.Get("tick");
				tick = tickData.Type == DataType.Number
					? Math.Max(tickData.ToObject<float>(), 0.05f)
					: 0.2f;
			}

			var addFunc = moduleTable.Get("AddModule");
			if (addFunc.Type == DataType.Function) {
				addModule = addFunc.Function;
			}

			var destroyFunc = moduleTable.Get("OnDestroy");
			if (destroyFunc.Type == DataType.Function) {
				onDestroy = destroyFunc.Function;
			}

			var counterFunc = moduleTable.Get("GetCounterString");
			if (counterFunc.Type != DataType.Function) counterFunc = moduleTable.Get("Counter");
			if (counterFunc.Type == DataType.Function) {
				getCounterString = counterFunc.Function;
			}

			var statFunc = moduleTable.Get("GetStatBuff");
			if (statFunc.Type != DataType.Function) statFunc = moduleTable.Get("Stat");
			if (statFunc.Type == DataType.Function) {
				getStatBuff = statFunc.Function;
			}

			var description = moduleTable.Get("GetDescriptionFromKey");
			if (description.Type != DataType.Function) description = moduleTable.Get("Description");
			if (description.Type == DataType.Function) {
				getDescriptionFromKey = description.Function;
			}
		}
	}

	#region[全局实例]

	// 类标签 用于定位类
	[SerializeField] private string moduleGuid;
	// 共享表 用于生成函数
	private static readonly Dictionary<string, ClosuresCache> _closuresRegistry = new();

	#endregion

	private ClosuresCache _closuresCache;     // Lua函数缓存
	private string _updateDebugName;
	private Closure _update;
	private float _tick;
	private bool _isBroken = false;
	private float _time;// 更新计时器
	private bool _initialize = false;// 是否初始化

	#region[Update接口实现变量]

	/// <summary>
	/// 通过 Lua 传入的表, 实例化一个 C# PerkModule
	/// </summary>
	public static PerkModule_Lua CreateLuaPerkModule(Table luaTable) {
		if (luaTable == null) {
			Debug.LogError("[LuaInWK] 尝试通过空的 Lua Table 创建 PerkModule_Lua");
			return new PerkModule_Lua { name = "Broken Lua Module" };
		}

		var module = new PerkModule_Lua {
			// 创建母体时生成唯一特征码，并将原始 Table 注册进中央调度室
			moduleGuid = Guid.NewGuid().ToString(),
			name = luaTable.Get("name").String ?? "Unknown Lua Module"
		};

		module._closuresCache = new ClosuresCache(luaTable);	// 创建Lua函数引用
		_closuresRegistry[module.moduleGuid] = module._closuresCache;// 缓存
		return module;
	}

	/// <summary>
	/// 重新建立Lua函数索引
	/// </summary>
	private void EnsureBound() {
		if (_closuresCache == null && !string.IsNullOrEmpty(moduleGuid)) {
			if (_closuresRegistry.TryGetValue(moduleGuid, out var sharedClosures)) {
				_closuresCache = sharedClosures;
			} else {
				_isBroken = true;
				Debug.LogError($"[Sandbox] 严重错误：未在全局注册表中找到特征码为 {moduleGuid} 的共享 Lua 表");
			}
		}
	}

	#endregion

	#region[虚方法 - 生命周期]

	/// <summary>
	/// 初始化模块: 绑定 Perk 引用.
	/// </summary>
	/// <param name="p">所属 Perk</param>
	/// <param name="firstTime">是否首次初始化</param>
	public override void Initialize(Perk p, bool firstTime) {
		base.Initialize(p, firstTime);
		EnsureBound();
		if (_closuresCache.update != null) {
			_time = Time.time;
			_update = _closuresCache.update;
			_tick = _closuresCache.tick;
			_updateDebugName = $"{name}_update";
		}

		if (_isBroken || _closuresCache == null || _closuresCache.initialize == null) {
			_initialize = true;
			return; 
		}
		LuaTaskManager.Execute(_closuresCache.initialize, $"{name}_initialize", p, firstTime);
		_initialize = true;
	}

	/// <summary>
	/// 添加模块 (堆叠增加时调用).
	/// </summary>
	/// <param name="amount">增加的堆叠数量</param>
	/// <param name="firstTime">是否首次添加</param>
	public override void AddModule(int amount = 1, bool firstTime = true){
		if (_isBroken || _closuresCache == null || _closuresCache.addModule == null) return;
		LuaTaskManager.Execute(_closuresCache.addModule, $"{name}_addModule", amount, firstTime);
	}
	

	/// <summary>
	/// 每帧更新 (非暂停时调用).
	/// </summary>
	public override void Update() {
		// 没有更新函数
		if (_isBroken || _update == null || !_initialize) return;
		// 未到触发时间
		if (_time > Time.time) return;
		_isBroken = !LuaTaskManager.InvokeFast(_update);
		if (_isBroken) Plugin.LogDebug($"[LuaInWK] PerkModule_Lua.Update: {name} 执行错误,以关闭全部模块");
		// 设置新触发时间
		_time = Time.time + _tick;
	}

	/// <summary>
	/// Perk 销毁时调用.
	/// </summary>
	public override void OnDestroy(Perk p) {
		if (_isBroken || _closuresCache == null || _closuresCache.onDestroy == null) return;
		LuaTaskManager.Execute(_closuresCache.onDestroy, $"{name}_onDestroy", p);
	}

	#endregion

	#region[虚方法 - 查询]

	/// <summary>
	/// 获取计数器文本 (显示在 Perk 图标下方).
	/// </summary>
	/// <returns>计数器字符串, 默认返回空字符串</returns>
	public override string GetCounterString() {
		if (_isBroken || _closuresCache == null || _closuresCache.getCounterString == null) return "";
		return LuaTaskManager.InvokeSync<string>(_closuresCache.getCounterString, $"{name}_getCounterString");
	}

	/// <summary>
	/// 获取指定键的统计 Buff 值.
	/// </summary>
	/// <param name="key">统计键名</param>
	/// <param name="total">是否计算总计值</param>
	/// <returns>统计值, 默认返回 NaN (表示未找到)</returns>
	public override float GetStatBuff(string key, bool total = false) {
		if (_isBroken || _closuresCache == null || _closuresCache.getStatBuff == null) return float.NaN;
		return LuaTaskManager.InvokeSync<float>(_closuresCache.getStatBuff, $"{name}_getStatBuff", key, total);
	}

	/// <summary>
	/// 根据键名获取描述文本.
	/// </summary>
	/// <param name="key">描述键名</param>
	/// <returns>描述文本, 默认返回空字符串</returns>
	public override string GetDescriptionFromKey(string key) {
		if (_isBroken || _closuresCache == null || _closuresCache.getDescriptionFromKey == null) return ""; 
		return LuaTaskManager.InvokeSync<string>(
			_closuresCache.getDescriptionFromKey, 
			$"{name}_getDescriptionFromKey", 
			key);
	}

	#endregion
}
