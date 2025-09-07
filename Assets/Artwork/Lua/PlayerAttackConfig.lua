-- PlayerAttackConfig.lua
-- 玩家普通攻击的配置数据和逻辑

-- 定义玩家普通攻击的属性
local PlayerAttackData = {
    attackDamage = 100,       -- 普攻伤害
    attackRange = 200,         -- **请确保这里的 attackRange 不为 0，且是一个数字**
    attackSpeed = 10,       -- 每秒攻击次数
}

-- 计算攻击间隔
PlayerAttackData.attackCooldown = 1.0 / PlayerAttackData.attackSpeed

print("[Lua] PlayerAttackData 定义完成，内容:")
for k, v in pairs(PlayerAttackData) do
    print(string.format("[Lua] %s = %s (类型: %s)", 
        tostring(k), 
        tostring(v), 
        type(v)))
end

-- 获取攻击数据的方法 (供C#调用)
function GetPlayerAttackData()
    print("[Lua] GetPlayerAttackData 被调用。")
    return PlayerAttackData
end

function ExecuteBasicAttack(playerTransform, targetGameObject, damage, hitEffectPrefab, attackSound, audioSource)
    print("[Lua] ExecuteBasicAttack 被调用，传入参数:")
    print(string.format("[Lua] 目标: %s, 伤害值: %s", 
        tostring(targetGameObject and targetGameObject.name or "nil"), 
        tostring(damage)))

    -- 检查目标有效性
    if not targetGameObject or not targetGameObject.activeInHierarchy then
        print("[Lua] 目标无效或已销毁，攻击终止")
        return
    end

    -- 对敌人造成伤害
    local enemy = targetGameObject:GetComponent("Enemy")
    if enemy then
        print("[Lua] 调用 Enemy:TakeDamage(", damage, ")")
        enemy:TakeDamage(damage) -- 假设 Enemy 脚本有 TakeDamage 方法
    else
        print("[Lua] 目标未找到 Enemy 组件")
    end

    -- 其他逻辑（特效、音效等保持不变）...
end

print("[Lua] PlayerAttackConfig.lua 已加载。")

-- 关键修正：将 PlayerAttackData 表作为模块的返回值
return PlayerAttackData
