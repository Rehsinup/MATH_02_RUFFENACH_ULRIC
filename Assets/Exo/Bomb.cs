using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bomb : MonoBehaviour
{
    [System.Serializable]
    private struct BombValues
    {
        public float LaunchForceX;
        public float LaunchForceY;
        public float Bounciness;
        public int MaxBounces;
        public float KnockbackForce;
    }

    [SerializeField] private BombValues _values = new BombValues();

    private Rigidbody2D _rigidbody;
    private Vector2 _velocity;
    private int _bounceCount = 0;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _rigidbody.gravityScale = 0f;
    }

    public void Launch(int direction, float playerHorizontalVelocity)
    {
        _velocity = new Vector2(_values.LaunchForceX * direction + playerHorizontalVelocity, _values.LaunchForceY);
    }

    private void FixedUpdate()
    {
        _rigidbody.velocity = _velocity;
        _velocity.y -= 9.81f * Time.fixedDeltaTime;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f || contact.normal.y < -0.5f)
            {
                if (_bounceCount < _values.MaxBounces)
                {
                    _velocity.y = -_velocity.y * _values.Bounciness;
                    _bounceCount++;
                }
                else
                {
                    _velocity = Vector2.zero;
                    _rigidbody.isKinematic = true;
                }
            }
        }

        if (collision.collider.CompareTag("Enemy"))
        {
            Rigidbody2D enemyRb = collision.collider.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                Vector2 knockbackDir = (collision.collider.transform.position - transform.position).normalized;
                enemyRb.velocity = knockbackDir * _values.KnockbackForce;

                MonoBehaviour enemyScript = collision.collider.GetComponent<MonoBehaviour>();
                if (enemyScript != null)
                {
                    enemyScript.enabled = false;
                    StartCoroutine(ReactivateEnemy(enemyScript, 0.5f));
                }
            }
        }
    }

    private IEnumerator ReactivateEnemy(MonoBehaviour enemyScript, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (enemyScript != null)
            enemyScript.enabled = true;
    }
}
