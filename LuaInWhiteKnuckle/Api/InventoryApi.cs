using HarmonyLib;
using LuaInWhiteKnuckle.Game;
using LuaInWhiteKnuckle.Registry;
using LuaInWhiteKnuckle.Runtime;
using MoonSharp.Interpreter;
using Newtonsoft.Json.Bson;
using Steamworks.Ugc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Unity.VisualScripting;
using UnityEngine;
using static Inventory;

namespace LuaInWhiteKnuckle.Api;

#region[API类]

[LuaApi("Inventory")]
[MoonSharpUserData]
public class InventoryApi {
	// 清空背包
	private delegate void ClearInventoryDelegate(Inventory instance);
	private static readonly ClearInventoryDelegate _clearInventory =
		AccessTools.MethodDelegate<ClearInventoryDelegate>(
			AccessTools.Method(typeof(Inventory), "ClearInventory"));
	private static readonly AccessTools.FieldRef<Inventory, int> _pouchMaxCapacityRef =
		AccessTools.FieldRefAccess<Inventory, int>("pouchMaxCapacity");

	[MoonSharpHidden]
	internal static Inventory _inventory = null;

	#region[背包物品API]

	/// <summary>
	/// 通过物品名称获取物品 搜寻顺序背包-口袋-双手
	/// </summary>
	public Item GetItem(string itemName) => _inventory.GetItem(itemName);

	/// <summary>
	/// 通过预制体名称获取物品 搜寻顺序背包-口袋-双手
	/// </summary>
	public Item GetItemByPrefab(string prefabName) => _inventory.GetItemByPrefab(prefabName);

	/// <summary>
	/// 通过预制体名称获取对应全物品 搜寻顺序背包-口袋-双手
	/// </summary>
	public List<Item> GetItemsByPrefab(string prefabName, bool includeHands = true, bool includeBag = true) =>
		new List<Item>(_inventory.GetItemsByPrefab(prefabName, includeHands, includeBag));

	/// <summary>
	/// 获取全背包物品
	/// </summary>
	/// <returns></returns>
	public List<Item> GetItems(bool checkBag = true, bool checkHands = true, bool checkPouches = true) =>
		new List<Item>(_inventory.GetItems(checkBag, checkHands, checkPouches));

	/// <summary>
	/// 清空背包
	/// </summary>
	public void ClearInventory() => _clearInventory(_inventory);

	/// <summary>
	/// 添加物品到背包
	/// </summary>
	public void AddItem(ItemData itemData, int count = 1) => AddItem(itemData.Raw, count);
	public void AddItem(Item item, int count = 1) {
		if (item == null) return;
		if (count <= 0) return;
		if (_inventory == null) return;
		GameObject itemPrefab = CL_AssetManager.GetAssetGameObject(item.prefabName);
		if (itemPrefab == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Item prefab not found for prefab '{item.prefabName}'");
			return;
		}

		for (int i = 0; i < count; i++) {
			var pickupObj = GameObject.Instantiate(itemPrefab, new Vector3(0, 1, 0), Quaternion.identity);
			var itemObject = pickupObj.GetComponent<Item_Object>();
			_inventory.AddItemToInventoryCenter(itemObject.itemData);
			itemObject.gameObject.SetActive(false);
		}
	}

	/// <summary>
	/// 添加物品到背包
	/// </summary>
	public void AddItem(string item, int count = 1) {
		if (item == null) return;
		if (count <= 0) return;
		if (_inventory == null) return;

		GameObject itemPrefab = CL_AssetManager.GetAssetGameObject(item);
		if (itemPrefab == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Item prefab not found for prefab '{item}'");
			return;
		}

		for (int i = 0; i < count; i++) {
			var pickupObj = GameObject.Instantiate(itemPrefab, new Vector3(0, 1, 0), Quaternion.identity);
			var itemObject = pickupObj.GetComponent<Item_Object>();
			_inventory.AddItemToInventoryCenter(itemObject.itemData);
			itemObject.gameObject.SetActive(false);
		}
	}

	/// <summary>
	/// 移除背包物品
	/// </summary>
	public void RemoveItem(string item, int count = 1) {
		if (item == null) return;
		if (count <= 0) return;
		if (_inventory == null) return;
		var bagItems = _inventory.bagItems;
		for (int i = bagItems.Count; i >= 0; i--) {
			if (bagItems[i].prefabName == item) {
				bagItems.RemoveAt(i);
				count--;
				if (count <= 0) break;
			}
		}
		_inventory.CalculateEncumberance();
	}

