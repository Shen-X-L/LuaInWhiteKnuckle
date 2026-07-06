using HarmonyLib;
using LuaInWhiteKnuckle.Core;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Unity.Core;
using Unity.Entities.UniversalDelegates;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
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

	#region[背包物品API]

	/// <summary>
	/// 获取全背包物品
	/// </summary>
	/// <returns></returns>
	public List<Item> GetItems() {
		if (Inventory.instance == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Inventory instance is null");
			return null;
		}
		var itemApis = new List<Item>();
		foreach (var item in Inventory.instance.GetItems())
			itemApis.Add(item);

		foreach (Pouch extraPouch in Inventory.instance.extraPouches)
			foreach (var item in extraPouch.pouchItems)
				itemApis.Add(item);

		return itemApis;
	}

	/// <summary>
	/// 清空背包
	/// </summary>
	public void ClearInventory() {
		if (Inventory.instance == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Inventory instance is null");
			return;
		}
		_clearInventory(Inventory.instance);
	}

	/// <summary>
	/// 添加物品到背包
	/// </summary>
	/// <param name="item"></param>
	/// <param name="count"></param>
	public void AddItem(ItemData item, int count = 1) {
		if (item == null) return;
		if (count <= 0) return;
		if (Inventory.instance == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Inventory instance is null");
			return;
		}
		GameObject itemPrefab = CL_AssetManager.GetAssetGameObject(item.prefabName);
		if (itemPrefab == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Item prefab not found for prefab '{item.prefabName}'");
			return;
		}

		for (int i = 0; i < count; i++) {
			var pickupObj = GameObject.Instantiate(itemPrefab, new Vector3(0, 1, 0), Quaternion.identity);
			var itemObject = pickupObj.GetComponent<Item_Object>();
			Inventory.instance.AddItemToInventoryCenter(itemObject.itemData);
			itemObject.gameObject.SetActive(false);
		}
	}
	/// <summary>
	/// 添加物品到背包
	/// </summary>
	public void AddItem(string item, int count = 1) {
		if (item == null) return;
		if (count <= 0) return;
		if (Inventory.instance == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Inventory instance is null");
			return;
		}
		GameObject itemPrefab = CL_AssetManager.GetAssetGameObject(item);
		if (itemPrefab == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Item prefab not found for prefab '{item}'");
			return;
		}

		for (int i = 0; i < count; i++) {
			var pickupObj = GameObject.Instantiate(itemPrefab, new Vector3(0, 1, 0), Quaternion.identity);
			var itemObject = pickupObj.GetComponent<Item_Object>();
			Inventory.instance.AddItemToInventoryCenter(itemObject.itemData);
			itemObject.gameObject.SetActive(false);
		}
	}

	/// <summary>
	/// 移除背包物品
	/// </summary>
	public void RemoveItem(string item, int count = 1) {
		if (item == null) return;
		if (count <= 0) return;
		if (Inventory.instance == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Inventory instance is null");
			return;
		}
		var bagItems = Inventory.instance.bagItems;
		for (int i = bagItems.Count; i >= 0; i--) {
			if (bagItems[i].prefabName == item) {
				bagItems.RemoveAt(i);
				count--;
				if (count <= 0) break;
			}
		}
		Inventory.instance.CalculateEncumberance();
	}

	/// <summary>
	/// 重置背包内物品坐标
	/// </summary>
	public void ResetItemPositions() { 
	
	}
	#endregion

	#region[手中物品API]

	/// <summary>
	/// 获取手中物品
	/// </summary>
	public List<Item> GetHandItems() {
		if (Inventory.instance == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Inventory instance is null");
			return null;
		}
		var itemApis = new List<Item>();
		foreach (var item in Inventory.instance.itemHands) {
			itemApis.Add(item.currentItem);
		}
		return itemApis;
	}

	/// <summary>
	/// 添加物品到手中
	/// </summary>
	public void AddHandItem(string item, int handIndex) {
		if (item == null) return;
		if (Inventory.instance == null || ENT_Player.GetPlayer() == null) {
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
		Inventory.instance.AddItemToHand(itemObject.itemData, handIndex);
		itemObject.gameObject.SetActive(false);
	}
	/// <summary>
	/// 从手中删除物品
	/// </summary>
	public void RemoveHandItem(int handIndex) {
		if (Inventory.instance == null || ENT_Player.GetPlayer() == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Inventory instance or player is null");
			return;
		}
		Inventory.instance.DestroyItemInHand(handIndex);
	}

	#endregion

	#region[口袋物品API]

	/// <summary>
	/// 获取口袋物品
	/// </summary>
	public List<Item> GetPocketItem(int pocketIndex) {
		if (Inventory.instance == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Inventory instance is null");
			return null;
		}
		if (pocketIndex < 0 || pocketIndex >= Inventory.instance.pockets.Count) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Invalid pocket index {pocketIndex}");
			return null;
		}

		return Inventory.instance.pockets[pocketIndex].pouch.pouchItems;
	}

	/// <summary>
	/// 添加物品到口袋
	/// </summary>
	public bool AddPocketItem(int pocketIndex, string item, int count = 1) {
		if (Inventory.instance == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Inventory instance is null");
			return false;
		}
		if (pocketIndex < 0 || pocketIndex >= Inventory.instance.pockets.Count) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Invalid pocket index {pocketIndex}");
			return false;
		}
		GameObject itemPrefab = CL_AssetManager.GetAssetGameObject(item);
		if (itemPrefab == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Item prefab not found for prefab '{item}'");
			return false;
		}
		var pocket = Inventory.instance.pockets[pocketIndex];
		if (!pocket.pouch.CanAddItemToPouch(itemPrefab.GetComponent<Item_Object>().itemData)) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Cannot add item to pocket {pocketIndex}");
			return false;
		}
		for (int i = 0; i < count; i++) {
			var pickupObj = GameObject.Instantiate(itemPrefab, new Vector3(0, 1, 0), Quaternion.identity);
			var itemObject = pickupObj.GetComponent<Item_Object>();
			pocket.pouch.AddItemToPouch(itemObject.itemData, false);
			itemObject.gameObject.SetActive(false);
		}
		return true;
	}

	/// <summary>
	/// 从口袋删除物品
	/// </summary>
	public void RemovePocketItem(int pocketIndex, string item, int count = 1) {
		if (Inventory.instance == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Inventory instance is null");
			return;
		}
		if (pocketIndex < 0 || pocketIndex >= Inventory.instance.pockets.Count) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Invalid pocket index {pocketIndex}");
			return;
		}
		var pocket = Inventory.instance.pockets[pocketIndex];
		for (int i = pocket.pouch.pouchItems.Count - 1; i >= 0; i--) {
			if (pocket.pouch.pouchItems[i].prefabName == item) {
				pocket.pouch.RemoveItemFromPouch(pocket.pouch.pouchItems[i]);
				count--;
				if (count <= 0) break;
			}
		}
	}

	#endregion

	#region[小袋物品API]

	/// <summary>
	/// 获取小袋物品
	/// </summary>
	public List<Item> GetPouchItem(int pouchIndex) {
		if (Inventory.instance == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Inventory instance is null");
			return null;
		}
		if (pouchIndex < 0 || pouchIndex >= Inventory.instance.extraPouches.Count) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Invalid pouch index {pouchIndex}");
			return null;
		}
		return Inventory.instance.extraPouches[pouchIndex].pouchItems;
	}

	/// <summary>
	/// 添加物品到小袋
	/// </summary>
	public bool AddPouchItem(int pouchIndex, string item, int count = 1) {
		if (Inventory.instance == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Inventory instance is null");
			return false;
		}
		if (pouchIndex < 0 || pouchIndex >= Inventory.instance.extraPouches.Count) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Invalid pouch index {pouchIndex}");
			return false;
		}
		GameObject itemPrefab = CL_AssetManager.GetAssetGameObject(item);
		if (itemPrefab == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Item prefab not found for prefab '{item}'");
			return false;
		}
		var pouch = Inventory.instance.extraPouches[pouchIndex];
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
		if (Inventory.instance == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Inventory instance is null");
			return;
		}
		if (pouchIndex < 0 || pouchIndex >= Inventory.instance.extraPouches.Count) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Invalid pouch index {pouchIndex}");
			return;
		}
		var pouch = Inventory.instance.extraPouches[pouchIndex];
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
	public int pouchCount { get => Inventory.instance?.pouchCount ?? 0; }
	// 小袋容量
	public int pouchMaxCapacity {
		get => Inventory.instance == null ? 0 : _pouchMaxCapacityRef(Inventory.instance);
		set { if (Inventory.instance != null) _pouchMaxCapacityRef(Inventory.instance) = value; }
	}
	// 当前负重系数
	public float encumberance {
		get => Inventory.instance?.encumberance ?? 0;
		set => Inventory.instance?.encumberance = value;
	}
	// 最大超重物品数
	public int maxEncumberedItems {
		get => Inventory.instance?.maxEncumberedItems ?? 0;
		set => Inventory.instance?.maxEncumberedItems = value;
	}
	// 无惩罚物品数
	public int unencumberedItems {
		get => Inventory.instance?.unencumberedItems ?? 0;
		set => Inventory.instance?.unencumberedItems = value;
	}

	// 额外小袋列表
	public List<Pouch> extraPouches => Inventory.instance?.extraPouches ?? new List<Pouch>();

	// 添加一个小袋
	public void AddExtraPouch() {
		if (Inventory.instance == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Inventory instance is null");
			return;
		}
		Inventory.instance.AddExtraPouch();
	}

	// 移除一个小袋
	public void RemoveExtraPouch() {
		if (Inventory.instance == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Inventory instance is null");
			return;
		}
		Inventory.instance.RemoveExtraPouch();
	}

	#endregion
}

