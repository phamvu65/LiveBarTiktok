using UnityEngine;

namespace TikTokLiveGame
{
    public sealed class DjPerformerController : MonoBehaviour
    {
        private Transform[] arms = new Transform[6];
        private Quaternion[] rest = new Quaternion[6];
        private Quaternion[] displayedPose = new Quaternion[6];
        private Transform booth;
        private Vector3 homePosition;
        private Quaternion homeRotation;
        private Transform spine, head;
        private Quaternion spineRest, headRest;
        private Transform hips, waist;
        private Quaternion hipsRest, waistRest;
        private float danceStartedAt;

        private void Start()
        {
            booth = transform.root;
            homePosition = transform.localPosition;
            homeRotation = transform.localRotation;
            danceStartedAt = Time.time;
            Transform skeleton = transform;
            foreach (Transform candidate in GetComponentsInChildren<Transform>(true))
                if (candidate.name == "player_019_mint_swimsuit_skin_LOD0_Skeleton") { skeleton = candidate; break; }
            Transform[] rigBones = skeleton.GetComponentsInChildren<Transform>(true);
            foreach (Transform bone in rigBones)
            {
                if (bone.name == "Bip001-Spine2") spine = bone;
                if (bone.name == "Bip001-Head") head = bone;
                if (bone.name == "Bip001-Pelvis") hips = bone;
                if (bone.name == "Bip001-Spine") waist = bone;
            }
            if (spine != null) spineRest = spine.localRotation;
            if (head != null) headRest = head.localRotation;
            if (hips != null) hipsRest = hips.localRotation;
            if (waist != null) waistRest = waist.localRotation;
            string[] names = { "Bip001-L-UpperArm", "Bip001-L-Forearm", "Bip001-L-Hand", "Bip001-R-UpperArm", "Bip001-R-Forearm", "Bip001-R-Hand" };
            foreach (Transform bone in rigBones)
                for (int i = 0; i < names.Length; i++)
                    if (bone.name == names[i]) { arms[i] = bone; rest[i] = bone.localRotation; }
            foreach (Transform bone in arms)
                if (bone == null) { Debug.LogWarning("Mint DJ arm bones were not found."); enabled = false; return; }
            for (int i = 0; i < arms.Length; i++) displayedPose[i] = arms[i].localRotation;
        }

