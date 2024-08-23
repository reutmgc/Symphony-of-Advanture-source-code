using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Yarn.Unity;

public class CustomDialogueLineView : DialogueViewBase
{
    /// <summary>
    /// The canvas group that contains the UI elements used by this Line
    /// View.
    /// </summary>
    /// <remarks>
    /// If <see cref="useFadeEffect"/> is true, then the alpha value of this
    /// <see cref="CanvasGroup"/> will be animated during line presentation
    /// and dismissal.
    /// </remarks>
    /// <seealso cref="useFadeEffect"/>
    [SerializeField]
    internal CanvasGroup canvasGroup;

    /// <summary>
    /// Controls whether the line view should fade in when lines appear, and
    /// fade out when lines disappear.
    /// </summary>
    /// <remarks><para>If this value is <see langword="true"/>, the <see
    /// cref="canvasGroup"/> object's alpha property will animate from 0 to
    /// 1 over the course of <see cref="fadeInTime"/> seconds when lines
    /// appear, and animate from 1 to zero over the course of <see
    /// cref="fadeOutTime"/> seconds when lines disappear.</para>
    /// <para>If this value is <see langword="false"/>, the <see
    /// cref="canvasGroup"/> object will appear instantaneously.</para>
    /// </remarks>
    /// <seealso cref="canvasGroup"/>
    /// <seealso cref="fadeInTime"/>
    /// <seealso cref="fadeOutTime"/>
    [SerializeField]
    internal bool useFadeEffect = true;

    /// <summary>
    /// The time that the fade effect will take to fade lines in.
    /// </summary>
    /// <remarks>This value is only used when <see cref="useFadeEffect"/> is
    /// <see langword="true"/>.</remarks>
    /// <seealso cref="useFadeEffect"/>
    [SerializeField]
    [Min(0)]
    internal float fadeInTime = 0.25f;

    /// <summary>
    /// The time that the fade effect will take to fade lines out.
    /// </summary>
    /// <remarks>This value is only used when <see cref="useFadeEffect"/> is
    /// <see langword="true"/>.</remarks>
    /// <seealso cref="useFadeEffect"/>
    [SerializeField]
    [Min(0)]
    internal float fadeOutTime = 0.05f;

    /// <summary>
    /// The <see cref="TextMeshProUGUI"/> object that displays the text of
    /// dialogue lines.
    /// </summary>
    [SerializeField]
    internal TextMeshProUGUI lineText = null;

    /// <summary>
    /// Controls whether the <see cref="lineText"/> object will show the
    /// character name present in the line or not.
    /// </summary>
    /// <remarks>
    /// <para style="note">This value is only used if <see
    /// cref="characterNameText"/> is <see langword="null"/>.</para>
    /// <para>If this value is <see langword="true"/>, any character names
    /// present in a line will be shown in the <see cref="lineText"/>
    /// object.</para>
    /// <para>If this value is <see langword="false"/>, character names will
    /// not be shown in the <see cref="lineText"/> object.</para>
    /// </remarks>
    [SerializeField]
    [UnityEngine.Serialization.FormerlySerializedAs("showCharacterName")]
    internal bool showCharacterNameInLineView = true;

    /// <summary>
    /// The <see cref="TextMeshProUGUI"/> object that displays the character
    /// names found in dialogue lines.
    /// </summary>
    /// <remarks>
    /// If the <see cref="LineView"/> receives a line that does not contain
    /// a character name, this object will be left blank.
    /// </remarks>
    [SerializeField]
    internal TextMeshProUGUI characterNameText = null;

    /// <summary>
    /// Controls whether the text of <see cref="lineText"/> should be
    /// gradually revealed over time.
    /// </summary>
    /// <remarks><para>If this value is <see langword="true"/>, the <see
    /// cref="lineText"/> object's <see
    /// cref="TMP_Text.maxVisibleCharacters"/> property will animate from 0
    /// to the length of the text, at a rate of <see
    /// cref="typewriterEffectSpeed"/> letters per second when the line
    /// appears. <see cref="onCharacterTyped"/> is called for every new
    /// character that is revealed.</para>
    /// <para>If this value is <see langword="false"/>, the <see
    /// cref="lineText"/> will all be revealed at the same time.</para>
    /// <para style="note">If <see cref="useFadeEffect"/> is <see
    /// langword="true"/>, the typewriter effect will run after the fade-in
    /// is complete.</para>
    /// </remarks>
    /// <seealso cref="lineText"/>
    /// <seealso cref="onCharacterTyped"/>
    /// <seealso cref="typewriterEffectSpeed"/>
    [SerializeField]
    internal bool useTypewriterEffect = false;

