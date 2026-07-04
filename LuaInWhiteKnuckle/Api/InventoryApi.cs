using HarmonyLib;
using LuaInWhiteKnuckle.Core;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Core;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static Inventory;

namespace LuaInWhiteKnuckle.Api;

[LuaApiAttribute("Inventory")]
[MoonSharpUserData]
public class InventoryApi{
	// 清空背包
	private delegate void ClearInventoryDelegate(Inventory instance);
	private static readonly ClearInventoryDelegate _clearInventory =
		AccessTools.MethodDelegate<ClearInventoryDelegate>(
			AccessTools.Method(typeof(Inventory), "ClearInventory"));
	public ItemDataApi[] GetItems() {
		if (Inventory.instance == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Inventory instance is null");
			return Array.Empty<ItemDataApi>();
		}
		var itemApis = new List<ItemDataApi>();
		foreach (var item in Inventory.instance.GetItems()) 
			itemApis.Add(new ItemDataApi(item));

		foreach (Pouch extraPouch in Inventory.instance.extraPouches) 
			foreach (var item in extraPouch.pouchItems) 
				itemApis.Add(new ItemDataApi(item));
				
		return itemApis.ToArray();
	}
	
	public void ClearInventory() {
		if (Inventory.instance == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Inventory instance is null");
			return;
		}
		_clearInventory(Inventory.instance);
	}

	public void AddItem(ItemDataApi item, int count = 1) {
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
}

public class InventoryMonitor : IWatcher {
	public List<string> EventName => new List<string> { "OnInventoryAdd", "OnInventoryRemove" };
	public float NextUpdateTime { get; set; }
	public float Interval { get { return 0.2f; } }
	private Dictionary<string,int> _lastItems = new();
	private Dictionary<string, int> _currentItems = new();
	public void Tick() {
		Plugin.LogDebug("InventoryMonitor.Tick");

		if (Inventory.instance == null) return;

		// 清空当前帧缓存
		_currentItems.Clear();
		// 构建Dict
		foreach (var item in Inventory.instance.GetItems()){
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
					ModEventBus.TriggerLuaEvent("OnInventoryAdd", kvp.Key, delta, kvp.Value);
				// 如果当前帧的物品数量小于上一帧的物品数量，则触发OnInventoryRemove事件
				else if (delta < 0)
					ModEventBus.TriggerLuaEvent("OnInventoryRemove", kvp.Key, -delta, kvp.Value);
			} else {
				// 如果上一帧没有该物品，则触发OnInventoryAdd事件
				ModEventBus.TriggerLuaEvent("OnInventoryAdd", kvp.Key, kvp.Value, kvp.Value);
			}
		}
		foreach (var kvp in _lastItems) {
			// 如果上一帧的物品数量大于当前帧的物品数量，并且当前帧没有该物品，则触发OnInventoryRemove事件
			if (!_currentItems.ContainsKey(kvp.Key))
				ModEventBus.TriggerLuaEvent("OnInventoryRemove", kvp.Key, kvp.Value, 0);
		}

		(_lastItems, _currentItems) = (_currentItems, _lastItems);
	}
}