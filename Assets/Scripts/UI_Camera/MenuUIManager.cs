using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Video;

public class MenuUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject introVideoPanel;
    public GameObject mainMenuPanel;
    public GameObject loadGamePanel;
    public GameObject optionsPanel;

    [Header("Buttons")]
    public Button startGameButton;
    public Button loadGameButton;
    public Button optionsButton;
    public Button quitButton;
    public Button loadGameBackButton;
    public Button optionsBackButton;

    [Header("Video Player")]
    public VideoPlayer introVideoPlayer;

    [Header("Volume Settings")]
    public Slider volumeSlider;
    public Image volumeFillImage;

    [Header("Scene Settings")]
    public string gameSceneName = "GameScene";

    private bool hasStartedTransition = false;

    private void Awake()
    {
        if (introVideoPlayer == null)
        {
            introVideoPlayer = introVideoPanel.GetComponentInChildren<VideoPlayer>();
            if (introVideoPlayer == null)
            {
                Debug.LogError("IntroVideoPanel 子对象中未找到 VideoPlayer。");
                introVideoPanel.SetActive(false);
                mainMenuPanel.SetActive(true);
                return;
            }
        }

        introVideoPlayer.loopPointReached += OnVideoEnd;

        introVideoPlayer.Play();
    }

    private void Start()
    {
        introVideoPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
        loadGamePanel.SetActive(false);
        optionsPanel.SetActive(false);

        float savedVolume = PlayerPrefs.GetFloat("volume", 1f);
        volumeSlider.value = savedVolume;
        UpdateVolumeUI(savedVolume);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        startGameButton.onClick.AddListener(OnStartGame);
        loadGameButton.onClick.AddListener(OpenLoadGamePanel);
        optionsButton.onClick.AddListener(OpenOptionsPanel);
        quitButton.onClick.AddListener(OnQuitGame);

        loadGameBackButton.onClick.AddListener(CloseLoadGamePanel);
        optionsBackButton.onClick.AddListener(CloseOptionsPanel);
    }

    private void Update()
    {
        if (!hasStartedTransition && introVideoPanel.activeSelf && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
        {
            StartCoroutine(EnterMainMenu());
        }
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        if (!hasStartedTransition)
        {
            StartCoroutine(EnterMainMenu());
        }
    }

    private IEnumerator EnterMainMenu()
    {
        hasStartedTransition = true;

        if (introVideoPlayer != null)
        {
            introVideoPlayer.Stop();
        }

        yield return new WaitForSeconds(0.2f);

        introVideoPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        yield return null;
    }

    private void OnStartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnQuitGame()
    {
        Debug.Log("退出游戏");
        Application.Quit();
    }

    private void OpenLoadGamePanel()
    {
        mainMenuPanel.SetActive(false);
        loadGamePanel.SetActive(true);
    }

    private void CloseLoadGamePanel()
    {
        loadGamePanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    private void OpenOptionsPanel()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    private void CloseOptionsPanel()
    {
        optionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        UpdateVolumeUI(value);
        PlayerPrefs.SetFloat("volume", value);
        PlayerPrefs.Save();
    }

    private void UpdateVolumeUI(float value)
    {
        if (volumeFillImage != null)
            volumeFillImage.fillAmount = value;
    }

    private void OnDestroy()
    {
        if (introVideoPlayer != null)
        {
            introVideoPlayer.loopPointReached -= OnVideoEnd;
        }
    }
}
