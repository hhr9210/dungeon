using UnityEngine;

public class CRTCurveMouseCorrector : MonoBehaviour
{
    [Range(0f, 1f)] public float curveAmount = 0.5f; // �� Shader �� _CurveAmount ����һ��
    public float strength = 3.0f; // �� Shader �е�����ǿ��ƥ��

    public int maxIterations = 10;
    public float tolerance = 0.0001f;


    public Vector2 GetCorrectedScreenPosition()
    {
        Vector2 mouse = Input.mousePosition;

        if (mouse.x < 0 || mouse.x > Screen.width || mouse.y < 0 || mouse.y > Screen.height)
            return mouse;

        Vector2 uv = new Vector2(mouse.x / Screen.width, mouse.y / Screen.height);
        Vector2 corrected = InverseCurveUV(uv);

        if (float.IsNaN(corrected.x) || float.IsNaN(corrected.y))
            return mouse;

        return new Vector2(corrected.x * Screen.width, corrected.y * Screen.height);
    }
    private Vector2 InverseCurveUV(Vector2 distortedUV)
    {
        Vector2 uv = distortedUV;

        for (int i = 0; i < maxIterations; i++)
        {
            Vector2 centered = uv - new Vector2(0.5f, 0.5f);
            float distSq = Vector2.Dot(centered, centered);
            float distortion = Mathf.Pow(distSq, 2.0f) * curveAmount * strength;
            Vector2 warped = centered * (1.0f + distortion) + new Vector2(0.5f, 0.5f);

            Vector2 delta = warped - distortedUV;

            if (delta.sqrMagnitude < tolerance)
                break;

            uv -= delta * 0.5f;
        }

        return uv;
    }
}
