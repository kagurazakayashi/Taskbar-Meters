﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Navigation;
using System.Windows.Threading;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Taskbar;
using TaskbarCore.Properties;

namespace TaskbarCore
{
	public partial class MainWindow
	{
		public event EventHandler	Tick;

		private readonly Timer		_timer;
		private readonly Settings	_settings = Settings.Default;
		private JumpList			_jumpList;

		#region P/Invoke stuff

		[DllImport("user32.dll")]
		private extern static Int32 SetWindowLong(IntPtr hWnd, Int32 nIndex, Int32 dwNewLong);
		[DllImport("user32.dll")]
		private extern static Int32 GetWindowLong(IntPtr hWnd, Int32 nIndex);

		private const Int32 GWL_STYLE = -16;
		private const Int32 WS_MAXIMIZEBOX = 0x10000;

		#endregion

		public MainWindow()
		{
			if (!TaskbarManager.IsPlatformSupported)
			{
				MessageBox.Show("至少需要 Windows 7 操作系统。", "系统不支持", MessageBoxButton.OK, MessageBoxImage.Error);
				Application.Current.Shutdown();
			}

			_timer = new Timer(_settings.UpdateFrequency);
			_timer.Elapsed += delegate
			{
				Tick?.Invoke(this, EventArgs.Empty);
			};

			InitializeComponent();
			
			slider.Value = (double)_settings.UpdateFrequency / 1000;
			yellowSlider.Value = _settings.Yellow;
			redSlider.Value = _settings.Red;

			// Need to set bindings after we set the initial values (above)
			var binding = new Binding("Value");
			binding.ElementName = "redSlider";
			yellowSlider.SetBinding(RangeBase.MaximumProperty, binding);

			binding = new Binding("Value");
			binding.ElementName = "yellowSlider";
			redSlider.SetBinding(RangeBase.MinimumProperty, binding);

			var thisAssembly = Assembly.GetEntryAssembly();
			var appTitle = ((AssemblyTitleAttribute)thisAssembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false).FirstOrDefault()).Title;
			TaskbarManager.Instance.ApplicationId = appTitle;
			var appCompany = ((AssemblyCompanyAttribute)thisAssembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false).FirstOrDefault()).Company;
			Title = appTitle;
			linkText.Text = appTitle + " 由 " + appCompany + " 制作";

			_timer.Start();

			if (_settings.FirstTime)
			{
				WindowState = WindowState.Normal;
				_settings.FirstTime = false;
			}

			SetFrequencyText();
			SetYellowText();
			SetRedText();
		}

		protected override void OnContentRendered(EventArgs e)
		{
			base.OnContentRendered(e);

			// Disable the minimize button
			var hWnd = new WindowInteropHelper(this).Handle;
			var windowLong = GetWindowLong(hWnd, GWL_STYLE);
			windowLong = windowLong & ~WS_MAXIMIZEBOX;
			SetWindowLong(hWnd, GWL_STYLE, windowLong);

			_jumpList = JumpList.CreateJumpList();
			_jumpList.Refresh();

			var systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);

			_jumpList.AddUserTasks(new JumpListLink(Path.Combine(systemFolder, "taskmgr.exe"), "启动任务管理器")
			{
				IconReference = new IconReference(Path.Combine(systemFolder, "taskmgr.exe"), 0)
			});

			_jumpList.AddUserTasks(new JumpListLink(Path.Combine(systemFolder, "perfmon.exe"), "启动资源监视器")
			{
				IconReference = new IconReference(Path.Combine(systemFolder, "perfmon.exe"), 0),
				Arguments = "/res"
			});

			_jumpList.Refresh();
		}

		public JumpList JumpList
		{
			get { return _jumpList; }
		}

		public void SetTaskBarStatus(int value, string text)
		{
			if (value < 0)
			{
				value = 0;
			}
			else if (value > 100)
			{
				value = 100;
			}

			var state = TaskbarProgressBarState.Normal;

			if (value > _settings.Yellow)
			{
				state = value < _settings.Red ? TaskbarProgressBarState.Paused : TaskbarProgressBarState.Error;
			}

			TaskbarManager.Instance.SetProgressState(state);
			TaskbarManager.Instance.SetProgressValue(value, 100);

			// Update the title of the window w/the percentage
			Dispatcher.Invoke(DispatcherPriority.Normal, (Action) (() => { Title = text; }));
		}

		private void SetFrequencyText()
		{
			updateFrequencyTextBlock.Text = $"更新间隔: {slider.Value} 秒{(slider.Value > 1 ? "s" : "")}";
		}

		private void SetYellowText()
		{
			yellowTextBlock.Text = $"黄条: {yellowSlider.Value}%";
		}

		private void SetRedText()
		{
			redTextBlock.Text = $"红条: {redSlider.Value}%";
		}

		protected override void OnClosed(EventArgs e)
		{
			_settings.Save();
			
			base.OnClosed(e);
		}

		private void WhenUpdateFrequencySliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (IsInitialized)
			{
				var updateFrequency = (int)(slider.Value * 1000);
				_settings.UpdateFrequency = updateFrequency;
				_timer.Interval = updateFrequency;

				SetFrequencyText();
			}
		}

		private void WhenYellowValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_settings.Yellow = (int)yellowSlider.Value;
			SetYellowText();
		}

		private void WhenRedValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_settings.Red = (int)redSlider.Value;
			SetRedText();
		}

		private void WhenNavigateRequested(object sender, RequestNavigateEventArgs e)
		{
			try
			{
				Process.Start(((Hyperlink)sender).NavigateUri.ToString());
			}
			catch (Exception exception)
			{
				MessageBox.Show("抱歉，未能成功打开浏览器并访问网站。\n" + exception.Message, "网站打开失败", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}