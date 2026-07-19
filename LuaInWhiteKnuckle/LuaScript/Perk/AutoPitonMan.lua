-- 电钻人技能
local perk_auto_piton_man
-- 定义perk描述
local perk_auto_piton_man_description = ""
-- 定义模块
local perk_module_auto_piton_man = {}

perk_module_auto_piton_man.name = "Perk_Module_Auto_Piton_Man"

function perk_module_auto_piton_man.Initialize(perk, firstTime)
    Game.Events.Off("OnHandItemChange", "AutoPitonMan")
    Inventory.AddHandItem("Item_AutoPiton", 0)
    Inventory.AddHandItem("Item_AutoPiton", 1)

    Game.Events.On("OnHandItemChange", "AutoPitonMan",
        function(handIndex, lastItem, currentItem)
            if currentItem == nil then
                if handIndex == 0 and Inventory.GetPocketItem(0)[0] == nil then
                    Inventory.AddHandItem("Item_AutoPiton", 0)
                end
                if handIndex == 1 and Inventory.GetPocketItem(1)[0] == nil then
                    Inventory.AddHandItem("Item_AutoPiton", 1)
                end
            elseif currentItem.prefabName ~= "Item_AutoPiton" and
                not currentItem.itemTags.Contains("trinket") and
                not currentItem.itemTags.Contains("disk") and
                not currentItem.itemTags.Contains("roach") then
                if handIndex == 0 then Inventory.RemoveHandItem(0) end
                if handIndex == 1 then Inventory.RemoveHandItem(1) end
            end
        end)
    Player.FocusModeOverride = true
end

function perk_module_auto_piton_man.OnDestroy(perk)
    Game.Events.Off("OnHandItemChange", "AutoPitonMan")
    Player.FocusModeOverride = nil
end

perk_auto_piton_man = Perk.CreateCustomBuffPerk("Perk_Auto_Piton_Man",
    "Infinite Auto Piton",
    perk_auto_piton_man_description,
    false)
Perk.AddLuaModule(perk_auto_piton_man, perk_module_auto_piton_man)
perk_auto_piton_man.canStack = false
perk_auto_piton_man.useBuff = false
Perk.AddPerk(perk_auto_piton_man)
