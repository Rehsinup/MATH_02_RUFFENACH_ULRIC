using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

public class PlayerInputsManager : MonoBehaviour
{
    private PlayerInputs _inputs = null;
    private PlayerCharacter _character = null;

    private void Awake()
    {
        _character = GetComponent<PlayerCharacter>();

        _inputs = new PlayerInputs();
        _inputs.Actions.Jump.started += _ => _character.StartJump();
        _inputs.Actions.ActionOne.started += _ => _character.ActionOne();
        _inputs.Actions.ActionTwo.started += _ => _character.ActionTwo();
        _inputs.Enable();
    }

    private void FixedUpdate()
    {
        _character.GetMovementInput(_inputs.Actions.Move.ReadValue<float>());
    }
}
