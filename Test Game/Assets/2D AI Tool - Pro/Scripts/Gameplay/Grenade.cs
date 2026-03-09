using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AI2DTool
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
    public class Grenade : MonoBehaviour
    {
        public string loop_animation;

        public string ground_explosion;

        public string air_explosion;

        [TagSelector]
        public string playerTag;

        [SerializeField]
        private ContactFilter2D GroundContactFilter;

        [SerializeField]
        private int Damage;

        [SerializeField]
        private int StunAmount;

        [SerializeField]
        private float criticalRate;

        [SerializeField]
        private int knockbackLevel = 4;

        [SerializeField]
        private float knockbackDuration = 0.3f;

        private Animator animator;

        private Rigidbody2D Rb;

        private bool IsAnimationFinish, IsExploding;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag(playerTag) && !IsExploding)
            {
                Explode(Rb.IsTouching(GroundContactFilter));
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (IsExploding && collision.CompareTag(playerTag))
            {
                if (collision.TryGetComponent(out IDamageable d))
                {
                    d.Damage(new DamageDetails()
                    {
                        damageAmount = Damage,
                        position = transform.position,
                        sender = gameObject,
                        stunDamageAmount = StunAmount
                    });

                    d.KnockBack(knockbackLevel, knockbackDuration, (collision.transform.position - transform.position).normalized);
                }
            }
        }

        private void Start()
        {
            animator = GetComponent<Animator>();
            Rb = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            if (!IsExploding && Rb.IsTouching(GroundContactFilter))
            {
                Explode(true);
            }
        }

        private void Update()
        {
            if (IsAnimationFinish)
            {
                Destroy(gameObject);
            }
        }

        public void Throw(Vector2 targetDir, float speed, float duration)
        {
            if (!animator)
                Start();

            IsExploding = false;

            IsAnimationFinish = false;

            animator.Play(loop_animation);

            Rb.isKinematic = false;

            Rb.AddForce(targetDir * speed, ForceMode2D.Impulse);
            Rb.AddTorque(speed / 100f);

            StartCoroutine(Timer(duration));
        }

        private IEnumerator Timer(float duration)
        {
            yield return new WaitForSeconds(duration);

            Explode(false);
        }

        private void Explode(bool ground)
        {
            StopAllCoroutines();

            IsExploding = true;

            Rb.linearVelocity = Vector2.zero;
            Rb.angularVelocity = 0f;
            Rb.isKinematic = true;

            transform.rotation = Quaternion.identity;

            if (ground)
            {
                animator.Play(ground_explosion);
            }
            else
            {
                animator.Play(air_explosion);
            }
        }

        public void AnimationFinish() => IsAnimationFinish = true;
    }
}