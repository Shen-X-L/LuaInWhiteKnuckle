local M = {
    id = "Template",
    name = "TemplateName"

}

function M.Initialize(perk, firstTime)
    print("Template.Initialize perkId: " .. perk.id .. " firstTime: " .. tostring(firstTime))
end

function M.AddModule(amount, firstTime)
    print("Template.AddModule amount: " .. amount .. " firstTime: " .. tostring(firstTime))
end

function M.OnDestroy(perk)
    print("Template.OnDestroy  perkId: " .. perk.id)
end

function M.Update()
    print("Template.Update")
end
-- min value 0.01
M.update_tick = 10

function M.GetCounterString()
    print("Template.GetCounterString")
    return "Template.GetCounterString"
end
-- min value 0.2
M.counter_tick = 10

function M.GetStatBuff(key,total)
    print("Template.GetStatBuff key:" .. key .. " total: " .. tostring(total))
    if key ~= "Description" then
        return 1  -- 修改攻击力 15%
    end
    -- 未匹配到该属性,返回 NaN
    return 0/0
end
-- min value 0.2
M.stat_tick = 10

function M.GetDescriptionFromKey(key)
    print("Template.GetDescriptionFromKey key:"..key)
    return "Template.GetDescriptionFromKey"
end
-- min value 0.2
M.description_tick = 10

function M.GetSaveData()
    print("Template.GetSaveData")
    return { "aaa", "bbb", "ccc" }
end

function M.LoadSaveData(list)
    print("Template.LoadSaveData")
    for i = 0, list.Count-1, 1 do
        print(list[i])
    end
end

return M
