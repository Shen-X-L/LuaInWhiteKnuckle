using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LuaInWhiteKnuckle.Api;
using LuaInWhiteKnuckle.Registry;
using LuaInWhiteKnuckle.Runtime;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LuaInWhiteKnuckle.Game;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
	public const string PLUGIN_GUID = "shenxl.LuaInWK";
	public const string PLUGIN_NAME = "Lua in White Knuckle";
	public const string PLUGIN_VERSION = "0.0.1";
	// 沙箱
	public static SafeLuaSandbox safeLuaSandbox = new SafeLuaSandbox();
	// 脚本文件管理器
	public static LuaFileManager luaFileManager = new LuaFileManager();
	// 任务管理器
	public static LuaTaskManager luaTaskManager;
	// 游戏监听器
	public static GameWatcherManager gameWatcherManager;
	// 日志
	internal static new ManualLogSource Logger;
	private static bool _isInitialized = false;
	// Harmony上下文
	private Harmony _harmony;

	private static ConfigEntry<bool> _needResetSendBox;
	public static bool NeedResetSendBox  { get { return _needResetSendBox.Value; } }

	// 单例引用
	public static Plugin Instance { get; set; }
	private void Awake() {
		this.gameObject.hideFlags = UnityEngine.HideFlags.HideAndDontSave;

		// 单例检查
		if (Instance != null) {
			Destroy(this);
			return;
		}
		Instance = this;

		Logger = base.Logger;
		Logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");

		_harmony = new Harmony($"{PLUGIN_GUID}");
		_harmony.PatchAll();

		PluginRegistry.Initialize();

		SceneManager.sceneLoaded += OnSceneLoaded;

		_needResetSendBox = base.Config.Bind<bool>("SendBox", "Need Reset", true, "每次死亡重开时是否重置Lua环境");
	}

	/// <summary>
	/// 当一个新场景被加载时触发
	/// </summary>
	private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
		if (scene.name == "Game-Main" || scene.name == "Playground") {
			Logger.LogInfo($"[场景监听]检测到进入主游戏场景 '{scene.name}'，开始初始化全新的 Lua 沙箱...");
			if (!_isInitialized) {
				_isInitialized = true;
				GameObject singleton = new GameObject("LuaRootObject");
				luaTaskManager = singleton.AddComponent<LuaTaskManager>();
				gameWatcherManager = singleton.AddComponent<GameWatcherManager>();
				DontDestroyOnLoad(singleton);
			}
			// 未初始化
			if (!safeLuaSandbox.IsInitialized)
				safeLuaSandbox.InitSandbox();
			// 每次重开刷新环境 && 初始化
			else if (NeedResetSendBox && safeLuaSandbox.IsInitialized)
				safeLuaSandbox.ResetSandbox();
			if (!NeedResetSendBox) {
				CommandConsole.cheatsEnabled = true;
				CommandConsole.hasCheated = true;
			}
			gameWatcherManager.enabled = true;
		} else {
			Logger.LogInfo($"[场景监听]检测到退出主游戏场景 '{scene.name}'，开始强制销毁并清空 Lua 沙箱...");
			safeLuaSandbox.CloseSandbox();
			if (_isInitialized){
				gameWatcherManager.enabled = false;
			}
			System.GC.Collect();
		}
	}

	/// <summary>
	/// 当 Mod 被卸载或游戏退出时触发
	/// </summary>
	private void OnDestroy() {
		Logger.LogError($"如果不是关闭游戏, 则mod被违规卸载");
		// 良好的习惯: Mod 被销毁时移除事件监听
		SceneManager.sceneLoaded -= OnSceneLoaded;

		// 释放文件监控句柄
		luaFileManager?.Dispose();
	}

	public static void LogInfo(string log = "") {
		Logger.LogInfo(log);
	}

	public static void LogWarning(string log = "") {
		Logger.LogWarning(log);
	}
	public static void LogError(string log = "") {
		Logger.LogError(log);
	}

	public static void LogDebug(string log = "") {
		Logger.LogDebug("[Lua Debug]" + log);
	}

	public static void LogTest(string log = "") {
		Logger.LogWarning("[Lua Test] " + log);
	}
}
