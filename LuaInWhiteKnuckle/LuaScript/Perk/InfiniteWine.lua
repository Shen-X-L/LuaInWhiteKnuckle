-- 无限酒技能
local perk_infinite_wine
-- 定义perk描述
local perk_infinite_wine_description = ""
-- 定义模块
local perk_module_infinite_wine = {}

perk_module_infinite_wine.name = "Perk_Module_Infinite_Wine"

function perk_module_infinite_wine.Initialize(perk, firstTime)
    Game.Events.Off("OnHandItemChange", "InfiniteWine")
    Inventory.AddHandItem("Item_Wine", 0)
    Inventory.AddHandItem("Item_Wine", 1)

    Game.Events.On("OnHandItemChange", "InfiniteWine",
        function(handIndex, lastItem, currentItem)
            if currentItem ~= nil and currentItem.prefabName == "Item_Wine_Empty" then
                if handIndex == 0 then
                    Inventory.RemoveHandItem(0)
                    Inventory.AddHandItem("Item_Wine", 0)
                end
                if handIndex == 1 then
                    Inventory.RemoveHandItem(1)
                    Inventory.AddHandItem("Item_Wine", 1)
                end
            end
        end)
end

function perk_module_infinite_wine.OnDestroy(perk)
    Game.Events.Off("OnHandItemChange", "InfiniteWine")
end

perk_infinite_wine = Perk.CreateCustomBuffPerk("Perk_Infinite_Wine",
    "Infinite Wine",
    perk_infinite_wine_description,
    false)
Perk.AddLuaModule(perk_infinite_wine, perk_module_infinite_wine)
perk_infinite_wine.canStack = false
perk_infinite_wine.useBuff = false

Perk.AddPerk(perk_infinite_wine)
