﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectPlayer : MonoBehaviour
{
    [SerializeField] private Wire parentWire;
    [SerializeField] private LineRenderer line;
    private Transform player;

    private void Start()
    {
        //If this wire is broken, set the start of the line to the end of this wire.
        //Otherwise, set it to the start of this wire.
        //This ensures the line originates from the broken side of the wire.
        line.SetPosition
            (
                0,
                parentWire.type == WireType.Broken ? parentWire.end.position : parentWire.start.position
            );
        line.enabled = false;
    }

    private void Update()
    {
        //Make sure the line is only seen when the player is close.
        line.enabled = parentWire.playerClose;

        //If the player is close, and we have a reference to the player (we should), update the end
        //position to be the player's position.
        if (parentWire.playerClose)
        {
            if (player) { line.SetPosition(1, player.position); }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.transform;
            parentWire.playerClose = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            parentWire.playerClose = false;
        }
    }
}