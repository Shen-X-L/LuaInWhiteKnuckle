using LuaInWhiteKnuckle.Core;
using MoonSharp.Interpreter;
using Steamworks.Ugc;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LuaInWhiteKnuckle.Api;

[LuaApi("Item")]
[MoonSharpUserData]
public class ItemApi {
	public Item GetItem(string prefabName) {
		GameObject itemPrefab = CL_AssetManager.GetAssetGameObject(prefabName);
		var item = itemPrefab.GetComponent<Item_Object>()?.itemData;
		if (item == null) {
			Plugin.LogError($"[LuaInWK] ItemApi: Item data not found for prefab '{prefabName}'");
			return null;
		}
		return item;
	}

	public bool isItemExist(string prefabName) {
		GameObject itemPrefab = CL_AssetManager.GetAssetGameObject(prefabName);
		var item = itemPrefab.GetComponent<Item_Object>()?.itemData;
		return item != null;
	}
}

[LuaData(typeof(Item))]
[MoonSharpUserData]
public class ItemData {
	private readonly Item _item;

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

	[MoonSharpHidden]
	public ItemData(Item item) {
		_item = item;
	}

	[MoonSharpHidden]
	public Item Raw => _item;
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
}