using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LiveSplit.ComponentUtil;

namespace LiveSplit.UI.Components
{
    class MemeHooks
    {
        public string state;

        [DllImport("winmm.dll")]
        static extern uint timeBeginPeriod(uint uMilliseconds);
        [DllImport("winmm.dll")]
        static extern uint timeEndPeriod(uint uMilliseconds);

        private Task _thread;
        private CancellationTokenSource _cancelSource;

        public float targetSpeed = 1.0f;
        public float lastSpeed = 1.0f;

        public MemeHooks()
        {
            this.state = "Loading...";
            _cancelSource = new CancellationTokenSource();
            _thread = Task.Factory.StartNew(() => MemoryReadThread(_cancelSource));
        }

        Process GetGameProcess()
        {
            Process p = Process.GetProcesses().FirstOrDefault(x => x.ProcessName.ToLower().Equals("hppoa"));

            if (p == null || p.HasExited)
                return null;

            // process is up, check if engine and server are both loaded yet
            ProcessModuleWow64Safe render = p.ModulesWow64Safe().FirstOrDefault(x => x.ModuleName.ToLower() == "engine.dll");
            if (render == null)
                return null;

            return p;
        }


        void HandleProcess(Process game, CancellationTokenSource cts)
        {

            ProcessModuleWow64Safe render = game.ModulesWow64Safe().FirstOrDefault(x => x.ModuleName.ToLower() == "engine.dll");

            var renderBase = render.BaseAddress;
            var speedPtr = new DeepPointer(renderBase+ 0x004E0298, new int[] { 0x68, 0x9C, 0x484 });

            while (!game.HasExited && !cts.IsCancellationRequested)
            {
                // iteration must never take longer than 1 tick
                byte[] CurrentSpeedBytes = speedPtr.DerefBytes(game, 4);
                float CurrentSpeed = BitConverter.ToSingle(CurrentSpeedBytes, 0);
                this.state = "Current game speed = " + CurrentSpeed.ToString("0.00");

                IntPtr ptr;
                

                if((CurrentSpeed == 1.0f || (CurrentSpeed == lastSpeed && lastSpeed != targetSpeed)) && speedPtr.DerefOffsets(game, out ptr))
                {
                    byte[] buffer = BitConverter.GetBytes(targetSpeed);
                    game.WriteBytes(ptr, buffer);
                    lastSpeed = targetSpeed;
                }

            }
        }

        void MemoryReadThread(CancellationTokenSource cts)
        {
            // force windows timer resolution to 1ms. it probably already is though, from the game.
            timeBeginPeriod(1);
            // we do a lot of timing critical stuff so this may help out
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal;
            while (true)
            {
                state = "Waiting for process";
                try
                {
                    Process game;
                    game = this.GetGameProcess();
                    while (game == null)
                    {
                        Thread.Sleep(750);

                        if (cts.IsCancellationRequested)
                            goto ret;

                        game = this.GetGameProcess();
                    }

                    this.HandleProcess(game, cts);

                    if (cts.IsCancellationRequested)
                        goto ret;
                }
                catch (Exception ex) // probably a Win32Exception on access denied to a process
                {
                    Trace.WriteLine(ex.ToString());
                    Thread.Sleep(1000);
                }
            }

        ret:

            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
            timeEndPeriod(1);
        }
    }
}

