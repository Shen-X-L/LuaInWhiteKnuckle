-- ============================================================
-- ItemAdd 模块
-- 功能: 初始化时一次性添加物品到玩家背包
--
-- 标签格式:
--   ItemAdd:物品ID:数量
--
-- 参数说明:
--   物品ID   - 要添加的物品ID
--   数量     - 可选,默认1
--
-- 示例:
--   ItemAdd:Item_Piton:5      → 添加5个岩钉
--   ItemAdd:Item_Hammer       → 添加1个锤子
--   ItemAdd:Item_Food:10      → 添加10个食物
-- ============================================================

local M = {
    id = "ItemAdd",
    name = "Perk_Module_Item_Add"
}

-- 要添加的物品列表
-- 每个元素: { itemId = "Item_XXX", count = number }
M.items = {}

-- ============================================================
-- 配置解析
-- ============================================================

--- 检查物品是否存在
-- @param itemId 物品 ID
-- @return boolean
local function IsItemExist(itemId)
    if Item and Item.isItemExist then
        return Item.isItemExist(itemId)
    end
    return false
end

--- 从单个tag解析添加规则
-- @param tag 如 "ItemAdd:Item_Piton:5" 或 "ItemAdd:Item_Hammer"
-- @return table 或 nil
local function ParseTag(tag)
    -- 尝试匹配: ItemAdd:ItemID:Count
    local itemId, countStr = tag:match("^ItemAdd:([^:]+):(%d+)$")
    
    if IsItemExist(itemId) then
        return {
            itemId = itemId,
            count = tonumber(countStr)
        }
    end
    
    -- 尝试匹配: ItemAdd:ItemID (无数量,默认1)
    itemId = tag:match("^ItemAdd:([^:]+)$")
    if IsItemExist(itemId) then
        return {
            itemId = itemId,
            count = 1
        }
    end
    
    return nil
end

--- 从perk.tags收集所有添加规则
-- @param tags C# List<string>
-- @return table 物品列表
local function BuildItems(tags)
    local items = {}
    
    for i = 0, tags.Count - 1 do
        local rule = ParseTag(tags[i])
        if rule then
            table.insert(items, rule)
        end
    end
    
    return items
end

-- ============================================================
-- 生命周期
-- ============================================================

--- 初始化: 一次性添加所有配置的物品
-- @param perk 当前Perk对象
-- @param firstTime 是否首次获得
function M.Initialize(perk, firstTime)
    -- 仅首次获得时添加,读档恢复时不重复添加
    if not firstTime then
        return
    end
    
    M.items = BuildItems(perk.tags)
    
    for _, item in ipairs(M.items) do
        -- 调用C# API添加物品
        for i = 1, item.count do
            Inventory.AddItem(item.itemId)
        end
    end
end

--- 销毁: 清理数据
function M.OnDestroy(perk)
    M.items = {}
end

return M