	/// <summary>
	/// 重置背包内物品坐标
	/// </summary>
	public void ResetItemPositions() {
		var items = _inventory.GetItems(checkBag: true, checkHands: false, checkPouches: false);
		int i = 0;              // 步长
		int x = 0, y = 0;       // 坐标
		int stepLength = 1;     // 当前该走的步长
		int stepCounter = 0;    // 在相同步长下已经走了几次（取0/1）
		int remaining = 1;      // 当前方向剩余步数
		int dir = 0;            // 0右 1上 2左 3下

		for (; i < items.Count - 1; ++i) {
			var item = items[i];
			_inventory.AddItemToInventoryScreen(
				new Vector3(x * 0.05f, y * 0.05f, 1f),
				item, localSpacePosition: true, pushItems: false);
			SpiralCoordinate();
		}
		_inventory.AddItemToInventoryScreen(
			new Vector3(x * 0.05f, y * 0.05f, 1f),
			items[i], localSpacePosition: true, pushItems: true);

		void SpiralCoordinate() {
			if (i == 0) return;
			// 走一步
			switch (dir) {
				case 0: x++; break;   // 右
				case 1: y++; break;   // 上
				case 2: x--; break;   // 左
				case 3: y--; break;   // 下
			}
			remaining--;
			// 当前方向走完, 切换方向
			if (remaining == 0) {
				dir = (dir + 1) & 0b11;       // 下一方向
				stepCounter++;
				// 每两次同长度走完后, 步长+1
				if (stepCounter == 2) {
					stepCounter = 0;
					stepLength++;
				}
				remaining = stepLength;    // 新方向的步数
			}
		}

	}

	// 丢弃物品
	public void DropItemIntoWorld(Item item, Vector3 pos) => _inventory.DropItemIntoWorld(item, pos);
	public void DropItemIntoWorld(ItemData itemData, Vector3 pos) => _inventory.DropItemIntoWorld(itemData.Raw, pos);

	/// <summary>
	/// 丢弃全部物品
	/// </summary>
	public void DropAllItem(Vector3 pos) {
		var items = new List<Item>(_inventory.GetItems());
		foreach (var item in items) _inventory.DropItemIntoWorld(item, pos);
	}
	#endregion

	#region[手中物品API]

	/// <summary>
	/// 获取手中物品
	/// </summary>
	public List<Item> GetHandItems() {
		var itemApis = new List<Item>();
		foreach (var item in _inventory.itemHands) {
			itemApis.Add(item.currentItem);
		}
		return itemApis;
	}

	/// <summary>
	/// 添加物品到手中
	/// </summary>
	public void AddHandItem(string item, int handIndex) {
		if (item == null) return;
		if (_inventory == null || ENT_Player.GetPlayer() == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Inventory instance or player is null");
			return;
		}
		GameObject itemPrefab = CL_AssetManager.GetAssetGameObject(item);
		if (itemPrefab == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Item prefab not found for prefab '{item}'");
			return;
		}
		var pickupObj = GameObject.Instantiate(itemPrefab, new Vector3(0, 1, 0), Quaternion.identity);
		var itemObject = pickupObj.GetComponent<Item_Object>();
		_inventory.AddItemToHand(itemObject.itemData, handIndex);
		itemObject.gameObject.SetActive(false);
	}

	// 从手中删除物品
	public void RemoveHandItem(int handIndex) => _inventory.DestroyItemInHand(handIndex);

	// 将手中物品放入背包.
	public void MoveItemFromHandToInventory(int handID) =>
		_inventory.AddItemToInventoryScreen(
			new Vector3(0f, 0f, 1f) + UnityEngine.Random.insideUnitSphere * 0.01f,
			_inventory.itemHands[handID].currentItem,
			localSpacePosition: true
		);

	// 丢弃手中物品
	public void DropItem(int handIndex) {
		var hands = ENT_Player.GetPlayer().hands;
		if (handIndex == -1) {
			foreach (var hand in hands)
				hand.DropItem();
			return;
		}
		if (handIndex < hands.Length)
			hands[handIndex].DropItem();
	}

	public void TryPocketItemInHand(int handIndex) => _inventory.TryPocketItemInHand(handIndex);

	#endregion

	#region[口袋物品API]

