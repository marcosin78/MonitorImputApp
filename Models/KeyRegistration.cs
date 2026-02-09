using System;
using System.Threading;
using System.Threading.Tasks;

namespace MonitorInputApp.Models
{
	public class KeyRegistration
	{
		private DateTime _lastKeyPressTime;
		private bool _isRunning;
		private CancellationTokenSource? _cts;

		public KeyRegistration()
		{
			_lastKeyPressTime = DateTime.MinValue;
			_isRunning = false;
		}

		public void Start()
		{
			if (_isRunning) return;
			_isRunning = true;
			_cts = new CancellationTokenSource();
			Task.Run(() => MonitorInput(_cts.Token));
		}

		public void Stop()
		{
			_isRunning = false;
			_cts?.Cancel();
		}

		private async Task MonitorInput(CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				if (Console.KeyAvailable)
				{
					var key = Console.ReadKey(true);
					_lastKeyPressTime = DateTime.Now;
					Console.WriteLine($"Tecla presionada: {key.KeyChar} a las {_lastKeyPressTime:HH:mm:ss}");
				}
				if (_lastKeyPressTime != DateTime.MinValue)
				{
					Console.WriteLine($"Última entrada detectada a las {_lastKeyPressTime:HH:mm:ss}");
					await Task.Delay(5000, token);
				}
				else
				{
					await Task.Delay(100, token);
				}
			}
		}
	}
}
