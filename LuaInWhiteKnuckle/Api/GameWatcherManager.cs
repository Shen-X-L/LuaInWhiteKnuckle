using LuaInWhiteKnuckle.Core;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LuaInWhiteKnuckle.Api;

public class GameWatcherManager : MonoBehaviour {
	private readonly List<IWatcher> _watchers = new();
	private readonly Dictionary<string, List<IWatcher>> _eventMap = new();
	private void Awake() {
		Plugin.gameWatcherManager.Register(new InventoryMonitor());
		Plugin.gameWatcherManager.Register(new HandItemMonitor());
	}

	private void Update() {
		float now = Time.unscaledTime;
		for (int i = 0; i < _watchers.Count; i++) {
			var watcher = _watchers[i];
			if (!watcher.NeedEnable) continue;
			Plugin.LogDebug("GameWatcherManager.Update B");
			if (now < watcher.NextUpdateTime) continue;
			watcher.NextUpdateTime = now + watcher.Interval;

			watcher.Tick();
		}
	}

	private void OnDestroy() {
		_watchers.Clear();
		_eventMap.Clear();
	}

	public void Register(IWatcher watcher) {
		if (watcher == null) return;

		if (_watchers.Contains(watcher)) return;

		_watchers.Add(watcher);

		foreach (string eventName in watcher.Events) {
			if (!_eventMap.TryGetValue(eventName, out var list)) {
				list = new List<IWatcher>();
				_eventMap[eventName] = list;
			}

			if (!list.Contains(watcher))
				list.Add(watcher);
		}
	}

	public void Unregister(IWatcher watcher) {
		if (watcher == null) return;
		_watchers.Remove(watcher);
		foreach (string eventName in watcher.Events) {
			if (!_eventMap.TryGetValue(eventName, out var list))
				continue;

			list.Remove(watcher);

			// 顺便清理空列表
			if (list.Count == 0)
				_eventMap.Remove(eventName);
		}
	}

	public void Enable(string eventName, bool enable) {
		if (!_eventMap.TryGetValue(eventName, out var watchers))
			return;

		foreach (var watcher in watchers) {
			if (enable)
				watcher.EnableCount++;
			else
				watcher.EnableCount--;
		}
	}
}

public interface IWatcher {

	int EnableCount { get; set; }

	bool NeedEnable => EnableCount > 0;
	// 事件名称
	IReadOnlyList<string> Events { get; }
	// 轮询间隔，单位秒
	float Interval { get; }
	// 下次轮询时间
	float NextUpdateTime { get; set; }
	// 轮询函数
	void Tick();
}