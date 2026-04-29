using E3NextSysTray.Forms;
using Ionic.Zip;
using Microsoft.Toolkit.Uwp.Notifications;
using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Services.Maps;
using Windows.UI.Notifications;

using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace E3NextSysTray
{
	internal class TrayApplicationContext:ApplicationContext
	{
		private readonly SynchronizationContext _syncContext;

		private string _versionNumber = "v1.53-3.1.1.4";
		private Boolean is32Bit = true;
		private NotifyIcon trayIcon;
		private ContextMenuStrip contextMenu;
		private Task _downloadTask = null;
		private Task _processingTask = null;
		ToolStripMenuItem checkForUpdateItem;
		ToolStripMenuItem exitItem;
		ToolStripMenuItem updateItem;
		ToolStripMenuItem progressItem;
		ToolStripMenuItem debugItem;
		private string _toatsTag = "zip-download";
		private string _toatsGroupTag = "e3n-updates";
		private string _mqLocation = String.Empty;
		private string _downloadFullFileName = "full_e3n_mq_download.zip";
		private string _currentExePath = Process.GetCurrentProcess().MainModule.FileName;
		private string _currentDirectory = String.Empty;
		private string _mqDebugLocation = @"D:\EQ\e3ntrayupdater";

		public Icon BytesToIcon(byte[] bytes)
		{
			// Pass the byte array directly into a memory stream
			using (MemoryStream ms = new MemoryStream(bytes))
			{
				// Generate and return the Icon object
				return new Icon(ms);
			}
		}
		public void DeleteSelfAndStartupNew()
		{
			string currentExePath = Process.GetCurrentProcess().MainModule.FileName;
			// 2. Build a chain of commands for cmd.exe:
			// - 'waitfor /t 3' forces cmd to silently wait for 3 seconds
			// - 'del /f /q' force-deletes the file silently without prompting
			string command = $"/c waitfor /t 2 e3ntrayDeleteSignal 2>nul & del /f /q \"{currentExePath}\"";
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = "cmd.exe",
				Arguments = command,
				CreateNoWindow = true,       // Keeps the command prompt window invisible
				UseShellExecute = false      // Required to keep it hidden and independent
			};
			// 3. Launch the delete task
			Process.Start(startInfo);
			string currentDirectory = Path.GetDirectoryName(currentExePath);
			// 2. Define the exact path for the duplicated target
			string newExeName = "E3NextSysTray.exe";
			string targetApp = Path.Combine(currentDirectory, newExeName);
			command = $"/c waitfor /t 3 e3ntrayStartupSignal 2>nul & start \"\" \"{targetApp}\"";
			ProcessStartInfo startInfo2 = new ProcessStartInfo
			{
				FileName = "cmd.exe",
				Arguments = command,
				CreateNoWindow = true,       // Keeps the command prompt window invisible
				UseShellExecute = false      // Required to keep it hidden and independent
			};
			// 3. Launch the delete task
			Process.Start(startInfo2);
			// 4. Force the C# app to close IMMEDIATELY
			// This releases the operating system lock on the file so cmd can delete it
			Environment.Exit(0);
		}
		public void RenameAndStartProcess()
		{
		
			// 2. Define the exact path for the duplicated target
			string newExeName = "E3NextSysTray_Working.exe";
			string newExePath = Path.Combine(_currentDirectory, newExeName);

			// 3. Duplicate the current executable over to the new file path
			// (Setting 'overwrite' to true cleans up any remnants of prior runs)
			Console.WriteLine("Copying active executable...");
			File.Copy(_currentExePath, newExePath, overwrite: true);


			int delayInSeconds = 3;
			string targetApp = newExeName;

			// Use 'waitfor' mapped to a dummy string instead of 'timeout'.
			// This is safe to run in a non-interactive, hidden shell!
			string command = $"/c waitfor /t {delayInSeconds} e3ntrayStartupSignal 2>nul & start \"\" \"{targetApp}\"";

			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = "cmd.exe",
				Arguments = command,
				CreateNoWindow = true,       // Runs completely hidden
				UseShellExecute = false,     // MUST be false to use CreateNoWindow in .NET Core/.NET 5+
				RedirectStandardOutput = false,
				RedirectStandardError = false
			};

			Process.Start(startInfo);

			Console.WriteLine("Command sent. Terminating this process now...");
			Environment.Exit(0);
		}
		public TrayApplicationContext()
		{
			_mqLocation = Process.GetCurrentProcess().MainModule.FileName;
			_mqLocation = Path.GetDirectoryName(_mqLocation).Replace(@"\mono\macros\e3", "").Replace(@"/mono/macros/e3", "");
			_currentDirectory = Path.GetDirectoryName(_currentExePath);


			//if (Debugger.IsAttached)
			//{
			//	_mqLocation = _mqDebugLocation;
			//}
			if (!System.Diagnostics.Debugger.IsAttached)
			{

				string currentExe = Process.GetCurrentProcess().MainModule.ModuleName;
				if(currentExe!= "E3NextSysTray_Working.exe")
				{
					RenameAndStartProcess();
					return;
				}

			}
			
			// 1. Initialize the Context Menu
			contextMenu = new ContextMenuStrip();

			checkForUpdateItem = new ToolStripMenuItem("Check for Update", null, OnCheckForUpdate);
			exitItem = new ToolStripMenuItem("Exit", null, OnExit);
			updateItem = new ToolStripMenuItem("Update", null, OnUpdate);
			updateItem.Enabled = false;
			progressItem = new ToolStripMenuItem("Show Progress", null, OnShowProgress);
			debugItem = new ToolStripMenuItem("Show Debug", null, OnDebug);
			progressItem.Enabled = false;
			contextMenu.Items.Add(checkForUpdateItem);
			contextMenu.Items.Add(new ToolStripSeparator());
			contextMenu.Items.Add(exitItem);
			contextMenu.Items.Add(new ToolStripSeparator());
			contextMenu.Items.Add(updateItem);
			contextMenu.Items.Add(new ToolStripSeparator());
			contextMenu.Items.Add(progressItem);
			contextMenu.Items.Add(new ToolStripSeparator());
			contextMenu.Items.Add(debugItem);
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
			trayIcon.DoubleClick += OnCheckForUpdate;
			// 2. CRITICAL: Capture the UI thread's synchronization boundary
			_syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
			CheckForUpdates();
		}

		private void OnDebug(object sender, EventArgs e)
		{
			if(!Program.IsConsoleConnected())
			{
				Program.AllocConsole();
				var standardOutput = new StreamWriter(Console.OpenStandardOutput());
				standardOutput.AutoFlush = true;
				Console.SetOut(standardOutput);
				debugItem.Text = "Hide Debug";
			}
			else
			{
				debugItem.Text = "Show Debug";
				Program.FreeConsole();
			}
		}
		private void OnCheckForUpdate(object sender, EventArgs e)
		{
			CheckForUpdates();
		}

		private void OnExit(object sender, EventArgs e)
		{
			// Hide tray icon, otherwise it lingers until mouseover
			trayIcon.Visible = false;
			trayIcon.Dispose();
			ToastNotificationManagerCompat.History.Remove(_toatsTag, _toatsGroupTag);
			// Safely terminate the application message loop
			System.Windows.Forms.Application.Exit();
		}
		private void OnShowProgress(object sender, EventArgs e)
		{
			string tempPngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "E3Next.png");
			Uri fileUri = new Uri("file:///" + tempPngPath.Replace("\\", "/"), UriKind.Absolute);

			if (File.Exists(tempPngPath))
			{
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
					toast.Tag = _toatsTag;
					toast.Group = _toatsGroupTag;
					toast.Data = new NotificationData();
					toast.Data.Values["percentValue"] = "indeterminate"; // Bouncing indeterminate bar
					toast.Data.Values["statusText"] = "Connecting...";
					toast.Data.Values["percentString"] = " ";
				});
			}
			else
			{
				new ToastContentBuilder()
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
					toast.Tag = _toatsTag;
					toast.Group = _toatsGroupTag;
					toast.Data = new NotificationData();
					toast.Data.Values["percentValue"] = "indeterminate"; // Bouncing indeterminate bar
					toast.Data.Values["statusText"] = "Connecting...";
					toast.Data.Values["percentString"] = " ";
				});
			}

			

		}

		private void UpdateToastStatus(string status)
		{
			Console.WriteLine(status);
			_syncContext.Post(_ =>
			{
				var data = new NotificationData();
				data.Values["percentValue"] = "indeterminate";
				data.Values["statusText"] = status;
				trayIcon.Text = status.Substring(0, Math.Min(status.Length, 63)); //64 character limit
				data.Values["percentString"] = " ";
				ToastNotificationManagerCompat.CreateToastNotifier()
					.Update(data, _toatsTag, _toatsGroupTag);
			}, null);
		}

		private void OnUpdate(object sender, EventArgs e)
		{

			updateItem.Enabled = false;
			progressItem.Enabled = true;
			//lets check to see if macroquest.exe exists and if its 32bit or 64bit.
			var numberOfBits = Program.CheckBitness("MacroQuest.exe");

			if(numberOfBits==-1)
			{
				string choice = AskBitVersions.Show(
						"Action Required",
						"Which version would you like?",
						"32bit (Rof2)",
						"64bit (TOB+)"
					);

				if (choice == "32bit (Rof2)")
				{
					// Do upload logic
					numberOfBits = 32;
				}
				else
				{
					numberOfBits = 64;

					MessageBox.Show("Sorry 64bit is not yet supported on EMU");
					updateItem.Enabled = true;
					progressItem.Enabled = false;
					return;

				}

			}

			string tempPngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "E3Next.png");
			Uri fileUri = new Uri("file:///" + tempPngPath.Replace("\\", "/"), UriKind.Absolute);

			if (File.Exists(tempPngPath))
			{
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
					toast.Tag = _toatsTag;
					toast.Group = _toatsGroupTag;
					toast.Data = new NotificationData();
					toast.Data.Values["percentValue"] = "indeterminate"; // Bouncing indeterminate bar
					toast.Data.Values["statusText"] = "Connecting...";
					toast.Data.Values["percentString"] = " ";
				});
			}
			else
			{
				new ToastContentBuilder()
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
					toast.Tag = _toatsTag;
					toast.Group = _toatsGroupTag;
					toast.Data = new NotificationData();
					toast.Data.Values["percentValue"] = "indeterminate"; // Bouncing indeterminate bar
					toast.Data.Values["statusText"] = "Connecting...";
					toast.Data.Values["percentString"] = " ";
				});
			}

			// 2. Start the background work
			_downloadTask = Task.Run(() =>
			{
				List<(string,string)> filesDownloaded = new List<(string, string)>();
				
				
				if(numberOfBits==32 || numberOfBits==-1)
				{
					UpdateToastStatus($"Downloading E3Next and MQ update");
					System.Threading.Thread.Sleep(2000);
					//DownloadUpdate("E3NextAndMQBinaryNoFramework", "full_e3n_mq_download.zip");
					filesDownloaded.Add(("full_e3n_mq_download.zip",_mqLocation));
					var sgenbits = Program.GetNativeMachineType("mono-2.0-sgen.dll");
					if (sgenbits!=32 || !File.Exists(Path.Combine(_currentDirectory, @"resources\Mono\32bit\bin\")+"mono.exe"))
					{
						UpdateToastStatus($"Downloading Mono framework....");
						System.Threading.Thread.Sleep(2000);
						DownloadUpdate("MQ2Mono-Framework32", "monoframework.zip");
						filesDownloaded.Add(("monoframework.zip",Path.Combine(_currentDirectory,@"resources\Mono\32bit\")));
					}
				}
				else
				{

					//Insert 64bit MQ Here


					var sgenbits = Program.GetNativeMachineType("mono-2.0-sgen.dll");
					if (sgenbits != 64 || !File.Exists(Path.Combine(_currentDirectory, @"resources\Mono\64bit\bin\mono.exe")))
					{
						UpdateToastStatus($"Downloading Mono framework....");
						System.Threading.Thread.Sleep(2000);
						DownloadUpdate("MQ2Mono-Framework64", "monoframework.zip");
						filesDownloaded.Add(("monoframework.zip", Path.Combine(_currentDirectory, @"resources\Mono\64bit\")));

					}
				}

				if (!Directory.Exists(Path.Combine(_currentDirectory, @"resources\MQ2Nav\")))
				{
					UpdateToastStatus($"Downloading MQ2Nav Meshes");
					System.Threading.Thread.Sleep(2000);
					//DownloadUpdate("EmuNavMeshes", "emu_navmeshs.zip");
					filesDownloaded.Add(("emu_navmeshs.zip", Path.Combine(_currentDirectory, @"resources\MQ2Nav\")));

				}
				_syncContext.Post(_ =>
				{
					DialogResult result = MessageBox.Show("Are you ready to apply changes?(this will stop EQ/MQ)",
									 "E3N Updater Confirmation",
									 MessageBoxButtons.YesNo);
					if (result == DialogResult.Yes)
					{
						if (!Debugger.IsAttached) CloseAllEQAndMQ();

						ProcessDownloadedFiles(filesDownloaded);
						
					}
					else
					{
						var data = new NotificationData();
						data.Values["percentValue"] = "indeterminate";
						data.Values["statusText"] = $"Complete!!";
						data.Values["percentString"] = " ";
						ToastNotificationManagerCompat.CreateToastNotifier()
							.Update(data, _toatsTag, _toatsGroupTag);
						System.Threading.Thread.Sleep(2000);
						ToastNotificationManagerCompat.History.Remove(_toatsTag, _toatsGroupTag);
						updateItem.Enabled = true;
						trayIcon.Text = $"E3Next";
						progressItem.Enabled = false;
					}
					
				}, null);
			} 
			
			);
		}	

		

		private void ProcessDownloadedFiles(List<(string, string)> fileNames)
		{
			//file is downloaded, lets decompress it
			_processingTask = Task.Run(() =>
			{
				try
				{
					foreach(var file in fileNames)
					{
						ReadDownloadedZipAndExtract(Path.Combine(_currentDirectory, file.Item1), file.Item2);
					}
					var data = new NotificationData();
					data.Values["percentValue"] = "indeterminate";
					data.Values["statusText"] = $"Complete!!";
					data.Values["percentString"] = " ";
					ToastNotificationManagerCompat.CreateToastNotifier()
						.Update(data, _toatsTag, _toatsGroupTag);
					System.Threading.Thread.Sleep(2000);
					ToastNotificationManagerCompat.History.Remove(_toatsTag, _toatsGroupTag);
					//we should have been done with downloading and decompressing the software
					if (!Debugger.IsAttached)
					{
						DeleteSelfAndStartupNew();
					}
					

					return;
				}
				catch(Exception ex)
				{
					_syncContext.Post(_ =>
					{
						MessageBox.Show("Exception!: " + ex.Message + " stack:" + ex.StackTrace);
						ToastNotificationManagerCompat.History.Remove(_toatsTag, _toatsGroupTag);
						updateItem.Enabled = true;
						trayIcon.Text = $"E3Next";
						progressItem.Enabled = false;
					}, null);
				}
					
			}

			);
	}

		private void DownloadUpdate(string repo,string downloadFileName)
		{
			try
			{
		
				//first lets get the e3nextandmqbinary without framework
				GitHubClient client = new GitHubClient(new ProductHeaderValue("E3NextUpdater"));
				var latestRelease = client.Repository.Release.GetLatest("RekkasGit", repo).Result;

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
						using (var fileStream = new FileStream(downloadFileName, System.IO.FileMode.Create, FileAccess.Write, FileShare.None))
						{
							//set a decent buffer, else there is just too much churn, 1mb is more than enough for fiber download
							byte[] buffer = new byte[1024*1024];
							long totalReadBytes = 0;
							int readBytes;

							while ((readBytes = stream.ReadAsync(buffer, 0, buffer.Length).Result) > 0)
							{
								fileStream.WriteAsync(buffer, 0, readBytes).Wait();
								totalReadBytes += readBytes;

								// Render the progress bar
								//throttle it
								if(stopwatch.ElapsedMilliseconds-startTime>750)
								{
									startTime = stopwatch.ElapsedMilliseconds;
									DrawProgressBar(totalReadBytes);
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
					ToastNotificationManagerCompat.History.Remove(_toatsTag, _toatsGroupTag);
					MessageBox.Show($"Download failed! {ex.Message}");
					trayIcon.Text = $"E3Next";
					
				}, null);
			
				return;
			}


			

		}
		private void CloseAllEQAndMQ()
		{
			return;
			try
			{
				string currentProcess = Process.GetCurrentProcess().MainModule.ModuleName.ToLower().Replace(".exe","");
				var exePaths = Directory.GetFiles(_mqLocation, "*.exe", SearchOption.TopDirectoryOnly)
								   .Select(f => Path.GetFileName(f).ToLower().Replace(".exe", ""))
								   .ToHashSet();

				//kill all proceesses that are in the mq directory, or are equal to eqgame.xe
				foreach (var p in Process.GetProcesses())
				{
					string processNameLower = p.ProcessName.ToLower();
					if (processNameLower != "eqgame" && !exePaths.Contains(processNameLower))
					{
						continue;
					}
					if(processNameLower == currentProcess) { continue; }
					try
					{
						// Skip the current process to avoid self-termination
						UpdateToastStatus($"Closing process: {p.ProcessName} (ID: {p.Id})");
						p.Kill();
					}
					catch (Exception ex)
					{
						// Usually happens for system processes you don't have permission to touch
						UpdateToastStatus($"Could not close {p.ProcessName}. Sleeping for 5 seconds. Error Message: {ex.Message}");
						System.Threading.Thread.Sleep(5000);
					}
				}
				UpdateToastStatus($"Killing all EQ/MQ. Waiting seconds 2 seconds for full close...");

			}
			catch(Exception ex)
			{

				UpdateToastStatus($"Error closing eq instances. message:{ex.Message} stack:{ex.StackTrace}");
			}
			
			System.Threading.Thread.Sleep(2000);
		}
		private void ReadDownloadedZipAndExtract(string zipLocation, string zipDest)
		{
			try
			{
				using (ZipFile zip = ZipFile.Read(zipLocation))
				{	
					//get root path
					string rootPath = zip.Entries.ElementAt(0).FileName;


					UpdateToastStatus($"Extracting files....");
					//we don't want the github generated name, so we will just strip it out

					//selection count is updated each time we extract, so we  only increment when we are going to skip one
					for (Int32 i = 0; i < zip.Entries.Count(); i++)
					{
						var e = zip.Entries.ElementAt(i);
						if (e.FileName == rootPath)
						{
							continue;
						}
						e.FileName = e.FileName.Replace(rootPath, "");
						Console.WriteLine("Extracing file to:"+e.FileName);

						//move the systray to the root of the mq folder
						if (e.FileName.IndexOf("mono/macros/e3/E3NextSysTray.exe", 0, StringComparison.OrdinalIgnoreCase) > -1)
						{
							e.FileName = "E3NextSysTray.exe";
						}

						try
						{
							if (e.FileName.IndexOf("config/", 0, StringComparison.OrdinalIgnoreCase) > -1)
							{
								e.Extract(zipDest, ExtractExistingFileAction.DoNotOverwrite);
							}
							else if (e.FileName.IndexOf("lua/", 0, StringComparison.OrdinalIgnoreCase) > -1)
							{
								e.Extract(zipDest, ExtractExistingFileAction.DoNotOverwrite);
							}
							else if (e.FileName.IndexOf("resources/", 0, StringComparison.OrdinalIgnoreCase) > -1)
							{
								e.Extract(zipDest, ExtractExistingFileAction.DoNotOverwrite);
							}
							else
							{
								e.Extract(zipDest, ExtractExistingFileAction.OverwriteSilently);
							}

						}
						catch (Exception ex)
						{
							Console.WriteLine($"Error Extract:{ex.Message} stack:{ex.StackTrace}");
						}
					}
				}

				try
				{
					UpdateToastStatus($"Cleaning up files files....");
					//File.Delete(zipLocation);
				}
				catch (Exception)
				{

				}
			}
			catch(Exception ex)
			{
				UpdateToastStatus($"Error Extract:{ex.Message} stack:{ex.StackTrace}");
				System.Threading.Thread.Sleep(10000);

			}


		}
		private  void DrawProgressBar(long totalReadBytes)
		{
				// If Content-Length is missing (common with dynamic zip streams), show accumulated megabytes
				double mbRead = totalReadBytes / 1024.0 / 1024.0;
				
				UpdateToastStatus($"Total Downloaded:{mbRead:F2} MB");
		}
		private void CheckForUpdates()
		{
			GitHubClient client = new GitHubClient(new ProductHeaderValue("E3NextUpdater"));
			var latestRelease = client.Repository.Release.GetLatest("RekkasGit", "E3NextAndMQBinaryNoFramework").Result;

			if (latestRelease.TagName != _versionNumber)
			{
				string tempPngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "E3Next.png");
				Uri fileUri = new Uri("file:///" + tempPngPath.Replace("\\", "/"), UriKind.Absolute);

				Console.WriteLine("New version!");
				new ToastContentBuilder()
				.AddAppLogoOverride(fileUri, ToastGenericAppLogoCrop.Circle)
				.SetToastScenario(ToastScenario.Default)
				.AddText($"New version available! {latestRelease.TagName}")
				.AddVisualChild(new AdaptiveProgressBar()
				{
					Title = "E3N Updater",
					Value = new BindableProgressBarValue("percentValue"),
					Status = new BindableString("statusText")
				})
				 .AddButton(new ToastButton("Hide", "dismissed"))
				.Show(toast =>
				{
					toast.Tag = _toatsTag;
					toast.Group = _toatsGroupTag;
					toast.Data = new NotificationData();
					toast.Data.Values["percentValue"] = "indeterminate"; // Bouncing indeterminate bar
					toast.Data.Values["statusText"] = $"New version available! {latestRelease.TagName}";
					toast.Data.Values["percentString"] = " ";
				});
				updateItem.Enabled = true;
			}
			else
			{
				string tempPngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "E3Next.png");
				Uri fileUri = new Uri("file:///" + tempPngPath.Replace("\\", "/"), UriKind.Absolute);

				Console.WriteLine("Fully updated!");
				new ToastContentBuilder()
				.AddAppLogoOverride(fileUri, ToastGenericAppLogoCrop.Circle)
				.SetToastScenario(ToastScenario.Default)
				.AddText($"Fully updated!! {latestRelease.TagName}")
				.AddVisualChild(new AdaptiveProgressBar()
				{
					Title = "E3N Updater",
					Value = new BindableProgressBarValue("percentValue"),
					Status = new BindableString("statusText")
				})
				 .AddButton(new ToastButton("Hide", "dismissed"))
				.Show(toast =>
				{
					toast.Tag = _toatsTag;
					toast.Group = _toatsGroupTag;
					toast.Data = new NotificationData();
					toast.Data.Values["percentValue"] = "indeterminate"; // Bouncing indeterminate bar
					toast.Data.Values["statusText"] = $"Fully updated! {latestRelease.TagName}";
					toast.Data.Values["percentString"] = " ";
				});
				updateItem.Enabled = false;


			}
		}


	}
}