	/// <summary>
	/// 获取口袋物品
	/// </summary>
	public List<Item> GetPocketItem(int pocketIndex) {
		if (_inventory == null)
			return null;

		if (pocketIndex < 0 || pocketIndex >= _inventory.pockets.Count) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Invalid pocket index {pocketIndex}");
			return null;
		}

		return new List<Item>(_inventory.pockets[pocketIndex].pouch.pouchItems);
	}

	/// <summary>
	/// 添加物品到口袋
	/// </summary>
	public bool AddPocketItem(int pocketIndex, string item, int count = 1) {
		if (_inventory == null) return false;

		if (pocketIndex < 0 || pocketIndex >= _inventory.pockets.Count) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Invalid pocket index {pocketIndex}");
			return false;
		}
		GameObject itemPrefab = CL_AssetManager.GetAssetGameObject(item);
		if (itemPrefab == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Item prefab not found for prefab '{item}'");
			return false;
		}
		var pocket = _inventory.pockets[pocketIndex];
		if (!pocket.pouch.CanAddItemToPouch(itemPrefab.GetComponent<Item_Object>().itemData)) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Cannot add item to pocket {pocketIndex}");
			return false;
		}
		for (int i = 0; i < count; i++) {
			var pickupObj = GameObject.Instantiate(itemPrefab, new Vector3(0, 1, 0), Quaternion.identity);
			var itemObject = pickupObj.GetComponent<Item_Object>();
			var itemData = itemObject.itemData;
			// 添加到背包
			_inventory.AddItemToInventoryScreen(Vector3.zero, itemData, localSpacePosition: false, useStoredValues: false, pushItems: false);
			// 添加到口袋并重置坐标
			pocket.pouch.AddItemToPouch(itemObject.itemData, pushItemsApart: false);
			itemData.GetDropObject().transform.position = pocket.transform.position;
			itemObject.gameObject.SetActive(false);
		}
		return true;
	}

	/// <summary>
	/// 从口袋删除物品
	/// </summary>
	public void RemovePocketItem(int pocketIndex, string item = "", int count = 1) {
		if (_inventory == null) return;
		if (pocketIndex < 0 || pocketIndex >= _inventory.pockets.Count) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Invalid pocket index {pocketIndex}");
			return;
		}
		var pocket = _inventory.pockets[pocketIndex];
		for (int i = pocket.pouch.pouchItems.Count - 1; i >= 0; i--) {
			var itemData = pocket.pouch.pouchItems[i];
			if (itemData.prefabName == item || item == "") {
				pocket.pouch.RemoveItemFromPouch(itemData);
				if (_inventory.bagItems.Contains(itemData)) _inventory.bagItems.Remove(itemData);
				count--;
				if (count <= 0) break;
			}
		}
	}

	/// <summary>
	/// 口袋对应小袋
	/// </summary>
	public Pouch PocketPouch(int pocketIndex) {
		if (_inventory == null) return null;

		if (pocketIndex < 0 || pocketIndex >= _inventory.pockets.Count) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Invalid pocket index {pocketIndex}");
			return null;
		}
		return _inventory.pockets[pocketIndex].pouch;
	}

	#endregion

	#region[小袋物品API]

	/// <summary>
	/// 获取小袋物品
	/// </summary>
	public List<Item> GetPouchItem(int pouchIndex) {
		if (_inventory == null) return null;

		if (pouchIndex < 0 || pouchIndex >= _inventory.extraPouches.Count) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Invalid pouch index {pouchIndex}");
			return null;
		}

		return new List<Item>(_inventory.extraPouches[pouchIndex].pouchItems);
	}

	/// <summary>
	/// 添加物品到小袋
	/// </summary>
	public bool AddPouchItem(int pouchIndex, string item, int count = 1) {
		if (_inventory == null) return false;

		if (pouchIndex < 0 || pouchIndex >= _inventory.extraPouches.Count) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Invalid pouch index {pouchIndex}");
			return false;
		}
		GameObject itemPrefab = CL_AssetManager.GetAssetGameObject(item);
		if (itemPrefab == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Item prefab not found for prefab '{item}'");
			return false;
		}
		var pouch = _inventory.extraPouches[pouchIndex];
		if (!pouch.CanAddItemToPouch(itemPrefab.GetComponent<Item_Object>().itemData)) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Cannot add item to pouch {pouchIndex}");
			return false;
		}
		for (int i = 0; i < count; i++) {
			var pickupObj = GameObject.Instantiate(itemPrefab, new Vector3(0, 1, 0), Quaternion.identity);
			var itemObject = pickupObj.GetComponent<Item_Object>();
			pouch.AddItemToPouch(itemObject.itemData, false);
			itemObject.gameObject.SetActive(false);
		}
		return true;
	}

	/// <summary>
	/// 从小袋删除物品
	/// </summary>
	public void RemovePouchItem(int pouchIndex, string item, int count = 1) {
		if (_inventory == null) return;

		if (pouchIndex < 0 || pouchIndex >= _inventory.extraPouches.Count) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Invalid pouch index {pouchIndex}");
			return;
		}
		var pouch = _inventory.extraPouches[pouchIndex];
		for (int i = pouch.pouchItems.Count - 1; i >= 0; i--) {
			if (pouch.pouchItems[i].prefabName == item) {
				pouch.RemoveItemFromPouch(pouch.pouchItems[i]);
				count--;
				if (count <= 0) break;
			}
		}
	}

	#endregion

	#region[背包属性API]

	// 小袋数量
	public int pouchCount { get => _inventory.pouchCount; }
	// 小袋容量
	public int pouchMaxCapacity {
		get => _pouchMaxCapacityRef(_inventory);
		set => _pouchMaxCapacityRef(_inventory) = value;
	}
	// 当前负重系数
	public float encumberance {
		get => _inventory.encumberance;
		set => _inventory.encumberance = value;
	}
	// 最大超重物品数
	public int maxEncumberedItems {
		get => _inventory.maxEncumberedItems;
		set => _inventory.maxEncumberedItems = value;
	}
	// 无惩罚物品数
	public int unencumberedItems {
		get => _inventory.unencumberedItems;
		set => _inventory.unencumberedItems = value;
	}

	// 额外小袋列表
	public List<Pouch> extraPouches => _inventory.extraPouches ?? new List<Pouch>();

	// 添加一个小袋
	public void AddExtraPouch() => _inventory.AddExtraPouch();


	// 移除一个小袋
	public void RemoveExtraPouch() => _inventory.RemoveExtraPouch();


	#endregion
}

