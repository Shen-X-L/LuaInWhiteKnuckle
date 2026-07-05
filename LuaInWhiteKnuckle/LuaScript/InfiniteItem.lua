do 
local item_prefab = "Item_Rubble" 
local item_number = 5 

if not Item.isItemExist(item_prefab) then 
	print("LuaInWK: not have item prefab: "..item_prefab) 
	return 
end 

Game.Events.Off("OnInventoryRemove","infinite_item")

Inventory.AddItem(item_prefab, count) 

Game.Events.On("OnInventoryRemove","infinite_item", 
	function(item_name, delta, count) 
		if item_name == item_prefab and count > item_number then 
			Inventory.AddItem(item_prefab, count - item_number) 
		end 
	end)
end