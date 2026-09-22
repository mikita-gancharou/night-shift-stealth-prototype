using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Stealth.Perception;

namespace Stealth.Tests
{
    /// <summary>
    /// Play mode tests for the two detection zones required by the assignment: the raycast view cone
    /// (distance, angle and obstacles) and the close range sphere.
    /// </summary>
    public class PerceptionTests
    {
        private const int EnvironmentLayer = 6;
        private const int PlayerLayer = 7;

        private readonly System.Collections.Generic.List<GameObject> _spawned = new System.Collections.Generic.List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null) Object.DestroyImmediate(go);
            }

            _spawned.Clear();
        }

        /// <summary>Minimal stand-in for the player so the sensors can be tested without the real character.</summary>
        private class TestTarget : MonoBehaviour, IStealthTarget
        {
            public float Height = 1.8f;
            public bool Hidden;
            public float Visibility = 1f;
            public float Noise;

            public Transform Transform => transform;
            public Vector3 Center => transform.position + Vector3.up * (Height * 0.55f);
            public bool IsHidden => Hidden;
            public float VisibilityMultiplier => Visibility;
            public float NoiseLevel => Noise;

            public int GetSamplePoints(Vector3[] buffer)
            {
                buffer[0] = transform.position + Vector3.up * (Height - 0.18f);
                buffer[1] = transform.position + Vector3.up * (Height * 0.55f);
                buffer[2] = transform.position + Vector3.up * 0.3f;
                return 3;
            }
        }

        private VisionSensor CreateSensor(Vector3 position, Quaternion rotation, float distance = 14f, float angle = 90f)
        {
            GameObject root = new GameObject("Observer");
            root.transform.SetPositionAndRotation(position, rotation);
            _spawned.Add(root);

            GameObject eye = new GameObject("Eye");
            eye.transform.SetParent(root.transform, false);
            eye.transform.localPosition = new Vector3(0f, 1.62f, 0f);

            VisionSensor sensor = root.AddComponent<VisionSensor>();
            sensor.Configure(eye.transform, 1 << EnvironmentLayer, 1 << PlayerLayer);
            sensor.ApplyShape(distance, angle);
            return sensor;
        }

        private TestTarget CreateTarget(Vector3 position)
        {
            GameObject go = new GameObject("Target") { layer = PlayerLayer };
            go.transform.position = position;
            _spawned.Add(go);

            CapsuleCollider capsule = go.AddComponent<CapsuleCollider>();
            capsule.height = 1.8f;
            capsule.radius = 0.32f;
            capsule.center = new Vector3(0f, 0.9f, 0f);

            return go.AddComponent<TestTarget>();
        }

        private GameObject CreateWall(Vector3 center, Vector3 size)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.layer = EnvironmentLayer;
            wall.transform.position = center;
            wall.transform.localScale = size;
            _spawned.Add(wall);
            return wall;
        }

        [UnityTest]
        public IEnumerator SeesTargetInFrontOfIt()
        {
            VisionSensor sensor = CreateSensor(Vector3.zero, Quaternion.identity);
            TestTarget target = CreateTarget(new Vector3(0f, 0f, 6f));
            yield return new WaitForFixedUpdate();

            Assert.Greater(sensor.SampleExposure(target), 0f);
        }

        [UnityTest]
        public IEnumerator DoesNotSeeTargetBehindIt()
        {
            VisionSensor sensor = CreateSensor(Vector3.zero, Quaternion.identity);
            TestTarget target = CreateTarget(new Vector3(0f, 0f, -6f));
            yield return new WaitForFixedUpdate();

            Assert.AreEqual(0f, sensor.SampleExposure(target));
        }

        [UnityTest]
        public IEnumerator DoesNotSeeTargetBeyondViewDistance()
        {
            VisionSensor sensor = CreateSensor(Vector3.zero, Quaternion.identity, 8f);
            TestTarget target = CreateTarget(new Vector3(0f, 0f, 12f));
            yield return new WaitForFixedUpdate();

            Assert.AreEqual(0f, sensor.SampleExposure(target));
        }

        [UnityTest]
        public IEnumerator WallBlocksTheLineOfSight()
        {
            VisionSensor sensor = CreateSensor(Vector3.zero, Quaternion.identity);
            TestTarget target = CreateTarget(new Vector3(0f, 0f, 8f));
            CreateWall(new Vector3(0f, 2f, 4f), new Vector3(6f, 4f, 0.5f));
            yield return new WaitForFixedUpdate();

            Assert.AreEqual(0f, sensor.SampleExposure(target), "A solid wall must hide the target completely.");
        }

        [UnityTest]
        public IEnumerator LowCoverHidesACrouchingTargetButNotAStandingOne()
        {
            VisionSensor sensor = CreateSensor(Vector3.zero, Quaternion.identity);
            TestTarget target = CreateTarget(new Vector3(0f, 0f, 6f));
            CreateWall(new Vector3(0f, 0.62f, 4f), new Vector3(6f, 1.25f, 0.6f));
            yield return new WaitForFixedUpdate();

            Assert.Greater(sensor.SampleExposure(target), 0f, "Standing up, the head is visible over the crate.");

            target.Height = 1.0f; // crouched
            Assert.AreEqual(0f, sensor.SampleExposure(target), "Crouching behind low cover must break the line of sight.");
        }

        [UnityTest]
        public IEnumerator HiddenTargetIsInvisibleEvenInTheOpen()
        {
            VisionSensor sensor = CreateSensor(Vector3.zero, Quaternion.identity);
            TestTarget target = CreateTarget(new Vector3(0f, 0f, 4f));
            yield return new WaitForFixedUpdate();

            Assert.Greater(sensor.SampleExposure(target), 0f);

            target.Hidden = true;
            Assert.AreEqual(0f, sensor.SampleExposure(target), "A player inside a hiding spot cannot be perceived.");
        }

        [UnityTest]
        public IEnumerator CloseTargetIsMoreExposedThanADistantOne()
        {
            VisionSensor sensor = CreateSensor(Vector3.zero, Quaternion.identity, 20f);
            TestTarget near = CreateTarget(new Vector3(0f, 0f, 3f));
            yield return new WaitForFixedUpdate();
            float nearExposure = sensor.SampleExposure(near);

            Object.DestroyImmediate(near.gameObject);
            TestTarget far = CreateTarget(new Vector3(0f, 0f, 18f));
            yield return new WaitForFixedUpdate();
            float farExposure = sensor.SampleExposure(far);

            Assert.Greater(nearExposure, farExposure);
        }

        [UnityTest]
        public IEnumerator CrouchingReducesExposure()
        {
            VisionSensor sensor = CreateSensor(Vector3.zero, Quaternion.identity);
            TestTarget target = CreateTarget(new Vector3(0f, 0f, 6f));
            yield return new WaitForFixedUpdate();

            float standing = sensor.SampleExposure(target);
            target.Visibility = 0.55f; // what PlayerVisibility reports while crouched
            float crouched = sensor.SampleExposure(target);

            Assert.Less(crouched, standing);
        }

        [UnityTest]
        public IEnumerator ProximityZoneNoticesNoiseButIgnoresSilence()
        {
            GameObject root = new GameObject("Observer");
            _spawned.Add(root);

            GameObject zoneGo = new GameObject("ProximityZone");
            zoneGo.transform.SetParent(root.transform, false);
            SphereCollider sphere = zoneGo.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            ProximitySensor proximity = zoneGo.AddComponent<ProximitySensor>();
            proximity.Configure(1 << PlayerLayer);
            proximity.ApplyRadius(5f);

            TestTarget target = CreateTarget(new Vector3(0f, 0f, 2.5f));
            Rigidbody body = target.gameObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;

            // Give the physics system a couple of steps to report the overlap.
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(proximity.TargetInside, "The trigger sphere should have registered the target.");
            Assert.AreEqual(0f, proximity.SampleExposure(target), "A silent, crouch walking player stays unnoticed.");

            target.Noise = 1f;
            Assert.Greater(proximity.SampleExposure(target), 0f, "A loud player close by must be noticed.");
        }

        [UnityTest]
        public IEnumerator NoiseReachesListenersInRangeOnly()
        {
            Listener near = new Listener(new Vector3(0f, 0f, 3f), 20f);
            Listener far = new Listener(new Vector3(0f, 0f, 40f), 20f);

            NoiseSystem.Register(near);
            NoiseSystem.Register(far);

            NoiseSystem.Emit(Vector3.zero, 12f, 1f, NoiseKind.Impact);
            yield return null;

            NoiseSystem.Unregister(near);
            NoiseSystem.Unregister(far);

            Assert.AreEqual(1, near.Heard, "A listener inside the radius must hear the stone.");
            Assert.AreEqual(0, far.Heard, "A listener far away must not.");
        }

        private class Listener : INoiseListener
        {
            public int Heard;

            public Listener(Vector3 position, float radius)
            {
                EarPosition = position;
                HearingRadius = radius;
            }

            public Vector3 EarPosition { get; }
            public float HearingRadius { get; }

            public void OnNoiseHeard(NoiseEvent noise) => Heard++;
        }
    }
}