#endregion

#region[数据类]

[LuaData(typeof(Pouch))]
[MoonSharpUserData]
public class PouchData {
	private readonly Pouch _pouch;

	[MoonSharpHidden]
	public PouchData(Pouch pouch) {
		_pouch = pouch;
	}

	[MoonSharpHidden]
	public Pouch Raw => _pouch;

	public PouchData() {
		_pouch = new Pouch();
	}

	// 小袋中的物品
	public List<Item> pouchItems => _pouch.pouchItems;
	// 最大容量
	public int maxCapacity {
		get => _pouch.maxCapacity;
		set => _pouch.maxCapacity = value;
	}
	// 最大大物品容量
	public int maxLargeCapacity {
		get => _pouch.maxLargeCapacity;
		set => _pouch.maxLargeCapacity = value;
	}
	// 是否允许不可袋装物品
	public bool allowNonPouchable {
		get => _pouch.allowNonPouchable;
		set => _pouch.allowNonPouchable = value;
	}
}

#endregion

#region[监听器类]

#region[	背包监听器]

public class InventoryMonitor : IWatcher {
	public int EnableCount { get; set; }
	public IReadOnlyList<string> Events { get; } = [
		"OnInventoryAdd",
		"OnInventoryRemove"
	];
	public float NextUpdateTime { get; set; }
	public float Interval { get { return 0.2f; } }
	private Dictionary<string, int> _lastItems = new();
	private Dictionary<string, int> _currentItems = new();
	public void Tick() {
		if (InventoryApi._inventory == null) return;

		// 清空当前帧缓存
		_currentItems.Clear();
		// 构建Dict
		foreach (var item in InventoryApi._inventory.GetItems()) {
			_currentItems.TryGetValue(item.prefabName, out int count);
			_currentItems[item.prefabName] = count + 1;
		}

		foreach (Pouch extraPouch in InventoryApi._inventory.extraPouches)
			foreach (var item in extraPouch.pouchItems) {
				_currentItems.TryGetValue(item.prefabName, out int count);
				_currentItems[item.prefabName] = count + 1;
			}

		// 对比
		foreach (var kvp in _currentItems) {
			if (_lastItems.TryGetValue(kvp.Key, out int last)) {
				int delta = kvp.Value - last;
				// 如果当前帧的物品数量大于上一帧的物品数量, 则触发OnInventoryAdd事件
				if (delta > 0)
					ModEventBus.TriggerEvent("OnInventoryAdd", kvp.Key, delta, kvp.Value);
				// 如果当前帧的物品数量小于上一帧的物品数量, 则触发OnInventoryRemove事件
				else if (delta < 0)
					ModEventBus.TriggerEvent("OnInventoryRemove", kvp.Key, -delta, kvp.Value);
			} else {
				// 如果上一帧没有该物品, 则触发OnInventoryAdd事件
				ModEventBus.TriggerEvent("OnInventoryAdd", kvp.Key, kvp.Value, kvp.Value);
			}
		}
		foreach (var kvp in _lastItems) {
			// 如果上一帧的物品数量大于当前帧的物品数量, 并且当前帧没有该物品, 则触发OnInventoryRemove事件
			if (!_currentItems.ContainsKey(kvp.Key))
				ModEventBus.TriggerEvent("OnInventoryRemove", kvp.Key, kvp.Value, 0);
		}

		(_lastItems, _currentItems) = (_currentItems, _lastItems);
	}
}

