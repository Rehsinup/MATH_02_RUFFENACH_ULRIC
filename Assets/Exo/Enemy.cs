using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [System.Serializable]
    private struct MovementValues
    {
        public float MaxSpeed;
        public float Acceleration;
        public float MaxAcceleration;
        [Tooltip("Durée avant changement de dir")]
        public float MoveDuration;
    }

    [Header("Movement Settings")]
    [SerializeField]
    private MovementValues _movementValues = new MovementValues()
    {
        MaxSpeed = 3f,
        Acceleration = 8f,
        MaxAcceleration = 10f,
        MoveDuration = 2f
    };

    private Rigidbody2D _rigidbody;
    private Vector2 _currentVelocity = Vector2.zero;
    private float _direction = 1f; 
    private float _timer = 0f;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _rigidbody.gravityScale = 0f;
    }

    private void FixedUpdate()
    {
        _timer += Time.fixedDeltaTime;

        if (_timer >= _movementValues.MoveDuration)
        {
            _timer = 0f;
            _direction *= -1f;
        }

        Move();
    }

    private void Move()
    {
        Vector2 targetVelocity = new Vector2(_movementValues.MaxSpeed * _direction, 0f);

        _currentVelocity = Vector2.MoveTowards(
            _rigidbody.velocity,
            targetVelocity,
            _movementValues.Acceleration * Time.fixedDeltaTime
        );

        _currentVelocity = Vector2.ClampMagnitude(_currentVelocity, _movementValues.MaxAcceleration);

        _rigidbody.velocity = _currentVelocity;
    }

  
}
