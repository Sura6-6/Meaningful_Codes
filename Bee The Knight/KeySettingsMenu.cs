using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KeySettingsMenu : MonoBehaviour {
    InputActionRebindingExtensions.RebindingOperation rebindingOperation;
    [SerializeField] InputActionAsset playerActions;
    [SerializeField] InputActionReference[] rebindingActions;
    [SerializeField] GameObject[] buttonObjects;
    [SerializeField] GameObject[] waitingObjects;

    InputActionReference rebindingAction;
    OptionsMenu options;
    int bindingIndex;

    void Start() {
        options = gameObject.GetComponent<OptionsMenu>();
        string rebinds = options.rebinds;
        if (!string.IsNullOrEmpty(rebinds)) {
            playerActions.LoadBindingOverridesFromJson(rebinds);
        }

        for (int index = 0; index < 8; index++) {
            rebindingAction = rebindingActions[index / 2];

            if (index % 2 == 0) {
                bindingIndex = rebindingAction.action.GetBindingIndex(InputBinding.MaskByGroup("Keyboard"));
            }
            else {
                bindingIndex = rebindingAction.action.GetBindingIndex(InputBinding.MaskByGroup("Gamepad"));
            }
            buttonObjects[index].transform.GetChild(0).GetComponent<Text>().text =
                InputControlPath.ToHumanReadableString(rebindingAction.action.bindings[bindingIndex].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
        }
    }

    public void StartRebinding(int index) {
        rebindingAction = rebindingActions[index / 2];

        if (index % 2 == 0) {
            bindingIndex = rebindingAction.action.GetBindingIndex(InputBinding.MaskByGroup("Keyboard"));
        }
        else {
            bindingIndex = rebindingAction.action.GetBindingIndex(InputBinding.MaskByGroup("Gamepad"));
        }

        buttonObjects[index].SetActive(false);
        waitingObjects[index].SetActive(true);

        rebindingOperation = rebindingAction.action.PerformInteractiveRebinding(bindingIndex)
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => RebindingComplete(index))
            .OnCancel(operation => RebindingComplete(index))
            .WithCancelingThrough("<Gamepad>/start")
            .WithCancelingThrough("<Keyboard>/escape");

        rebindingOperation.Start();
    }

    void RebindingComplete(int index) {
        buttonObjects[index].transform.GetChild(0).GetComponent<Text>().text =
            InputControlPath.ToHumanReadableString(rebindingAction.action.bindings[bindingIndex].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
        rebindingOperation.Dispose();

        buttonObjects[index].SetActive(true);
        waitingObjects[index].SetActive(false);

        string rebinds = playerActions.SaveBindingOverridesAsJson();
        options.rebinds = rebinds;
    }
}
