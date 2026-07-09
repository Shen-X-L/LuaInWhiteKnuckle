using LuaInWhiteKnuckle.Game;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using System.Text;
using UnityEngine;


namespace LuaInWhiteKnuckle.Runtime;

public class SafeLuaSandbox {
	private Script _env;

	public ModRootApi Api { get; private set; }

	public bool IsInitialized { get; private set; } = false;

	#region[生命周期函数]
	public void InitSandbox() {
		if (_env != null)
			CloseSandbox();
		// 强制采用硬沙箱预设，仅保留纯计算模块，额外开放一个安全的时间库
		_env = new Script(CoreModules.Preset_HardSandbox | CoreModules.OS_Time);
		// 限制执行次数
		_env.Options.TailCallOptimizationThreshold = 10000;
		// 严格限制互操作策略为默认 (非白名单注册的类一律无法被 Lua 访问) 
		UserData.RegistrationPolicy = InteropRegistrationPolicy.Default;
		// 实例化当前沙箱生命周期内的 API 根节点
		Api = new ModRootApi(_env);
		// 把 Game 全局变量和 C# 实例绑定
		_env.Globals["print"] = new CallbackFunction((ctx, args) => {
			StringBuilder sb = new();
			sb.Append("[LuaInWK]");
			for (int i = 0; i < args.Count; i++) {
				sb.Append(args[i].ToPrintString());
			}
			Plugin.LogInfo(sb.ToString());
			return DynValue.Nil;
		});
		_env.Globals["com_print"] = new CallbackFunction((ctx, args) => {
			StringBuilder sb = new();
			sb.Append("[Lua]");
			for (int i = 0; i < args.Count; i++) {
				sb.Append(args[i].ToPrintString());
			}
			CommandConsole.Log(sb.ToString());
			return DynValue.Nil;
		});
		IsInitialized = true;
	}

	/// <summary>
	/// 关闭并彻底清空当前沙箱环境
	/// </summary>
	public void CloseSandbox() {
		if (_env == null)
			return;
		// 清空 事件监听 和 HOOK
		Api?.Dispose();
		// 将 Api 和虚拟机置为空
		Api = null;
		_env = null;

		// 提示: 此时这个 Lua 虚拟机由于在 C# 端没有任何常驻对象引用它了，
		Plugin.Logger.LogInfo("[LuaInWK] Sandbox Closed");
		IsInitialized = false;
	}

	/// <summary>
	/// 重置沙箱
	/// </summary>
	public void ResetSandbox() {
		CloseSandbox();
		InitSandbox();
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