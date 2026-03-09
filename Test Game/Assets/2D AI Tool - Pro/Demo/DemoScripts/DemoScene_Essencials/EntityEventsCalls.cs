using AI2DTool;
using UnityEngine;

namespace MaykerStudio
{
    [RequireComponent(typeof(EntityDelegates))]
    public class EntityEventsCalls : MonoBehaviour
    {
        private ShakeIn2D shakeScript;

        public Sound targetSpottedSound;

        public GameObject autoDestroySound;

        EntityDelegates entityDelegates;

        public bool ShowDebugLog;

        private bool HasDetected;

        private float detectedCounter;

        private void Start()
        {
            shakeScript = Camera.main.GetComponent<ShakeIn2D>();
        }

        private void OnEnable()
        {
            if (entityDelegates == null)
                entityDelegates = GetComponent<EntityDelegates>();

            entityDelegates.OnDamageReceive += OnDamageReceive;
            entityDelegates.OnEntityDead += OnEntityDead;
            entityDelegates.OnEntityStunned += OnentityStunned;
            entityDelegates.OnEntityRecoveredStunned += OnentityRecoveredStunned;
            entityDelegates.OnDropAttackEnd += OnDropAttackEnd;
            entityDelegates.OnTargetDetected += OnTargetDetected;
            entityDelegates.OnTargetNotDetected += OnTargetNotDetected;
            entityDelegates.OnDamageSend += OnDamageSend;
        }

        private void OnDisable()
        {
            entityDelegates.OnDamageReceive -= OnDamageReceive;
            entityDelegates.OnEntityDead -= OnEntityDead;
            entityDelegates.OnEntityStunned -= OnentityStunned;
            entityDelegates.OnEntityRecoveredStunned -= OnentityRecoveredStunned;
            entityDelegates.OnDropAttackEnd -= OnDropAttackEnd;
            entityDelegates.OnTargetDetected -= OnTargetDetected;
            entityDelegates.OnTargetNotDetected -= OnTargetNotDetected;
            entityDelegates.OnDamageSend -= OnDamageSend;
        }

        private void OnentityRecoveredStunned(Entity entity)
        {
            //Do something when entity is recovered from stunned.
        }

        private void OnentityStunned(Entity entity)
        {
            //Do something when entity is stunned.
        }

        private void OnEntityDead(Entity entity, int score)
        {
            //Do something when entity is dead.
        }

        private void OnDamageReceive(Entity entity, DamageDetails details)
        {
            if (ShowDebugLog)
                Debug.Log(entity.name + " receive damage");


            shakeScript._Direction = ((Vector2)transform.position - details.position).normalized;
            shakeScript.FireOnce(.1f);
        }

        private void OnDamageSend(Entity entity, GameObject target, DamageDetails details)
        {
            if (ShowDebugLog)
                Debug.Log(entity.name + " send damage to: " + target.name);
        }


        private void OnDropAttackEnd(Entity entity)
        {
            if (ShowDebugLog)
                Debug.Log(entity.name + " finish a dropAttack");

            shakeScript.FireOnce(1f);
        }

        private void OnTargetDetected(Entity entity)
        {
            detectedCounter = 0f;

            if (!HasDetected)
            {
                HasDetected = true;

                GameObject st = Instantiate(autoDestroySound, entity.transform.position, Quaternion.identity);

                SoundWithTimer stS = st.GetComponent<SoundWithTimer>();

                StartCoroutine(stS.PlaySound(targetSpottedSound));
            }
        }

        private void OnTargetNotDetected(Entity entity)
        {
            detectedCounter += Time.deltaTime;
            if (detectedCounter >= 1.5f)
                HasDetected = false;
        }
    }
}