#endregion

#region[	手部物品监听器]

public class HandItemMonitor : IWatcher {
	public int EnableCount { get; set; }
	public IReadOnlyList<string> Events { get; } = ["OnHandItemChange"];
	public float NextUpdateTime { get; set; }
	public float Interval { get { return 0.2f; } }
	private Item[] _lastItems = new Item[2];
	private string[] _lastItemsPrefab = new string[2];
	private Item[] _currentItems = new Item[2];
	private string[] _currentItemsPrefab = new string[2];
	public void Tick() {
		if (InventoryApi._inventory == null) return;

		// 构建Dict
		_currentItems[0] = InventoryApi._inventory.itemHands[0].currentItem;
		_currentItems[1] = InventoryApi._inventory.itemHands[1].currentItem;

		// 可能还是同一个Item 但是不同状态 (如食物 perfab不同)
		_currentItemsPrefab[0] = _currentItems[0]?.prefabName ?? "None";
		_currentItemsPrefab[1] = _currentItems[1]?.prefabName ?? "None";

		// 对比
		if (_currentItems[0] != _lastItems[0]|| _currentItemsPrefab[0]!= _lastItemsPrefab[0]) {
			ModEventBus.TriggerEvent("OnHandItemChange", 0, _lastItems[0], _currentItems[0]);
		}
		if (_currentItems[1] != _lastItems[1] || _currentItemsPrefab[1] != _lastItemsPrefab[1]) {
			ModEventBus.TriggerEvent("OnHandItemChange", 1, _lastItems[1], _currentItems[1]);
		}

		(_lastItems, _currentItems) = (_currentItems, _lastItems);
		(_lastItemsPrefab, _currentItemsPrefab) = (_currentItemsPrefab, _lastItemsPrefab);
	}
}

// 拦截HandItem全子类 Use StopUse
[HarmonyPatch]
public static class Patch_HandItem_Lifecycle {

	// 统一的主入口
	static IEnumerable<MethodBase> TargetMethods() {
		// 优化:只扫描包含 HandItem 的程序集和本 Mod 程序集
		var scanAssemblies = new[] { typeof(HandItem).Assembly, Assembly.GetExecutingAssembly() };

		foreach (var assembly in scanAssemblies) {
			foreach (var type in assembly.GetTypes()) {
				// 允许抽象类通过
				if (!typeof(HandItem).IsAssignableFrom(type)) continue;

				// DeclaredMethod 只抓取当前类定义的 Method
				MethodInfo use = AccessTools.DeclaredMethod(type, nameof(HandItem.Use));
				if (use != null) yield return use;

				MethodInfo stopUse = AccessTools.DeclaredMethod(type, nameof(HandItem.StopUse));
				if (stopUse != null) yield return stopUse;
			}
		}
	}

	[HarmonyPostfix]
	static void Postfix(MethodBase __originalMethod) {
		GameWatcherManager.Get<HandItemMonitor>()?.Tick();
	}
}

#endregion

#region[	口袋监听器]

