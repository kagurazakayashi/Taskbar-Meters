using System;
using System.Diagnostics;
using System.Windows;
using TaskbarCore;

namespace TaskbarDiskIOMeter
{
	public partial class App : Application
	{
        private readonly PerformanceCounter _counter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			var mainWindow = new MainWindow();
			mainWindow.Tick += WhenTimerTick;
			mainWindow.Show();
		}

		private void WhenTimerTick(object sender, EventArgs e)
		{
			var value = (int)_counter.NextValue();
			((MainWindow)sender).SetTaskBarStatus(value, $"Disk: {value}%");
		}
	}
}
