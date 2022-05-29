using System;

namespace DataAcquisition.Downloader
{
    public class TimeEstimator
    {
        private TimeSpan?[] _spans;
        private int _i;
        private int _depth;

        public TimeEstimator(int depth)
        {
            _spans = new TimeSpan?[depth];
            _depth = depth;
            _i = 0;
        }

        public void AddTimeSpan(TimeSpan time)
        {
            _spans[_i] = time;
            _i = ++_i % _depth;
        }

        public TimeSpan Estimate(long left)
        {
            var count = 0;
            long sum = 0;
            foreach (var time in _spans)
            {
                if (time == null)
                    continue;
                sum += time.Value.Ticks;
                count++;
            }
            if (count == 0)
                return TimeSpan.MaxValue;

            return TimeSpan.FromTicks((long)(left * sum / (double)count));
        }
    }
}
