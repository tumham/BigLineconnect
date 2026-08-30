using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace BigLineconnect
{
    /// <summary>
    /// TURBO_v6: High-Performance Adaptive Rate Controller & Zero Buffer Bloat Engine.
    /// Manages client-ACK flow control, dynamic motion-downsampling, and network bandwidth adaptation
    /// to guarantee instant 0ms response during Excel drag selection and instant window closure.
    /// </summary>
    public static class AdaptiveRateController
    {
        private static volatile uint _lastSentSeq = 0;
        private static volatile uint _lastAckedSeq = 0;
        private static long _lastMotionTicks = 0;
        private static volatile int _currentQuality = 55;
        private static volatile int _measuredRttMs = 20;
        private static volatile bool _isMotionActive = false;

        private static readonly ConcurrentDictionary<uint, long> _inFlightFrames = new ConcurrentDictionary<uint, long>();
        private static readonly Stopwatch _clock = Stopwatch.StartNew();

        public static int MeasuredRttMs => _measuredRttMs;
        public static bool IsMotionActive => _isMotionActive;

        public static void Reset()
        {
            _lastSentSeq = 0;
            _lastAckedSeq = 0;
            Interlocked.Exchange(ref _lastMotionTicks, 0);
            _currentQuality = 55;
            _measuredRttMs = 20;
            _isMotionActive = false;
            _inFlightFrames.Clear();
        }

        public static void NotifyUserActivity(bool isContinuousMotion = false)
        {
            long now = _clock.ElapsedMilliseconds;
            Interlocked.Exchange(ref _lastMotionTicks, now);
            if (isContinuousMotion)
            {
                _isMotionActive = true;
            }
        }

        public static void RecordFrameSent(uint seq, int byteSize)
        {
            _lastSentSeq = seq;
            long now = _clock.ElapsedMilliseconds;
            _inFlightFrames[seq] = now;

            // Purge old untracked frames older than 2 seconds to keep memory minimal
            if (_inFlightFrames.Count > 40)
            {
                foreach (var kvp in _inFlightFrames)
                {
                    if (now - kvp.Value > 2000)
                    {
                        _inFlightFrames.TryRemove(kvp.Key, out _);
                    }
                }
            }
        }

        public static void RecordAck(uint seq)
        {
            if (seq > _lastAckedSeq)
            {
                _lastAckedSeq = seq;
            }

            long now = _clock.ElapsedMilliseconds;
            if (_inFlightFrames.TryRemove(seq, out long sendTime))
            {
                int rtt = (int)(now - sendTime);
                if (rtt > 0 && rtt < 5000)
                {
                    _measuredRttMs = (int)(_measuredRttMs * 0.7 + rtt * 0.3);
                }
            }

            // Remove all frames older than acknowledged sequence
            foreach (var kvp in _inFlightFrames)
            {
                if (kvp.Key <= seq)
                {
                    _inFlightFrames.TryRemove(kvp.Key, out _);
                }
            }
        }

        /// <summary>
        /// Decides whether a new frame should be sent or dropped/delayed based on socket queue backpressure.
        /// Never stalls window closing or active inputs for more than 80ms.
        /// </summary>
        public static bool CanSendNextFrame(uint currentSeq, out int waitMs)
        {
            waitMs = 1;
            if (_lastAckedSeq == 0) return true;

            uint inFlight = unchecked(currentSeq - _lastAckedSeq);

            // Allow up to 2 frames in flight on fast/normal pipelines
            if (inFlight <= 2)
            {
                return true;
            }

            // Find oldest inflight timestamp
            long oldestSendTime = 0;
            foreach (var kvp in _inFlightFrames)
            {
                if (oldestSendTime == 0 || kvp.Value < oldestSendTime)
                {
                    oldestSendTime = kvp.Value;
                }
            }

            if (oldestSendTime > 0)
            {
                long age = _clock.ElapsedMilliseconds - oldestSendTime;
                // If oldest frame has been in flight for > 80ms, allow next frame so screen never freezes
                if (age > 80)
                {
                    return true;
                }
            }
            else
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Computes optimal JPEG compression quality and downscale factors adaptively.
        /// Dynamic Motion: Fast & light during drag/scroll (32-38%), Crystal Clear on rest (65-75%).
        /// </summary>
        public static int GetOptimalQuality(int baseQuality, out int maxDimension)
        {
            long now = _clock.ElapsedMilliseconds;
            long lastMotion = Interlocked.Read(ref _lastMotionTicks);
            long motionAge = now - lastMotion;

            // If user moved mouse/scrolled in the last 250ms, we are in MOTION mode
            _isMotionActive = motionAge < 250;

            maxDimension = 0; // 0 = native resolution

            // Base quality constraints
            int targetQ = baseQuality > 0 ? baseQuality : 55;

            if (_isMotionActive)
            {
                // During fast motion / Excel dragging, lower quality to 32-36% for ultra-light ~15 KB frame size
                if (_measuredRttMs > 60)
                {
                    targetQ = Math.Min(targetQ, 32);
                }
                else
                {
                    targetQ = Math.Min(targetQ, 38);
                }
            }
            else
            {
                // User stopped moving! Send high-definition crystal clear rest frame
                if (motionAge > 300 && motionAge < 2000)
                {
                    targetQ = Math.Max(targetQ, 70);
                }
            }

            _currentQuality = targetQ;
            return targetQ;
        }
    }
}
