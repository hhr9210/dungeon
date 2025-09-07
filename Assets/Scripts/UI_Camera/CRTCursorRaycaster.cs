using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;


public class CRTCursorRaycaster : MonoBehaviour
{
    public CRTCurveMouseCorrector corrector;
    public bool simulateClick = true;

    private List<RaycastResult> results = new List<RaycastResult>();

    void Update()
    {
        if (corrector == null || EventSystem.current == null)
            return;

        Vector2 correctedPos = corrector.GetCorrectedScreenPosition();

        if (float.IsNaN(correctedPos.x) || float.IsNaN(correctedPos.y))
            return;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = correctedPos
        };

        results.Clear();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var hit in results)
        {
            if (simulateClick && Input.GetMouseButtonDown(0))
            {
                ExecuteEvents.Execute(hit.gameObject, eventData, ExecuteEvents.pointerClickHandler);
            }
        }
    }
}
