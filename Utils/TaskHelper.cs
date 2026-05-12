using System;
using System.Threading;
using System.Threading.Tasks;

namespace EliteHunt.Utils;

public class TaskHelper : IDisposable
{
	private readonly CancellationTokenSource _cancellationTokenSource = new();

	public void Dispose()
	{
		_cancellationTokenSource.Cancel();
		_cancellationTokenSource.Dispose();
	}
}
