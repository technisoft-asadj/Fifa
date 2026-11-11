using UnityEngine;

namespace FootballSim.CameraSystem
{
    /// <summary>Smooth broadcast camera that tracks a blend of the ball and the controlled player.</summary>
    public class BroadcastCamera : MonoBehaviour
    {
        [SerializeField] Transform ball;
        [SerializeField] Transform player;
        [SerializeField] float height = 18f;
        [SerializeField] float distance = 22f;
        [SerializeField] float smoothTime = 0.35f;
        [SerializeField, Range(0f, 1f)] float ballWeight = 0.7f;
        [SerializeField] Vector2 pitchHalfExtents = new Vector2(30f, 45f);

        Vector3 vel;

        /// <summary>Set the player the camera blends toward (called when control switches).</summary>
        public void SetPlayer(Transform p) { player = p; }

        void LateUpdate()
        {
            if (ball == null) return;
            Vector3 focus = ball.position;
            if (player) focus = Vector3.Lerp(player.position, ball.position, ballWeight);
            focus.x = Mathf.Clamp(focus.x, -pitchHalfExtents.x, pitchHalfExtents.x);
            focus.z = Mathf.Clamp(focus.z, -pitchHalfExtents.y, pitchHalfExtents.y);
            focus.y = 0f;

            Vector3 desired = focus + new Vector3(0f, height, -distance);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref vel, smoothTime);
            Quaternion look = Quaternion.LookRotation(focus - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 1f - Mathf.Exp(-6f * Time.deltaTime));
        }
    }
}
