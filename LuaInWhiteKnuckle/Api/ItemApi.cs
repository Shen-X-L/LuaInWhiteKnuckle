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
	/// 通过预制体名称获取并克隆一个全新的物品实例
	/// </summary>
	/// <param name="prefabName">预制体资源名称</param>
	/// <returns>深拷贝后的 Item 实例，若未找到则返回 null</returns>
	public static Item GetItem(string prefabName) {
		// 参数防御校验
		if (string.IsNullOrEmpty(prefabName)) {
			Plugin.LogWarning("[LuaInWK] ItemApi.GetItem: prefabName is null or empty.");
			return null;
		}

		// 双重检测获取 Item_Object 组件
		Item_Object itemObject = null;
		// 优先使用专用的 Item_Object 查找机制
		Item_Object itemPrefab = CL_AssetManager.GetItemObjectPrefab(prefabName);
		if (itemPrefab == null) {
			// 备用：从通用 GameObject 中获取组件
			GameObject go = CL_AssetManager.GetAssetGameObject(prefabName);
			itemPrefab = go?.GetComponent<Item_Object>();
		}
		if (itemObject == null || itemObject.itemData == null) {
			Plugin.LogError($"[LuaInWK] ItemApi: Item_Object or itemData not found for prefab '{prefabName}'");
			return null;
		}

		// 返回深拷贝实例，防止运行时修改影响预制体模板
		return itemObject.itemData.GetClone();
	}

	/// <summary>
	/// 验证该 预制体 是否是 物品
	/// </summary>
	/// <param name="prefabName"></param>
	/// <returns></returns>
	public bool isItemExist(string prefabName) {
		Item_Object itemPrefab1 = CL_AssetManager.GetItemObjectPrefab(prefabName);
		var item1 = itemPrefab1?.itemData;
		if (item1 != null) return true;
		GameObject itemPrefab2 = CL_AssetManager.GetAssetGameObject(prefabName);
		var item2 = itemPrefab2?.GetComponent<Item_Object>()?.itemData; 
		return item2 != null;
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
	public string name {
		get => _item.itemName;
		set => _item.itemName = value;
	}
	// 物品标签 (单一)
	public string tag {
		get => _item.itemTag;
		set => _item.itemTag = value;
	}
	// 物品标签列表
	public List<string> tags => _item.itemTags;
	// 预制体名称
	public string prefab => _item.prefabName;
	// 物品重量
	public float weight {
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
	// 背包中的坐标
	public Transform bagTransform => _item.GetDropObject().transform;
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

	public Item item => _handItem.item;
	public bool active => _handItem.active;
	public bool used => _handItem.used;

	public void Use()=> _handItem.Use();
	public void StopUse()=> _handItem.StopUse();
	public void Activate() => _handItem.Activate();
}

#endregion

#region[监听器/补丁类]

[HarmonyPatch(typeof(Item))]
public static class Patch_Item {
	public static readonly AccessTools.FieldRef<Item, HandItem> _handItemField =
		AccessTools.FieldRefAccess<Item, HandItem>("handItem");
}

#endregion