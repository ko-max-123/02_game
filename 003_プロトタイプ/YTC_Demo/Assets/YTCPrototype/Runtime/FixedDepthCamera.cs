using UnityEngine;

namespace YTCPrototype
{
    [RequireComponent(typeof(Camera))]
    public sealed class FixedDepthCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 followOffset = new Vector2(0f, 3f);
        [SerializeField] private float fixedDepth = -12f;
        [SerializeField, Min(0.01f)] private float smoothTime = 0.12f;
        [SerializeField] private Vector3 fixedEulerAngles = new Vector3(8f, 0f, 0f);

        private Vector3 velocity;

        public void Configure(Transform followTarget, float cameraDepth)
        {
            target = followTarget;
            fixedDepth = cameraDepth;
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desired = PrototypeMovementMath.CalculateFixedDepthCameraPosition(
                target.position,
                followOffset,
                fixedDepth);

            Vector3 smoothedPosition = Vector3.SmoothDamp(
                transform.position,
                desired,
                ref velocity,
                smoothTime);
            smoothedPosition.z = fixedDepth;
            transform.position = smoothedPosition;
            transform.rotation = Quaternion.Euler(fixedEulerAngles);
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            transform.position = PrototypeMovementMath.CalculateFixedDepthCameraPosition(
                target.position,
                followOffset,
                fixedDepth);
            transform.rotation = Quaternion.Euler(fixedEulerAngles);
            velocity = Vector3.zero;
        }
    }
}
