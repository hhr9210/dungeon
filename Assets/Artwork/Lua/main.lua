print("[Lua] main.lua 开始加载")

-- 加载配置模块
wave_config = require("wave_config")
tower_config = require("tower_config")

-- 加载玩家普通攻击配置
player_attack_config = require("PlayerAttackConfig") -- 注意文件名大小写必须与实际文件一致
if not player_attack_config then
    print("[Lua] 错误: PlayerAttackConfig 模块加载失败！")
else
    print("[Lua] PlayerAttackConfig 模块已成功加载，内容如下:")
    for k, v in pairs(player_attack_config) do
        print(string.format("[Lua] %s = %s (类型: %s)", 
            tostring(k), 
            tostring(v), 
            type(v)))
    end
end


-- 如果有通用函数、类，也可以在这里 require
-- require("util")
-- require("ai_controller")

print("[Lua] 所有配置加载完毕")
