using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LuaInWhiteKnuckle.Game;

public class LuaFileManager : IDisposable {
	private static readonly string _path = Path.GetDirectoryName(typeof(LuaFileManager).Assembly.Location) ?? string.Empty;
	private string[] _cachedPaths;
	private readonly FileSystemWatcher _watcher;
	private readonly object _lock = new object();

	public LuaFileManager() {
		// 首次加载
		RefreshCache();

		// 监听文件变化
		_watcher = new FileSystemWatcher(_path) {
			Filter = "*.lua",
			IncludeSubdirectories = true,
			NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
						 | NotifyFilters.DirectoryName,
			EnableRaisingEvents = true
		};

		// 变化时自动刷新缓存
		_watcher.Created += (s, e) => RefreshCache();
		_watcher.Deleted += (s, e) => RefreshCache();
		_watcher.Renamed += (s, e) => RefreshCache();
		// 可选: 内容修改也刷新
		// _watcher.Changed += (s, e) => RefreshCache();
	}

	public string[] LuaRelativePaths {
		get {
			lock (_lock) {
				return _cachedPaths;
			}
		}
	}

	public string ReadLuaFile(string relativePath) {
		string fullPath = Path.Combine(_path, relativePath);
		return File.ReadAllText(fullPath);
	}

	private void RefreshCache() {
		lock (_lock) {
			try {
				if (Directory.Exists(_path)) {
					_cachedPaths = Directory.GetFiles(_path, "*.lua", SearchOption.AllDirectories)
						.Select(f => Path.GetRelativePath(_path, f))
						.ToArray();
				}
			} catch (Exception ex) {
				// 日志记录
				Console.WriteLine($"Failed to refresh Lua cache: {ex.Message}");
			}
		}
	}

	public void Dispose() {
		_watcher?.Dispose();
	}
}

