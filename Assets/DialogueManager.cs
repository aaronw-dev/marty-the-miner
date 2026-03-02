using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[System.Serializable]
public struct DialogueEntry
{
    [TextArea(3, 6)]
    public string text;
    public Sprite image;
    public UnityEvent onDialogueFinished;
}

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Dialogue Settings")]
    public List<DialogueEntry> dialogueEntries = new List<DialogueEntry>();
    public float textSpeed = 0.05f;

    [Header("UI References")]
    public TMP_Text dialogueText;
    public Image dialogueImage;
    public GameObject dialoguePanel;

    [Header("Animation")]
    public Animator finishAnimator;

    private int currentDialogueIndex = 0;
    private bool isTyping = false;
    private bool dialogueFinished = false;
    private Coroutine typingCoroutine;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (dialogueEntries.Count > 0)
        {
            StartDialogue();
        }
    }

    public void SkipText(InputAction.CallbackContext ctx)
    {
        if (!dialogueFinished && ctx.performed)
            SkipNext();
    }

    public void SkipNext()
    {
        if (isTyping)
        {
            SkipCurrentText();
        }
        else
        {
            NextDialogue();
        }
    }

    public void StartDialogue()
    {
        if (dialogueEntries.Count == 0)
            return;

        currentDialogueIndex = 0;
        dialogueFinished = false;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            finishAnimator.Play("dialogue-open");
        }

        ShowCurrentDialogue();
    }

    void ShowCurrentDialogue()
    {
        if (currentDialogueIndex >= dialogueEntries.Count)
        {
            FinishDialogue();
            return;
        }

        DialogueEntry currentEntry = dialogueEntries[currentDialogueIndex];

        if (dialogueImage != null)
        {
            if (currentEntry.image != null)
            {
                dialogueImage.sprite = currentEntry.image;
                dialogueImage.gameObject.SetActive(true);
            }
            else
            {
                dialogueImage.gameObject.SetActive(false);
            }
        }

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(currentEntry.text));
    }

    IEnumerator TypeText(string text)
    {
        isTyping = true;

        if (dialogueText != null)
        {
            dialogueText.text = "";

            int currentIndex = 0;
            while (currentIndex < text.Length)
            {
                if (text[currentIndex] == '<')
                {
                    int tagEndIndex = text.IndexOf('>', currentIndex);
                    if (tagEndIndex != -1)
                    {
                        string tag = text.Substring(currentIndex, tagEndIndex - currentIndex + 1);
                        dialogueText.text += tag;
                        currentIndex = tagEndIndex + 1;
                        continue;
                    }
                }

                dialogueText.text += text[currentIndex];
                currentIndex++;
                yield return new WaitForSeconds(textSpeed);
            }
        }

        isTyping = false;
    }

    void SkipCurrentText()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (dialogueText != null && currentDialogueIndex < dialogueEntries.Count)
        {
            dialogueText.text = dialogueEntries[currentDialogueIndex].text;
        }

        isTyping = false;
    }

    void NextDialogue()
    {
        // Fire the event for the current dialogue entry before moving to the next
        if (currentDialogueIndex < dialogueEntries.Count)
        {
            dialogueEntries[currentDialogueIndex].onDialogueFinished?.Invoke();
        }

        currentDialogueIndex++;
        ShowCurrentDialogue();
    }

    void FinishDialogue()
    {
        dialogueFinished = true;
        finishAnimator.Play("dialogue-close");
    }

    public void SetDialogue(List<DialogueEntry> newDialogue)
    {
        dialogueEntries = newDialogue;
    }

    public void AddDialogueEntry(string text, Sprite image = null)
    {
        DialogueEntry newEntry = new DialogueEntry { text = text, image = image };
        dialogueEntries.Add(newEntry);
    }

    public bool IsDialogueActive()
    {
        return !dialogueFinished && dialogueEntries.Count > 0;
    }

    public void StartDialogueWithArray(DialogueEntry[] newDialogue)
    {
        dialogueEntries = new List<DialogueEntry>(newDialogue);
        StartDialogue();
    }
}
