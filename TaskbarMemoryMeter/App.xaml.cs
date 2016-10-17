using System;
using System.Windows;
using Microsoft.VisualBasic.Devices;
using TaskbarCore;

namespace TaskbarMemoryMeter
{
	public partial class App : Application
	{
		private ComputerInfo	_computerInfo;
		private ulong			_totalPhysicalMemory;

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			_computerInfo = new ComputerInfo();
			_totalPhysicalMemory = _computerInfo.TotalPhysicalMemory;

			var mainWindow = new MainWindow();
			mainWindow.Tick += WhenTimerTick;
			mainWindow.Show();
		}

		private void WhenTimerTick(object sender, EventArgs e)
		{
			var used = (double)(_totalPhysicalMemory - _computerInfo.AvailablePhysicalMemory);
			var usedPercent = (int)(used / _totalPhysicalMemory * 100);

			string suffix;
			double usedDisplay;

			if (used >= 1000000000)
			{
				usedDisplay = used / 1000000000d;
				suffix = "GB";
			}
			else if (used >= 1000000)
			{
				usedDisplay = used / 1000000d;
				suffix = "MB";
			}
			else
			{
				usedDisplay = used;
				suffix = "bytes";
			}

			((MainWindow)sender).SetTaskBarStatus(usedPercent, $"RAM: {usedDisplay:n2} {suffix}");
		}
	}
}