    /// <summary>
    /// A Unity Event that is called each time a character is revealed
    /// during a typewriter effect.
    /// </summary>
    /// <remarks>
    /// This event is only invoked when <see cref="useTypewriterEffect"/> is
    /// <see langword="true"/>.
    /// </remarks>
    /// <seealso cref="useTypewriterEffect"/>
    [SerializeField]
    internal UnityEngine.Events.UnityEvent onCharacterTyped;

    /// <summary>
    /// The number of characters per second that should appear during a
    /// typewriter effect.
    /// </summary>
    /// <seealso cref="useTypewriterEffect"/>
    [SerializeField]
    [Min(0)]
    internal float typewriterEffectSpeed = 0f;

    /// <summary>
    /// The amount of time to wait after any line
    /// </summary>
    [SerializeField]
    [Min(0)]
    internal float holdTime = 1f;

    [SerializeField]
    internal bool autoAdvance = false;

    /// <summary>
    /// The current <see cref="LocalizedLine"/> that this line view is
    /// displaying.
    /// </summary>
    LocalizedLine currentLine = null;

    /// <summary>
    /// A stop token that is used to interrupt the current animation.
    /// </summary>
    Effects.CoroutineInterruptToken currentStopToken = new Effects.CoroutineInterruptToken();

    [SerializeField]
    InputActionReference continueAction, interruptAction, skipAction;


