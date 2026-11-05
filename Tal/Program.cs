// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Application entry afterint.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Single instance + file arguments + named pipe IPC.
// Drugi proces z plikami w argumentach przekazuje je do pierwszego i wychodzi.

using System.IO.Pipes;
using System.Text;

namespace Tal
{
    internal static class Program
    {
        #region Constants

        private const string MUTEX_NAME = "Global\\TAL_Crystallographic_App_Mutex";
        private const string PIPE_NAME = "TAL_Crystallographic_App_Pipe";

        #endregion

        #region Main

        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();

 // File association registration — idempotent, once per session
            try
            {
                MainForm.EnsureFileAssociations();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Program] EnsureFileAssociations failed: {ex.Message}");
            }

 // Extract existing files from arguments (handles "C:\x\y.cif" and C:\x\y.cif)
            var startupFiles = ExtractExistingFiles(args);

            if (startupFiles.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Program] Startup files ({startupFiles.Count}): " +
                    string.Join(", ", startupFiles));
            }

 // ═══════════════════════════════════════════════════════════
 // SINGLE INSTANCE — Mutex
 // ═══════════════════════════════════════════════════════════
            bool isFirstInstance;
            using var mutex = new Mutex(true, MUTEX_NAME, out isFirstInstance);

            if (!isFirstInstance)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[Program] Another instance is running — sending files and exiting");

                SendFilesToRunningInstance(startupFiles);
                return;   // ⭐ NIE otwieraj drugiego okna
            }

 // ═══════════════════════════════════════════════════════════
 // FIRST PROCESS — starts the pipe server (listens for subsequent files)
 // ═══════════════════════════════════════════════════════════
            StartPipeServer();

 // ═══════════════════════════════════════════════════════════
 // SPLASH
 // ═══════════════════════════════════════════════════════════
            try
            {
                using var splash = new SplashForm();
                splash.Show();
                Application.DoEvents();
                System.Threading.Thread.Sleep(1500);
                splash.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Splash error: {ex}");
                MessageBox.Show(
                    $"Splash failed (program will continue):\n{ex.Message}",
                    "TAL",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

 // ═══════════════════════════════════════════════════════════
 // MAIN FORM
 // ═══════════════════════════════════════════════════════════
            try
            {
                Application.Run(new MainForm(startupFiles));
            }
            finally
            {
                try { mutex.ReleaseMutex(); }
                catch { /* ignore */ }
            }
        }

        #endregion

        #region Argument Parsing

 /// <summary>
 /// Extracts only existing files from args, normalises paths.
 /// Handles "C:\x\y.cif" (with quotes) and C:\x\y.cif (without).
 /// </summary>
        private static List<string> ExtractExistingFiles(string[] args)
        {
            var result = new List<string>();
            if (args == null || args.Length == 0) return result;

            foreach (var arg in args)
            {
                if (string.IsNullOrWhiteSpace(arg)) continue;

                string candidate = arg.Trim().Trim('"');

                try
                {
                    if (File.Exists(candidate))
                    {
                        string fullPath = Path.GetFullPath(candidate);

 // deduplikacja (case-insensitive)
                        bool dup = false;
                        foreach (var existing in result)
                        {
                            if (string.Equals(existing, fullPath,
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                dup = true;
                                break;
                            }
                        }

                        if (!dup)
                            result.Add(fullPath);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[Program] Arg is not an existing file: '{candidate}'");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Program] Arg parse error for '{candidate}': {ex.Message}");
                }
            }

            return result;
        }

        #endregion

        #region IPC — Client (second process sends files)

        private static void SendFilesToRunningInstance(List<string> files)
        {
            if (files == null || files.Count == 0)
            {
 // Nothing to send — simply exit
                return;
            }

            try
            {
                using var pipe = new NamedPipeClientStream(
                    ".", PIPE_NAME, PipeDirection.Out);

                pipe.Connect(3000);   // 3 s connection timeout

                using var writer = new StreamWriter(pipe, new UTF8Encoding(false))
                {
                    AutoFlush = true
                };

                foreach (var f in files)
                    writer.WriteLine(f);

 // Pusty wiersz jako terminator (serwer i tak przerwie after EOF)
                writer.WriteLine();

                System.Diagnostics.Debug.WriteLine(
                    $"[Program] Sent {files.Count} files to running instance");
            }
            catch (TimeoutException)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[Program] Pipe connect timeout — is the other instance hung?");
                ShowSendFailureMessage(files);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Program] Pipe send failed: {ex.Message}");
                ShowSendFailureMessage(files);
            }
        }

        private static void ShowSendFailureMessage(List<string> files)
        {
            try
            {
                MessageBox.Show(
                    "TAL is already running, but failed to pass the files:\n\n" +
                    string.Join("\n", files),
                    "TAL",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch { /* ignore */ }
        }

        #endregion

        #region IPC — Server (first process listens)

 /// <summary>
 /// Starts a background thread that listens on the NamedPipe.
 /// Each connection = list of file paths to open.
 /// </summary>
        private static void StartPipeServer()
        {
            var thread = new System.Threading.Thread(PipeServerLoop)
            {
                IsBackground = true,
                Name = "TAL.PipeServer"
            };
            thread.Start();

            System.Diagnostics.Debug.WriteLine("[Program] Pipe server thread started");
        }

        private static void PipeServerLoop()
        {
            while (true)
            {
                try
                {
                    using var pipe = new NamedPipeServerStream(
                        PIPE_NAME,
                        PipeDirection.In,
                        maxNumberOfServerInstances: 1,
                        transmissionMode: PipeTransmissionMode.Byte,
                        options: PipeOptions.None);

                    pipe.WaitForConnection();

                    System.Diagnostics.Debug.WriteLine(
                        "[Pipe] Client connected");

                    using var reader = new StreamReader(pipe, Encoding.UTF8);

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        string path = line.Trim();
                        System.Diagnostics.Debug.WriteLine(
                            $"[Pipe] Received file: {path}");

                        DispatchToMainForm(path);
                    }

                    System.Diagnostics.Debug.WriteLine(
                        "[Pipe] Client disconnected");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Pipe] Server loop error: {ex.Message}");

 // Short pause to avoid busy-loop on persistent error
                    System.Threading.Thread.Sleep(500);
                }
            }
        }

 /// <summary>
 /// Calls OpenFileInternal on the MainForm UI thread.
 /// </summary>
        private static void DispatchToMainForm(string filePath)
        {
            var form = MainForm.Instance;

            if (form == null || form.IsDisposed || !form.IsHandleCreated)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Pipe] MainForm not ready — dropping: {filePath}");
                return;
            }

            try
            {
                form.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (!File.Exists(filePath))
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"[Pipe] File gone before open: {filePath}");
                            return;
                        }

                        form.OpenFileInternal(filePath);

 // Bring the window to the front
                        if (form.WindowState == FormWindowState.Minimized)
                            form.WindowState = FormWindowState.Normal;

                        form.Activate();
                        form.BringToFront();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[Pipe] OpenFileInternal error: {ex.Message}");
                    }
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Pipe] BeginInvoke failed: {ex.Message}");
            }
        }

        #endregion
    }
}