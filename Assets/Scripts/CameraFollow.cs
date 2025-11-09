using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CameraFollow : MonoBehaviour
{
    [Serializable]
    private struct FollowParameters
    {
        public float FollowSpeed;
        public AnimationCurve SpeedFactorFromOffset;
    }

    [SerializeField] private Rigidbody2D _objectToFollow = null;

    [SerializeField] private Vector2 _targetOffset = Vector2.zero;
    [SerializeField] private FollowParameters _horizontalFollow = new FollowParameters();
    [SerializeField] private FollowParameters _verticalFollow = new FollowParameters();

    private Vector3 _newPosition = Vector3.zero;
    private float _yOffset = 0.0f;
    private PlayerCharacter _player = null;

    private void Awake()
    {
        _yOffset = transform.position.y - _objectToFollow.transform.position.y;
        _player = _objectToFollow.GetComponent<PlayerCharacter>();
    }

    private void FixedUpdate()
    {
        _newPosition = transform.position;

        HorizontalMovement();
        VerticalMovement();

        transform.position = _newPosition;
    }

    private void HorizontalMovement()
    {
        if (_objectToFollow.velocity.x == 0.0f)
            return;

        if (_objectToFollow.velocity.x > 0.0f)
        {
            float rightTargetOffset = _objectToFollow.transform.position.x + _targetOffset.y;
            float distance = Mathf.Abs(rightTargetOffset - transform.position.x);
            float speed = _horizontalFollow.FollowSpeed * _horizontalFollow.SpeedFactorFromOffset.Evaluate(distance) * Time.fixedDeltaTime;
            _newPosition.x = Mathf.MoveTowards(_newPosition.x, rightTargetOffset, speed);
        }
        else
        {
            float leftTargetOffset = _objectToFollow.transform.position.x - _targetOffset.x;
            float distance = Mathf.Abs(leftTargetOffset - transform.position.x);
            float speed = _horizontalFollow.FollowSpeed * _horizontalFollow.SpeedFactorFromOffset.Evaluate(distance) * Time.fixedDeltaTime;
            _newPosition.x = Mathf.MoveTowards(_newPosition.x, leftTargetOffset, speed);
        }
    }

    private void VerticalMovement()
    {
        if (_player == null || _player.IsGrounded)
        {
            float targetYPosition = _objectToFollow.transform.position.y + _yOffset;
            float distance = Mathf.Abs(targetYPosition - transform.position.y);
            float speed = _verticalFollow.FollowSpeed * _verticalFollow.SpeedFactorFromOffset.Evaluate(distance) * Time.fixedDeltaTime;
            _newPosition.y = Mathf.MoveTowards(_newPosition.y, targetYPosition, speed);
        }
    }
}
