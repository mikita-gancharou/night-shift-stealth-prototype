using NUnit.Framework;
using Stealth.Perception;

namespace Stealth.Tests
{
    /// <summary>
    /// Unit tests for the progressive detection rule: it has to climb through the levels, hold for a
    /// moment after losing the target and then drain back to nothing.
    /// </summary>
    public class DetectionMeterTests
    {
        private static void Tick(DetectionMeter meter, float exposure, float seconds, float step = 0.05f)
        {
            for (float t = 0f; t < seconds; t += step) meter.Tick(exposure, step);
        }

        [Test]
        public void StartsUnaware()
        {
            DetectionMeter meter = new DetectionMeter();

            Assert.AreEqual(AwarenessLevel.Unaware, meter.Level);
            Assert.AreEqual(0f, meter.Value);
        }

        [Test]
        public void ClimbsThroughEveryLevelBeforeDetecting()
        {
            DetectionMeter meter = new DetectionMeter();
            bool sawSuspicious = false;
            bool sawAlerted = false;

            for (float t = 0f; t < 5f && meter.Level != AwarenessLevel.Detected; t += 0.05f)
            {
                meter.Tick(1f, 0.05f);
                if (meter.Level == AwarenessLevel.Suspicious) sawSuspicious = true;
                if (meter.Level == AwarenessLevel.Alerted) sawAlerted = true;
            }

            Assert.AreEqual(AwarenessLevel.Detected, meter.Level, "Constant exposure should end in full detection.");
            Assert.IsTrue(sawSuspicious, "The meter must pass through the suspicious level.");
            Assert.IsTrue(sawAlerted, "The meter must pass through the alerted level.");
        }

        [Test]
        public void HoldsBrieflyThenDrainsWhenExposureStops()
        {
            DetectionMeter meter = new DetectionMeter();
            Tick(meter, 1f, 0.6f);

            float peak = meter.Value;
            Assert.Greater(peak, 0f);

            // Grace period: the value must not fall immediately.
            Tick(meter, 0f, 0.4f);
            Assert.AreEqual(peak, meter.Value, 0.001f, "The meter should hold its value right after losing the target.");

            Tick(meter, 0f, 6f);
            Assert.AreEqual(AwarenessLevel.Unaware, meter.Level);
            Assert.AreEqual(0f, meter.Value, 0.001f);
        }

        [Test]
        public void LowExposureTakesLongerThanHighExposure()
        {
            DetectionMeter fast = new DetectionMeter();
            DetectionMeter slow = new DetectionMeter();

            Tick(fast, 1.5f, 0.5f);
            Tick(slow, 0.3f, 0.5f);

            Assert.Greater(fast.Value, slow.Value, "A well lit, close target must be noticed faster.");
        }

        [Test]
        public void NoiseBumpRaisesSuspicionWithoutDetecting()
        {
            DetectionMeter meter = new DetectionMeter();
            meter.AddInstant(0.3f);

            Assert.AreEqual(AwarenessLevel.Suspicious, meter.Level);
            Assert.Less(meter.Value, 1f, "A single noise must never fully detect the player.");
        }

        [Test]
        public void HidingDropsTheMeterBackToAlmostNothing()
        {
            DetectionMeter meter = new DetectionMeter();
            Tick(meter, 2f, 2f);
            Assert.AreEqual(AwarenessLevel.Detected, meter.Level);

            meter.Drop(0.05f);

            Assert.AreEqual(AwarenessLevel.Unaware, meter.Level);
            Assert.AreEqual(0.05f, meter.Value, 0.001f);
        }
    }
}