        private void LateUpdate()
        {
            float beat = ClubBeatClock.Beat * Mathf.PI * 2f * 1.25f;
            float entrance = Mathf.SmoothStep(0f, 1f, (Time.time - danceStartedAt) / 1.5f);
            float bodyBlend = 1f - Mathf.Exp(-Time.deltaTime * 6.5f);
            // Convert metres to model-local units so imported FBX scale cannot amplify the bounce.
            float unitsPerMetre = 1f / Mathf.Max(0.001f, transform.parent.lossyScale.y);
            Vector3 desiredPosition = homePosition + new Vector3(Mathf.Sin(beat * 0.5f) * 0.075f,
                (1f - Mathf.Cos(beat * 0.5f)) * 0.03f, Mathf.Sin(beat * 0.5f + 0.8f) * 0.035f) * unitsPerMetre * entrance;
            transform.localPosition = Vector3.Lerp(transform.localPosition, desiredPosition, bodyBlend);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, homeRotation * Quaternion.Euler(0f, Mathf.Sin(beat * 0.25f) * 5f, Mathf.Sin(beat * 0.5f) * 2.5f), bodyBlend);
            if (hips != null) hips.localRotation = Quaternion.Slerp(hips.localRotation,
                hipsRest * Quaternion.Euler(Mathf.Sin(beat * 0.5f) * 7f * entrance, Mathf.Sin(beat * 0.5f + 0.6f) * 12f * entrance, Mathf.Sin(beat * 0.5f) * 9f * entrance), bodyBlend);
            if (waist != null) waist.localRotation = Quaternion.Slerp(waist.localRotation,
                waistRest * Quaternion.Euler(Mathf.Sin(beat * 0.5f - 0.65f) * 8f * entrance, -Mathf.Sin(beat * 0.5f + 0.6f) * 8f * entrance, -Mathf.Sin(beat * 0.5f) * 7f * entrance), bodyBlend);
            if (spine != null) spine.localRotation = Quaternion.Slerp(spine.localRotation, spineRest * Quaternion.Euler(Mathf.Sin(beat * 0.5f - 1.1f) * 7f * entrance, Mathf.Sin(beat * 0.5f) * 9f * entrance, Mathf.Sin(beat * 0.5f - 0.6f) * 5f * entrance), bodyBlend);
            if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, headRest * Quaternion.Euler(Mathf.Sin(beat * 0.5f - 1.45f) * 5f, Mathf.Sin(beat * 0.25f - 0.4f) * 5f, 0f), bodyBlend);
            for (int i = 0; i < arms.Length; i++) arms[i].localRotation = rest[i];
            float sequence = (Time.time - danceStartedAt) * ClubBeatClock.Bpm / 60f / 8f;
            int pose = Mathf.FloorToInt(sequence) % 5;
            int nextPose = (pose + 1) % 5;
            float progress = Mathf.InverseLerp(0.68f, 1f, Mathf.Repeat(sequence, 1f));
            // Quintic easing has zero velocity and acceleration at both ends.
            float change = progress * progress * progress * (progress * (progress * 6f - 15f) + 10f);
            PoseTargets(pose, beat, out Vector3 left, out Vector3 right, out float leftWave, out float rightWave);
            PoseTargets(nextPose, beat, out Vector3 nextLeft, out Vector3 nextRight, out float nextLeftWave, out float nextRightWave);
            Vector3 leftTarget = Vector3.Lerp(new Vector3(-0.65f, 3.12f, -7.4f), HandArc(left, nextLeft, change), entrance);
            currentLeftTarget = Vector3.SmoothDamp(currentLeftTarget, leftTarget, ref leftVelocity, 0.07f, Mathf.Infinity, Time.deltaTime);
            Solve(0, booth.TransformPoint(currentLeftTarget), -booth.right);
            Vector3 rightTarget = Vector3.Lerp(new Vector3(0.35f, 3.1f, -7.38f), HandArc(right, nextRight, change), entrance);
            // Ease between the deck and the raised-hand pose instead of snapping at bar boundaries.
            currentRightTarget = Vector3.SmoothDamp(currentRightTarget, rightTarget, ref rightVelocity, 0.07f, Mathf.Infinity, Time.deltaTime);
            Solve(3, booth.TransformPoint(currentRightTarget), booth.right);
            arms[2].localRotation = rest[2] * Quaternion.Euler(0f, 0f, Mathf.Lerp(leftWave, nextLeftWave, change) * entrance);
            arms[5].localRotation = rest[5] * Quaternion.Euler(0f, 0f, Mathf.Lerp(rightWave, nextRightWave, change) * entrance);
            float armBlend = 1f - Mathf.Exp(-Time.deltaTime * 20f);
            for (int i = 0; i < arms.Length; i++)
            {
                displayedPose[i] = Quaternion.Slerp(displayedPose[i], arms[i].localRotation, armBlend);
                arms[i].localRotation = displayedPose[i];
            }
        }

        private Vector3 currentRightTarget = new(0.35f, 3.1f, -7.38f);
        private Vector3 currentLeftTarget = new(-0.65f, 3.12f, -7.4f);
        private Vector3 leftVelocity, rightVelocity;

        private static Vector3 HandArc(Vector3 from, Vector3 to, float t)
        {
            float arc = 16f * t * t * (1f - t) * (1f - t);
            return Vector3.Lerp(from, to, t) + new Vector3(0f, 0.12f, 0.16f) * arc;
        }

        private static void PoseTargets(int pose, float beat, out Vector3 left, out Vector3 right, out float leftWave, out float rightWave)
        {
            float sway = Mathf.Sin(beat * 0.5f) * 0.12f;
            left = new Vector3(-0.52f + sway, 3.65f + Mathf.Sin(beat) * 0.06f, -7.05f);
            right = new Vector3(0.52f + sway, 3.65f + Mathf.Sin(beat + 0.5f) * 0.06f, -7.05f);
            leftWave = rightWave = 0f;
            // Both forward, left up, right greeting, right up, left greeting.
            if (pose == 1) left = new Vector3(-0.5f + sway, 4.85f, -7.85f);
            if (pose == 3) right = new Vector3(0.5f + sway, 4.85f, -7.85f);
            if (pose == 2)
            {
                right = new Vector3(0.65f + Mathf.Sin(beat) * 0.1f, 4.1f, -7.7f);
                rightWave = Mathf.Sin(beat * 0.75f) * 16f;
            }
            if (pose == 4)
            {
                left = new Vector3(-0.65f + Mathf.Sin(beat) * 0.1f, 4.1f, -7.7f);
                leftWave = -Mathf.Sin(beat * 0.75f + 0.35f) * 16f;
            }
        }

        private void Solve(int index, Vector3 target, Vector3 outward)
        {
            Transform upper = arms[index], elbow = arms[index + 1], hand = arms[index + 2];
            Vector3 start = upper.position;
            float a = Vector3.Distance(start, elbow.position), b = Vector3.Distance(elbow.position, hand.position);
            if (a < 0.001f || b < 0.001f) return;
            Vector3 direction = target - start;
            // Retain an elbow bend instead of locking the arm at full extension.
            float maximumReach = (a + b) * 0.94f;
            float minimumReach = Mathf.Min(Mathf.Abs(a - b) + 0.001f, maximumReach);
            float distance = Mathf.Clamp(direction.magnitude, minimumReach, maximumReach);
            direction.Normalize();
            Vector3 bend = Vector3.ProjectOnPlane(outward + Vector3.down * 0.4f, direction).normalized;
            float along = (a * a - b * b + distance * distance) / (2f * distance);
            Vector3 aim = start + direction * along + bend * Mathf.Sqrt(Mathf.Max(0f, a * a - along * along));
            upper.rotation = Quaternion.FromToRotation(elbow.position - start, aim - start) * upper.rotation;
            elbow.rotation = Quaternion.FromToRotation(hand.position - elbow.position, start + direction * distance - elbow.position) * elbow.rotation;
        }
    }
}
