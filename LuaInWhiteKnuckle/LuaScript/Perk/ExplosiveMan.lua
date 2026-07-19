-- 技能
local perk_explosive_man
-- 定义perk描述
local perk_explosive_man_description = ""
-- 定义模块
local perk_module_explosive_man = {}

perk_module_explosive_man.name = "Perk_Module_Explosive_Man"

function perk_module_explosive_man.Initialize(perk, firstTime)
    Game.Events.Off("OnHandItemChange", "ExplosiveMan")
    Inventory.AddHandItem("Item_Rebar_Explosive", 0)
    Inventory.AddHandItem("Item_Rebar_Explosive", 1)

    Game.Events.On("OnHandItemChange", "ExplosiveMan",
        function(handIndex, lastItem, currentItem)
            if currentItem == nil then
                if handIndex == 0 and Inventory.GetPocketItem(0)[0] == nil then
                    Inventory.AddHandItem("Item_Rebar_Explosive", 0)
                end
                if handIndex == 1 and Inventory.GetPocketItem(1)[0] == nil then
                    Inventory.AddHandItem("Item_Rebar_Explosive", 1)
                end
            elseif currentItem.prefabName ~= "Item_Rebar_Explosive" and
                not currentItem.itemTags.Contains("trinket") and
                not currentItem.itemTags.Contains("disk") and
                not currentItem.itemTags.Contains("roach") then
                if handIndex == 0 then Inventory.RemoveHandItem(0) end
                if handIndex == 1 then Inventory.RemoveHandItem(1) end
            end
        end)

    Player.FocusModeOverride = true

    Game.Hooks.Register("OnPlayerDamage", "ExplosiveMan",
        function(DamageInfo)
            DamageInfo.tags.Contains("explosion")
            return DamageInfo, true
        end)
end

function perk_module_explosive_man.OnDestroy(perk)
    Game.Events.Off("OnHandItemChange", "ExplosiveMan")
    Player.FocusModeOverride = nil
    Game.Hooks.Unregister("OnPlayerDamage")
end

perk_explosive_man = Perk.CreateCustomBuffPerk("Perk_Explosive_Man",
    "Infinite Explosive Rebar",
    perk_explosive_man_description,
    false)
Perk.AddLuaModule(perk_explosive_man, perk_module_explosive_man)
perk_explosive_man.useBuff = false

Perk.AddPerk(perk_explosive_man)
