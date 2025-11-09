using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectable : MonoBehaviour
{
    [SerializeField] private float _oscillationAmplitude = 0.0f;
    [SerializeField] private float _oscillationFrequency = 0.0f;

    [SerializeField] private float _rotationSpeed = 0.0f;

    [SerializeField] private bool _reverseGravity = false;

    private Vector3 _basePosition = Vector3.zero;

    private void Awake()
    {
        _basePosition = transform.position;
    }

    private void Update()
    {
        float oscillation = Mathf.Sin(Time.time * _oscillationFrequency);
        oscillation = (oscillation + 1.0f) / 2.0f;
        oscillation *= _oscillationAmplitude;
        oscillation *= _reverseGravity ? -1.0f : 1.0f;
        transform.position = _basePosition + new Vector3(0.0f, oscillation, 0.0f);

        transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);
    }
}
