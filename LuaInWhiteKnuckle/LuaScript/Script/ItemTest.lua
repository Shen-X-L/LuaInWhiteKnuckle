-- 额外挎包测试
-- Inventory.AddExtraPouch()
-- Inventory.RemoveExtraPouch()
-- local pouches = Inventory.extraPouches
-- for i=0,pouches.Count-1,1 do
--     print(pouches[i].maxCapacity 
--         .. " " .. pouches[i].maxLargeCapacity 
--         .. " ".. tostring(pouches[i].allowNonPouchable))
--     pouches[i].allowNonPouchable = true
--     pouches[i].maxLargeCapacity = pouches[i].maxCapacity 
-- end
-- 口袋测试
-- Inventory.AddPocketItem(0, "Item_Piton", 1)
-- Inventory.AddPocketItem(1, "Item_AutoPiton", 1)
-- Inventory.RemovePocketItem(0)
-- Inventory.RemovePocketItem(1)
-- local left_item = Inventory.GetPocketItem(0)[0]
-- local right_item = Inventory.GetPocketItem(1)[0]
-- local position = left_item.bagPosition
-- local rotation = left_item.bagRotation
-- print("x: " .. position.x .. "y: " .. position.y .. "z: " .. position.z)
-- print("w: " .. rotation.w .. "x: " .. rotation.x 
--     .. "y: " .. rotation.y .. "z: " .. rotation.z)
-- print()
-- local position = right_item.bagPosition
-- local rotation = right_item.bagRotation
-- print("x: " .. position.x .. "y: " .. position.y .. "z: " .. position.z)
-- print("w: " .. rotation.w .. "x: " .. rotation.x 
--     .. "y: " .. rotation.y .. "z: " .. rotation.z)
-- left_item.bagPosition= Vector3.zero
-- 手部物品测试
Inventory.AddHandItem("Item_AutoPiton", 0)
Inventory.AddHandItem("Item_AutoPiton", 1)
-- Inventory.RemoveHandItem(0)
-- Inventory.RemoveHandItem(1)
Game.Events.Off("OnHandItemChange", "ItemTest")
Game.Events.On("OnHandItemChange", "ItemTest",
               function(handIndex, lastItem, currentItem)
    if currentItem == nil then
        if handIndex == 0 then Inventory.AddHandItem("Item_AutoPiton", 0) end
        if handIndex == 1 then Inventory.AddHandItem("Item_AutoPiton", 1) end
    end
end)

-- Inventory.AddHandItem("Item_Wine", 0)
-- Inventory.AddHandItem("Item_Wine", 1)
-- Game.Events.Off("OnHandItemChange", "ItemTest")
-- Game.Events.On("OnHandItemChange", "ItemTest",
--                function(handIndex, lastItem, currentItem)
--     com_print("handIndex: "..handIndex
--             .." lastItem: "..(lastItem and lastItem.prefabName or "None")
--             .." currentItem: "..(currentItem and currentItem.prefabName or "None"))
--     print("handIndex: "..handIndex
--             .." lastItem: "..(lastItem and lastItem.prefabName or "None")
--             .." currentItem: "..(currentItem and currentItem.prefabName or "None"))
-- end)
