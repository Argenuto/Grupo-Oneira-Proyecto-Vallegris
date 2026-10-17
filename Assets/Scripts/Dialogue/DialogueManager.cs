using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Added for the UI Image
using TMPro;
using UnityEngine.Localization;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Image portraitImage; // New Portrait Image reference

    [Header("Input")]
    [SerializeField] private InputAction advanceAction;

    [Header("Typing Settings")]
    [SerializeField] private float typingSpeed = 0.05f;

    [Header("Wavy Text Settings &( )&")]
    [SerializeField] private float waveAmount = 2f;
    [SerializeField] private float waveSpeed = 20f;

    [Header("Shaky Text Settings %( )%")]
    [SerializeField] private float shakeAmount = 1.5f;

    private DialogueObject currentDialogue;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    private HashSet<int> wavyCharacterIndices = new HashSet<int>();
    private HashSet<int> shakyCharacterIndices = new HashSet<int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable() => advanceAction.Enable();
    private void OnDisable() => advanceAction.Disable();

    private void Update()
    {
        HandleInput();
        ApplyTextEffects();
    }

    private void HandleInput()
    {
        if (dialoguePanel.activeSelf && advanceAction.WasPressedThisFrame())
        {
            if (isTyping)
            {
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                dialogueText.maxVisibleCharacters = dialogueText.textInfo.characterCount;
                isTyping = false;
            }
            else
            {
                NextLine();
            }
        }
    }

    public void StartDialogue(DialogueObject dialogueObj)
    {
        currentDialogue = dialogueObj;
        currentLineIndex = 0;
        
        // Clear text immediately so old dialogue doesn't flash before panel opens
        speakerNameText.text = "";
        dialogueText.text = "";
        
        dialoguePanel.SetActive(true);
        DisplayLine();
    }

    private void DisplayLine()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(PrepareAndTypeLine());
    }

    private IEnumerator PrepareAndTypeLine()
    {
        isTyping = true; 

        // Clear text before async loading begins to prevent mid-conversation flashes
        speakerNameText.text = "";
        dialogueText.text = "";

        DialogueObject.DialogueLine currentLine = currentDialogue.lines[currentLineIndex];

        // Handle Portrait
        if (currentLine.portrait != null)
        {
            portraitImage.sprite = currentLine.portrait;
            portraitImage.gameObject.SetActive(true);
        }
        else
        {
            portraitImage.gameObject.SetActive(false);
        }

        string rawSpeaker = "";
        string rawText = "";
        
        bool isSpeakerLoaded = currentLine.speakerName.IsEmpty;
        bool isTextLoaded = currentLine.dialogueText.IsEmpty;

        if (!isSpeakerLoaded)
        {
            var speakerHandle = currentLine.speakerName.GetLocalizedStringAsync();
            speakerHandle.Completed += (handle) => 
            {
                if (handle.IsValid() && handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                    rawSpeaker = handle.Result;
                isSpeakerLoaded = true;
            };
        }

        if (!isTextLoaded)
        {
            var textHandle = currentLine.dialogueText.GetLocalizedStringAsync();
            textHandle.Completed += (handle) => 
            {
                if (handle.IsValid() && handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                    rawText = handle.Result;
                isTextLoaded = true;
            };
        }

        yield return new WaitUntil(() => isSpeakerLoaded && isTextLoaded);

        speakerNameText.text = rawSpeaker;
        
        string parsedText = ParseTags(rawText);
        dialogueText.text = parsedText;
        
        dialogueText.ForceMeshUpdate();
        dialogueText.maxVisibleCharacters = 0;
        
        int totalCharacters = dialogueText.textInfo.characterCount;

        while (dialogueText.maxVisibleCharacters < totalCharacters)
        {
            dialogueText.maxVisibleCharacters++;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private string ParseTags(string rawText)
    {
        wavyCharacterIndices.Clear();
        shakyCharacterIndices.Clear();
        string cleanText = "";
        
        bool isWavy = false;
        bool isShaky = false;

        for (int i = 0; i < rawText.Length; i++)
        {
            if (i + 1 < rawText.Length && rawText[i] == '&' && rawText[i + 1] == '(') { isWavy = true; i++; continue; }
            if (i + 1 < rawText.Length && rawText[i] == ')' && rawText[i + 1] == '&') { isWavy = false; i++; continue; }

            if (i + 1 < rawText.Length && rawText[i] == '%' && rawText[i + 1] == '(') { isShaky = true; i++; continue; }
            if (i + 1 < rawText.Length && rawText[i] == ')' && rawText[i + 1] == '%') { isShaky = false; i++; continue; }

            cleanText += rawText[i];

            if (rawText[i] != ' ')
            {
                if (isWavy) wavyCharacterIndices.Add(cleanText.Length - 1);
                if (isShaky) shakyCharacterIndices.Add(cleanText.Length - 1);
            }
        }

        return cleanText;
    }

    private void ApplyTextEffects()
    {
        if ((wavyCharacterIndices.Count == 0 && shakyCharacterIndices.Count == 0) || dialogueText.textInfo == null) return;

        dialogueText.ForceMeshUpdate();
        TMP_TextInfo textInfo = dialogueText.textInfo;

        for (int charIndex = 0; charIndex < textInfo.characterCount; charIndex++)
        {
            if (charIndex >= dialogueText.maxVisibleCharacters) continue;

            TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
            if (!charInfo.isVisible) continue;

            bool applyWavy = wavyCharacterIndices.Contains(charIndex);
            bool applyShaky = shakyCharacterIndices.Contains(charIndex);

            if (!applyWavy && !applyShaky) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Vector3[] sourceVertices = textInfo.meshInfo[materialIndex].vertices;
            Vector3 offset = Vector3.zero;

            if (applyWavy)
            {
                offset += new Vector3(0, Mathf.Sin(Time.time * waveSpeed + charIndex) * waveAmount, 0);
            }

            if (applyShaky)
            {
                Vector2 randomShake = UnityEngine.Random.insideUnitCircle * shakeAmount;
                offset += new Vector3(randomShake.x, randomShake.y, 0);
            }

            sourceVertices[vertexIndex + 0] += offset; 
            sourceVertices[vertexIndex + 1] += offset; 
            sourceVertices[vertexIndex + 2] += offset; 
            sourceVertices[vertexIndex + 3] += offset; 
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            dialogueText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    private void NextLine()
    {
        currentLineIndex++;
        if (currentLineIndex < currentDialogue.lines.Length) DisplayLine();
        else EndDialogue();
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        currentDialogue = null;
    }
}