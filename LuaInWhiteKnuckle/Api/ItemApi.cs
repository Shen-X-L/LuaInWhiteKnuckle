using LuaInWhiteKnuckle.Registry;
using LuaInWhiteKnuckle.Game;
using MoonSharp.Interpreter;
using Steamworks.Ugc;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using HarmonyLib;

namespace LuaInWhiteKnuckle.Api;

[LuaApi("Item")]
[MoonSharpUserData]
public class ItemApi {
	/// <summary>
	/// 通过 预制体名称 创建物品
	/// </summary>
	/// <param name="prefabName"></param>
	/// <returns></returns>
	public Item GetItem(string prefabName) {
		GameObject itemPrefab = CL_AssetManager.GetAssetGameObject(prefabName);
		var item = itemPrefab.GetComponent<Item_Object>()?.itemData;
		if (item == null) {
			Plugin.LogError($"[LuaInWK] ItemApi: Item data not found for prefab '{prefabName}'");
			return null;
		}
		return item;
	}

	/// <summary>
	/// 验证该 预制体 是否是 物品
	/// </summary>
	/// <param name="prefabName"></param>
	/// <returns></returns>
	public bool isItemExist(string prefabName) {
		GameObject itemPrefab = CL_AssetManager.GetAssetGameObject(prefabName);
		var item = itemPrefab.GetComponent<Item_Object>()?.itemData; 
		itemPrefab = null;
		return item != null;
	}
}

#region[数据类]

[LuaData(typeof(Item))]
[MoonSharpUserData]
public class ItemData {
	private readonly Item _item;

	[MoonSharpHidden]
	public ItemData(Item item) {
		_item = item;
	}

	[MoonSharpHidden]
	public Item Raw => _item;

	public ItemData() {
		_item = new Item();
	}

	public ItemData(string prefabName) {
		GameObject itemPrefab = CL_AssetManager.GetAssetGameObject(prefabName);
		if (itemPrefab == null) {
			Plugin.LogError($"[LuaInWK] InventoryApi: Item prefab not found for prefab '{prefabName}'");
			_item = new Item();
			return;
		}
		var pickupObj = GameObject.Instantiate(itemPrefab, new Vector3(0, 1, 0), Quaternion.identity);
		var itemObject = pickupObj.GetComponent<Item_Object>();
		_item = itemObject.itemData;
	}

	// 物品名称
	public string itemName {
		get => _item.itemName;
		set => _item.itemName = value;
	}
	// 物品标签 (单一)
	public string itemTag {
		get => _item.itemTag;
		set => _item.itemTag = value;
	}
	// 物品标签列表
	public List<string> itemTags => _item.itemTags;
	// 预制体名称
	public string prefabName => _item.prefabName;
	// 物品重量
	public float itemWeight {
		get => _item.itemWeight;
		set => _item.itemWeight = value;
	}
	// 丢弃时投掷速度
	public float dropVel {
		get => _item.dropVel;
		set => _item.dropVel = value;
	}
	// 是否可放入口袋
	public bool pocketable {
		get => _item.pocketable;
		set => _item.pocketable = value;
	}
	// 是否可放入小袋
	public bool pouchable {
		get => _item.pouchable;
		set => _item.pouchable = value;
	}
	// 价值 (蟑螂数)
	public int worth {
		get => _item.worth;
		set => _item.worth = value;
	}
	// 背包中的位置
	public Vector3 bagPosition {
		get => _item.bagPosition;
		set => _item.bagPosition = value;
	}
	// 背包中的旋转
	public Quaternion bagRotation {
		get => _item.bagRotation;
		set => _item.bagRotation = value;
	}
	// 是否在背包/手中
	public bool inInventory => _item.inventory != null;
	// 是否在背包中
	public bool inBag => _item.InBag();
	// 是否在手中
	public bool inhand => Patch_Item._handItemField(_item) != null;
	// 销毁
	public void Destroy(bool clearFromInventory = true) => _item.Destroy(clearFromInventory);
}

[LuaData(typeof(HandItem))]
[MoonSharpUserData]
public class HandItemData{
	private readonly HandItem _handItem;

	[MoonSharpHidden]
	public HandItemData(HandItem handItem) {
		_handItem = handItem;
	}

	[MoonSharpHidden]
	public HandItem Raw => _handItem;
}

#endregion

#region[监听器/补丁类]

[HarmonyPatch(typeof(Item))]
public static class Patch_Item {
	public static readonly AccessTools.FieldRef<Item, HandItem> _handItemField =
		AccessTools.FieldRefAccess<Item, HandItem>("handItem");
}

#endregion