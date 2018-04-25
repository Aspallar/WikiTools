using log4net;
using System.Threading;
using System.Threading.Tasks;

namespace UploadFiles
{
    internal class Waiter
    {
        private int _waitEvery;
        private int _waitTime;
        private int _count;
        private int _delay;
        private CancellationToken _cancellationToken;
        private ILog _log;

        public Waiter(int waitEvery, int waitTimeInSeconds, int delay, CancellationToken token, ILog log)
        {
            _log = log;
            _waitEvery = waitEvery;
            _waitTime = waitTimeInSeconds;
            _count = 0;
            _cancellationToken = token;
            _delay = delay;
        }

        public async Task Wait()
        {
            try
            {
                if (_waitEvery <= 0 || _waitTime <= 0 || ++_count < _waitEvery)
                {
                    await Task.Delay(_delay, _cancellationToken);
                    return;
                }
                _count = 0;
                _log.Info($"Waiting {_waitTime} seconds.");
                await Task.Delay(_waitTime * 1000, _cancellationToken);
            }
            catch (TaskCanceledException) { }
        }
    }
}