public class PocketItemMonitor : IWatcher {
	public int EnableCount { get; set; }
	public IReadOnlyList<string> Events { get; } = ["OnPocketItemChange"];
	public float NextUpdateTime { get; set; }
	public float Interval { get { return 0.1f; } }
	private List<(string, int)> _lastItems = new();
	private List<(string, int)> _currentItems = new();
	public void Tick() {
		if (InventoryApi._inventory == null) return;

		// 构建口袋物品列表
		foreach (var pocket in InventoryApi._inventory.pockets) {
			var items = pocket.pouch.pouchItems;
			if (items != null && items.Count > 0)
				_currentItems.Add((items[0].prefabName, items.Count));
			else
				_currentItems.Add((null, 0));
		}

		for (int i = 0; i < _currentItems.Count && i < _lastItems.Count; ++i) {
			if (_currentItems[i] != _lastItems[i]) {
				var (oldName, oldCount) = _lastItems[i];
				var (newName, newCount) = _currentItems[i];
				ModEventBus.TriggerEvent("OnPocketItemChange",
					"Pocket" + i, oldName, oldCount, newName, newCount);
			}
		}

		(_lastItems, _currentItems) = (_currentItems, _lastItems);
	}
}

#endregion

#endregion

#region[补丁类]

[HarmonyPatch(typeof(Inventory))]
public class Patch_Inventory {
	#region[无限小袋]

	[HarmonyPatch(nameof(Inventory.AddExtraPouch))]
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> Transpiler_Add(IEnumerable<CodeInstruction> instructions) {
		// 状态机控制 0 = 寻找 Clamp, 1 = 寻找 bagHandler, 2 = 正常放行后续指令
		int locateState = 0;

		foreach (var inst in instructions) {
			// 阶段1 寻找原版的 Mathf.Clamp 调用
			if (locateState == 0) {
				if (inst.opcode == OpCodes.Call && inst.operand is MethodInfo method && method.Name == "Clamp") {
					locateState = 1; // 找到了 Clamp,切换到下一阶段
				}
				// 在找到 Clamp 之前的所有指令(包括 if >= 4 检查和 Clamp 压栈参数)一律物理丢弃
				continue;
			}

			// 阶段2 在 Clamp 之后,寻找第一个加载 bagHandler 的指令
			if (locateState == 1) {
				if (inst.opcode == OpCodes.Ldfld && inst.operand is FieldInfo field && field.Name == "bagHandler") {
					locateState = 2; // 匹配成功,进入放行状态

					// --- 开始动态注入无上限自增逻辑 this.pouchCount = this.pouchCount + 1; ---
					yield return new CodeInstruction(OpCodes.Ldarg_0); // this
					yield return new CodeInstruction(OpCodes.Ldarg_0); // this
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Inventory), "pouchCount")); // 取出 pouchCount
					yield return new CodeInstruction(OpCodes.Ldc_I4_1); // 压入常量 1
					yield return new CodeInstruction(OpCodes.Add); // 加法
					yield return new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(Inventory), "pouchCount")); // 存回 pouchCount

					// 补回原版本来紧跟在 bagHandler 前面、但被我们丢弃掉的那句 ldarg.0
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					// 产出当前的 ldfld bagHandler 指令
					yield return inst;
				}
				// 在 Clamp 到 bagHandler 之间的指令（如原版的 stfld pouchCount 和这句前面的 ldarg.0）一律丢弃
				continue;
			}

			// 阶段3 目标已经安全注入完毕,后续的所有 UI 刷新和循环分配逻辑直接原样放行
			yield return inst;
		}
	}

	[HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveExtraPouch))]
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> Transpiler_Remove(IEnumerable<CodeInstruction> instructions) {
		CodeInstruction prevInst = null;

		foreach (var inst in instructions) {
			// 1. 捕捉原版的 Mathf.Clamp 调用
			if (inst.opcode == OpCodes.Call && inst.operand is MethodInfo method && method.Name == "Clamp") {
				// 2. 检查此时留在缓冲区里的前一条指令,是不是原版的上限常数 4
				if (prevInst != null && prevInst.opcode == OpCodes.Ldc_I4_4) {
					// 3. 将 4 替换为 int.MaxValue
					prevInst.opcode = OpCodes.Ldc_I4;
					prevInst.operand = int.MaxValue;
				}
			}

			// 延迟一步输出指令,允许我们在"下一帧"偷看并修改"上一帧"的内容
			if (prevInst != null) {
				yield return prevInst;
			}
			prevInst = inst;
		}

		// 吐出最后一条残留指令
		if (prevInst != null) {
			yield return prevInst;
		}
	}

	#endregion

	#region[手部物品监听]

	[HarmonyPatch(nameof(Inventory.DropItemFromHand))]
	[HarmonyPostfix]
	public static void Patch_DropItemFromHand(bool __result) { 
		if (__result) GameWatcherManager.Get<HandItemMonitor>()?.Tick();
	}

	#endregion
}
#endregion