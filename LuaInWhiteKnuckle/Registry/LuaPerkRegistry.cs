using HarmonyLib;
using LuaInWhiteKnuckle.Game;
using LuaInWhiteKnuckle.Runtime;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace LuaInWhiteKnuckle.Registry;

public static class LuaPerkRegistry {
	/// <summary>
	/// 场景重启或沙箱清空时调用
	/// </summary>
	public static void Clear() {
		// 销毁 ScriptableObject 实例
		//foreach (var kvp in _luaPerkTemplates)
		//	if (kvp.Value != null)
		//		GameObject.Destroy(kvp.Value);

		//// 清空缓存
		//_luaPerkTemplates.Clear();
		_moduleCache.Clear();
	}

	#region[perk module注册]

	// 静态缓存: Key = scriptId (如 "fire_immunity"), Value = 编译好的 Lua Table
	private static readonly Dictionary<string, Table> _moduleCache = new();

	/// <summary>
	/// 按需获取模块
	/// </summary>
	public static Table GetModule(string scriptId) {
		if (string.IsNullOrEmpty(scriptId)) return null;

		// 检查沙箱合法性
		if (Plugin.safeLuaSandbox == null || !Plugin.safeLuaSandbox.IsInitialized) {
			Plugin.LogError($"[PerkRegistry] 获取模块 [{scriptId}] 失败: 沙箱未初始化");
			return null;
		}

		// 命中缓存: 直接返回
		if (_moduleCache.TryGetValue(scriptId, out var cachedTable)) 
			return cachedTable;

		// 未命中缓存: 触发按需文件加载
		Table loadedTable = LoadModuleFromDisk(scriptId);
		if (loadedTable != null) {
			_moduleCache[scriptId] = loadedTable; // 写入缓存
			Plugin.LogInfo($"[PerkRegistry] 加载成功并写入缓存: [{scriptId}]");
		}

		return loadedTable;
	}

	/// <summary>
	/// 从磁盘物理文件动态加载 Lua 脚本
	/// </summary>
	private static Table LoadModuleFromDisk(string scriptId) {
		var matchedPaths = Plugin.luaFileManager.FilterBySuffix(Path.Combine("Perks", $"{scriptId}.lua"));

		if (matchedPaths == null || matchedPaths.Length == 0) {
			Plugin.LogError($"[PerkRegistry] 脚本 [{scriptId}] 未找到");
			return null;
		}
		if (matchedPaths.Length > 1) {
			Plugin.LogError($"[PerkRegistry] 脚本 [{scriptId}] 匹配项过多: {string.Join(",", matchedPaths)}");
			return null;
		}

		string relativePath = matchedPaths[0];

		try {
			// 通过 LuaFileManager 安全读取文件内容 (与 FileSystemWatcher 监听联动)
			if (!Plugin.luaFileManager.TryReadLuaFile(relativePath,out var luaScript)) {
				Plugin.LogError($"[PerkRegistry] 脚本文件内容为空: [{relativePath}]");
				return null;
			}

			Script script = Plugin.safeLuaSandbox.GetScript();

			// 使用安全包装: 将脚本编译为匿名函数,避免顶层执行代码绕过指令拦截
			DynValue chunk = script.DoString($"return function()\n{luaScript}\nend");
			if (chunk.Type != DataType.Function) {
				Plugin.LogError($"[PerkRegistry] 编译脚本失败: [{scriptId}]");
				return null;
			}

			// 通过 LuaTaskManager 执行编译块,享用指令超限熔断保护
			DynValue moduleResult = LuaTaskManager.InvokeSyncFast(chunk.Function);
			if (moduleResult == null || moduleResult.IsNil()) return null;

			// 处理返回结果 (Table 或 Factory Function)
			Table moduleTable = null;

			if (moduleResult.Type == DataType.Table) {
				moduleTable = moduleResult.Table;
			} else if (moduleResult.Type == DataType.Function) {
				// 如果返回的是工厂函数,通过 TaskManager 安全执行工厂闭包
				DynValue factoryResult = LuaTaskManager.InvokeSyncFast(moduleResult.Function);
				if (factoryResult != null && factoryResult.Type == DataType.Table) {
					moduleTable = factoryResult.Table;
				}
			}

			if (moduleTable != null) {
				// 如果 Lua 内没写 id,默认用 scriptId 补全
				if (string.IsNullOrEmpty(moduleTable.Get("id").String)) moduleTable["id"] = scriptId;
				return moduleTable;
			}

			Plugin.LogError($"[PerkRegistry] 脚本 [{scriptId}] 结尾必须 return 一个 Table 或 Factory Function！");
		} catch (ScriptRuntimeException ex) {
			Plugin.LogError($"[PerkRegistry] 懒加载 Lua 脚本崩溃 [{scriptId}]: {ex.DecoratedMessage}");
		} catch (Exception ex) {
			Plugin.LogError($"[PerkRegistry] 读取脚本磁盘文件失败 [{scriptId}]: {ex.Message}");
		}

		return null;
	}

	#endregion

	#region[perk 注册]

	// 静态字典: 生命周期贯穿整个游戏运行期，场景切换不丢失
	private static readonly Dictionary<string, Perk> _luaPerkTemplates = new();

	public static IReadOnlyDictionary<string, Perk> LuaPerks => _luaPerkTemplates;

	/// <summary>
	/// 注册或替换模板
	/// </summary>
	public static void RegisterTemplate(string id, Perk perk) {
		_luaPerkTemplates[id] = perk;
	}

	/// <summary>
	/// 获取 Perk 模板（如果找不到则去原版 AssetManager 找）[cite: 9]
	/// </summary>
	public static Perk GetPerk(string perkId) {
		if (_luaPerkTemplates.TryGetValue(perkId, out var perk)) return perk;
		return CL_AssetManager.GetPerkAsset(perkId); // 回退到原版[cite: 9]
	}

	/// <summary>
	/// 获取 Perk 模板（如果找不到则去原版 AssetManager 找）[cite: 9]
	/// </summary>
	public static Perk GetLuaPerk(string perkId) {
		if (_luaPerkTemplates.TryGetValue(perkId, out var perk)) return perk;
		return null; // 回退到原版[cite: 9]
	}

	#endregion
}

[HarmonyPatch(typeof(CL_AssetManager), nameof(CL_AssetManager.GetPerkAsset))]
public static class Patch_CL_AssetManager_GetPerkAsset {
	[HarmonyPrefix]
	public static bool Prefix(string id, ref Perk __result) {
		if (string.IsNullOrEmpty(id)) return true;
		Plugin.LogTest(id);
		Perk luaPerk = LuaPerkRegistry.GetLuaPerk(id);
		if (luaPerk != null) {
			__result = luaPerk;
			return false; // 阻止原方法继续执行
		}

		return true; // 继续执行原版逻辑
	}
}