using System;
using System.Threading.Tasks;

namespace UploadFiles
{
    internal class Waiter
    {
        private int _waitEvery;
        private int _waitTime;
        private int _count;

        public Waiter(int waitEvery, int waitTimeInSeconds)
        {
            _waitEvery = waitEvery;
            _waitTime = waitTimeInSeconds;
            _count = 0;
        }

        // Consider: Upgrade to .net 6 and use Task.CompletedTask
        public async Task Wait()
        {
            if (_waitEvery <= 0 || _waitTime <= 0 || ++_count < _waitEvery)
                return;
            _count = 0;
            Console.WriteLine($"Waiting {_waitTime} seconds.");
            await Task.Delay(_waitTime * 1000);
        }
    }
}
