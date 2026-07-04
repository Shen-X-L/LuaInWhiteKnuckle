using LuaInWhiteKnuckle.Core;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LuaInWhiteKnuckle.Api;

public class GameWatcherManager : MonoBehaviour {
	private readonly List<IWatcher> _watchers = new();
	private void Awake() { 
		_watchers.Add(new InventoryMonitor());
	}

	private void Update() {
		float now = Time.unscaledTime;
		for (int i = 0; i < _watchers.Count; i++) {
			var watcher = _watchers[i];

			bool needEnable = false;
			foreach (var eventName in watcher.EventName) {
				if (Plugin.safeLuaSandbox?.Api?.Events?.TryGetListeners(eventName, out var listeners) == true)
					needEnable = true;
			}
			Plugin.LogDebug("GameWatcherManager.Update A");
			if (!needEnable) continue;
			Plugin.LogDebug("GameWatcherManager.Update B");
			if (now < watcher.NextUpdateTime) continue;
			watcher.NextUpdateTime = now + watcher.Interval;

			watcher.Tick();
		}
	}
	

	private void OnDestroy() {
		_watchers.Clear();
	}
}

public interface IWatcher {
	// 事件名称
	List<string> EventName { get; }
	// 轮询间隔，单位秒
	float Interval { get; }
	// 下次轮询时间
	float NextUpdateTime { get; set; }
	// 轮询函数
	void Tick();
}