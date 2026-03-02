using UnityEngine;

public class CallDialogueFromEvent : MonoBehaviour
{
    [Header("Dialogue Configuration")]
    [SerializeField]
    private DialogueEntry[] dialogueEntries;

    [Header("Settings")]
    [SerializeField]
    private bool startOnAwake = false;

    void Start()
    {
        if (startOnAwake)
        {
            StartDialogue();
        }
    }

    public void StartDialogue()
    {
        if (DialogueManager.Instance != null && dialogueEntries.Length > 0)
        {
            DialogueManager.Instance.StartDialogueWithArray(dialogueEntries);
        }
        else if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager instance not found! Make sure there's a DialogueManager in the scene.");
        }
        else
        {
            Debug.LogWarning("No dialogue entries configured in CallDialogueFromEvent.");
        }
    }

    public void StartDialogueIfNotActive()
    {
        if (DialogueManager.Instance != null && !DialogueManager.Instance.IsDialogueActive())
        {
            StartDialogue();
        }
    }
}