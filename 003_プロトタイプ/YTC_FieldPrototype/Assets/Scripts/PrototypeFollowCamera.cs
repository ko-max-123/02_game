using UnityEngine;

namespace YTC.Prototype
{
    public sealed class PrototypeFollowCamera : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0f, 3.8f, 10.5f);
        [SerializeField, Min(0.01f)] private float smoothTime = 0.12f;
        [SerializeField, Min(0f)] private float lookHeight = 1.1f;

        private Transform target;
        private Vector3 velocity;
        private float fixedDepth;

        public Vector3 Offset => offset;
        public float FixedDepth => fixedDepth;

        public void Configure(Transform followTarget)
        {
            target = followTarget;
            if (target == null)
            {
                return;
            }

            fixedDepth = target.position.z;
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (target == null)
            {
                return;
            }

            transform.position = Vector3.SmoothDamp(
                transform.position,
                FollowPosition(),
                ref velocity,
                smoothTime,
                Mathf.Infinity,
                Mathf.Max(0f, deltaTime));
            transform.LookAt(LookPosition());
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            velocity = Vector3.zero;
            transform.position = FollowPosition();
            transform.LookAt(LookPosition());
        }

        private Vector3 FollowPosition()
        {
            return new Vector3(
                target.position.x + offset.x,
                target.position.y + offset.y,
                fixedDepth + offset.z);
        }

        private Vector3 LookPosition()
        {
            return new Vector3(
                target.position.x,
                target.position.y + lookHeight,
                fixedDepth);
        }
    }
}
