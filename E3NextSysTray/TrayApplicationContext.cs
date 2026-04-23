using Microsoft.Toolkit.Uwp.Notifications;
using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Networking.NetworkOperators;
using Windows.UI.Notifications;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Microsoft.Toolkit.Uwp.Notifications;

namespace E3NextSysTray
{
	internal class TrayApplicationContext:ApplicationContext
	{
		private readonly SynchronizationContext _syncContext;

		private NotifyIcon trayIcon;
		private ContextMenuStrip contextMenu;
		private Task _downloadTask = null;
		ToolStripMenuItem openItem;
		ToolStripMenuItem exitItem;
		ToolStripMenuItem updateItem;
		ToolStripMenuItem progressItem;
		public Icon BytesToIcon(byte[] bytes)
		{
			// Pass the byte array directly into a memory stream
			using (MemoryStream ms = new MemoryStream(bytes))
			{
				// Generate and return the Icon object
				return new Icon(ms);
			}
		}
		public TrayApplicationContext()
		{
			// 1. Initialize the Context Menu
			contextMenu = new ContextMenuStrip();

			 openItem = new ToolStripMenuItem("Open App", null, OnOpen);
			 exitItem = new ToolStripMenuItem("Exit", null, OnExit);
			 updateItem = new ToolStripMenuItem("Update", null, OnUpdate);
			progressItem = new ToolStripMenuItem("Show Progress", null, OnShowProgress);
			progressItem.Enabled = false;
			contextMenu.Items.Add(openItem);
			contextMenu.Items.Add(new ToolStripSeparator());
			contextMenu.Items.Add(exitItem);
			contextMenu.Items.Add(new ToolStripSeparator());
			contextMenu.Items.Add(updateItem);
			contextMenu.Items.Add(new ToolStripSeparator());
			contextMenu.Items.Add(progressItem);
			// 2. Initialize the NotifyIcon
			trayIcon = new NotifyIcon()
			{
				// To use your own icon: new Icon("yourfile.ico")
				Icon = BytesToIcon(Properties.Resources.e3n_logo),
				ContextMenuStrip = contextMenu,
				Text = "E3Next",
				Visible = true
			};

			// Double click event to open the app
			trayIcon.DoubleClick += OnOpen;
			// 2. CRITICAL: Capture the UI thread's synchronization boundary
			_syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
			
		}

		private void OnOpen(object sender, EventArgs e)
		{
			// Example: Show a standard form when requested
			Form1 mainForm = new Form1();
			mainForm.Show();

			// Bring it to the foreground if minimized
			mainForm.Activate();
		}

		private void OnExit(object sender, EventArgs e)
		{
			// Hide tray icon, otherwise it lingers until mouseover
			trayIcon.Visible = false;
			trayIcon.Dispose();
			ToastNotificationManagerCompat.History.Remove("zip-download", "github-downloads");
			// Safely terminate the application message loop
			System.Windows.Forms.Application.Exit();
		}
		private void OnShowProgress(object sender, EventArgs e)
		{
			string tempPngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "E3Next.png");
			Uri fileUri = new Uri("file:///" + tempPngPath.Replace("\\", "/"), UriKind.Absolute);

			new ToastContentBuilder()
			  .AddAppLogoOverride(fileUri, ToastGenericAppLogoCrop.Circle)
			.SetToastScenario(ToastScenario.Reminder)
			.AddText("Downloading update...")
			.AddVisualChild(new AdaptiveProgressBar()
			{
				Title = "E3N Updater",
				Value = new BindableProgressBarValue("percentValue"),
				Status = new BindableString("statusText")
			})
			 .AddButton(new ToastButton("Hide", "dismissed"))
			.Show(toast =>
			{
				toast.Tag = "zip-download";
				toast.Group = "github-downloads";
				toast.Data = new NotificationData();
				toast.Data.Values["percentValue"] = "indeterminate"; // Bouncing indeterminate bar
				toast.Data.Values["statusText"] = "Connecting...";
				toast.Data.Values["percentString"] = " ";
			});

		}
		private void OnUpdate(object sender, EventArgs e)
		{
			updateItem.Enabled = false;
			progressItem.Enabled = true;
			string tempPngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "E3Next.png");
			Uri fileUri = new Uri("file:///" + tempPngPath.Replace("\\", "/"), UriKind.Absolute);

