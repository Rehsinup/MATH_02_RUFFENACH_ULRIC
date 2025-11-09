using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerCharacter : MonoBehaviour
{
    #region DataStructure

    public enum PhysicState
    {
        Ground,
        Air
    }

    [Serializable]
    private struct MovementValues
    {
        public float MaxSpeed;
        public float Acceleration;
        public float MaxAcceleration;
        [Tooltip("Range [-1, 1]")] public AnimationCurve AccelerationRemapFromVelocityDot;
    }

    [Serializable]
    private struct GravityValues
    {
        public float MaxForce;
        public float Acceleration;
        public float MaxAcceleration;
        public float CoyoteTime;
        [Tooltip("Range [0, 1]")] public AnimationCurve GravityRemapFromCoyoteTime;
    }

    [Serializable]
    private struct JumpValues
    {
        public float ImpulseForce;
        public float Deceleration;
        public float MaxDeceleration;
        [Tooltip("Range [0, 1]")] public AnimationCurve DecelerationFromAirTime;
        public float Height;
        public float BufferTime;
        public float Bounciness;
    }

    #endregion DataStructure

    #region EditorVariables

    [Header("Gameplay")]
    [SerializeField] private MovementValues _groundPhysic = new MovementValues();
    [SerializeField] private MovementValues _sprintPhysic = new MovementValues();
    [SerializeField] private MovementValues _airPhysic = new MovementValues();
    [SerializeField] private GravityValues _gravityParameters = new GravityValues();
    [SerializeField] private JumpValues _jumpParameters = new JumpValues();
    [SerializeField] private ContactFilter2D _groundContactFilter = new ContactFilter2D();
    [SerializeField] private ContactFilter2D _ceilingContactFilter = new ContactFilter2D();

    [Header("Setup")]
    [SerializeField] private Transform _mesh = null;
    [SerializeField] private float _meshRotationSpeed = 10.0f;

    [Header("Bomb")]
    [SerializeField] private Bomb _bombPrefab;     
    [SerializeField] private Transform _bombSpawnPoint; 
    #endregion EditorVariables

    #region Variables

    //Components
    private Rigidbody2D _rigidbody = null;

    //Force
    private Vector2 _forceToAdd = Vector2.zero;
    private Vector2 _prePhysicPosition = Vector2.zero;

    //Horizontal movement
    private Vector2 _currentHorizontalVelocity = Vector2.zero;
    private float _movementInput = 0.0f;
    private MovementValues _horizontalPhysic = new MovementValues();
    private bool _isSprinting = false;

    //Gravity
    private float _currentGravity = 0.0f;
    private bool _isGravityReversed = false;

    //Ground
    public bool IsGrounded { get; private set; } = true;

    //Air
    private float _airTime = 0.0f;
    private bool _isInCoyoteTime = false;

    //Jump
    private Vector2 _currentJumpForce = Vector2.zero;
    private bool _isJumping = false;
    private float _jumpTime = 0.0f;
    private float _startJumpTime = 0.0f;
    private bool _bufferJump = false;
    private bool _hasBounce = false;

    private Vector3 _currentMeshRotation = Vector3.zero;

    public event Action<PhysicState> OnPhysicStateChanged;

    #endregion Variables

    #region Initialization

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _horizontalPhysic = _groundPhysic;
        _currentMeshRotation = _mesh.eulerAngles;
        CalculateJumpTime();

        OnPhysicStateChanged += ChangePhysic;
        OnPhysicStateChanged += ResetGravity;
        OnPhysicStateChanged += CancelJump;
        OnPhysicStateChanged += TryJumpBuffer;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CalculateJumpTime();
    }
