using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ConsoleApp3;

namespace ConsoleApp3.Tests
{
    [TestFixture]
    public class WaveGeneratorTests
    {
        private MethodInfo _generateMethod;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _generateMethod = typeof(WaveGeneratorForm).GetMethod(
                "GenerateWaveSamples",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (_generateMethod == null)
                Assert.Fail("Could not find private method GenerateWaveSamples via reflection.");
        }

        [Test]
        public void GenerateWaveSamples_LengthMatchesExpected()
        {
            using (var form = new WaveGeneratorForm())
            {
                int sampleRate = 8000;
                double duration = 0.5;
                var result = (short[])_generateMethod.Invoke(form, new object[] { WaveType.Sine, 440, 0.8, duration, sampleRate });
                int expected = (int)(sampleRate * duration);
                Assert.AreEqual(expected, result.Length, "Sample count should match sampleRate * duration (cast to int).");
            }
        }

        [Test]
        public void GenerateWaveSamples_SamplesWithinInt16Range()
        {
            using (var form = new WaveGeneratorForm())
            {
                var result = (short[])_generateMethod.Invoke(form, new object[] { WaveType.Sawtooth, 1000, 2.0, 0.01, 8000 });
                foreach (var s in result)
                {
                    Assert.LessOrEqual(Math.Abs(s), short.MaxValue, "Sample must fit in Int16 range.");
                }
            }
        }

        [Test]
        public void GenerateWaveSamples_SquareWave_IsBipolar()
        {
            using (var form = new WaveGeneratorForm())
            {
                var result = (short[])_generateMethod.Invoke(form, new object[] { WaveType.Square, 50, 1.0, 0.02, 8000 });
                Assert.IsTrue(result.Any(s => s > 0), "Square wave should contain positive samples.");
                Assert.IsTrue(result.Any(s => s < 0), "Square wave should contain negative samples.");
            }
        }
    }
}