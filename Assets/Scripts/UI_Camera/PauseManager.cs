using UnityEngine;

public class PauseManager : MonoBehaviour
{

    public static PauseManager Instance { get; private set; }


    public delegate void OnPauseStateChanged(bool isPaused);
    public static event OnPauseStateChanged PauseStateChanged;

    private bool _isGamePaused = false;


    public bool IsGamePaused
    {
        get { return _isGamePaused; }
        private set
        {
            if (_isGamePaused != value)
            {
                _isGamePaused = value;

                if (value) 
                {
                    Time.timeScale = 0f; 
                    Debug.Log("PauseManager: 游戏已暂停。");
                }
                else
                {
                    Time.timeScale = 1f; 
                    Debug.Log("PauseManager: 游戏已恢复。");
                }

                ToggleCursorVisibility(value);

                // 触发事件，通知所有订阅者暂停状态已改变
                // ?. Invoke() 是 C# 6.0 的语法糖，等同于 if (PauseStateChanged != null) PauseStateChanged.Invoke(_isGamePaused);
                PauseStateChanged?.Invoke(_isGamePaused);
            }
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        IsGamePaused = false;
    }

    void Update()
    {

    }





    public void SetGamePaused(bool pause)
    {
        IsGamePaused = pause;
    }

    /// <summary>
    /// 切换鼠标的锁定状态和可见性。
    /// </summary>
    /// <param name="showCursor">如果为true，显示鼠标并解锁；如果为false，隐藏鼠标并锁定。</param>
    private void ToggleCursorVisibility(bool showCursor)
    {
        if (showCursor)
        {
            Cursor.lockState = CursorLockMode.None; // 解锁鼠标，使其可以在屏幕上自由移动
            Cursor.visible = true;                  // 显示鼠标光标
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked; // 锁定鼠标到屏幕中心
            Cursor.visible = false;                   // 隐藏鼠标光标
        }
    }
}