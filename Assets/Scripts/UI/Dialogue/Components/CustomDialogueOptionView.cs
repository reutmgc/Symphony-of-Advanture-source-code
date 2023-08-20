using System;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using Yarn.Unity;

public class CustomDilogueOptionView : UnityEngine.UI.Selectable, ISubmitHandler
{
    [SerializeField] TextMeshProUGUI text;
    [SerializeField] bool showCharacterName = false;

    public Action<DialogueOption> OnOptionSelected;

    DialogueOption _option;

    bool hasSubmittedOptionSelection = false;

    public DialogueOption Option
    {
        get => _option;

        set
        {
            _option = value;

            hasSubmittedOptionSelection = false;

            // When we're given an Option, use its text and update our
            // interactibility.
            if (showCharacterName)
            {
                text.text = value.Line.Text.Text;
            }
            else
            {
                text.text = value.Line.TextWithoutCharacterName.Text;
            }
            interactable = value.IsAvailable;
        }
    }

    // If we receive a submit or click event, invoke our "we just selected
    // this option" handler.
    public void OnSubmit(BaseEventData eventData)
    {
        InvokeOptionSelected();
        Debug.Log("a");
    }

    public void InvokeOptionSelected()
    {
        // turns out that Selectable subclasses aren't intrinsically interactive/non-interactive
        // based on their canvasgroup, you still need to check at the moment of interaction
        if (!IsInteractable())
        {
            return;
        }

        // We only want to invoke this once, because it's an error to
        // submit an option when the Dialogue Runner isn't expecting it. To
        // prevent this, we'll only invoke this if the flag hasn't been cleared already.
        if (hasSubmittedOptionSelection == false)
        {
            OnOptionSelected.Invoke(Option);
            hasSubmittedOptionSelection = true;
        }
    }
}