#endif

    private void CalculateJumpTime()
    {
        _jumpTime = _jumpParameters.Height / _jumpParameters.ImpulseForce;
    }

    #endregion Initialization

    #region Visual

    private void Update()
    {
        RotateMesh();
    }

    private void RotateMesh()
    {
        bool hasChange = false;

        if (_currentHorizontalVelocity != Vector2.zero)
        {
            float targetRotation = 0.0f;

            float dotDirection = Vector2.Dot(Vector2.right, _currentHorizontalVelocity);

            if (_isGravityReversed)
                targetRotation = dotDirection > 0.0f ? 180.0f : 0.0f;
            else
                targetRotation = dotDirection > 0.0f ? 0.0f : 180.0f;

            _currentMeshRotation.y = Mathf.MoveTowards(_currentMeshRotation.y, targetRotation, _meshRotationSpeed * Time.deltaTime);
            hasChange = true;
        }

        if ((_isGravityReversed && _currentMeshRotation.z != 180.0f) || (!_isGravityReversed && _currentMeshRotation.z != 0.0f))
        {
            float targetRotation = _isGravityReversed ? 180.0f : 0.0f;
            _currentMeshRotation.z = Mathf.MoveTowards(_currentMeshRotation.z, targetRotation, _meshRotationSpeed * Time.deltaTime);
            hasChange = true;
        }

        if (hasChange)
            _mesh.rotation = Quaternion.Euler(_currentMeshRotation);
    }

    #endregion Visual

    private void FixedUpdate()
    {
        _forceToAdd = Vector2.zero;
        _prePhysicPosition = _rigidbody.position;

        GroundDetection();
        ManageAirTime();
        ManageCoyoteTime();

        Movement();
        Gravity();
        JumpForce();

        _rigidbody.velocity += _forceToAdd;
    }

    private void LateUpdate()
    {
        if (_prePhysicPosition == _rigidbody.position && _forceToAdd != Vector2.zero)
        {
            _rigidbody.velocity = new Vector2(0.0f, _rigidbody.velocity.y);
            _currentHorizontalVelocity.x = 0.0f;
        }
    }

    #region PhysicState

    private void GroundDetection()
    {
        ContactFilter2D filter = _isGravityReversed ? _ceilingContactFilter : _groundContactFilter;
        bool isTouchingGround = _rigidbody.IsTouching(filter);

        if (isTouchingGround && !IsGrounded)
        {
            IsGrounded = true;
            OnPhysicStateChanged.Invoke(PhysicState.Ground);
        }
        else if (!isTouchingGround && IsGrounded)
        {
            IsGrounded = false;
            if (!_isJumping)
                _isInCoyoteTime = true;
            OnPhysicStateChanged.Invoke(PhysicState.Air);
        }
    }

    private void ManageAirTime()
    {
        if (!IsGrounded)
            _airTime += Time.fixedDeltaTime;
    }

    private void ManageCoyoteTime()
    {
        if (_airTime > _gravityParameters.CoyoteTime)
            _isInCoyoteTime = false;
    }

    private void ChangePhysic(PhysicState groundState)
    {
        if (groundState == PhysicState.Ground && !_isSprinting)
            _horizontalPhysic = _groundPhysic;
        else if (groundState == PhysicState.Ground && _isSprinting)
            _horizontalPhysic = _sprintPhysic;
        else if (groundState == PhysicState.Air)
            _horizontalPhysic = _airPhysic;
    }

    public void ActionOne()
    {
        _isSprinting = !_isSprinting;

        if (IsGrounded)
            _horizontalPhysic = _isSprinting ? _sprintPhysic : _groundPhysic;
    }

    #endregion PhysicState

    #region HorizontalMovement

    public void GetMovementInput(float input)
    {
        _movementInput = input;
    }

    private void Movement()
    {
        Vector2 maxSpeed = SnapToGround(_movementInput);
        float velocityDot = Mathf.Clamp(Vector2.Dot(_rigidbody.velocity, maxSpeed), -1.0f, 1.0f);
        velocityDot = _horizontalPhysic.AccelerationRemapFromVelocityDot.Evaluate(velocityDot);
        float acceleration = _horizontalPhysic.Acceleration * velocityDot * Time.fixedDeltaTime;

        _currentHorizontalVelocity = Vector2.MoveTowards(_currentHorizontalVelocity, maxSpeed, acceleration);

        Vector2 velocityDelta = _currentHorizontalVelocity - _rigidbody.velocity;
        if (_currentHorizontalVelocity.y == 0.0f)
            velocityDelta.y = 0.0f;

        velocityDelta = Vector2.ClampMagnitude(velocityDelta, _horizontalPhysic.MaxAcceleration);

        _forceToAdd += velocityDelta;
    }

    private Vector2 SnapToGround(float input)
    {
        if (!IsGrounded || _isJumping)
            return new Vector2(input * _horizontalPhysic.MaxSpeed, 0.0f);

        ContactPoint2D[] contactPointArray = new ContactPoint2D[1];
        ContactFilter2D filter = _isGravityReversed ? _ceilingContactFilter : _groundContactFilter;
        _rigidbody.GetContacts(filter, contactPointArray);
        Vector2 normal = contactPointArray.Length > 0 ? contactPointArray[0].normal : Vector2.zero;

        if (normal == Vector2.zero || (normal == Vector2.up && !_isGravityReversed) || (normal == Vector2.down && _isGravityReversed) || input == 0.0f)
            return new Vector2(input * _horizontalPhysic.MaxSpeed, 0.0f);

        Vector3 force = Vector3.zero;

        if (input > 0.0f)
            force = Vector3.Cross(normal, Vector3.forward);
        else
            force = Vector3.Cross(normal, Vector3.back);

        return _horizontalPhysic.MaxSpeed * force;
    }

    #endregion HorizontalMovement

    #region Gravity

    private void Gravity()
    {
        if (IsGrounded || _isJumping)
            return;

        float coyoteTimeRatio = Mathf.Clamp01(_airTime / _gravityParameters.CoyoteTime);
        float coyoteTimeFactor = _isInCoyoteTime ? _gravityParameters.GravityRemapFromCoyoteTime.Evaluate(coyoteTimeRatio) : 1.0f;
        float acceleration = _gravityParameters.Acceleration * coyoteTimeFactor * Time.fixedDeltaTime;

        float maxGravityForce = _isGravityReversed ? -_gravityParameters.MaxForce : _gravityParameters.MaxForce;
        _currentGravity = Mathf.MoveTowards(_currentGravity, maxGravityForce, acceleration);

        float velocityDelta = _currentGravity - _rigidbody.velocity.y;

        if (_isGravityReversed)
            velocityDelta = Mathf.Clamp(velocityDelta, 0.0f, _gravityParameters.MaxAcceleration);
        else
            velocityDelta = Mathf.Clamp(velocityDelta, -_gravityParameters.MaxAcceleration, 0.0f);

        _forceToAdd.y += velocityDelta;
    }

    private void ResetGravity(PhysicState physicState)
    {
        if (physicState != PhysicState.Air)
        {
            _currentGravity = 0.0f;
            _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, 0.0f);
            _airTime = 0.0f;
        }
    }

    public void ReverseGravity()
    {
        _isGravityReversed = !_isGravityReversed;
    }

    #endregion Gravity

    #region Jump

    public void StartJump()
    {
        if ((!IsGrounded && !_isInCoyoteTime) || _isJumping)
        {
            _bufferJump = true;
            Invoke(nameof(StopJumpBuffer), _jumpParameters.BufferTime);
            return;
        }

        _currentJumpForce.y = _isGravityReversed ? -_jumpParameters.ImpulseForce : _jumpParameters.ImpulseForce;
        _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, _currentJumpForce.y);
        _currentHorizontalVelocity.y = 0.0f; 
        _isJumping = true;
        _isInCoyoteTime = false;
        _startJumpTime = _airTime;
        _hasBounce = false;
    }

    private void StopJumpBuffer()
    {
        _bufferJump = false;
    }

    private void JumpForce()
    {
        if (!_isJumping)
            return;

        ContactPoint2D[] contactPointArray = new ContactPoint2D[1];
        ContactFilter2D filter = _isGravityReversed ? _groundContactFilter : _ceilingContactFilter;
        _rigidbody.GetContacts(filter, contactPointArray);
        Vector2 normal = contactPointArray.Length > 0 ? contactPointArray[0].normal : Vector2.zero;

        _currentJumpForce = GetBounceForce(_currentJumpForce, normal, _jumpParameters.Bounciness, ref _hasBounce);

        float jumpTimeRatio = Mathf.Clamp01((_airTime - _startJumpTime) / _jumpTime);
        float deceleration = _jumpParameters.Deceleration * _jumpParameters.DecelerationFromAirTime.Evaluate(jumpTimeRatio) * Time.fixedDeltaTime;

        _currentJumpForce = Vector2.MoveTowards(_currentJumpForce, Vector2.zero, deceleration);

        Vector2 velocityDelta = _currentJumpForce - _rigidbody.velocity;
        if (_currentJumpForce.x == 0.0f)
            velocityDelta.x = 0.0f;
        velocityDelta = Vector2.ClampMagnitude(velocityDelta, _jumpParameters.MaxDeceleration);

        _forceToAdd += velocityDelta;

        if (jumpTimeRatio >= 1.0f)
        {
            _isJumping = false;
            _currentJumpForce = Vector2.zero;

            //if (_hasBounce)
            //{
            //    Debug.Log("Bounce");
            //    _currentGravity = _rigidbody.velocity.y;
            //    _airTime = 0.0f;
            //}
        }
    }

    private Vector2 GetBounceForce(Vector2 initialForce, Vector2 normal, float bouciness, ref bool hasBounce)
    {
        if (!hasBounce && normal != Vector2.zero && ((normal.y > 0 && _isGravityReversed) || (normal.y < 0 && !_isGravityReversed)))
        {
            float dot = Vector2.Dot(initialForce, normal);
            Vector2 projectedVector = -2 * dot * normal;
            Vector2 bounceForce = bouciness * (projectedVector + initialForce);
            hasBounce = true;
            return bounceForce;
        }
        return initialForce;
    }

    private void CancelJump(PhysicState state)
    {
        if (state != PhysicState.Air)
        {
            _isJumping = false;
            _currentJumpForce = Vector2.zero;
        }
    }

    private void TryJumpBuffer(PhysicState state)
    {
        if (state != PhysicState.Air && _bufferJump)
        {
            StartJump();
            _bufferJump = false;
            CancelInvoke(nameof(StopJumpBuffer));
        }
    }

    #endregion Jump

    public void ActionTwo()
    {
        Bomb bomb = Instantiate(_bombPrefab, _bombSpawnPoint.position, Quaternion.identity);

        int direction = _currentHorizontalVelocity.x < 0 ? -1 : 1;
        bomb.Launch(direction, _currentHorizontalVelocity.x);
    }

}