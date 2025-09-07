using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(Button))]
public class ButtonUnderline : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TextMeshProUGUI tmpText;
    private Button button;

    public int flashCount = 5;
    public float flashInterval = 0.1f;

    private Coroutine flashCoroutine;
    private string originalText;

    void Start()
    {
        button = GetComponent<Button>();
        tmpText = GetComponentInChildren<TextMeshProUGUI>();

        if (tmpText != null)
        {
            originalText = tmpText.text;
            SetUnderline(false);
        }
        else
        {
            Debug.LogWarning("ButtonTMPUnderlineEffect: No TextMeshProUGUI component found as a child. This script requires a TextMeshProUGUI child.", this);
            enabled = false;
            return;
        }

        if (button != null)
        {
            button.onClick.AddListener(() => StartFlashing());
        }
    }

    void SetUnderline(bool enable)
    {
        if (tmpText == null) return;

        string currentText = tmpText.text;
        string underlineTagStart = "<u>";
        string underlineTagEnd = "</u>";

        if (enable)
        {
            if (!currentText.StartsWith(underlineTagStart) || !currentText.EndsWith(underlineTagEnd))
            {
                tmpText.text = underlineTagStart + originalText + underlineTagEnd;
            }
        }
        else
        {
            if (currentText.StartsWith(underlineTagStart) && currentText.EndsWith(underlineTagEnd))
            {
                tmpText.text = originalText;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tmpText != null && flashCoroutine == null)
        {
            SetUnderline(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tmpText != null && flashCoroutine == null)
        {
            SetUnderline(false);
        }
    }

    void StartFlashing()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(FlashUnderline());
    }

    IEnumerator FlashUnderline()
    {
        if (tmpText == null)
        {
            flashCoroutine = null;
            yield break;
        }

        bool wasUnderlinedBeforeFlash = tmpText.text.StartsWith("<u>");

        for (int i = 0; i < flashCount; i++)
        {
            SetUnderline(true);
            yield return new WaitForSeconds(flashInterval);
            SetUnderline(false);
            yield return new WaitForSeconds(flashInterval);
        }

        flashCoroutine = null;

        if (EventSystem.current != null && IsPointerOverGameObject())
        {
            SetUnderline(true);
        }
        else
        {
            SetUnderline(false);
        }
    }

    bool IsPointerOverGameObject()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }
}
