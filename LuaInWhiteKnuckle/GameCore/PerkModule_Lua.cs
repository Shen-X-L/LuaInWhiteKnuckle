using LuaInWhiteKnuckle.Api;
using LuaInWhiteKnuckle.Registry;
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
		#region[ClosuresCache 缓存结构]

		// Update接口
		public readonly Closure update;// 每帧更新
		public readonly float updateTick;// 更新间隔

		// 生命周期接口
		public readonly Closure initialize;// 初始化模块
		public readonly Closure addModule;// 添加模块 (堆叠增加时调用)
		public readonly Closure onDestroy;// Perk 销毁时调用

		// 数据查询接口
		public readonly Closure getCounterString;// 获取计数器文本 (显示在 Perk 图标下方)
		public readonly float counterTick;// 获取计数器文本间隔 (防止每帧调用)
		public readonly Closure getStatBuff;// 获取指定键的统计 Buff 值
		public readonly float statTick;// 获取指定键的统计文本间隔 (防止每帧调用)
		public readonly Closure getDescriptionFromKey;// 根据键名获取描述文本
		public readonly float descriptionTick;// 获取指定键的描述文本间隔 (防止每帧调用)

		// 序列化接口
		public readonly Closure getSaveData;// 序列化生成保存文件
		public readonly Closure loadSaveData;// 反序列化加载保存文件

		/// <summary>
		/// 构建Lua函数
		/// </summary>
		public ClosuresCache(Table moduleTable) {
			if (moduleTable.Get("Initialize").Type == DataType.Function)
				initialize = moduleTable.Get("Initialize").Function;
			if (moduleTable.Get("AddModule").Type == DataType.Function)
				addModule = moduleTable.Get("AddModule").Function;
			if (moduleTable.Get("OnDestroy").Type == DataType.Function)
				onDestroy = moduleTable.Get("OnDestroy").Function;

			if (moduleTable.Get("Update").Type == DataType.Function) {
				update = moduleTable.Get("Update").Function;
				var tickData = moduleTable.Get("update_tick");
				updateTick = tickData.Type == DataType.Number ? Math.Max(tickData.ToObject<float>(), 0.01f) : 0.2f;
			}

			var counterFunc = moduleTable.Get("GetCounterString").Type == DataType.Function
				? moduleTable.Get("GetCounterString") : moduleTable.Get("Counter");
			if (counterFunc.Type == DataType.Function) {
				getCounterString = counterFunc.Function;
				var tickData = moduleTable.Get("counter_tick");
				counterTick = tickData.Type == DataType.Number ? Math.Max(tickData.ToObject<float>(), 0.2f) : 1f;
			}

			var statFunc = moduleTable.Get("GetStatBuff").Type == DataType.Function
				? moduleTable.Get("GetStatBuff") : moduleTable.Get("Stat");
			if (statFunc.Type == DataType.Function) {
				getStatBuff = statFunc.Function;
				var tickData = moduleTable.Get("stat_tick");
				statTick = tickData.Type == DataType.Number ? Math.Max(tickData.ToObject<float>(), 0.2f) : 1f;

			}

			var descFunc = moduleTable.Get("GetDescriptionFromKey").Type == DataType.Function
				? moduleTable.Get("GetDescriptionFromKey") : moduleTable.Get("Description");
			if (descFunc.Type == DataType.Function) {
				getDescriptionFromKey = descFunc.Function;
				var tickData = moduleTable.Get("description_tick");
				descriptionTick = tickData.Type == DataType.Number ? Math.Max(tickData.ToObject<float>(), 0.2f) : 1f;
			}

			if (moduleTable.Get("GetSaveData").Type == DataType.Function) 
				getSaveData = moduleTable.Get("GetSaveData").Function;
			if (moduleTable.Get("LoadSaveData").Type == DataType.Function) 
				loadSaveData = moduleTable.Get("LoadSaveData").Function;
		}	
		
		#endregion
	}

	#region[字段和属性]

	// 类标签 用于定位类
	[SerializeField] 
	private string scriptId;
	private bool _isBroken = false;// 是否损坏并不进行更新
	private bool _initialized = false;// 是否初始化

	private ClosuresCache _closuresCache;     // Lua函数缓存

	private Closure _update;
	private float _updateTick;
	private float _updateTime;// Update更新计时器
	private float _counterTime;// GetCounterString更新计时器
	private string _counterCache;// GetCounterString结果缓存
	
	// Stat 依赖 (key, total)，使用 Dictionary 存储 (缓存值, 到期时间)
	private Dictionary<(string key, bool total), (float value, float expireTime)> _statCacheDict;

	// Description 依赖 key，使用 Dictionary 存储 (缓存值, 到期时间)
	private Dictionary<string, (string value, float expireTime)> _descCacheDict;


	#endregion

	#region[构造函数]

	public PerkModule_Lua(string scriptId) {
		this.scriptId = scriptId;
		EnsureBound();
	}

	/// <summary>
	/// 重新建立Lua函数索引
	/// </summary>
	private void EnsureBound() {
		if (_closuresCache == null && !string.IsNullOrEmpty(scriptId)) {
			// 如果缓存里有, 直接拿缓存;没有, 自动去读 Perks/{scriptId}.lua 并放入缓存
			Table currentTable = LuaPerkRegistry.GetModule(scriptId);

			if (currentTable != null) {
				_closuresCache = new ClosuresCache(currentTable);
				this.name = currentTable.Get("name").String ?? scriptId;
				_isBroken = false;
			} else {
				_isBroken = true;
				Debug.LogError($"[PerkModule_Lua] 致命错误: 无法绑定模块 [{scriptId}]");
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
			_update = _closuresCache.update;
			_updateTick = _closuresCache.updateTick;
			_updateTime = Time.time;
		}
		if (_closuresCache.getCounterString != null) {
			_counterTime = Time.time;
			_counterCache = "";
		}
		if (_closuresCache.getStatBuff != null) {
			_statCacheDict = new();
		}
		if (_closuresCache.getDescriptionFromKey != null) {
			_descCacheDict = new();
		}
		if (_initialized) return;
		_initialized = true;
		if (_isBroken || _closuresCache == null || _closuresCache.initialize == null) return;
		LuaTaskManager.Execute(_closuresCache.initialize, $"{name}_initialize", p, firstTime);
	}

	/// <summary>
	/// 添加模块 (堆叠增加时调用).
	/// </summary>
	/// <param name="amount">增加的堆叠数量</param>
	/// <param name="firstTime">是否首次添加</param>
	public override void AddModule(int amount = 1, bool firstTime = true) {
		if (_isBroken || _closuresCache == null || _closuresCache.addModule == null) return;
		LuaTaskManager.Execute(_closuresCache.addModule, $"{name}_addModule", amount, firstTime);
	}


	/// <summary>
	/// 每帧更新 (非暂停时调用).
	/// </summary>
	public override void Update() {
		// 没有更新函数
		if (_isBroken || _update == null || !_initialized) return;
		// 未到触发时间
		if (_updateTime > Time.time) return;
		_isBroken = !LuaTaskManager.InvokeFast(_update);
		if (_isBroken) Plugin.LogDebug($"[LuaInWK] PerkModule_Lua.Update: {name} 执行错误,以关闭全部模块");
		// 设置新触发时间
		_updateTime = Time.time + _updateTick;
	}

	/// <summary>
	/// Perk 销毁时调用.
	/// </summary>
	public override void OnDestroy(Perk p) {
		if (!_initialized) return;
		_initialized = false;
		if (_isBroken || _closuresCache == null || _closuresCache.onDestroy == null) return;
		LuaTaskManager.Execute(_closuresCache.onDestroy, $"{name}_onDestroy", p);
	}

	#endregion

	#region [虚方法 - 读存档]

	/// <summary>
	/// 进行存档数据保存
	/// </summary>
	public override List<string> GetSaveData() {
		var saveData = new List<string> { scriptId }; // Index 0 固定存 scriptId

		if (!_isBroken && _closuresCache?.getSaveData != null) {
			var luaCustomData = LuaTaskManager.InvokeSync<List<string>>(_closuresCache.getSaveData, $"{name}_getSaveData");
			if (luaCustomData != null) 
				saveData.AddRange(luaCustomData);
		}

		return saveData;
	}

	/// <summary>
	/// 读档数据保存
	/// </summary>
	/// <param name="m"></param>
	public override void LoadSaveData(PerkModuleSaveInfo m) {
		base.LoadSaveData(m);
		if (m?.data == null || m.data.Count == 0) return;

		// 恢复静态 scriptId (如 "ModID:FireImmunity")
		this.scriptId = m.data[0];

		// 重新匹配最新加载的 Table 闭包
		EnsureBound();

		// 将自定义运行时数据送回 Lua 恢复状态
		if (!_isBroken && _closuresCache?.loadSaveData != null && m.data.Count > 1) {
			var customData = m.data.GetRange(1, m.data.Count - 1);
			LuaTaskManager.Execute(_closuresCache.loadSaveData, $"{name}_loadSaveData", customData);
		}
	}

	#endregion

	#region[虚方法 - 查询]

	/// <summary>
	/// 获取计数器文本 (显示在 Perk 图标下方).
	/// </summary>
	/// <returns>计数器字符串, 默认返回空字符串</returns>
	public override string GetCounterString() {
		if (_isBroken || _closuresCache == null || _closuresCache.getCounterString == null) return "";
		if (_counterTime > Time.time) return _counterCache;
		_counterCache = LuaTaskManager.InvokeSync<string>(_closuresCache.getCounterString, $"{name}_getCounterString");
		_counterTime = Time.time + _closuresCache.counterTick;
		return _counterCache;
	}

	/// <summary>
	/// 获取指定键的统计 Buff 值.
	/// </summary>
	/// <param name="key">统计键名</param>
	/// <param name="total">是否计算总计值</param>
	/// <returns>统计值, 默认返回 NaN (表示未找到)</returns>
	public override float GetStatBuff(string key, bool total = false) {
		if (_isBroken || _closuresCache == null || _closuresCache.getStatBuff == null) return float.NaN;

		var cacheKey = (key, total);
		float currentTime = Time.time;

		// 检查当前 Key 是否已有未过期的缓存
		if (_statCacheDict.TryGetValue(cacheKey, out var cache)) 
			if (cache.expireTime > currentTime) 
				return cache.value; // 直接返回该 Key 专属的缓存
		
		// 缓存失效或首次获取 调用 Lua
		float newValue = LuaTaskManager.InvokeSync<float>(_closuresCache.getStatBuff, $"{name}_getStatBuff", key, total);
		// 更新该 Key 的缓存与过期时间
		_statCacheDict[cacheKey] = (newValue, currentTime + _closuresCache.statTick);
		return newValue;
	}

	/// <summary>
	/// 根据键名获取描述文本.
	/// </summary>
	/// <param name="key">描述键名</param>
	/// <returns>描述文本, 默认返回空字符串</returns>
	public override string GetDescriptionFromKey(string key) {
		if (_isBroken || _closuresCache == null || _closuresCache.getDescriptionFromKey == null) return "";

		float currentTime = Time.time;

		// 检查当前 Key 是否已有未过期的缓存
		if (_descCacheDict.TryGetValue(key, out var cache)) {
			if (cache.expireTime > currentTime) {
				return cache.value;
			}
		}

		// 调用 Lua 获取描述
		string newValue = LuaTaskManager.InvokeSync<string>(
			_closuresCache.getDescriptionFromKey,
			$"{name}_getDescriptionFromKey",
			key);

		// 更新该 Key 的缓存
		_descCacheDict[key] = (newValue, currentTime + _closuresCache.descriptionTick);

		return newValue;
	}

	#endregion
}
