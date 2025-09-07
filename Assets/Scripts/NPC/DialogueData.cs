using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    // 存储对话中的所有台词
    public List<DialogueLine> dialogueLines;
}

[System.Serializable]
public class DialogueLine
{

    [TextArea(3, 5)] 
    public string dialogueText;
    
    public AudioClip voiceClip; 

    public List<PlayerOption> playerOptions; 
}

[System.Serializable]
public class PlayerOption
{
    public string optionText;
    
    // 指向下一个对话数据的引用，实现对话分支
    public DialogueData nextDialogue; 
    
    // public UnityEvent onOptionSelected; 
}