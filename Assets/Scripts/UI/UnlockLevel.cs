﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public class UnlockLevel : MonoBehaviour
{
    [SerializeField] private Button button = null;
    [SerializeField] private RawImage boltIcon = null;
    [SerializeField] private Texture emptyIcon = null;
    [SerializeField] private Texture filledIcon = null;
    [Space(10)]
    [SerializeField] private int index = -1;
    [Space(10)]
    [SerializeField] private bool levelHasBolt = false;

#if UNITY_EDITOR
    [SerializeField] private bool toggleBoltIcon = false;
    private void OnValidate()
    {
        if (toggleBoltIcon && boltIcon)
        {
            boltIcon.gameObject.SetActive(!boltIcon.gameObject.activeSelf);
            toggleBoltIcon = false;
        }
    }
#endif

    void Start()
    {
        //If the level doesn't have a bolt to collect, deactivate the bolt icon.
        boltIcon.gameObject.SetActive(levelHasBolt);

        //If the player hasn't gotten to this level yet, make the button unclickable and deactivate the bolt icon.
        if (index > SaveManager.CachedData.LastCompletedIndex + 1)
        {
            button.interactable = false;
            boltIcon.gameObject.SetActive(false);
        }

        //If the player has collected the bolt in this level, make the bolt icon filled in.
        //Otherwise, make it empty.
        else if (levelHasBolt && SaveManager.CachedData.CollectedBoltIndices.Contains(index))
        {
            boltIcon.texture = filledIcon;
        }
        else
        {
            boltIcon.texture = emptyIcon;
        }
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Keyboard.current.digit1Key.isPressed && Keyboard.current.digit3Key.isPressed
            && Keyboard.current.numpad1Key.isPressed && Keyboard.current.numpad3Key.isPressed)
        {
            button.interactable = true;
        }
    }
#endif
}
