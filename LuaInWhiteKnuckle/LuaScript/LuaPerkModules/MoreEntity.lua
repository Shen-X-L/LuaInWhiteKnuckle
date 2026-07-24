local M = {
    id = "More Entity",
    name = "Perk_Binding_MoreEntity"
}

function M.Initialize(perk, firstTime)
    if firstTime then
        local perk = Perk.GetPerk("Perk_CarnalBloodlust")
        Perk.AddPerk(perk)
        Inventory.AddItem("Item_Cleaver")
    end
    
    Game.Events.On("EnterLevel", "MoreEntity", function(level, isFrist)
        if not isFrist then
            return
        end
        -- 上升电梯 额外判定
        if string.find(level.name, "Campaign_Interlude_Pipeworks_To_Habitation") then
            print("Campaign_Interlude_Pipeworks_To_Habitation")
            local entrancePos = level.entrance.position
            local exitPos = level.exit.position
            local exitRot = level.exit.rotation
            local pos = Vector3(exitPos.x, entrancePos.y + 20, exitPos.z)
            Other.SpawnEntity("Denizen_Sprider_Swarmer", pos, exitRot)
            Other.SpawnEntity("Denizen_Sprider_Swarmer", pos, exitRot)
            Other.SpawnEntity("Denizen_Sprider_Swarmer", pos, exitRot)
            Other.SpawnEntity("Denizen_Sprider_Swarmer", pos, exitRot)
            Other.SpawnEntity("Denizen_Sprider_Swarmer", pos, exitRot)
            Other.SpawnEntity("Denizen_Bloodbug", pos, exitRot)
            Other.SpawnEntity("Denizen_Bloodbug", pos, exitRot)
            Other.SpawnEntity("Denizen_Bloodbug", pos, exitRot)
            Other.SpawnEntity("Denizen_Bloodbug_Spitter", pos, exitRot)
            Other.SpawnEntity("Denizen_Bloodbug_Spitter", pos, exitRot)
            Other.SpawnEntity("Denizen_Hopper_Explosive", pos, exitRot)
        end
        -- 房间内全部生物
        local denizens = level.denizens
        for i = 0, denizens.Count - 1 do
            local denizen = denizens[i]
            local pos = denizen.transform.position
            local rot = denizen.transform.rotation
            local randomValue = Random.value
            if randomValue < 0.25 then
                -- 小血虫
                Other.SpawnEntity("Denizen_Bloodbug_Swarmer", pos, rot)
            elseif randomValue < 0.37 then
                -- 喷射血虫
                Other.SpawnEntity("Denizen_Bloodbug_Spitter", pos, rot)
            else
                -- 生成血虫
                Other.SpawnEntity("Denizen_Bloodbug", pos, rot)
            end

            if Random.Chance(0.15) then
                -- 生成爆炸大蟑螂
                Other.SpawnEntity("Denizen_Hopper_Explosive", pos, rot)
            else
                -- 生成小螃蟹
                Other.SpawnEntity("Denizen_Sprider_Swarmer", pos, rot)
            end
        end
    end)
end

function M.OnDestroy(perk)
    Game.Events.Off("OnHandItemChange", "MoreEntity")
end

return M
