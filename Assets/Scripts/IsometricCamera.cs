using UnityEngine;

namespace MidLane
{
    public sealed class IsometricCamera : MonoBehaviour
    {
        LaneWorld world;
        Camera view;
        Vector3 focus;
        float distance;
        public bool Locked { get; private set; }
        Vector3 direction;
        public Vector3 Focus { get { return focus; } }
        public Vector3 TeamOffset
        {
            get { return (world.PlayerTeam == Team.Blue ? Vector3.right : Vector3.left) * world.Settings.lockedCameraOffset; }
        }
        public void Initialize(LaneWorld lane)
        {
            world = lane; view = GetComponent<Camera>();
            ScenePresentationConfig config = world.Presentation;
            view.orthographic = false; view.fieldOfView = config.camera_fov_degrees;
            // Unity positive X is the downward-looking pitch. Negative X would look skyward.
            transform.rotation = Quaternion.Euler(config.camera_pitch_degrees, config.camera_yaw_degrees, 0);
            direction = -transform.forward; distance = config.CameraMaxDistance; Locked = true;
        }
        public void Snap()
        {
            if (world.Player != null) { focus = world.Player.Position + TeamOffset; Apply(); }
        }
        public void SetLocked(bool value) { Locked = value; if (value) Snap(); }
        public void OnPlayerRespawn() { if (Locked) Snap(); }
        public void Pan(Vector2 screenDirection, float delta)
        {
            Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            Vector3 up = Vector3.ProjectOnPlane(transform.up, Vector3.up).normalized;
            focus += (right * screenDirection.x + up * screenDirection.y) * delta;
            focus = new Vector3(Mathf.Clamp(focus.x, -world.Settings.halfLength, world.Settings.halfLength), 0,
                Mathf.Clamp(focus.z, -world.Settings.halfWidth - 5, world.Settings.halfWidth + 5)); Apply();
        }
        void Apply() { transform.position = focus + direction * distance; }
        void LateUpdate()
        {
            if (world.Player == null || world.Paused || ShopHud.Open) return;
            var hud = world.GetComponent<PracticeHud>(); bool typing = hud != null && hud.LatencyFieldFocused;
            if (!typing && Input.GetKeyDown(KeyCode.Y)) SetLocked(!Locked);
            Vector2 pointer = Input.mousePosition;
            bool inside = pointer.x > 0 && pointer.x < Screen.width && pointer.y > 0 && pointer.y < Screen.height;
            if (Locked || Input.GetKey(KeyCode.Space)) { Snap(); return; }
            if (typing) return;
            Vector2 pan = new Vector2(Input.GetKey(KeyCode.RightArrow) ? 1 : Input.GetKey(KeyCode.LeftArrow) ? -1 : 0,
                Input.GetKey(KeyCode.UpArrow) ? 1 : Input.GetKey(KeyCode.DownArrow) ? -1 : 0);
            if (inside && Application.isFocused && !ShopHud.Open)
            {
                if (pointer.x < 8) pan.x = -1; else if (pointer.x > Screen.width - 8) pan.x = 1;
                if (pointer.y < 8) pan.y = -1; else if (pointer.y > Screen.height - 8) pan.y = 1;
            }
            Pan(pan.normalized, 39 * Time.unscaledDeltaTime); Apply();
        }
    }
}
