using HarmonyLib;
using LuaInWhiteKnuckle.Core;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Core;
using Unity.Entities.UniversalDelegates;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static Inventory;

namespace LuaInWhiteKnuckle.Api;

[LuaApiAttribute("Inventory")]
[MoonSharpUserData]
public class InventoryApi {
	// 清空背包
	private delegate void ClearInventoryDelegate(Inventory instance);
	private static readonly ClearInventoryDelegate _clearInventory =
		AccessTools.MethodDelegate<ClearInventoryDelegate>(
			AccessTools.Method(typeof(Inventory), "ClearInventory"));
	/// <summary>
	/// 获取全背包物品
	/// </summary>
	/// <returns></returns>
	public ItemData[] GetItems() {
		if (Inventory.instance == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Inventory instance is null");
			return Array.Empty<ItemData>();
		}
		var itemApis = new List<ItemData>();
		foreach (var item in Inventory.instance.GetItems())
			itemApis.Add(new ItemData(item));

		foreach (Pouch extraPouch in Inventory.instance.extraPouches)
			foreach (var item in extraPouch.pouchItems)
				itemApis.Add(new ItemData(item));

		return itemApis.ToArray();
	}
	/// <summary>
	/// 获取手中物品
	/// </summary>
	public ItemData[] GetHandItems() {
		if (Inventory.instance == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Inventory instance is null");
			return Array.Empty<ItemData>();
		}
		var itemApis = new ItemData[2];
		itemApis[0] = new ItemData(Inventory.instance.itemHands[0].currentItem);
		itemApis[1] = new ItemData(Inventory.instance.itemHands[1].currentItem);
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
	/// 添加物品到手中
	/// </summary>
	public void AddItemToHand(string item, int handIndex) {
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
	public void RemoveItemFromHand(int handIndex) {
		if (Inventory.instance == null || ENT_Player.GetPlayer() == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Inventory instance or player is null");
			return;
		}
		Inventory.instance.DestroyItemInHand(handIndex);
	}
}

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
		Plugin.LogDebug("InventoryMonitor.Tick");

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
	public IReadOnlyList<string> Events { get; } = [ "OnHandItemChange" ];
	public float NextUpdateTime { get; set; }
	public float Interval { get { return 0.2f; } }
	private string[] _lastItems = new string[2];
	private string[] _currentItems = new string[2];
	public void Tick() {
		Plugin.LogDebug("HandItemMonitor.Tick");

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