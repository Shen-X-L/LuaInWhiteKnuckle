using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Entities.UniversalDelegates;
using UnityEngine.UIElements;

namespace LuaInWhiteKnuckle.Core;

public class SafeLuaSandbox {
	private Script _env;

	public ModRootApi Api { get; private set; }

	#region[生命周期函数]
	public void InitSandbox() {
		// 强制采用硬沙箱预设，仅保留纯计算模块，额外开放一个安全的时间库
		_env = new Script(CoreModules.Preset_HardSandbox | CoreModules.OS_Time);
		// 限制执行次数
		_env.Options.TailCallOptimizationThreshold = 10000;
		// 严格限制互操作策略为默认 (非白名单注册的类一律无法被 Lua 访问) 
		UserData.RegistrationPolicy = InteropRegistrationPolicy.Default;
		// 注册类型白名单
		UserData.RegisterType<ModRootApi>();
		UserData.RegisterType<ModEventBus>();
		// 实例化当前沙箱生命周期内的 API 根节点
		Api = new ModRootApi(_env);
		// 把 Game 全局变量和 C# 实例绑定
		_env.Globals["Game"] = Api;
		PluginRegistry.Build(_env);
		_env.Globals["print"] = (Action<DynValue>)(v => { Plugin.Logger.LogInfo($"[LuaInWK] {v.ToPrintString()}"); });
		_env.Globals["com_print"] = (Action<DynValue>)(v => { CommandConsole.Log($"[Lua] {v.ToPrintString()}"); });

	}

	/// <summary>
	/// 关闭并彻底清空当前沙箱环境
	/// </summary>
	public void CloseSandbox() {
		if (Api != null && Api.Events != null) {
			// 1. 核心：斩断所有 C# 列表对 Lua 闭包（Closure）的强引用
			Api.Events.ClearAllListeners();
		}

		// 2. 将 Api 和虚拟机置为空
		Api = null;
		_env = null;

		// 3. 提示：此时这个 Lua 虚拟机由于在 C# 端没有任何常驻对象引用它了，
		// 当 Unity 切换场景调用 GC 时，它会被自动完美回收。
		Plugin.Logger.LogInfo("[沙箱核心]当前游戏场景已关闭，Lua沙箱环境已安全卸载清空。");
	}

	#endregion

	/// <summary>
	/// 公开注册接口
	/// </summary>
	public static void RegisterType<T>() {
		UserData.RegisterType<T>();
	}

	public Script GetScript() {
		return _env;
	}
}