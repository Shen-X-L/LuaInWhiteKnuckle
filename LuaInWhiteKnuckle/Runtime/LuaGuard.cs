using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LuaInWhiteKnuckle.Runtime;

public static class LuaHookGuard {
	// 使用 ThreadStatic 防止在支持多线程的框架下发生抢占冲突
	[ThreadStatic]
	private static HashSet<string> _activeHooks;

	/// <summary>
	/// 防护令牌
	/// </summary>
	public readonly struct GuardHandle : IDisposable {
		/// <summary>
		/// 是否成功获取到了执行权
		/// </summary>
		public readonly bool CanExecute;
		private readonly string _hookName;

		internal GuardHandle(string hookName, bool canExecute) {
			_hookName = hookName;
			CanExecute = canExecute;
		}

		/// <summary>
		/// using 块结束时自动调用
		/// </summary>
		public void Dispose() {
			// 只有成功进入的,退出时才需要移除标记
			if (CanExecute && _activeHooks != null) {
				_activeHooks.Remove(_hookName);
			}
		}
	}

	/// <summary>
	/// 尝试进入一个 Hook,配合 using 使用
	/// </summary>
	public static GuardHandle Enter(string hookName) {
		if (_activeHooks == null) {
			_activeHooks = new HashSet<string>();
		}

		// Add 成功说明是首次进入,返回 true 失败说明发生重入,返回 false
		bool canExecute = _activeHooks.Add(hookName);
		return new GuardHandle(hookName, canExecute);
	}
}

public class LuaIncludeGuard {
	// 已加载过的脚本集合 (实现 Include Once，防重复执行)
	private readonly HashSet<string> _loadedScripts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	// 当前正在加载的栈 (用于检测循环依赖 A -> B -> A)
	private readonly Stack<string> _loadingStack = new Stack<string>();

	public readonly struct GuardHandle : IDisposable {
		public readonly bool ShouldExecute;
		private readonly string _scriptPath;
		private readonly LuaIncludeGuard _owner;

		internal GuardHandle(LuaIncludeGuard owner, string scriptPath, bool shouldExecute) {
			_owner = owner;
			_scriptPath = scriptPath;
			ShouldExecute = shouldExecute;
		}

		// 退出当前文件的加载栈
		public void Dispose() {
			if (ShouldExecute && _owner != null) _owner.PopLoadingStack(_scriptPath);
		}
	}

	/// <summary>
	/// 尝试开始 include
	/// </summary>
	public GuardHandle TryInclude(string scriptPath, out string warningOrError) {
		warningOrError = null;
		string normalizedPath = Path.GetFullPath(scriptPath); // 规范化路径，防止 ../ 导致同一文件判断失误

		// 检查 1: 检测是否发生死循环依赖 (A -> B -> A)
		if (_loadingStack.Contains(normalizedPath)) {
			string chain = string.Join(" -> ", _loadingStack.Reverse().Select(Path.GetFileName));
			warningOrError = $"[LuaIncludeGuard] 检测到循环依赖！加载链: {chain} -> {Path.GetFileName(normalizedPath)}";
			return new GuardHandle(this, normalizedPath, false);
		}

		// 检查 2: 检查是否已经加载过 (Include Once 语义)
		if (_loadedScripts.Contains(normalizedPath)) 
			// 已经加载过了，静默跳过，不需要重复执行
			return new GuardHandle(this, normalizedPath, false);

		// 标记为正在加载 & 已加载
		_loadingStack.Push(normalizedPath);
		_loadedScripts.Add(normalizedPath);

		return new GuardHandle(this, normalizedPath, true);
	}

	/// <summary>
	/// 退栈
	/// </summary>
	/// <param name="scriptPath"></param>
	private void PopLoadingStack(string scriptPath) {
		if (_loadingStack.Count > 0 && _loadingStack.Peek() == scriptPath) 
			_loadingStack.Pop();
	}

	/// <summary>
	/// 重启沙箱或重新加载 Lua 时清空
	/// </summary>
	public void Clear() {
		_loadedScripts.Clear();
		_loadingStack.Clear();
	}
}