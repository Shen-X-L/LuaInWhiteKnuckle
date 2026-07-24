-- ============================================================
-- CantClimb 模块
-- 禁止玩家攀爬/抓取 仅可触发按钮/拾取物品
-- ============================================================

local M = {
    id = "CantClimb",
    name = "Perk_Module_Cant_Climb"
}   
 
function M.Initialize(perk, firstTime)
        Player.FocusModeOverride = true
end

function M.OnDestroy(perk)
        Player.FocusModeOverride = nil
end

return M