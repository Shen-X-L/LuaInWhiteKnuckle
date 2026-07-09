using System;
using System.Collections.Generic;
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
	/// 尝试进入一个 Hook,配合 using 使用。
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
