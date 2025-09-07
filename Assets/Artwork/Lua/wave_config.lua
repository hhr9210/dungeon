-- wave_config.lua
return {
    waves = {
        {
            time = 3,  -- 第 1 波在游戏开始 3 秒后出现
            enemies = {
                { type = "orc", count = 3 },
            }
        },
        {
            time = 10,  -- 第 2 波在第 10 秒出现
            enemies = {
                { type = "orc", count = 5 },
                { type = "goblin", count = 2 },
            }
        },
        {
            time = 20,
            enemies = {
                { type = "boss", count = 1 },
            }
        }
    }
}
