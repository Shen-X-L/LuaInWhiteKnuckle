using LuaInWhiteKnuckle.Core;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LuaInWhiteKnuckle.Api;

[LuaApiAttribute("Item")]
[MoonSharpUserData]
public class ItemApi {
	public ItemDataApi GetItem(string prefabName) {
		GameObject itemPrefab = CL_AssetManager.GetAssetGameObject(prefabName);
		var item = itemPrefab.GetComponent<Item_Object>()?.itemData;
		if (item == null) {
			Plugin.LogError($"[LuaInWK] ItemApi: Item data not found for prefab '{prefabName}'");
			return null;
		}
		return new ItemDataApi(item);
	}

	public bool isItemExist(string prefabName) {
		GameObject itemPrefab = CL_AssetManager.GetAssetGameObject(prefabName);
		var item = itemPrefab.GetComponent<Item_Object>()?.itemData;
		return item != null;
	}
}

[MoonSharpUserData]
public class ItemDataApi {
	public string itemName;	// 物品名称
	public string itemTag;	// 物品标签 (单一)
	public string[] itemTags;	// 物品标签列表
	public string prefabName;   // 预制体名称
	public ItemDataApi() {}

	public ItemDataApi(Item item) {
		itemName = item.itemName;
		itemTag = item.itemTag;
		itemTags = item.itemTags?.ToArray();
		prefabName = item.prefabName;
	}
}