#endregion

#region[数据类]

[LuaData(typeof(Pouch))]
[MoonSharpUserData]
public class PouchData {
	private readonly Pouch _pouch;

	public PouchData() {
		_pouch = new Pouch();
	}

	[MoonSharpHidden]
	public PouchData(Pouch pouch) {
		_pouch = pouch;
	}

	[MoonSharpHidden]
	public Pouch Raw => _pouch;
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
		Plugin.LogTest("InventoryMonitor.Tick");

		if (Inventory.instance == null) return;

		// 清空当前帧缓存
		_currentItems.Clear();
		// 构建Dict
		foreach (var item in Inventory.instance.GetItems()) {
			_currentItems.TryGetValue(item.prefabName, out int count);
			_currentItems[item.prefabName] = count + 1;
		}

		foreach (Pouch extraPouch in Inventory.instance.extraPouches)
			foreach (var item in extraPouch.pouchItems) {
				_currentItems.TryGetValue(item.prefabName, out int count);
				_currentItems[item.prefabName] = count + 1;
			}

		// 对比
		foreach (var kvp in _currentItems) {
			if (_lastItems.TryGetValue(kvp.Key, out int last)) {
				int delta = kvp.Value - last;
				// 如果当前帧的物品数量大于上一帧的物品数量，则触发OnInventoryAdd事件
				if (delta > 0)
					ModEventBus.TriggerEvent("OnInventoryAdd", kvp.Key, delta, kvp.Value);
				// 如果当前帧的物品数量小于上一帧的物品数量，则触发OnInventoryRemove事件
				else if (delta < 0)
					ModEventBus.TriggerEvent("OnInventoryRemove", kvp.Key, -delta, kvp.Value);
			} else {
				// 如果上一帧没有该物品，则触发OnInventoryAdd事件
				ModEventBus.TriggerEvent("OnInventoryAdd", kvp.Key, kvp.Value, kvp.Value);
			}
		}
		foreach (var kvp in _lastItems) {
			// 如果上一帧的物品数量大于当前帧的物品数量，并且当前帧没有该物品，则触发OnInventoryRemove事件
			if (!_currentItems.ContainsKey(kvp.Key))
				ModEventBus.TriggerEvent("OnInventoryRemove", kvp.Key, kvp.Value, 0);
		}

