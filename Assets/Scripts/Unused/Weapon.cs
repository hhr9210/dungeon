using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/New Weapon")]
public class Weapon : ScriptableObject
{
    [Header("������Ϣ")]
    public string itemName = "New Weapon";     // ��������
    [TextArea(3, 5)]
    public string description = "Weapon description."; // ��������
    public Sprite icon = null;                 // UI��ʾͼ��

    [Header("�����������")]
    [Tooltip("��������������ħ������㡣")]
    public int manaRequirement = 0;
    [Tooltip("�������������Ĺ�������㡣")]
    public int attackRequirement = 0;
    [Tooltip("����������������������㡣")]
    public int strengthRequirement = 0;

    // �Ƴ������й���������������Ϊ��ʱ�����Ǿ���Ч��
    // public float attackDamageModifier = 0f;
    // public float attackSpeedModifier = 0f;
}