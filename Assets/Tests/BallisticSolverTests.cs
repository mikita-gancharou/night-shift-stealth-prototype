using NUnit.Framework;
using UnityEngine;
using Stealth.Player;

namespace Stealth.Tests
{
    /// <summary>The thrown stone has to land where the player aimed, otherwise distractions feel random.</summary>
    public class BallisticSolverTests
    {
        [Test]
        public void SolvedThrowLandsOnTheAimedSpot()
        {
            Vector3 start = new Vector3(0f, 1.5f, 0f);
            Vector3 target = new Vector3(0f, 0f, 9f);

            Assert.IsTrue(BallisticSolver.TrySolve(start, target, 13f, out Vector3 velocity));

            // Integrate the same way the preview does and check where the path crosses the ground.
            Vector3 position = start;
            Vector3 currentVelocity = velocity;
            const float step = 0.01f;

            for (int i = 0; i < 2000 && position.y > 0f; i++)
            {
                currentVelocity += Physics.gravity * step;
                position += currentVelocity * step;
            }

            Assert.Less(Vector3.Distance(new Vector3(position.x, 0f, position.z), target), 0.6f,
                "The stone should land within half a metre of the aim point.");
        }

        [Test]
        public void OutOfRangeThrowStillProducesAForwardVelocity()
        {
            Vector3 start = Vector3.zero;
            Vector3 target = new Vector3(0f, 0f, 400f);

            Assert.IsFalse(BallisticSolver.TrySolve(start, target, 8f, out Vector3 velocity),
                "A target 400 m away cannot be reached with a hand throw.");
            Assert.Greater(velocity.z, 0f, "The fallback throw must still go towards the target.");
            Assert.Greater(velocity.y, 0f, "The fallback throw must be lobbed upwards.");
        }

        [Test]
        public void SimulatedPathStartsAtTheOrigin()
        {
            Vector3[] buffer = new Vector3[32];
            Vector3 start = new Vector3(1f, 1.4f, -2f);

            int count = BallisticSolver.SimulatePath(start, new Vector3(0f, 4f, 10f), 0, buffer);

            Assert.Greater(count, 1);
            Assert.AreEqual(start, buffer[0]);
        }
    }
}