			new ToastContentBuilder()
			  .AddAppLogoOverride(fileUri, ToastGenericAppLogoCrop.Circle)
			.SetToastScenario(ToastScenario.Reminder)
			.AddText("Downloading update...")
			.AddVisualChild(new AdaptiveProgressBar()
			{
				Title = "E3N Updater",
				Value = new BindableProgressBarValue("percentValue"),
				Status = new BindableString("statusText")
			})
			 .AddButton(new ToastButton("Hide", "dismissed"))
			.Show(toast =>
			{
				toast.Tag = "zip-download";
				toast.Group = "github-downloads";
				toast.Data = new NotificationData();
				toast.Data.Values["percentValue"] = "indeterminate"; // Bouncing indeterminate bar
				toast.Data.Values["statusText"] = "Connecting...";
				toast.Data.Values["percentString"] = " ";
			});
			// 2. Start the background work
			_downloadTask= Task.Run(() => DownloadUpdate());
		}			
		private void DownloadUpdate()
		{
			try
			{
				
				GitHubClient client = new GitHubClient(new ProductHeaderValue("E3NextUpdater"));
				var latestRelease = client.Repository.Release.GetLatest("RekkasGit", "E3NextAndMQNextBinary").Result;

				var stopwatch = new Stopwatch();

				stopwatch.Start();

				Int64 startTime = stopwatch.ElapsedMilliseconds;
				string downloadUrl = latestRelease.ZipballUrl;
				// 3. Use HttpClient to stream the zip chunks
				using (var httpClient = new HttpClient())
				{
					httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MyProgressDownloader");

					// Needed only if accessing a PRIVATE repository:
					// httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "YOUR_PERSONAL_ACCESS_TOKEN");

					// Request the headers without buffering the content immediately
					using (var response = httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead).Result)
					{
						response.EnsureSuccessStatusCode();

						// Find total size (GitHub doesn't always send this on redirected source streams)
						long? totalBytes = response.Content.Headers.ContentLength;

						using (var stream = response.Content.ReadAsStreamAsync().Result)
						using (var fileStream = new FileStream("source_code.zip", System.IO.FileMode.Create, FileAccess.Write, FileShare.None))
						{
							byte[] buffer = new byte[1024*1024];
							long totalReadBytes = 0;
							int readBytes;

							while ((readBytes = stream.ReadAsync(buffer, 0, buffer.Length).Result) > 0)
							{
								fileStream.WriteAsync(buffer, 0, readBytes).Wait();
								totalReadBytes += readBytes;

								// Render the progress bar

								if(stopwatch.ElapsedMilliseconds-startTime>750)
								{
									startTime = stopwatch.ElapsedMilliseconds;
									DrawProgressBar(totalReadBytes, totalBytes);
								}
							}
						}
					}
				}
			}
			catch(Exception ex)
			{
				_syncContext.Post(_ =>
				{
					ToastNotificationManagerCompat.History.Remove("zip-download", "github-downloads");
					MessageBox.Show($"Download failed! {ex.Message}");
					trayIcon.Text = $"E3Next";
					updateItem.Enabled = true;
				}, null);
			
				return;
			}
			_syncContext.Post(_ =>
			{
					var data = new NotificationData();
					data.Values["percentValue"] = "indeterminate";
					data.Values["statusText"] = $"Complete!!";
					data.Values["percentString"] = " ";
					ToastNotificationManagerCompat.CreateToastNotifier()
						.Update(data, "zip-download", "github-downloads");
				System.Threading.Thread.Sleep(2000);
				ToastNotificationManagerCompat.History.Remove("zip-download", "github-downloads");
				updateItem.Enabled = true;
				trayIcon.Text = $"E3Next";
				progressItem.Enabled = false;
			}, null);
	
		}
		private void UpdateTrayProgress(double megabytes)
		{
			_syncContext.Post(_ =>
			{
				var data = new NotificationData();
				data.Values["percentValue"] = "indeterminate";
				data.Values["statusText"] = $"Total Downloaded:{megabytes:F2} MB";
				trayIcon.Text = $"Total Downloaded:{megabytes:F2} MB";
				data.Values["percentString"] = " ";
				ToastNotificationManagerCompat.CreateToastNotifier()
					.Update(data, "zip-download", "github-downloads");
			}, null);

		}
		private  void DrawProgressBar(long totalReadBytes, long? totalBytes)
		{
			//if (totalBytes.HasValue)
			//{
			//	// If Content-Length is provided, show progress percentage
			//	double percentage = (double)totalReadBytes / totalBytes.Value * 100;
			//	int progressBlocks = (int)(percentage / 2);
			//	double mbRead = totalReadBytes / 1024.0 / 1024.0;
			//	UpdateTrayProgress(percentage,mbRead);
			//	Debug.WriteLine($"\r[{new string('#', progressBlocks)}{new string('-', 50 - progressBlocks)}] {percentage:F1}%");
			//}
			//else
			//{
				// If Content-Length is missing (common with dynamic zip streams), show accumulated megabytes
				double mbRead = totalReadBytes / 1024.0 / 1024.0;
				UpdateTrayProgress(mbRead);
				Debug.WriteLine($"\rDownloading: {mbRead:F2} MB retrieved...");
			//}
		}


	}
}