		(_lastItems, _currentItems) = (_currentItems, _lastItems);
	}
}

public class HandItemMonitor : IWatcher {
	public int EnableCount { get; set; }
	public IReadOnlyList<string> Events { get; } = ["OnHandItemChange"];
	public float NextUpdateTime { get; set; }
	public float Interval { get { return 0.2f; } }
	private string[] _lastItems = new string[2];
	private string[] _currentItems = new string[2];
	public void Tick() {
		Plugin.LogTest("HandItemMonitor.Tick");

		if (Inventory.instance == null) return;

		// 构建Dict
		_currentItems[0] = Inventory.instance.itemHands[0].currentItem?.prefabName;
		_currentItems[1] = Inventory.instance.itemHands[1].currentItem?.prefabName;

		// 对比
		if (_currentItems[0] != _lastItems[0])
			ModEventBus.TriggerEvent("OnHandItemChange", "Hand0", _currentItems[0], _lastItems[0]);
		if (_currentItems[1] != _lastItems[1])
			ModEventBus.TriggerEvent("OnHandItemChange", "Hand1", _currentItems[1], _lastItems[1]);

		(_lastItems, _currentItems) = (_currentItems, _lastItems);
	}
}

#endregion

#region[补丁类]

[HarmonyPatch(typeof(Inventory))]
public class Patch_Inventory {
	// 无限小袋
	[HarmonyPatch(nameof(Inventory.AddExtraPouch))]
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> Transpiler_Add(IEnumerable<CodeInstruction> instructions) {
		bool foundAnchor = false;

		foreach (var inst in instructions) {
			if (!foundAnchor) {
				// 定位锚点：找第一个加载 bagHandler 字段的指令
				if (inst.opcode == OpCodes.Ldfld && inst.operand is FieldInfo field && field.Name == "bagHandler") {
					foundAnchor = true;
					// 所以我们在这里手动注入底层的 IL 指令： this.pouchCount = this.pouchCount + 1;

					yield return new CodeInstruction(OpCodes.Ldarg_0); // this
					yield return new CodeInstruction(OpCodes.Ldarg_0); // this
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Inventory), "pouchCount")); // 取出 pouchCount
					yield return new CodeInstruction(OpCodes.Ldc_I4_1); // 压入常量 1
					yield return new CodeInstruction(OpCodes.Add); // 加法
					yield return new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(Inventory), "pouchCount")); // 存回 pouchCount

					// 最后，补回原本紧跟在 bagHandler 前面的那句 ldarg.0 (因为我们从方法开头一直丢弃到了现在的 inst)
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					// 产出当前的 ldfld bagHandler，原版流程继续
					yield return inst;
				}
				// 如果还没找到锚点，什么都不 yield，这就实现了物理意义上的“直接跳过执行”
			} else {
				// 找到锚点之后的所有后续动画和UI刷新指令，正常放行
				yield return inst;
			}
		}
	}
}
#endregion