    private void Awake()
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
    }

    private void Reset()
    {
        canvasGroup = GetComponentInParent<CanvasGroup>();
    }
    private void OnEnable()
    {
        continueAction.action.performed+=context=> UserRequestedViewAdvancement();
        interruptAction.action.performed += context => UserRequestedViewInterrupt();
        skipAction.action.performed += context => UserRequestedViewSkip();
    }
    private void OnDisable()
    {
        continueAction.action.performed -= context => UserRequestedViewAdvancement();
        interruptAction.action.performed -= context => UserRequestedViewInterrupt();
        skipAction.action.performed -= context => UserRequestedViewSkip();
    }

    /// <inheritdoc/>
    public override void DismissLine(Action onDismissalComplete)
    {
        currentLine = null;
        StartCoroutine(DismissLineInternal(onDismissalComplete));
    }

    private IEnumerator DismissLineInternal(Action onDismissalComplete)
    {

        // disabling interaction temporarily while dismissing the line
        // we don't want people to interrupt a dismissal
        var interactable = canvasGroup.interactable;
        canvasGroup.interactable = false;

        // If we're using a fade effect, run it, and wait for it to finish.
        if (useFadeEffect)
        {
            yield return StartCoroutine(Effects.FadeAlpha(canvasGroup, 1, 0, fadeOutTime, currentStopToken));
            currentStopToken.Complete();
        }

        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        // turning interaction back on, if it needs it
        canvasGroup.interactable = interactable;

        if (onDismissalComplete != null)
        {
            onDismissalComplete();
        }
    }

    /// <inheritdoc/>
    public override void InterruptLine(LocalizedLine dialogueLine, Action onInterruptLineFinished)
    {
        currentLine = dialogueLine;

        // Cancel all coroutines that we're currently running. This will
        // stop the RunLineInternal coroutine, if it's running.
        StopAllCoroutines();

        // for now we are going to just immediately show everything
        // later we will make it fade in
        lineText.gameObject.SetActive(true);
        canvasGroup.gameObject.SetActive(true);

        int length;

        if (characterNameText == null)
        {
            if (showCharacterNameInLineView)
            {
                lineText.text = dialogueLine.Text.Text;
                length = dialogueLine.Text.Text.Length;
            }
            else
            {
                lineText.text = dialogueLine.TextWithoutCharacterName.Text;
                length = dialogueLine.TextWithoutCharacterName.Text.Length;
            }
        }
        else
        {
            characterNameText.text = dialogueLine.CharacterName;
            lineText.text = dialogueLine.TextWithoutCharacterName.Text;
            length = dialogueLine.TextWithoutCharacterName.Text.Length;
        }

        // Show the entire line's text immediately.
        lineText.maxVisibleCharacters = length;

        // Make the canvas group fully visible immediately, too.
        canvasGroup.alpha = 1;

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        onInterruptLineFinished();
    }

    /// <inheritdoc/>
    public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
    {
        // Stop any coroutines currently running on this line view (for
        // example, any other RunLine that might be running)
        StopAllCoroutines();

        // Begin running the line as a coroutine.
        StartCoroutine(RunLineInternal(dialogueLine, onDialogueLineFinished));
    }

    private IEnumerator RunLineInternal(LocalizedLine dialogueLine, Action onDialogueLineFinished)
    {
        IEnumerator PresentLine()
        {
            lineText.gameObject.SetActive(true);
            canvasGroup.gameObject.SetActive(true);

            if (characterNameText != null)
            {
                // If we have a character name text view, show the character
                // name in it, and show the rest of the text in our main
                // text view.
                characterNameText.text = dialogueLine.CharacterName;
                lineText.text = dialogueLine.TextWithoutCharacterName.Text;
            }
            else
            {
                // We don't have a character name text view. Should we show
                // the character name in the main text view?
                if (showCharacterNameInLineView)
                {
                    // Yep! Show the entire text.
                    lineText.text = dialogueLine.Text.Text;
                }
                else
                {
                    // Nope! Show just the text without the character name.
                    lineText.text = dialogueLine.TextWithoutCharacterName.Text;
                }
            }

            if (useTypewriterEffect)
            {
                // If we're using the typewriter effect, hide all of the
                // text before we begin any possible fade (so we don't fade
                // in on visible text).
                lineText.maxVisibleCharacters = 0;
            }
            else
            {
                // Ensure that the max visible characters is effectively
                // unlimited.
                lineText.maxVisibleCharacters = int.MaxValue;
            }

            // If we're using the fade effect, start it, and wait for it to
            // finish.
            if (useFadeEffect)
            {
                yield return StartCoroutine(Effects.FadeAlpha(canvasGroup, 0, 1, fadeInTime, currentStopToken));
                if (currentStopToken.WasInterrupted)
                {
                    // The fade effect was interrupted. Stop this entire
                    // coroutine.
                    yield break;
                }
            }

            // If we're using the typewriter effect, start it, and wait for
            // it to finish.
            if (useTypewriterEffect)
            {
                // setting the canvas all back to its defaults because if we didn't also fade we don't have anything visible
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                yield return StartCoroutine(
                    Effects.Typewriter(
                        lineText,
                        typewriterEffectSpeed,
                        () => onCharacterTyped.Invoke(),
                        currentStopToken
                    )
                );
                if (currentStopToken.WasInterrupted)
                {
                    // The typewriter effect was interrupted. Stop this
                    // entire coroutine.
                    yield break;
                }
            }
        }
        currentLine = dialogueLine;

        // Run any presentations as a single coroutine. If this is stopped,
        // which UserRequestedViewAdvancement can do, then we will stop all
        // of the animations at once.
        yield return StartCoroutine(PresentLine());

        currentStopToken.Complete();

        // All of our text should now be visible.
        lineText.maxVisibleCharacters = int.MaxValue;

        // Our view should at be at full opacity.
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // If we have a hold time, wait that amount of time, and then
        // continue.
        if (holdTime > 0)
        {
            yield return new WaitForSeconds(holdTime);
        }

        if (autoAdvance == false)
        {
            // The line is now fully visible, and we've been asked to not
            // auto-advance to the next line. Stop here, and don't call the
            // completion handler - we'll wait for a call to
            // UserRequestedViewAdvancement, which will interrupt this
            // coroutine.
            yield break;
        }

        // Our presentation is complete; call the completion handler.
        onDialogueLineFinished();
    }

    /// <inheritdoc/>
    public override void UserRequestedViewAdvancement()
    {
        if (this == null)
            return;
        // we have no line, so the user just mashed randomly
        if (currentLine == null)
        {
            return;
        }
        // when line is already finished
        if (!currentStopToken.CanInterrupt)
            // move forward
            requestInterrupt?.Invoke();
    }
    public void UserRequestedViewInterrupt()
    {
        if (this == null)
            return;
        // we have no line, so the user just mashed randomly
        if (currentLine == null)
            return;
        if (currentStopToken.CanInterrupt)
        {
            // Stop the current animation, and skip to the end of whatever
            // started it.
            requestInterrupt?.Invoke();
        }

    }

    public void UserRequestedViewSkip()
    {
        if (currentLine == null)
            return;
    }

}
